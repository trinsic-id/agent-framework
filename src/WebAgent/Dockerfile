FROM streetcred/dotnet-indy:latest AS base
WORKDIR /app

FROM streetcred/dotnet-indy:latest AS build
WORKDIR /src
COPY ["src/WebAgent/WebAgent.csproj", "WebAgent/"]
RUN dotnet restore "WebAgent/WebAgent.csproj" \
    -s "https://api.nuget.org/v3/index.json" \
    -s "https://www.myget.org/F/streetcred-sdk/api/v3/index.json"
COPY src/ .
WORKDIR "/src/WebAgent"
RUN dotnet build "WebAgent.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "WebAgent.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ARG PORT
EXPOSE ${PORT}
ENV ASPNETCORE_URLS "http://127.0.0.1:${PORT}"
ENTRYPOINT ["dotnet", "WebAgent.dll"]