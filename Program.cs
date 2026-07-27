using Base.Interfaces;
using Base.Models;
using Base.Services;
using GroupProgCmd.Models;
using GroupProgCmd.Services;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Data.Common;
using System.Threading.Tasks;

namespace GroupProgCmd
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            //console app使用DOTNET_ENVIRONMENT來決定環境, 預設是Production, 所以在Debug模式下要改成Development !!
#if DEBUG
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Development");
#endif

            //todo: 變成無法讀不到組態!!
            //initial
            var builder = Host.CreateApplicationBuilder(args);
            var services = builder.Services;

            Console.WriteLine(builder.Environment.EnvironmentName);

            //console app 預設不會copy appsettings.* 到 bin 目錄, 造成讀不到組態
            //需要在專案檔裡面加上以下設定, 才會把 appsettings.* copy 到 bin 目錄
            //1. FunConfig
            var config = new ConfigDto();
            builder.Configuration.GetSection("FunConfig").Bind(config);
            _Fun.Config = config;

            //2. MyConfig
            var myConfig = new MyConfigDto();
            builder.Configuration.GetSection("MyConfig").Bind(myConfig);
            _Xp.Config = myConfig;

            #region init 
            services.AddSingleton<IBaseUserSvc, BaseUserSvc>();            
            services.AddTransient<DbConnection, SqlConnection>();   //ado.net for mssql
            services.AddTransient<DbCommand, SqlCommand>();

            //is development or not
            var host = builder.Build();
            var env = host.Services.GetRequiredService<IHostEnvironment>();
            var isDev = env.IsDevelopment();

            //set DI container & initial _Fun
            IServiceProvider diBox = services.BuildServiceProvider();
            _Fun.Init(isDev, diBox, Base.Enums.DbTypeEnum.MSSql, Base.Enums.AuthTypeEnum.None, false);
            #endregion

            //decode after _Fun.Init(), 連線HR目錄, 有加密
            if (_Fun.Config.Encode)
            {
                var key = _Str.GetKey();
                _Xp.Config.HrDirUid = _Str.DecodeByKey(_Xp.Config.HrDirUid, key).Replace("\\\\", "\\");   //有domain
                _Xp.Config.HrDirPwd = _Str.DecodeByKey(_Xp.Config.HrDirPwd, key);
            }

            //7.run service 
            //todo: temp remark
            //await new Down5RpaSvc().RunA();
            await new DownAddInsSvc().RunA();
        }

    }
}
