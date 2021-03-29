### Bug Fixes

- The daemon no longer stops if the authentication request expired

### Other Notes

- After a loss of connection, the daemon immediately made a new connection attempt. From now on, it will wait 1 minute before any attempt.
