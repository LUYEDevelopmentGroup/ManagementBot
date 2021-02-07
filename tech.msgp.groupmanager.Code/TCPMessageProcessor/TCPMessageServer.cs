using Mirai_CSharp.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using BiliApi;

namespace tech.msgp.groupmanager.Code.TCPMessageProcessor
{
    public class TCPMessageServer
    {
        public Socket tcp_server;
        public IPEndPoint ipep;

        public Dictionary<IPEndPoint, Thread> thread_pool;
        public List<Socket> mode2socklist;

        /// <summary>
        /// 将该值设为false可以结束连接循环和消息循环
        /// </summary>
        public bool serverthread_flag = true;

        public TCPMessageServer(int port)
        {
            tcp_server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPAddress ip = IPAddress.Any;
            ipep = new IPEndPoint(ip, port);
            thread_pool = new Dictionary<IPEndPoint, Thread>();
            mode2socklist = new List<Socket>();
        }

        /// <summary>
        /// 启动服务器
        /// 警告：该方法阻塞当前线程。
        /// </summary>
        /// <returns>正常结束返回true，发生异常导致消息循环停止则返回false</returns>
        public bool init_server_sync()
        {
            try
            {
                tcp_server.Bind(ipep);
                tcp_server.Listen(5);
                MainHolder.Logger.Debug("RMTAPI", "连接点(ConnectPoint)服务启动");
                while (serverthread_flag)
                {
                    Socket msg_socket = tcp_server.Accept();
                    thread_pool.Add((IPEndPoint)msg_socket.RemoteEndPoint, new Thread(new ThreadStart(() => { _thread_message_process(msg_socket); })));
                    thread_pool[(IPEndPoint)msg_socket.RemoteEndPoint].Start();
                }
                MainHolder.Logger.Debug("RMTAPI", "连接点(ConnectPoint)服务被FLAG关闭");
                return true;
            }
            catch (Exception err)
            {
                MainHolder.Logger.Warning("RMTAPI", "连接点(ConnectPoint)服务异常结束");
                MainHolder.Logger.Warning("RMTAPI", err.Message + err.StackTrace);
                return false;
            }
        }

