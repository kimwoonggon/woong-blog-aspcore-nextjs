# Pasteable Server Commands - Production Runtime Retest

Use this when direct SSH from the local Codex environment is unavailable.

Paste block 1 on the production server. It updates only non-secret runtime image/base-url keys, backs up `.env.prod`, pulls/recreates containers, and runs production preflight.

```bash
cat > /tmp/server-main-runtime-redeploy.sh <<'SCRIPT'
#!/usr/bin/env bash
set -euo pipefail

cd /root/service/woong-blog-aspcore-nextjs

git fetch origin main --prune
git checkout main
git pull --ff-only origin main

backup=".env.prod.backup.$(date -u +%Y%m%dT%H%M%SZ)"
cp .env.prod "$backup"
echo "Backed up .env.prod to $backup"

upsert_env() {
  local key="$1"
  local value="$2"
  if grep -q "^${key}=" .env.prod; then
    sed -i "s#^${key}=.*#${key}=${value}#" .env.prod
  else
    printf '\n%s=%s\n' "$key" "$value" >> .env.prod
  fi
}

upsert_env FRONTEND_IMAGE ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-frontend:main
upsert_env BACKEND_IMAGE ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-backend:main
upsert_env LoadTesting__BaseUrl https://woonglab.com
upsert_env APP_ENV_FILE .env.prod
upsert_env NGINX_DEFAULT_CONF ./nginx/prod.conf

printf 'Runtime env image/base-url check:\n'
grep -E '^(FRONTEND_IMAGE|BACKEND_IMAGE|LoadTesting__BaseUrl|APP_ENV_FILE|NGINX_DEFAULT_CONF)=' .env.prod

printf 'Compose resolved runtime images:\n'
docker compose --env-file .env.prod -f docker-compose.prod.yml config \
  | grep -E 'image: ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-(frontend|backend):main'

docker compose --env-file .env.prod -f docker-compose.prod.yml pull
docker compose --env-file .env.prod -f docker-compose.prod.yml up -d --remove-orphans
docker compose --env-file .env.prod -f docker-compose.prod.yml ps

printf 'Backend env check:\n'
docker compose --env-file .env.prod -f docker-compose.prod.yml exec -T backend printenv \
  | grep -E '^(ASPNETCORE_ENVIRONMENT|LoadTesting__BaseUrl|POSTGRES_MAX_POOL_SIZE)=|^ConnectionStrings__Postgres=' \
  | sed -E 's/(Password=)[^;]*/\1***REDACTED***/'

BASE_URL=https://woonglab.com \
REQUIRE_PUBLIC_WORK_VIDEO_CONTRACT=1 \
WORK_READ_PATH=/api/public/works/smoke-fluid-simulation \
./scripts/prod-runtime-preflight.sh

printf 'Public stale-key recheck after deploy:\n'
curl -fsS --compressed 'https://woonglab.com/api/public/works?page=1&pageSize=12' \
  | node -e 'let s=""; process.stdin.on("data", c => s += c); process.stdin.on("end", () => { const x=JSON.parse(s); const keys=Object.keys(x.items?.[0]||{}); console.log(keys.join(",")); if (keys.includes("period") || keys.includes("iconUrl")) process.exit(20); })'
SCRIPT
bash /tmp/server-main-runtime-redeploy.sh
```

If block 1 passes, paste block 2. It runs preflight again, auto-selects a non-seed Study URL from `pageSize=12`, then runs the real load steps.

