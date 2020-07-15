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
using FileListClasses;
using EasyLogService.Commands;
using System.IO;
using System.Linq;

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

    public class LogSimulatorReadAllContent 
    {
        public LogSimulatorReadAllContent()
        {

        }

        private bool readDone = false;
        public void InitialRead(string directory, ICentralLogServiceCache cache)
        {
            if (readDone)
                return;

            var files = Directory.GetFiles(directory);

            Console.WriteLine($"Read simulation files from [{directory}]");
            Parallel.ForEach(files, (file) =>
            {
                var lines = File.ReadAllLines(file);
                foreach(var line in lines.Take(10000))
                {
                    cache.AddEntry(new LogEntry(file, line));
                }
            });

            readDone = true;
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
            services.AddSingleton<ICentralLogServiceCache>(x => new CentralLogServiceCache(Int32.Parse(Configuration["MaxLogLines"])));
            services.AddSingleton<ICentralLogService, CentralLogService>();
            services.AddSingleton<ICentralLogServiceWatcher, CentralLogServiceWatcher>();
            services.AddTransient<ISearchCommand, SearchCommand>();

            services.AddSingleton<LogSimulatorReadAllContent>();



            services.AddRazorPages();
            services.AddServerSideBlazor();

        }

        public void ConfigureOwnServices(ICentralLogServiceWatcher centralWatcher, LogSimulatorReadAllContent logSimulator, ICentralLogServiceCache cache)
        {
            centralWatcher.Start();

            string directory = Configuration["SimulatorDirectory"];
            if (String.IsNullOrEmpty(directory))
            {
                directory = @"c:\test\logs";
            }
            logSimulator.InitialRead(directory, cache);
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
