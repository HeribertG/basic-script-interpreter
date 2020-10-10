using Basic_Script_Interpreter.Macro.Process;
using System;
using System.Collections.ObjectModel;
using System.Globalization;

namespace Basic_Script_Interpreter
{
    public class Code : IDisposable
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

        public delegate void MessageEventHandler(int type, string message);

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
        private Collection<object> code = new Collection<object>(); // jeder Item ist eine komplette Anweisung mit evtl. Parametern

        private Scopes scopes;
        private int pc; // Program Counter = zeigt auf aktuelle Anweisung in code
        private Scope external = new Scope();
        private bool running; // wird noch code ausgeführt?


        [System.Runtime.InteropServices.DllImport("kernel32")]
        private static extern int GetTickCount();


        // Übergebenen Sourcecode übersetzen.
        public bool Compile(string source, bool optionExplicit = true, bool allowExternal = true)
        {
            ErrorObject = new InterpreterError();

            
            StringInputStream sourceStream = new StringInputStream();
            // den Inputstream syntaktisch prüfen und Code erzeugen
            var parser = new SyntaxAnalyser(ErrorObject);


            code = new Collection<object>();

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
        internal int EndOfCodePC
        {
            get
            {
                return code.Count;
            }
        }  //Befehlszählerstand der aktuell letzten Anweisung
        public Identifier ImportAdd(string name, object value = null, Identifier.IdentifierTypes idType = Identifier.IdentifierTypes.idVariable
                                     )
        {
            return external.Allocate(name, value, idType);
        }
        public void ImportItem(string name, object value = null)
        {
            external.Assign(name, value);
        }
        public void ImportClear() //Globalen Gültigkeitsbereich löschen
        {
            external = new Scope();
        }
        public object ImportRead(string name)
        {
            return external.Retrieve(name);
        }
        internal Scope External()
        {
            return external;
        }
        public Code Clone()
        {
            var clone = new Code();
            for (int i = 1; i <= code.Count; i++)
                clone.CloneAdd(code[i]);

            for (int i = 1; i <= external.CloneCount(); i++)

                clone.ImportAdd(((Identifier)external.CloneItem(i)).Name);
            return clone;
        }
        internal void CloneAdd(object Value)
        {
            code.Add(Value);
        }
        internal int Add(Opcodes opCode, params object[] parameters)
        {
            object[] operation = new object[parameters.Length + 1];
            operation[0] = opCode;

            for (int i = 0; i <= parameters.Length - 1; i++)
                operation[i + 1] = parameters[i];

            code.Add(operation);

            return code.Count;
        }
        internal void FixUp(int index, params object[] parameters)
        {
            object[] operation = new object[parameters.Length + 1];
            var  tmp = (object[])code[index];
            operation[0] = tmp[0];

            for (int i = 0; i <= parameters.Length - 1; i++)
                operation[i + 1] = parameters[i];

            code.RemoveAt(index);

            if (index > code.Count)
                code.Add(operation);
            else
                code.Insert(index, operation);
        }


        private void Interpret()
        {
            object[] operation;
            object akkumulator;
            object register;

            int startTime;

            scopes = new Scopes();
            scopes.PushScope(external);
            scopes.PushScope();

            startTime = GetTickCount();
            Cancel = false;
            running = true;

            pc = 0;

            bool accepted = false;
            bool continues = false;
            object xPos, default_Renamed, yPos;
            while ((pc <= code.Count - 1) & running)
            {
                akkumulator = null;
                register = null;

                operation = (Object[])code[pc];

                switch ((Opcodes)operation.GetValue(0))
                {
                    // Konstante allozieren
                    case Opcodes.opAllocConst:
                        {
                            // Parameter:    Name der Konstanten; Wert
                            scopes.Allocate(operation.GetValue(1).ToString(), operation.GetValue(2).ToString(), Identifier.IdentifierTypes.idConst);
                            break;
                        }
                    // Variable allozieren
                    case Opcodes.opAllocVar:
                        {
                            // Parameter:    Name der Variablen
                            scopes.Allocate(operation.GetValue(1).ToString());
                            break;
                        }
                    // Wert auf den Stack schieben
                    case Opcodes.opPushValue:
                        {
                            // Parameter:    Wert
                            scopes.Push(operation.GetValue(1));
                            break;
                        }
                    // Wert einer Variablen auf den Stack schieben
                    case Opcodes.opPushVariable:
                        {
                            // Parameter:    Variablenname
                            Identifier tmp =null;
                            try
                            {
                                tmp = scopes.Retrieve(operation.GetValue(1).ToString());
                                register = tmp.Value;
                            }
                            catch (Exception)
                            {
                                // Variable nicht alloziert, also bei Host nachfragen
                                accepted = false;
                                Retrieve?.Invoke(operation.GetValue(1).ToString(), register.ToString(), accepted);
                                if (!accepted)
                                {
                                    // der Host weiß nichts von der Var. Implizit anlegen tun wir
                                    // sie aber nicht, da sie hier auf sie sofort lesend zugegriffen
                                    // würde

                                    running = false;
                                    ErrorObject.Raise((int)InterpreterError.runErrors.errUnknownVar, "Code.Run", "Unknown variable '" + operation.GetValue(1).ToString() + "'", 0, 0, 0);
                                }
                            }

                            if (tmp ==null)
                            {
                                running = false;
                                ErrorObject.Raise((int)InterpreterError.runErrors.errUninitializedVar, "Code.Run", "Variable '" + operation.GetValue(1).ToString() + "' not hasn´t been assigned a Value yet", 0, 0, 0);
                            } else
                            {
                            scopes.Push(register);

                            }

                            break;
                        }
                    // entfernt obersten Wert vom Stack
                    case Opcodes.opPop:
                        {
                            scopes.PopScopes();
                            break;
                        }
                    // legt den n-ten Stackwert zuoberst auf den Stack
                    case Opcodes.opPopWithIndex:
                        {
                            // Parameter:    Index in den Stack (von oben an gezählt: 0..n)
                            object result = null;
                            register = scopes.Pop(Convert.ToInt32(operation.GetValue(1)));
                            if (register is Identifier) { result = ((Identifier)register).Value; } else { result = register; }
                            scopes.Push(result);
                            break;
                        }
                    // Wert auf dem Stack einer Variablen zuweisen
                    case Opcodes.opAssign:
                        {
                            // Parameter:    Variablenname
                            // Stack:        der zuzuweisende Wert
                            try
                            {
                                object result = null;
                                register = scopes.Pop();
                                if (register is Identifier) { result = ((Identifier)register).Value; } else { result = register; }
                                scopes.Assign(operation.GetValue(1).ToString(), result);
                            }
                            catch (Exception)
                            {
                                // Variable nicht alloziert, also Host anbieten
                                accepted = false;
                                Assign?.Invoke(operation.GetValue(1).ToString(), register.ToString(), ref accepted);
                                if (!accepted)
                                    // Host hat nicht mit Var am Hut, dann legen wir
                                    // sie eben selbst an
                                    scopes.Allocate(operation.GetValue(1).ToString(), register.ToString());
                            }

                            break;
                        }

                    case Opcodes.opAdd:
                        BinaryMathOperators(operation, akkumulator, register);
                        break;
                    case Opcodes.opSub:
                        BinaryMathOperators(operation, akkumulator, register);
                        break;
                    case Opcodes.opMultiplication:
                        BinaryMathOperators(operation, akkumulator, register);
                        break;
                    case Opcodes.opDivision:
                        BinaryMathOperators(operation, akkumulator, register);
                        break;
                    case Opcodes.opDiv:
                        BinaryMathOperators(operation, akkumulator, register);
                        break;
                    case Opcodes.opMod:
                        BinaryMathOperators(operation, akkumulator, register);
                        break;
                    case Opcodes.opPower:
                        BinaryMathOperators(operation, akkumulator, register);
                        break;
                    case Opcodes.opStringConcat:
                        BinaryMathOperators(operation, akkumulator, register);
                        break;
                    case Opcodes.opOr:
                        BinaryMathOperators(operation, akkumulator, register);
                        break;
                    case Opcodes.opAnd:
                        BinaryMathOperators(operation, akkumulator, register);
                        break;
                    case Opcodes.opEq:
                        BinaryMathOperators(operation, akkumulator, register);
                        break;
                    case Opcodes.opNotEq:
                        BinaryMathOperators(operation, akkumulator, register);
                        break;
                    case Opcodes.oplt:
                        BinaryMathOperators(operation, akkumulator, register);
                        break;
                    case Opcodes.opLEq:
                        BinaryMathOperators(operation, akkumulator, register);
                        break;
                    case Opcodes.opGt:
                        BinaryMathOperators(operation, akkumulator, register);
                        break;
                    case Opcodes.opGEq:
                        BinaryMathOperators(operation, akkumulator, register);
                        break;

                    case Opcodes.opNegate:
                        UnaryMathOperators(operation);
                        break;
                    case Opcodes.opNot:
                        UnaryMathOperators(operation);
                        break;
                    case Opcodes.opFactorial:
                        UnaryMathOperators(operation);
                        break;
                    case Opcodes.opSin:
                        UnaryMathOperators(operation);
                        break;
                    case Opcodes.opCos:
                        UnaryMathOperators(operation);
                        break;
                    case Opcodes.opTan:
                        UnaryMathOperators(operation);
                        break;
                    case Opcodes.opATan:
                        UnaryMathOperators(operation);
                        break;

                    case Opcodes.opDebugPrint:
                        {

                            string msg = string.Empty;


                            register = scopes.PopScopes().Value;
                            if (register != null) { msg = register.ToString(); }

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
                                register = scopes.PopScopes().Value; // Message
                                akkumulator = scopes.PopScopes().Value; // Type
                                Type = Convert.ToInt32(akkumulator);

                                msg = register.ToString();
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
                                running = false;
                                ErrorObject.Raise((int)InterpreterError.runErrors.errNoUIallowed, "Code.Run", "MsgBox-Statement cannot be executed when no UI-elements are allowed", 0, 0, 0);
                            }

                            register = scopes.PopScopes().Value; // Title
                            akkumulator = scopes.PopScopes().Value; // Buttons

                            try
                            {

                                // TODO:InputBox  // scopes.Push(MsgBox(scopes.Pop, (MsgBoxStyle)Akkumulator.ToString(), Register));
                            }
                            catch (Exception ex)
                            {

                                running = false;
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
                                running = false;
                                ErrorObject.Raise((int)InterpreterError.runErrors.errNoUIallowed, "Code.Run", "Inputbox-Statement cannot be executed when no UI-elements are allowed", 0, 0, 0);
                            }

                            yPos = scopes.PopScopes().Value;
                            xPos = scopes.PopScopes().Value;
                            default_Renamed = scopes.PopScopes().Value;
                            register = scopes.PopScopes().Value;
                            akkumulator = scopes.PopScopes().Value;

                            try
                            {
                                // TODO:InputBox
                                //string Anwert = Microsoft.VisualBasic.Interaction.InputBox(Akkumulator.ToString(), Register.ToString(), default_Renamed.ToString(), Convert.ToInt32(xPos), Convert.ToInt32(yPos));
                                //scopes.Push(Anwert);
                            }
                            catch (Exception ex)
                            {
                                running = false;
                                ErrorObject.Raise((int)InterpreterError.runErrors.errMath, "Code.Run", "Error during MsgBox-call: " + ex.HResult + " (" + ex.Message + ")", 0, 0, 0);
                            }

                            break;
                        }

                    case Opcodes.opJump:
                        {
                            pc = Convert.ToInt32(operation.GetValue(1)) - 1;
                            break;
                        }

                    case Opcodes.opJumpTrue:
                        {
                            akkumulator = scopes.PopScopes().Value;
                            if (Convert.ToBoolean(akkumulator))
                                pc = Convert.ToInt32(operation.GetValue(1)) - 1;
                            break;
                        }

                    case Opcodes.opJumpFalse:
                        {
                            akkumulator = scopes.PopScopes().Value;
                            if (!Convert.ToBoolean(akkumulator))
                                pc = Convert.ToInt32(operation.GetValue(1)) - 1;
                            break;
                        }

                    case Opcodes.opJumpPop:
                        {
                            pc = Convert.ToInt32(scopes.PopScopes().Value) - 1;
                            break;
                        }

                    case Opcodes.opPushScope:
                        {
                            scopes.PushScope();
                            break;
                        }

                    case Opcodes.opPopScope:
                        {
                            scopes.PopScopes();
                            break;
                        }

                    case Opcodes.opCall:
                        {
                            scopes.Allocate("~RETURNADDR", (pc + 1).ToString(), Identifier.IdentifierTypes.idConst);
                            pc = Convert.ToInt32(operation.GetValue(1)) - 1;
                            break;
                        }

                    case Opcodes.opReturn:
                        {
                            pc = Convert.ToInt32(Convert.ToDouble(scopes.Retrieve("~RETURNADDR").Value, CultureInfo.InvariantCulture) - 1);
                            break;
                        }
                }


                pc = pc + 1; // zum nächsten Befehl

                // wurde Interpretation unterbrochen?
                if (Cancel)
                {
                    running = false;
                    ErrorObject.Raise((int)InterpreterError.runErrors.errCancelled, "Code.Run", "Code execution aborted", 0, 0, 0);
                }

                // Timeout erreicht?
                //if (CodeTimeout > 0 & (GetTickCount() - startTime) >= CodeTimeout)
                //{
                //    if (AllowUI)
                //        Timeout?.Invoke(continues);

                //    if (continues)
                //        startTime = GetTickCount(); // Timer wieder zurücksetzen und den nächsten Timeout abwarten
                //    else
                //    {
                //        running = false;
                //        ErrorObject.Raise((int)InterpreterError.runErrors.errTimedOut, "Code.Run", "Timeout reached: code execution has been aborted", 0, 0, 0);
                //    }
                //}
            }

            running = false;
        }


