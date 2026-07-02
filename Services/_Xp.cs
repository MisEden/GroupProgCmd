using Base.Services;
using GroupProgCmd.Models;
using System.Diagnostics;
using System;

namespace GroupProgCmd.Services
{
    //project service
#pragma warning disable CA2211 // 非常數欄位不應可見
    public static class _Xp
    {
        //public const string SiteVer = "20201228f";     //for my.js/css
        //public static string MyVer = _Date.NowSecStr(); //for my.js/css
        //public const string LibVer = "20250501";       //for lib.js/css

        //public const string RoleAll = "_All";       //角色Id:所有人員, 與XpRole.Id一致
        //public const string RoleHrMgr = "HrMgr";    //角色Id:Hr主管, 與XpRole.Id一致

        //public static string NoImagePath = _Fun.DirRoot + "/wwwroot/image/noImage.jpg";

        //dir
        public static string DirTpl = _Fun.DirRoot + "_template/";
        //public static string DirUpload = _Fun.DirRoot + "_upload/";

        public static string DirBaseUpload = _Fun.Dir("_upload");
        //public static string DirHrInsGov = DirUpload("HrInsGov");

        public static MyConfigDto Config = null!;

        /*
        public static string GetTplPath(string fileName, bool hasLocale)
        {
            return $"{DirTpl}{(hasLocale ? _Locale.GetLocale() : "")}/{fileName}";
        }
        */

        /// <summary>
        /// 傳回spire pdf key file 路徑
        /// </summary>
        /// <returns></returns>
        public static string GetPdfKeyPath()
        {
            return string.IsNullOrEmpty(Config.PdfKeyFile)
                ? "" : _Fun.DirRoot + Config.PdfKeyFile;
        }

        public static void DisConnectHrInsGov()
        {
            //_Net.Disconnect(Config.DirHrInsGovFrom);
            _Net.Disconnect(@"\\192.168.127.123"); // 清掉所有 share
        }

        //連線加保目錄
        public static bool ConnectHrInsGov()
        {
            //temp log error
            //_Log.Error($"path={Config.DirHrInsGovFrom},uid={Config.DirHrInsGovUid},pwd={Config.DirHrInsGovPwd}");
            DisConnectHrInsGov();
            
            var psi = new ProcessStartInfo("cmd.exe", "/c net use")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using Process proc = Process.Start(psi);
            var output = proc.StandardOutput.ReadToEnd();
            Console.WriteLine(output);

            return _Net.Connect(Config.DirHrInsGovFrom, Config.DirHrInsGovUid, Config.DirHrInsGovPwd);
        }

        private static string DirUpload(string subDir, bool sep = true)
        {
            return DirBaseUpload + subDir + (sep ? _Fun.DirSep : "");
        }

    }//class
#pragma warning restore CA2211 // 非常數欄位不應可見
}