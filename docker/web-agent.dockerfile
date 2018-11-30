FROM streetcred/dotnet-indy:latest AS base
WORKDIR /app

FROM streetcred/dotnet-indy:latest AS build
WORKDIR /src
COPY ["samples/aspnetcore-sample/WebAgent.csproj", "."]
RUN dotnet restore "WebAgent.csproj" \
    -s "https://api.nuget.org/v3/index.json" \
    -s "https://www.myget.org/F/streetcred-sdk/api/v3/index.json"
COPY ["samples/aspnetcore-sample/", "."]
COPY ["docker/docker_pool_genesis.txn", "./pool_genesis.txn"]
RUN dotnet build "WebAgent.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "WebAgent.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ARG AGENT_IP=127.0.0.1
ARG AGENT_PORT=7001
EXPOSE ${AGENT_PORT}
ENV ASPNETCORE_URLS "http://${AGENT_IP}:${AGENT_PORT}"
ENTRYPOINT ["dotnet", "WebAgent.dll"]