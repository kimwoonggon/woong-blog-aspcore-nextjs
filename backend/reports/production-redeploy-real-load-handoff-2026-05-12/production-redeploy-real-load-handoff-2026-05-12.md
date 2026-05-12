# Production Redeploy And Real Load Handoff - 2026-05-12

## Current Ready State

- `main` merge commit: `389a117ee8cda43e84536c85164bf13afd8e38bf`
- CI Main Runtime run: `25717479751`, success
- Publish GHCR Main run: `25717736048`, success
- Backend runtime image: `ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-backend:main`
- Backend manifest digest: `sha256:ae0679f8166380e0edef07c3d546cc31c9c291380e6f7eccc691c225c44afdf2`
- Frontend runtime image: `ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-frontend:main`
- Frontend manifest digest: `sha256:8cd7cdea54ad6388bcb34f3b8b694cf87e9721a35273cafd35d0f5fea0bfb5f3`

## Scope Boundary

This handoff does not execute production SSH or production deployment. Running the workflow below will use the repository production SSH secrets and will pull/deploy on the production host.

## Required Secret Blocker

Current `gh secret list --repo kimwoonggon/woong-blog-aspcore-nextjs` output exposes only `PROMOTION_TOKEN`, and the repository environment list is empty. The production redeploy workflow requires these secrets before dispatch:

- `PROD_SSH_HOST`
- `PROD_SSH_USER`
- `PROD_SSH_PRIVATE_KEY`
- `PROD_SSH_KNOWN_HOSTS`
- `PROD_SSH_PORT` if not using port 22

`PROD_GHCR_TOKEN` is optional. Empty `DOCKER_CONFIG` manifest inspect succeeded for both runtime `:main` images from this workspace, so tokenless pull should work unless the production host is blocked by network/rate-limit policy.

Do not dispatch `Production Runtime Redeploy` until the required production SSH secrets exist, or the workflow will fail in `Prepare SSH`.

### Secret Setup Commands

Run these locally only with real production values. Do not commit the values.

```bash
gh secret set PROD_SSH_HOST --repo kimwoonggon/woong-blog-aspcore-nextjs --body '<production-host>'
gh secret set PROD_SSH_USER --repo kimwoonggon/woong-blog-aspcore-nextjs --body '<production-user>'
gh secret set PROD_SSH_PRIVATE_KEY --repo kimwoonggon/woong-blog-aspcore-nextjs < /path/to/production_ssh_private_key
gh secret set PROD_SSH_KNOWN_HOSTS --repo kimwoonggon/woong-blog-aspcore-nextjs --body '<ssh-keyscan-output>'
gh secret set PROD_SSH_PORT --repo kimwoonggon/woong-blog-aspcore-nextjs --body '22'
# Optional only if production host cannot pull public GHCR runtime packages:
gh secret set PROD_GHCR_TOKEN --repo kimwoonggon/woong-blog-aspcore-nextjs --body '<read-packages-token-if-needed>'
```

Recommended known-hosts capture:

```bash
ssh-keyscan -p 22 <production-host> > /tmp/woong-prod-known-hosts
gh secret set PROD_SSH_KNOWN_HOSTS --repo kimwoonggon/woong-blog-aspcore-nextjs < /tmp/woong-prod-known-hosts
```

## Step 1: Select Real Public Targets From `pageSize=12`

Use the public list APIs to choose real current targets. Do not use `seeded-*`, `fixture-*`, or cache-only shortcuts.

Current public-origin scan from `https://woonglab.com` found these larger real read targets from `pageSize=12` lists:

- Work read: `/api/public/works/water-fluid-simulation`
  - Title: `Water Fluid Simulation`
  - Probe: HTTP 200, about 76,234 bytes, about 979.6 ms from this workspace
- Study read: `/api/public/blogs/%EC%95%88%EB%A9%B4%EC%9D%B8%EC%8B%9D%EC%9D%84-%EB%84%A3%EA%B8%B0-%EC%9C%84%ED%95%9C-%EC%BD%94%ED%8B%80%EB%A6%B0-%EC%95%88%EB%93%9C%EB%A1%9C%EC%9D%B4%EB%93%9C-%EC%A7%80%EC%8B%9D-%EC%95%88%EB%A9%B4%EC%9D%B8%EC%8B%9D%EC%9D%84-%EB%84%A3%EA%B8%B0-%EC%9C%84%ED%95%9C-%EC%BD%94%ED%8B%80%EB%A6%B0-%EC%95%88%EB%93%9C%EB%A1%9C%EC%9D%B4%EB%93%9C-%EC%A7%80%EC%8B%9D-2`
  - Title: `안면인식을 넣기 위한 코틀린 안드로이드 지식 안면인식을 넣기 위한 코틀린 & 안드로이드 지식 (2)`
  - Probe: HTTP 200, about 101,560 bytes, about 575.5 ms from this workspace

