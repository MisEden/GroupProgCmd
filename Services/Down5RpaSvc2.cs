using Base.Models;
using Base.Services;
using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using PdfSpire;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace GroupProgCmd.Services
{
    //下載5個RPA上傳的pdf檔案, 加上印章圖檔, 存到新目錄, 並更新HrInsGov.SysDown=1
    public class Down5RpaSvc2
    {
        public async Task RunA()
        {
            const string preLog = "Down5RpaSvc: ";
            _Log.Info(preLog + "Start.");

            //move pdf and add mark image, save to new dir
            var dirFrom = _Str.AddDirSep(_Xp.Config.DirHrInsGovFrom);
            var dirTo = _Str.AddDirSep(_Xp.Config.DirHrInsGovTo);
            var errors = new List<string>();
            string empNo, empName, deptNo, insUnitName, insTypeName;
            DirectoryInfo dir = new DirectoryInfo(dirFrom);
            DateTime today = DateTime.Today; // 取得今天的日期
            // 取得今天修改過的檔案
            var todayFiles = dir.GetFiles()
                                .Where(f => f.LastWriteTime.Date == today)
                                .ToList();

            //印章圖檔, PosX/Y在for loop裡面設定, 因為不同類型的pdf位置不一樣
            PdfImageDto[] markImagesW =
            [
                new() { PosX = 0, PosY = 0, Width = 35, FilePath = _Xp.DirTpl + "HrAddInsSmall.png" },
                new() { PosX = 0, PosY = 0, Width = 70, FilePath = _Xp.DirTpl + "HrAddInsBig.png" },
            ];

            //set pdf key
            var pdfSvc = new SpireSvc();
            pdfSvc.SetKey(_Xp.GetPdfKeyPath());

            var info = _Lib.FindHrInsGovDto("IB")!;

            //調整印章圖檔位置, 因為不同類型的pdf位置不一樣
            markImagesW[0].PosX = info.PosX1;
            markImagesW[0].PosY = info.PosY1;
            markImagesW[1].PosX = info.PosX2;
            markImagesW[1].PosY = info.PosY2;

            foreach (var file in todayFiles)
            {
                Console.WriteLine($"檔名: {file.Name} (修改時間: {file.LastWriteTime})");

                var fname = file.Name;
                var fromPath = $"{dirFrom}{fname}";
                var toPath = $"{dirTo}{fname}";
                //File.Copy(fromPath, toPath, true);   //overwrite 

                var cols = fname.ToLower().Replace(".pdf", "").Split('-');
                if (cols.Length != 6)
                {
                    errors.Add($"pdf檔案格式不正確：{file}");
                    continue;
                }

                DateTime inputDate;
                var now = _Date.NowDbStr();
                //set variables
                inputDate = _Date.TwYmdToDate(cols[0]);
                deptNo = cols[1];
                empNo = cols[2];
                empName = cols[3];
                insTypeName = cols[4];
                insUnitName = cols[5];
                var rowYear = (short)inputDate.Year;
                var id = _Str.NewId();
                var codeInsType = insTypeName == "健保" ? "H" : "W";
                var emp = empNo == "10621" ? "11650" : "16905";
                var dept = deptNo == "106" ? "414" : "462";

                //pdf 加上印章圖檔 & save to new dir
                pdfSvc.AddImages(fromPath, toPath, markImagesW);

                var fnewName = id + "." + _File.UpExtRename(_File.GetFileExt(fname));
                var newPath = $"{dirTo}{fnewName}";

                File.Copy(toPath, newPath);
                File.Delete(toPath);

                var sql = $@"
insert into dbo.HrAddIns (Id,RowYear,AddInsType,InsUnit,EmpId,NowDeptId,FileName,Created)
values('{id}',{rowYear},'{codeInsType}','16','{emp}','{dept}', '{fname}', '{now}')
";
                //                var sql = $@"
                //insert into dbo.HrAddIns (Id,RowYear,AddInsType,InsUnit,EmpId,NowDeptId,FileName,Created)
                //values('{_Str.NewId()}',{rowYear},'H','16','11650','414', '{fname}', '{now}')
                //";
                await _Db.ExecSqlA(sql);

            }

            //disconnect & log end
            _Xp.DisConnectHrInsGov();
            _Log.Info(preLog + "End.");

        }


    }//class
}