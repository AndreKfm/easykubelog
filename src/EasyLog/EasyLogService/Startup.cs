using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EasyLogService.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WatcherFileListClasses;
using EasyLogService.Commands;
using System.IO;
using System.Linq;
using EasyLogService.Tool.Simulator;
using EasyLogService.Services.CentralLogService;
using Microsoft.Extensions.Logging;
using LogEntries;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using DirectoryWatcher;

namespace EasyLogService
{

    public interface ICentralLogServiceWatcher
    {
        public void Start();
        public void Stop();
    }

    /// <summary>
    /// Configurations: 
    ///    WatchDirectory
    /// </summary>
    public class CentralLogServiceWatcher : ICentralLogServiceWatcher
    {
        readonly IAutoCurrentFileList _watchCurrentFileList;
        readonly ICentralLogService _centralLogService;
        readonly string _directory;

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
            _watchCurrentFileList.Start();
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
            //Trace.TraceInformation($"CentralLogService add log entry: [{newOutput.FileName}] - [{newOutput.Lines}]");
            _centralLogService.AddLogEntry(new LogEntry(newOutput.FileName, newOutput.Lines));
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
            var consoleTracer = new ConsoleTraceListener(true);
            Trace.Listeners.Add(consoleTracer);
            consoleTracer.Name = "EasyLogService";



            services.AddControllers();
            services.Configure<AutoCurrentFileListSettings>(Configuration.GetSection("AutoCurrentFileListSettings"));
            services.AddSingleton<IAutoCurrentFileList, AutoCurrentFileList>();
            services.AddOptions();

            services.Configure<CentralLogServiceCacheSettings>(Configuration.GetSection("CentralLogServiceCacheSettings"));
            services.Configure<FileDirectoryWatcherSettings>(Configuration.GetSection("FileDirectoryWatcherSettings"));
            services.AddSingleton<ICentralLogServiceCache>(x =>
            {
                var logger = x.GetService<ILogger<CentralLogServiceCache>>();
                var settings = x.GetService<IOptions<CentralLogServiceCacheSettings>>();
                return new CentralLogServiceCache(settings, Configuration, logger);
            });

            services.AddSingleton<ICentralLogService, CentralLogService>();
            services.AddSingleton<ICentralLogServiceWatcher, CentralLogServiceWatcher>();
            services.AddTransient<ISearchCommand, SearchCommandHandler>();

            services.AddSingleton<LogSimulatorReadAllContent>();



            services.AddRazorPages();
            services.AddServerSideBlazor();

        }

        public void ConfigureOwnServices(ICentralLogServiceWatcher centralWatcher, LogSimulatorReadAllContent logSimulator, ICentralLogServiceCache cache)
        {
            centralWatcher.Start();

            bool logSimulatorActive = Configuration.GetValue<bool>("EnableLogSimulatorReadFromEachFile");
            if (logSimulatorActive)
            {
                string directory = Configuration["LogSimulatorDirectory"];
                int maxLines = Configuration.GetValue<int>("MaxLogSimulatorLinesToReadFromEachFile");
                logSimulator.InitialRead(directory, cache, maxLines);
            }
        }


        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/Main");
            });

            //app.AddComponent<App>("app");

            // Call ConfigureOwnServices with variable number of arguments automatically resolved by DI
            StartupHelper.RetrieveServicesAndCallMethod(this, "ConfigureOwnServices", app.ApplicationServices);
        }
    }

    class StartupHelper
    {
        public static void RetrieveServicesAndCallMethod(object callObject, string methodName, IServiceProvider serviceProvider)
        {
            var method = callObject.GetType().GetMethod(methodName);
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
