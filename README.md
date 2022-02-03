# Acces.sh daemon

Welcome to the acces.sh daemon repository. Binaries for Debian distributions are distributed in the release section.

## Changelog

The complete list of changes is available [here](CHANGELOG.md)

## Getting started

To build the Daemon you need:

- [.net](https://dotnet.microsoft.com/download) SDK 5.0.1

To start working on the Daemon, you can build the main branch:

1. Clone the repo: `git clone https://github.com/Acces-sh/daemon.git` and cd into.
3. Restore project's dependencies : `dotnet restore`
4. Inside `src\Accessh.Daemon`, edit `config.json` file, update `ApiToken` field with a valid API Key, and
   update `AuthorizedKeyFilePath` with a valid path.
    * Linux & MacOs, use `/` for path ex:`/root/.ssh/authorized_keys`
    * Windows, use `\ ` for path ex:`C:\authorized_keys`
5. Also edit the `appsettings.json` file and update `ConfigurationFilePath` field
6. Build the Daemon with `dotnet build`

### Run

You can run the daemon with:  
`dotnet ./src/Accessh.Daemon/bin/Debug/net5.0/Accessh.Daemon.dll`
