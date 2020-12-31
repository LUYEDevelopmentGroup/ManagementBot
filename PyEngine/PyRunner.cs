using System;
using System.IO;
using System.Text;
using IronPython.Hosting;
using Microsoft.Scripting.Hosting;

namespace PyEngine
{
    public class PyRunner
    {
        ScriptEngine py;
        ScriptScope scope;
        StreamReceiver output;

        public PyRunner()
        {
            py = Python.CreateEngine();
            scope = py.CreateScope();
            MemoryStream ms = new MemoryStream();
            output = new StreamReceiver(ms);
            py.Runtime.IO.SetOutput(ms, output);
            py.Runtime.IO.SetErrorOutput(ms, output);
        }

        public string runPyCommand(string code)
        {
            try
            {
                var rr = py.CreateScriptSourceFromString(code);
                rr.Execute<object>(scope);
                var ret = output.ReadToEnd();
                return ret;
            }catch(Exception err)
            {
                return err.Message;
            }
        }
    }
}
