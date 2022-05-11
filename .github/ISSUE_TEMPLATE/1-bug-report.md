---
name: 🐞 Daemon -  Bug report
description: Report a bug on Windows Subsystem for Linux
title: "[Bug]"
labels: bug
body:
- type: markdown
  attributes:
  value: |
  Please [search for existing issues](https://github.com/Acces-sh/daemon/issues) before creating a new one.
  If you'd like to create a new bug report, please [read the guidance here](https://github.com/Acces-sh/daemon/blob/master/CONTRIBUTING.md) before getting started. This will save you time.

- type: input
  attributes:
  label: Version
  description: |
  Docker : Please run `docker image inspect ghcr.io/acces-sh/daemon-test` and check `"org.opencontainers.image.version":"X.X.X"`
  Linux : Please run `cat /etc/sh-daemon/Configurations/core.json` and check `"Version": "2.0.0",`
  placeholder: "2.0.0"
  validations:
  required: true

- type: checkboxes
  attributes:
  label: Mode
  description: |
  Tell us whether the issue is on WSL 2 and/or WSL 1. You can tell your WSL version by running `wsl -l -v`.
  options:
  - label: "Docker image"
  - label: "Linux package"

- type: input
  attributes:
  label: Host OS Version
  description: |
  Please tell us what distro you are using, not needed for docker.
  You can get additional information about the version where possible, e.g. on Debian / Ubuntu, run `lsb_release -r`
  placeholder: "Ubuntu 22.04"
  validations:
  required: false

- type: textarea
  attributes:
  label: Other Software
  description: If you're reporting a bug involving WSL's interaction with other applications, please tell us. What applications? What versions?
  placeholder: |
  Docker Desktop (Windows), version 3.2.2
  traceroute, Version: 1:2.0.21-1
  Visual Studio Code 1.54.3 with Remote-WSL Extension 0.54.6
  MyCustomApplication
  validations:
  required: false

- type: textarea
  attributes:
  label: Repro Steps
  description: Please list out the steps to reproduce your bug.  
  placeholder: Your steps go here. Include relevant environmental variables or any other configuration.
  validations:
  required: true

- type: textarea
  attributes:
  label: Expected Behavior
  description: What were you expecting to see? Include any relevant examples or documentation links.
  placeholder: If you want to include screenshots, paste them into the text area or follow up with a separate comment.
  validations:
  required: true

- type: textarea
  attributes:
  label: Actual Behavior
  description: What happened instead?
  placeholder: Include the terminal output, straces of the failing command, etc. as necessary.
  validations:
  required: true

- type: textarea
  attributes:
  label: Diagnostic Logs
  description: |
  Please provide additional diagnostics if needed.
  e.g : standard output, any other information
  validations:
  required: false
