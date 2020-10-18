using System;
using System.Collections.Generic;
using System.Linq;


namespace Basic_Script_Interpreter
{

    // SyntaxAnalyser: Führt die Syntaxanalyser durch, erzeugt
    // aber auch den Code. Der Parser steuert den ganzen Übersetzungsprozeß.

    public class SyntaxAnalyser
    {

        public SyntaxAnalyser(InterpreterError interpreterError)
        {
            errorObject = interpreterError;
        }
        private enum Exits
        {
            exitNone = 0,
            exitDo = 1,
            exitFor = 2,
            exitFunction = 4,
            exitSub = 8
        }

        private LexicalAnalyser lex = new LexicalAnalyser();
        private Symbol sym = new Symbol(); // das jeweils aktuelle Symbol
        private Scopes symboltable;
        // Symboltabelle während der Übersetzungszeit. Sie vermerkt, welche
        // Identifier mit welchem Typ usw. bereits definiert sind und simuliert
        // die zur Laufzeit nötigen Gültigkeitsbereiche

        private bool optionExplicit; // müssen alle Identifier vor Benutzung explizit deklariert werden?
        private bool allowExternal; // sollen EXTERNAL-Definitionen von Variablen erlaubt sein?
        private Code code;
        private InterpreterError errorObject;



        public Code Parse(Code.IInputStream source, Code code, bool optionExplicit = true, bool allowExternal = true)
        {
            lex.Connect(source, code.ErrorObject);
            // globalen und ersten Gültigkeitsbereich anlegen
            symboltable = new Scopes();
            symboltable.PushScope();

            errorObject = code.ErrorObject;
            this.code = code;
            this.optionExplicit = optionExplicit;
            this.allowExternal = allowExternal;



            // --- Erstes Symbol lesen
            GetNextSymbol();

            // --- Wurzelregel der Grammatik aufrufen und Syntaxanalyse starten
            StatementList(false, true, (int)Exits.exitNone, Symbol.Tokens.tokEOF);

            if (errorObject.Number != 0)
            {
                return this.code;
            }


            // Wenn wir hier ankommen, dann muß der komplette Code korrekt erkannt
            // worden sein, d.h. es sind alle Symbole gelesen. Falls also nicht
            // das EOF-Symbol aktuell ist, ist ein Symbol nicht erkannt worden und
            // ein Fehler liegt vor.
            if (!sym.Token.Equals(Symbol.Tokens.tokEOF))
                errorObject.Raise((int)InterpreterError.parsErrors.errUnexpectedSymbol, "SyntaxAnalyser.Parse", "Expected: end of statement", sym.Line, sym.Col, sym.Index, sym.Text);

            return this.code;
        }



        private Symbol GetNextSymbol()
        {
            sym = lex.GetNextSymbol();
            return sym;
        }

        private bool InSymbolSet(Symbol.Tokens Token, params Symbol.Tokens[] tokenSet)
        {
            //for (int i = 0; i <= tokenSet.Length - 1; i++)
            //{
            //    if (Token.Equals(tokenSet[i]))
            //    {
            //        return true;

            //    }
            //}

            return tokenSet.Contains(Token);
            //return false;
        }


        private void StatementList(bool singleLineOnly, bool allowFunctionDeclarations, int exitsAllowed, params Symbol.Tokens[] endSymbols)
        {
           
            do
            {
                
                // Anweisungstrenner (":" und Zeilenwechsel) überspringen
                while (sym.Token == Symbol.Tokens.tokStatementDelimiter)
                {
                    if (sym.Text == "\n" & singleLineOnly) { return; }

                    GetNextSymbol();
                }

                // Wenn eines der übergebenen Endesymbole erreicht ist, dann
                // Listenverarbeitung beenden
                if (endSymbols.Contains(sym.Token)) { return; }



                // Alles ok, die nächste Anweisung liegt an...
                Statement(singleLineOnly, allowFunctionDeclarations, exitsAllowed);

            }
            while (true);
        }


