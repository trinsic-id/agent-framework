*********************
Getting Started Guide
*********************

Overview
=========

This getting started guide will show you how to help alice get and present a credential. 


Setting up environment
======================
Install visual Studio
Install dotnet sdk 2.2

Create Project
==============

Open Visual Studio and select new project, then choose Web Application (Model-View-Controller):

.. image:: _static/images/choose_template.png
   :width: 500

Select the .NET Core 2.2, then name your project. I've named mine MyAgent:

.. image:: _static/images/configure_agent.png
   :width: 500


Install Required Packages
=========================

Follow the instructions in installation.rst to install the AgentFramework.Core package into your project. 

Continue with the instructions to build libindy and move it into your PATH


Configure your Agent
====================

The ``Startup.cs`` and ``Program.cs`` files control how the agent begins. 

Our first goal is to create the Startup Script. Copy and paste the below code into your ``Startup.cs`` file: 

.. container:: toggle
    
    .. container:: header

      **Startup.cs (Click to show)**

    .. code-block:: C#
       :linenos:

        using System;
        using Microsoft.AspNetCore.Builder;
        using Microsoft.AspNetCore.Hosting;
        using Microsoft.Extensions.Configuration;
        using Microsoft.Extensions.DependencyInjection;
        using AgentFramework.AspNetCore;
        using MyAgent.Utils;
        using AgentFramework.Core.Models.Wallets;
        using Jdenticon.AspNetCore;


        namespace MyAgent
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
                    services.AddAgentFramework(builder =>
                    {
                        builder.AddBasicAgent<SimpleWebAgent>(c =>
                        {
                            c.OwnerName = Environment.GetEnvironmentVariable("AGENT_NAME") ?? NameGenerator.GetRandomName();
                            c.EndpointUri = new Uri(Environment.GetEnvironmentVariable("ENDPOINT_HOST") ?? Environment.GetEnvironmentVariable("ASPNETCORE_URLS"));
                            //c.WalletConfiguration = new WalletConfiguration { Id = "WebAgentWallet" };
                            //c.WalletCredentials = new WalletCredentials { Key = "MyWalletKey" };
                        });
                    });

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
                        app.UseExceptionHandler("/Home/Error");
                        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                    }

                    app.UseStaticFiles();

                    // add the agent middleware
                    app.UseAgentFramework();

                    // add randome picture 
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


Next, we will arrange the ``Program.cs`` file: 

.. container:: toggle
    
    .. container:: header

      **Program.cs (Click to show)**

    .. code-block:: C#
       :linenos:

        using Microsoft.AspNetCore;
        using Microsoft.AspNetCore.Hosting;

        namespace MyAgent
        {
            public class Program
            {
                public static void Main(string[] args)
                {
                    BuildWebHost(args).Run();
                }

                public static IWebHost BuildWebHost(string[] args) =>
                    WebHost.CreateDefaultBuilder(args)
                        .UseKestrel()
                        .UseStartup<Startup>()
                        .Build();
            }
        }


Now create a file name ``SimpleWebAgent.cs`` in the main directory

This file will inherit from the AgentBase class in the AgentFramework, and it extends the IAgent Interface. 
This interface includes only one function named ``Task<MessageResponse>ProcessAsync(IAgentContext context, MessageContext messageContext)``
This will process any message that is sent to the agent's endpoint. 

Copy and paste the below code into the file:

.. container:: toggle
    
    .. container:: header

      **SimpleWebAgent.cs (Click to show)**

    .. code-block:: C#
       :linenos:

        using System;
        using AgentFramework.Core.Handlers;
        using WebAgent.Messages;
        using WebAgent.Protocols.BasicMessage;

        namespace MyAgent
        {
            public class SimpleWebAgent : AgentBase
            {
                public SimpleWebAgent(IServiceProvider serviceProvider)
                    : base(serviceProvider)
                { }

                protected override void ConfigureHandlers()
                {
                    AddConnectionHandler();
                    AddForwardHandler();
                    AddHandler<BasicMessageHandler>();
                    AddHandler<TrustPingMessageHandler>();
                }
            }
        }

Create a bundleconfig.json file in your project root directory, and add this json array to it: 

.. container:: toggle
    
    .. container:: header

      **bundleconfig.json** 

    .. code-block:: javascript
       :linenos:

        // Configure bundling and minification for the project.
        // More info at https://go.microsoft.com/fwlink/?LinkId=808241
        [
        {
            "outputFileName": "wwwroot/css/site.min.css",
            // An array of relative input file paths. Globbing patterns supported
            "inputFiles": [
            "wwwroot/css/site.css"
            ]
        },
        {
            "outputFileName": "wwwroot/js/site.min.js",
            "inputFiles": [
            "wwwroot/js/site.js"
            ],
            // Optionally specify minification options
            "minify": {
            "enabled": true,
            "renameLocals": true
            },
            // Optionally generate .map file
            "sourceMap": false
        }
        ]

Finally, edit the ``Property/launchSettings.json`` 

.. container:: toggle
    
    .. container:: header

      **launchSettings.json**

    .. code-block:: json
       :linenos:

        {
        "profiles": {
            "workshop_agent": {
            "commandName": "Project",
            "launchBrowser": true,
            "environmentVariables": {
                "ASPNETCORE_ENVIRONMENT": "Development"
            },
            "applicationUrl": "http://localhost:5000/"
            }
        }
        }


Click run, you should see your agent appear in your web browser at http://localhost:5000 !