        // Hilfsfunktion zur Fakultätsberechnung
        private int Factorial(int n)
        {
            if (n == 0)
                return 1;
            else
                return n * Factorial(n - 1);
        }

        private void BinaryMathOperators(object[] operation, object akkumulator, object register)
        {

            register = scopes.PopScopes().Value;
            akkumulator = scopes.PopScopes().Value;
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
                                 
                                    TmpAk = Convert.ToDouble(((Identifier)akkumulator).Value, CultureInfo.InvariantCulture);
                                else if (Helper.IsNumericDouble(akkumulator))
                                    TmpAk = Convert.ToDouble(akkumulator, CultureInfo.InvariantCulture);
                                double TmpReg = 0.0D;
                                if (register.GetType() == typeof(Identifier))
                                    TmpReg = Convert.ToDouble(((Identifier)akkumulator).Value, CultureInfo.InvariantCulture);
                                else if (Helper.IsNumericDouble(register))
                                    TmpReg = Convert.ToDouble(register, CultureInfo.InvariantCulture);
                                scopes.Push(TmpAk + TmpReg);
                                break;
                            }

                        case Opcodes.opSub:
                            {
                                double TmpAk = 0.0D;
                                if (akkumulator.GetType() == typeof(Identifier))
                                    TmpAk = Convert.ToDouble(((Identifier)akkumulator).Value, CultureInfo.InvariantCulture);
                                else if (Helper.IsNumericDouble(akkumulator))
                                    TmpAk = Convert.ToDouble(akkumulator, CultureInfo.InvariantCulture);
                                double TmpReg = 0.0D;
                                if (register.GetType() == typeof(Identifier))
                                    TmpReg = Convert.ToDouble(((Identifier)akkumulator).Value, CultureInfo.InvariantCulture);
                                else if (Helper.IsNumericDouble(register))
                                    TmpReg = Convert.ToDouble(register, CultureInfo.InvariantCulture);
                                scopes.Push(TmpAk - TmpReg);
                                break;
                            }

