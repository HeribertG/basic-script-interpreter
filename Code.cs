using basic_script_interpreter.Macro.Process;
using System;
using System.Collections.ObjectModel;
using System.Globalization;

namespace basic_script_interpreter
{
  public class Code : IDisposable
  {

    #region Events
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

    public delegate void AssignEventHandler(string name, string value, ref bool accepted);

    public event RetrieveEventHandler Retrieve;

    public delegate void RetrieveEventHandler(string name, string value, bool accepted);

    public event MessageEventHandler Message;

    public delegate void MessageEventHandler(int type, string message);

    #endregion

    public interface IInputStream
    {
      IInputStream Connect(string connectString); // Objekt an Script-Code anbinden
      string GetNextChar(); // Nächstes Zeichen aus Code herauslesen


      // Um 1 Zeichen im Code zurückgehen
      // Wenn diese Funktion für eine bestimmte Quelle nicht realisierbar
      // ist, muss ein Fehler erzeugt werden: errGoBackPastStartOfSource
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

    public int Count
    {
      get
      {
        return code.Count;
      }
    }

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
    public bool AllowUi { get; set; }
    public int CodeTimeout { get; set; } = 120000; // 120 Sekunden default
    public bool Cancel { get; set; } //Bricht den Programablauf ab
    public bool Running { get; private set; } // wird noch code ausgeführt?
    internal int EndOfCodePc
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
      foreach (var item in code) { clone.CloneAdd(item); }


      for (int i = 0; i <= external.CloneCount() - 1; i++)
      {
        clone.ImportAdd(((Identifier)external.CloneItem(i)).name);
      }


      clone.ErrorObject = new InterpreterError();

      return clone;
    }
    internal void CloneAdd(object value)
    {
      code.Add(value);
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
      var tmp = (object[])code[index];
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

      scopes = new Scopes();
      scopes.PushScope(external);
      scopes.PushScope();

      int startTime = Environment.TickCount;
      Cancel = false;
      running = true;

      pc = 0;

      bool accepted;
      // bool continues;
      object xPos;
      object defaultRenamed;
      object yPos;


      while ((pc < code.Count - 1) & running)
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
              string name;
              try
              {

                var tmp = operation.GetValue(1);
                if (tmp.GetType() == typeof(Identifier)) { name = ((Identifier)tmp).value.ToString(); }
                else { name = tmp.ToString(); }
                register = scopes.Retrieve(name);
              }
              catch (Exception)
              {
                // Variable nicht alloziert, also bei Host nachfragen
                accepted = false;
                Retrieve?.Invoke(operation.GetValue(1).ToString(), register.ToString(), accepted);
                if (!accepted)
                {
                  // der Host weiss nichts von der Var. Implizit anlegen tun wir
                  // sie aber nicht, da sie hier auf sie sofort lesend zugegriffen
                  // würde

                  running = false;
                  ErrorObject.Raise((int)InterpreterError.runErrors.errUnknownVar, "Code.Run", "Unknown variable '" + operation.GetValue(1) + "'", 0, 0, 0);
                }
              }

              if (register == null)
              {
                running = false;
                ErrorObject.Raise((int)InterpreterError.runErrors.errUninitializedVar, "Code.Run", "Variable '" + operation.GetValue(1) + "' not hasn´t been assigned a Value yet", 0, 0, 0);
              }
              else
              {
                if (register.GetType() == typeof(Identifier)) { scopes.Push(((Identifier)register).value); }
                else { scopes.Push(register.ToString()); }


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
              object result;
              register = scopes.Pop(Convert.ToInt32(operation.GetValue(1)));
              if (register is Identifier) { result = ((Identifier)register).value; } else { result = register; }
              scopes.Push(result);
              break;
            }
          // Wert auf dem Stack einer Variablen zuweisen
          case Opcodes.opAssign:
            {
              // Parameter:    Variablenname
              // Stack:        der zuzuweisende Wert

              object result;
              register = scopes.Pop();
              if (register is Identifier) { result = ((Identifier)register).value; } else { result = register; }
              if (!scopes.Assign(operation.GetValue(1).ToString(), result)) ;
              {
                // Variable nicht alloziert, also Host anbieten
                accepted = false;


                Assign?.Invoke(operation.GetValue(1).ToString(), result.ToString(), ref accepted);
                if (!accepted)
                  // Host hat nicht mit Var am Hut, dann legen wir
                  // sie eben selbst an
                  scopes.Allocate(operation.GetValue(1).ToString(), result.ToString());
              }

              break;
            }

          case Opcodes.opAdd:
          case Opcodes.opSub:
          case Opcodes.opMultiplication:
          case Opcodes.opDivision:
          case Opcodes.opDiv:
          case Opcodes.opMod:
          case Opcodes.opPower:
          case Opcodes.opStringConcat:
          case Opcodes.opOr:
          case Opcodes.opAnd:
          case Opcodes.opEq:
          case Opcodes.opNotEq:
          case Opcodes.oplt:
          case Opcodes.opLEq:
          case Opcodes.opGt:
          case Opcodes.opGEq:
            BinaryMathOperators(operation, akkumulator, register);
            break;

          case Opcodes.opNegate:
          case Opcodes.opNot:
          case Opcodes.opFactorial:
          case Opcodes.opSin:
          case Opcodes.opCos:
          case Opcodes.opTan:
          case Opcodes.opATan:
            UnaryMathOperators(operation);
            break;

          case Opcodes.opDebugPrint:
            {

              string msg = string.Empty;


              register = scopes.PopScopes();
              if (register != null) { msg = ((Identifier)register).value.ToString(); }

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
                int type = 0;
                register = scopes.PopScopes(); // Message
                akkumulator = scopes.PopScopes().value; // Type
                if (register is Identifier)
                {
                  if (register != null)
                  {
                    if (register.GetType() == typeof(Identifier)) { msg = ((Identifier)register).value.ToString(); }
                    else { msg = register.ToString(); }

                  }
                }


                if (akkumulator != null)
                {
                  if (akkumulator.GetType() == typeof(Identifier)) { type = Convert.ToInt32(((Identifier)akkumulator).value); }
                  else { type = Convert.ToInt32(akkumulator); }

                }


                Message?.Invoke(type, msg);
              }
              catch (Exception)
              {
                Message?.Invoke(-1, string.Empty);
              }

              break;
            }

