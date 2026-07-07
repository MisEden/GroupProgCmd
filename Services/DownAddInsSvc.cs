using Base.Models;
using Base.Services;
using PdfSpire;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GroupProgCmd.Services
{
    //下載加退保pdf檔案, 加上印章圖檔
    public class DownAddInsSvc
    {
        public async Task RunA()
        {
            const string preLog = "DownAddInsSvc: ";
            _Log.Info(preLog + "Start.");

            //move pdf and add mark image, save to new dir
            //string empNo, empName, deptNo, insUnitName, insTypeName;
            var errors = new List<string>();
            var dirFrom = _Str.AddDirSep(_Xp.Config.DirHrAddInsFrom);
            var dirTo = _Str.AddDirSep(_Xp.Config.DirHrAddInsTo);
            var dir = new DirectoryInfo(dirFrom);
            var today = DateTime.Today; // 取得今天的日期

            //取得今天修改過的檔案
            var todayFiles = dir.GetFiles()
                .Where(f => f.LastWriteTime.Date == today)
                .ToList();

            //印章圖檔, PosX/Y在for loop裡面設定, 因為不同類型的pdf位置不一樣
            PdfImageDto[] stamps =
            [
                new() { PosX = 0, PosY = 0, Width = 35, FilePath = _Xp.PathHrSmallStamp },
                new() { PosX = 0, PosY = 0, Width = 70, FilePath = _Xp.PathHrBigStamp },
            ];

            //set pdf key
            var pdfSvc = new SpireSvc();
            pdfSvc.SetKey(_Xp.GetPdfKeyPath());

            var info = _Lib.FindHrInsGovDto("IB")!;

            //調整印章圖檔位置, 因為不同類型的pdf位置不一樣
            stamps[0].PosX = info.PosX1;
            stamps[0].PosY = info.PosY1;
            stamps[1].PosX = info.PosX2;
            stamps[1].PosY = info.PosY2;

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
                    errors.Add($"pdf檔名格式不正確：{file}");
                    continue;
                }

                //set variables
                var inputDate = _Date.TwYmdToDate(cols[0]);
                var deptNo = cols[1];
                var empNo = cols[2];
                var empName = cols[3];
                var insTypeName = cols[4];
                var insUnitName = cols[5];

                var now = _Date.NowDbStr();
                var rowYear = (short)inputDate.Year;
                var id = _Str.NewId();
                var insType = insTypeName == "健保" ? "H" : "W";
                var empId = empNo == "10621" ? "11650" : "16905";
                var deptId = deptNo == "106" ? "414" : "462";

                //pdf 加上印章圖檔 & save to new dir
                pdfSvc.AddImages(fromPath, toPath, stamps);

                var newFname = id + "." + _File.UpExtRename(_File.GetFileExt(fname));
                var newPath = $"{dirTo}{newFname}";

                File.Copy(toPath, newPath);
                File.Delete(toPath);

                var sql = $@"
insert into dbo.HrAddIns (Id, RowYear, AddInsType, InsUnit, EmpId, NowDeptId, FileName, Created)
values('{id}',{rowYear},'{insType}','16','{empId}','{deptId}', '{fname}', '{now}')
";
                await _Db.ExecSqlA(sql);
            }

            //disconnect & log end
            _Xp.BreakHrDir();
            _Log.Info(preLog + "End.");
        }
    }//class
}