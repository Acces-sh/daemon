# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine-amd64 AS build
WORKDIR /source

# copy csproj and restore as distinct layers
COPY src/Daemon.Host/Daemon.Host.csproj ./Daemon.Host/
COPY src/Daemon.Application/Daemon.Application.csproj ./Daemon.Application/
RUN dotnet restore Daemon.Host/Daemon.Host.csproj

# copy and publish app and libraries
ARG TARGETPLATFORM
COPY . .
RUN rm src/Daemon.Host/Configurations/core.json
RUN mv src/Daemon.Host/Configurations/core.docker.json src/Daemon.Host/Configurations/core.json
RUN if [ "$TARGETPLATFORM" = "linux/amd64" ]; then \
    RID=linux-x64 ; \
    elif [ "$TARGETPLATFORM" = "linux/arm64" ]; then \
    RID=linux-arm64 ; \
    elif [ "$TARGETPLATFORM" = "linux/arm/v7" ]; then \
    RID=linux-arm ; \
    fi \
    && dotnet publish -c release -o /app -r $RID --self-contained false /p:PublishReadyToRun=false


# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine
WORKDIR /app
COPY --from=build /app .
RUN touch /app/authorized_keys
ENV DOCKER true
ENV ARCH $TARGETPLATFORM
ENTRYPOINT ["dotnet","Daemon.Host.dll"]
