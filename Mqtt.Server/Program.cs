using System;
using System.Threading.Tasks;
using Mqtt.Application.Contracts;
using Mqtt.Application.Services;
using Mqtt.Application.Services.Hosted;
using Mqtt.Context;
using Mqtt.Data.Contracts;
using Mqtt.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Server;
using Serilog;

namespace Mqtt.Server
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile(@"appsettings.json", false, true)               
                .Build();

            Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

            var host = CreateHostBuilder(args).Build();
            var context = host.Services.GetRequiredService<DataContext>();
            //context.Database.EnsureDeleted();
            context.Database.Migrate();
            await context.SaveChangesAsync();
            Storage.Seed(context);
            await host.RunAsync();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
            => Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureServices((hostContext, services) =>
                {
                    var configuration = hostContext.Configuration;
                    services.AddSingleton<IMqttServerOptions, MqttServerOptions>();
                    services.AddSingleton<IMqttServerFactory, MqttFactory>();
                    services.AddSingleton<IMqttServerStorage, MqttStorage>();
                    services.AddSingleton<IMqttServerSubscriptionInterceptor, MqttServerSubscriptionInterceptor>();
                    services.AddSingleton<IMqttServerApplicationMessageInterceptor, MqttServerApplicationMessageInterceptor>();
                    services.AddSingleton<IMqttServerConnectionValidator, MqttServerConnectionValidator>();
                    services.AddSingleton<IServerBuilder, ServerBuilder>();
                    services.AddScoped<IMqttRepository, MqttRepository>();
                    ISecretProvider sp = new SecretProvider();
                    services.AddSingleton(sp);
                    var stage = Environment.GetEnvironmentVariable("STAGE") ?? "Development";
                    string connectionString;
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
                        connectionString = $"Host={host};Port={port};Username={username};Password={pw};Database={db};";
                    }
                    services.AddDbContext<DataContext>(options =>
                        options.UseNpgsql(connectionString, b => b.MigrationsAssembly("Mqtt.Context")));
                    
                    services.AddHostedService<MqttService>();
                });
    }
}