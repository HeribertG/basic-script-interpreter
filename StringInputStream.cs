using System;
using System.Diagnostics;

namespace Basic_Script_Interpreter
{


    namespace Macro.Process
    {
        public class StringInputStream : Code.IInputStream
        {
            private string _sourcetext;
         


            public Code.IInputStream Connect(string sourcetext)
            {
               
                _sourcetext = sourcetext + " " ;
                Index = 0;

                Line = 1; Col = 0;

                errorObject = new InterpreterError();

                return this;
            }
            public InterpreterError errorObject { get; set; }

            private bool EOF
            {
                get
                {
                    return Index >= _sourcetext.Length;
                }
            }
            private int Line { get; set; }
            public int Col { get; set; }
            public int Index { get; set; }



            bool Code.IInputStream.EOF => Index >= _sourcetext.Length;

            int Code.IInputStream.Line => Line;

            int Code.IInputStream.Col => Col;

            int Code.IInputStream.Index => Index;

            InterpreterError Code.IInputStream.ErrorObject { get => errorObject; set => errorObject= value; }

            string Code.IInputStream.GetNextChar()
            {
                var result = string.Empty;

                Index++;

                string nextChar = string.Empty;
                if (Index < _sourcetext.Length)
                {
                    nextChar = _sourcetext.Substring(Index - 1, 1);
                    Col++;

                    switch (nextChar)
                    {
                        case "\t":
                            {
                                result = " ";
                                break;
                            }
                        case "\r":
                            {
                                // Nachschauen, ob "\r\n"
                                var tmp = _sourcetext.Substring(Index, 1);
                                if (tmp == "\n")
                                {
                                    Index += 1;
                                    Line += 1;
                                    Col = 0;
                                    result = "\n";
                                    break;
                                }
                                else
                                {
                                    Col -= 1;
                                    result = nextChar;
                                }

                                break;
                            }
                        case "\n":
                            {
                                // Nachschauen, ob "\n\r"
                                var tmp = _sourcetext.Substring(Index, 1);
                                if (tmp == "\r") Index += 1;

                                Line += 1; Col = 0;
                                result = "\n";
                                break;
                            }

                        default:
                            {
                                if (nextChar.Length >= 1  )
                                    result = nextChar;
                                else
                                    errorObject.Raise((int)InterpreterError.inputStreamErrors.errInvalidChar, "StringInputStream.GetNextChar", "Invalid character (ASCII " + nextChar.Substring(0, 1) + ")", Line, Col, Index);
                                break;
                            }
                    }
                }
                else
                { result = ""; }

                return result;

            }

            void Code.IInputStream.GoBack()
            {
                try
                {
                    if (!EOF)
                    {
                        if (Index > 0)
                        {
                            Col = Col - 1;
                            string c = _sourcetext.Substring(Index, 1);
                            // Achtung: Zeilenzählung auch beim Zurückgehen beachten
                            if (c == "\r\n" || c == "\n" || c == "\r")
                                Line -= 1;

                            Index -= 1;
                        }
                        else
                            errorObject.Raise((int)InterpreterError.inputStreamErrors.errGoBackPastStartOfSource, "StringInputStream.GoBack", "GoBack past start of source", 0, 0, 0);
                    }
                }
                catch (Exception ex)
                {
                    Debug.Print("StringInputStream.GoBack " + ex.Message);
                }
            }

            // Kommentare gehen immer bis zum Zeilenende; also springen wir auf den nächsten Zeilenanfang
            void Code.IInputStream.SkipComment()
            {
                int i;
                i = _sourcetext.IndexOf("\n", Index);
                if (i == -1)
                    i = _sourcetext.IndexOf("\r", Index);
                if (i == -1)
                    i = _sourcetext.IndexOf("\r\n", Index);

                if (i == 0)
                    i = _sourcetext.Length + 1;
                Col = Col + (i - Index);

                Index = i;
            }
        }
    }
}
