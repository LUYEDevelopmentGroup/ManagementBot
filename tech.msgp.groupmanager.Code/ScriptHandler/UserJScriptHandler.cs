using System;
using System.Collections.Generic;
using System.Text;
//using Jint;

namespace tech.msgp.groupmanager.Code.ScriptHandler
{
    class UserJScriptHandler
    {
        //public static Engine JsEngine;
        public static void InitEngine()
        {
            return;
            /*JsEngine = new Engine((Options op) =>
            {
                op.TimeoutInterval(TimeSpan.FromSeconds(3));
            });*/
        }

        public static string EvaluateJs(string code)
        {
            return "";
            /*
            JsEngine?.Execute(code);
            return JsEngine?.GetCompletionValue().AsString();
        */
        }

        public static void RunCode(string code)
        {
            /*
            JsEngine?.Execute(code);
        */
        }
    }
}