Use those two paths unless the public list changes before the production run.

```bash
BASE_URL=https://woonglab.com

WORK_READ_PATH="$(
  curl -fsS "${BASE_URL}/api/public/works?page=1&pageSize=12" \
    | node -e '
const fs = require("node:fs")
const payload = JSON.parse(fs.readFileSync(0, "utf8"))
const item = (payload.items || []).find((x) => {
  const slug = String(x.slug || "")
  const title = String(x.title || "")
  return slug && !/seed|fixture/i.test(slug) && !/seed|fixture/i.test(title)
})
if (!item) process.exit(2)
console.log(`/api/public/works/${encodeURIComponent(item.slug)}`)
'
)"

STUDY_READ_PATH="$(
  curl -fsS "${BASE_URL}/api/public/blogs?page=1&pageSize=12" \
    | node -e '
const fs = require("node:fs")
const payload = JSON.parse(fs.readFileSync(0, "utf8"))
const item = (payload.items || []).find((x) => {
  const slug = String(x.slug || "")
  const title = String(x.title || "")
  return slug && !/seed|fixture/i.test(slug) && !/seed|fixture/i.test(title)
})
if (!item) process.exit(2)
console.log(`/api/public/blogs/${encodeURIComponent(item.slug)}`)
'
)"

printf 'WORK_READ_PATH=%s\nSTUDY_READ_PATH=%s\n' "${WORK_READ_PATH}" "${STUDY_READ_PATH}"
```

## Step 2: Dispatch Production Runtime Redeploy With Real Load Enabled

```bash
gh workflow run "Production Runtime Redeploy" \
  -f base_url=https://woonglab.com \
  -f repo_dir=/root/service/woong-blog-aspcore-nextjs \
  -f work_read_path="/api/public/works/water-fluid-simulation" \
  -f study_read_path="/api/public/blogs/%EC%95%88%EB%A9%B4%EC%9D%B8%EC%8B%9D%EC%9D%84-%EB%84%A3%EA%B8%B0-%EC%9C%84%ED%95%9C-%EC%BD%94%ED%8B%80%EB%A6%B0-%EC%95%88%EB%93%9C%EB%A1%9C%EC%9D%B4%EB%93%9C-%EC%A7%80%EC%8B%9D-%EC%95%88%EB%A9%B4%EC%9D%B8%EC%8B%9D%EC%9D%84-%EB%84%A3%EA%B8%B0-%EC%9C%84%ED%95%9C-%EC%BD%94%ED%8B%80%EB%A6%B0-%EC%95%88%EB%93%9C%EB%A1%9C%EC%9D%B4%EB%93%9C-%EC%A7%80%EC%8B%9D-2" \
  -f run_real_load=true \
  -f rates="100 200 300 400" \
  -f duration_seconds=30 \
  -f max_vus=500 \
  -f pre_allocated_vus=100
```

## Step 3: Watch The Workflow

```bash
gh run list --workflow "Production Runtime Redeploy" --limit 3
gh run watch <RUN_ID> --interval 20
gh run view <RUN_ID> --json status,conclusion,url,headSha
```

## Expected Gates

- Remote `git checkout main` and `git pull --ff-only origin main` reach `389a117ee8cda43e84536c85164bf13afd8e38bf` or newer.
- Remote `.env.prod` is backed up before image env changes.
- `FRONTEND_IMAGE` and `BACKEND_IMAGE` are set to GHCR runtime `:main` images.
- `LoadTesting__BaseUrl` is the public origin, not `backend:8080`.
- `docker compose --env-file .env.prod -f docker-compose.prod.yml pull` succeeds.
- `prod-runtime-preflight.sh` passes through nginx/public origin.
- `prod-real-load-steps.sh` runs with `LIST_PAGE_SIZE=12`.
- Load targets reject seed/fixture paths.
- Evidence bundle verifier passes with the pulled image digests and actual main SHA.

## Result-Based Slice Selection

Use the Real Backend Test summary to choose only one next slice:

- HLS fatal fix: choose this if upload/HLS processing produces fatal errors or video contract preflight fails.
- Public detail serialization/body optimization: choose this if Work/Study read P95 dominates, response bytes are high, or app elapsed tracks content serialization.
- DB/index optimization: choose this if DB latency, connection open P95, active connections, or query diagnostics dominate.

## Stop Conditions

- Any 5xx, timeout, missing evidence bundle, seed/fixture target, or `pageSize` other than 12 invalidates the run.
- Do not select the next optimization slice from a failed or partially verified production run.
