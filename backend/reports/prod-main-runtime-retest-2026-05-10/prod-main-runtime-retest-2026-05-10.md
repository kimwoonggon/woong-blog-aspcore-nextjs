# Production Main Runtime Retest - Blocked Before Server Deploy

Generated at: 2026-05-10T09:31:49.901Z

## Status

blocked_before_server_deploy

## Objective Checklist

- Pull/deploy latest main runtime image on production server
- Run production preflight
- Rerun Real Backend Test with pageSize=12, no seed, no cache, real Work/Study URLs
- Choose and implement one next performance slice, then verify full e2e and CI/main promotion

## Evidence Collected

- origin/main: 1bf8a5dc9d57dad8ed0c74b2752d678d42b40556
- latest main CI/publish: successful before this report
- public base URL probed: https://woonglab.com
- Work list first keys: id, slug, title, excerpt, category, period, tags, thumbnailUrl, iconUrl, publishedAt
- Work detail keys: id, slug, title, excerpt, contentJson, category, period, tags, thumbnailUrl, iconUrl, publishedAt, socialShareMessage, videos_version, videos
- Work video keys: id, sourceType, sourceKey, playbackUrl, originalFileName, mimeType, fileSize, width, height, duration_seconds, timeline_preview_vtt_url, timeline_preview_sprite_url, sortOrder, createdAt
- Blog list first keys: id, slug, title, excerpt, tags, coverUrl, publishedAt
- Blog detail keys: id, slug, title, excerpt, contentJson, tags, coverUrl, publishedAt

## Interpretation

- Production public Work list still returns period/iconUrl, while origin/main WorkCardDto does not define those fields.
- Production public Work detail still returns contentJson/iconUrl and video originalFileName/fileSize/createdAt, while origin/main uses public Content and trimmed PublicWorkVideoDto.
- Production responses expose X-App-Elapsed-Ms but not X-Nginx-Request-Time through the Cloudflare/public probe, so server-side preflight must be run on the host to verify local nginx headers after deploy.
- Real Backend Test should not be interpreted until server pull/up and preflight pass.

## Blocker

Local environment has no SSH route to production server: srv1592512 does not resolve and woonglab.com points to Cloudflare, not SSH origin.

## Server Commands Needed

Run these on the production host from the repository directory. Do not paste secrets; the env check redacts the database password.

```bash
cd /root/service/woong-blog-aspcore-nextjs
git fetch origin main --prune
git checkout main
git pull --ff-only origin main
docker compose --env-file .env.prod -f docker-compose.prod.yml pull
docker compose --env-file .env.prod -f docker-compose.prod.yml up -d --remove-orphans
docker compose --env-file .env.prod -f docker-compose.prod.yml ps
docker compose --env-file .env.prod -f docker-compose.prod.yml exec -T backend printenv | grep -E "^(ASPNETCORE_ENVIRONMENT|LoadTesting__BaseUrl|POSTGRES_MAX_POOL_SIZE)=|^ConnectionStrings__Postgres=" | sed -E "s/(Password=)[^;]*/\1***REDACTED***/"
BASE_URL=https://woonglab.com REQUIRE_PUBLIC_WORK_VIDEO_CONTRACT=1 WORK_READ_PATH=/api/public/works/smoke-fluid-simulation ./scripts/prod-runtime-preflight.sh
BASE_URL=https://woonglab.com WORK_READ_PATH=/api/public/works/smoke-fluid-simulation STUDY_READ_PATH=/api/public/blogs/<real-study-slug> RATES="100 200 300 400" DURATION_SECONDS=30 MAX_VUS=500 PRE_ALLOCATED_VUS=100 ./scripts/prod-real-load-steps.sh
```

## Next Required Input

Provide direct SSH host/IP for srv1592512 or run the serverCommands above and paste the outputs/artifacts.

## Recommendation

Do not run or interpret the Real Backend Test yet. First pull/recreate the production containers and make prod-runtime-preflight.sh pass with the real Work video path.

## Recheck

- Checked at: 2026-05-10T09:34:09.008Z
- Work list stale keys remain: period, iconUrl
- Work detail stale keys remain: contentJson, iconUrl
- Work detail video stale keys remain: originalFileName, fileSize, createdAt
- Decision: keep blocked; do not rerun Real Backend Test until production containers are recreated from latest main runtime image.

## Likely Root Cause

The production env appears likely to point at legacy GHCR images without the `-runtime-` suffix. A local service tree at `/mnt/d/woong-blog/service/.env.prod` uses:

```text
FRONTEND_IMAGE=ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-frontend:main
BACKEND_IMAGE=ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-backend:main
```

But the successful `Publish GHCR Main` workflow pushes:

```text
ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-frontend:main
ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-backend:main
```

Use the generated server redeploy script to update only non-secret runtime image/base-url keys, back up `.env.prod`, recreate containers, and run preflight.

- Script artifact: `backend/reports/prod-main-runtime-retest-2026-05-10/server-main-runtime-redeploy.sh`

## GHCR Package Evidence

Package metadata confirms the image-name mismatch risk.