                        case Opcodes.opMultiplication:
                            {
                                double TmpAk = 0.0D;
                                if (akkumulator.GetType() == typeof(Identifier))
                                    TmpAk = Convert.ToDouble(((Identifier)akkumulator).Value, CultureInfo.InvariantCulture);
                                else if (Helper.IsNumericDouble(akkumulator))
                                    TmpAk = Convert.ToDouble(akkumulator, CultureInfo.InvariantCulture);
                                double TmpReg = 0.0D;
                                if (register.GetType() == typeof(Identifier))
                                    TmpReg = Convert.ToDouble((((Identifier)register)).Value, CultureInfo.InvariantCulture);
                                else if (Helper.IsNumericDouble(register))
                                    TmpReg = Convert.ToDouble(register, CultureInfo.InvariantCulture);
                                scopes.Push(Convert.ToDouble(TmpAk, CultureInfo.InvariantCulture) * Convert.ToDouble(TmpReg, CultureInfo.InvariantCulture));
                                break;
                            }

                        case Opcodes.opDivision:
                            {
                                double TmpAk = 0.0D;
                                if (akkumulator.GetType() == typeof(Identifier))
                                    TmpAk = Convert.ToDouble(((Identifier)akkumulator).Value, CultureInfo.InvariantCulture);
                                else if (Helper.IsNumericDouble(akkumulator))
                                    TmpAk = Convert.ToDouble(akkumulator, CultureInfo.InvariantCulture);
                                double TmpReg = 0.0D;
                                if (register.GetType() == typeof(Identifier))
                                    TmpReg = Convert.ToDouble(((Identifier)register).Value, CultureInfo.InvariantCulture);
                                else if (Helper.IsNumericDouble(register))
                                    TmpReg = Convert.ToDouble(register, CultureInfo.InvariantCulture);
                                scopes.Push(Convert.ToDouble(TmpAk, CultureInfo.InvariantCulture) / Convert.ToDouble(TmpReg, CultureInfo.InvariantCulture));
                                break;
                            }

