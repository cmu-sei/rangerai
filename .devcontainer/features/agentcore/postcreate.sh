#!/bin/bash
## Set up the AgentCore CLI
#
# Everything AgentCore needs at create time lives here, so a devcontainer.json that
# lists "./features/agentcore" gets a working CLI with NOTHING added to the project's
# own postcreate.sh. Declared as this feature's postCreateCommand in
# devcontainer-feature.json; the workspace folder is the cwd. The profile-derived
# wrapper and agent skill are written by poststart.sh (see there for why).
set -euo pipefail

TARGET=agentcore

# --- Persistent data volume ---
# ~/.agentcore holds secrets.key, the key that encrypts API keys in every AgentCore
# project's agentcore/.env.local. Losing it on a rebuild makes those values
# undecryptable, so it lives on the volume. AGENTCORE_DISABLE_KEYCHAIN (containerEnv)
# keeps the key in that file rather than a session keyring that would not survive.
# The volume mounts root-owned on first create; the chown is idempotent and every
# feature that touches ~/.data does it, since any of them may run first.
DATA="$HOME/.data"
sudo chown "$(id -u):$(id -g)" "$DATA"
mkdir -p "$DATA/$TARGET"

vol_dir="$DATA/$TARGET"
home_dir="$HOME/.$TARGET"
if [ -d "$home_dir" ] && [ ! -L "$home_dir" ]; then
    if [ -z "$(ls -A "$vol_dir" 2>/dev/null)" ]; then
        cp -a "$home_dir/." "$vol_dir/"
    fi
    rm -rf "$home_dir"
fi
ln -sfn "$vol_dir" "$home_dir"

# Update to the latest version (runs on every create, bypassing the image cache).
# --json keeps it non-interactive: without flags in a terminal, update opens its TUI.
echo "Updating AgentCore CLI..."
agentcore update --json >/dev/null || true
