#!/bin/bash
set -euo pipefail
export PGPASSWORD="$POSTGRES_PASSWORD"

# Wait for server
until pg_isready -h localhost -U "$POSTGRES_USER" >/dev/null 2>&1; do sleep 1; done

# Drop the empty DB auto-created by entrypoint so the dump can recreate exactly
psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d postgres -c "DROP DATABASE IF EXISTS n8ndb;"

# Restore full cluster (roles + DBs + ownership)
psql -v ON_ERROR_STOP=1 -U "$POSTGRES_USER" -d postgres -f /docker-entrypoint-initdb.d/full_cluster.sql

echo "[initdb] restore complete."
