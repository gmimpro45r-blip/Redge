#!/usr/bin/env bash
# Apply db/schema.sql to a Supabase project.
#
# Usage:
#   ./scripts/setup-supabase.sh <DB_URL>
#
# Or via env:
#   SUPABASE_DB_URL='postgresql://postgres:PWD@db.PROJECT.supabase.co:5432/postgres' \
#       ./scripts/setup-supabase.sh
#
# Tip: the connection string lives in Supabase → Settings → Database → "Connection string".

set -euo pipefail

DB_URL="${1:-${SUPABASE_DB_URL:-}}"

if [[ -z "${DB_URL}" ]]; then
    echo "ERROR: Pass the Supabase DB connection string as the first argument" >&2
    echo "       or export SUPABASE_DB_URL." >&2
    exit 1
fi

if ! command -v psql >/dev/null 2>&1; then
    echo "ERROR: 'psql' is not installed. Install PostgreSQL client tools first." >&2
    echo "  Ubuntu / Debian:  sudo apt-get install postgresql-client" >&2
    echo "  macOS (brew):     brew install libpq && brew link --force libpq" >&2
    exit 1
fi

SCHEMA_FILE="$(cd "$(dirname "$0")/.." && pwd)/db/schema.sql"

if [[ ! -f "${SCHEMA_FILE}" ]]; then
    echo "ERROR: schema file not found at ${SCHEMA_FILE}" >&2
    exit 1
fi

echo ">>> Applying schema to Supabase project..."
psql "${DB_URL}" -v ON_ERROR_STOP=1 -f "${SCHEMA_FILE}"

echo ">>> Done.  Tables, triggers, views, RPC and RLS policies are in place."
