# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:5.0-alpine AS build
WORKDIR /source

# copy csproj and restore as distinct layers
COPY src/Accessh.Daemon/Accessh.Daemon.csproj ./Accessh.Daemon/
COPY src/Accessh.Configuration/Accessh.Configuration.csproj ./Accessh.Configuration/
RUN dotnet restore Accessh.Daemon/Accessh.Daemon.csproj -r linux-musl-x64

# copy and publish app and libraries
COPY . .
RUN dotnet publish -c release -o /app -r linux-musl-x64 --self-contained true /p:PublishTrimmed=false /p:PublishReadyToRun=true

# final stage/image
FROM mcr.microsoft.com/dotnet/runtime:5.0-alpine-amd64
WORKDIR /app
COPY --from=build /app .
RUN touch /app/authorized_keys
ENTRYPOINT ["dotnet","Accessh.Daemon.dll"]
