using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mqtt.Application.Contracts;
using Mqtt.Application.Services;
using Mqtt.Application.Services.Hosted;
using Mqtt.Context;
using Mqtt.Data.Contracts;
using Mqtt.Data.Repositories;
using MQTTnet;
using MQTTnet.Server;
using NLog.Extensions.Hosting;

namespace Mqtt.WebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.ConfigureServices((hostContext, services) =>
                    {
                        var configuration = hostContext.Configuration;
                        services.AddSingleton<IMqttServerOptions, MqttServerOptions>();
                        services.AddSingleton<IMqttServerFactory, MqttFactory>();
                        services.AddSingleton<IMqttServerStorage, MqttStorage>();
                        services.AddSingleton<IMqttServerSubscriptionInterceptor, MqttServerSubscriptionInterceptor>();
                        services
                            .AddSingleton<IMqttServerApplicationMessageInterceptor,
                                MqttServerApplicationMessageInterceptor>();
                        services.AddSingleton<IMqttServerConnectionValidator, MqttServerConnectionValidator>();
                        services.AddSingleton<IServerBuilder, ServerBuilder>();
                        services.AddScoped<IMqttRepository, MqttRepository>();
                        ISecretProvider sp = new SecretProvider();
                        services.AddSingleton(sp);
                        var stage = Environment.GetEnvironmentVariable("STAGE") ?? "Development";
                        var connectionString = string.Empty;
                        if (stage == "Development")
                        {
                            connectionString = configuration.GetConnectionString("postgres");
                        }
                        else
                        {
                            var db = sp.GetSecret("database");
                            var host = sp.GetSecret("host");
                            var username = sp.GetSecret("username");
                            var port = sp.GetSecret("port");
                            var pw = sp.GetSecret("postgres_db_password");
                            connectionString =
                                $"Host={host};Port={port};Username={username};Password={pw};Database={db};";
                        }

                        services.AddDbContext<DataContext>(options =>
                            options.UseNpgsql(connectionString, b => b.MigrationsAssembly("Mqtt.Context")));
                        
                        services.AddHostedService<MqttService>();
                    });
                }).ConfigureLogging(loggerBuilder =>
                {
                    loggerBuilder.ClearProviders();
                    loggerBuilder.AddConsole();
                    loggerBuilder.SetMinimumLevel(LogLevel.Warning);
                }).UseNLog();
    }
}
