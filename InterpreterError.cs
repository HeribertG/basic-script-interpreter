
namespace basic_script_interpreter
{
    // InterpreterError: Fehler-Objekt für alle Klasse der myScript-Engine
    // Über Raise() werden die Fehlerparameter gesetzt und ein VB-Fehler
    // ausgelöst. Alle Klassen der Engine sind so ausgerichtet, dass sie
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

       

        public void Raise(int number, string source, string description, int line, int col, int index, string errSource = "")
        {
            Number = number;
            Source = source;
            Description = description;
            Line = line;
            Col = col;
            Index = index;
            ErrSource = errSource;

        }

        public void Clear()
        {
            Number = 0;
            Source = string.Empty;
            Description = string.Empty;
            Line = 0;
            Col = 0;
            Index = 0;
            ErrSource = string.Empty;
        }

  
        public int Index { get; private set; }
        public int Number { get; private set; }
        public string Source { get; private set; }
        public string Description { get; private set; }
        public int Line { get; private set; }
        public int Col { get; private set; }
        public string ErrSource { get; private set; }

    }


}