        private void Statement(bool singleLineOnly, bool allowFunctionDeclarations, int exitsAllowed)
        {
            string Ident;

            // Symbol.Tokens op;
            switch (sym.Token)
            {
                case Symbol.Tokens.tokEXTERNAL:
                    {
                        if (allowExternal)
                        {
                            GetNextSymbol();

                            VariableDeclaration(true);
                        }
                        else
                            errorObject.Raise((int)InterpreterError.parsErrors.errSyntaxViolation, "SyntaxAnalyser.Statement", "IMPORT declarations not allowed", sym.Line, sym.Col, sym.Index, sym.Text);
                        break;
                    }

                case Symbol.Tokens.tokCONST:
                    {
                        GetNextSymbol();

                        ConstDeclaration();
                        break;
                    }

                case Symbol.Tokens.tokDIM:
                    {
                        GetNextSymbol();

                        VariableDeclaration(false);
                        break;
                    }

                case Symbol.Tokens.tokFUNCTION:
                    {
                        if (allowFunctionDeclarations)
                            FunctionDefinition();
                        else
                            // im aktuellen Kontext (z.B. innerhalb einer FOR-Schleife)
                            // ist eine Funktionsdefinition nicht erlaubt
                            errorObject.Raise((int)InterpreterError.parsErrors.errSyntaxViolation, "SyntaxAnalyser.Statement", "No function declarations allowed at this point", sym.Line, sym.Col, sym.Index, sym.Text);
                        break;
                    }
                case Symbol.Tokens.tokSUB:
                    {
                        if (allowFunctionDeclarations)
                            FunctionDefinition();
                        else
                            // im aktuellen Kontext (z.B. innerhalb einer FOR-Schleife)
                            // ist eine Funktionsdefinition nicht erlaubt
                            errorObject.Raise((int)InterpreterError.parsErrors.errSyntaxViolation, "SyntaxAnalyser.Statement", "No function declarations allowed at this point", sym.Line, sym.Col, sym.Index, sym.Text);
                        break;
                    }

                case Symbol.Tokens.tokIF:
                    {
                        GetNextSymbol();

                        IFStatement(singleLineOnly, exitsAllowed);
                        break;
                    }

                case Symbol.Tokens.tokFOR:
                    {
                        GetNextSymbol();

                        FORStatement(singleLineOnly, exitsAllowed);
                        break;
                    }

                case Symbol.Tokens.tokDO:
                    {
                        GetNextSymbol();

                        DoStatement(singleLineOnly, exitsAllowed);
                        break;
                    }

                case Symbol.Tokens.tokEXIT:
                    {
                        GetNextSymbol();

                        switch (sym.Token)
                        {
                            case Symbol.Tokens.tokDO:
                                {
                                    if (exitsAllowed + Exits.exitDo == Exits.exitDo)

                                        code.Add(Code.Opcodes.opJumpPop); // Exit-Adresse liegt auf dem Stack
                                    else
                                        errorObject.Raise((int)InterpreterError.parsErrors.errUnexpectedSymbol, "SyntaxAnalyser.Statement", "'EXIT DO' not allowed at this point", sym.Line, sym.Col, sym.Index, sym.Text);
                                    break;
                                }

                            case Symbol.Tokens.tokFOR:
                                {
                                    if ((Convert.ToByte(exitsAllowed) & Convert.ToByte(Exits.exitFor)) == Convert.ToByte(Exits.exitFor))

                                        code.Add(Code.Opcodes.opJumpPop); // Exit-Adresse liegt auf dem Stack
                                    else
                                        errorObject.Raise((int)InterpreterError.parsErrors.errUnexpectedSymbol, "SyntaxAnalyser.Statement", "'EXIT FOR' not allowed at this point", sym.Line, sym.Col, sym.Index, sym.Text);
                                    break;
                                }

                            case Symbol.Tokens.tokSUB:
                                {
                                    if ((Convert.ToByte(exitsAllowed) & Convert.ToByte(Exits.exitSub)) == Convert.ToByte(Exits.exitSub))

                                        // zum Ende der aktuellen Funktion spring
                                        code.Add(Code.Opcodes.opReturn);
                                    else if ((Convert.ToByte(exitsAllowed) & Convert.ToByte(Exits.exitFunction)) == Convert.ToByte(Exits.exitFunction))
                                        errorObject.Raise((int)InterpreterError.parsErrors.errUnexpectedSymbol, "SyntaxAnalyser.Statement", "Expected: 'EXIT FUNCTION' in function", sym.Line, sym.Col, sym.Index, sym.Text);
                                    else
                                        errorObject.Raise((int)InterpreterError.parsErrors.errUnexpectedSymbol, "SyntaxAnalyser.Statement", "'EXIT SUB' not allowed at this point", sym.Line, sym.Col, sym.Index, sym.Text);
                                    break;
                                }

                            case Symbol.Tokens.tokFUNCTION:
                                {
                                    if ((Convert.ToByte(exitsAllowed) & Convert.ToByte(Exits.exitFunction)) == Convert.ToByte(Exits.exitFunction))

                                        code.Add(Code.Opcodes.opReturn);
                                    else if ((Convert.ToByte(exitsAllowed) & +Convert.ToByte(Exits.exitSub)) == Convert.ToByte(Exits.exitSub))
                                        errorObject.Raise((int)InterpreterError.parsErrors.errUnexpectedSymbol, "SyntaxAnalyser.Statement", "Expected: 'EXIT SUB' in sub", sym.Line, sym.Col, sym.Index, sym.Text);
                                    else
                                        errorObject.Raise((int)InterpreterError.parsErrors.errUnexpectedSymbol, "SyntaxAnalyser.Statement", "'EXIT FUNCTION' not allowed at this point", sym.Line, sym.Col, sym.Index, sym.Text);
                                    break;
                                }

                            default:
                                {
                                    errorObject.Raise((int)InterpreterError.parsErrors.errUnexpectedSymbol, "SyntaxAnalyser.Statement", "Expected: 'DO' or 'FOR' or 'FUNCTION' after 'EXIT'", sym.Line, sym.Col, sym.Index, sym.Text);
                                    break;
                                }
                        }

                        GetNextSymbol();
                        break;
                    }

                case Symbol.Tokens.tokDebugPrint:
                    {
                        GetNextSymbol();

                        if (errorObject.Number == 0)
                        {
                            ActualOptionalParameter("");
                            code.Add(Code.Opcodes.opDebugPrint);

                        }


                        break;
                    }

                case Symbol.Tokens.tokDebugClear:
                    {

                        code.Add(Code.Opcodes.opDebugClear);

                        GetNextSymbol();
                        break;
                    }

                case Symbol.Tokens.tokDebugShow:
                    {

                        code.Add(Code.Opcodes.opDebugShow);

                        GetNextSymbol();
                        break;
                    }

                case Symbol.Tokens.tokDebugHide:
                    {

                        code.Add(Code.Opcodes.opDebugHide);

                        GetNextSymbol();
                        break;
                    }

                case Symbol.Tokens.tokMessage:
                    {
                        GetNextSymbol();

                        CallMsg(true);
                        break;
                    }

                case Symbol.Tokens.tokMsgbox:
                    {
                        GetNextSymbol();

                        CallMsgBox(true);
                        break;
                    }

                case Symbol.Tokens.tokDoEvents:
                    {

                        code.Add(Code.Opcodes.opDoEvents);

                        GetNextSymbol();
                        break;
                    }

                case Symbol.Tokens.tokInputbox:
                    {
                        GetNextSymbol();

                        CallInputbox(true);
                        break;
                    }

                case Symbol.Tokens.tokIdentifier:
                    {
                        Ident = sym.Text;

                        GetNextSymbol();

                        switch (sym.Token)
                        {
                            case Symbol.Tokens.tokEq:
                                StatementComparativeOperators(Ident);
                                break;
                            case Symbol.Tokens.tokPlusEq:
                                StatementComparativeOperators(Ident);
                                break;
                            case Symbol.Tokens.tokMinusEq:
                                StatementComparativeOperators(Ident);
                                break;
                            case Symbol.Tokens.tokMultiplicationEq:
                                StatementComparativeOperators(Ident);
                                break;
                            case Symbol.Tokens.tokDivisionEq:
                                StatementComparativeOperators(Ident);
                                break;
                            case Symbol.Tokens.tokStringConcatEq:
                                StatementComparativeOperators(Ident);
                                break;
                            case Symbol.Tokens.tokDivEq:
                                StatementComparativeOperators(Ident);
                                break;
                            case Symbol.Tokens.tokModEq:
                                StatementComparativeOperators(Ident);
                                break;

                            default:
                                {
                                    CallUserdefinedFunction(Ident);
                                    break;
                                }
                        }

                        break;
                    }

                default:
                    {
                        errorObject.Raise((int)InterpreterError.parsErrors.errUnexpectedSymbol, "SyntaxAnalyser.Statement", "Expected: declaration, function call or assignment", sym.Line, sym.Col, sym.Index, sym.Text);
                        break;
                    }
            }
        }

        private void ConstDeclaration()
        {
            string ident;
            if (sym.Token == Symbol.Tokens.tokIdentifier)
            {

                // Wurde Identifier schon für etwas anderes in diesem Scope benutzt?
                if (symboltable.Exists(sym.Text, true))
                    errorObject.Raise((int)InterpreterError.parsErrors.errIdentifierAlreadyExists, "ConstDeclaration", "Constant identifier '" + sym.Text + "' is already declared", sym.Line, sym.Col, sym.Index, sym.Text);

                ident = sym.Text;

                GetNextSymbol();

                if (sym.Token == Symbol.Tokens.tokEq)
                {
                    GetNextSymbol();

                    if (sym.Token == Symbol.Tokens.tokNumber | sym.Token == Symbol.Tokens.tokString)
                    {

                        symboltable.Allocate(ident, sym.Value, Identifier.IdentifierTypes.idConst);
                        code.Add(Code.Opcodes.opAllocConst, ident, sym.Value);

                        GetNextSymbol();
                    }
                    else
                        errorObject.Raise((int)InterpreterError.parsErrors.errUnexpectedSymbol, "SyntaxAnalyser.ConstDeclaration", "Expected: const Value", sym.Line, sym.Col, sym.Index, sym.Text);
                }
                else
                    errorObject.Raise((int)InterpreterError.parsErrors.errUnexpectedSymbol, "SyntaxAnalyser.ConstDeclaration", "Expected: '=' after const identifier", sym.Line, sym.Col, sym.Index, sym.Text);
            }
            else
                errorObject.Raise((int)InterpreterError.parsErrors.errUnexpectedSymbol, "SyntaxAnalyser.ConstDeclaration", "Expected: const identifier", sym.Line, sym.Col, sym.Index, sym.Text);
        }