```bash
cat > /tmp/server-run-real-load-after-preflight.sh <<'SCRIPT'
#!/usr/bin/env bash
set -euo pipefail

REPO_DIR="${REPO_DIR:-/root/service/woong-blog-aspcore-nextjs}"
BASE_URL="${BASE_URL:-https://woonglab.com}"
WORK_READ_PATH="${WORK_READ_PATH:-/api/public/works/smoke-fluid-simulation}"
STUDY_READ_PATH="${STUDY_READ_PATH:-}"
RATES="${RATES:-100 200 300 400}"
DURATION_SECONDS="${DURATION_SECONDS:-30}"
MAX_VUS="${MAX_VUS:-500}"
PRE_ALLOCATED_VUS="${PRE_ALLOCATED_VUS:-100}"
OUTPUT_DIR="${OUTPUT_DIR:-backend/reports/prod-real-load-steps-$(date -u +%Y%m%dT%H%M%SZ)/loadtest}"

cd "${REPO_DIR}"

require_command() {
  local name="$1"
  if ! command -v "${name}" >/dev/null 2>&1; then
    echo "missing required command: ${name}" >&2
    exit 2
  fi
}

reject_seed_path() {
  local label="$1"
  local value="$2"
  case "${value}" in
    *seeded*|*fixture*)
      echo "${label} must be a real public target, not seed/fixture: ${value}" >&2
      exit 3
      ;;
  esac
}

resolve_first_non_seed_public_path() {
  local api_path="$1"
  local public_prefix="$2"
  curl -fsS "${BASE_URL%/}${api_path}" | node -e '
const fs = require("node:fs")
const prefix = process.argv[1]
const input = fs.readFileSync(0, "utf8")
const payload = JSON.parse(input)
const items = Array.isArray(payload.items) ? payload.items : []
const selected = items.find((item) => {
  const slug = String(item.slug || "")
  const title = String(item.title || "")
  return slug && !/seed|fixture/i.test(slug) && !/seed|fixture/i.test(title)
})
if (!selected) {
  console.error("no non-seed public target found")
  process.exit(4)
}
console.log(`${prefix}${encodeURIComponent(selected.slug)}`)
' "${public_prefix}"
}

require_command git
require_command docker
require_command curl
require_command node
require_command k6

if [[ -z "${STUDY_READ_PATH}" ]]; then
  STUDY_READ_PATH="$(resolve_first_non_seed_public_path '/api/public/blogs?page=1&pageSize=12' '/api/public/blogs/')"
fi

reject_seed_path WORK_READ_PATH "${WORK_READ_PATH}"
reject_seed_path STUDY_READ_PATH "${STUDY_READ_PATH}"

printf 'Using real load targets:\n'
printf '  BASE_URL=%s\n' "${BASE_URL}"
printf '  WORK_READ_PATH=%s\n' "${WORK_READ_PATH}"
printf '  STUDY_READ_PATH=%s\n' "${STUDY_READ_PATH}"
printf '  RATES=%s\n' "${RATES}"
printf '  DURATION_SECONDS=%s\n' "${DURATION_SECONDS}"
printf '  MAX_VUS=%s\n' "${MAX_VUS}"
printf '  PRE_ALLOCATED_VUS=%s\n' "${PRE_ALLOCATED_VUS}"
printf '  OUTPUT_DIR=%s\n' "${OUTPUT_DIR}"

BASE_URL="${BASE_URL}" \
REQUIRE_PUBLIC_WORK_VIDEO_CONTRACT=1 \
WORK_READ_PATH="${WORK_READ_PATH}" \
./scripts/prod-runtime-preflight.sh

BASE_URL="${BASE_URL}" \
WORK_READ_PATH="${WORK_READ_PATH}" \
STUDY_READ_PATH="${STUDY_READ_PATH}" \
RATES="${RATES}" \
DURATION_SECONDS="${DURATION_SECONDS}" \
MAX_VUS="${MAX_VUS}" \
PRE_ALLOCATED_VUS="${PRE_ALLOCATED_VUS}" \
OUTPUT_DIR="${OUTPUT_DIR}" \
./scripts/prod-real-load-steps.sh
SCRIPT
bash /tmp/server-run-real-load-after-preflight.sh
```

Paste back:

- full output of block 1
- full output of block 2
- generated `prod-real-load-steps-summary.md`
- generated `prod-real-load-steps-summary.json`
