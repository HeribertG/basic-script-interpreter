using Basic_Script_Interpreter;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace basic_script_interpreter
{
    class MacroEngine :IDisposable
    {
        private Code code;
        private Dictionary<Guid, string> codeCollection = new Dictionary<Guid, string>();
        public event MessageEventHandler Message;

        public delegate void MessageEventHandler(int type, string message);

        public MacroEngine()
        {
            AddEvent();
        }

        private void AddEvent()
        {
            code.Message += Code_Message;

        }

        private void RemoveEvent()
        {
            code.Message -= Code_Message;

        }

        public Imports Imports { get; set; }

        public void ImportAdd()
        {
            code.ImportClear();

            Type t = Imports.GetType();
            PropertyInfo[] props = t.GetProperties();
           
            foreach (PropertyInfo prp in props)
            {
                object value = prp.GetValue(Imports, new object[] { });
                code.ImportAdd(prp.Name, value);
            }
        }

        public void ImportItem()
        {
            Type t = Imports.GetType();
            PropertyInfo[] props = t.GetProperties();

            foreach (PropertyInfo prp in props)
            {
                object value = prp.GetValue(Imports, new object[] { });
                code.ImportItem(prp.Name, value);
            }
        }

        public void Prepare(Guid id, string script)
        {
            code = null;
            code = new Code();

        }
        public void Run()
        {
            code.ErrorObject.Clear();
            code.Run();
        }

        private void Code_Message(int type, string message)
        {
            Message.Invoke(type, message);
        }

        public void Dispose()
        {
            RemoveEvent();
        }
    }
}
