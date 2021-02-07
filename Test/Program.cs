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
        }
    }
}
