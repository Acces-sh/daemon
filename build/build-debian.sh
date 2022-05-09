#!/bin/bash
set -e

if (( $# != 2 ))
then
  echo "Usage: ./build-debian.sh [package_version] [build_type]"
  echo "Build type: linux-x64, linux-arm, linux-arm64"
  echo "Example: ./build-debian.sh 1.0.0 linux-x64"
  exit 1
fi

echo Build Debian package

extension=""

# Build
cd build

if(($2 == "linux-x64"))
then
  dotnet publish ../src/Accessh.Daemon/Accessh.Daemon.csproj -c Release -o ./app -r linux-x64 --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=true
  extension="amd64"
elif(($2 == "linux-arm"))
then
  dotnet publish ../src/Accessh.Daemon/Accessh.Daemon.csproj -c Release -o ./app -r linux-arm --self-contained true -p:PublishSingleFile=true
  extension="arm"
elif(($2 == "linux-arm64"))
then
  dotnet publish ../src/Accessh.Daemon/Accessh.Daemon.csproj -c Release -o ./app -r linux-arm64 --self-contained true -p:PublishSingleFile=true
  extension="arm64"
else 
  echo "Incorrect build type"
  exit 1
fi

# Move app
mv app/* ./deb/opt/sh-daemon/
mv ./deb/opt/sh-daemon/config.json ./deb/etc/sh-daemon/config.json

# Update architecture type
sed -i "s/CUSTOM_ARCH/${extension}/g" deb/DEBIAN/control

# Cleanup
rm -f deb/opt/sh-daemon/.gitkeep
rm -f deb/etc/sh-daemon/.gitkeep

# Set permission
chmod 755 -R deb

# Create debian package
dpkg-deb --build deb

# Finalize
mv deb.deb shdaemon_"$1"-1_${extension}.deb
rm -rf app/

echo The Debian package has been created
