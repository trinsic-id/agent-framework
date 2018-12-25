# Agent Framework - .NET Core library for Sovrin agents

[![Build Status](https://dev.azure.com/streetcred/Agent%20Framework/_apis/build/status/Agent%20Framework%20-%20Build?branchName=master)](https://dev.azure.com/streetcred/Agent%20Framework/_build/latest?definitionId=10?branchName=master)
[![Build Status](https://travis-ci.com/streetcred-id/agent-framework.svg?branch=master)](https://travis-ci.com/streetcred-id/agent-framework)

Agent Framework is a .NET Core library for building Sovrin interoperable agent services.
It is an abstraction on top of Indy SDK that provides a set of API's for managing agent workflows.
The framework runs .NET Standard (2.0+), including ASP.NET Core and Xamarin.

## Documentation

## Quick demo

```lang=bash
docker-compose up
```

This will create an agent network with a pool and three identical agents able to communicate with each other in the network.
Navigate to [http://localhost:7001](http://localhost:7001), [http://localhost:7002](http://localhost:7001) and [http://localhost:7003](http://localhost:7001) to create and accept connection invitations between the different agents.