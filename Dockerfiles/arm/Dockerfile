# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS build
WORKDIR /source

# copy csproj and restore as distinct layers
COPY src/Daemon.Host/Daemon.Host.csproj ./Daemon.Host/
COPY src/Daemon.Application/Daemon.Application.csproj ./Daemon.Application/
RUN dotnet restore Daemon.Host/Daemon.Host.csproj -r linux-musl-x64

# copy and publish app and libraries
COPY . .
RUN rm src/Daemon.Host/Configurations/core.json
RUN mv src/Daemon.Host/Configurations/core.docker.json src/Daemon.Host/Configurations/core.json
RUN dotnet publish -c release -o /app -r linux-arm --self-contained false /p:PublishTrimmed=false

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine-arm32v7
WORKDIR /app
COPY --from=build /app .
RUN touch /app/authorized_keys
ENV DOCKER true
ENTRYPOINT ["dotnet","Daemon.Host.dll"]