        private void VariableDeclaration(bool external)
        {
            do
            {
                if (sym.Token == Symbol.Tokens.tokIdentifier)
                {

                    if (symboltable.Exists(sym.Text, true))
                        errorObject.Raise((int)InterpreterError.parsErrors.errIdentifierAlreadyExists, "VariableDeclaration", "Variable identifier '" + sym.Text + "' is already declared", sym.Line, sym.Col, sym.Index, sym.Text);
                    if (external)
                        symboltable.Allocate(sym.Text);
                    else
                    {
                        symboltable.Allocate(sym.Text);
                        code.Add(Code.Opcodes.opAllocVar, sym.Text);
                    }

                    GetNextSymbol();
                    if (sym.Token == Symbol.Tokens.tokComma)
                        GetNextSymbol();
                    else
                        break;
                }
                else
                    errorObject.Raise((int)InterpreterError.parsErrors.errUnexpectedSymbol, "SyntaxAnalyser.VariableDeclaration", "Expected: variable identifier", sym.Line, sym.Col, sym.Index, sym.Text);
            }
            while (true);
        }


        private void FunctionDefinition()
        {
            string ident;
            List<object> formalParameters = new List<object>();
            int skipFunctionPC;
            bool isSub;

            isSub = Convert.ToBoolean(sym.Token == Symbol.Tokens.tokSUB);

            GetNextSymbol();

            Identifier definition;
            if (sym.Token == Symbol.Tokens.tokIdentifier)
            {
                ident = sym.Text; // Der Funktionsname ist immer an Position 1 in der collection

                GetNextSymbol();

                if (sym.Token == Symbol.Tokens.tokLeftParent)
                {
                    // Liste der formalen Parameter abarbeiten
                    GetNextSymbol();

                    while (sym.Token == Symbol.Tokens.tokIdentifier)
                    {
                        formalParameters.Add(sym.Text);

                        GetNextSymbol();

                        if (sym.Token != Symbol.Tokens.tokComma)
                            break;
                        GetNextSymbol();
                    }

                    if (sym.Token == Symbol.Tokens.tokRightParent)
                        GetNextSymbol();
                    else
                        errorObject.Raise((int)InterpreterError.parsErrors.errSyntaxViolation, "SyntaxAnalyser.FunctionDefinition", "Expected: ',' or ')' or identifier", sym.Line, sym.Col, sym.Index, sym.Text);
                }


                // Funktion im aktuellen Scope definieren
                definition = symboltable.Allocate(ident, null, isSub == true ? Identifier.IdentifierTypes.idSub : Identifier.IdentifierTypes.idFunction);
                code.Add(Code.Opcodes.opAllocVar, ident); // Funktionsvariable anlegen

                // in der sequentiellen Codeausführung die Funktion überspringen
                skipFunctionPC = code.Add(Code.Opcodes.opJump);
                definition.Address = code.EndOfCodePC;

                // Neuen Scope für die Funktion öffnen
                symboltable.PushScope();

                // Formale Parameter als lokale Variablen der Funktion definieren
                definition.FormalParameters = formalParameters;
                for (int i = 0; i <= formalParameters.Count() - 1; i++)
                    symboltable.Allocate(formalParameters[i].ToString(), null, Identifier.IdentifierTypes.idVariable);

                // Funktionsrumpf übersetzen
                // (er darf auch wieder Funktionsdefinitionen enthalten!)
                StatementList(false, true, isSub == true ? (int)Exits.exitSub : (int)Exits.exitFunction, Symbol.Tokens.tokEOF, Symbol.Tokens.tokEND, Symbol.Tokens.tokENDFUNCTION, Symbol.Tokens.tokENDSUB); // geschachtelte Funktionsdefinitionen erlaubt

                // Ist die Funktion korrekt abgeschlossen worden?
                // (etwas unelegant, aber irgendwie nicht zu umgehen, wenn man
                // mehrwortige Endsymbole und Alternativen erlauben will)
                if (sym.Token == Symbol.Tokens.tokENDFUNCTION | sym.Token == Symbol.Tokens.tokENDSUB)
                {
                    if (isSub & sym.Token != Symbol.Tokens.tokENDSUB)
                        errorObject.Raise((int)InterpreterError.parsErrors.errSyntaxViolation, "SyntaxAnalyser.FunctionDefinition", "Expected: 'END SUB' or 'ENDSUB' at end of sub body", sym.Line, sym.Col, sym.Index, sym.Text);

                    GetNextSymbol();

                    goto GenerateEndFunctionCode;
                }
                else if (sym.Token == Symbol.Tokens.tokEND)
                {
                    GetNextSymbol();

                    if (isSub)
                    {
                        if (sym.Token == Symbol.Tokens.tokSUB)
                        {
                            GetNextSymbol();
                            goto GenerateEndFunctionCode;
                        }
                        else
                            errorObject.Raise((int)InterpreterError.parsErrors.errSyntaxViolation, "SyntaxAnalyser.FunctionDefinition", "Expected: 'END SUB' or 'ENDSUB' at end of sub body", sym.Line, sym.Col, sym.Index, sym.Text);
                    }
                    else if (sym.Token == Symbol.Tokens.tokFUNCTION)
                    {
                        GetNextSymbol();
                        goto GenerateEndFunctionCode;
                    }
                    else
                        errorObject.Raise((int)InterpreterError.parsErrors.errSyntaxViolation, "SyntaxAnalyser.FunctionDefinition", "Expected: 'END FUNCTION' or 'ENDFUNCTION' at end of function body", sym.Line, sym.Col, sym.Index, sym.Text);
                }
                else
                    errorObject.Raise((int)InterpreterError.parsErrors.errSyntaxViolation, "SyntaxAnalyser.FunctionDefinition", "Expected: 'END FUNCTION'/'ENDFUNCTION', 'END SUB'/'ENDSUB' at end of function body", sym.Line, sym.Col, sym.Index, sym.Text);
            }
            else
                errorObject.Raise((int)InterpreterError.parsErrors.errSyntaxViolation, "SyntaxAnalyser.FunctionDefinition", "Function/Sub Name is missing in definition", sym.Line, sym.Col, sym.Index, sym.Text);

            return;

        GenerateEndFunctionCode:

            symboltable.PopScopes(-1); // lokalen Gültigkeitsbereich wieder verwerfen

            code.Add(Code.Opcodes.opReturn);

            code.FixUp(skipFunctionPC - 1, code.EndOfCodePC);
        }


