#!/bin/bash
set -e

echo -> Build Debian package

# Build
dotnet publish ../src/Accessh.Daemon/Accessh.Daemon.csproj -c Release -o ./app -r linux-x64 --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=true

# Move app
mv app/* ./deb/opt/sh-daemon/
mv ./deb/opt/sh-daemon/config.json ./deb/etc/sh-daemon/config.json

# Cleanup
rm -f deb/opt/sh-daemon/.gitkeep
rm -f deb/etc/sh-daemon/.gitkeep

# Create debian package
dpkg-deb --build deb

# Finalize
mv deb.deb shdaemon_1.0-1_amd64.deb
rm -rf app/
