using System;
using Serilog;
using EasyNetQ;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using WhatBackend.EmailRenderService.Services;
using Microsoft.Extensions.DependencyInjection;
using CharlieBackend.Core.IntegrationEvents.Events;
using WhatBackend.EmailRenderService.Services.Interfaces;
using WhatBackend.EmailRenderService.IntegrationEvents.EventHandling;

namespace WhatBackend.EmailRenderService
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
            services.AddCors();
            services.AddSingleton<IMessageTemplateService, MessageTemplateService>();
            services.AddSingleton<AccountApprovedHanlder>();
            services.AddSingleton<RegistrationSuccessHanlder>();
            services.AddSingleton<CourseOpenedHanlder>();
            services.AddSingleton(RabbitHutch.CreateBus(Configuration.GetConnectionString("RabbitMQ")));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(
            IApplicationBuilder app,
            IWebHostEnvironment env,
            IHostApplicationLifetime applicationLifetime,
            IBus bus
            )
        {
            //When the application is stopping we need to make unsubscribe for IBus.
            //Important! Even in debugg mode finish this application with Ctrl+C in a console to make sure
            //what unsubscribe was done well.
            applicationLifetime.ApplicationStopping.Register(OnShutdown, bus);

            app.ApplicationServices.GetRequiredService<IBus>().SendReceive.Receive("AccountApproved", 
                    x => x.Add<AccountApprovedEvent>(message => 
                    app.ApplicationServices.GetService<AccountApprovedHanlder>()
                    .HandleAsync(message)));

            app.ApplicationServices.GetRequiredService<IBus>().SendReceive.Receive("RegistrationSuccess",
                    x => x.Add<RegistrationSuccessEvent>(message =>
                    app.ApplicationServices.GetService<RegistrationSuccessHanlder>()
                    .HandleAsync(message)));

            app.ApplicationServices.GetRequiredService<IBus>().SendReceive.Receive("CourseOpened",
                    x => x.Add<CourseOpenedEvent>(message =>
                    app.ApplicationServices.GetService<CourseOpenedHanlder>()
                    .HandleAsync(message)));

            app.UseCors(builder =>
            {
                builder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSerilogRequestLogging();

            app.UseRouting();
        }

        private void OnShutdown(object toDispose)
        {
            ((IDisposable)toDispose).Dispose();
        }
    }
}
