using System;
using System.Diagnostics;
using System.Security.Principal;
using static Basic_Script_Interpreter.InterpreterError;

namespace Basic_Script_Interpreter
{


    public class LexicalAnalyser
    {
        private const string COMMENT_CHAR = "'"; // Anfangszeichen für Kommentare

        private Code.IInputStream source; // Quelltext-Datenstrom
        private System.Collections.Generic.Dictionary<string, int> predefinedIdentifiers;
        private InterpreterError errorObject;


        

        public LexicalAnalyser Connect(Code.IInputStream source, InterpreterError errorObject)
        {
            this.source = source;
            this.errorObject = errorObject;

            return this;
        }

        public InterpreterError ErrorObject
        {
            set
            {
                try
                {
                    if (source != null)
                    {
                        errorObject = value;
                        source.ErrorObject = errorObject;
                    }
                }
                catch (Exception ex)
                {
                    Debug.Print("LexicalAnalyser.ErrorObject " + ex.Message);
                }
            }
        }


        public Symbol GetNextSymbol()
        {
            var nextSymbol = new Symbol();

            string c = string.Empty;
            string symbolText = string.Empty;
            bool returnNumberSymbol = false;

            // führende Leerzeichen und Tab und Kommentare
            // vor nächstem Symbol überspringen
            if (!source.EOF)
            {
                do
                {
                    c = source.GetNextChar();
                    if (c == COMMENT_CHAR)
                    {
                        source.SkipComment();
                        c = " ";
                    }
                }
                while (c == " " & !source.EOF);
            }

            // Zeile/Spalte des aktuellen Symbols vermerken
            nextSymbol.Position(source.Line, source.Col, source.Index);

            
            if (!source.EOF)
            {
                // Zeichenweise das nächste Symbol zusammensetzen
                switch (c.ToUpper())
                {
                    case "+":
                        mathOperatorOrAssignments(nextSymbol, c);
                        break;
                    case "-":
                        mathOperatorOrAssignments(nextSymbol, c);
                        break;
                    case "*":
                        mathOperatorOrAssignments(nextSymbol, c);
                        break;
                    case "/":
                        mathOperatorOrAssignments(nextSymbol, c);
                        break;
                    case "&":
                        mathOperatorOrAssignments(nextSymbol, c);
                        break;
                    case "\\":
                        mathOperatorOrAssignments(nextSymbol, c);
                        break;
                    case "%":
                        mathOperatorOrAssignments(nextSymbol, c);
                        break;

                    case "^":
                        {
                            nextSymbol.Init((Symbol.Tokens)Symbol.Tokens.tokPower, c);
                            break;
                        }

                    case "!":
                        {
                            nextSymbol.Init((Symbol.Tokens)Symbol.Tokens.tokFactorial, c);
                            break;
                        }

                    case "~":
                        {
                            nextSymbol.Init((Symbol.Tokens)Symbol.Tokens.tokNOT, c);
                            break;
                        }

                    case "(":
                        {
                            nextSymbol.Init((Symbol.Tokens)Symbol.Tokens.tokLeftParent, c);
                            break;
                        }

                    case ")":
                        {
                            nextSymbol.Init((Symbol.Tokens)Symbol.Tokens.tokRightParent, c);
                            break;
                        }

                    case ",":
                        {
                            nextSymbol.Init((Symbol.Tokens)Symbol.Tokens.tokComma, c);
                            break;
                        }

                    case "=":
                        {
                            nextSymbol.Init((Symbol.Tokens)Symbol.Tokens.tokEq, c);
                            break;
                        }

                    case "<" // "<", "<=", "<>"
             :
                        {
                            c = source.GetNextChar();
                            switch (c)
                            {
                                case ">":
                                    {
                                        nextSymbol.Init((Symbol.Tokens)Symbol.Tokens.tokNotEq, "<>");
                                        break;
                                    }

                                case "=":
                                    {
                                        nextSymbol.Init((Symbol.Tokens)Symbol.Tokens.tokLEq, "<=");
                                        break;
                                    }

                                default:
                                    {
                                        source.GoBack();

                                        nextSymbol.Init((Symbol.Tokens)Symbol.Tokens.tokLT, "<");
                                        break;
                                    }
                            }

                            break;
                        }

                    case ">":
                        {
                            c = source.GetNextChar();
                            switch (c)
                            {
                                case "=":
                                    {
                                        nextSymbol.Init((Symbol.Tokens)Symbol.Tokens.tokGEq, ">=");
                                        break;
                                    }
                                default:
                                    {
                                        source.GoBack();

                                        nextSymbol.Init((Symbol.Tokens)Symbol.Tokens.tokGT, ">");
                                        break;
                                    }
                            }

                            break;
                        }

                    case "0":
                        var res = Numbers(nextSymbol, c);
                        returnNumberSymbol = res.Item1;
                        symbolText = res.Item2;
                        break;
                    case "1":
                        res = Numbers(nextSymbol, c);
                        returnNumberSymbol = res.Item1;
                        symbolText = res.Item2;
                        break;
                    case "2":
                        res = Numbers(nextSymbol, c);
                        returnNumberSymbol = res.Item1;
                        symbolText = res.Item2;
                        break;
                    case "3":
                        res = Numbers(nextSymbol, c);
                        returnNumberSymbol = res.Item1;
                        symbolText = res.Item2;
                        break;
                    case "4":
                        res = Numbers(nextSymbol, c);
                        returnNumberSymbol = res.Item1;
                        symbolText = res.Item2;
                        break;
                    case "5":
                        res = Numbers(nextSymbol, c);
                        returnNumberSymbol = res.Item1;
                        symbolText = res.Item2;
                        break;
                    case "6":
                        res = Numbers(nextSymbol, c);
                        returnNumberSymbol = res.Item1;
                        symbolText = res.Item2;
                        break;
                    case "7":
                        res = Numbers(nextSymbol, c);
                        returnNumberSymbol = res.Item1;
                        symbolText = res.Item2;
                        break;
                    case "8":
                        res = Numbers(nextSymbol, c);
                        returnNumberSymbol = res.Item1;
                        symbolText = res.Item2;
                        break;
                    case "9":
                        res = Numbers(nextSymbol, c);
                        returnNumberSymbol = res.Item1;
                        symbolText = res.Item2;
                        break;
                    case "@":
                        symbolText = this.Identifier(nextSymbol, c);
                        break;
                    case "A":
                        symbolText = this.Identifier(nextSymbol, c);
                        break;
                    case "B":
                        symbolText = this.Identifier(nextSymbol, c);
                        break;
                    case "C":
                        symbolText = this.Identifier(nextSymbol, c);
                        break;
                    case "D":
                        symbolText = this.Identifier(nextSymbol, c);
                        break;
                    case "E":
                        symbolText = this.Identifier(nextSymbol, c);
                        break;
                    case "F":
                        symbolText = this.Identifier(nextSymbol, c);
                        break;
                    case "G":
                        symbolText = this.Identifier(nextSymbol, c);
                        break;
                    case "H":
                        symbolText = this.Identifier(nextSymbol, c);
                        break;
                    case "I":
                        symbolText = this.Identifier(nextSymbol, c);
                        break;
                    case "J":
                        symbolText = this.Identifier(nextSymbol, c);
                        break;
                    case "K":
                        symbolText = this.Identifier(nextSymbol, c);
                        break;
                    case "L":
                        symbolText = this.Identifier(nextSymbol, c);
                        break;
                    case "M":
                        symbolText = this.Identifier(nextSymbol, c);
                        break;
                    case "N":
                        symbolText = this.Identifier(nextSymbol, c);
                        break;
                    case "O":
                        symbolText = this.Identifier(nextSymbol, c);
                        break;
                    case "P":
                        symbolText = this.Identifier(nextSymbol, c);
                        break;
                    case "Q":
                        symbolText = this.Identifier(nextSymbol, c);
                        break;
                    case "R":
                        symbolText = this.Identifier(nextSymbol, c);
                        break;
                    case "S":
                        symbolText = this.Identifier(nextSymbol, c);
                        break;
                    case "T":
                        symbolText = this.Identifier(nextSymbol, c);
                        break;
                    case "U":
                        symbolText = this.Identifier(nextSymbol, c);
                        break;
                    case "V":
                        symbolText = this.Identifier(nextSymbol, c);
                        break;
                    case "W":
                        symbolText = this.Identifier(nextSymbol, c);
                        break;
                    case "X":
                        symbolText = this.Identifier(nextSymbol, c);
                        break;
                    case "Y":
                        symbolText = this.Identifier(nextSymbol, c);
                        break;
                    case "Z":
                        symbolText = this.Identifier(nextSymbol, c);
                        break;
                    case "Ä":
                        symbolText = this.Identifier(nextSymbol, c);
                        break;
                    case "Ö":
                        symbolText = this.Identifier(nextSymbol, c);
                        break;
                    case "Ü":
                        symbolText = this.Identifier(nextSymbol, c);
                        break;
                    case "ß":
                        symbolText = this.Identifier(nextSymbol, c);
                        break;
                    case "_":
                        symbolText = this.Identifier(nextSymbol, c);
                        break;
                    case "\"":
                        
                       
                        symbolText = "";
                        var endOfEmptyChar = false;
                        do
                        {
                            c = source.GetNextChar();
                            switch (c)
                            {
                                case "\"":
                                    c = source.GetNextChar();
                                    if (c == "\"")
                                    {
                                        symbolText += "\"";
                                    }
                                    else
                                    {
                                        endOfEmptyChar = true;
                                        source.GoBack();
                                        nextSymbol.Init(Symbol.Tokens.tokString, symbolText, symbolText);
                                    }
                                    break;
                                case "\n": // keinen Zeilenwechsel im String zulassen

                                    errorObject.Raise((int)lexErrors.errUnexpectedEOL,
                                      "LexAnalyser.nextSymbol", "String not closed; unexpected end of line encountered",
                                      source.Line, source.Col, source.Index);
                                    endOfEmptyChar = true;
                                    break;


                                case "":
                                    errorObject.Raise((int)lexErrors.errUnexpectedEOF,
                                      "LexAnalyser.nextSymbol", "String not closed; unexpected end of source",
                                       source.Line, source.Col, source.Index);
                                    endOfEmptyChar = true;
                                    break;
                                default:
                                    symbolText = (symbolText + c);
                                   
                                    break;
                            }
                        }
                        while (!endOfEmptyChar);
                        break;

                    case ":":
                        {
                            nextSymbol.Init(Symbol.Tokens.tokStatementDelimiter, c);
                            break;
                        }
                    case "\n":
                        {
                            nextSymbol.Init(Symbol.Tokens.tokStatementDelimiter, c);
                            break;
                        }

                    default:
                        {
                            errorObject.Raise(Convert.ToInt32(InterpreterError.lexErrors.errUnknownSymbol), "LexicalAnalyser.GetNextSymbol", "Unknown symbol starting with character ASCII " + c, nextSymbol.Line, nextSymbol.Col, nextSymbol.Index);
                            break;
                        }
                }
            }
            else
            {
                nextSymbol.Init((Symbol.Tokens)Symbol.Tokens.tokEOF);
            }

            if (returnNumberSymbol)
            {
                nextSymbol.Init(Symbol.Tokens.tokNumber, symbolText, symbolText);
            }

            return nextSymbol;

        }

