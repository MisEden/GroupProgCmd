namespace GroupProgCmd.Models
{
    /// <summary>
    /// get from appSettings.json MyConfig section
    /// </summary>
    public class MyConfigDto
    {
        public string PdfKeyFile { get; set; } = "";        
        public string DirHrInsGovFrom { get; set; } = "";
        public string DirHrInsGovTo { get; set; } = "";
        public string DirHrInsGovUid { get; set; } = "";

        public string DirHrInsGovPwd { get; set; } = "";

        /*
        //勞健退-加保 來源pdf檔案目錄(網路磁碟目錄)
        public string DirHrAddInsPdf { get; set; } = "";

        //nas連線帳號/密碼
        public string DirHrAddInsPdfUid { get; set; } = "";
        public string DirHrAddInsPdfPwd { get; set; } = "";
        */
    }
}
