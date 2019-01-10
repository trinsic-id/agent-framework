using System;
using System.IO;
using AgentFramework.AspNetCore.Configuration.Service;
using AgentFramework.AspNetCore.Options;
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

            var agentBaseUrl = new Uri(Environment.GetEnvironmentVariable("ASPNETCORE_URLS"));

            app.UseAgent($"{new Uri(agentBaseUrl, "/agent")}",
                obj =>
                {
                    obj.AddOwnershipInfo(NameGenerator.GetRandomName(), 
                        $"{new Uri(agentBaseUrl, "/images/profile.png")}");
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
