FROM mcr.microsoft.com/dotnet/aspnet:10.0.8-alpine3.23 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0.300-alpine3.23 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["source/FastAddress.Web/FastAddress.Web.csproj", "source/FastAddress.Web/"]
COPY ["source/FastAddress.Sdk/FastAddress.Sdk.csproj", "source/FastAddress.Sdk/"]
COPY ["Directory.Build.props", "Directory.Build.props"]
COPY ["Directory.Build.targets", "Directory.Build.targets"]
COPY ["Directory.Packages.props", "Directory.Packages.props"]
COPY ["nuget.config", "nuget.config"]
RUN dotnet restore "source/FastAddress.Web/FastAddress.Web.csproj"
COPY . .
WORKDIR "/src/source/FastAddress.Web"
# Alpine SDK image is musl-based — select the matching Tailwind standalone CLI asset.
RUN dotnet build "./FastAddress.Web.csproj" -c $BUILD_CONFIGURATION -o /app/build -p:TailwindAsset=tailwindcss-linux-x64-musl

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./FastAddress.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false -p:TailwindAsset=tailwindcss-linux-x64-musl

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FastAddress.Web.dll"]