        public LexicalAnalyser()
        {

            // Initialisieren der Tabelle mit den vordefinierten
            // Namenssymbolen.
            predefinedIdentifiers = new System.Collections.Generic.Dictionary<string, int>();

            predefinedIdentifiers.Add("DIV", (int)Symbol.Tokens.tokDiv);
            predefinedIdentifiers.Add("MOD", (int)Symbol.Tokens.tokMod);
            predefinedIdentifiers.Add("AND", (int)Symbol.Tokens.tokAND);
            predefinedIdentifiers.Add("OR", (int)Symbol.Tokens.tokOR);
            predefinedIdentifiers.Add("NOT", (int)Symbol.Tokens.tokNOT);
            predefinedIdentifiers.Add("SIN", (int)Symbol.Tokens.tokSin);
            predefinedIdentifiers.Add("COS", (int)Symbol.Tokens.tokCos);
            predefinedIdentifiers.Add("TAN", (int)Symbol.Tokens.tokTan);
            predefinedIdentifiers.Add("ATAN", (int)Symbol.Tokens.tokATan);
            predefinedIdentifiers.Add("IIF", (int)Symbol.Tokens.tokIIF);
            predefinedIdentifiers.Add("IF", (int)Symbol.Tokens.tokIF);
            predefinedIdentifiers.Add("THEN", (int)Symbol.Tokens.tokTHEN);
            predefinedIdentifiers.Add("ELSE", (int)Symbol.Tokens.tokELSE);
            predefinedIdentifiers.Add("END", (int)Symbol.Tokens.tokEND);
            predefinedIdentifiers.Add("ENDIF", (int)Symbol.Tokens.tokENDIF);
            predefinedIdentifiers.Add("DO", (int)Symbol.Tokens.tokDO);
            predefinedIdentifiers.Add("WHILE", (int)Symbol.Tokens.tokWHILE);
            predefinedIdentifiers.Add("LOOP", (int)Symbol.Tokens.tokLOOP);
            predefinedIdentifiers.Add("UNTIL", (int)Symbol.Tokens.tokUNTIL);
            predefinedIdentifiers.Add("FOR", (int)Symbol.Tokens.tokFOR);
            predefinedIdentifiers.Add("TO", (int)Symbol.Tokens.tokTO);
            predefinedIdentifiers.Add("STEP", (int)Symbol.Tokens.tokSTEP);
            predefinedIdentifiers.Add("NEXT", (int)Symbol.Tokens.tokNEXT);
            predefinedIdentifiers.Add("CONST", (int)Symbol.Tokens.tokCONST);
            predefinedIdentifiers.Add("DIM", (int)Symbol.Tokens.tokDIM);
            predefinedIdentifiers.Add("FUNCTION", (int)Symbol.Tokens.tokFUNCTION);
            predefinedIdentifiers.Add("ENDFUNCTION", (int)Symbol.Tokens.tokENDFUNCTION);
            predefinedIdentifiers.Add("SUB", (int)Symbol.Tokens.tokSUB);
            predefinedIdentifiers.Add("ENDSUB", (int)Symbol.Tokens.tokENDSUB);
            predefinedIdentifiers.Add("EXIT", (int)Symbol.Tokens.tokEXIT);
            predefinedIdentifiers.Add("DEBUGPRINT", (int)Symbol.Tokens.tokDebugPrint);
            predefinedIdentifiers.Add("DEBUGCLEAR", (int)Symbol.Tokens.tokDebugClear);
            predefinedIdentifiers.Add("DEBUGSHOW", (int)Symbol.Tokens.tokDebugShow);
            predefinedIdentifiers.Add("DEBUGHIDE", (int)Symbol.Tokens.tokDebugHide);
            predefinedIdentifiers.Add("MSGBOX", (int)Symbol.Tokens.tokMsgbox);
            predefinedIdentifiers.Add("MESSAGE", (int)Symbol.Tokens.tokMessage);
            predefinedIdentifiers.Add("DOEVENTS", (int)Symbol.Tokens.tokDoEvents);
            predefinedIdentifiers.Add("INPUTBOX", (int)Symbol.Tokens.tokInputbox);
            predefinedIdentifiers.Add("TRUE", (int)Symbol.Tokens.tokTrue);
            predefinedIdentifiers.Add("FALSE", (int)Symbol.Tokens.tokFalse);
            predefinedIdentifiers.Add("PI", (int)Symbol.Tokens.tokPI);
            predefinedIdentifiers.Add("VBCRLF", (int)Symbol.Tokens.tokCrlf);
            predefinedIdentifiers.Add("VBTAB", (int)Symbol.Tokens.tokTab);
            predefinedIdentifiers.Add("VBCR", (int)Symbol.Tokens.tokCr);
            predefinedIdentifiers.Add("VBLF", (int)Symbol.Tokens.tokLf);
            predefinedIdentifiers.Add("IMPORT", (int)Symbol.Tokens.tokEXTERNAL);


        }

