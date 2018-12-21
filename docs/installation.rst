
******************************
Installation and configuration
******************************

Using NuGet
===========

.. code-block:: bash

    Install-Package AgentFramework.Core -Source https://www.myget.org/F/agent-framework/api/v3/index.json


The framework will be moved to nuget.org soon. For the time being, stable and pre-release packages are available at ``https://www.myget.org/F/agent-framework/api/v3/index.json``.
You can add `nuget.config
<nuget.config>`_ anywhere in your project path with the myget.org repo.

.. code-block:: xml

    <?xml version="1.0" encoding="utf-8"?>
    <configuration>
        <packageSources>
            <add key="myget.org" value="https://www.myget.org/F/agent-framework/api/v3/index.json" />
            <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
        </packageSources>
    </configuration>

Setting up development environment
==================================

Agent Framework uses Indy SDK wrapper for .NET which requires platform specific native libraries of libindy to be available in the running environment.
Check the [Indy SDK project page](https://github.com/hyperledger/indy-sdk) for details on installing libindy for different platforms or read the brief instructions below.

Make sure you have [.NET Core SDK](https://dotnet.microsoft.com/download) installed for your platform.

Windows
-------

You can download binaries of libindy and all dependencies from the [Sovrin repo](https://repo.sovrin.org/windows/libindy/). The dependcies are under `deps` folder and `libindy` under one of streams (rc, master, stable). There are two options to link the DLLs

- Unzip all files in a directory and add that to your PATH variable (recommended for development)
- Or copy all DLL files in the publish directory (recommended for published deployments)

More details at the [Indy documentation for setting up Windows environment](https://github.com/hyperledger/indy-sdk/blob/master/doc/windows-build.md)

MacOS
-----

Check `Setup Indy SDK build environment for MacOS
<https://github.com/hyperledger/indy-sdk/blob/master/doc/mac-build.md>`_

Linux
-----

Build instructions for [Ubuntu based distros](https://github.com/hyperledger/indy-sdk/blob/master/doc/ubuntu-build.md) and [RHEL
based distros](https://github.com/hyperledger/indy-sdk/blob/master/doc/rhel-build.md).

Hosting and configuring agent services for production
=====================================================

- [ASP.NET Core](web-agents-aspnetcore.md)
- [Xamarin](xamarin-mobile.md)
- [Hosting agents in Docker container](docker-agents.md)

Introduction to Agent Framework Core
====================================

Before you begin reading any of the topics below, please familiarize youself with the core idea behind Hyperledger Indy.
We strongly suggest that you go over the [Indy SDK Getting Started Guide](https://github.com/hyperledger/indy-sdk/blob/master/doc/getting-started/getting-started.md) first.

- Connections - establishing secure peer to peer connection
- Working with schemas and definitions
- Credentials - issuing credentials
- Proofs - credential attestations and verifications
- Notes on revocation and tails services

Samples
=======

Running the example
-------------------

.. code-block:: bash

    docker-compose up

This will create an agent network with a pool and three identical agents able to communicate with each other in the network.
Navigate to [http://localhost:7001](), [http://localhost:7002]() and [http://localhost:7003]() to create and accept connection invitations between the different agents.

### Running the unit tests

```lang=bash
docker-compose -f docker-compose.test.yaml up --build --remove-orphans --abort-on-container-exit --exit-code-from test-agent
```

Note: You may need to cleanup previous docker network created using `docker network prune`
