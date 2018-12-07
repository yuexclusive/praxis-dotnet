﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using System;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using LY.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Swashbuckle.AspNetCore.Swagger;
using System.IO;
using Microsoft.Extensions.PlatformAbstractions;
using Ocelot.Provider.Consul;
using Microsoft.AspNetCore.Mvc;

namespace LY.Gateway
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            var builder = new Microsoft.Extensions.Configuration.ConfigurationBuilder();
            builder.SetBasePath(ConfigUtil.CurrentDirectory)
                   //.AddJsonFile("appsettings.json")
                   //add configuration.json
                   .AddJsonFile("configuration.json", optional: false, reloadOnChange: true)
                   .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //swagger ui
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "LY.Gateway", Version = "v1" });
                c.IncludeXmlComments(Path.Combine(ConfigUtil.CurrentDirectory, "LY.Gateway.xml"));
            });

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer("TestKey", options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,//是否验证Issuer
                        ValidateAudience = true,//是否验证Audience
                        ValidateLifetime = true,//是否验证失效时间
                        ValidateIssuerSigningKey = true,//是否验证SecurityKey
                        ValidAudience = Const.JWT.Audience,//Audience
                        ValidIssuer = Const.JWT.Issuer,//Issuer，这两项和前面签发jwt的设置一致
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Const.JWT.SecurityKey)),//拿到SecurityKey
                    };
                });
            services.AddOcelot(Configuration).AddConsul();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseMvc().UseSwagger().UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/LY.SysService/swagger.json", "LY.SysService");
                c.SwaggerEndpoint("/LY.OrderService/swagger.json", "LY.OrderService");
            });
            app.UseWebSockets();
            app.UseOcelot().Wait();
        }
    }
}