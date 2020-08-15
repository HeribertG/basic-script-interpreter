﻿using System;
using System.Diagnostics;
using System.Security.Principal;
using static basic_script_interpreter.InterpreterError;

namespace basic_script_interpreter
{


    public class LexicalAnalyser
    {
        private const string COMMENT_CHAR = "'"; // Anfangszeichen für Kommentare

        private Code.IInputStream _source; // Quelltext-Datenstrom
        private System.Collections.Generic.Dictionary<string, int> _predefinedIdentifiers;

        // Liste der vordefinierten Identifier
        private InterpreterError errorObject = new InterpreterError();


        public LexicalAnalyser Connect(Code.IInputStream source)
        {
            _source = source;

            ErrorObject = errorObject;
            return this;
        }

        public InterpreterError ErrorObject
        {
            set
            {
                try
                {
                    if (_source != null)
                    {
                        errorObject = value;
                        _source.ErrorObject = errorObject;
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
            if (!_source.EOF)
            {
                do
                {
                    c = _source.GetNextChar();
                    if (c == COMMENT_CHAR)
                    {
                        _source.SkipComment();
                        c = " ";
                    }
                }
                while (c == " " & !_source.EOF);
            }

            // Zeile/Spalte des aktuellen Symbols vermerken
            nextSymbol.Position(_source.Line, _source.Col, _source.Index);

            
            if (!_source.EOF)
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
                            c = _source.GetNextChar();
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
                                        _source.GoBack();

                                        nextSymbol.Init((Symbol.Tokens)Symbol.Tokens.tokLT, "<");
                                        break;
                                    }
                            }

                            break;
                        }

                    case ">":
                        {
                            c = _source.GetNextChar();
                            switch (c)
                            {
                                case "=":
                                    {
                                        nextSymbol.Init((Symbol.Tokens)Symbol.Tokens.tokGEq, ">=");
                                        break;
                                    }
                                default:
                                    {
                                        _source.GoBack();

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
                    case "'":
                        // StringAbschlußzeichen ::= """ | "'"
                        // Es muß dasselbe StringAbschlußzeichen am Anfang und Ende benutzt werden!
                        // Ein doppeltes StringAbschlußzeichen wird reduziert auf ein einzelnes.
                        var openChar = c;
                        symbolText = "";
                        var endOfEmptyChar = false;
                        do
                        {
                            c = _source.GetNextChar();
                            switch (c)
                            {
                                case "'":
                                    c = _source.GetNextChar();
                                    if (c == openChar)
                                    {
                                        symbolText += openChar;
                                    }
                                    else
                                    {
                                        endOfEmptyChar = true;
                                        _source.GoBack();
                                        nextSymbol.Init(Symbol.Tokens.tokString, symbolText, symbolText);
                                    }
                                    break;
                                case "\n": // keinen Zeilenwechsel im String zulassen

                                    errorObject.Raise((int)lexErrors.errUnexpectedEOL,
                                      "LexAnalyser.nextSymbol", "String not closed; unexpected end of line encountered",
                                      _source.Line, _source.Col, _source.Index);
                                    break;

                                case "":
                                    errorObject.Raise((int)lexErrors.errUnexpectedEOF,
                                      "LexAnalyser.nextSymbol", "String not closed; unexpected end of source",
                                       _source.Line, _source.Col, _source.Index);
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
            _predefinedIdentifiers = new System.Collections.Generic.Dictionary<string, int>();

            _predefinedIdentifiers.Add("DIV", (int)Symbol.Tokens.tokDiv);
            _predefinedIdentifiers.Add("MOD", (int)Symbol.Tokens.tokMod);
            _predefinedIdentifiers.Add("AND", (int)Symbol.Tokens.tokAND);
            _predefinedIdentifiers.Add("OR", (int)Symbol.Tokens.tokOR);
            _predefinedIdentifiers.Add("NOT", (int)Symbol.Tokens.tokNOT);
            _predefinedIdentifiers.Add("SIN", (int)Symbol.Tokens.tokSin);
            _predefinedIdentifiers.Add("COS", (int)Symbol.Tokens.tokCos);
            _predefinedIdentifiers.Add("TAN", (int)Symbol.Tokens.tokTan);
            _predefinedIdentifiers.Add("ATAN", (int)Symbol.Tokens.tokATan);
            _predefinedIdentifiers.Add("IIF", (int)Symbol.Tokens.tokIIF);
            _predefinedIdentifiers.Add("IF", (int)Symbol.Tokens.tokIF);
            _predefinedIdentifiers.Add("THEN", (int)Symbol.Tokens.tokTHEN);
            _predefinedIdentifiers.Add("ELSE", (int)Symbol.Tokens.tokELSE);
            _predefinedIdentifiers.Add("END", (int)Symbol.Tokens.tokEND);
            _predefinedIdentifiers.Add("ENDIF", (int)Symbol.Tokens.tokENDIF);
            _predefinedIdentifiers.Add("DO", (int)Symbol.Tokens.tokDO);
            _predefinedIdentifiers.Add("WHILE", (int)Symbol.Tokens.tokWHILE);
            _predefinedIdentifiers.Add("LOOP", (int)Symbol.Tokens.tokLOOP);
            _predefinedIdentifiers.Add("UNTIL", (int)Symbol.Tokens.tokUNTIL);
            _predefinedIdentifiers.Add("FOR", (int)Symbol.Tokens.tokFOR);
            _predefinedIdentifiers.Add("TO", (int)Symbol.Tokens.tokTO);
            _predefinedIdentifiers.Add("STEP", (int)Symbol.Tokens.tokSTEP);
            _predefinedIdentifiers.Add("NEXT", (int)Symbol.Tokens.tokNEXT);
            _predefinedIdentifiers.Add("CONST", (int)Symbol.Tokens.tokCONST);
            _predefinedIdentifiers.Add("DIM", (int)Symbol.Tokens.tokDIM);
            _predefinedIdentifiers.Add("FUNCTION", (int)Symbol.Tokens.tokFUNCTION);
            _predefinedIdentifiers.Add("ENDFUNCTION", (int)Symbol.Tokens.tokENDFUNCTION);
            _predefinedIdentifiers.Add("SUB", (int)Symbol.Tokens.tokSUB);
            _predefinedIdentifiers.Add("ENDSUB", (int)Symbol.Tokens.tokENDSUB);
            _predefinedIdentifiers.Add("EXIT", (int)Symbol.Tokens.tokEXIT);
            _predefinedIdentifiers.Add("DEBUGPRINT", (int)Symbol.Tokens.tokDebugPrint);
            _predefinedIdentifiers.Add("DEBUGCLEAR", (int)Symbol.Tokens.tokDebugClear);
            _predefinedIdentifiers.Add("DEBUGSHOW", (int)Symbol.Tokens.tokDebugShow);
            _predefinedIdentifiers.Add("DEBUGHIDE", (int)Symbol.Tokens.tokDebugHide);
            _predefinedIdentifiers.Add("MSGBOX", (int)Symbol.Tokens.tokMsgbox);
            _predefinedIdentifiers.Add("MESSAGE", (int)Symbol.Tokens.tokMessage);
            _predefinedIdentifiers.Add("DOEVENTS", (int)Symbol.Tokens.tokDoEvents);
            _predefinedIdentifiers.Add("INPUTBOX", (int)Symbol.Tokens.tokInputbox);
            _predefinedIdentifiers.Add("TRUE", (int)Symbol.Tokens.tokTrue);
            _predefinedIdentifiers.Add("FALSE", (int)Symbol.Tokens.tokFalse);
            _predefinedIdentifiers.Add("PI", (int)Symbol.Tokens.tokPI);
            _predefinedIdentifiers.Add("VBCRLF", (int)Symbol.Tokens.tokCrlf);
            _predefinedIdentifiers.Add("VBTAB", (int)Symbol.Tokens.tokTab);
            _predefinedIdentifiers.Add("VBCR", (int)Symbol.Tokens.tokCr);
            _predefinedIdentifiers.Add("VBLF", (int)Symbol.Tokens.tokLf);
            _predefinedIdentifiers.Add("IMPORT", (int)Symbol.Tokens.tokEXTERNAL);


        }

        private void mathOperatorOrAssignments(Symbol nextSymbol, string c)
        {
            var symbolText = c;
            c = _source.GetNextChar();

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

            if (c != "=") { _source.GoBack(); }
        }

        private Tuple<bool, string> Numbers(Symbol nextSymbol, string c)
        {
            var symbolText = c;
            var returnNumberSymbol = false;

            do
            {
                c = _source.GetNextChar();
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
                        c = _source.GetNextChar();
                        if (Helper.IsNumericInt(c) && ((int.Parse(c) >= 0) && (int.Parse(c) <= 9)))
                        {
                            symbolText = (symbolText + c);
                        }
                        else
                        {
                            _source.GoBack();
                            returnNumberSymbol = true;
                            break;
                        }

                    }

                    break;
                }
                else
                {
                    _source.GoBack();
                    returnNumberSymbol = true;
                    break;
                }
            } while (_source.EOF == false);

            return new Tuple<bool, string>(returnNumberSymbol, symbolText);
        }

        private string Identifier(Symbol nextSymbol, string c)
        {

            string symbolText = c;
            bool breakLoop = false;

            c = _source.GetNextChar();
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
                        _source.GoBack();
                        breakLoop = true;
                        if (_predefinedIdentifiers.ContainsKey(symbolText.ToUpper()))
                        {
                            nextSymbol.Init((Symbol.Tokens)_predefinedIdentifiers[symbolText.ToUpper()], symbolText);
                        }
                        else
                        {
                            nextSymbol.Init(Symbol.Tokens.tokIdentifier, symbolText);
                        }
                        break;
                }
                if (_source.EOF)
                {
                    breakLoop = true;
                }
                if (!breakLoop)
                {
                    c = _source.GetNextChar();
                }
            }
            while (!breakLoop);

            return symbolText;
        }
    }


}