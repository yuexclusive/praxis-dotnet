﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using LY.Common.Utils;
using LY.DTO;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LY.DaemonService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            MQUtil.Start();
            MQUtil.Subscrib<TestMQDTO>(x =>
            {
                BackgroundJob.Schedule(() => Console.WriteLine(x.Name), TimeSpan.FromSeconds(10));
            }, "test");
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}