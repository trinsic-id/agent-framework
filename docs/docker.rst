***********************************
Hosting agents in docker containers
***********************************

Hosting agents in docker container is the easiest way to ensure your running environment has all dependencies required by the framework.
We provide images with libindy and dotnet-sdk preinstalled.

## Usage

```lang-docker
FROM streetcred/dotnet-indy:latest
```

The images are based on `ubuntu:16.04`. You can check [the docker repo](https://github.com/streetcred-id/docker) if you want to build your own image or require specific version of .NET Core or libindy.

## Example build

Our [web agent docker file](../docker/web-agent.dockerfile) is an example of building and running ASP.NET Core project inside docker container with libindy support.