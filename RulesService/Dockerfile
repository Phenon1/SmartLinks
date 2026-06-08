FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["SmartLinks.Contracts/SmartLinks.Contracts.csproj", "SmartLinks.Contracts/"]
COPY ["RulesService/RulesService.csproj", "RulesService/"]
COPY ["RulePlugins.Core/RulePlugins.Core.csproj", "RulePlugins.Core/"]
RUN dotnet restore "./RulesService/RulesService.csproj"
COPY . .
WORKDIR "/src/RulesService"
RUN dotnet build "./RulesService.csproj" -c $BUILD_CONFIGURATION -o /app/build
WORKDIR "/src/RulePlugins.Core"
RUN dotnet build "./RulePlugins.Core.csproj" -c $BUILD_CONFIGURATION -o /app/core-plugin-build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR "/src/RulesService"
RUN dotnet publish "./RulesService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false
RUN mkdir -p /app/publish/Plugins && cp /app/core-plugin-build/RulePlugins.Core.dll /app/publish/Plugins/

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RulesService.dll"]
