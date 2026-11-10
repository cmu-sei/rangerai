#!/bin/bash
## Configure OpenCode
#
# Everything OpenCode needs at create time lives here, so a devcontainer.json that
# lists "./features/opencode" gets a working agent with NOTHING added to the project's
# own postcreate.sh. Declared as this feature's postCreateCommand in
# devcontainer-feature.json; the workspace folder is the cwd.
set -euo pipefail

# .devcontainer/, two levels up from .devcontainer/features/opencode/.
DEVCONTAINER_DIR="$(cd "$(dirname "$0")/../.." && pwd)"

# --- Persistent data volume ---
# OpenCode keeps its data in two XDG directories rather than one ~/.opencode, so both
# are redirected onto the ~/.data volume: sessions and data (share), plus model
# selection and prompt history (state). Config (~/.config/opencode) is deliberately
# NOT persisted — it is regenerated from the profiles below on every create.
#
# The volume mounts root-owned on first create; the chown is idempotent and every
# feature that touches ~/.data does it, since any of them may run first.
DATA="$HOME/.data"
sudo chown "$(id -u):$(id -g)" "$DATA"
mkdir -p "$DATA/opencode" "$DATA/opencode-state"

# --- Symlink the data directories into the persistent volume ---
# If the volume directory is empty and the home directory has content from the feature
# installer, seed the volume with those files before symlinking.
link_to_volume() {
    local home_dir="$1"
    local vol_dir="$2"
    if [ -d "$home_dir" ] && [ ! -L "$home_dir" ]; then
        if [ -z "$(ls -A "$vol_dir" 2>/dev/null)" ]; then
            cp -a "$home_dir/." "$vol_dir/"
        fi
        rm -rf "$home_dir"
    fi
    mkdir -p "$(dirname "$home_dir")"
    ln -sfn "$vol_dir" "$home_dir"
}

link_to_volume "$HOME/.local/share/opencode" "$DATA/opencode"
link_to_volume "$HOME/.local/state/opencode" "$DATA/opencode-state"

# --- OpenCode config from the configured profiles ---
# Regenerated on every create rather than persisted, so a provider or model added to a
# profile in git reaches an existing container on its next rebuild.
DEVENV="$DEVCONTAINER_DIR/devcontainer.env"
PROFILES_DIR="$DEVCONTAINER_DIR/profiles"

if [ -f "$DEVENV" ]; then
    PROFILES_LINE=$(grep '^CONFIGURED_PROFILES=' "$DEVENV" 2>/dev/null || true)
    if [ -n "$PROFILES_LINE" ]; then
        CSV="${PROFILES_LINE#CONFIGURED_PROFILES=}"
        # Strip CR before splitting — load-bearing on Windows hosts, where setup.ps1
        # writes devcontainer.env with CRLF and we read the file DIRECTLY (Docker's
        # --env-file trims for us, this path does not). Without it the LAST profile in
        # the CSV keeps a trailing \r and every path built from it misses, silently.
        # Full story in features/chat/postcreate.sh, where the bug first surfaced.
        CSV="${CSV//$'\r'/}"
        IFS=',' read -ra CONFIGURED <<< "$CSV"

        OC_FILES=()
        for profile in "${CONFIGURED[@]}"; do
            if [ -f "$PROFILES_DIR/$profile/opencode.json" ]; then
                OC_FILES+=("$profile")
            fi
        done

        if [ ${#OC_FILES[@]} -eq 1 ]; then
            mkdir -p "$HOME/.config/opencode"
            cp "$PROFILES_DIR/${OC_FILES[0]}/opencode.json" "$HOME/.config/opencode/opencode.json"
        elif [ ${#OC_FILES[@]} -ge 2 ]; then
            mkdir -p "$HOME/.config/opencode"
            OC_PATHS=()
            for p in "${OC_FILES[@]}"; do OC_PATHS+=("$PROFILES_DIR/$p/opencode.json"); done
            # Merge providers and disabled_providers; model is omitted so
            # OpenCode uses whatever the user last selected. `provider` is the
            # OpenCode 1 layout (deprecated in 2 but still read for custom models,
            # options and endpoints); `providers` is OpenCode 2's, used for the
            # per-model `disabled` flags that replaced `blacklist`, which 2 ignores.
            jq -s '{
                "$schema": "https://opencode.ai/config.json",
                disabled_providers: ([.[].disabled_providers // [] | .[]] | unique | if length > 0 then . else null end),
                provider: (reduce (.[].provider // {}) as $p ({}; . * $p)),
                providers: (reduce (.[].providers // {}) as $p ({}; . * $p) | if length > 0 then . else null end)
            } | with_entries(select(.value != null))' "${OC_PATHS[@]}" > "$HOME/.config/opencode/opencode.json"
        fi
    fi
fi

# Update to the latest version (runs on every create, bypassing the image cache)
echo "Updating OpenCode..."
opencode upgrade || true

# --- Run OpenCode's shell tool in bash ---
# OpenCode runs tool commands in $SHELL, which is zsh here (common-utils makes it the
# login shell), and the commands it writes assume bash: zsh's no-word-splitting, NOMATCH
# globs and `=word` expansion made them fail and be retried. This launcher starts it with
# SHELL=/bin/bash; interactive terminals keep zsh. ~/.local/bin is first on PATH, ahead of
# the installer's ~/.opencode/bin, and is in the image rather than on the volume, so this
# runs on every create. `rm -f` first, so an existing link is replaced, not written through.
if [ -x "$HOME/.opencode/bin/opencode" ]; then
    mkdir -p "$HOME/.local/bin"
    launcher="$HOME/.local/bin/opencode"
    rm -f "$launcher"
    cat > "$launcher" <<'EOF'
#!/bin/bash
# Generated by .devcontainer/features/opencode/postcreate.sh: run OpenCode's shell tool in bash.
export SHELL=/bin/bash
exec "$HOME/.opencode/bin/opencode" "$@"
EOF
    chmod 755 "$launcher"
fi

# Interactive zsh runs ~/.zshrc, whose installer-added lines put ~/.grok/bin and
# ~/.opencode/bin AHEAD of ~/.local/bin, which would bypass the launcher there. A marked
# block at the end of ~/.zshrc puts ~/.local/bin first again; it is removed and re-added on
# every create, so it stays last and never duplicates. The grok and opencode features write
# the identical block, so either one alone suffices. ~/.zshrc comes from the image, not the
# volume.
if [ -f "$HOME/.zshrc" ]; then
    sed -i '/^# >>> devcontainer: local-bin-first >>>$/,/^# <<< devcontainer: local-bin-first <<<$/d' "$HOME/.zshrc"
    cat >> "$HOME/.zshrc" <<'EOF'
# >>> devcontainer: local-bin-first >>>
# Added by .devcontainer/features/{grok,opencode}/postcreate.sh: keep ~/.local/bin (the
# agent launchers) ahead of the installers' PATH entries above.
export PATH="$HOME/.local/bin:$PATH"
# <<< devcontainer: local-bin-first <<<
EOF
fi
