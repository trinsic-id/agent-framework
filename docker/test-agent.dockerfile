FROM streetcred/dotnet-indy:1.6.7 
WORKDIR /app

COPY . .
RUN dotnet restore "AgentFramework.sln"

COPY docker/docker_pool_genesis.txn test/AgentFramework.Core.Tests/pool_genesis.txn

ENTRYPOINT ["dotnet", "test", "--verbosity", "normal", "--no-restore"]