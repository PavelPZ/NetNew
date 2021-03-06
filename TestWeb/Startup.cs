﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Http;
using Microsoft.Extensions.DependencyInjection;

namespace TestWeb {
  public class Startup {
    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
    public void ConfigureServices(IServiceCollection services) {
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app) {
      app.UseIISPlatformHandler();

      var str = LMNetLib.LowUtils.bytesToString(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 });
      //var ctx = NewData.Container.CreateContext();
      //var cnt = ctx.Companies.Count();
      //ctx = null;

      app.Run(async (context) => {
        await context.Response.WriteAsync("Hello World! " + str.ToString());
      });
    }

    // Entry point for the application.
    public static void Main(string[] args) => WebApplication.Run<Startup>(args);
  }
}