        private void mathOperatorOrAssignments(Symbol nextSymbol, string c)
        {
            var symbolText = c;
            c = source.GetNextChar();

            if ((c == "="))
            {
                symbolText = (symbolText + c);
            }

            switch (symbolText.Substring(0, 1))
            {
                case "+":
                    nextSymbol.Init(c == "=" ? Symbol.Tokens.tokPlusEq : Symbol.Tokens.tokPlus, symbolText);
                    break;
                case "-":
                    nextSymbol.Init(c == "=" ? Symbol.Tokens.tokMinusEq : Symbol.Tokens.tokMinus, symbolText);
                    break;
                case "*":
                    nextSymbol.Init(c == "=" ? Symbol.Tokens.tokMultiplicationEq : Symbol.Tokens.tokMultiplication, symbolText);
                    break;
                case "/":
                    nextSymbol.Init(c == "=" ? Symbol.Tokens.tokDivisionEq : Symbol.Tokens.tokDivision, symbolText);
                    break;
                case "&":
                    nextSymbol.Init(c == "=" ? Symbol.Tokens.tokStringConcatEq : Symbol.Tokens.tokStringConcat, symbolText);
                    break;
                case "\\":
                    nextSymbol.Init(c == "=" ? Symbol.Tokens.tokDivEq : Symbol.Tokens.tokDiv, symbolText);
                    break;
                case "%":
                    nextSymbol.Init(c == "=" ? Symbol.Tokens.tokModEq : Symbol.Tokens.tokMod, symbolText);
                    break;
            }

            if (c != "=") { source.GoBack(); }
        }

