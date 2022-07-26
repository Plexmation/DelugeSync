using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using Serilog.Sinks.Elasticsearch;
using Microsoft.Extensions.Logging;
using Jaeger;

namespace DelugeSync
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureAppConfiguration(configApp =>
                {
                    configApp.SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", false)
                    .AddEnvironmentVariables();
                })
                .ConfigureLogging((hostContext, config) =>
                {
                    var logger = new LoggerConfiguration()
                        .MinimumLevel.Information()
                        .WriteTo.Console()
                        .WriteTo.Elasticsearch(
                            new ElasticsearchSinkOptions(new Uri(hostContext.Configuration["Elasticsearch:Uri"]))
                            {
                                AutoRegisterTemplate = true,
                                BufferLogShippingInterval = TimeSpan.FromSeconds(10),
                                BufferBaseFilename = "logs",
                                IndexFormat = hostContext.Configuration["Elasticsearch:IndexFormat"],
                                TemplateName = hostContext.Configuration["Elasticsearch:TemplateName"]
                            })
                        .CreateLogger();
                    config.AddSerilog(logger, false);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddSingleton(provider =>
                    {
                        var loggerFactory = provider.GetService<ILoggerFactory>();
                        var sampler = new Configuration.SamplerConfiguration(loggerFactory);
                        var reporter = new Configuration.ReporterConfiguration(loggerFactory).WithLogSpans(true);
                        return new Configuration(hostContext.Configuration["ServiceName"], loggerFactory).WithSampler(sampler).WithReporter(reporter).GetTracer();
                    });
                    services.AddSingleton(s =>
                    {
                        return new ConnectionFactory()
                        {
                            UserName = hostContext.Configuration["RabbitMQ:Username"],
                            Password = hostContext.Configuration["RabbitMQ:Password"],
                            HostName = hostContext.Configuration["RabbitMQ:Hostname"],
                            VirtualHost = hostContext.Configuration["RabbitMQ:VirtualHost"]
                        };
                    });
                    services.AddSingleton<Worker>();
                    services.AddHostedService<Service>();
                })
                .UseConsoleLifetime()
                .Build();

            host.Run();
        }
    }
}
