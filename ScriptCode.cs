using Basic_Script_Interpreter;
using System;
using System.Collections.Generic;
using System.Text;

namespace basic_script_interpreter
{
    /// Wird gebraucht, um compilierte Code immer wieder zu gebrauchen ohne erneute Compilierung
    /// Script enthält alle externen Variablennamen
    // Code ein Clone der  compilierten Code Klasse
    class ScriptCode
    {
        public List<string> Script { get; set; }
        public Code currentCode { get; set; }
    }
}
