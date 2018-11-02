Streetcred SDK

[![Build status](https://streetcred.visualstudio.com/Streetcred/_apis/build/status/Streetcred-SDK-CI)](https://streetcred.visualstudio.com/Streetcred/_build/latest?definitionId=2)

# Streetcred SDK
## Agent Framework for building Sovrin agent services w/ .NET Core

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
docker-compose -f docker-compose.test.yaml run test-agent
```

Note: You may need to cleanup previous docker network created using `docker network prune`