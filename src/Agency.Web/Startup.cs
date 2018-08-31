using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Agency.Web.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Streetcred.Sdk.Extensions;
using Streetcred.Sdk.Model.Wallets;

namespace Agency.Web
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
            services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace).AddConsole());

            services.AddAgent();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();

            app.UseAgent(options =>
            {
                options
                    .AsPublicAgent("http://localhost:5000")
                    .WithPool("DefaultPool", "pool_genesis.txn")
                    .WithWallet(WalletUtils.Configuration, WalletUtils.Credentials);
            });
        }
    }
}
