#!/bin/bash
set -e

daemon_version=1.0.1
sudo_cmd=''
restart_cmd="$sudo_cmd systemctl restart sh-daemon"
config_location=/etc/sh-daemon/config.json
file_name_temp=config_daemon.json

echo -e "\033[34m\n* Daemon (v$daemon_version) install script\n\033[0m"

rm -f shdaemon_$daemon_version-1_amd64.deb

echo -e "\033[34m\n* Retrieving the latest version of the daemon\n\033[0m"

wget https://github.com/Acces-sh/daemon/releases/download/v$daemon_version/shdaemon_$daemon_version-1_amd64.deb

# Root user detection
if (( $EUID != 0 )); then
    sudo_cmd='sudo'
fi

echo -e "\033[34m\n* Installing daemon...\n\033[0m"
$sudo_cmd dpkg -i shdaemon_$daemon_version-1_amd64.deb
$sudo_cmd rm -f shdaemon_$daemon_version-1_amd64.deb

echo -e "\033[34m\n* The daemon has been installed.\n\033[0m"

if [[ -z "${API_TOKEN}" ]]; then
  echo -e "\033[33mWarning: No token were provided. You will have to modify the /etc/sh-daemon/config.json file before running daemon.\033[0m"
  echo -e "\033[33mOnce modified, execute the following command: $restart_cmd\033[0m"
else
  echo -e "\033[34m\n* An API key has been provided. Installation of the key.\n\033[0m"

  wget https://github.com/stedolan/jq/releases/download/jq-1.6/jq-linux64
  chmod +x jq-linux64

  ./jq-linux64 '.ApiToken="'${API_TOKEN}'"' $config_location > $file_name_temp
  rm jq-linux64
  $sudo_cmd  mv -f config_daemon.json $config_location
  echo -e "\033[34m\n* The key has been installed.\n\033[0m"

  echo -e "\033[34m\n* Starting the daemon...\n\033[0m"
  eval "$restart_cmd"
fi
