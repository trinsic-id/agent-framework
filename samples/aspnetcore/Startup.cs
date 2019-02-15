using System;
using System.IO;
using AgentFramework.AspNetCore.Configuration.Service;
using AgentFramework.AspNetCore.Options;
using Jdenticon.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebAgent.Utils;

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

            // Register agent framework dependency services and handlers
            services.AddAgentFramework(c => c.SetPoolOptions(new PoolOptions { GenesisFilename = Path.GetFullPath("pool_genesis.txn") }));
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

            // Add agent middleware
            var agentBaseUrl = new Uri(Environment.GetEnvironmentVariable("ASPNETCORE_URLS"));
            app.UseAgentFramework<WebAgentMiddleware>($"{new Uri(agentBaseUrl, "/agent")}",
                obj =>
                {
                    obj.AddOwnershipInfo(NameGenerator.GetRandomName(), null);
                });

            // fun identicons
            app.UseJdenticon();
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
