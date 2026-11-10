#!/bin/bash
set -euo pipefail

# Install the AgentCore CLI from the published npm release, using the command
# upstream documents at https://github.com/aws/agentcore-cli. `agentcore update`
# runs the same `npm install -g`, so the create-time upgrade stays on this route.
#
# NODE_EXTRA_CA_CERTS is exported inside the su body so npm trusts the
# SEI/Zscaler CA while fetching from the registry: containerEnv is runtime-only
# (unset during the image build when features run) and `su -` resets the env
# anyway. The system bundle already carries the custom CAs (features/sei-certs).
su - "${_REMOTE_USER}" -c "
    set -euo pipefail
    . ${NVM_DIR}/nvm.sh
    export NODE_EXTRA_CA_CERTS='/etc/ssl/certs/ca-certificates.crt'
    npm install -g @aws/agentcore
"
