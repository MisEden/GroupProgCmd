namespace GroupProgCmd.Models
{
    /// <summary>
    /// get from appSettings.json MyConfig section
    /// </summary>
    public class MyConfigDto
    {
        public string PdfKeyFile { get; set; } = "";
        public string HrDirUid { get; set; } = "";
        public string HrDirPwd { get; set; } = "";

        //政府官網5個RPA
        public string DirHrInsGovFrom { get; set; } = "";
        public string DirHrInsGovTo { get; set; } = "";

        //勞健保-加保
        public string DirHrAddInsFrom { get; set; } = "";
        public string DirHrAddInsTo { get; set; } = "";

        //勞健保-加保
        public string DirHrBackInsFrom { get; set; } = "";
        public string DirHrBackInsTo { get; set; } = "";

        /*
        //勞健退-加保 來源pdf檔案目錄(網路磁碟目錄)
        public string DirHrAddInsPdf { get; set; } = "";

        //nas連線帳號/密碼
        public string DirHrAddInsPdfUid { get; set; } = "";
        public string DirHrAddInsPdfPwd { get; set; } = "";
        */
    }
}
