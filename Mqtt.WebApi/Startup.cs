using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Mqtt.Application.Contracts;
using Mqtt.Application.Services;
using Mqtt.Application.Services.Hosted;
using Mqtt.Context;
using Mqtt.Data.Contracts;
using Mqtt.Data.Repositories;
using MQTTnet;
using MQTTnet.Server;

namespace Mqtt.WebApi
{
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
            services.AddControllers().AddControllersAsServices();;
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Platooning MQTT Broker", Version = "v1" });
            });

            
            services.AddSingleton<IMqttServerOptions, MqttServerOptions>();
            services.AddSingleton<IMqttServerFactory, MqttFactory>();
            services.AddSingleton<IMqttServerStorage, MqttStorage>();
            services.AddSingleton<IMqttServerSubscriptionInterceptor, MqttServerSubscriptionInterceptor>();
            services.AddSingleton<IMqttServerApplicationMessageInterceptor, MqttServerApplicationMessageInterceptor>();
            services.AddSingleton<IMqttServerConnectionValidator, MqttServerConnectionValidator>();
            services.AddSingleton<IServerBuilder, ServerBuilder>();
            services.AddTransient<IMqttRepository, MqttRepository>();
            ISecretProvider sp = new SecretProvider();
            services.AddSingleton(sp);
            var stage = Environment.GetEnvironmentVariable("STAGE") ?? "Development";
            var connectionString = string.Empty;
            if (stage == "Development")
            {
                connectionString = Configuration.GetConnectionString("postgres");
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
            {
                options.UseNpgsql(connectionString, b => b.MigrationsAssembly("Mqtt.Context"));
            },ServiceLifetime.Transient);
            services.AddTransient<Func<DataContext>>(options => () => options.GetService<DataContext>());
            
            services.AddHostedService<MqttService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "MQTT Broker v1"));


            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
