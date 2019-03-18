using System;
using AgentFramework.AspNetCore.Configuration.Service;
using Jdenticon.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebAgent.Messages;
using WebAgent.Protocols.BasicMessage;
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

            services.AddLogging();

            // Register agent framework dependency services and handlers
            services.AddAgentFramework();

            services.AddSingleton<BasicMessageHandler>();
            services.AddSingleton<TrustPingMessageHandler>();
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
            var agentBaseUrl = Environment.GetEnvironmentVariable("ENDPOINT_HOST") ?? Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
            var agentName = Environment.GetEnvironmentVariable("AGENT_NAME") ?? NameGenerator.GetRandomName();

            app.UseAgentFramework<WebAgentMiddleware>($"{new Uri(new Uri(agentBaseUrl), "/agent")}",
                obj =>
                {
                    obj.AddOwnershipInfo(agentName, null);
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
