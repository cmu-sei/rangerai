#!/usr/bin/env bash

echo "🦊 Ranger Stack Configuration Check"
echo "--------------------------------------------------------------------"
echo "--------------------------------------------------------------------"

# 1. List all running containers
echo -e "\n🔍 Running Docker Containers:"
echo "--------------------------------------------------------------------"
docker ps --format "table {{.Names}}\t{{.Image}}\t{{.Status}}"

# 2. Define tools and ports using parallel arrays
tools=("baserow" "qdrant" "n8n" "ollama" "ranger")
ports=(3000 6333 5678 11434 8000)

echo ""
echo "Checking tools:"
echo "--------------------------------------------------------------------"

configure_baserow()  { echo "⚙️  Configuring Baserow..."; }
configure_qdrant()   { echo "⚙️  Configuring Qdrant..."; }
configure_n8n()      { echo "⚙️  Configuring n8n..."; }
configure_ollama()   { echo "⚙️  Configuring Ollama..."; curl -s http://localhost:11434/api/tags || echo "❌ Ollama API check failed"; }
configure_ranger()   { echo "⚙️  Configuring Ranger..."; }

for i in "${!tools[@]}"; do
  tool="${tools[$i]}"
  port="${ports[$i]}"
  echo -e "\n🧪 Checking $tool..."

  is_container_up=$(docker ps --format '{{.Names}}' | grep -q "$tool" && echo "yes" || echo "no")
  is_port_up=$(nc -z localhost "$port" >/dev/null 2>&1 && echo "yes" || echo "no")

  if [[ "$tool" == "ollama" ]]; then
    if [[ "$is_container_up" == "yes" ]]; then
      echo "✅ Ollama container is running."
    fi
    if [[ "$is_port_up" == "yes" ]]; then
      echo "🖥️  Ollama is also running on host (port 11434)."
    fi
    if [[ "$is_container_up" == "no" && "$is_port_up" == "no" ]]; then
      echo "❌ Ollama not found (no container, no host service)."
    fi
  else
    if [[ "$is_container_up" == "yes" ]]; then
      echo "✅ $tool container '$tool' is running."
    else
      echo "❌ $tool container '$tool' is NOT running."
    fi
  fi

  echo -n "🌐 $tool (port $port): "
  [[ "$is_port_up" == "yes" ]] && echo "✅ reachable" || echo "❌ unreachable"

  # Run configuration if up
  if [[ "$is_port_up" == "yes" ]]; then
    configure_func="configure_${tool}"
    if declare -f "$configure_func" > /dev/null; then
      $configure_func
    else
      echo "⚠️  No configuration function for $tool"
    fi
  fi
done

echo "--------------------------------------------------------------------"
echo "--------------------------------------------------------------------"
echo -e "\n🧼 Done."
