using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static basic_script_interpreter.Identifier;


// Das jeweils zuoberst liegende Scope-Objekt dient dabei als
// normaler Programm-Stack.
//
// Variablen _symboltable (parser.cls) und _scopes (code.cls)
// sind Scopes-Objekte.
namespace basic_script_interpreter
{

    public class Scopes
    {
        private List<Scope> _scopes = new List<Scope>();


        public void Push(object s = null)
        {
            if (s == null)
                _scopes.Add(new Scope());
            else
            {
                var tmp = new Scope();
                tmp.Push(s);
                _scopes.Add(tmp);
            }
                
        }


        public  object PopScopes(int? index)
        {
            var ind= -1;
            if (index.HasValue) { ind = index.Value; }
            var scope = _scopes[_scopes.Count()-1];
            var result = scope.Pop(ind);
            
            return result;
        }

        public Identifier Allocate(string name, object value = null, Identifier.IdentifierTypes idType = Identifier.IdentifierTypes.idVariable)
        {
            return _scopes[_scopes.Count() - 1].Allocate(name, value, idType);
        }

        public void Assign(string name, object value)
        {
            var vari = getVariable(name);
            vari.value = value;
        }


        public Identifier Retrieve(string name)
        {
            return getVariable(name);
        }

        public bool Exists(string name, bool? inCurrentScopeOnly = false, IdentifierTypes? idType = IdentifierTypes.idNone)
        {

            int i, n;
            Identifier renamed;
            Scope s;
            var result = false;

            n = inCurrentScopeOnly == true ? this._scopes.Count() - 1 : 0;

            for (i = _scopes.Count() - 1; i >= n; i += -1)
            {
                s = _scopes[i];
                renamed = (Identifier)s.getVariable(name);

                if (renamed != null)
                {
                    if (renamed.name == name)
                    {
                        // Prüfen, ob gefundener Wert vom gewünschten Typ ist
                        if (idType == IdentifierTypes.idNone)
                        { result = true; }
                        else
                        {
                            if (idType == IdentifierTypes.idIsVariableOfFunction)
                            {
                                if (renamed.idType == IdentifierTypes.idVariable || renamed.idType == IdentifierTypes.idFunction)
                                {
                                    result = true;
                                }
                            }
                            else if (idType == IdentifierTypes.idSubOfFunction)
                            {
                                if (renamed.idType == IdentifierTypes.idSub || renamed.idType == IdentifierTypes.idFunction)
                                {
                                    result = true;
                                }
                            }
                            else if (idType == IdentifierTypes.idFunction)
                            {
                                if (renamed.idType == IdentifierTypes.idSub || renamed.idType == IdentifierTypes.idFunction)
                                {
                                    result = true;
                                }
                            }
                            else if (idType == IdentifierTypes.idFunction)
                            {
                                if (renamed.idType == IdentifierTypes.idFunction)
                                {
                                    result = true;
                                }
                            }
                            else if (idType == IdentifierTypes.idSub)
                            {
                                if (renamed.idType == IdentifierTypes.idSub)
                                {
                                    result = true;
                                }
                            }
                            else if (idType == IdentifierTypes.idVariable)
                            {
                                if (renamed.idType == IdentifierTypes.idVariable)
                                {
                                    result = true;
                                }
                            }
                        }

                    }

                }
            }

            return result;

        }

        public void Push(Identifier value)
        {
            try
            {
                if (value != null)
                {
                    Scope s = _scopes[_scopes.Count() - 1];
                    s.Push(value);
                }
            }
            catch (Exception ex)
            {
                Debug.Print("Scope.Push: " + ex.Message);
            }
        }

        public object Pop(int index = -1)
        {

            Scope s;
            object x;
            for (int i = _scopes.Count - 1; i >= 0; i += -1)
            {
                s = _scopes[i];
                x = s.Pop(index);
                if (x != null)
                    return x;
            }
            return null;
        }


        private Identifier getVariable(string name)
        {

            Scope s;
            Identifier x;
            try
            {
                for (int i = _scopes.Count - 1; i >= 0; i += -1)
                {
                    s = _scopes[i];
                    x = (Identifier)s.getVariable(name);
                    if (x != null)
                        return x;
                }
            }
            catch (Exception ex)
            {
                Debug.Print("Scope.zVariable: " + ex.Message);
            }

            return null;

        }

        ~Scopes()
        {
            _scopes.Clear();
        }
    }

}
