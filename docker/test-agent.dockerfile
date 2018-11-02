FROM streetcred/dotnet-indy:1.6.7 
WORKDIR /app

COPY . .
RUN dotnet restore "test/Streetcred.Sdk.Tests/Streetcred.Sdk.Tests.csproj"

COPY docker/docker_pool_genesis.txn test/Streetcred.Sdk.Tests/pool_genesis.txn

WORKDIR /app/test/Streetcred.Sdk.Tests

ENTRYPOINT ["dotnet", "test", "--verbosity", "normal"]