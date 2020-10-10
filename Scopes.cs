using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static Basic_Script_Interpreter.Identifier;


// Das jeweils zuoberst liegende Scope-Objekt dient dabei als
// normaler Programm-Stack.
//
// Variablen _symboltable (SyntaxAnalyser.cls) und scopes (Code.cls)
// sind Scopes-Objekte.
namespace Basic_Script_Interpreter
{

    public class Scopes
    {
        private List<Scope> _scopes = new List<Scope>();


        public void PushScope(Scope s = null)
        {
            if (s == null)
                _scopes.Add(new Scope());
            else
            {
                _scopes.Add(s);
            }

        }


        public Identifier PopScopes(int? index = null)
        {
            var ind = -1;
            if (index.HasValue) { ind = index.Value; }
            var scope = _scopes[_scopes.Count() - 1];
            var result = scope.Pop(ind);

            return result;
        }

        public Identifier Allocate(string name, object value = null, Identifier.IdentifierTypes idType = Identifier.IdentifierTypes.idVariable)
        {
            return _scopes[_scopes.Count() - 1].Allocate(name, value, idType);
        }

        // Von oben nach unten alle Scopes durchgehen und dem
        // ersten benannten Wert mit dem übergebenen Namen den
        // Wert zuweisen.
        public void Assign(string name, object value)
        {
            var vari = getVariable(name);
            vari.Value = value;
        }

        // dito, jedoch Wert zurückliefern (als kompletten Identifier)
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
                renamed = (Identifier)s.GetVariable(name);

                if (renamed != null)
                {
                    if (renamed.Name == name)
                    {
                        // Prüfen, ob gefundener Wert vom gewünschten Typ ist
                        if (idType == IdentifierTypes.idNone)
                        { result = true; }
                        else
                        {
                            if (idType == IdentifierTypes.idIsVariableOfFunction)
                            {
                                if (renamed.IdType == IdentifierTypes.idVariable || renamed.IdType == IdentifierTypes.idFunction)
                                {
                                    result = true;
                                }
                            }
                            else if (idType == IdentifierTypes.idSubOfFunction)
                            {
                                if (renamed.IdType == IdentifierTypes.idSub || renamed.IdType == IdentifierTypes.idFunction)
                                {
                                    result = true;
                                }
                            }
                            else if (idType == IdentifierTypes.idFunction)
                            {
                                if (renamed.IdType == IdentifierTypes.idSub || renamed.IdType == IdentifierTypes.idFunction)
                                {
                                    result = true;
                                }
                            }
                            else if (idType == IdentifierTypes.idFunction)
                            {
                                if (renamed.IdType == IdentifierTypes.idFunction)
                                {
                                    result = true;
                                }
                            }
                            else if (idType == IdentifierTypes.idSub)
                            {
                                if (renamed.IdType == IdentifierTypes.idSub)
                                {
                                    result = true;
                                }
                            }
                            else if (idType == IdentifierTypes.idVariable)
                            {
                                if (renamed.IdType == IdentifierTypes.idVariable)
                                {
                                    result = true;
                                }
                            }
                        }

                    }
                    return result;

                }
            }

            return result;

        }

        public void Push(object value)
        {

            if (value != null)
            {
                Scope s = _scopes[_scopes.Count() - 1];
                var c = new Identifier();
                c.Value = value;
                s.Push(c);
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
                    x = (Identifier)s.GetVariable(name);
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