                        case Opcodes.opDiv:
                            {
                                double TmpAk = 0.0D;
                                if (akkumulator.GetType() == typeof(Identifier))
                                    TmpAk = Convert.ToDouble(((Identifier)akkumulator).Value, CultureInfo.InvariantCulture);
                                else if (Helper.IsNumericDouble(akkumulator))
                                    TmpAk = Convert.ToDouble(akkumulator, CultureInfo.InvariantCulture);
                                double TmpReg = 0.0D;
                                if (register.GetType() == typeof(Identifier))
                                    TmpReg = Convert.ToDouble(((Identifier)register).Value, CultureInfo.InvariantCulture);
                                else if (Helper.IsNumericDouble(register))
                                    TmpReg = Convert.ToDouble(register, CultureInfo.InvariantCulture);
                                scopes.Push(Convert.ToInt32(TmpAk, CultureInfo.InvariantCulture) / Convert.ToInt32(TmpReg, CultureInfo.InvariantCulture));
                                break;
                            }

                        case Opcodes.opMod:
                            {
                                double TmpAk = 0.0D;
                                if (akkumulator.GetType() == typeof(Identifier))
                                    TmpAk = Convert.ToDouble(((Identifier)akkumulator).Value, CultureInfo.InvariantCulture);
                                else if (Helper.IsNumericDouble(akkumulator))
                                    TmpAk = Convert.ToDouble(akkumulator, CultureInfo.InvariantCulture);
                                double TmpReg = 0.0D;
                                if (register.GetType() == typeof(Identifier))
                                    TmpReg = Convert.ToDouble(((Identifier)register).Value, CultureInfo.InvariantCulture);
                                else if (Helper.IsNumericDouble(register))
                                    TmpReg = Convert.ToDouble(register, CultureInfo.InvariantCulture);
                                scopes.Push(Convert.ToDouble(TmpAk, CultureInfo.InvariantCulture) % Convert.ToDouble(TmpReg, CultureInfo.InvariantCulture));
                                break;
                            }

