using System;
using System.Diagnostics;
using static basic_script_interpreter.InterpreterError;

namespace basic_script_interpreter
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
          case "-":
          case "*":
          case "/":
          case "&":
          case "\\":
          case "%":
            MathOperatorOrAssignments(nextSymbol, c);
            break;

          case "^":
            {
              nextSymbol.Init(Symbol.Tokens.tokPower, c);
              break;
            }

          case "!":
            {
              nextSymbol.Init(Symbol.Tokens.tokFactorial, c);
              break;
            }

          case "~":
            {
              nextSymbol.Init(Symbol.Tokens.tokNot, c);
              break;
            }

          case "(":
            {
              nextSymbol.Init(Symbol.Tokens.tokLeftParent, c);
              break;
            }

          case ")":
            {
              nextSymbol.Init(Symbol.Tokens.tokRightParent, c);
              break;
            }

          case ",":
            {
              nextSymbol.Init(Symbol.Tokens.tokComma, c);
              break;
            }

          case "=":
            {
              nextSymbol.Init(Symbol.Tokens.tokEq, c);
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
                    nextSymbol.Init(Symbol.Tokens.tokNotEq, "<>");
                    break;
                  }

                case "=":
                  {
                    nextSymbol.Init(Symbol.Tokens.tokLEq, "<=");
                    break;
                  }

                default:
                  {
                    source.GoBack();

                    nextSymbol.Init(Symbol.Tokens.tokLt, "<");
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
                    nextSymbol.Init(Symbol.Tokens.tokGEq, ">=");
                    break;
                  }
                default:
                  {
                    source.GoBack();

                    nextSymbol.Init(Symbol.Tokens.tokGt, ">");
                    break;
                  }
              }

              break;
            }

          case "0":
          case "1":
          case "2":
          case "3":
          case "4":
          case "5":
          case "6":
          case "7":
          case "8":
          case "9":
            var res = Numbers(nextSymbol, c);
            returnNumberSymbol = res.Item1;
            symbolText = res.Item2;
            break;
          case "@":
          case "A":
          case "B":
          case "C":
          case "D":
          case "E":
          case "F":
          case "G":
          case "H":
          case "I":
          case "J":
          case "K":
          case "L":
          case "M":
          case "N":
          case "O":
          case "P":
          case "Q":
          case "R":
          case "S":
          case "T":
          case "U":
          case "V":
          case "W":
          case "X":
          case "Y":
          case "Z":
          case "Ä":
          case "Ö":
          case "Ü":
          case "ß":
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
              errorObject.Raise(Convert.ToInt32(lexErrors.errUnknownSymbol), "LexicalAnalyser.GetNextSymbol", "Unknown symbol starting with character ASCII " + c, nextSymbol.Line, nextSymbol.Col, nextSymbol.Index);
              break;
            }
        }
      }
      else
      {
        nextSymbol.Init(Symbol.Tokens.tokEof);
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
      predefinedIdentifiers.Add("AND", (int)Symbol.Tokens.tokAnd);
      predefinedIdentifiers.Add("OR", (int)Symbol.Tokens.tokOr);
      predefinedIdentifiers.Add("NOT", (int)Symbol.Tokens.tokNot);
      predefinedIdentifiers.Add("SIN", (int)Symbol.Tokens.tokSin);
      predefinedIdentifiers.Add("COS", (int)Symbol.Tokens.tokCos);
      predefinedIdentifiers.Add("TAN", (int)Symbol.Tokens.tokTan);
      predefinedIdentifiers.Add("ATAN", (int)Symbol.Tokens.tokATan);
      predefinedIdentifiers.Add("IIF", (int)Symbol.Tokens.tokIif);
      predefinedIdentifiers.Add("IF", (int)Symbol.Tokens.tokIf);
      predefinedIdentifiers.Add("THEN", (int)Symbol.Tokens.tokThen);
      predefinedIdentifiers.Add("ELSE", (int)Symbol.Tokens.tokElse);
      predefinedIdentifiers.Add("END", (int)Symbol.Tokens.tokEnd);
      predefinedIdentifiers.Add("ENDIF", (int)Symbol.Tokens.tokEndif);
      predefinedIdentifiers.Add("DO", (int)Symbol.Tokens.tokDo);
      predefinedIdentifiers.Add("WHILE", (int)Symbol.Tokens.tokWhile);
      predefinedIdentifiers.Add("LOOP", (int)Symbol.Tokens.tokLoop);
      predefinedIdentifiers.Add("UNTIL", (int)Symbol.Tokens.tokUntil);
      predefinedIdentifiers.Add("FOR", (int)Symbol.Tokens.tokFor);
      predefinedIdentifiers.Add("TO", (int)Symbol.Tokens.tokTo);
      predefinedIdentifiers.Add("STEP", (int)Symbol.Tokens.tokStep);
      predefinedIdentifiers.Add("NEXT", (int)Symbol.Tokens.tokNext);
      predefinedIdentifiers.Add("CONST", (int)Symbol.Tokens.tokConst);
      predefinedIdentifiers.Add("DIM", (int)Symbol.Tokens.tokDim);
      predefinedIdentifiers.Add("FUNCTION", (int)Symbol.Tokens.tokFunction);
      predefinedIdentifiers.Add("ENDFUNCTION", (int)Symbol.Tokens.tokEndfunction);
      predefinedIdentifiers.Add("SUB", (int)Symbol.Tokens.tokSub);
      predefinedIdentifiers.Add("ENDSUB", (int)Symbol.Tokens.tokEndsub);
      predefinedIdentifiers.Add("EXIT", (int)Symbol.Tokens.tokExit);
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
      predefinedIdentifiers.Add("PI", (int)Symbol.Tokens.tokPi);
      predefinedIdentifiers.Add("VBCRLF", (int)Symbol.Tokens.tokCrlf);
      predefinedIdentifiers.Add("VBTAB", (int)Symbol.Tokens.tokTab);
      predefinedIdentifiers.Add("VBCR", (int)Symbol.Tokens.tokCr);
      predefinedIdentifiers.Add("VBLF", (int)Symbol.Tokens.tokLf);
      predefinedIdentifiers.Add("IMPORT", (int)Symbol.Tokens.tokExternal);


    }

    private void MathOperatorOrAssignments(Symbol nextSymbol, string c)
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
          case "A":
          case "B":
          case "C":
          case "D":
          case "E":
          case "F":
          case "G":
          case "H":
          case "I":
          case "J":
          case "K":
          case "L":
          case "M":
          case "N":
          case "O":
          case "P":
          case "Q":
          case "R":
          case "S":
          case "T":
          case "U":
          case "V":
          case "W":
          case "X":
          case "Y":
          case "Z":
          case "Ä":
          case "Ö":
          case "Ü":
          case "ß":
          case "_":
          case "0":
          case "1":
          case "2":
          case "3":
          case "4":
          case "5":
          case "6":
          case "7":
          case "8":
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