        private Tuple<bool, string> Numbers(Symbol nextSymbol, string c)
        {
            var symbolText = c;
            var returnNumberSymbol = false;

            do
            {
                c = source.GetNextChar();
                if (Helper.IsNumericInt(c) && ((int.Parse(c) >= 0) && (int.Parse(c) <= 9)))
                {
                    symbolText = (symbolText + c);
                }
                else if ((c == "."))
                {
                    symbolText = (symbolText + ".");
                    for (
                      ; true;
                    )
                    {
                        c = source.GetNextChar();
                        if (Helper.IsNumericInt(c) && ((int.Parse(c) >= 0) && (int.Parse(c) <= 9)))
                        {
                            symbolText = (symbolText + c);
                        }
                        else
                        {
                            source.GoBack();
                            returnNumberSymbol = true;
                            break;
                        }

                    }

                    break;
                }
                else
                {
                    source.GoBack();
                    returnNumberSymbol = true;
                    break;
                }
            } while (source.EOF == false);

            return new Tuple<bool, string>(returnNumberSymbol, symbolText);
        }

        private string Identifier(Symbol nextSymbol, string c)
        {

            string symbolText = c;
            bool breakLoop = false;

            c = source.GetNextChar();
            do
            {
                switch (c.ToUpper())
                {
                    case "@":
                        symbolText += c;
                        break;
                    case "A":
                        symbolText += c;
                        break;
                    case "B":
                        symbolText += c;
                        break;
                    case "C":
                        symbolText += c;
                        break;
                    case "D":
                        symbolText += c;
                        break;
                    case "E":
                        symbolText += c;
                        break;
                    case "F":
                        symbolText += c;
                        break;
                    case "G":
                        symbolText += c;
                        break;
                    case "H":
                        symbolText += c;
                        break;
                    case "I":
                        symbolText += c;
                        break;
                    case "J":
                        symbolText += c;
                        break;
                    case "K":
                        symbolText += c;
                        break;
                    case "L":
                        symbolText += c;
                        break;
                    case "M":
                        symbolText += c;
                        break;
                    case "N":
                        symbolText += c;
                        break;
                    case "O":
                        symbolText += c;
                        break;
                    case "P":
                        symbolText += c;
                        break;
                    case "Q":
                        symbolText += c;
                        break;
                    case "R":
                        symbolText += c;
                        break;
                    case "S":
                        symbolText += c;
                        break;
                    case "T":
                        symbolText += c;
                        break;
                    case "U":
                        symbolText += c;
                        break;
                    case "V":
                        symbolText += c;
                        break;
                    case "W":
                        symbolText += c;
                        break;
                    case "X":
                        symbolText += c;
                        break;
                    case "Y":
                        symbolText += c;
                        break;
                    case "Z":
                        symbolText += c;
                        break;
                    case "Ä":
                        symbolText += c;
                        break;
                    case "Ö":
                        symbolText += c;
                        break;
                    case "Ü":
                        symbolText += c;
                        break;
                    case "ß":
                        symbolText += c;
                        break;
                    case "_":
                        symbolText += c;
                        break;

                    case "0":
                        symbolText += c;
                        break;
                    case "1":
                        symbolText += c;
                        break;
                    case "2":
                        symbolText += c;
                        break;
                    case "3":
                        symbolText += c;
                        break;
                    case "4":
                        symbolText += c;
                        break;
                    case "5":
                        symbolText += c;
                        break;
                    case "6":
                        symbolText += c;
                        break;
                    case "7":
                        symbolText += c;
                        break;
                    case "8":
                        symbolText += c;
                        break;
                    case "9":
                        symbolText += c;
                        break;
                    default:
                        source.GoBack();
                        breakLoop = true;
                        if (predefinedIdentifiers.ContainsKey(symbolText.ToUpper()))
                        {
                            nextSymbol.Init((Symbol.Tokens)predefinedIdentifiers[symbolText.ToUpper()], symbolText);
                        }
                        else
                        {
                            nextSymbol.Init(Symbol.Tokens.tokIdentifier, symbolText);
                        }
                        break;
                }
                if (source.EOF)
                {
                    breakLoop = true;
                }
                if (!breakLoop)
                {
                    c = source.GetNextChar();
                }
            }
            while (!breakLoop);

            return symbolText;
        }

        public void Dispose()
        {
            errorObject = null;
            source = null;

        }

        ~LexicalAnalyser()
        {
            Dispose();
        }
    }


}