                        case Opcodes.opPower:
                            {
                                double TmpAk = 0.0D;
                                if (akkumulator.GetType() == typeof(Identifier))
                                    TmpAk = Convert.ToDouble(((Identifier)akkumulator), CultureInfo.InvariantCulture);
                                else if (Helper.IsNumericDouble(akkumulator))
                                    TmpAk = Convert.ToDouble(akkumulator, CultureInfo.InvariantCulture);
                                double TmpReg = 0.0D;
                                if (register.GetType() == typeof(Identifier))
                                    TmpReg = Convert.ToDouble(((Identifier)register).Value, CultureInfo.InvariantCulture);
                                else if (Helper.IsNumericDouble(register))
                                    TmpReg = Convert.ToDouble(register, CultureInfo.InvariantCulture);
                                scopes.Push(Math.Pow(Convert.ToDouble(TmpAk, CultureInfo.InvariantCulture), Convert.ToDouble(TmpReg, CultureInfo.InvariantCulture)));
                                break;
                            }

                        case Opcodes.opStringConcat:
                            {
                                string TmpAk = string.Empty;
                                if (akkumulator.GetType() == typeof(Identifier))
                                    TmpAk = Convert.ToString(((Identifier)akkumulator).Value);
                                else
                                    TmpAk = Convert.ToString(akkumulator);
                                string TmpReg = string.Empty;
                                if (register.GetType() == typeof(Identifier))
                                    TmpReg = Convert.ToString(((Identifier)register).Value);
                                else
                                    TmpReg = Convert.ToString(register);
                                scopes.Push(akkumulator.ToString() + TmpReg.ToString());
                                break;
                            }

