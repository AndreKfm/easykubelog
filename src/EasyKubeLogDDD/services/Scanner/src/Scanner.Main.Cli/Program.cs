using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Scanner.Domain;
using Scanner.Domain.Events;
using Scanner.Domain.Ports;
using Scanner.Domain.Ports.Query;
using Scanner.Infrastructure.Adapter;
using Scanner.Infrastructure.Adapter.EventQueue;
using Scanner.Infrastructure.Adapter.LogDirWatcher;
using Scanner.Infrastructure.Adapter.LogDirWatcher.ManualDirectoryScan;
using Scanner.Infrastructure.Adapter.ScanLogFiles;
using SharedKernel;
using SharedKernel.RootInterfaces;

namespace Scanner.Main.Cli
{


    class EventConsumer : IEventConsumer
    {
        public void NewEventReceived(Event newEvent)
        {
            Console.WriteLine($"Event: {newEvent.Name}");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            using var container = CreateServiceCollection();


            IApplicationMain mainRoot = CreateApplicationRoot(container);


            if (mainRoot != null)
            {
                //mainRoot = new ScannerMainApplicationRoot(watcher, scanner, eventBus);
                Console.WriteLine("Starting scanner");
                mainRoot.Start();
                Console.ReadKey();
                Console.WriteLine("Stopping scanner");
                mainRoot.Stop();
                Console.WriteLine("Stopped scanner - exit");
            }


        }

        private static ServiceProvider CreateServiceCollection()
        {
            var serviceCollection = new ServiceCollection();


            ManualDirectoryScanAndGenerateDifferenceToLastScan pollDirectoryForChanges =
                new ManualDirectoryScanAndGenerateDifferenceToLastScan(
                    new ManualDirectoryScanAndGenerateDifferenceToLastScanSettings(@"d:\test\polldir"),
                    new ManualScanDirectory());

            ILogDirWatcher watcher = new LogDirectoryWatcher(pollDirectoryForChanges);


            serviceCollection.AddSingleton(watcher);
            serviceCollection.AddSingleton<IEventBus, CentralEventQueue>();
            serviceCollection.AddSingleton<IEventProducer>(x => x.GetService<IEventBus>()?.GetProducer());
            serviceCollection.AddSingleton<IScanLogFile, ScanLogFile>();

            var container = serviceCollection.BuildServiceProvider();
            container.GetService<IEventBus>()?.Start();
            return container;
        }

        private static IApplicationMain CreateApplicationRoot(ServiceProvider container)
        {
            var classes = AppDomain.CurrentDomain.GetAssemblies()
                .Where(p =>
                {
                    var name = p.GetName().Name;
                    return (String.IsNullOrEmpty(name) == false) && (name.EndsWith(".Domain"));
                }).Select(a =>
                    a.GetTypes().Where(type => type.IsClass && type.GetInterface("IApplicationMain") != null))
                .SelectMany(p => p.Select(p => p));

            IApplicationMain mainRoot = null;
            foreach (var classType in classes)
            {
                var cstr = classType.GetConstructors();
                if (cstr.Length == 1)
                {
                    var main = cstr[0];
                    var mainParams = main.GetParameters();
                    var param = mainParams.Select(p => container.GetService(p.ParameterType))
                        .Where(p => p != null);

                    if (mainParams.Count() == main.GetParameters().Length)
                    {
                        mainRoot = (IApplicationMain) System.ComponentModel.TypeDescriptor.CreateInstance(null,
                            classType,
                            main.GetParameters().Select(p => p.ParameterType).ToArray(),
                            param.ToArray());
                    }
                }
                else Console.Error.WriteLine($"Application entry class has more than one constructor: {classType.FullName}");
            }

            return mainRoot;
        }

        private static void TestCentralQueue()
        {
            CentralEventQueue queue = new CentralEventQueue();
            var second = new EventConsumer();
            queue.AddConsumer(new EventConsumer());
            queue.AddConsumer(second);
            queue.Start();

            Task.Delay(100).Wait();
            queue.PostEvent(new DirScanStartedEvent("dummy")).Wait();
            Task.Delay(1000).Wait();
            queue.RemoveConsumer(second);
            queue.PostEvent(new DirScanCompletedEvent("dummy")).Wait();

            //Task.Delay(100).Wait();
            Console.WriteLine("Call Stop");
            queue.Stop();
            Console.ReadLine();
        }
    }
}
