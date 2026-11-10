## n8n + Postgres Migration & Restore

This setup enables a full backup of a n8n Postgres database — including roles, databases, workflows, credentials, and ownership — and restores it into a fresh container with all data and encrypted credentials intact.

We ran a full cluster dump (roles + DB) as a superuser in the existing Postgres:

```bash 
docker exec -e PGPASSWORD=n8npass postgres \
  pg_dumpall -U n8n > ./pgseed/full_cluster.sql
```

Then we tested a restore in a throwaway container

```bash
docker run --rm -d --name pgtest \
  -e POSTGRES_USER=n8n -e POSTGRES_PASSWORD=n8npass -p 5433:5432 postgres:15

cat pgseed/full_cluster.sql | docker exec -e PGPASSWORD=n8npass -i pgtest \
  psql -U n8n -d postgres

```

In the final compose we include an init script (00-restore.sh) to drop the placeholder DB and run the dump on empty volumes.

On docker compose up with a fresh volume, Postgres will recreate the roles, DB, and data exactly as in the dump.

Notes

    - Set N8N_ENCRYPTION_KEY in the n8n service to match your original.
    - Use the same POSTGRES_USER, POSTGRES_PASSWORD, and POSTGRES_DB values that exist in your restored dump.
