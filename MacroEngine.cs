using basic_script_interpreter;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace basic_script_interpreter
{
  public class MacroEngine : IDisposable
  {
    //private Code code;
    private List<string> importList;
    private Dictionary<Guid, ScriptCode> codeCollection;
    private readonly List<ResultMessage> result = new List<ResultMessage>();

    // public event MessageEventHandler Message;

    // public delegate void MessageEventHandler(int type, string message);

    private string message;
    private int type;

    public MacroEngine()
    {
     
      code = new Code();
      importList = new List<string>();
      codeCollection = new Dictionary<Guid, ScriptCode>();

      AddEvent();
    }
    public dynamic Imports { get; set; }

    public Code code { get; set; }
    public string  DebungText { get; set; }

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

    public void ResetImports()
    {

      foreach (var item in importList)
      {
        SetImports(item);
      }
    }

    public void PrepareMacro(Guid id, string script)
    {


      if (codeCollection.ContainsKey(id))
      {
        var sc = codeCollection[id];

        importList = null;

        code = sc.currentCode.Clone();
        importList = sc.importList;
      }
      else
      {


        PrepareImports(script);

        code.Compile(script);

        ScriptCode sc = new ScriptCode();
        sc.currentCode = code;
        sc.importList = importList;
        sc.script = script;

        codeCollection.Add(id, sc);
      }
    }
    public List<ResultMessage> Run()
    {
      result.Clear();
      code.ErrorObject.Clear();
      code.Run();

      return result;
    }

    #region code_events

    private void AddEvent()
    {
      code.Message += Code_Message;
      code.DebugClear += Code_DebugClear;
      code.DebugHide += Code_DebugHide;
      code.DebugShow += Code_DebugShow;
      code.DebugPrint += Code_DebugPrint;
    }

    private void RemoveEvent()
    {
      code.Message -= Code_Message;
      code.DebugClear -= Code_DebugClear;
      code.DebugHide -= Code_DebugHide;
      code.DebugShow -= Code_DebugShow;
      code.DebugPrint -= Code_DebugPrint;

    }

    private void Code_Message(int type, string message)
    {
      if (message != "undefined")
      {

        var c = new ResultMessage()
        {
          Type = type,
          Message = message
        };

        result.Add(c);
      }
    }

   
    private void Code_DebugClear()
    {
      DebungText = string.Empty;
    }

    private void Code_DebugHide()
    {

    }

    private void Code_DebugShow()
    {

    }

    private void Code_DebugPrint(string msg)
    {
      DebungText += msg + "\n";
    }

    #endregion code_events

    private void PrepareImports(string script)
    {
      var i = 1;
      var endposition = 1;
      code.ImportClear();
      importList.Clear();

      script =RemoveDescription(script);
      while (i != -1)
      {
        i = script.IndexOf("Import");
        if (i < 0) { break; }


        endposition = script.IndexOf("\r\n", i + 1);
        if (endposition == -1)
          endposition = script.IndexOf("\r", i + 1);
        if (endposition == -1)
          endposition = script.IndexOf("\n", i + 1);

        if (endposition < i)
          break;

        string tmpstr = script.Substring(i, endposition - i + 1);
        script = script.Remove(i, endposition - i + 1);

        tmpstr = tmpstr.Replace("\r\n", string.Empty);
        tmpstr = tmpstr.Replace("\r", string.Empty);
        tmpstr = tmpstr.Replace("\n", string.Empty);
        tmpstr = tmpstr.Replace("Import", string.Empty);
        tmpstr = tmpstr.Trim();
        i = 0;

        SetImports(tmpstr);
      }


    }

    private string RemoveDescription(string script)
    {
      int endposition;
      var i = 1;
      while (i != -1)
      {
        i = script.IndexOf("'");
        if (i < 0) { break; }


        if (!string.IsNullOrEmpty(script))
        {
          endposition = script.IndexOf("\r\n", i + 1, StringComparison.Ordinal);
          if (endposition == -1)
            endposition = script.IndexOf("\r", i + 1, StringComparison.Ordinal);
          if (endposition == -1)
            endposition = script.IndexOf("\n", i + 1, StringComparison.Ordinal);

          if (endposition < i) { break; }


          script = script.Remove(i, endposition - i + 1);


          i = 0;

        }
      }


      return script;

    }
    private void SetImports(string key)
    {
      var res = ReadImports(key);
      code.ImportAdd(key, ReadImports(key), Identifier.IdentifierTypes.idVariable);
    }


    private object ReadImports(string key)
    {
      var tmp = (IDictionary<string, object>)Imports;

      if (tmp.ContainsKey(key)) { return  tmp[key]; }
      return null;
      
    }

    public void Dispose()
    {
      RemoveEvent();
    }
  }

  public class ResultMessage
  {
    public int Type { get; set; }
    public string Message { get; set; }
  }
}
