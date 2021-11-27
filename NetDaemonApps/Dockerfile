#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["TuyaIntegrationApp/TuyaIntegrationApp.csproj", "TuyaIntegrationApp/"]
RUN dotnet restore "TuyaIntegrationApp/TuyaIntegrationApp.csproj"
COPY . .
WORKDIR "/src/TuyaIntegrationApp"
RUN dotnet build "TuyaIntegrationApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TuyaIntegrationApp.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TuyaIntegrationApp.dll"]


#
#FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
#WORKDIR /app
#
## Copy csproj and restore as distinct layers
#COPY *.csproj ./
#RUN dotnet restore
#
## Copy everything else and build
#COPY . ./
#RUN dotnet publish -c Release -o out ./daemonapp.csproj
#
## Build runtime image
#FROM mcr.microsoft.com/dotnet/aspnet:6.0
#WORKDIR /app
#COPY --from=build-env /app/out .
#ENTRYPOINT ["dotnet", "daemonapp.dll"]