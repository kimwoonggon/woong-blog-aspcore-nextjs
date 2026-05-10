#!/usr/bin/env bash
set -euo pipefail

cd /root/service/woong-blog-aspcore-nextjs

git fetch origin main --prune
git checkout main
git pull --ff-only origin main

docker_auth_config="$(mktemp -d)"
cleanup() {
  rm -rf "${docker_auth_config}"
}
trap cleanup EXIT

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

if [[ -n "${GHCR_TOKEN:-}" ]]; then
  printf '%s' "${GHCR_TOKEN}" | docker --config "${docker_auth_config}" login ghcr.io -u "${GHCR_USER:-kimwoonggon}" --password-stdin >/dev/null
  echo "Logged in to ghcr.io with GHCR_TOKEN using a temporary Docker config."
else
  echo "GHCR_TOKEN is not set; using a temporary anonymous Docker config for public GHCR runtime images."
fi

printf 'Runtime env image/base-url check:\n'
grep -E '^(FRONTEND_IMAGE|BACKEND_IMAGE|LoadTesting__BaseUrl|APP_ENV_FILE|NGINX_DEFAULT_CONF)=' .env.prod

printf 'Compose resolved runtime images:\n'
docker --config "${docker_auth_config}" compose --env-file .env.prod -f docker-compose.prod.yml config \
  | grep -E 'image: ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-(frontend|backend):main'

check_ghcr_manifest() {
  local image="$1"
  if ! docker --config "${docker_auth_config}" manifest inspect "${image}" >/dev/null 2>&1; then
    cat >&2 <<MSG
Cannot read ${image}.
The GHCR runtime package is public, but this host still cannot read it with the current temporary Docker config.
If this is a GitHub/GHCR auth policy issue, rerun with GHCR_TOKEN set to a GitHub token that has read:packages access, for example:

  GHCR_USER=kimwoonggon GHCR_TOKEN=<read-packages-token> bash /tmp/server-main-runtime-redeploy.sh

MSG
    exit 8
  fi
}

check_ghcr_manifest ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-backend:main
check_ghcr_manifest ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-frontend:main

docker --config "${docker_auth_config}" compose --env-file .env.prod -f docker-compose.prod.yml pull
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