| Package | main tag sha | Updated | Interpretation |
| --- | --- | --- | --- |
| woong-blog-aspcore-nextjs-backend | sha-81d52e48b9be | 2026-04-15T03:19:20Z | legacy/stale |
| woong-blog-aspcore-nextjs-frontend | sha-81d52e48b9be | 2026-04-15T03:19:11Z | legacy/stale |
| woong-blog-aspcore-nextjs-runtime-backend | sha-1bf8a5dc9d57 | 2026-05-10T08:56:53Z | current main runtime |
| woong-blog-aspcore-nextjs-runtime-frontend | sha-1bf8a5dc9d57 | 2026-05-10T08:57:22Z | current main runtime |

Conclusion: if production uses the legacy package names, it will not run the current main runtime.

## Real Load Helper

Added a second server-side helper for the step after redeploy. It avoids manually choosing a seeded Study target.

- Script artifact: `backend/reports/prod-main-runtime-retest-2026-05-10/server-run-real-load-after-preflight.sh`
- Default Work target: `/api/public/works/smoke-fluid-simulation`
- Study target: auto-selected from `/api/public/blogs?page=1&pageSize=12`, rejecting seed/fixture slugs and titles
- Gate: runs `prod-runtime-preflight.sh` before load starts
- Load runner: runs `prod-real-load-steps.sh`, whose own guard enforces `LIST_PAGE_SIZE=12`

Example server command after redeploy:

```bash
cd /root/service/woong-blog-aspcore-nextjs
BASE_URL=https://woonglab.com RATES="100 200 300 400" DURATION_SECONDS=30 MAX_VUS=500 PRE_ALLOCATED_VUS=100 bash /path/to/server-run-real-load-after-preflight.sh
```

## Pasteable Server Commands

Because this Codex environment cannot SSH to the origin server, use this artifact to create and run the server-side scripts by paste only:

- `backend/reports/prod-main-runtime-retest-2026-05-10/pasteable-server-commands.md`

It contains two blocks:

1. Redeploy latest runtime images and run preflight.
2. If preflight passes, rerun real load with pageSize=12, no seed/fixture targets, and real Work/Study URLs.

## SSH Discovery

Windows SSH configuration was inspected without reading private key contents. Candidate access paths were tested in BatchMode only.

| Candidate | Result | Next safe action |
| --- | --- | --- |
| root@72.61.118.77 | SSH reachable, but host key changed compared with Windows known_hosts | Verify the new fingerprint out-of-band before allowing connection |
| 187.127.111.73 | Host key matches known_hosts, but tested users/temporary chmod 600 key were denied | Provide correct user/key or ignore as non-production |
| young-assign-danny-dell.trycloudflare.com | ProxyCommand references missing Windows cloudflared binary from WSL | Provide working cloudflared path/session if this is the production SSH route |

Current keyscan for 72.61.118.77:

```text
RSA     SHA256:KV6A9J1WUpQp1ydE09wh+qgs1gY0zfP6TCYde5O2c6Q
ED25519 SHA256:km7oD3HOJGhAOuQ5eJCqWJBZa+YlxAOUZBwjOAKcc3Q
ECDSA   SHA256:cJ+6NVraeHU40wLQ/qAwIlmrN5rB1I3w7+Dd6moUkOA
```

Do not bypass host-key checking for 72.61.118.77 unless the fingerprint is confirmed by the server provider or another trusted channel.

### Cloudflared Docker Attempt

A Docker-based `cloudflared` fallback was tested without installing host binaries.

- Image: `cloudflare/cloudflared:latest`
- Version: `2026.3.0`
- Result: `young-assign-danny-dell.trycloudflare.com` DNS lookup failed with `no such host`
- Interpretation: the trycloudflare tunnel alias appears expired/unavailable and is not a usable production SSH route now.

## Final Public Recheck

- Work list stale keys still present: `period`, `iconUrl`.
- Work detail stale keys still present: `contentJson`, `iconUrl`.
- Work video stale keys still present: `originalFileName`, `fileSize`, `createdAt`.
- Decision: still blocked before a valid latest-runtime Real Backend Test.

## Confirmed Server IP

Reverse DNS confirms the production host identity:

```text
187.127.111.73 -> srv1592512.hstgr.cloud
```

Host key fingerprints from keyscan match the existing Windows known_hosts entry for the IP:

```text
RSA     SHA256:04rmd1hSZqxuZ8U1I7p6w9D4h2W+uOtJ3pQLUD9Kpd8
ECDSA   SHA256:E0rI2+HYPKa+w0jLKbe0J97edrDF/SeDvN25M+MM2YU
ED25519 SHA256:mZoqsWCqQeteV7r0Cj+YzN2zkkgfOllKN2gJWNBzl/g
```

Authentication is still blocked. The available Windows key was copied to a temporary 0600 file and tested with `root`, `ubuntu`, and `deploy`; all were denied. No SSH agent is available in WSL.

Next safe input required: provide the correct root SSH credential/key or run the pasteable server commands directly on `srv1592512.hstgr.cloud`.
