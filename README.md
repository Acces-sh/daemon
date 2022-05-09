# Acces.sh daemon

Welcome to the acces.sh daemon repository. Binaries for Debian distributions are distributed in the release section.

## Changelog

The complete list of changes is available [here](CHANGELOG.md)

## Getting started

To build the Daemon you need:

- [.net](https://dotnet.microsoft.com/download) SDK 6.0.0

To start working on the Daemon, you can build the main branch:

1. Clone the repo: `git clone https://github.com/Acces-sh/daemon.git` and cd into.
2. Restore project's dependencies : `dotnet restore`
3. Inside `src\Daemon.Host`, edit `config.json` file, update `ApiToken` field with a valid API Key, and
   update `AuthorizedKeyFilePath` with a valid path.
    * Linux & MacOs, use `/` for path ex:`/root/.ssh/authorized_keys`
    * Windows, use `\ ` for path ex:`C:\authorized_keys`
4. Also edit the `Configurations/core.json` file and update `ConfigurationFilePath` field with the path of build  
` "ConfigurationFilePath": "ABSOLUTE_PATH_OF_PROJECT/src/Daemon.Host/bin/Debug/net6.0/"`
6. Build the Daemon with `dotnet build`
7. Run Daemon with `dotnet run`


## Run with Docker compose

```
version: "3.9"
services:
  daemon:
    image: ghcr.io/acces-sh/daemon
    volumes:
      - "/root/.ssh/authorized_keys:/app/authorized_keys/"
    environment:
      API_TOKEN: "YOUR_TOKEN"
```
