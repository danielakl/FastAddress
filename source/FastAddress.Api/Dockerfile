FROM mcr.microsoft.com/dotnet/aspnet:10.0.8-alpine3.23 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0.300-alpine3.23 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["source/FastAddress.Api/FastAddress.Api.csproj", "source/FastAddress.Api/"]
COPY ["source/FastAddress.Api.Database/FastAddress.Api.Database.csproj", "source/FastAddress.Api.Database/"]
COPY ["source/FastAddress.Api.Services/FastAddress.Api.Services.csproj", "source/FastAddress.Api.Services/"]
COPY ["source/FastAddress.Api.Vendors/FastAddress.Api.Vendors.csproj", "source/FastAddress.Api.Vendors/"]
COPY ["source/FastAddress.Sdk/FastAddress.Sdk.csproj", "source/FastAddress.Sdk/"]
COPY ["Directory.Build.props", "Directory.Build.props"]
COPY ["Directory.Build.targets", "Directory.Build.targets"]
COPY ["Directory.Packages.props", "Directory.Packages.props"]
COPY ["nuget.config", "nuget.config"]
RUN dotnet restore "source/FastAddress.Api/FastAddress.Api.csproj"
COPY . .
WORKDIR "/src/source/FastAddress.Api"
RUN dotnet build "./FastAddress.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./FastAddress.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FastAddress.Api.dll"]
