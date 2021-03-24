#!/bin/bash
set -e

if (( $# != 1 ))
then
  echo "Usage: ./build-debian.sh [package_version]"
  echo "Example: ./build-debian.sh 1.0.0"
  exit 1
fi

echo Build Debian package

# Build
cd build
dotnet publish ../src/Accessh.Daemon/Accessh.Daemon.csproj -c Release -o ./app -r linux-x64 --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=true

# Move app
mv app/* ./deb/opt/sh-daemon/
mv ./deb/opt/sh-daemon/config.json ./deb/etc/sh-daemon/config.json

# Cleanup
rm -f deb/opt/sh-daemon/.gitkeep
rm -f deb/etc/sh-daemon/.gitkeep

# Set permission
chmod 755 -R deb

# Create debian package
dpkg-deb --build deb

# Finalize
mv deb.deb shdaemon_$1-1_amd64.deb
rm -rf app/

echo The Debian package has been created
