using Microsoft.VisualStudio.TestTools.UnitTesting;
using BiliApi.Auth;
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace BiliApi.Auth.Tests
{
    [TestClass()]
    public class QRLoginTests
    {
        static QRLogin obj;
        static QRLogin.LoginQRCode code;

        [TestMethod()]
        public void GenerateCode()
        {
            obj = new QRLogin();
            Debug.WriteLine("URL=" + obj.QRToken.ScanUrl);
            code = obj.QRToken;
        }

        [TestMethod()]
        public void Login()
        {
            obj = new QRLogin(code);
            obj.Login();
            Debug.WriteLine("COOKIECOUNT=" + obj.GetLoginCookies().Count);
        }
    }
}