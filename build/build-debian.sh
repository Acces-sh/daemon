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

if [[ "$2" == "linux-x64" ]]
then
  dotnet publish ../src/Daemon.Host/Daemon.Host.csproj -c Release -o ./app -r linux-x64 --self-contained true -p:PublishSingleFile=true
  extension="amd64"
elif [[ "$2" == "linux-arm" ]]
then
  dotnet publish ../src/Daemon.Host/Daemon.Host.csproj -c Release -o ./app -r linux-arm --self-contained true -p:PublishSingleFile=true
  extension="armhf"
elif [[ "$2" == "linux-arm64" ]]
then
  dotnet publish ../src/Daemon.Host/Daemon.Host.csproj -c Release -o ./app -r linux-arm64 --self-contained true -p:PublishSingleFile=true
  extension="arm64"
else 
  echo "Incorrect build type"
  exit 1
fi

cp -r ./deb ./deb-build

# Move app
mv app/* ./deb-build/opt/sh-daemon/
mv ./deb-build/opt/sh-daemon/config.json ./deb-build/etc/sh-daemon/config.json

# Update architecture type
sed -i "s/CUSTOM_ARCH/${extension}/g" deb-build/DEBIAN/control

# Cleanup
rm -f deb-build/opt/sh-daemon/.gitkeep
rm -f deb-build/etc/sh-daemon/.gitkeep

# Set permission
chmod 755 -R deb-build

# Create debian package
dpkg-deb --build deb-build

# Finalize
mv deb-build.deb shdaemon_"$1"-1_${extension}.deb

rm -rf deb-build/

cd ..
echo The Debian package has been created