        // FORStatement ::= "FOR" variable "=" Value "TO" [ "STEP" Value ] Value Statementlist "NEXT"
        // (Achtung: bei FOR, DO, IF kann die Anweisung entweder über mehrere Zeilen laufen
        // und muß dann mit einem entsprechenden Endesymbol abgeschlossen sein (NEXT, END IF usw.). Oder
        // sie umfaßt nur 1 Zeile und bedarf keines Abschlusses! Daher der Aufwand mit
        // singleLineOnly und thisFORisSingleLineOnly.
        private void FORStatement(bool singleLineOnly, int exitsAllowed)
        {
            string counterVariable = string.Empty;

            if (sym.Token == Symbol.Tokens.tokIdentifier)
            {
                int forPC, pushExitAddrPC;
                bool thisFORisSingleLineOnly;
                if (optionExplicit & !symboltable.Exists(sym.Text, null, Identifier.IdentifierTypes.idVariable))
                    errorObject.Raise((int)InterpreterError.parsErrors.errIdentifierAlreadyExists, "SyntaxAnalyser.FORStatement", "Variable '" + sym.Text + "' not declared", sym.Line, sym.Col, sym.Index, sym.Text);

                counterVariable = sym.Text;

                GetNextSymbol();

                if (sym.Token == Symbol.Tokens.tokEq)
                {
                    GetNextSymbol();

                    Condition(); // Startwert der FOR-Schleife


                    // Startwert (auf dem Stack) der Zählervariablen zuweisen
                    code.Add(Code.Opcodes.opAssign, counterVariable);

                    if (sym.Token == Symbol.Tokens.tokTO)
                    {
                        GetNextSymbol();

                        Condition(); // Endwert der FOR-Schleife

                        if (sym.Token == Symbol.Tokens.tokSTEP)
                        {
                            GetNextSymbol();

                            Condition(); // Schrittweite
                        }
                        else
                            // keine explizite Schrittweite, also default auf 1
                            code.Add(Code.Opcodes.opPushValue, 1);


                        // EXIT-Adresse auf Stack legen. Es ist wichtig, daß sie zuoberst liegt!
                        // Nur so kommen wir jederzeit an sie mit EXIT heran.
                        pushExitAddrPC = code.Add(Code.Opcodes.opPushValue);

                        // hier gehen endlich die Statements innerhalb der Schleife los
                        forPC = code.EndOfCodePC;

                        thisFORisSingleLineOnly = !(sym.Token == Symbol.Tokens.tokStatementDelimiter & sym.Text == "\n");
                        if (sym.Token == Symbol.Tokens.tokStatementDelimiter)
                            GetNextSymbol();

                        singleLineOnly = singleLineOnly | thisFORisSingleLineOnly;

                        // FOR-body
                        StatementList(singleLineOnly, false, Convert.ToByte(Exits.exitFor) | Convert.ToByte(exitsAllowed), Symbol.Tokens.tokEOF, Symbol.Tokens.tokNEXT);

                        if (sym.Token == Symbol.Tokens.tokNEXT)
                            GetNextSymbol();
                        else if (!thisFORisSingleLineOnly)
                            errorObject.Raise((int)InterpreterError.parsErrors.errSyntaxViolation, "SyntaxAnalyser.FORStatement", "Expected: 'NEXT' at end of FOR-statement", sym.Line, sym.Col, sym.Index, sym.Text);


                        // nach den Schleifenstatements wird die Zählervariable hochgezählt und
                        // geprüft, ob eine weitere Runde gedreht werden soll

                        // Wert der counter variablen um die Schrittweite erhöhen
                        code.Add(Code.Opcodes.opPopWithIndex, 1); // Schrittweite
                        code.Add(Code.Opcodes.opPushVariable, counterVariable); // aktuellen Zählervariableninhalt holen
                        code.Add(Code.Opcodes.opAdd); // aktuellen Zähler + Schrittweite
                        code.Add(Code.Opcodes.opAssign, counterVariable); // Zählervariable ist jetzt auf dem neuesten Stand

                        // Prüfen, ob Endwert schon überschritten
                        code.Add(Code.Opcodes.opPopWithIndex, 2); // Endwert
                        code.Add(Code.Opcodes.opPushVariable, counterVariable); // aktuellen Zählervariableninhalt holen (ja, den hatten wir gerade schon mal)
                        code.Add(Code.Opcodes.opGEq); // wenn Endwert >= Zählervariable, dann weitermachen
                        code.Add(Code.Opcodes.opJumpTrue, forPC);

                        // Stack bereinigen von allen durch FOR darauf zwischengespeicherten Werten
                        code.Add(Code.Opcodes.opPop); // Exit-Adresse vom Stack entfernen
                        code.FixUp(pushExitAddrPC - 1, code.EndOfCodePC); // Adresse setzen, zu der EXIT springen soll
                        code.Add(Code.Opcodes.opPop); // Schrittweite
                        code.Add(Code.Opcodes.opPop); // Endwert
                    }
                    else
                        errorObject.Raise((int)InterpreterError.parsErrors.errSyntaxViolation, "SyntaxAnalyser.FORStatement", "Expected: 'TO' after start Value of FOR-statement", sym.Line, sym.Col, sym.Index, sym.Text);
                }
                else
                    errorObject.Raise((int)InterpreterError.parsErrors.errSyntaxViolation, "SyntaxAnalyser.FORStatement", "Expected: '=' after counter variable", sym.Line, sym.Col, sym.Index, sym.Text);
            }
            else
                errorObject.Raise((int)InterpreterError.parsErrors.errSyntaxViolation, "SyntaxAnalyser.FORStatement", "Counter variable missing in FOR-statement", sym.Line, sym.Col, sym.Index, sym.Text);
        }


