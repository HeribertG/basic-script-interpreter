using basic_script_interpreter.Macro.Process;
using System;
using System.Collections.ObjectModel;

// -----------------------------------------------------------------
// Copyright © 2011 Heribert Gasparoli
// -----------------------------------------------------------------
// 
// Stand: 01/2011
// 
// -----------------------------------------------------------------

namespace basic_script_interpreter
{
    public class Code :IDisposable
    {


        // -------------------
        // EREIGNISDEKLARATION
        // -------------------
        public event DebugPrintEventHandler DebugPrint;

        public delegate void DebugPrintEventHandler(string msg);

        public event DebugClearEventHandler DebugClear;

        public delegate void DebugClearEventHandler();

        public event DebugShowEventHandler DebugShow;

        public delegate void DebugShowEventHandler();

        public event DebugHideEventHandler DebugHide;

        public delegate void DebugHideEventHandler();

        // für nicht definierte Vars dem Host erlauben, sie per
        // Event bereitzustellen
        public event AssignEventHandler Assign;

        public delegate void AssignEventHandler(string name, string Value, ref bool accepted);

        public event RetrieveEventHandler Retrieve;

        public delegate void RetrieveEventHandler(string name, string Value, bool accepted);

        public event TimeoutEventHandler Timeout;    // für TimeOut

        public delegate void TimeoutEventHandler(bool cont);

        public event MessageEventHandler Message;

        public delegate void MessageEventHandler(int Type, string Message);

        public interface IInputStream
        {
            IInputStream Connect(string connectString); // Objekt an Script-Code anbinden
            string GetNextChar(); // Nächstes Zeichen aus Code herauslesen


            // Um 1 Zeichen im Code zurückgehen
            // Wenn diese Funktion für eine bestimmte Quelle nicht realisierbar
            // ist, muß ein Fehler erzeugt werden: errGoBackPastStartOfSource
            // oder errGoBackNotImplemented
            void GoBack();
            void SkipComment(); // Kommentar im Code überspringe
            bool EOF { get; }  // Ist das Code-Ende erreicht?
            int Line { get; } // aktuelle Zeilennummer im Code
            int Col { get; } // aktuelle Spalte im Code
            int Index { get; } // aktuelle Index im Code
            InterpreterError ErrorObject { get; set; } // Fehlerobjekt lesen oder Fehlerobjekt setzen
        }



        // Befehlscodes der Virtuellen Maschine (VM)
        // Die VM ist im wesentlichen eine Stack-Maschine. Die meisten
        // für die Operationen relevanten Parameter befinden sich
        // jeweils auf dem Stack, wenn der Befehl zur Ausführung kommt.
        public enum Opcodes
        {

            // 0
            opAllocConst,
            opAllocVar,

            // 2
            opPushValue,
            opPushVariable,
            opPop,
            opPopWithIndex,

            // 6
            opAssign,

            // 7
            opAdd,
            opSub,
            opMultiplication,
            opDivision,
            opDiv,
            opMod,
            opPower,
            opStringConcat,
            opOr,
            opAnd,
            opEq,
            opNotEq,
            oplt,
            opLEq,
            opGt,
            opGEq,

            // 23
            opNegate,
            opNot,
            opFactorial,
            opSin,
            opCos,
            opTan,
            opATan,

            // 30
            opDebugPrint,
            opDebugClear,
            opDebugShow,
            opDebugHide,
            opMsgbox,
            opDoEvents,
            opInputbox,

            // 37
            opJump,
            opJumpTrue,
            opJumpFalse,
            opJumpPop,

            // 41
            opPushScope,
            opPopScope,
            opCall,
            opReturn,
            opMessage
        }



        // -------------------------------
        // LOKALE VARIABLEN (VerweisTypen)
        // -------------------------------
        private Collection<object> _code = new Collection<object>(); // jeder Item ist eine komplette Anweisung mit evtl. Parametern

        private Scopes _scopes;
        private int _pc; // Program Counter = zeigt auf aktuelle Anweisung in _code
        private Scope _external = new Scope();
        private bool _running; // wird noch code ausgeführt?


        [System.Runtime.InteropServices.DllImport("kernel32")]
        private static extern int GetTickCount();


