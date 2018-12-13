# AgentFramework

| Stage | Status |
| --- | --- |
| Build | [![Build status](https://streetcred.visualstudio.com/Streetcred/_apis/build/status/SDK/SDK%20(Compile%20Only))](https://streetcred.visualstudio.com/Streetcred/_build/latest?definitionId=7) |
| Package | [![Build status](https://streetcred.visualstudio.com/Streetcred/_apis/build/status/SDK/SDK%20(Build%20Package%20&%20Publish))](https://streetcred.visualstudio.com/Streetcred/_build/latest?definitionId=2) |
| Unit Tests | [![Build Status](https://travis-ci.com/streetcred-id/agent-framework.svg?branch=master)](https://travis-ci.com/streetcred-id/agent-framework) |
---

## Overview

AgentFramework is a .NET Core library for building Sovrin interoperable agent services.
The framework runs on any .NET Standard target, including ASP.NET Core and Xamarin.

## Installation

```bash
PM> Install-Package AgentFramework.Core -Source https://www.myget.org/F/agent-framework/api/v3/index.json
```

The framework will be moved to nuget.org soon. For the time being, stable and pre-release packages are available at `https://www.myget.org/F/agent-framework/api/v3/index.json`.
You can add [nuget.config](nuget.config) anywhere in your project path with the myget.org repo.

## Setting up development environment

Agent Framework uses Indy SDK wrapper for .NET which requires platform specific native libraries of libindy to be available in the running environment.
Check the [Indy SDK project page](https://github.com/hyperledger/indy-sdk) for details on installing libindy for different platforms or read the brief instructions below.

Make sure you have [.NET Core SDK](https://dotnet.microsoft.com/download) installed for your platform.

### Windows

You can download binaries of libindy and all dependncies from the [Sovrin repo](https://repo.sovrin.org/windows/libindy/). The dependcies are under `deps` folder and `libindy` under one of streams (rc, master, stable). There are two options to link the DLLs

- Unzip all files in a directory and add that to your PATH variable (recommended for development)
- Or copy all DLL files in the publish directory (recommended for published deployments)

More details at the [Indy documentation for setting up Windows environment](https://github.com/hyperledger/indy-sdk/blob/master/doc/windows-build.md)

### MacOS

Check [Setup Indy SDK build environment for MacOS](https://github.com/hyperledger/indy-sdk/blob/master/doc/mac-build.md)

### Linux

Build instructions for [Ubuntu based distros](https://github.com/hyperledger/indy-sdk/blob/master/doc/ubuntu-build.md) and [RHEL
based distros](https://github.com/hyperledger/indy-sdk/blob/master/doc/rhel-build.md).

## Hosting and configuring agent services for production

- [ASP.NET Core](docs/web-agents-aspnetcore.md)
- [Xamarin](docs/xamarin-mobile.md)
- [Hosting agents in Docker container](docs/docker-agents.md)

## Introduction to Agent Framework Core

Before you begin reading any of the topics below, please familiarize youself with the core idea behind Hyperledger Indy.
We strongly suggest that you go over the [Indy SDK Getting Started Guide](https://github.com/hyperledger/indy-sdk/blob/master/doc/getting-started/getting-started.md) first.

- Connections - establishing secure peer to peer connection
- Working with schemas and definitions
- Credentials - issuing credentials
- Proofs - credential attestations and verifications
- Notes on revocation and tails services

## Samples

### Running the example

```lang=bash
docker-compose up
```

This will create an agent network with a pool and three identical agents able to communicate with each other in the network.
Navigate to [http://localhost:7001](), [http://localhost:7002]() and [http://localhost:7003]() to create and accept connection invitations between the different agents.

### Running the unit tests

```lang=bash
docker-compose -f docker-compose.test.yaml up --build --remove-orphans --abort-on-container-exit --exit-code-from test-agent
```

Note: You may need to cleanup previous docker network created using `docker network prune`
