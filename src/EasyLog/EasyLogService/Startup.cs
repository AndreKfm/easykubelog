using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EasyLogService.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WatchedFileList;

namespace EasyLogService
{

    public interface ICentralLogServiceWatcher
    {
        public void Start();
        public void Stop();
    }


    public class CentralLogServiceWatcher : ICentralLogServiceWatcher
    {
        IAutoCurrentFileList _watchCurrentFileList;
        ICentralLogService _centralLogService;
        string _directory;

        public CentralLogServiceWatcher(IConfiguration config, IAutoCurrentFileList watchCurrentFileList, ICentralLogService centralLogService)
        {
            _watchCurrentFileList = watchCurrentFileList;
            _centralLogService = centralLogService;
            _directory =  config["WatchDirectory"];
        }

        public void Start()
        {
            Stop();
            _centralLogService.Start();
            _watchCurrentFileList.Start(_directory);
            _current = _watchCurrentFileList.BlockingReadAsyncNewOutput(HandleWrittenLogs);
        }

        public void Stop()
        {
            _watchCurrentFileList.Stop();
            _centralLogService.Stop();
            _current?.Wait();
            _current = null;
        }

        Task _current;


        private void HandleWrittenLogs(NewOutput newOutput, CancellationToken token)
        {
            // Wait for new entries written to any log file and pass it to the central log service
            _centralLogService.AddLogEntry(new LogEntry(newOutput.Filename, newOutput.Lines));
        }
    }

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddSingleton<IAutoCurrentFileList, AutoCurrentFileList>();
            services.AddSingleton<ICentralLogService, CentralLogService>();
            services.AddSingleton<ICentralLogServiceWatcher, CentralLogServiceWatcher>();

        }

        public void ConfigureOwnServices(ICentralLogServiceWatcher centralWatcher)
        {
            centralWatcher.Start();
        }


        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });


            // Call ConfigureOwnServices with variable number of arguments automatically resolved by DI
            StartupHelper.RetrieveServicesAndCallMethod(this, "ConfigureOwnServices", app.ApplicationServices);
        }
    }

    class StartupHelper
    {
        public static void RetrieveServicesAndCallMethod(object callObject, string methodName, IServiceProvider serviceProvider)
        {
            var method = callObject.GetType().GetMethod("ConfigureOwnServices");
            var paramArray = method.GetParameters();
            List<object> services = new List<object>();
            foreach (var p in paramArray)
            {
                var paramType = p.ParameterType;
                var service = serviceProvider.GetService(paramType);
                services.Add(service);
            }

            method.Invoke(callObject, services.ToArray());

        }
    }
}
