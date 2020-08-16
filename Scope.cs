using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;



///  Scope: Stackähnliche Datenstruktur, in der benannte Werte
/// (Variablen, Konstanten) und unbenannte Werte geführt werden.
///
///  Die benannten Werte werden immer am unteren Ende des Stacks
///  zusammengefaßt. Der Zugriff erfolgt über die Namen.
///
///  Unbenannt Werte liegen in LIFO-Manier oben auf dem Stack.
///  Der Zugriff erfolgt per Pop/Push - wie man es von einem
///  Stack erwartet.
///
///  Jedes Scope-Objekt realisiert einen eigenen Stack und
///  einen eigenen Namensraum, in welchem alle benannten Werte
///  unterschiedliche Namen haben müssen. Werte in unterschiedlichen
///  Scope-Objekten können gleich sein.
namespace basic_script_interpreter
{

    public class Scope
    {
        private Collection<Identifier> _variables = new Collection<Identifier>();


        public Identifier Allocate(string name, object value = null, Identifier.IdentifierTypes idType = Identifier.IdentifierTypes.idVariable)
        {
            Identifier id = new Identifier()
            {
                name = name,
                value = value,
                idType = idType
            };


            setVariable(id, name);

            return id;
        }


        public void Assign(string name, object value)
        {
            setVariable(value, name);
        }


        public object Retrieve(string name)
        {
            return getVariable(name);
        }


        public bool Exists(string name)
        {
            return getVariable(name) != null;
        }


        public object getVariable(string name)
        {
            try
            {
                return _variables.Where(x => ((Identifier)x).name == name).FirstOrDefault();

            }
            catch (Exception)
            {
                return null;
            }
        }

        public void setVariable(object value, string name)
        {
            // Benannten Wert löschen , damit es ersetzt wird
            if (this._variables.Count > 0)
            {
                var tmp = _variables.Where(x => ((Identifier)x).name == name).FirstOrDefault();
                if (tmp != null) { _variables.Remove(tmp); }
            }

            // Variablen immer am Anfang des Scopes zusammenhalten. Nach der letzten
            // Variablen kommen nur noch echte Stackwerte

            Identifier c = null;
            if (value is Identifier)
            {
                c = (Identifier)value;
            }
            else
            {
                c = new Identifier();
                c.name = name;
                c.value = value;
            }
         
            if (_variables.Count == 0) { _variables.Add(c); }
            else { _variables.Insert(0, c); }

        }


        public void Push(Identifier value)
        {
            _variables.Add(value);
        }


        ///  Holt den obersten unbenannten Wert vom Stack.
        ///  Wenn eine index übergeben wird, kann auch auf Stackwerte
        ///  direkt zugegriffen werden. In dem Fall werden sie nicht
        ///  gelöscht! Index: 0..n; 0=oberster Stackwert, 1=darunterliegender usw.
        public Identifier Pop(int index = -1)
        {
            Identifier pop = null;
            if (index < 0)
            {
                // Den obersten Stackwert vom Stack nehmen und zurückliefern
                // Die Stackwerte fangen nach der letzten benannten Variablen im Scope an
                try
                {
                    pop = _variables[_variables.Count() - 1];
                    _variables.Remove(pop);
                }
                catch (Exception ex)
                {
                    Debug.Print("Scope.Pop: " + ex.Message);
                }
            }
            else
                // Eine Stackwert vom Stacktop aus gezählt (0..n) zurückliefern, der Stack
                // bleibt aber wie er ist

                pop = _variables[_variables.Count() - index];

            return pop;
        }

        internal int CloneCount()
        {
            return _variables.Count();
        }

        public object CloneItem(int Index)
        {
            return _variables[Index];
        }
    }

}