        /// <summary>
        /// 启动服务器
        /// 机器人会自动开启新的线程，不会阻塞当前线程
        /// </summary>
        /// <returns>是否成功创建了新线程</returns>
        public bool init_server_async()
        {
            try
            {
                new Thread(new ThreadStart(() =>
                {
                    init_server_sync();
                })).Start();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 单个客户端的消息循环。该方法通常不应被外部调用
        /// 警告：该方法阻塞当前线程。
        /// </summary>
        /// <param name="msg_sock"></param>
        public void _thread_message_process(Socket msg_sock)
        {
            try
            {
                int type = 0;
                string name = "UNKNOW";
                //Dictionary<long, GroupMemberInfo> infos = new Dictionary<long, GroupMemberInfo>();
                Dictionary<long, string> qqname_index = new Dictionary<long, string>();
                Dictionary<long, string> qqgname_index = new Dictionary<long, string>();
                List<int> uid_tobebound = new List<int>();
                MainHolder.Logger.Warning("RMTAPI", "新连接");
                //MainHolder.broadcaster.broadcastToAdminGroup("<DEBUG>[第三方连结API]\n一个新的节点试图连接，开启子MessageLoop");
                while (serverthread_flag)
                {
                    //客户端连接服务器成功后，服务器接收客户端发送的消息
                    byte[] buffer = new byte[1024];
                    //实际接收到的有效字节数
                    int len = msg_sock.Receive(buffer, buffer.Length, SocketFlags.None);
                    if (len == 0)
                    {
                        continue;
                    }
                    string str = Encoding.UTF8.GetString(buffer, 0, len);
                    string[] st = str.Split('^');
                    if (st.Length < 2)
                    {
                        continue;
                    }

                    if (type == 0)
                    {
                        switch (st[0])
                        {
                            case "M":
                                MainHolder.broadcaster.BroadcastToAdminGroup(str.Substring(2));
                                msg_sock.Send(Encoding.UTF8.GetBytes("REPLY^OK"));
                                break;
                            case "A":
                                MainHolder.broadcaster.BroadcastToAllGroup(str.Substring(2));
                                msg_sock.Send(Encoding.UTF8.GetBytes("REPLY^OK"));
                                break;
                            case "D":
                                if (MainHolder.useBiliRecFuncs) MainHolder.bilidmkproc.blr.sendDanmaku(str.Substring(2));
                                msg_sock.Send(Encoding.UTF8.GetBytes("REPLY^OK"));
                                break;
                            case "E":
                                MainHolder.broadcaster.SendToAnEgg(str.Substring(2));
                                msg_sock.Send(Encoding.UTF8.GetBytes("REPLY^OK"));
                                break;
                            case "INIT":
                                name = str.Substring(5);
                                //MainHolder.broadcaster.broadcastToAdminGroup("[第三方连结API]\n一个节点通过了Challenge，已对其开放消息广播API\n应用名称：" + name);
                                msg_sock.Send(Encoding.UTF8.GetBytes("REPLY^OK"));
                                break;
                            case "CREWE":
                                type = 1;
                                msg_sock.Send(Encoding.UTF8.GetBytes("REPLY^MODE_SWITCHED_1"));
                                break;
                            case "DANMAKU":
                                type = 2;
                                mode2socklist.Add(msg_sock);
                                msg_sock.Send(Encoding.UTF8.GetBytes("REPLY^MODE_SWITCHED_2#_MODE_JSON"));
                                break;
                            default:
                                MainHolder.broadcaster.BroadcastToAdminGroup("[第三方连结API]\n接收到不能被识别的远程消息：\n" + str);
                                msg_sock.Send(Encoding.UTF8.GetBytes("REPLY^UNKNOW_MESSAGE_TYPE"));
                                break;
                        }
                    }
                    else if (type == 1)//舰长群UID绑定模式
                    {
                        switch (st[0])
                        {
                            case "I":
                                if (qqname_index.Count < 1 || qqgname_index.Count < 1)
                                {//初始化舰长群数据
                                    qqname_index.Clear();
                                    qqgname_index.Clear();
                                    Dictionary<int, long> l = DataBase.me.listCrewBound();
                                    List<long> g = DataBase.me.getCrewGroup();
                                    foreach (long gg in g)
                                    {
                                        IGroupMemberInfo[] members = MainHolder.session.GetGroupMemberListAsync(gg).Result;//抓群成员
                                        foreach (IGroupMemberInfo ginfo in members)
                                        {
                                            if (!DataBase.me.isUserBoundedUID(ginfo.Id))
                                            {
                                                qqname_index.Add(ginfo.Id, ginfo.Name);
                                                qqgname_index.Add(ginfo.Id, ginfo.Name);
                                            }
                                        }
                                    }
                                }
                                if (uid_tobebound.Count < 1)
                                {
                                    uid_tobebound = DataBase.me.listCrewunBoundUID();
                                }
                                msg_sock.Send(Encoding.UTF8.GetBytes("REPLY^OK"));
                                break;
                            case "N"://下一个需操作的用户
                                {
                                    foreach (int uid in uid_tobebound)
                                    {
                                        BiliUser biuser = new BiliUser(uid, MainHolder.biliapi);
                                        msg_sock.Send(Encoding.UTF8.GetBytes("BINFO^" + uid + "^SOF@" + biuser.name + "^EOF@"));
                                        uid_tobebound.Remove(uid);
                                        break;
                                    }
                                }
                                break;
                            case "S"://QQ名片和名中关键字查找
                                {
                                    Dictionary<long, string> hits = new Dictionary<long, string>();
                                    foreach (KeyValuePair<long, string> item in qqname_index)
                                    {
                                        if (item.Value.IndexOf(st[1]) > -1 && !hits.ContainsKey(item.Key))
                                        {
                                            hits.Add(item.Key, item.Value);
                                        }
                                    }
                                    foreach (KeyValuePair<long, string> item in qqgname_index)
                                    {
                                        if (item.Value.IndexOf(st[1]) > -1 && !hits.ContainsKey(item.Key))
                                        {
                                            hits.Add(item.Key, item.Value);
                                        }
                                    }
                                    msg_sock.Send(Encoding.UTF8.GetBytes("KEEPALIVE^SOF@"));
                                    foreach (KeyValuePair<long, string> item in hits)
                                    {
                                        len = msg_sock.Receive(buffer, buffer.Length, SocketFlags.None);
                                        string rpl = Encoding.UTF8.GetString(buffer, 0, len);//等待询问
                                        if (rpl == "?")
                                        {
                                            msg_sock.Send(Encoding.UTF8.GetBytes(item.Key + "^" + item.Value));
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }
                                    Encoding.UTF8.GetString(buffer, 0, len);//等待询问
                                    msg_sock.Send(Encoding.UTF8.GetBytes("KEEPALIVE^EOF@"));
                                }
                                break;
                            case "B"://UID和QQ绑定
                                if (DataBase.me.boundBiliWithQQ(long.Parse(st[1]), long.Parse(st[2])))
                                {
                                    BiliUser biuser = new BiliUser(int.Parse(st[1]), MainHolder.biliapi);
                                    MainHolder.broadcaster.SendToQQ(long.Parse(st[2]), "管理员手动将您的QQ号与B站账号\"" + biuser.name + "\"绑定。如有错误，请联系管理解决。");
                                    msg_sock.Send(Encoding.UTF8.GetBytes("REPLY^OK"));
                                }
                                else
                                {
                                    msg_sock.Send(Encoding.UTF8.GetBytes("ERROR^FAIL"));
                                }

                                break;
                            case "Q"://退出模式1
                                type = -1;
                                msg_sock.Send(Encoding.UTF8.GetBytes("CMD^RECONN"));
                                msg_sock.Close();
                                break;
                            default:
                                MainHolder.broadcaster.BroadcastToAdminGroup("[第三方连结API]\n(type=1)接收到不能被识别的远程消息：\n" + str);
                                msg_sock.Send(Encoding.UTF8.GetBytes("REPLY^UNKNOW_MESSAGE_TYPE"));
                                break;
                        }
                    }
                    else if (type == 2)
                    {
                        switch (st[0])
                        {
                            case "BAN":
                                MainHolder.bilidmkproc.blr.manage.banUID(int.Parse(st[1]), int.Parse(st[2]));
                                msg_sock.Send(Encoding.UTF8.GetBytes("REPLY^OK"));
                                break;
                            case "DEBAN":
                                MainHolder.bilidmkproc.blr.manage.debanBID(int.Parse(st[1]));
                                msg_sock.Send(Encoding.UTF8.GetBytes("REPLY^OK"));
                                break;
                            case "SEND":
                                if (MainHolder.useBiliRecFuncs) MainHolder.bilidmkproc.blr.sendDanmaku(st[1]);
                                msg_sock.Send(Encoding.UTF8.GetBytes("REPLY^OK"));
                                break;
                        }
                    }
                }
                MainHolder.broadcaster.BroadcastToAdminGroup("<DEBUG>[第三方连结API]\n\"" + name + "\"对应的一个子MessageLoop结束");
            }
            catch (Exception err)
            {
                MainHolder.Logger.Warning("RMTAPI", "连接点(ConnectPoint)服务的一个消息循环异常结束");
                MainHolder.Logger.Warning("RMTAPI", err.Message + err.StackTrace);
            }
        }

        public void sendDanmakuToCli(string json)
        {
            MainHolder.pool.submitWorkload(() =>//提交发送任务到任务池
            {
                try
                {
                    List<Socket> fails = new List<Socket>();
                    foreach (Socket s in mode2socklist)
                    {
                        try
                        {
                            s.Send(Encoding.UTF8.GetBytes(json));
                        }
                        catch
                        {
                            fails.Add(s);
                        }
                    }
                    foreach (Socket s in fails)
                    {
                        mode2socklist.Remove(s);
                    }
                }
                catch
                {

                }
            });
        }
    }
}
