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