        // Übergebenen Sourcecode übersetzen.
        public bool Compile(string source, bool optionExplicit = true, bool allowExternal = true)
        {
            ErrorObject = new InterpreterError();

            // entscheiden, ob Quelltext oder ein Dateiname
            // übergeben wurden und danach einen InputStream wählen
            StringInputStream sourceStream = new StringInputStream();
            // den Inputstream syntaktisch prüfen und Code erzeugen
            var parser = new SyntaxAnalyser(ErrorObject);


            _code = new Collection<object>();

            parser.Parse(sourceStream.Connect(source), this, optionExplicit, allowExternal);

            if (ErrorObject.Number == 0)
            {
                return true;
            }
            return false;
        }


        //Code ausführen
        public bool Run()
        {
            if (ErrorObject.Number == 0)
            {
                Interpret();
                return Convert.ToBoolean(ErrorObject.Number == 0);
            }
            return false;
        }

        public InterpreterError ErrorObject { get; private set; }

        public bool AllowUI { get; set; }
        public int CodeTimeout { get; set; } = 60000; // 60 Sekunden default
        public bool Cancel { get; set; } //Bricht den Programablauf ab
        public bool Running { get; private set; } // wird noch code ausgeführt?

      


        //Befehlszählerstand der aktuell letzten Anweisung
        internal int EndOfCodePC
        {
            get
            {
                return _code.Count;
            }
        }


        public Identifier ImportAdd(string name, object value = null, Identifier.IdentifierTypes idType = Identifier.IdentifierTypes.idVariable
                                     )
        {
            return _external.Allocate(name, value, idType);
        }

        public void ImportItem(string name, object value = null)
        {
            _external.Assign(name, value);
        }


        //Globalen Gültigkeitsbereich löschen
        public void ImportClear()
        {
            _external = new Scope();
        }

        public object ImportRead(string name)
        {
            return _external.Retrieve(name);
        }

        internal Scope External()
        {
            return _external;
        }

        public Code Clone()
        {
            var clone = new Code();
            for (int i = 1; i <= _code.Count; i++)
                clone.CloneAdd(_code[i]);

            for (int i = 1; i <= _external.CloneCount(); i++)

                clone.ImportAdd(((Identifier)_external.CloneItem(i)).name);
            return clone;
        }

        internal void CloneAdd(object Value)
        {
            _code.Add(Value);
        }


        internal int Add(Opcodes opCode, params object[] parameters)
        {
            object[] operation = new object[parameters.Length + 1];
            operation[0] = opCode;

            for (int i = 0; i <= parameters.Length - 1; i++)
                operation[i + 1] = parameters[i];

            _code.Add(operation);

            return _code.Count;
        }


        internal void FixUp(int index, params object[] parameters)
        {
            object[] operation = new object[parameters.Length + 1];
            var tmp = _code[index];
            operation[0] = tmp;

            for (int i = 0; i <= parameters.Length - 1; i++)
                operation[i + 1] = parameters[i];

            _code.Remove(index);

            if (index > _code.Count)
                _code.Add(operation);
            else
                _code.Insert(index, operation);
        }