                        case Opcodes.opOr:
                            {
                                int TmpAk = 0;
                                if (akkumulator.GetType() == typeof(Identifier))
                                    TmpAk = Convert.ToInt32(((Identifier)akkumulator).Value);
                                else if (Helper.IsNumericInt(akkumulator))
                                    TmpAk = Convert.ToInt32(akkumulator);
                                int TmpReg = 0;
                                if (register.GetType() == typeof(Identifier))
                                    TmpReg = Convert.ToInt32(((Identifier)register).Value);
                                else if (Helper.IsNumericInt(register))
                                    TmpReg = Convert.ToInt32(register);
                                scopes.Push(TmpAk | TmpReg);
                                break;
                            }

                        case Opcodes.opAnd:
                            {
                                int TmpAk = 0;
                                if (akkumulator.GetType() == typeof(Identifier))
                                    TmpAk = Convert.ToInt32(((Identifier)akkumulator).Value);
                                else if (Helper.IsNumericInt(akkumulator))
                                    TmpAk = Convert.ToInt32(akkumulator);
                                int TmpReg = 0;
                                if (register.GetType() == typeof(Identifier))
                                    TmpReg = Convert.ToInt32(((Identifier)register).Value);
                                else if (Helper.IsNumericInt(register))
                                    TmpReg = Convert.ToInt32(register);
                                scopes.Push(TmpAk & TmpReg);
                                break;
                            }

                        case Opcodes.opEq // =
                 :
                            {
                                string TmpAk = string.Empty;
                                if (akkumulator.GetType() == typeof(Identifier))
                                    TmpAk = Convert.ToString(((Identifier)akkumulator).Value);
                                else
                                    TmpAk = Convert.ToString(akkumulator);
                                string TmpReg = string.Empty;
                                if (register.GetType() == typeof(Identifier))
                                    TmpReg = Convert.ToString(((Identifier)register).Value);
                                else
                                    TmpReg = Convert.ToString(register);
                                scopes.Push(TmpAk.Equals(TmpReg));
                                break;
                            }

                        case Opcodes.opNotEq // <>
                 :
                            {
                                string TmpAk = string.Empty;
                                if (akkumulator.GetType() == typeof(Identifier))
                                    TmpAk = Convert.ToString(((Identifier)akkumulator).Value);
                                else
                                    TmpAk = Convert.ToString(akkumulator);
                                string TmpReg = string.Empty;
                                if (register.GetType() == typeof(Identifier))
                                    TmpReg = Convert.ToString(((Identifier)register).Value);
                                else if (!Helper.IsNumericInt(register))
                                    TmpReg = Convert.ToString(register);
                                scopes.Push(!TmpAk.Equals(TmpReg));
                                break;
                            }

                        case Opcodes.oplt // <
                 :
                            {
                                double TmpAk = 0.0D;
                                if (akkumulator.GetType() == typeof(Identifier))
                                    TmpAk = Convert.ToInt32(((Identifier)akkumulator).Value);
                                else if (Helper.IsNumericInt(akkumulator))
                                    TmpAk = Convert.ToInt32(akkumulator);
                                double TmpReg = 0.0D;
                                if (register.GetType() == typeof(Identifier))
                                    TmpReg = Convert.ToInt32(((Identifier)register).Value);
                                else if (Helper.IsNumericInt(register))
                                    TmpReg = Convert.ToInt32(register);
                                scopes.Push(TmpAk < TmpReg);
                                break;
                            }

