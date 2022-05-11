#!/bin/bash
set -e

daemon_version=2.0.0
sudo_cmd=''
restart_cmd="$sudo_cmd systemctl restart sh-daemon"
config_location=/etc/sh-daemon/config.json
arch=$(dpkg --print-architecture)
arch_supported=("amd64" "armhf" "arm64")

echo -e "* Acces.sh Daemon install script"
echo -e "* Version $daemon_version"
echo -e "* Current architecture : $arch \n"
	
## Root user detection
if (( $EUID != 0 )); then
    sudo_cmd='sudo'
fi

if [[ ${arch_supported[*]} -ne $arch ]]
then
  echo -e "\e[31mThe current architecture isn't supported\e[0m"
  exit 1;
fi

echo -e "\e[36m# Retrieving the latest version of the daemon\e[0m"

rm -f shdaemon_$daemon_version-1_"$arch".deb

curl -O -J -L https://github.com/Acces-sh/daemon-test/releases/download/v$daemon_version/shdaemon_$daemon_version-1_$arch.deb

echo -e "\e[36m# Installing daemon...\e[0m"

$sudo_cmd dpkg -i shdaemon_$daemon_version-1_"$arch".deb
$sudo_cmd rm -f shdaemon_$daemon_version-1_"$arch".deb

echo -e "\e[36m# The daemon has been installed\e[0m"

if [[ -z "${API_TOKEN}" ]]; then
  echo -e "\033[33mWarning: No token were provided. You will have to modify the /etc/sh-daemon/config.json file before running daemon.\033[0m"
  echo -e "\033[33mOnce modified, execute the following command: $restart_cmd\033[0m"
else
  echo -e "\033[36m\n* An API key has been provided. Installation of the key.\e[0m"

  echo -e $sudo_cmd sed -i "s/API_TOKEN/${API_TOKEN}/g" $config_location

  $sudo_cmd sed -i "s/API_TOKEN/${API_TOKEN}/g" $config_location
  
  echo -e "\033[36m\n* The key has been installed.\e[0m"

  echo -e "\033[34m\n* Starting the daemon...\n\033[0m"
  eval "$restart_cmd"
fi

printf -- '\n';
exit 0;