        private void Interpret()
        {
            object[] operation;
            object Akkumulator;
            object Register;

            int startTime;

            _scopes = new Scopes();
            _scopes.Push(_external);
            _scopes.Push(null);

            startTime = GetTickCount();
            Cancel = false;
            _running = true;

            _pc = 1;

            bool accepted = false;
            bool continues = false;
            object xPos, default_Renamed, yPos;
            while ((_pc <= _code.Count - 1) & _running)
            {
                Akkumulator = null;
                Register = null;

                operation = (Object[])_code[_pc];

                switch ((Opcodes)operation.GetValue(0))
                {
                    case Opcodes.opAllocConst // Konstante allozieren
                   :
                        {
                            // Parameter:    Name der Konstanten; Wert
                            _scopes.Allocate(operation.GetValue(1).ToString(), operation.GetValue(2).ToString(), Identifier.IdentifierTypes.idConst);
                            break;
                        }

                    case Opcodes.opAllocVar // Variable allozieren
             :
                        {
                            // Parameter:    Name der Variablen
                            _scopes.Allocate(operation.GetValue(1).ToString());
                            break;
                        }

                    case Opcodes.opPushValue // Wert auf den Stack schieben
             :
                        {
                            // Parameter:    Wert
                            _scopes.Push(operation.GetValue(1));
                            break;
                        }

                    case Opcodes.opPushVariable // Wert einer Variablen auf den Stack schieben
             :
                        {
                            // Parameter:    Variablenname
                            try
                            {
                                Register = _scopes.Retrieve(operation.GetValue(1).ToString()).value;
                            }
                            catch (Exception ex)
                            {
                                // Variable nicht alloziert, also bei Host nachfragen
                                accepted = false;
                                Retrieve?.Invoke(operation.GetValue(1).ToString(), Register.ToString(), accepted);
                                if (!accepted)
                                {
                                    // der Host weiß nichts von der Var. Implizit anlegen tun wir
                                    // sie aber nicht, da sie hier auf sie sofort lesend zugegriffen
                                    // würde

                                    _running = false;
                                    ErrorObject.Raise((int)InterpreterError.runErrors.errUnknownVar, "Code.Run", "Unknown variable '" + operation.GetValue(1).ToString() + "'", 0, 0, 0);
                                }
                            }

                            //if (TypeName(register) == "Error")
                            //{
                            //    _running = false;
                            //    ErrorObject.Raise(InterpreterError.runErrors.errUninitializedVar, "Code.Run", "Variable '" + operation.GetValue(1).ToString() + "' not hasn´t been assigned a value yet", 0, 0, 0);
                            //}

                            _scopes.Push(Register);
                            break;
                        }

                    case Opcodes.opPop // entfernt obersten Wert vom Stack
             :
                        {
                            _scopes.PopScopes(null);
                            break;
                        }

                    case Opcodes.opPopWithIndex // legt den n-ten Stackwert zuoberst auf den Stack
             :
                        {
                            // Parameter:    Index in den Stack (von oben an gezählt: 0..n)
                            _scopes.Push(_scopes.Pop(Convert.ToInt32(operation.GetValue(1))));
                            break;
                        }

                    case Opcodes.opAssign // Wert auf dem Stack einer Variablen zuweisen
             :
                        {
                            // Parameter:    Variablenname
                            // Stack:        der zuzuweisende Wert
                            try
                            {
                                Register = _scopes.PopScopes(null);
                                _scopes.Assign(operation.GetValue(1).ToString(), Register);
                            }
                            catch (Exception ex)
                            {
                                // Variable nicht alloziert, also Host anbieten
                                accepted = false;
                                Assign?.Invoke(operation.GetValue(1).ToString(), Register.ToString(), ref accepted);
                                if (!accepted)
                                    // Host hat nicht mit Var am Hut, dann legen wir
                                    // sie eben selbst an
                                    _scopes.Allocate(operation.GetValue(1).ToString(), Register.ToString());
                            }

                            break;
                        }

                    case Opcodes.opAdd:
                        binaryMathOperators(operation, Akkumulator, Register);
                        break;
                    case Opcodes.opSub:
                        binaryMathOperators(operation, Akkumulator, Register);
                        break;
                    case Opcodes.opMultiplication:
                        binaryMathOperators(operation, Akkumulator, Register);
                        break;
                    case Opcodes.opDivision:
                        binaryMathOperators(operation, Akkumulator, Register);
                        break;
                    case Opcodes.opDiv:
                        binaryMathOperators(operation, Akkumulator, Register);
                        break;
                    case Opcodes.opMod:
                        binaryMathOperators(operation, Akkumulator, Register);
                        break;
                    case Opcodes.opPower:
                        binaryMathOperators(operation, Akkumulator, Register);
                        break;
                    case Opcodes.opStringConcat:
                        binaryMathOperators(operation, Akkumulator, Register);
                        break;
                    case Opcodes.opOr:
                        binaryMathOperators(operation, Akkumulator, Register);
                        break;
                    case Opcodes.opAnd:
                        binaryMathOperators(operation, Akkumulator, Register);
                        break;
                    case Opcodes.opEq:
                        binaryMathOperators(operation, Akkumulator, Register);
                        break;
                    case Opcodes.opNotEq:
                        binaryMathOperators(operation, Akkumulator, Register);
                        break;
                    case Opcodes.oplt:
                        binaryMathOperators(operation, Akkumulator, Register);
                        break;
                    case Opcodes.opLEq:
                        binaryMathOperators(operation, Akkumulator, Register);
                        break;
                    case Opcodes.opGt:
                        binaryMathOperators(operation, Akkumulator, Register);
                        break;
                    case Opcodes.opGEq:
                        binaryMathOperators(operation, Akkumulator, Register);
                        break;

                    case Opcodes.opNegate:
                        unaryMathOperators(operation, Akkumulator, Register);
                        break;
                    case Opcodes.opNot:
                        unaryMathOperators(operation, Akkumulator, Register);
                        break;
                    case Opcodes.opFactorial:
                        unaryMathOperators(operation, Akkumulator, Register);
                        break;
                    case Opcodes.opSin:
                        unaryMathOperators(operation, Akkumulator, Register);
                        break;
                    case Opcodes.opCos:
                        unaryMathOperators(operation, Akkumulator, Register);
                        break;
                    case Opcodes.opTan:
                        unaryMathOperators(operation, Akkumulator, Register);
                        break;
                    case Opcodes.opATan:
                        unaryMathOperators(operation, Akkumulator, Register);
                        break;

                    case Opcodes.opDebugPrint:
                        {

                            string msg = string.Empty;


                            Register = _scopes.PopScopes(null);
                            if (Register != null) { msg = Register.ToString(); }

                            DebugPrint?.Invoke(msg);


                            break;
                        }

                    case Opcodes.opDebugClear:
                        {

                            DebugClear?.Invoke();
                            break;
                        }

                    case Opcodes.opDebugShow:
                        {

                            DebugShow?.Invoke();
                            break;
                        }

                    case Opcodes.opDebugHide:
                        {

                            DebugHide?.Invoke();
                            break;
                        }

                    case Opcodes.opMessage:
                        {
                            try
                            {
                                string msg = string.Empty;
                                int Type;
                                Register = _scopes.PopScopes(null); // Message
                                Akkumulator = _scopes.PopScopes(null); // Type
                                Type = Convert.ToInt32(Akkumulator);

                                msg = Register.ToString();
                                Message?.Invoke(Type, msg);
                            }
                            catch (Exception ex)
                            {
                                Message?.Invoke(-1, string.Empty);
                            }

                            break;
                        }

                    case Opcodes.opMsgbox:
                        {
                            if (!AllowUI)
                            {
                                _running = false;
                                ErrorObject.Raise((int)InterpreterError.runErrors.errNoUIallowed, "Code.Run", "MsgBox-Statement cannot be executed when no UI-elements are allowed", 0, 0, 0);
                            }

                            Register = _scopes.PopScopes(null); // Title
                            Akkumulator = _scopes.PopScopes(null); // Buttons

                            try
                            {

                                // TODO:InputBox  // _scopes.Push(MsgBox(_scopes.Pop, (MsgBoxStyle)Akkumulator.ToString(), Register));
                            }
                            catch (Exception ex)
                            {

                                _running = false;
                                ErrorObject.Raise((int)InterpreterError.runErrors.errMath, "Code.Run", "Error during MsgBox-call: " + ex.HResult + " (" + ex.Message + ")", 0, 0, 0);
                            }

                            break;
                        }

                    case Opcodes.opDoEvents:
                        {

                            break;
                        }

                    case Opcodes.opInputbox:
                        {
                            if (!AllowUI)
                            {
                                _running = false;
                                ErrorObject.Raise((int)InterpreterError.runErrors.errNoUIallowed, "Code.Run", "Inputbox-Statement cannot be executed when no UI-elements are allowed", 0, 0, 0);
                            }

                            yPos = _scopes.PopScopes(null);
                            xPos = _scopes.PopScopes(null);
                            default_Renamed = _scopes.PopScopes(null);
                            Register = _scopes.PopScopes(null);
                            Akkumulator = _scopes.PopScopes(null);

                            try
                            {
                                // TODO:InputBox
                                //string Anwert = Microsoft.VisualBasic.Interaction.InputBox(Akkumulator.ToString(), Register.ToString(), default_Renamed.ToString(), Convert.ToInt32(xPos), Convert.ToInt32(yPos));
                                //_scopes.Push(Anwert);
                            }
                            catch (Exception ex)
                            {
                                _running = false;
                                ErrorObject.Raise((int)InterpreterError.runErrors.errMath, "Code.Run", "Error during MsgBox-call: " + ex.HResult + " (" + ex.Message + ")", 0, 0, 0);
                            }

                            break;
                        }

                    case Opcodes.opJump:
                        {
                            _pc = Convert.ToInt32(operation.GetValue(1)) - 1;
                            break;
                        }

                    case Opcodes.opJumpTrue:
                        {
                            Akkumulator = _scopes.PopScopes(null);
                            if (Convert.ToBoolean(Akkumulator))
                                _pc = Convert.ToInt32(operation.GetValue(1)) - 1;
                            break;
                        }

                    case Opcodes.opJumpFalse:
                        {
                            Akkumulator = _scopes.PopScopes(null);
                            if (!Convert.ToBoolean(Akkumulator))
                                _pc = Convert.ToInt32(operation.GetValue(1)) - 1;
                            break;
                        }

                    case Opcodes.opJumpPop:
                        {
                            _pc = Convert.ToInt32(_scopes.PopScopes(null)) - 1;
                            break;
                        }

                    case Opcodes.opPushScope:
                        {
                            _scopes.Push(null);
                            break;
                        }

                    case Opcodes.opPopScope:
                        {
                            _scopes.PopScopes(null);
                            break;
                        }

                    case Opcodes.opCall:
                        {
                            _scopes.Allocate("~RETURNADDR", (_pc + 1).ToString(), Identifier.IdentifierTypes.idConst);
                            _pc = Convert.ToInt32(operation.GetValue(1)) - 1;
                            break;
                        }

                    case Opcodes.opReturn:
                        {
                            _pc = Convert.ToInt32(Convert.ToDouble(_scopes.Retrieve("~RETURNADDR").value) - 1);
                            break;
                        }
                }


                _pc = _pc + 1; // zum nächsten Befehl

                // wurde Interpretation unterbrochen?
                if (Cancel)
                {
                    _running = false;
                    ErrorObject.Raise((int)InterpreterError.runErrors.errCancelled, "Code.Run", "Code execution aborted", 0, 0, 0);
                }

                // Timeout erreicht?
                if (CodeTimeout > 0 & (GetTickCount() - startTime) >= CodeTimeout)
                {
                    if (AllowUI)
                        Timeout?.Invoke(continues);

                    if (continues)
                        startTime = GetTickCount(); // Timer wieder zurücksetzen und den nächsten Timeout abwarten
                    else
                    {
                        _running = false;
                        ErrorObject.Raise((int)InterpreterError.runErrors.errTimedOut, "Code.Run", "Timeout reached: code execution has been aborted", 0, 0, 0);
                    }
                }
            }

            _running = false;
        }