        // DoStatement ::= "DO" [ "WHILE" Condition ] Statementlist "LOOP" [ ("UNTIL" | "WHILE") Condition ) ]
        private void DoStatement(bool singleLineOnly, int exitsAllowed)
        {


            int conditionPC = 0;
            int doPC;
            int pushExitAddrPC;
            bool thisDOisSingleLineOnly;
            bool doWhile = false;


            // EXIT-Adresse auf den Stack legen

            pushExitAddrPC = code.Add(Code.Opcodes.opPushValue);

            doPC = code.EndOfCodePC;

            if (sym.Token == Symbol.Tokens.tokWHILE)
            {
                // DO-WHILE
                doWhile = true;
                GetNextSymbol();

                Condition();


                conditionPC = code.Add(Code.Opcodes.opJumpFalse);
            }

            thisDOisSingleLineOnly = !(sym.Token == Symbol.Tokens.tokStatementDelimiter & sym.Text == "\n");
            if (sym.Token == Symbol.Tokens.tokStatementDelimiter)
                GetNextSymbol();

            singleLineOnly = singleLineOnly | thisDOisSingleLineOnly;

            // DO-body
            StatementList(singleLineOnly, false, Convert.ToByte(Exits.exitDo) | Convert.ToByte(exitsAllowed), Symbol.Tokens.tokEOF, Symbol.Tokens.tokLOOP);

            bool loopWhile;
            if (sym.Token == Symbol.Tokens.tokLOOP)
            {
                GetNextSymbol();

                switch (sym.Token)
                {
                    case Symbol.Tokens.tokWHILE:
                        {
                            if (doWhile == true)
                                errorObject.Raise((int)InterpreterError.parsErrors.errUnexpectedSymbol, "SyntaxAnalyser.DoStatement", "No 'WHILE'/'UNTIL' allowed after 'LOOP' in DO-WHILE-statement", sym.Line, sym.Col, sym.Index);

                            loopWhile = sym.Token == Symbol.Tokens.tokWHILE;

                            GetNextSymbol();

                            Condition();


                            code.Add(loopWhile == true ? Code.Opcodes.opJumpTrue : Code.Opcodes.opJumpFalse, doPC);
                            // Sprung zum Schleifenanf. wenn Bed. entsprechenden Wert hat

                            code.Add(Code.Opcodes.opPop); // Exit-Adresse vom Stack entfernen

                            code.FixUp(pushExitAddrPC - 1, code.EndOfCodePC); // Adresse setzen, zu der EXIT springen soll
                            break;
                        }
                    case Symbol.Tokens.tokUNTIL:
                        {
                            if (doWhile == true)
                                errorObject.Raise((int)InterpreterError.parsErrors.errUnexpectedSymbol, "SyntaxAnalyser.DoStatement", "No 'WHILE'/'UNTIL' allowed after 'LOOP' in DO-WHILE-statement", sym.Line, sym.Col, sym.Index);

                            loopWhile = sym.Token == Symbol.Tokens.tokWHILE;

                            GetNextSymbol();

                            Condition();


                            code.Add(loopWhile == true ? Code.Opcodes.opJumpTrue : Code.Opcodes.opJumpFalse, doPC);
                            // Sprung zum Schleifenanf. wenn Bed. entsprechenden Wert hat

                            code.Add(Code.Opcodes.opPop); // Exit-Adresse vom Stack entfernen

                            code.FixUp(pushExitAddrPC - 1, code.EndOfCodePC); // Adresse setzen, zu der EXIT springen soll
                            break;
                        }

                    default:
                        {

                            code.Add(Code.Opcodes.opJump, doPC);
                            if (doWhile == true)
                                code.FixUp(conditionPC - 1, code.EndOfCodePC);

                            code.Add(Code.Opcodes.opPop); // Exit-Adresse vom Stack entfernen, sie wurde ja nicht benutzt

                            code.FixUp(pushExitAddrPC - 1, code.EndOfCodePC); // Adresse setzen, zu der EXIT springen soll
                            break;
                        }
                }
            }
            else if (!(doWhile & thisDOisSingleLineOnly))
                errorObject.Raise((int)InterpreterError.parsErrors.errSyntaxViolation, "SyntaxAnalyser.DoStatement", "'LOOP' is missing at end of DO-statement", sym.Line, sym.Col, sym.Index);
        }


        //IFStatement ::= "IF" Condition "THEN" Statementlist [ "ELSE" Statementlist ] [ "END IF" ]
        //("END IF" kann nur entfallen, wenn alle IF-Teile in einer Zeile stehen
        private void IFStatement(bool singleLineOnly, int exitsAllowed)
        {
            bool thisIFisSingleLineOnly;

            Condition();

            int thenPC = 0;
            int elsePC = 0;
            if (sym.Token == Symbol.Tokens.tokTHEN)
            {
                GetNextSymbol();

                thisIFisSingleLineOnly = !(sym.Token == Symbol.Tokens.tokStatementDelimiter & sym.Text == "\n");
                if (sym.Token == Symbol.Tokens.tokStatementDelimiter) { GetNextSymbol(); }


                singleLineOnly = singleLineOnly || thisIFisSingleLineOnly;


                thenPC = code.Add(Code.Opcodes.opJumpFalse);
                // Spring zum ELSE-Teil oder ans Ende

                StatementList(singleLineOnly, false, exitsAllowed, Symbol.Tokens.tokEOF, Symbol.Tokens.tokELSE, Symbol.Tokens.tokEND, Symbol.Tokens.tokENDIF);

                if (sym.Token == Symbol.Tokens.tokELSE)
                {

                    elsePC = code.Add(Code.Opcodes.opJump); // Spring ans Ende
                    code.FixUp(thenPC - 1, elsePC);

                    GetNextSymbol();

                    StatementList(singleLineOnly, false, exitsAllowed, Symbol.Tokens.tokEOF, Symbol.Tokens.tokEND, Symbol.Tokens.tokENDIF);
                }

                if (sym.Token == Symbol.Tokens.tokEND)
                {
                    GetNextSymbol();

                    if (sym.Token == Symbol.Tokens.tokIF)
                        GetNextSymbol();
                    else
                        errorObject.Raise((int)InterpreterError.parsErrors.errSyntaxViolation, "SyntaxAnalyser.IFStatement", "'END IF' or 'ENDIF' expected to close IF-statement", sym.Line, sym.Col, sym.Index, sym.Text);
                }
                else if (sym.Token == Symbol.Tokens.tokENDIF)
                    GetNextSymbol();
                else if (!thisIFisSingleLineOnly)
                    // kein 'END IF' zu finden ist nur ein Fehler, wenn das IF über mehrere Zeilen geht
                    errorObject.Raise((int)InterpreterError.parsErrors.errSyntaxViolation, "SyntaxAnalyser.IFStatement", "'END IF' or 'ENDIF' expected to close IF-statement", sym.Line, sym.Col, sym.Index, sym.Text);
            }
            else
                errorObject.Raise((int)InterpreterError.parsErrors.errSyntaxViolation, "SyntaxAnalyser.IFStatement", "THEN missing after IF", sym.Line, sym.Col, sym.Index, sym.Text);


            if (elsePC == 0)
                code.FixUp(thenPC - 1, code.EndOfCodePC);
            else
                code.FixUp(elsePC - 1, code.EndOfCodePC);
        }


        // Einstieg für die Auswertung math. Ausdrücke (s.a. Expression())
        private void Condition()
        {

            // ConditionalTerm { "OR" ConditionalTerm }
            ConditionalTerm();

            while (InSymbolSet(sym.Token, Symbol.Tokens.tokOR))
            {
                GetNextSymbol();
                ConditionalTerm();


                code.Add(Code.Opcodes.opOr);
            }
        }


        // Bei AND-Verknüpfungen werden nur soviele Operanden tatsächlich berechnet, bis einer FALSE ergibt! In diesem Fall wird das
        // Ergebnis aller AND-Verknüpfungen sofort auch auf FALSE gesetzt und die restlichen Prüfungen/Kalkulationen nicht mehr durchgeführt.
        private void ConditionalTerm()
        {
            var operandPCs = new List<object>();

            ConditionalFactor();

            while (InSymbolSet(sym.Token, Symbol.Tokens.tokAND))
            {

                operandPCs.Add(code.Add(Code.Opcodes.opJumpFalse));

                GetNextSymbol();
                ConditionalFactor();
            }

            int thenPC;
            if (operandPCs.Count() > 0)
            {

                operandPCs.Add(code.Add(Code.Opcodes.opJumpFalse));

                // wenn wir hier ankommen, dann sind alle AND-Operanden TRUE
                code.Add(Code.Opcodes.opPushValue, true); // also dieses Ergebnis auch auf den Stack legen
                thenPC = code.Add(Code.Opcodes.opJump); // und zum Code springen, der mit dem Ergebnis weiterarbeitet

                // wenn wir hier ankommen, dann war mindestens ein
                // AND-Operand FALSE
                // Alle Sprünge von Operandentests hierher umleiten, da
                // sie ja FALSE ergeben haben
                for (int i = 0; i < operandPCs.Count(); i++)
                    code.FixUp(Convert.ToInt32(operandPCs[i]) - 1, code.EndOfCodePC);
                code.Add(Code.Opcodes.opPushValue, false); // also dieses Ergebnis auch auf den Stack legen

                code.FixUp(thenPC-1, code.EndOfCodePC);
            }
        }

