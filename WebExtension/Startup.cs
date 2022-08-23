using DirectScale.Disco.Extension.Middleware;
using DirectScale.Disco.Extension.Middleware.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebExtension.Helper;
using WebExtension.Helper.Interface;
using WebExtension.Helper.Models;
using WebExtension.Hooks.Order;
using WebExtension.Repositories;
using WebExtension.Services;

namespace WebExtension
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            CurrentEnvironment = env;
        }

        public IConfiguration Configuration { get; }
        private IWebHostEnvironment CurrentEnvironment { get; set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add cors
            services.AddCors();

            #region FOR LOCAL DEBUGGING USE
            //
            //
            //
            //Remark This section before upload
            if (CurrentEnvironment.IsDevelopment())
            {
                //services.AddSingleton<ITokenProvider>(x => new WebExtensionTokenProvider
                //{
                //    DirectScaleUrl = Configuration["configSetting:BaseURL"].Replace("{clientId}", Configuration["configSetting:Client"]).Replace("{environment}", Configuration["configSetting:Environment"]),
                //    DirectScaleSecret = Configuration["configSetting:DirectScaleSecret"],
                //    ExtensionSecrets = new[] { Configuration["configSetting:ExtensionSecrets"] }
                //});
            }
            //Remark This section before upload
            //
            //
            //
            #endregion

            //Repositories
            services.AddSingleton<ICustomLogRepository, CustomLogRepository>();
            services.AddSingleton<IAssociateWebRepository, AssociateWebRepository>();
            services.AddSingleton<IOrderWebRepository, OrderWebRepository>();

            //Services
            services.AddSingleton<ICommonService, CommonService>();
            services.AddSingleton<ICustomLogService, CustomLogService>();
            services.AddSingleton<IAssociateWebService, AssociateWebService>();
            services.AddSingleton<IOrderWebService, OrderWebService>();

            //DS
            services.AddDirectScale(c =>
            {
                //CustomPage
                //c.AddCustomPage(Menu.Associates, "Custom Order Report", "/CustomPage/CustomOrderReport");

                //Hooks
                //c.AddHook<SubmitOrderHook>();

                //Event Handler
                //options.AddEventHandler("CreateAssociateEvent", "/api/WebHook/CreateAssociateEvent");
            });

            services.AddControllersWithViews();

            //Swagger
            services.AddSwaggerGen();

            //Configurations
            services.Configure<configSetting>(Configuration.GetSection("configSetting"));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            //Configure Cors
            app.UseCors(builder => builder
                .AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod());

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            //DS
            app.UseDirectScale();

            //Swagger
            app.UseSwagger();
            app.UseSwaggerUI(c => {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V2");
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
    internal class WebExtensionTokenProvider : ITokenProvider
    {
        public string DirectScaleUrl { get; set; }
        public string DirectScaleSecret { get; set; }
        public string[] ExtensionSecrets { get; set; }

        public async Task<string> GetDirectScaleSecret()
        {
            return await Task.FromResult(DirectScaleSecret);
        }
        public async Task<string> GetDirectScaleServiceUrl()
        {
            return await Task.FromResult(DirectScaleUrl);
        }
        public async Task<IEnumerable<string>> GetExtensionSecrets()
        {
            return await Task.FromResult(ExtensionSecrets);
        }

    }
}
