#!/bin/sh
cat <<EOF

 ____ ____ __ _ ____ ____ ____   ____ ___  ___  _    _ ____ __ _ ____ ____
 |--< |--| | \| |__, |=== |--<   |--| |--' |--' |___ | |--| | \| |___ |===


Welcome to Ranger Appliance $(cat /etc/appliance_version) ($(lsb_release -ds))

Visit https://ranger.local:8888 to get started.
EOF
