
namespace basic_script_interpreter
{
    // InterpreterError: Fehler-Objekt für alle Klasse der myScript-Engine
    // Über Raise() werden die Fehlerparameter gesetzt und ein VB-Fehler
    // ausgelöst. Alle Klassen der Engine sind so ausgerichtet, daß sie
    // bei einem VB-Fehler komplett zurückfallen zur Aufrufstelle des
    // Parsers bzw. Interpreters, d.h. jeder Syntaxfehler usw. ist für
    // den Script-Host ein trappable error.

    public class InterpreterError
    {
        const int OBJECTERROR = -2147221504;
        public enum inputStreamErrors
        {
            errGoBackPastStartOfSource = OBJECTERROR + 1,
            errInvalidChar = OBJECTERROR + 2,
            errGoBackNotImplemented = OBJECTERROR + 3
        }

        public enum lexErrors
        {
            errUnknownSymbol = OBJECTERROR + 21,
            errUnexpectedEOF = OBJECTERROR + 22,
            errUnexpectedEOL = OBJECTERROR + 23
        }

        public enum parsErrors
        {
            errMissingClosingParent = OBJECTERROR + 31,
            errUnexpectedSymbol = OBJECTERROR + 32,
            errMissingLeftParent = OBJECTERROR + 33,
            errMissingComma = OBJECTERROR + 34,
            errNoYetImplemented = OBJECTERROR + 35,
            errSyntaxViolation = OBJECTERROR + 36,
            errIdentifierAlreadyExists = OBJECTERROR + 37,
            errWrongNumberOfParams = OBJECTERROR + 38,
            errCannotCallSubInExpression = OBJECTERROR + 39
        }

        public enum runErrors
        {
            errMath = OBJECTERROR + 61,
            errTimedOut = OBJECTERROR + 62,
            errCancelled = OBJECTERROR + 63,
            errNoUIallowed = OBJECTERROR + 64,
            errUninitializedVar = OBJECTERROR + 65,
            errUnknownVar = OBJECTERROR + 66
        }

        private int _number; // Fehlercode
        private string _source; // Fehlermeldende Routine
        private string _description; // Fehlermeldung
        private int _line; // Code-Zeile
        private int _col; // Code-Spalte
        private int _Index; // Code-Index

        private string _ErrSource; // Fehlerhaftes Symbol bzw. Code-Ausschnitt

        public void Raise(int Number, string source, string Description, int Line, int Col, int Index, string ErrSource = "")
        {
            _number = Number;
            _source = source;
            _description = Description;
            _line = Line;
            _col = Col;
            _Index = Index;
            _ErrSource = ErrSource;

        }

        public void Clear()
        {
            _number = 0;
            _source = string.Empty;
            _description = string.Empty;
            _line = 0;
            _col = 0;
            _Index = 0;
            _ErrSource = string.Empty;
        }

        // ----------------------------------------------------------------------------------------------------------------
        public int Index
        {
            get
            {
                return _Index;
            }
        }

        public int Number
        {
            get
            {
                return _number;
            }
        }

        public string source
        {
            get
            {
                return _source;
            }
        }

        public string Description
        {
            get
            {
                return _description;
            }
        }

        public int Line
        {
            get
            {
                return _line;
            }
        }

        public int Col
        {
            get
            {
                return _col;
            }
        }

        public string ErrSource
        {
            get
            {
                return _ErrSource;
            }
        }
    }


}
