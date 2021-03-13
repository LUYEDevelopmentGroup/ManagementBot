using System;
using System.Collections.Generic;
using System.Text;
//using Jint;

namespace tech.msgp.groupmanager.Code.ScriptHandler
{
    class AdminJScriptHandler
    {
        //public static Engine JsEngine;
        public static void InitEngine()
        {
            return;
            /*
            JsEngine = new Engine((Options op) =>
            {
                op.TimeoutInterval(TimeSpan.FromMinutes(1));
                op.AllowClr();
                op.AllowClr(typeof(MainHolder).Assembly);
            });*/
        }

        public static string EvaluateJs(string code)
        {
            /*
            JsEngine?.Execute(code);
            return JsEngine?.GetCompletionValue().AsString();
        */
            return "";
        }

        public static void RunCode(string code)
        {
            //JsEngine?.Execute(code);
        }
    }
}
