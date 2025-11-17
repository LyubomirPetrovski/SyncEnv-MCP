# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 5000
ENV ASPNETCORE_URLS=http://0.0.0.0:5000

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["SyncEnv.Mcp.Http/SyncEnv.Mcp.Http.csproj", "SyncEnv.Mcp.Http/"]
COPY ["MongoDAL/MongoDAL.csproj", "MongoDAL/"]
COPY ["SyncEnv.Mcp/SyncEnv.Mcp.csproj", "SyncEnv.Mcp/"]
RUN dotnet restore "SyncEnv.Mcp.Http/SyncEnv.Mcp.Http.csproj"
COPY . .
WORKDIR "/src/SyncEnv.Mcp.Http"
RUN dotnet build "SyncEnv.Mcp.Http.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "SyncEnv.Mcp.Http.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://0.0.0.0:5000
ENTRYPOINT ["dotnet", "SyncEnv.Mcp.Http.dll"]