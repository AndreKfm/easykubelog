using DirectoryWatcher;
using EasyKubeLogService.Components.Commands;
using EasyKubeLogService.Services.CentralLogService;
using EasyKubeLogService.Tool.Simulator;
using LogEntries;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using EmbeddedBlazorContent;
using WatcherFileListClasses;

namespace EasyKubeLogService
{
    public class TraceLogging
    {
        public bool EnableConsoleTracing { get; set; }
    }

    public interface ICentralLogServiceWatcher
    {
        public void Start();

        // ReSharper disable once UnusedMemberInSuper.Global
        public void Stop();
    }

    /// <summary>
    /// Configurations:
    ///    WatchDirectory
    /// </summary>
    public class CentralLogServiceWatcher : ICentralLogServiceWatcher
    {
        private readonly IAutoCurrentFileList _watchCurrentFileList;
        private readonly ICentralLogService _centralLogService;

        public CentralLogServiceWatcher(IAutoCurrentFileList watchCurrentFileList, ICentralLogService centralLogService)
        {
            _watchCurrentFileList = watchCurrentFileList;
            _centralLogService = centralLogService;
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

        private Task _current;

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

        private void CheckConsoleTracing()
        {
            // Check if console tracing is enabled and add trace listener if set
            // Will be used only in debuggin scenarios, where more detailed output is needed
            if (Configuration.GetSection("Logging").Get<TraceLogging>().EnableConsoleTracing)
            {
                var consoleTracer = new ConsoleTraceListener(true);
                Trace.Listeners.Add(consoleTracer);
                consoleTracer.Name = "EasyKubeLogService";
            }
        }

        public void ConfigureServices(IServiceCollection services)
        {
            CheckConsoleTracing();

            services.AddControllers();
            services.AddOptions();

            services.Configure<AutoCurrentFileListSettings>(Configuration.GetSection("AutoCurrentFileListSettings"));
            services.AddSingleton<IAutoCurrentFileList, AutoCurrentFileList>();

            services.Configure<CentralLogServiceCacheSettings>(Configuration.GetSection("CentralLogServiceCacheSettings"));
            services.Configure<FileDirectoryWatcherSettings>(Configuration.GetSection("FileDirectoryWatcherSettings"));
            services.AddSingleton<ICentralLogServiceCache>(x =>
            {
                var logger = x.GetService<ILogger<CentralLogServiceCache>>();
                var settings = x.GetService<IOptions<CentralLogServiceCacheSettings>>();
                return new CentralLogServiceCache(settings, logger);
            });

            services.AddSingleton<ICentralLogService, CentralLogService>();
            services.AddSingleton<ICentralLogServiceWatcher, CentralLogServiceWatcher>();
            services.AddTransient<ISearchCommand, SearchCommandHandler>();

            services.AddSingleton<LogSimulatorReadAllContent>();

            services.AddRazorPages();
            services.AddServerSideBlazor();
        }

        // ReSharper disable once UnusedMember.Global
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

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            //app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseEmbeddedBlazorContent(typeof(MatBlazor.BaseMatComponent).Assembly);
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapBlazorHub();
                endpoints.MapFallbackToPage("/Main");
            });

            // Call ConfigureOwnServices with variable number of arguments automatically resolved by DI
            StartupHelper.RetrieveServicesAndCallMethod(this, "ConfigureOwnServices", app.ApplicationServices);
        }
    }

    internal class StartupHelper
    {
        public static void RetrieveServicesAndCallMethod(object callObject, string methodName, IServiceProvider serviceProvider)
        {
            var method = callObject.GetType().GetMethod(methodName);
            if (method is { })
            {
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
}