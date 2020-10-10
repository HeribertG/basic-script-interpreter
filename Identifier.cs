
using System.Collections.Generic;

namespace Basic_Script_Interpreter
{
   
    // Identifier: Dient während Compile- und Laufzeit zur
    // Aufnahme der relevanten Daten für benannte Variablen,
    // Konstanten, Funktionen/Subs

    // Insbesondere werden der Identifier-Typ, sein Wert - und
    // bei Funktionen die Parameternamen gespeichert.

    // Identifier-Objekte sind immer Elemente von Scope-Objekten.

    public class Identifier
    {

        public enum IdentifierTypes
        {
            idIsVariableOfFunction = -2,
            idSubOfFunction = -1,
            idNone = 0,
            idConst = 1,
            idVariable = 2,
            idFunction = 4,
            idSub = 8
        }


        public string Name;
        public object Value;

        public IdentifierTypes IdType;

        public int Address; // Adresse der Funktion
     
        public List<object> FormalParameters; // nur bei Funktionen: Namen der formalen Parameter
    }

}