        private void ConditionalFactor()
        {

            // Expression { ( "=" | "<>" | "<=" | "<" | ">=" | ">" ) Expression }
            Symbol.Tokens operator_Renamed;

            Expression();

            while (InSymbolSet(sym.Token, Symbol.Tokens.tokEq, Symbol.Tokens.tokNotEq, Symbol.Tokens.tokLEq, Symbol.Tokens.tokLT, Symbol.Tokens.tokGEq, Symbol.Tokens.tokGT))
            {
                operator_Renamed = sym.Token;

                GetNextSymbol();

                Expression();


                switch (operator_Renamed)
                {
                    case Symbol.Tokens.tokEq:
                        {
                            code.Add(Code.Opcodes.opEq);
                            break;
                        }

                    case Symbol.Tokens.tokNotEq:
                        {
                            code.Add(Code.Opcodes.opNotEq);
                            break;
                        }

                    case Symbol.Tokens.tokLEq:
                        {
                            code.Add(Code.Opcodes.opLEq);
                            break;
                        }

                    case Symbol.Tokens.tokLT:
                        {
                            code.Add(Code.Opcodes.oplt);
                            break;
                        }

                    case Symbol.Tokens.tokGEq:
                        {
                            code.Add(Code.Opcodes.opGEq);
                            break;
                        }

                    case Symbol.Tokens.tokGT:
                        {
                            code.Add(Code.Opcodes.opGt);
                            break;
                        }
                }
            }
        }

        private void Expression()
        {

            // Term { ("+" | "-" | "%" | "MOD" | "&") Term }
            Symbol.Tokens operator_Renamed;

            Term();

            while (InSymbolSet(sym.Token, Symbol.Tokens.tokPlus, Symbol.Tokens.tokMinus, Symbol.Tokens.tokMod, Symbol.Tokens.tokStringConcat))
            {
                operator_Renamed = sym.Token;

                GetNextSymbol();
                Term();


                switch (operator_Renamed)
                {
                    case Symbol.Tokens.tokPlus:
                        {
                            code.Add(Code.Opcodes.opAdd);
                            break;
                        }

                    case Symbol.Tokens.tokMinus:
                        {
                            code.Add(Code.Opcodes.opSub);
                            break;
                        }

                    case Symbol.Tokens.tokMod:
                        {
                            code.Add(Code.Opcodes.opMod);
                            break;
                        }

                    case Symbol.Tokens.tokStringConcat:
                        {
                            code.Add(Code.Opcodes.opStringConcat);
                            break;
                        }
                }
            }
        }

        private void Term()
        {

            // Factor { ("*" | "/" | "\" | "DIV") Factor }
            Symbol.Tokens operator_Renamed;

            Factor();

            while (InSymbolSet(sym.Token, Symbol.Tokens.tokMultiplication, Symbol.Tokens.tokDivision, Symbol.Tokens.tokDiv))
            {
                operator_Renamed = sym.Token;

                GetNextSymbol();
                Factor();


                switch (operator_Renamed)
                {
                    case Symbol.Tokens.tokMultiplication:
                        {
                            code.Add(Code.Opcodes.opMultiplication);
                            break;
                        }

                    case Symbol.Tokens.tokDivision:
                        {
                            code.Add(Code.Opcodes.opDivision);
                            break;
                        }

                    case Symbol.Tokens.tokDiv:
                        {
                            code.Add(Code.Opcodes.opDiv);
                            break;
                        }
                }
            }
        }

        private void Factor()
        {

            // Factorial [ "^" Factorial ]
            Factorial();

            if (sym.Token == Symbol.Tokens.tokPower)
            {
                GetNextSymbol();

                Factorial();


                code.Add(Code.Opcodes.opPower);
            }
        }

        // Fakultät
        private void Factorial()
        {

            // Terminal [ "!" ]
            Terminal();

            if (sym.Token == Symbol.Tokens.tokFactorial)
            {

                code.Add(Code.Opcodes.opFactorial);

                GetNextSymbol();
            }
        }