                        case Opcodes.opLEq // <=
                 :
                            {
                                double TmpAk = 0.0D;
                                if (akkumulator.GetType() == typeof(Identifier))
                                    TmpAk = Convert.ToInt32(((Identifier)akkumulator).Value);
                                else if (Helper.IsNumericInt(akkumulator))
                                    TmpAk = Convert.ToInt32(akkumulator);
                                double TmpReg = 0.0D;
                                if (register.GetType() == typeof(Identifier))
                                    TmpReg = Convert.ToInt32(((Identifier)register).Value);
                                else if (Helper.IsNumericInt(register))
                                    TmpReg = Convert.ToInt32(register);
                                scopes.Push(TmpAk <= TmpReg);
                                break;
                            }

                        case Opcodes.opGt // >
                 :
                            {
                                double TmpAk = 0.0D;
                                if (akkumulator.GetType() == typeof(Identifier))
                                    TmpAk = Convert.ToInt32(((Identifier)akkumulator).Value);
                                else if (Helper.IsNumericInt(akkumulator))
                                    TmpAk = Convert.ToInt32(akkumulator);
                                double TmpReg = 0;
                                if (register.GetType() == typeof(Identifier))
                                    TmpReg = Convert.ToInt32(((Identifier)register).Value);
                                else if (Helper.IsNumericInt(register))
                                    TmpReg = Convert.ToInt32(register);
                                scopes.Push(TmpAk > TmpReg);
                                break;
                            }

                        case Opcodes.opGEq // >=
                 :
                            {
                                double TmpAk = 0.0D;
                                if (akkumulator.GetType() == typeof(Identifier))
                                    TmpAk = Convert.ToInt32(((Identifier)akkumulator).Value);
                                else if (Helper.IsNumericInt(akkumulator))
                                    TmpAk = Convert.ToInt32(akkumulator);
                                double TmpReg = 0.0D;
                                if (register.GetType() == typeof(Identifier))
                                    TmpReg = Convert.ToInt32(((Identifier)register).Value);
                                else if (Helper.IsNumericInt(register))
                                    TmpReg = Convert.ToInt32(register);
                                scopes.Push(TmpAk >= TmpReg);
                                break;
                            }
                    }
                }
                catch (Exception ex)
                {

                    running = false;
                    ErrorObject.Raise((int)InterpreterError.runErrors.errMath, "Code.Run", "Error during calculation (binary op " + operation.GetValue(0).ToString() + "): " + ex.HResult + "(" + ex.Message + ")", 0, 0, 0);
                }
            }


        }
        private void UnaryMathOperators(object[] operation)
        {
            var akkumulator = scopes.PopScopes().Value;

            try
            {
                switch ((Opcodes)operation.GetValue(0))
                {
                    case Opcodes.opNegate:
                        {
                            double number = Formathelper.FormatDoubleNumber(akkumulator.ToString());
                            scopes.Push(number * -1);
                            break;
                        }

                    case Opcodes.opNot:
                        {
                            var tmp = Convert.ToBoolean(akkumulator);
                            scopes.Push(!tmp);
                            break;
                        }

                    case Opcodes.opFactorial:
                        {
                            scopes.Push(Factorial(Convert.ToInt32(akkumulator)));
                            break;
                        }

                    case Opcodes.opSin:
                        {
                            double number = Formathelper.FormatDoubleNumber(akkumulator.ToString());
                            scopes.Push(System.Math.Sin(number));
                            break;
                        }

                    case Opcodes.opCos:
                        {
                            double number = Formathelper.FormatDoubleNumber(akkumulator.ToString());
                            scopes.Push(System.Math.Cos(number));
                            break;
                        }

                    case Opcodes.opTan:
                        {
                            double number = Formathelper.FormatDoubleNumber(akkumulator.ToString());
                            scopes.Push(System.Math.Tan(number));
                            break;
                        }

                    case Opcodes.opATan:
                        {
                            double number = Formathelper.FormatDoubleNumber(akkumulator.ToString());
                            scopes.Push(System.Math.Atan(number));
                            break;
                        }
                }
            }
            catch (Exception ex)
            {

                running = false;
                ErrorObject.Raise((int)InterpreterError.runErrors.errMath, "Code.Run", "Error during calculation (unary op " + operation.GetValue(0).ToString() + "): " + ex.HResult + " (" + ex.Message + ")", 0, 0, 0);
            }

        }

        public void Dispose()
        {
            ErrorObject = null;
        }
    }

}
