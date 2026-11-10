packer {
    required_plugins {
        proxmox = {
            version = "~> 1"
            source  = "github.com/hashicorp/proxmox"
        }
    }
}

variable "ssh_username" { default = "ranger" }
variable "ssh_password" { default = "ranger" }
variable "proxmox_url" { default = "https://192.168.1.155:8006/api2/json" }
variable "proxmox_node" { default = "proxmox" }
variable "proxmox_username" { default = "root@pam" }
variable "proxmox_password" {
  default   = "Sadams@@1!"
  sensitive = true
}
variable "virtualbox_headless" {
  type    = bool
  default = false
}
variable "appliance_version" { default = "0.5" }

locals {
    boot_command = [
        "e<wait>",
        "<down><down><down>",
        "<end><bs><bs><bs><bs><wait>",
        "autoinstall ds=nocloud-net\\;s=http://{{ .HTTPIP }}:{{ .HTTPPort }}/ ---<wait>",
        "<f10><wait>"
    ]
    boot_wait            = "5s"
    cpus                 = 2
    disk_size_virtualbox = "40000"
    disk_size_proxmox    = "40G"
    iso_url              = "https://releases.ubuntu.com/noble/ubuntu-24.04.2-live-server-amd64.iso"
    iso_file_proxmox     = "local:iso/ubuntu-24.04.2-live-server-amd64.iso"
    iso_checksum         = "sha256:d6dab0c3a657988501b4bd76f1297c053df710e06e0c3aece60dead24f270b4d"
    memory               = 8192
    ssh_timeout          = "30m"
}

source "proxmox-iso" "ranger-appliance" {
  boot_command = local.boot_command
  boot_iso {
    type         = "scsi"
    iso_file     = local.iso_file_proxmox
    iso_checksum = local.iso_checksum
    unmount      = true
  }
  boot_wait = local.boot_wait
  cores     = local.cpus
  cpu_type  = "x86-64-v2-AES"
  disks {
    disk_size    = local.disk_size_proxmox
    storage_pool = "local-lvm"
    type         = "scsi"
    format       = "raw"
  }
  http_directory           = "http"
  insecure_skip_tls_verify = true
  memory                   = local.memory
  network_adapters {
    bridge = "vmbr0"
    model  = "virtio"
  }
  node                 = var.proxmox_node
  os                   = "l26"
  password             = var.proxmox_password
  proxmox_url          = var.proxmox_url
  scsi_controller      = "virtio-scsi-single"
  ssh_password         = var.ssh_password
  ssh_timeout          = local.ssh_timeout
  ssh_username         = var.ssh_username
  username             = var.proxmox_username
  vga {
    type = "qxl"
  }
  template_name        = "ranger-appliance-${var.appliance_version}"
  template_description = "Ranger AI Appliance ${var.appliance_version} - built {{ isotime \"2006-01-02T15:04:05Z\" }}"
}

build {
  sources = [
    "source.proxmox-iso.ranger-appliance"
  ]

  provisioner "file" {
    source      = "../docs"
    destination = "/home/${var.ssh_username}/docs"
  }

  provisioner "file" {
    source      = "../docker-compose.yml"
    destination = "/home/${var.ssh_username}/docker-compose.yml"
  }

  provisioner "shell" {
    inline = [
        "chown -R ${var.ssh_username}:${var.ssh_username} /home/${var.ssh_username}/docs"
    ]
  }

  # Trailing slashes both sides means upload contents only, not folder.
  provisioner "file" {
    source      = "./ranger/"
    destination = "/home/${var.ssh_username}/"
  }

  provisioner "shell" {
    execute_command = "echo '${var.ssh_password}' | {{ .Vars }} sudo -E -S bash '{{ .Path }}'"
    environment_vars = [
      "DEBIAN_FRONTEND=noninteractive",
      "APPLIANCE_VERSION=${var.appliance_version}",
      "SSH_USERNAME=${var.ssh_username}",
    ]
    script = "setup-appliance.sh"
  }
}
