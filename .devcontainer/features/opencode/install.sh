#!/bin/bash
set -euo pipefail

# Install OpenCode 2 from its v2 installer. The original https://opencode.ai/install
# installs OpenCode 1 ONLY: it refuses any 2.x --version and skips GitHub's latest
# release because that may be OpenCode 2 (whose npm package is @opencode/cli, not
# opencode-ai). The v2 installer uses the same ~/.opencode/bin, so the PATH entry and
# the ~/.local/bin launcher written at create are unchanged. `opencode upgrade` on
# create then stays within OpenCode 2.
su - "${_REMOTE_USER}" -c "curl -fsSL https://opencode.ai/v2/install | bash"
