# Deployment Guide

This document walks you through everything needed to install and configure Ranger — from prerequisites to smoke testing. Whether you're setting up in a connected lab or a fully air-gapped environment, you'll get Ranger running cleanly and confidently.

Ranger uses a modular architecture built around Docker containers. It plays well with most range infrastructure and is designed to scale — both technically and in terms of scenario complexity.

1. System Requirements and Prerequisites

    You’ll need:

    - A Linux 🐧 (or MacOS 🍎) host (VM or bare metal) with:
        - [Docker](https://www.docker.com/) 🐳
        - At least 16GB RAM (more for large LLMs)
        - NVIDIA GPU + drivers for the LLM (or Apple Silicon on MacOS)
    - [Docker Compose](https://docs.docker.com/compose/) (optional, but hugely helpful)
    - [Ollama](https://ollama.com/) installed on the host (or included via container)
    - For air-gapped deployment:
        - An internet-connected machine to fetch Docker images and LLM model files
        - A secure way to move files to your isolated target system

2. Creating the Docker Network
```bash
docker network create ranger-net

docker run -d --name n8n --network ranger-net -p 5678:5678 \
  -v n8n_data:/home/node/.n8n \
  -e N8N_SECURE_COOKIE=false \
  --restart unless-stopped \
  docker.n8n.io/n8nio/n8n:1.90.2

docker run -d --name open-webui --network ranger-net -p 3000:8080 \
  -e OPENBLAS_NUM_THREADS=1 -e OMP_NUM_THREADS=1 \
  --ulimit nproc=8192 --ulimit nofile=65535:65535 \
  -v open-webui-data:/app/backend/data \
  --restart always ghcr.io/open-webui/open-webui:main

docker run -d --name pipelines --network ranger-net -p 9099:9099 \
  --restart always ghcr.io/open-webui/pipelines:main

docker run -d --name qdrant --network ranger-net -p 6333:6333 \
  --restart unless-stopped qdrant/qdrant:latest

docker run -d --name baserow --network ranger-net -p 8081:80 \
  -v baserow_data:/baserow/data \
  -e BASEROW_PUBLIC_URL=http://<HOST_IP>:8081 \
  --restart unless-stopped baserow/baserow:1.32.5
```