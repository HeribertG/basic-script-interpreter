namespace basic_script_interpreter
{
    public class Symbol
    {

        // Symbol: Ein Symbol-Objekt steht immer für 1 im Quelltext
        // erkanntes Symbol, z.B. math. Operator, Identifier, vordefinierte
        // Symbole ("FOR", "SUB" usw.).

        // Jedes Symbol bzw. jede Symbolklasse wird über ein
        // sog. token für den schnellen Vergleich identifiziert.

        // Bei simplen Symbolen wie "+" reicht für die weitere Verarbeitung
        // das token. Bei Identifier jedoch wird immer auch der Name benötigt.
        // Im Symbol sind daher token, textuelle Repräsentation und Wert vereint.


        public enum Tokens
        {
            // 0
            tokPlus,
            tokMinus,
            tokDivision,
            tokMultiplication,
            // 4
            tokPower,
            tokFactorial,
            // 6
            tokDiv // "\" oder "DIV" = ganzzahlige Division
    ,
            tokMod // "%" oder "MOD" = modulo
    ,
            // 8
            tokStringConcat // "&"
    ,
            // 9
            tokPlusEq // +=
    ,
            tokMinusEq // -=
    ,
            tokMultiplicationEq // *=
    ,
            tokDivisionEq // /=
    ,
            tokStringConcatEq // &=
    ,
            tokDivEq // \=
    ,
            tokModEq // %=
    ,
            // 16
            tokAnd,
            tokOr,
            tokNot,
            // 19
            tokEq // "="
    ,
            tokNotEq // "<>"
    ,
            tokLt // less than "<"
    ,
            tokLEq // less or equal "<="
    ,
            tokGt // greater than ">"
    ,
            tokGEq // greater or equal ">="
    ,
            // 25
            tokLeftParent,
            tokRightParent,
            // 27
            tokString,
            tokNumber,
            // 29
            tokIdentifier,
            // 30
            tokSin,
            tokCos,
            tokTan,
            tokATan,
            // 34
            tokIif,
            tokIf,
            tokThen,
            tokElse,
            tokEnd,
            tokEndif,
            tokDo,
            tokWhile,
            tokLoop,
            tokUntil,
            tokFor,
            tokTo,
            tokStep,
            tokNext,
            tokConst,
            tokDim,
            tokExternal,
            tokFunction,
            tokEndfunction,
            tokSub,
            tokEndsub,
            tokExit,
            // 56
            tokComma,
            tokStatementDelimiter,
            // 58
            tokDebugPrint,
            tokDebugClear,
            tokDebugShow,
            tokDebugHide,
            tokMsgbox,
            tokDoEvents,
            tokInputbox,
            tokMessage,
            // 66
            tokTrue,
            tokFalse,
            tokPi,
            tokCrlf,
            tokTab,
            tokCr,
            tokLf,
            // 73
            tokEof
        }

        internal void Position(int line, int col, int index)
        {
            Line = line;
            Col = col;
            Index = index;
        }

        internal void Init(Tokens token, string text = "", object value = null)
        {
            Token = token;
            Text = text;
            Value = value;
        }

        public Tokens Token { get; private set; }
        public string Text { get; private set; }
        public object Value { get; private set; }
        public int Line { get; private set; }
        public int Col { get; private set; }
        public int Index { get; private set; }
        
    }

}
