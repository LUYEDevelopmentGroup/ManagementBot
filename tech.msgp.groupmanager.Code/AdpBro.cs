using Mirai.CSharp.HttpApi.Models.ChatMessages;
using MySqlX.XDevAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tech.msgp.groupmanager.Code.FunctionMods;

namespace tech.msgp.groupmanager.Code
{
    internal static class AdpBro
    {
        public const long CrewGroupN = 781858343;
        public static AdaptiveBroadcaster bro;
        public static bool cansend = false;
        public static bool locked = false;

        public static void Init(string template)
        {
            if (locked)
            {
                MainHolder.broadcaster.BroadcastToAdminGroup("[舰长群发模式]\n主互斥锁无法被锁定，一个群发任务可能正在运行。请等待运行完成或进入后台操作。");
                return;
            }
            bro = new AdaptiveBroadcaster(new Dictionary<string, string[]>(), template);
            MainHolder.broadcaster.BroadcastToAdminGroup("[舰长群发模式]\n当前模板：\n" + template + "\n将会从参数库中抽取参数填入模板。\n使用#qf_add指令配置参数库。");
            cansend = false;
        }

        public static void AddArg(string argname, string[] values)
        {
            if (locked)
            {
                MainHolder.broadcaster.BroadcastToAdminGroup("[舰长群发模式]\n主互斥锁无法被锁定，一个群发任务可能正在运行。请等待运行完成或进入后台操作。");
                return;
            }
            if (!bro.AdaptiveArgs.ContainsKey(argname)) bro.AdaptiveArgs.Add(argname, values);
            else bro.AdaptiveArgs[argname] = values;
            MainHolder.broadcaster.BroadcastToAdminGroup("[舰长群发模式]\n参数{" + argname + "}已设置，目前具有" + values.Length + "个对象。\n输入#qf_done查看群发效果。");
            cansend = false;
        }

        public static void TryFi()
        {
            if (locked)
            {
                MainHolder.broadcaster.BroadcastToAdminGroup("[舰长群发模式]\n主互斥锁无法被锁定，一个群发任务可能正在运行。请等待运行完成或进入后台操作。");
                return;
            }
            string[] msgs = bro.GetMessages(3);
            var qlist = MainHolder.session.GetGroupMemberListAsync(CrewGroupN).Result;
            foreach (var item in bro.AdaptiveArgs)
            {
                if (qlist.Length > item.Value.Count())
                {
                    MainHolder.broadcaster.BroadcastToAdminGroup("[舰长群发模式]\n警告：提供的参数库不足以向所有目标发送消息：\n参数{" + item.Key + "}有" + item.Value.Count() + "个对象，但总共有" + qlist.Length + "个发送目标。");
                    return;
                }
            }
            MainHolder.broadcaster.BroadcastToAdminGroup("[舰长群发模式]\n群发效果预览：\n----------\n" + msgs[0] + "\n----------\n" + msgs[1] + "\n----------\n" + msgs[2] + "\n----------\n群发程序已就绪，输入#qf_send开始群发。");
            cansend = true;
        }

        public static void Confirm()
        {
            if (locked)
            {
                MainHolder.broadcaster.BroadcastToAdminGroup("[舰长群发模式]\n主互斥锁无法被锁定，一个群发任务可能正在运行。请等待运行完成或进入后台操作。");
                return;
            }
            if (!cansend)
            {
                MainHolder.broadcaster.BroadcastToAdminGroup("[舰长群发模式]\n没有等待发送的群发队列。");
                return;
            }
            var qlist = MainHolder.session.GetGroupMemberListAsync(CrewGroupN).Result;
            string[] msgs = bro.GetMessages(qlist.Length);
            MainHolder.broadcaster.BroadcastToAdminGroup("[舰长群发模式]\n向" + qlist.Length + "个目标发送消息。\n预计将在" + (DateTime.Now + TimeSpan.FromSeconds(qlist.Length * 2.5)).ToString("yy-MM-dd HH:mm:ss") + "完成。\n此任务将作为低优先级后台任务执行，请耐心等待发送完成。");
            Task.Run(() =>
            {
                int errcnt = 0;
                locked = true;
                for (int i = 0; i < qlist.Length; i++)
                {
                    try
                    {
                        if (i % 10 == 0) Thread.Sleep(5000);
                        MainHolder.session.SendTempMessageAsync(qlist[i].Id, CrewGroupN, new PlainMessage[] { new PlainMessage(msgs[i]) });
                        var percentage = i * 100 / qlist.Length;
                        if (percentage % 20 == 0)
                        {
                            MainHolder.broadcaster.BroadcastToAdminGroup("[舰长群发模式]\n群发任务进行中，已向" + (i + 1) + "/" + qlist.Length + "(" + percentage + "%)个目标发送私信。");
                        }
                        errcnt = 0;
                    }
                    catch (Exception ex)
                    {
                        MainHolder.broadcaster.BroadcastToAdminGroup("[舰长群发模式]\n群发任务#" + i + "出现错误:" + ex.Message + "\n" + ex.StackTrace);
                        errcnt++;
                        if(errcnt > 5)
                        {
                            MainHolder.broadcaster.BroadcastToAdminGroup("[舰长群发模式]\n已连续发生超过5个错误，群发任务终止。请检查日志确定错误原因，然后重试。");
                            locked = false;
                        }
                    }
                    Thread.Sleep(2000);
                }
                MainHolder.broadcaster.BroadcastToAdminGroup("[舰长群发模式]\n群发任务结束，向" + qlist.Length + "个目标发送了私信。");
                locked = false;
            });
        }
    }
}
