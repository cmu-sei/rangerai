# Ranger Appliance Quickstart Guide

This guide will help you get a Ranger instance up and running as a VM on Proxmox.

## Requirements

- Proxmox VE 7.4 or later
- Packer installed locally
- At least 16GB RAM recommended
- Internet access (initial build and image pull only)

## Clone, configure, and build the Appliance

```bash
git clone https://github.com/CMU-SEI/RangerAI.git
touch .env
```

This has created an .env file in the root of the repo. Now add the following content:

```env
BASEROW_PUBLIC_URL="" set to your machine’s IP or domain
OLLAMA_HOST="http://localhost:11434" #Ollama server URL
N8N_API_URL="http://localhost:5678" #n8n API URL
N8N_API_KEY="" #set to your n8n API key
GHOSTS_HOST="" #set to your ghosts api url
```

Edit any port conflicts by changing host:container mappings in `docker-compose.yml` if needed.

Lastly, start the Packer build process:

```bash
cd ranger-appliance
packer build ranger.pkr.hcl
```

This launches:

- Docs on http://localhost:8888
- Open-WebUI (chat frontend) on port 3001
- Ranger API on port 5076
- n8n (workflow engine) on port 5678
- Qdrant (vector database) on port 6333
- Baserow (data backend) on port 80
- Ghosts MCP (tool server) on port 8000

## Start Ollama

If you haven't already, install [Ollama](https://ollama.ai) and run it:

```bash
ollama serve
```

You may need to pull models. For example, to pull the Mistral model:

```bash
ollama pull mistral:latest
```

## Configure the Workflow Interface

1. Open a browser and navigate to: `http://localhost:5678`. If it does not render, run `docker restart n8n`.
1. Create your admin account. This is your main interface for interacting with Ranger as an Admin.
1. Click on your account name and go to settings. Create an n8n API Key. Copy it into your .env file.
1. Import any workflows and complete their necessary accounts for access to second-level tools such as Qdrant, etc.
1. Rebuild Ranger: `docker compose up -d --force-recreate ranger` - Ranger will poll n8n for active workflows now.

## Settings Up Client Access to Test the System

Open a browser and navigate to: `http://localhost:3001`

1. Setup a new user account.
2. Go to admin settings and add a connection to both Ollama and Ranger, using the urls http://host.docker.internal:11434 for Ollama and http://host.docker.internal:5076 for Ranger.
3. Now return to the main page and switch the chat model to a Ranger model (e.g., `ranger:mistral`).

Try a prompt like:

```
ghosts machines list
```

Ranger will parse, plan, and execute using GHOSTS or other tools via MCP.
