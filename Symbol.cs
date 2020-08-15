using System;
using System.Collections.Generic;
using System.Text;

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
            tokAND,
            tokOR,
            tokNOT,
            // 19
            tokEq // "="
    ,
            tokNotEq // "<>"
    ,
            tokLT // less than "<"
    ,
            tokLEq // less or equal "<="
    ,
            tokGT // greater than ">"
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
            tokIIF,
            tokIF,
            tokTHEN,
            tokELSE,
            tokEND,
            tokENDIF,
            tokDO,
            tokWHILE,
            tokLOOP,
            tokUNTIL,
            tokFOR,
            tokTO,
            tokSTEP,
            tokNEXT,
            tokCONST,
            tokDIM,
            tokEXTERNAL,
            tokFUNCTION,
            tokENDFUNCTION,
            tokSUB,
            tokENDSUB,
            tokEXIT,
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
            tokPI,
            tokCrlf,
            tokTab,
            tokCr,
            tokLf,
            // 73
            tokEOF
        }

        private Tokens _token;
        private string _text;
        private object _value;
        private int _line;
        private int _col;
        private int _Index;

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