        private void Terminal()
        {
            Symbol.Tokens operator_Renamed;
            int thenPC, elsePC;
            string ident;
            switch (sym.Token)
            {
                case Symbol.Tokens.tokMinus // "-" Terminal
               :
                    {
                        GetNextSymbol();

                        Terminal();


                        code.Add(Code.Opcodes.opNegate);
                        break;
                    }

                case Symbol.Tokens.tokNOT // "NOT" Terminal
         :
                    {
                        GetNextSymbol();

                        Terminal();


                        code.Add(Code.Opcodes.opNot);
                        break;
                    }

                case Symbol.Tokens.tokNumber:
                    {

                        code.Add(Code.Opcodes.opPushValue, sym.Value);

                        GetNextSymbol();
                        break;
                    }
                case Symbol.Tokens.tokString:
                    {

                        code.Add(Code.Opcodes.opPushValue, sym.Value);

                        GetNextSymbol();
                        break;
                    }

                case Symbol.Tokens.tokIdentifier // Identifier [ "(" Condition { "," Condition } ]
         :
                    {
                        // folgt hinter dem Identifier eine "(" so sind danach Funktionsparameter zu erwarten

                        // Wurde Identifier überhaupt schon deklariert?
                        if (optionExplicit & !symboltable.Exists(sym.Text))
                            errorObject.Raise((int)InterpreterError.parsErrors.errIdentifierAlreadyExists, "SyntaxAnalyser.Terminal", "Identifier '" + sym.Text + "' has not be declared", sym.Line, sym.Col, sym.Index, sym.Text);

                        if (symboltable.Exists(sym.Text, null, Identifier.IdentifierTypes.idFunction))
                        {
                            // Userdefinierte Funktion aufrufen
                            ident = sym.Text;

                            GetNextSymbol();

                            CallUserdefinedFunction(ident);

                            code.Add(Code.Opcodes.opPushVariable, ident); // Funktionsresultat auf den Stack
                        }
                        else if (symboltable.Exists(sym.Text, null, Identifier.IdentifierTypes.idSub))
                            errorObject.Raise((int)InterpreterError.parsErrors.errCannotCallSubInExpression, "SyntaxAnalyser.Terminal", "Cannot call sub '" + sym.Text + "' in expression", sym.Line, sym.Col, sym.Index);
                        else
                        {
                            // Wert einer Variablen bzw. Konstante auf den Stack legen

                            code.Add(Code.Opcodes.opPushVariable, sym.Text);
                            GetNextSymbol();
                        }

                        break;
                    }

                case Symbol.Tokens.tokTrue:
                    {

                        code.Add(Code.Opcodes.opPushValue, true);
                        GetNextSymbol();
                        break;
                    }

                case Symbol.Tokens.tokFalse:
                    {

                        code.Add(Code.Opcodes.opPushValue, false);
                        GetNextSymbol();
                        break;
                    }

                case Symbol.Tokens.tokPI:
                    {

                        code.Add(Code.Opcodes.opPushValue, 3.141592654);
                        GetNextSymbol();
                        break;
                    }

                case Symbol.Tokens.tokCrlf:
                    {

                        code.Add(Code.Opcodes.opPushValue, "\r\n");
                        GetNextSymbol();
                        break;
                    }

                case Symbol.Tokens.tokTab:
                    {

                        code.Add(Code.Opcodes.opPushValue, "\t");
                        GetNextSymbol();
                        break;
                    }

                case Symbol.Tokens.tokCr:
                    {

                        code.Add(Code.Opcodes.opPushValue, "\r");
                        GetNextSymbol();
                        break;
                    }

                case Symbol.Tokens.tokLf:
                    {

                        code.Add(Code.Opcodes.opPushValue, "\n");
                        GetNextSymbol();
                        break;
                    }

                case Symbol.Tokens.tokMsgbox:
                    operator_Renamed = Boxes(sym.Token);
                    break;
                case Symbol.Tokens.tokInputbox:
                    CallInputbox(false);
                    GetNextSymbol();
                    //operator_Renamed = Boxes(sym.Token);
                    break;
                case Symbol.Tokens.tokMessage:
                    operator_Renamed = Boxes(sym.Token);
                    break;

                case Symbol.Tokens.tokSin:
                    operator_Renamed = ComplexGeometry(sym.Token);
                    break;
                case Symbol.Tokens.tokCos:
                    operator_Renamed = ComplexGeometry(sym.Token);
                    break;
                case Symbol.Tokens.tokTan:
                    operator_Renamed = ComplexGeometry(sym.Token);
                    break;
                case Symbol.Tokens.tokATan:
                    operator_Renamed = ComplexGeometry(sym.Token);
                    break;
                case Symbol.Tokens.tokIIF // "IIF" "(" Condition "," Condition "," Condition ")"
         :
                    {
                        GetNextSymbol();
                        if (sym.Token == Symbol.Tokens.tokLeftParent)
                        {
                            GetNextSymbol();

                            Condition();


                            thenPC = code.Add(Code.Opcodes.opJumpFalse);

                            if (sym.Token == Symbol.Tokens.tokComma)
                            {
                                GetNextSymbol();

                                Condition(); // true Value


                                elsePC = code.Add(Code.Opcodes.opJump);
                                code.FixUp(thenPC - 1, code.EndOfCodePC);

                                if (sym.Token == Symbol.Tokens.tokComma)
                                {
                                    GetNextSymbol();

                                    Condition(); // false Value


                                    code.FixUp(elsePC - 1, code.EndOfCodePC);

                                    if (sym.Token == Symbol.Tokens.tokRightParent)
                                        GetNextSymbol();
                                    else
                                        errorObject.Raise((int)InterpreterError.parsErrors.errMissingClosingParent, "SyntaxAnalyser.Terminal", "Missing closing bracket ')' after last IIF-parameter", sym.Line, sym.Col, sym.Index, sym.Text);
                                }
                                else
                                    errorObject.Raise((int)InterpreterError.parsErrors.errMissingComma, "SyntaxAnalyser.Terminal", "Missing ',' after true-Value of IIF", sym.Line, sym.Col, sym.Index, sym.Text);
                            }
                            else
                                errorObject.Raise((int)InterpreterError.parsErrors.errMissingComma, "SyntaxAnalyser.Terminal", "Missing ',' after IIF-condition", sym.Line, sym.Col, sym.Index, sym.Text);
                        }
                        else
                            errorObject.Raise((int)InterpreterError.parsErrors.errMissingLeftParent, "SyntaxAnalyser.Terminal", "Missing opening bracket '(' after IIF", sym.Line, sym.Col, sym.Index, sym.Text);
                        break;
                    }

                case Symbol.Tokens.tokLeftParent // "(" Condition ")"
         :
                    {
                        GetNextSymbol();

                        Condition();

                        if (sym.Token == Symbol.Tokens.tokRightParent)
                            GetNextSymbol();
                        else
                            errorObject.Raise((int)InterpreterError.parsErrors.errMissingClosingParent, "SyntaxAnalyser.Terminal", "Missing closing bracket ')'", sym.Line, sym.Col, sym.Index, sym.Text);
                        break;
                    }

                case Symbol.Tokens.tokEOF:
                    {
                        errorObject.Raise((int)InterpreterError.parsErrors.errUnexpectedSymbol, "SyntaxAnalyser.Terminal", "Identifier or function or '(' expected but end of source found", sym.Line, sym.Col, sym.Index, sym.Text);
                        break;
                    }

                default:
                    {
                        errorObject.Raise((int)InterpreterError.parsErrors.errUnexpectedSymbol, "SyntaxAnalyser.Terminal", "Expected: expression; found symbol '" + sym.Text + "'", sym.Line, sym.Col, sym.Index, sym.Text);
                        break;
                    }
            }
        }


        private void CallMsg(bool dropReturnValue)
        {
            ActualOptionalParameter(0);
            ActualOptionalParameter("");

            code.Add(Code.Opcodes.opMessage);

            if (dropReturnValue)
                code.Add(Code.Opcodes.opPop);
        }


        private void CallMsgBox(bool dropReturnValue)
        {
            ActualOptionalParameter("");
            ActualOptionalParameter(0);
            ActualOptionalParameter("");


            code.Add(Code.Opcodes.opMsgbox);

            if (dropReturnValue)
                code.Add(Code.Opcodes.opPop);
        }


        ///         ''' Inputbox aufrufen
        private void CallInputbox(bool dropReturnValue)
        {
            ActualOptionalParameter(""); // Prompt
            ActualOptionalParameter(""); // Title
            ActualOptionalParameter(""); // Default
            //ActualOptionalParameter(System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width / (double)5);  // xPos
            //ActualOptionalParameter(System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height / (double)5);  // yPos

            code.Add(Code.Opcodes.opInputbox);

            if (dropReturnValue)
                code.Add(Code.Opcodes.opPop);
        }


        private void ActualOptionalParameter(object default_Renamed)
        {
            if (sym.Token == Symbol.Tokens.tokComma | sym.Token == Symbol.Tokens.tokStatementDelimiter | sym.Token == Symbol.Tokens.tokEOF | sym.Token == Symbol.Tokens.tokRightParent)
                // statt eines Parameters sind wir nur auf ein "," oder das Statement-Ende gestoßen
                // wir nehmen daher den default-Wert an
                code.Add(Code.Opcodes.opPushValue, default_Renamed);
            else
                // Parameterwert bestimmen
                Condition();

            if (sym.Token == Symbol.Tokens.tokComma)
                GetNextSymbol();
        }


