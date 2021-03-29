# Release Notes

## v1.0.1

#### Bug Fixes

- The daemon no longer stops if the authentication request expired

### Other Notes

- After a loss of connection, the daemon immediately made a new connection attempt. From now on, it will wait 1 minute before any attempt.

## v1.0

### New Features

- The daemon can now run natively on a Debian(x64 only) with systemd
- Add install script for bash system

### Other Notes
- The semantic version management will now be used for the daemon.

