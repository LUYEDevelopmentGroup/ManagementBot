using System;
using BiliApi;
using BiliApi.Auth;
using BiliApi.BiliPrivMessage;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            QRLogin login = new QRLogin();
            Console.WriteLine(login.QRToken.ScanUrl);
            login.Login();
            Console.WriteLine("CookieCount=" + login.Cookies.Count.ToString());
            Console.WriteLine();
            ThirdPartAPIs api = new ThirdPartAPIs(login.Cookies);
            var priv = PrivMessageSession.openSessionWith(415413197, api);
            priv.fetch();
            foreach(var msg in priv.messages)
            {
                Console.WriteLine(msg.Key.content);
            }
        }
    }
}