          case Opcodes.opMsgbox:
            {
              if (!AllowUi)
              {
                running = false;
                ErrorObject.Raise((int)InterpreterError.runErrors.errNoUIallowed, "Code.Run", "MsgBox-Statement cannot be executed when no UI-elements are allowed", 0, 0, 0);
              }

              register = scopes.PopScopes().value; // Title
              akkumulator = scopes.PopScopes().value; // Buttons

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
              if (!AllowUi)
              {
                running = false;
                ErrorObject.Raise((int)InterpreterError.runErrors.errNoUIallowed, "Code.Run", "Inputbox-Statement cannot be executed when no UI-elements are allowed", 0, 0, 0);
              }

              yPos = scopes.PopScopes().value;
              xPos = scopes.PopScopes().value;
              defaultRenamed = scopes.PopScopes().value;
              register = scopes.PopScopes().value;
              akkumulator = scopes.PopScopes().value;

              try
              {
                // TODO:InputBox
                //string Anwert = Microsoft.VisualBasic.Interaction.InputBox(Akkumulator.ToString(), Register.ToString(), defaultRenamed.ToString(), Convert.ToInt32(xPos), Convert.ToInt32(yPos));
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
              akkumulator = scopes.PopScopes().value;
              if (Convert.ToBoolean(akkumulator))
                pc = Convert.ToInt32(operation.GetValue(1)) - 1;
              break;
            }

          case Opcodes.opJumpFalse:
            {
              akkumulator = scopes.PopScopes().value;
              if (!Convert.ToBoolean(akkumulator))
                pc = Convert.ToInt32(operation.GetValue(1)) - 1;
              break;
            }

          case Opcodes.opJumpPop:
            {
              pc = Convert.ToInt32(scopes.PopScopes().value) - 1;
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
              pc = Convert.ToInt32(Convert.ToDouble(scopes.Retrieve("~RETURNADDR").value, CultureInfo.InvariantCulture) - 1);
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
        var tickPassed = (Environment.TickCount - startTime);
        if (CodeTimeout > 0 && tickPassed >= CodeTimeout)
        {

          running = false;
          ErrorObject.Raise((int)InterpreterError.runErrors.errTimedOut, "Code.Run", "Timeout reached: code execution has been aborted", 0, 0, 0);

        }
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
    private void BinaryMathOperators(object[] operation, object accumulator, object register)
    {

      register = scopes.PopScopes();
      accumulator = scopes.PopScopes();

      if (register != null && accumulator != null)
      {
        try
        {
          switch ((Opcodes)operation.GetValue(0))
          {
            case Opcodes.opAdd:
              {
                double tmpAk = 0.0D;
                if (accumulator.GetType() == typeof(Identifier))

                  tmpAk = Convert.ToDouble(((Identifier)accumulator).value, CultureInfo.InvariantCulture);
                else if (Helper.IsNumericDouble(accumulator))
                  tmpAk = Convert.ToDouble(accumulator, CultureInfo.InvariantCulture);
                double tmpReg = 0.0D;
                if (register.GetType() == typeof(Identifier))
                  tmpReg = Convert.ToDouble(((Identifier)register).value, CultureInfo.InvariantCulture);
                else if (Helper.IsNumericDouble(register))
                  tmpReg = Convert.ToDouble(register, CultureInfo.InvariantCulture);
                scopes.Push(tmpAk + tmpReg);
                break;
              }

            case Opcodes.opSub:
              {
                var tmpAk = 0.0D;
                if (accumulator.GetType() == typeof(Identifier))
                  tmpAk = Convert.ToDouble(((Identifier)accumulator).value, CultureInfo.InvariantCulture);
                else if (Helper.IsNumericDouble(accumulator))
                  tmpAk = Convert.ToDouble(accumulator, CultureInfo.InvariantCulture);
                double tmpReg = 0.0D;
                if (register.GetType() == typeof(Identifier))
                  tmpReg = Convert.ToDouble(((Identifier)register).value, CultureInfo.InvariantCulture);
                else if (Helper.IsNumericDouble(register))
                  tmpReg = Convert.ToDouble(register, CultureInfo.InvariantCulture);
                scopes.Push(tmpAk - tmpReg);
                break;
              }

            case Opcodes.opMultiplication:
              {
                double tmpAk = 0.0D;
                if (accumulator.GetType() == typeof(Identifier))
                  tmpAk = Convert.ToDouble(((Identifier)accumulator).value, CultureInfo.InvariantCulture);
                else if (Helper.IsNumericDouble(accumulator))
                  tmpAk = Convert.ToDouble(accumulator, CultureInfo.InvariantCulture);
                double tmpReg = 0.0D;
                if (register.GetType() == typeof(Identifier))
                  tmpReg = Convert.ToDouble((((Identifier)register)).value, CultureInfo.InvariantCulture);
                else if (Helper.IsNumericDouble(register))
                  tmpReg = Convert.ToDouble(register, CultureInfo.InvariantCulture);
                scopes.Push(Convert.ToDouble(tmpAk, CultureInfo.InvariantCulture) * Convert.ToDouble(tmpReg, CultureInfo.InvariantCulture));
                break;
              }

            case Opcodes.opDivision:
              {
                double tmpAk = 0.0D;
                if (accumulator.GetType() == typeof(Identifier))
                  tmpAk = Convert.ToDouble(((Identifier)accumulator).value, CultureInfo.InvariantCulture);
                else if (Helper.IsNumericDouble(accumulator))
                  tmpAk = Convert.ToDouble(accumulator, CultureInfo.InvariantCulture);
                double tmpReg = 0.0D;
                if (register.GetType() == typeof(Identifier))
                  tmpReg = Convert.ToDouble(((Identifier)register).value, CultureInfo.InvariantCulture);
                else if (Helper.IsNumericDouble(register))
                  tmpReg = Convert.ToDouble(register, CultureInfo.InvariantCulture);
                scopes.Push(Convert.ToDouble(tmpAk, CultureInfo.InvariantCulture) / Convert.ToDouble(tmpReg, CultureInfo.InvariantCulture));
                break;
              }

            case Opcodes.opDiv:
              {
                double tmpAk = 0.0D;
                if (accumulator.GetType() == typeof(Identifier))
                  tmpAk = Convert.ToDouble(((Identifier)accumulator).value, CultureInfo.InvariantCulture);
                else if (Helper.IsNumericDouble(accumulator))
                  tmpAk = Convert.ToDouble(accumulator, CultureInfo.InvariantCulture);
                double tmpReg = 0.0D;
                if (register.GetType() == typeof(Identifier))
                  tmpReg = Convert.ToDouble(((Identifier)register).value, CultureInfo.InvariantCulture);
                else if (Helper.IsNumericDouble(register))
                  tmpReg = Convert.ToDouble(register, CultureInfo.InvariantCulture);
                scopes.Push(Convert.ToInt32(tmpAk, CultureInfo.InvariantCulture) / Convert.ToInt32(tmpReg, CultureInfo.InvariantCulture));
                break;
              }

            case Opcodes.opMod:
              {
                double tmpAk = 0.0D;
                if (accumulator.GetType() == typeof(Identifier))
                  tmpAk = Convert.ToDouble(((Identifier)accumulator).value, CultureInfo.InvariantCulture);
                else if (Helper.IsNumericDouble(accumulator))
                  tmpAk = Convert.ToDouble(accumulator, CultureInfo.InvariantCulture);
                double tmpReg = 0.0D;
                if (register.GetType() == typeof(Identifier))
                  tmpReg = Convert.ToDouble(((Identifier)register).value, CultureInfo.InvariantCulture);
                else if (Helper.IsNumericDouble(register))
                  tmpReg = Convert.ToDouble(register, CultureInfo.InvariantCulture);
                scopes.Push(Convert.ToDouble(tmpAk, CultureInfo.InvariantCulture) % Convert.ToDouble(tmpReg, CultureInfo.InvariantCulture));
                break;
              }

            case Opcodes.opPower:
              {
                double tmpAk = 0.0D;
                if (accumulator.GetType() == typeof(Identifier))
                  tmpAk = Convert.ToDouble(((Identifier)accumulator).value, CultureInfo.InvariantCulture);
                else if (Helper.IsNumericDouble(accumulator))
                  tmpAk = Convert.ToDouble(accumulator, CultureInfo.InvariantCulture);
                double tmpReg = 0.0D;
                if (register.GetType() == typeof(Identifier))
                  tmpReg = Convert.ToDouble(((Identifier)register).value, CultureInfo.InvariantCulture);
                else if (Helper.IsNumericDouble(register))
                  tmpReg = Convert.ToDouble(register, CultureInfo.InvariantCulture);
                scopes.Push(Math.Pow(Convert.ToDouble(tmpAk, CultureInfo.InvariantCulture), Convert.ToDouble(tmpReg, CultureInfo.InvariantCulture)));
                break;
              }

            case Opcodes.opStringConcat:
              {
                string tmpAk;
                if (accumulator.GetType() == typeof(Identifier))
                  tmpAk = Convert.ToString(((Identifier)accumulator).value);
                else
                  tmpAk = Convert.ToString(accumulator);
                string tmpReg;
                if (register.GetType() == typeof(Identifier))
                  tmpReg = Convert.ToString(((Identifier)register).value);
                else
                  tmpReg = Convert.ToString(register);
                scopes.Push(tmpAk + tmpReg);
                break;
              }

            case Opcodes.opOr:
              {
                int tmpAk = 0;
                if (accumulator.GetType() == typeof(Identifier))
                  tmpAk = Convert.ToInt32(((Identifier)accumulator).value);
                else if (Helper.IsNumericInt(accumulator))
                  tmpAk = Convert.ToInt32(accumulator);
                int tmpReg = 0;
                if (register.GetType() == typeof(Identifier))
                  tmpReg = Convert.ToInt32(((Identifier)register).value);
                else if (Helper.IsNumericInt(register))
                  tmpReg = Convert.ToInt32(register);
                scopes.Push(tmpAk | tmpReg);
                break;
              }

            case Opcodes.opAnd:
              {
                int tmpAk = 0;
                if (accumulator.GetType() == typeof(Identifier))
                  tmpAk = Convert.ToInt32(((Identifier)accumulator).value);
                else if (Helper.IsNumericInt(accumulator))
                  tmpAk = Convert.ToInt32(accumulator);
                int tmpReg = 0;
                if (register.GetType() == typeof(Identifier))
                  tmpReg = Convert.ToInt32(((Identifier)register).value);
                else if (Helper.IsNumericInt(register))
                  tmpReg = Convert.ToInt32(register);
                scopes.Push(tmpAk & tmpReg);
                break;
              }

            case Opcodes.opEq // =
     :
              {
                string tmpAk;
                if (accumulator.GetType() == typeof(Identifier))
                  tmpAk = Convert.ToString(((Identifier)accumulator).value);
                else
                  tmpAk = Convert.ToString(accumulator);
                string tmpReg;
                if (register.GetType() == typeof(Identifier))
                  tmpReg = Convert.ToString(((Identifier)register).value);
                else
                  tmpReg = Convert.ToString(register);
                scopes.Push(tmpAk.Equals(tmpReg));
                break;
              }

            case Opcodes.opNotEq // <>
     :
              {
                string tmpAk;
                if (accumulator.GetType() == typeof(Identifier))
                  tmpAk = Convert.ToString(((Identifier)accumulator).value);
                else
                  tmpAk = Convert.ToString(accumulator);
                string tmpReg = string.Empty;
                if (register.GetType() == typeof(Identifier))
                  tmpReg = Convert.ToString(((Identifier)register).value);
                else if (!Helper.IsNumericInt(register))
                  tmpReg = Convert.ToString(register);
                scopes.Push(!tmpAk.Equals(tmpReg));
                break;
              }

            case Opcodes.oplt // <
     :
              {
                double tmpAk = 0.0D;
                if (accumulator.GetType() == typeof(Identifier))
                  tmpAk = Convert.ToInt32(((Identifier)accumulator).value);
                else if (Helper.IsNumericInt(accumulator))
                  tmpAk = Convert.ToInt32(accumulator);
                double tmpReg = 0.0D;
                if (register.GetType() == typeof(Identifier))
                  tmpReg = Convert.ToInt32(((Identifier)register).value);
                else if (Helper.IsNumericInt(register))
                  tmpReg = Convert.ToInt32(register);
                scopes.Push(tmpAk < tmpReg);
                break;
              }

            case Opcodes.opLEq // <=
     :
              {
                double tmpAk = 0.0D;
                if (accumulator.GetType() == typeof(Identifier))
                  tmpAk = Convert.ToInt32(((Identifier)accumulator).value);
                else if (Helper.IsNumericInt(accumulator))
                  tmpAk = Convert.ToInt32(accumulator);
                double tmpReg = 0.0D;
                if (register.GetType() == typeof(Identifier))
                  tmpReg = Convert.ToInt32(((Identifier)register).value);
                else if (Helper.IsNumericInt(register))
                  tmpReg = Convert.ToInt32(register);
                scopes.Push(tmpAk <= tmpReg);
                break;
              }

            case Opcodes.opGt // >
     :
              {
                double tmpAk = 0.0D;
                if (accumulator.GetType() == typeof(Identifier))
                  tmpAk = Convert.ToInt32(((Identifier)accumulator).value);
                else if (Helper.IsNumericInt(accumulator))
                  tmpAk = Convert.ToInt32(accumulator);
                double tmpReg = 0;
                if (register.GetType() == typeof(Identifier))
                  tmpReg = Convert.ToInt32(((Identifier)register).value);
                else if (Helper.IsNumericInt(register))
                  tmpReg = Convert.ToInt32(register);
                scopes.Push(tmpAk > tmpReg);
                break;
              }

            case Opcodes.opGEq // >=
     :
              {
                double tmpAk = 0.0D;
                if (accumulator.GetType() == typeof(Identifier))
                  tmpAk = Convert.ToInt32(((Identifier)accumulator).value);
                else if (Helper.IsNumericInt(accumulator))
                  tmpAk = Convert.ToInt32(accumulator);
                double tmpReg = 0.0D;
                if (register.GetType() == typeof(Identifier))
                  tmpReg = Convert.ToInt32(((Identifier)register).value);
                else if (Helper.IsNumericInt(register))
                  tmpReg = Convert.ToInt32(register);
                scopes.Push(tmpAk >= tmpReg);
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
      var akkumulator = scopes.PopScopes().value;

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
              scopes.Push(Math.Sin(number));
              break;
            }

          case Opcodes.opCos:
            {
              double number = Formathelper.FormatDoubleNumber(akkumulator.ToString());
              scopes.Push(Math.Cos(number));
              break;
            }

          case Opcodes.opTan:
            {
              double number = Formathelper.FormatDoubleNumber(akkumulator.ToString());
              scopes.Push(Math.Tan(number));
              break;
            }

          case Opcodes.opATan:
            {
              double number = Formathelper.FormatDoubleNumber(akkumulator.ToString());
              scopes.Push(Math.Atan(number));
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

    ~Code()
    {
      Dispose();
    }
  }

}
