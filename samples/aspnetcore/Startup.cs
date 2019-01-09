using System;
using System.IO;
using AgentFramework.AspNetCore.Configuration.Service;
using AgentFramework.AspNetCore.Options;
using AgentFramework.Core.Models.Wallets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace WebAgent
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
            services.AddMvc();
            services.AddAgent(config =>
            {
                config.SetPoolOptions(new PoolOptions { GenesisFilename = Path.GetFullPath("pool_genesis.txn") });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseAgent($"{Environment.GetEnvironmentVariable("ASPNETCORE_URLS")}/agent",
                (obj) =>
                {
                    obj.AddOwnershipInfo(Environment.GetEnvironmentVariable("AF_OWNER_NAME"), null);
                });

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
