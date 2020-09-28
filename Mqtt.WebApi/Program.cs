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
            var host = CreateHostBuilder(args).Build();
            var context = host.Services.GetRequiredService<DataContext>();
            //context.Database.EnsureDeleted();
            context.Database.Migrate();
            context.SaveChanges();
            Storage.Seed(context);
            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>()
                        .UseDefaultServiceProvider((context, options) =>
                    {
                        options.ValidateScopes = context.HostingEnvironment.IsDevelopment();
                        options.ValidateOnBuild = true;
                    });
                }).ConfigureLogging(loggerBuilder =>
                {
                    loggerBuilder.ClearProviders();
                    loggerBuilder.AddConsole();
                    loggerBuilder.SetMinimumLevel(LogLevel.Warning);
                }).UseNLog().UseDefaultServiceProvider(options =>
                    options.ValidateScopes = false);
    }
}
