# Release Notes

## v2.0

### Features

- Complete rewrite of the daemon
- Added ARM support for Docker & Debian 
  - arm64-v8
  - arm32-v7
- The demon no longer has the ability to make a synchronization request. Acces.sh api sends the keys as soon as the connection is successful

### Performance Improvements
- Keys synchronize faster (better file management) 
- The startup is faster after removing several dependencies (1 second gain)

### Bug Fixes
- The Daemon will no longer write duplicate keys

## v1.0.4

### Bug Fixes

- Rollback:~~The daemon tries to reconnect immediately after a connection loss instead of waiting for a minute~~
- The daemon now waits one minute before reconnecting

## v1.0.3

### Bug Fixes

- If the connection failed, the daemon did not make a new attempt. Now, the connection error is correctly reported, and the daemon will continue its attempts.

## v1.0.2

### Enhancement

- The daemon tries to reconnect immediately after a connection loss instead of waiting for a minute

### Other Notes

- Update log messages
- Update api url (Auth & Hub)

## v1.0.1

### Bug Fixes

- The daemon no longer stops if the authentication request expired

### Other Notes

- After a loss of connection, the daemon immediately made a new connection attempt. From now on, it will wait 1 minute before any attempt.

## v1.0

### New Features

- The daemon can now run natively on a Debian(x64 only) with systemd
- Add install script for bash system

### Other Notes
- The semantic version management will now be used for the daemon.

