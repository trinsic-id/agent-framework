[![Build Status](https://dev.azure.com/streetcred/Agent%20Framework/_apis/build/status/Agent%20Framework%20-%20Build?branchName=master)](https://dev.azure.com/streetcred/Agent%20Framework/_build/latest?definitionId=10?branchName=master)
[![Build Status](https://travis-ci.com/streetcred-id/agent-framework.svg?branch=master)](https://travis-ci.com/streetcred-id/agent-framework)
[![MyGet](https://img.shields.io/myget/agent-framework/v/AgentFramework.Core.svg)](https://www.myget.org/feed/agent-framework/package/nuget/AgentFramework.Core)

# AgentFramework
## .NET Core tools for building Sovrin agent services

### Contents
- TODO

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