        //Benutzerdefinierte Funktion aufrufen: feststellen, ob für alle formalen Parameter ein aktueller Param. angegeben ist.
        private void CallUserdefinedFunction(string ident)
        {
            bool requireRightParent = false;

            // Identifier überhaupt als Funktion definiert?
            if (!symboltable.Exists(ident, null, Identifier.IdentifierTypes.idSubOfFunction))
                errorObject.Raise((int)InterpreterError.parsErrors.errIdentifierAlreadyExists, "Statement", "Function/Sub '" + ident + "' not declared", sym.Line, sym.Col, sym.Index, ident);

            if (sym.Token == Symbol.Tokens.tokLeftParent)
            {
                requireRightParent = true;
                GetNextSymbol();
            }

            // Funktionsdefinition laden (Parameterzahl, Adresse)
            Identifier definition;
            definition = symboltable.Retrieve(ident);

            // --- Function-Scope vorbereiten

            // Neuen Scope für die Funktion öffnen
            code.Add(Code.Opcodes.opPushScope);

            // --- Parameter verarbeiten
            int n = 0;
            int i = 0;

            if (sym.Token == Symbol.Tokens.tokStatementDelimiter | sym.Token == Symbol.Tokens.tokEOF)
                // Aufruf ohne Parameter
                n = 0;
            else
                // Funktion mit Parametern: Parameter { "," Parameter }
                do
                {
                    if (n > definition.FormalParameters.Count - 1) { break; }
                    if (n > 0) { GetNextSymbol(); }

                    Condition();

                    n++;
                }
                // wir standen noch auf dem "," nach dem vorhergehenden Parameter // Wert des n-ten Parameters auf den Stack legen
                while (sym.Token == Symbol.Tokens.tokComma);

            // Wurde die richtige Anzahl Parameter übergeben?
            if (definition.FormalParameters.Count != n)
                errorObject.Raise((int)InterpreterError.parsErrors.errWrongNumberOfParams, "SyntaxAnalyser.Statement", "Wrong number of parameters in call to function '" + ident + "' (" + definition.FormalParameters.Count + " expected but " + n + " found)", sym.Line, sym.Col, sym.Index, sym.Text);


            // Formale Parameter als lokale Variablen der Funktion definieren und zuweisen
            // (in umgekehrter Reihenfolge, weil die Werte so auf dem Stack liegen)
            for (i = definition.FormalParameters.Count - 1; i >= 0; i += -1)
            {
                code.Add(Code.Opcodes.opAllocVar, definition.FormalParameters[i]);
                code.Add(Code.Opcodes.opAssign, definition.FormalParameters[i]);
            }

            if (requireRightParent)
            {
                if (sym.Token == Symbol.Tokens.tokRightParent)
                    GetNextSymbol();
                else
                    errorObject.Raise((int)InterpreterError.parsErrors.errUnexpectedSymbol, "SyntaxAnalyser.Statement", "Expected: ')' after function parameters", sym.Line, sym.Col, sym.Index, sym.Text);
            }


            // --- Funktion rufen
            code.Add(Code.Opcodes.opCall, definition.Address);

            // --- Scopes aufräumen
            code.Add(Code.Opcodes.opPopScope);
        }


        private void StatementComparativeOperators(string Ident)
        {
            var op = sym.Token;

            if (symboltable.Exists(Ident, null, Identifier.IdentifierTypes.idConst))
            {
                errorObject.Raise((int)(int)InterpreterError.parsErrors.errIdentifierAlreadyExists,
                  "Statement",
                  "Assignment to constant " + Ident + " not allowed",
                  sym.Line, sym.Col, sym.Index, Ident);
            }

            if (this.optionExplicit && !symboltable.Exists(Ident, null, Identifier.IdentifierTypes.idIsVariableOfFunction))
            {
                errorObject.Raise((int)InterpreterError.parsErrors.errIdentifierAlreadyExists,
                  "Statement",
                  "Variable/Function " + Ident + " not declared",
                   sym.Line, sym.Col, sym.Index, Ident);
            }

            if (op != Symbol.Tokens.tokEq)
            {
                this.code.Add(Code.Opcodes.opPushVariable, Ident);
            }

            GetNextSymbol();
            Condition();

            switch (op)
            {
                case Symbol.Tokens.tokPlusEq:
                    code.Add(Code.Opcodes.opAdd);
                    break;
                case Symbol.Tokens.tokMinusEq:
                    code.Add(Code.Opcodes.opSub);
                    break;
                case Symbol.Tokens.tokMultiplicationEq:
                    code.Add(Code.Opcodes.opMultiplication);
                    break;
                case Symbol.Tokens.tokDivisionEq:
                    code.Add(Code.Opcodes.opDivision);
                    break;
                case Symbol.Tokens.tokStringConcatEq:
                    code.Add(Code.Opcodes.opStringConcat);
                    break;
                case Symbol.Tokens.tokDivEq:
                    code.Add(Code.Opcodes.opDiv);
                    break;
                case Symbol.Tokens.tokModEq:
                    code.Add(Code.Opcodes.opMod);
                    break;
            }

            code.Add(Code.Opcodes.opAssign, Ident);

        }

        private Symbol.Tokens Boxes(Symbol.Tokens operator_Renamed)
        {

            GetNextSymbol();

            if (sym.Token == Symbol.Tokens.tokLeftParent)
            {
                GetNextSymbol();

                switch (operator_Renamed)
                {
                    case Symbol.Tokens.tokMsgbox:
                        {
                            CallMsgBox(false);
                            break;
                        }

                    case Symbol.Tokens.tokInputbox:
                        {
                            CallInputbox(false);
                            break;
                        }

                    case Symbol.Tokens.tokMessage:
                        {
                            CallMsg(false);
                            break;
                        }
                }

                if (sym.Token == Symbol.Tokens.tokRightParent)
                    GetNextSymbol();
                else
                    errorObject.Raise((int)InterpreterError.parsErrors.errMissingClosingParent, "SyntaxAnalyser.Terminal", "Missing closing bracket ')' after function parameters", sym.Line, sym.Col, sym.Index, sym.Text);
            }
            else
                errorObject.Raise((int)InterpreterError.parsErrors.errMissingLeftParent, "SyntaxAnalyser.Terminal", "Missing opening bracket '(' in function call", sym.Line, sym.Col, sym.Index, sym.Text);

            return operator_Renamed;
        }

        // ( "SIN" | "COS" | "TAN" | "ATAN" ) "(" Condition ")"
        private Symbol.Tokens ComplexGeometry(Symbol.Tokens operator_Renamed)
        {


            GetNextSymbol();
            if (sym.Token == Symbol.Tokens.tokLeftParent)
            {
                GetNextSymbol();

                Condition();

                if (sym.Token == Symbol.Tokens.tokRightParent)
                {
                    GetNextSymbol();


                    switch (operator_Renamed)
                    {
                        case Symbol.Tokens.tokSin:
                            {
                                code.Add(Code.Opcodes.opSin);
                                break;
                            }

                        case Symbol.Tokens.tokCos:
                            {
                                code.Add(Code.Opcodes.opCos);
                                break;
                            }

                        case Symbol.Tokens.tokTan:
                            {
                                code.Add(Code.Opcodes.opTan);
                                break;
                            }

                        case Symbol.Tokens.tokATan:
                            {
                                code.Add(Code.Opcodes.opATan);
                                break;
                            }
                    }
                }
                else
                    errorObject.Raise((int)InterpreterError.parsErrors.errMissingClosingParent, "SyntaxAnalyser.Terminal", "Missing closing bracket ')' after function parameter", sym.Line, sym.Col, sym.Index, sym.Text);
            }
            else
                errorObject.Raise((int)InterpreterError.parsErrors.errMissingLeftParent, "SyntaxAnalyser.Terminal", "Missing opening bracket '(' after function Name", sym.Line, sym.Col, sym.Index, sym.Text);

            return operator_Renamed;
        }



        public void Dispose()
        {
            errorObject = null;
        }

        ~SyntaxAnalyser()
        {
            Dispose();
        }
    }
}
