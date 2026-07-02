using Base.Interfaces;
using Base.Models;
using Base.Services;
using GroupProgCmd.Models;
using GroupProgCmd.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace GroupProgCmd
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            //1.initial & load Config.json
            IConfiguration configBuild = new ConfigurationBuilder()
                .AddJsonFile("Config.json", optional: true, reloadOnChange: true)
                .Build();

            var myConfig = new MyConfigDto();
            configBuild.GetSection("MyConfig").Bind(myConfig);
            _Xp.Config = myConfig;

            //2.appSettings "FunConfig" section -> _Fun.Config
            var config = new ConfigDto();
            configBuild.GetSection("FunConfig").Bind(config);
            _Fun.Config = config;

            //3.setup our DI
            var services = new ServiceCollection();

            //4.base user info for base component
            services.AddSingleton<IBaseUserSvc, BaseUserSvc>();

            //5.ado.net for mssql
            services.AddTransient<DbConnection, SqlConnection>();
            services.AddTransient<DbCommand, SqlCommand>();

            //6.initial _Fun by mssql
            IServiceProvider diBox = services.BuildServiceProvider();
            _Fun.Init(false, diBox, Base.Enums.DbTypeEnum.MSSql, Base.Enums.AuthTypeEnum.None, false);

            /*
            _Xp.Config.DbEip = _Str.DecodeByFile(_Xp.Config.DbEip).Replace("\\\\", "\\");    //config的\到字串會變\\
            _Xp.Config.DirHrAddInsPdfUid = _Str.DecodeByFile(_Xp.Config.DirHrAddInsPdfUid).Replace("\\\\", "\\");   //有domain
            _Xp.Config.DirHrAddInsPdfPwd = _Str.DecodeByFile(_Xp.Config.DirHrAddInsPdfPwd);
            */

            //7.run main 
            await new MyService().RunA();
        }
    }
}
