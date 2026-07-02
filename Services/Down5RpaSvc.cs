using Base.Models;
using Base.Services;
using PdfSpire;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace GroupProgCmd.Services
{
    //下載5個RPA上傳的pdf檔案, 加上印章圖檔, 存到新目錄, 並更新HrInsGov.SysDown=1
    public class Down5RpaSvc
    {
        public async Task RunA()
        {
            const string preLog = "Down5RpaSvc: ";
            _Log.Info(preLog + "Start.");

            //讀取HrInsGov待處理: RPA已上傳, 系統未下載, 狀態=1(正常)
            var sql = $@"
select 
    a.*,
    e.EmpNo
from dbo.HrInsGov a
join dbo.OrgEmp e on a.EmpId=e.Id
where 1=1
and FileOkCount > 0
and SysDown=0
and Status=1
order by a.StartYear, a.StartMonth
";
            var rows = await _Db.GetRowsA(sql);
            if (rows == null)
            {
                _Log.Info(preLog + "HrInsGov 無符合資料.");
                return;
            }

            //印章圖檔, PosX/Y在for loop裡面設定, 因為不同類型的pdf位置不一樣
            PdfImageDto[] markImagesW =
            [
                new() { PosX = 0, PosY = 0, Width = 35, FilePath = _Xp.DirTpl + "HrAddInsSmall.png" },
                new() { PosX = 0, PosY = 0, Width = 70, FilePath = _Xp.DirTpl + "HrAddInsBig.png" },
            ];

            //connect hr folder
            if (!_Xp.ConnectHrInsGov())
            {
                _Log.Error(preLog + "無法連線pdf來源目錄。");
                return;
            }

            //set pdf key
            var pdfSvc = new SpireSvc();
            pdfSvc.SetKey(_Xp.GetPdfKeyPath());

            //move pdf and add mark image, save to new dir
            var dirFrom = _Str.AddDirSep(_Xp.Config.DirHrInsGovFrom);
            var dirTo = _Str.AddDirSep(_Xp.Config.DirHrInsGovTo);
            foreach (var row in rows)
            {
                //todo: 如果是 type 1,2,3則沒有起迄年月, 一律讀取建檔年月(單筆) by HR !!
                var rowType = row["RowType"]!.ToString();

                var startYear = Convert.ToInt32(row["StartYear"]);
                var startMonth = Convert.ToInt32(row["StartMonth"]);
                var endYear = Convert.ToInt32(row["EndYear"]);
                var endMonth = Convert.ToInt32(row["EndMonth"]);

                var start = new DateTime(startYear, startMonth, 1);
                var end = new DateTime(endYear, endMonth, 1);
                var info = _Lib.FindHrInsGovDto(rowType)!;
                var empNo = row["EmpNo"]!.ToString();

                //調整印章圖檔位置, 因為不同類型的pdf位置不一樣
                markImagesW[0].PosX = info.PosX1;
                markImagesW[0].PosY = info.PosY1;
                markImagesW[1].PosX = info.PosX2;
                markImagesW[1].PosY = info.PosY2;

                List<string> toPaths = [];
                for (var dt = start; dt <= end; dt = dt.AddMonths(1))
                {
                    var fname = $"{_Lib.GetHrInsGovFstem(dt.Year, dt.Month, info.Sname, empNo)}.pdf";
                    var fromPath = $"{dirFrom}{info.FromDir}/{fname}";
                    if (!File.Exists(fromPath))
                    {
                        _Log.Error(preLog + $"來源pdf檔案不存在: {fromPath}");
                        break;  //離開這一層for
                    }

                    //pdf 加上印章圖檔 & save to new dir
                    //var marks = (insType == HrAddInsTypeEstr.Work) ? markImagesW : markImagesH;
                    var toPath = $"{dirTo}{fname}";
                    pdfSvc.AddImages(fromPath, toPath, markImagesW);

                    //add to list for zip or copy
                    toPaths.Add(toPath);

                    //不刪除來源pdf檔案
                    //File.Delete(fromPath);
                }

                //check
                var fileCount = toPaths.Count;
                if (fileCount == 0)
                    continue;

                //zip if need, 2者都要修改副檔名 for 資安考慮
                var id = row["Id"]!.ToString();
                var toFile2 = "";
                if (fileCount > 1)
                {
                    toFile2 = $"{id}.{_File.UpExtRename("zip")}";
                    _File.ZipFiles(toPaths, $"{dirTo}{toFile2}");       //delete then write
                    _Log.Info(preLog + $"寫入{toFile2}, pdf檔案數={fileCount}");
                }
                else
                {
                    toFile2 = $"{id}.{_File.UpExtRename("pdf")}";
                    File.Copy(toPaths[0], $"{dirTo}{toFile2}", true);   //overwrite 
                    _Log.Info(preLog + $"寫入{toFile2}");
                }

                //刪除 toPaths
                foreach (var toPath in toPaths)
                    File.Delete(toPath);

                //update HrInsGov.SysDown
                await _Db.ExecSqlA($"update dbo.HrInsGov set SysDown=1 where Id='{id}'");
            }

            //disconnect & log end
            _Xp.DisConnectHrInsGov();
            _Log.Info(preLog + "End.");
        }

    }//class
}