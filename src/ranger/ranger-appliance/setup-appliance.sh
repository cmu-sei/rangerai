# Ranger Appliance Setup

# Exit on errors
set -euo pipefail

echo "$APPLIANCE_VERSION" >/etc/appliance_version

# Customize MOTD and other text for the appliance
chmod -x /etc/update-motd.d/00-header
chmod -x /etc/update-motd.d/10-help-text
sed -i -r 's/(ENABLED=)1/\10/' /etc/default/motd-news
cp ~/scripts/display-banner.sh /etc/update-motd.d/05-display-banner
rm ~/scripts/display-banner.sh
echo -e "Ranger Appliance $APPLIANCE_VERSION \\\n \l \n" >/etc/issue

# # Expand LVM volume to use full drive capacity
# cp ~/scripts/expand-volume.sh /usr/local/bin/expand-volume
# rm ~/scripts/expand-volume.sh
# /usr/local/bin/expand-volume

# # Disable swap for Kubernetes
# swapoff -a
# sed -i -r 's/(\/swap\.img.*)/#\1/' /etc/fstab

# # Upgrade existing packages to latest
# apt-get update
# apt-get full-upgrade -y

# # Add ranger.local to hosts file
sed -i -r 's/(ranger)$/\1 ranger.local/' /etc/hosts

# Install mkdocs
sudo apt update
sudo apt install -y python3-venv

python3 -m venv ~/mkdocs-env
source ~/mkdocs-env/bin/activate

pip install mkdocs-material mkdocs-material-extensions pymdown-extensions

touch /home/ranger/mkdocs.log
sudo chown ranger:ranger /home/ranger/mkdocs.log

cd /home/ranger/

MKDOCS_USER="ranger"
MKDOCS_WORKDIR="/home/${MKDOCS_USER}"
MKDOCS_BIN="/home/${MKDOCS_USER}/mkdocs-env/bin/mkdocs"

# Create systemd service
sudo tee /etc/systemd/system/mkdocs.service > /dev/null <<EOF
[Unit]
Description=MkDocs server
After=network.target

[Service]
User=${MKDOCS_USER}
WorkingDirectory=${MKDOCS_WORKDIR}
ExecStart=${MKDOCS_BIN} serve --dev-addr=0.0.0.0:8888
Restart=always
Environment=PATH=${MKDOCS_WORKDIR}/.local/bin:/usr/bin:/bin

[Install]
WantedBy=multi-user.target
EOF

# Reload systemd, enable + start MkDocs
sudo systemctl daemon-reexec
sudo systemctl enable mkdocs.service
sudo systemctl start mkdocs.service

echo "MkDocs serving at http://<VM-IP>:8888/"

# Install docker
for pkg in docker.io docker-doc docker-compose docker-compose-v2 podman-docker containerd runc; do sudo apt-get remove -y $pkg; done

# Add Docker's official GPG key:
sudo apt-get update
sudo apt-get install -y ca-certificates curl
sudo install -m 0755 -d /etc/apt/keyrings
sudo curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
sudo chmod a+r /etc/apt/keyrings/docker.asc

# Add the repository to Apt sources:
echo \
  "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] https://download.docker.com/linux/ubuntu \
  $(. /etc/os-release && echo "${UBUNTU_CODENAME:-$VERSION_CODENAME}") stable" | \
  sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
sudo apt-get update

sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

IP=$(hostname -I | awk '{print $1}')

# Rewrite docker-compose.yml with correct IP since baserow is picky
sed -i "s|BASEROW_PUBLIC_URL=.*|BASEROW_PUBLIC_URL=http://${IP}:8081|g" docker-compose.yml
sed -i "s|BASEROW_EXTRA_ALLOWED_HOSTS=.*|BASEROW_EXTRA_ALLOWED_HOSTS=${IP},localhost,127.0.0.1|g" docker-compose.yml

echo "Starting docker compose..."
sudo docker compose up -d