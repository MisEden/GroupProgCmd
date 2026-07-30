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
            //var dirFrom = ;
            //var dirTo = _Str.AddDirSep(_Xp.Config.DirHrAddInsTo);
            //var dirFrom2 = new DirectoryInfo(dirFrom);

            //印章圖檔, PosX/Y在for loop裡面設定, 因為不同類型的pdf位置不一樣
            PdfImageDto[] stampsAdd =
            [
                new() { PosX = 370, PosY = 470, Width = 35, FilePath = _Xp.PathHrSmallStamp },
                new() { PosX = 450, PosY = 420, Width = 70, FilePath = _Xp.PathHrBigStamp },
            ];
            PdfImageDto[] stampsBack =
            [
                new() { PosX = 370, PosY = 470, Width = 35, FilePath = _Xp.PathHrSmallStamp },
                new() { PosX = 450, PosY = 420, Width = 70, FilePath = _Xp.PathHrBigStamp },
            ];


            //讀取今天建立的 HrAddIns(不處理) & 比對 todayFiles
            var db = new Db();
            var sql = @"
select FileName
from dbo.HrAddIns
where Created >= CAST(getDate() AS DATE)
";
            var dbFiles = await db.GetStrsA(sql) ?? [];

            //加保
            var okCount = await CopyFiles(_Str.AddDirSep(_Xp.Config.DirHrAddInsFrom), 
                _Str.AddDirSep(_Xp.Config.DirHrAddInsTo), dbFiles, db, stampsAdd);
            _Log.Info(preLog + "處理加保檔案數: " + okCount);

            //退保
            okCount = await CopyFiles(_Str.AddDirSep(_Xp.Config.DirHrBackInsFrom),
                _Str.AddDirSep(_Xp.Config.DirHrBackInsTo), dbFiles, db, stampsBack);
            _Log.Info(preLog + "處理退保檔案數: " + okCount);

            //disconnect & log end
            await db.DisposeAsync();
            _Xp.BreakHrDir();
            _Log.Info(preLog + "End.");
        }

        /// <summary>
        /// 傳回: 處理檔案數
        /// </summary>
        /// <param name="dirFrom">右側有分隔線</param>
        /// <param name="dirTo">右側有分隔線</param>
        /// <param name="dbFiles"></param>
        /// <param name="db"></param>
        /// <param name="stamps"></param>
        /// <returns></returns>
        private async Task<int> CopyFiles(string dirFrom, string dirTo, List<string> dbFiles, Db db, PdfImageDto[] stamps)
        {
            //connect hr folder
            if (!_Xp.ConnectHrDir(dirFrom))
            {
                _Log.Error($"CopyFiles 無法連線pdf來源目錄。({dirFrom})");
                return 0;
            }

            //dirFrom 加上民國年度(ex:115年), 月份(ex:11506)再讀檔案
            var today = DateTime.Today;
            var twYear = today.Year - 1911;
            var month = today.ToString("MM");
            dirFrom = $"{dirFrom}{twYear}年/{twYear}{month}/";

            //取得檔案開頭為今天的檔案
            var dirFrom2 = new DirectoryInfo(dirFrom);
            if (!dirFrom2.Exists)
                return 0;     //回傳0 ,不記錄error

            var dateStr = _Date.ToTwDateStr(DateTime.Today, 3) + "-";
            var todayFiles = dirFrom2.GetFiles()
                .Where(f => f.Name.Contains(dateStr))
                .ToList();

            //set pdf key
            var pdfSvc = new SpireSvc();
            pdfSvc.SetKey(_Xp.GetPdfKeyPath());

            //loop for file
            string sql;
            var errors = new List<string>();
            var okCount = 0;
            foreach (var file in todayFiles)
            {
                //如果dbFiles存在此檔案, 則skip
                var fname = file.Name;
                if (dbFiles.Contains(fname)) continue;

                //Console.WriteLine($"檔名: {file.Name} (修改時間: {file.LastWriteTime})");

                var fromPath = $"{dirFrom}{fname}";
                var toPath = $"{dirTo}{fname}";
                //File.Copy(fromPath, toPath, true);   //overwrite 

                var cols = fname.ToLower().Replace(".pdf", "").Split('-');
                if (cols.Length != 4)
                {
                    errors.Add($"pdf檔名格式不正確：{fname}");
                    continue;
                }

                //set variables
                var inputDate = _Date.TwYmdToDate(cols[0]);
                //var deptNo = cols[1];
                var empNo = cols[1];
                var empName = cols[2];
                var insTypeName = cols[3];  //投保種類
                //var insUnitName = cols[5];  //投保單位

                //var empId = empNo == "10621" ? "11650" : "16905";
                //var deptId = deptNo == "106" ? "414" : "462";
                var emp = await db.GetModelA<IdStrDto>("select Id,Str=DeptId from dbo.OrgEmp where EmpNo=@EmpNo", ["EmpNO", empNo]);
                if (emp == null)
                {
                    errors.Add($"檔名的 EmpNo 不存在({fname})");
                    continue;
                }

                /*
                var deptId = await db.GetStrA("select Id from dbo.XpDept where DeptNo=@DeptNo", ["DeptNO", deptNo]);
                if (_Str.IsEmpty(deptId))
                {
                    errors.Add($"檔名的 DeptNo 不存在({fname})");
                    continue;
                }
                */

                var now = _Date.NowDbStr();
                var rowYear = (short)inputDate.Year;
                var id = _Str.NewId();
                var insType = insTypeName == "加保" ? "A" : "B";

                //pdf 加上印章圖檔 & save to new dir
                pdfSvc.AddImages(fromPath, toPath, stamps);

                var newFname = id + "." + _File.UpExtRename(_File.GetFileExt(fname));
                var newPath = $"{dirTo}{newFname}";

                File.Copy(toPath, newPath);
                File.Delete(toPath);

                sql = $@"
insert into dbo.HrAddIns (Id, RowYear, InsType, EmpId, NowDeptId, FileName, Created)
values('{id}',{rowYear},'{insType}','{emp.Id}','{emp.Str}', '{fname}', '{now}')
";
                await _Db.ExecSqlA(sql);
                okCount++;
            }

            //write error log if any
            if (errors.Count > 0)
            {
                var msg = "DownAddInsSvc.s Error:\n" + _List.ToStr(errors, false, "\n");
                //_Log.Info(msg);
                _Log.Error(msg);
            }
            return okCount;
        }
    }//class
}