        // Hilfsfunktion zur Fakultätsberechnung
        private int Factorial(int n)
        {
            if (n == 0)
                return 1;
            else
                return n * Factorial(n - 1);
        }

        private void binaryMathOperators(object[] operation, object akkumulator, object register)
        {

            register = _scopes.PopScopes(null);
            akkumulator = _scopes.PopScopes(null);
            if (register != null && akkumulator != null)
            {
                try
                {
                    switch ((Opcodes)operation.GetValue(0))
                    {
                        case Opcodes.opAdd:
                            {
                                double TmpAk = 0.0D;
                                if (akkumulator.GetType() == typeof(Identifier))
                                    TmpAk = Convert.ToDouble(((Identifier)akkumulator).value);
                                else if (Helper.IsNumericDouble(akkumulator))
                                    TmpAk = Convert.ToDouble(akkumulator);
                                double TmpReg = 0.0D;
                                if (register.GetType() == typeof(Identifier))
                                    TmpReg = Convert.ToDouble(((Identifier)akkumulator).value);
                                else if (Helper.IsNumericDouble(register))
                                    TmpReg = Convert.ToDouble(register);
                                _scopes.Push(TmpAk + TmpReg);
                                break;
                            }

                        case Opcodes.opSub:
                            {
                                double TmpAk = 0.0D;
                                if (akkumulator.GetType() == typeof(Identifier))
                                    TmpAk = Convert.ToDouble(((Identifier)akkumulator).value);
                                else if (Helper.IsNumericDouble(akkumulator))
                                    TmpAk = Convert.ToDouble(akkumulator);
                                double TmpReg = 0.0D;
                                if (register.GetType() == typeof(Identifier))
                                    TmpReg = Convert.ToDouble(((Identifier)akkumulator).value);
                                else if (Helper.IsNumericDouble(register))
                                    TmpReg = Convert.ToDouble(register);
                                _scopes.Push(TmpAk - TmpReg);
                                break;
                            }

                        case Opcodes.opMultiplication:
                            {
                                double TmpAk = 0.0D;
                                if (akkumulator.GetType() == typeof(Identifier))
                                    TmpAk = Convert.ToDouble(((Identifier)akkumulator).value);
                                else if (Helper.IsNumericDouble(akkumulator))
                                    TmpAk = Convert.ToDouble(akkumulator);
                                double TmpReg = 0.0D;
                                if (register.GetType() == typeof(Identifier))
                                    TmpReg = Convert.ToDouble((((Identifier)register)).value);
                                else if (Helper.IsNumericDouble(register))
                                    TmpReg = Convert.ToDouble(register);
                                _scopes.Push(Convert.ToDouble(TmpAk) * Convert.ToDouble(TmpReg));
                                break;
                            }

                        case Opcodes.opDivision:
                            {
                                double TmpAk = 0.0D;
                                if (akkumulator.GetType() == typeof(Identifier))
                                    TmpAk = Convert.ToDouble(((Identifier)akkumulator).value);
                                else if (Helper.IsNumericDouble(akkumulator))
                                    TmpAk = Convert.ToDouble(akkumulator);
                                double TmpReg = 0.0D;
                                if (register.GetType() == typeof(Identifier))
                                    TmpReg = Convert.ToDouble(((Identifier)register).value);
                                else if (Helper.IsNumericDouble(register))
                                    TmpReg = Convert.ToDouble(register);
                                _scopes.Push(Convert.ToDouble(TmpAk) / Convert.ToDouble(TmpReg));
                                break;
                            }

                        case Opcodes.opDiv:
                            {
                                double TmpAk = 0.0D;
                                if (akkumulator.GetType() == typeof(Identifier))
                                    TmpAk = Convert.ToDouble(((Identifier)akkumulator).value);
                                else if (Helper.IsNumericDouble(akkumulator))
                                    TmpAk = Convert.ToDouble(akkumulator);
                                double TmpReg = 0.0D;
                                if (register.GetType() == typeof(Identifier))
                                    TmpReg = Convert.ToDouble(((Identifier)register).value);
                                else if (Helper.IsNumericDouble(register))
                                    TmpReg = Convert.ToDouble(register);
                                _scopes.Push(Convert.ToInt32(TmpAk) / Convert.ToInt32(TmpReg));
                                break;
                            }

                        case Opcodes.opMod:
                            {
                                double TmpAk = 0.0D;
                                if (akkumulator.GetType() == typeof(Identifier))
                                    TmpAk = Convert.ToDouble(((Identifier)akkumulator).value);
                                else if (Helper.IsNumericDouble(akkumulator))
                                    TmpAk = Convert.ToDouble(akkumulator);
                                double TmpReg = 0.0D;
                                if (register.GetType() == typeof(Identifier))
                                    TmpReg = Convert.ToDouble(((Identifier)register).value);
                                else if (Helper.IsNumericDouble(register))
                                    TmpReg = Convert.ToDouble(register);
                                _scopes.Push(Convert.ToDouble(TmpAk) % Convert.ToDouble(TmpReg));
                                break;
                            }

                        case Opcodes.opPower:
                            {
                                double TmpAk = 0.0D;
                                if (akkumulator.GetType() == typeof(Identifier))
                                    TmpAk = Convert.ToDouble(((Identifier)akkumulator).value);
                                else if (Helper.IsNumericDouble(akkumulator))
                                    TmpAk = Convert.ToDouble(akkumulator);
                                double TmpReg = 0.0D;
                                if (register.GetType() == typeof(Identifier))
                                    TmpReg = Convert.ToDouble(((Identifier)register).value);
                                else if (Helper.IsNumericDouble(register))
                                    TmpReg = Convert.ToDouble(register);
                                _scopes.Push(Math.Pow(Convert.ToDouble(TmpAk), Convert.ToDouble(TmpReg)));
                                break;
                            }

                        case Opcodes.opStringConcat:
                            {
                                string TmpAk = string.Empty;
                                if (akkumulator.GetType() == typeof(Identifier))
                                    TmpAk = Convert.ToString(((Identifier)akkumulator).value);
                                else
                                    TmpAk = Convert.ToString(akkumulator);
                                string TmpReg = string.Empty;
                                if (register.GetType() == typeof(Identifier))
                                    TmpReg = Convert.ToString(((Identifier)register).value);
                                else
                                    TmpReg = Convert.ToString(register);
                                _scopes.Push(akkumulator.ToString() + TmpReg.ToString());
                                break;
                            }

                        case Opcodes.opOr:
                            {
                                int TmpAk = 0;
                                if (akkumulator.GetType() == typeof(Identifier))
                                    TmpAk = Convert.ToInt32(((Identifier)akkumulator).value);
                                else if (Helper.IsNumericInt(akkumulator))
                                    TmpAk = Convert.ToInt32(akkumulator);
                                int TmpReg = 0;
                                if (register.GetType() == typeof(Identifier))
                                    TmpReg = Convert.ToInt32(((Identifier)register).value);
                                else if (Helper.IsNumericInt(register))
                                    TmpReg = Convert.ToInt32(register);
                                _scopes.Push(TmpAk | TmpReg);
                                break;
                            }

                        case Opcodes.opAnd:
                            {
                                int TmpAk = 0;
                                if (akkumulator.GetType() == typeof(Identifier))
                                    TmpAk = Convert.ToInt32(((Identifier)akkumulator).value);
                                else if (Helper.IsNumericInt(akkumulator))
                                    TmpAk = Convert.ToInt32(akkumulator);
                                int TmpReg = 0;
                                if (register.GetType() == typeof(Identifier))
                                    TmpReg = Convert.ToInt32(((Identifier)register).value);
                                else if (Helper.IsNumericInt(register))
                                    TmpReg = Convert.ToInt32(register);
                                _scopes.Push(TmpAk & TmpReg);
                                break;
                            }

                        case Opcodes.opEq // =
                 :
                            {
                                string TmpAk = string.Empty;
                                if (akkumulator.GetType() == typeof(Identifier))
                                    TmpAk = Convert.ToString(((Identifier)akkumulator).value);
                                else
                                    TmpAk = Convert.ToString(akkumulator);
                                string TmpReg = string.Empty;
                                if (register.GetType() == typeof(Identifier))
                                    TmpReg = Convert.ToString(((Identifier)register).value);
                                else
                                    TmpReg = Convert.ToString(register);
                                _scopes.Push(TmpAk.Equals(TmpReg));
                                break;
                            }

                        case Opcodes.opNotEq // <>
                 :
                            {
                                string TmpAk = string.Empty;
                                if (akkumulator.GetType() == typeof(Identifier))
                                    TmpAk = Convert.ToString(((Identifier)akkumulator).value);
                                else
                                    TmpAk = Convert.ToString(akkumulator);
                                string TmpReg = string.Empty;
                                if (register.GetType() == typeof(Identifier))
                                    TmpReg = Convert.ToString(((Identifier)register).value);
                                else if (!Helper.IsNumericInt(register))
                                    TmpReg = Convert.ToString(register);
                                _scopes.Push(!TmpAk.Equals(TmpReg));
                                break;
                            }

                        case Opcodes.oplt // <
                 :
                            {
                                double TmpAk = 0.0D;
                                if (akkumulator.GetType() == typeof(Identifier))
                                    TmpAk = Convert.ToInt32(((Identifier)akkumulator).value);
                                else if (Helper.IsNumericInt(akkumulator))
                                    TmpAk = Convert.ToDouble(akkumulator);
                                double TmpReg = 0.0D;
                                if (register.GetType() == typeof(Identifier))
                                    TmpReg = Convert.ToInt32(((Identifier)register).value);
                                else if (Helper.IsNumericInt(register))
                                    TmpReg = Convert.ToDouble(register);
                                _scopes.Push(TmpAk < TmpReg);
                                break;
                            }

                        case Opcodes.opLEq // <=
                 :
                            {
                                double TmpAk = 0.0D;
                                if (akkumulator.GetType() == typeof(Identifier))
                                    TmpAk = Convert.ToInt32(((Identifier)akkumulator).value);
                                else if (Helper.IsNumericInt(akkumulator))
                                    TmpAk = Convert.ToDouble(akkumulator);
                                double TmpReg = 0.0D;
                                if (register.GetType() == typeof(Identifier))
                                    TmpReg = Convert.ToInt32(((Identifier)register).value);
                                else if (Helper.IsNumericInt(register))
                                    TmpReg = Convert.ToDouble(register);
                                _scopes.Push(TmpAk <= TmpReg);
                                break;
                            }

                        case Opcodes.opGt // >
                 :
                            {
                                double TmpAk = 0.0D;
                                if (akkumulator.GetType() == typeof(Identifier))
                                    TmpAk = Convert.ToInt32(((Identifier)akkumulator).value);
                                else if (Helper.IsNumericInt(akkumulator))
                                    TmpAk = Convert.ToDouble(akkumulator);
                                double TmpReg = 0;
                                if (register.GetType() == typeof(Identifier))
                                    TmpReg = Convert.ToInt32(((Identifier)register).value);
                                else if (Helper.IsNumericInt(register))
                                    TmpReg = Convert.ToDouble(register);
                                _scopes.Push(TmpAk > TmpReg);
                                break;
                            }

                        case Opcodes.opGEq // >=
                 :
                            {
                                double TmpAk = 0.0D;
                                if (akkumulator.GetType() == typeof(Identifier))
                                    TmpAk = Convert.ToInt32(((Identifier)akkumulator).value);
                                else if (Helper.IsNumericInt(akkumulator))
                                    TmpAk = Convert.ToDouble(akkumulator);
                                double TmpReg = 0.0D;
                                if (register.GetType() == typeof(Identifier))
                                    TmpReg = Convert.ToInt32(((Identifier)register).value);
                                else if (Helper.IsNumericInt(register))
                                    TmpReg = Convert.ToDouble(register);
                                _scopes.Push(TmpAk >= TmpReg);
                                break;
                            }
                    }
                }
                catch (Exception ex)
                {

                    _running = false;
                    ErrorObject.Raise((int)InterpreterError.runErrors.errMath, "Code.Run", "Error during calculation (binary op " + operation.GetValue(0).ToString() + "): " + ex.HResult + "(" + ex.Message + ")", 0, 0, 0);
                }
            }


        }
        private void unaryMathOperators(object[] operation, object Akkumulator, object register)
        {
            Akkumulator = _scopes.PopScopes(null);

            try
            {
                switch ((Opcodes)operation.GetValue(0))
                {
                    case Opcodes.opNegate:
                        {
                            _scopes.Push(-Convert.ToDouble(Akkumulator));
                            break;
                        }

                    case Opcodes.opNot:
                        {
                            var tmp = Convert.ToBoolean(Akkumulator);
                            _scopes.Push(!tmp);
                            break;
                        }

                    case Opcodes.opFactorial:
                        {
                            _scopes.Push(Factorial(Convert.ToInt32(Akkumulator)));
                            break;
                        }

                    case Opcodes.opSin:
                        {
                            _scopes.Push(System.Math.Sin(Convert.ToDouble(Akkumulator)));
                            break;
                        }

                    case Opcodes.opCos:
                        {
                            _scopes.Push(System.Math.Cos(Convert.ToDouble(Akkumulator)));
                            break;
                        }

                    case Opcodes.opTan:
                        {
                            _scopes.Push(System.Math.Tan(Convert.ToDouble(Akkumulator)));
                            break;
                        }

                    case Opcodes.opATan:
                        {
                            _scopes.Push(System.Math.Atan(Convert.ToDouble(Akkumulator)));
                            break;
                        }
                }
            }
            catch (Exception ex)
            {

                _running = false;
                ErrorObject.Raise((int)InterpreterError.runErrors.errMath, "Code.Run", "Error during calculation (unary op " + operation.GetValue(0).ToString() + "): " + ex.HResult + " (" + ex.Message + ")", 0, 0, 0);
            }

        }

        public void Dispose()
        {
            ErrorObject = null;
        }
    }

}
