FROM streetcred/dotnet-indy:latest AS base
WORKDIR /app

FROM streetcred/dotnet-indy:latest AS build
WORKDIR /src
COPY ["samples/aspnetcore", "samples/aspnetcore"]
COPY ["src/AgentFramework.AspNetCore", "src/AgentFramework.AspNetCore"]
COPY ["src/AgentFramework.Core", "src/AgentFramework.Core"]
COPY ["src/AgentFramework.Core.Handlers", "src/AgentFramework.Core.Handlers"]
RUN dotnet restore "samples/aspnetcore/WebAgent.csproj" \
    -s "https://api.nuget.org/v3/index.json" \
    -s "https://www.myget.org/F/agent-framework/api/v3/index.json"
COPY ["docker/docker_pool_genesis.txn", "./pool_genesis.txn"]
RUN dotnet build "samples/aspnetcore/WebAgent.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "samples/aspnetcore/WebAgent.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .

ENTRYPOINT ["dotnet", "WebAgent.dll"]