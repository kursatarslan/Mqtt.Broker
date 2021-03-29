using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mqtt.Context;
using NpgsqlTypes;
using Serilog;

namespace Mqtt.WebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile(@"appsettings.json", false, true)               
                .Build();

            Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()  
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

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
                .UseSerilog()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>()
                        .UseDefaultServiceProvider((context, options) =>
                    {
                        options.ValidateScopes = context.HostingEnvironment.IsDevelopment();
                        options.ValidateOnBuild = true;
                    });
                }).UseDefaultServiceProvider(options =>
                    options.ValidateScopes = false);
    }
}
