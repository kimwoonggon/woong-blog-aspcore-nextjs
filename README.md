# Woong Blog — Next.js + ASP.NET Core + PostgreSQL

Woong Blog is a split-stack portfolio/blog application:

- **Frontend:** Next.js 16 App Router
- **Backend:** ASP.NET Core (.NET 10)
- **Database:** PostgreSQL 16
- **Edge/runtime routing:** nginx + Docker Compose
- **Auth/session authority:** ASP.NET Core cookie + OpenID Connect
- **Media authority:** ASP.NET Core local file storage under `/media/*`

The current repository standard is **compose-first**:

- local development runs through `docker-compose.dev.yml`
- production/runtime deployment runs through `docker-compose.prod.yml`
- GitHub Actions validates the same runtime shapes before merge or publish
- `npm run dev` may still help with isolated UI work, but it is **not** the supported verification path for auth, admin mutations, uploads, or final release checks

## Main URLs

- App entry: `http://localhost/`
- Login: `http://localhost/login`
- Admin: `http://localhost/admin`
- Backend health: `http://localhost/api/health`

## Runtime Topology

- Browser hits **nginx**
- `nginx` forwards:
  - `/api/*` → ASP.NET Core backend
  - `/media/*` → ASP.NET Core backend
  - everything else → Next.js frontend
- Next.js server components fetch backend data through the API base helpers in `src/lib/api/*`
- ASP.NET Core owns:
  - auth challenge / callback / session / logout
  - admin content mutation APIs
  - upload/delete APIs
  - PostgreSQL persistence
  - local media file serving

See `ARCHITECTURE.md` for the detailed system view.

## Requirements

- Docker / Docker Compose
- Node.js 20+ and npm

Optional for direct backend local work:
- .NET 10 SDK

## Local Run

### 1. Start the dev stack

```bash
docker compose -f docker-compose.dev.yml up -d --build
```

### 2. Check health

```bash
curl -fsS http://localhost/api/health
```

### 3. Open the app

```text
http://localhost/
```

### 4. Stop the stack

```bash
docker compose down
```

## Local Development Modes

### Supported dev full-stack mode

```bash
docker compose -f docker-compose.dev.yml up -d --build
```

### Dev mode with local-admin shortcut enabled

```bash
./scripts/dev-up.sh
```

Use this mode for:
- login/auth validation
- uploads
- admin mutation checks
- e2e stack regression

### UI-only iteration mode

```bash
npm install
npm run dev
```

Use this only for:
- isolated frontend styling
- component/UI iteration

Do **not** treat it as the source of truth for:
- auth callback behavior
- admin API ownership
- upload routing
- final regression sign-off

## Branch Strategy

This repository now treats `dev` as the default integration branch and `main` as the production branch.

- `feature/*` branches merge into `dev`
- `dev` is the branch where full-stack development continues
- `main` is the branch that should be safe to deploy
- `main` must not expose dev-only affordances such as `Continue as Local Admin`

### Dev-only auth behavior

Two flags control the local admin shortcut:

- `ENABLE_LOCAL_ADMIN_SHORTCUT`
- `Auth__EnableTestLoginEndpoint`

Defaults:

- `docker-compose.dev.yml` enables local admin shortcut and test-login
- `docker-compose.prod.yml` disables local admin shortcut and test-login
- `./scripts/dev-up.sh` turns both flags `true`
- `./scripts/main-up.sh` simulates the production stack locally with local-tag images
- `.github/workflows/ci-dev.yml` validates source-build dev integration
- `.github/workflows/ci-main-runtime.yml` validates image-based production shape
- `.github/workflows/publish-ghcr-main.yml` runs only after `CI Main Runtime` succeeds on `main`

This means:

- `main` / production-safe runs do **not** expose the local admin shortcut
- local development can still opt in explicitly
- CI proves the policy through the full compose stack:
  - `dev` mode must expose `Continue as Local Admin` and keep `test-login` enabled
  - `main` mode must hide `Continue as Local Admin` and return `404` from `test-login`

### Compose-first CI policy

The repository no longer treats standalone image builds as sufficient release evidence.

- `CI Dev` boots `docker-compose.dev.yml`
- `CI Main Runtime` boots `docker-compose.prod.yml`
- `CI Main Runtime` uses locally tagged images to prove the production compose contract before publish
- `Publish GHCR Main` only runs after `CI Main Runtime` succeeds on `main`

The shared smoke entrypoint is:

```bash
./scripts/ci-compose-smoke.sh dev
./scripts/ci-compose-smoke.sh main
```

It verifies:

- compose config for the selected runtime file
- dev: source build via `docker-compose.dev.yml`
- main: image-based launch via `docker-compose.prod.yml`
- backend health via `/api/health`
- frontend routing via `/` and `/login`
- db-backed public pages via `/blog` and `/works`
- branch-policy auth behavior on `/login`
- branch-policy auth behavior on `/api/auth/test-login`

### Daily development flow

1. Start from `dev`
2. Create a feature branch from `dev`
3. Push the feature branch
4. Open a PR into `dev`
5. Merge into `dev` after checks pass

Example:

```bash
git switch dev
git pull origin dev
git switch -c feature/my-change
git push -u origin feature/my-change
```

### CI and publish flow

1. `feature/*` push or PR to `dev`
   - runs `CI Dev`
2. `dev` push
   - runs `CI Dev`
3. successful `CI Dev` completion on `dev`
   - triggers `Publish GHCR Dev`
   - publishes staging images with `:dev` and `:dev-sha-<sha>` tags
3. `release/main-promote -> main` PR
   - runs `CI Main Runtime`
4. `main` push
   - runs `CI Main Runtime`
5. successful `CI Main Runtime` completion on `main`
   - triggers `Publish GHCR Main`
   - reruns prod compose smoke
   - publishes runtime images to GHCR only after the `main` smoke passes

### Promote `dev` into `main`

`main` is promoted from `dev` through a runtime-only export so production does not accumulate test files, planning notes, or agent assets.

Prepare the promotion worktree:

```bash
./scripts/promote-main-runtime.sh
```

This creates a runtime-only worktree on `release/main-promote` using the allowlist in:

- [`scripts/main-runtime-allowlist.txt`](./scripts/main-runtime-allowlist.txt)

Then push the promotion branch:

```bash
git -C ../woong-blog-main-runtime push origin HEAD:release/main-promote
```

Then open a PR:

```text
release/main-promote -> main
```

If you want GitHub Actions to prepare and push the promotion branch for you:

```text
Actions -> Promote Main Runtime -> Run workflow
```

### What stays out of `main`

The promotion allowlist is designed so `main` can stay focused on runtime/deploy assets.

Examples of content that should not be promoted:

- `tests/`
- `.codex/`
- `.agents/`
- planning / todo markdown
- local QA artifacts

The intent is:

- `dev` keeps the full engineering surface
- `main` keeps the runtime surface

Production deployment details live in [DEPLOYMENT.md](./DEPLOYMENT.md).
Staging pull/up details live in [STAGING.md](./STAGING.md).

## Operator Flow

이 저장소는 아래 3단계 흐름으로 운영한다.

1. `dev`
   - source-build 개발 및 검증 브랜치
   - `docker-compose.dev.yml`
   - `CI Dev`
2. `staging`
   - `dev`에서 publish된 `:dev` 이미지 검증 환경
   - `docker-compose.staging.yml`
   - 별도 폴더에서 `pull && up -d`
3. `main`
   - production runtime-only 브랜치
   - `docker-compose.prod.yml`
   - `CI Main Runtime`
   - `Publish GHCR Main`

## Dev Start

### 1. 브랜치 준비

```bash
git switch dev
git pull origin dev
git switch -c feature/my-change
```

### 2. 로컬 dev stack 기동

권장:

```bash
./scripts/dev-up.sh
```

동등 명령:

```bash
docker compose --env-file .env -f docker-compose.dev.yml up -d --build
```

### 3. 로컬 dev 확인

```bash
curl -fsS http://localhost/api/health
curl -fsS http://localhost/login | head
curl -fsS http://localhost/works | head
curl -fsS http://localhost/blog | head
```

dev 기대값:
- `/login`에 `Continue as Local Admin`가 보여야 함
- `/api/auth/test-login`이 동작해야 함
- `backend`는 필요 시 `8080`으로 직접 확인 가능

### 4. dev 품질 게이트

```bash
npm run lint
npm run typecheck
npm run test -- --run
./scripts/ci-compose-smoke.sh dev
PLAYWRIGHT_SKIP_AUTH_BOOTSTRAP=1 PLAYWRIGHT_EXPECT_LOCAL_ADMIN_SHORTCUT=visible PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://localhost npx playwright test tests/ci-compose-public.spec.ts --project=chromium-public --workers=1
PLAYWRIGHT_EXPECT_LOCAL_ADMIN_SHORTCUT=visible PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://localhost npx playwright test tests/test-server-runtime.spec.ts tests/auth-security-browser.spec.ts tests/public-admin-affordances.spec.ts tests/ui-header-overlays.spec.ts --project=chromium-runtime-auth --workers=1
```

### 5. dev 반영

```bash
git push -u origin feature/my-change
```

PR:

```text
feature/my-change -> dev
```

## Staging Validation

### 목적

`main`으로 올리기 전에, `dev`에서 publish된 이미지를 **별도 폴더**에서 `pull && up -d`로 띄워서 실제 홈서버/스테이징 운용 흐름을 검증한다.

### 1. staging 이미지 publish

`dev`가 green이면 `Publish GHCR Dev`가 `:dev`, `:dev-sha-<sha>` 태그를 publish한다.

예시:

```text
ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-frontend:dev
ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-backend:dev
```

### 2. staging 폴더 준비

```bash
mkdir -p ~/woong-blog-staging
cd ~/woong-blog-staging
cp /path/to/repo/docker-compose.staging.yml .
cp /path/to/repo/.env.staging.example .env.staging
cp -r /path/to/repo/nginx ./nginx
mkdir -p certbot/www certbot/conf/live/current
```

### 3. `.env.staging` 수정

최소:

```env
FRONTEND_IMAGE=ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-frontend:dev
BACKEND_IMAGE=ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-backend:dev
POSTGRES_PASSWORD=change-me
Auth__ClientId=replace-me
Auth__ClientSecret=replace-me
Auth__AdminEmails__0=admin@example.com
```

### 4. pull / up

```bash
echo "$GHCR_TOKEN" | docker login ghcr.io -u YOUR_GITHUB_USERNAME --password-stdin
docker compose --env-file .env.staging -f docker-compose.staging.yml pull
docker compose --env-file .env.staging -f docker-compose.staging.yml up -d
docker compose --env-file .env.staging -f docker-compose.staging.yml ps
```

### 5. staging 기능 확인

빠른 확인:

```bash
curl -I http://localhost/
curl -I http://localhost/login
curl -I http://localhost/works
curl -I http://localhost/blog
```

브라우저 검증:

```bash
PLAYWRIGHT_HEADED=1 PLAYWRIGHT_SKIP_AUTH_BOOTSTRAP=1 PLAYWRIGHT_EXPECT_LOCAL_ADMIN_SHORTCUT=hidden PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://localhost npx playwright test tests/ci-compose-public.spec.ts --project=chromium-public --headed --workers=1
```

추가 public 검증:

```bash
PLAYWRIGHT_HEADED=1 PLAYWRIGHT_SKIP_AUTH_BOOTSTRAP=1 PLAYWRIGHT_EXPECT_LOCAL_ADMIN_SHORTCUT=hidden PLAYWRIGHT_EXTERNAL_SERVER=1 PLAYWRIGHT_BASE_URL=http://localhost npx playwright test tests/home.spec.ts tests/public-content.spec.ts --project=chromium-public --headed --workers=1
```

staging 기대값:
- `/login`에 `Continue as Local Admin`가 없어야 함
- `/api/auth/test-login`은 `404`여야 함
- `frontend/backend`는 외부 포트를 직접 열지 않아야 함
- 홈/works/blog가 정상 렌더돼야 함

## Main Promotion

### 1. runtime-only promotion branch 생성

```bash
./scripts/promote-main-runtime.sh
git -C ../woong-blog-main-runtime status --short
git -C ../woong-blog-main-runtime push origin HEAD:release/main-promote
```

### 2. PR 생성

```text
release/main-promote -> main
```

### 3. main 검증

PR 또는 merge 뒤 기대 흐름:
- `CI Main Runtime` green
- `Publish GHCR Main` green

main 기대값:
- `Continue as Local Admin` hidden
- `/api/auth/test-login` 404
- `docker-compose.prod.yml`는 `image:`만 사용
- 운영 이미지는 GHCR `:main`, `:latest`, `:sha-<sha>`로 publish

### 4. 운영 배포

```bash
docker compose --env-file .env.prod -f docker-compose.prod.yml pull
docker compose --env-file .env.prod -f docker-compose.prod.yml up -d
```

### Hotfix rule

If a hotfix lands on `main`, it must be replayed back to `dev` immediately so the branches do not drift.

Recommended sequence:

```bash
git switch dev
git pull origin dev
git merge origin/main
git push origin dev
```

## Test and Verification Commands

### Frontend

```bash
npm run test -- --run
npm run lint
npm run typecheck
npm run build
```

### Backend tests via Dockerized SDK

```bash
docker run --pull=never --rm -v "$PWD/backend:/src" -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet test tests/WoongBlog.Api.Tests/WoongBlog.Api.Tests.csproj
```

### Database smoke/load

```bash
./scripts/db-load-smoke.sh
```

## Database Migration and Bootstrap

The current repository does not use a checked-in EF Core migration chain for runtime deployment.
Instead, the backend boot path creates and patches the schema on startup:

- `EnsureCreatedAsync(...)`
- custom SQL bootstrap in `backend/src/WoongBlog.Api/Infrastructure/Persistence/DatabaseBootstrapper.cs`
- seed/bootstrap logic in `backend/src/WoongBlog.Api/Infrastructure/Persistence/Seeding/SeedData.cs`

That means the practical DB migration flow today is:

1. start `db`
2. start `backend`
3. let the backend create missing tables and apply schema patches
4. optionally run the Notion blog import scripts to backfill real content

### Local DB bootstrap

```bash
docker compose up -d db backend
docker compose exec -T db psql -U portfolio -d portfolio -X -qAt -c "SELECT count(*) FROM \"Blogs\";"
```

If the backend has connected successfully, the schema should exist and the query above should return a number instead of an error.

### PostgreSQL bind mount

PostgreSQL data is persisted through `POSTGRES_DATA_DIR`.

Default compose targets:

- `docker-compose.yml` -> `./.docker-data/local/postgres`
- `docker-compose.dev.yml` -> `./.docker-data/dev/postgres`
- `docker-compose.staging.yml` -> `./.docker-data/staging/postgres`
- `docker-compose.prod.yml` -> `./.docker-data/prod/postgres`

Example:

```bash
POSTGRES_DATA_DIR=/home/kimwoonggon/.woong-blog-docker/local/postgres docker compose up -d db backend
```

On WSL, a Linux-home path is safer than a `/mnt/c` or `/mnt/d` bind mount because PostgreSQL may fail with permission errors on NTFS-backed paths.

### Notion blog data migration

The blog backfill path is Node.js-based and imports downloaded Notion exports into PostgreSQL `Blogs` and `Assets`, while copying downloaded files into backend media storage.

Primary scripts:

- `scripts/import-notion-downloads-to-db.mjs`
- `scripts/migrate-notion-blog-downloads.mjs`
- `scripts/notion-db-import-lib.mjs`

Default multi-folder migration:

```bash
npm run migrate:blog:notion
```

This currently processes:

- `downloads/notion-connected-2026-03-27T10-24-20-364Z`
- `downloads/notion-connected-2026-04-13T08-03-24-517Z`

Custom target folders:

```bash
node scripts/migrate-notion-blog-downloads.mjs \
  downloads/notion-connected-2026-03-27T10-24-20-364Z \
  downloads/notion-connected-2026-04-13T08-03-24-517Z
```

Single-folder import:

```bash
NOTION_EXPORT_DIR=downloads/notion-connected-2026-03-27T10-24-20-364Z \
node scripts/import-notion-downloads-to-db.mjs
```

Targeting another compose stack, such as production:

```bash
DOCKER_COMPOSE_ENV_FILE=.env.prod.local \
DOCKER_COMPOSE_FILES=docker-compose.prod.yml \
POSTGRES_DB=portfolio \
POSTGRES_USER=portfolio \
node scripts/migrate-notion-blog-downloads.mjs
```

### Migration verification

Status and summary files:

- `db_status/current.json`
- `db_status/notion-migration-01.json`
- `db_status/notion-migration-02.json`
- `db_status/notion-blog-migration-summary.json`

Quick DB checks:

```bash
docker compose exec -T db psql -U portfolio -d portfolio -X -qAt -c "SELECT count(*) FROM \"Blogs\";"
docker compose exec -T db psql -U portfolio -d portfolio -X -qAt -c "SELECT count(*) FROM \"Assets\" WHERE \"Path\" LIKE 'blogs/notion/%';"
docker compose exec -T backend sh -lc "find /app/media/blogs/notion -type f | wc -l"
```

Recent imported blog rows:

```bash
docker compose exec -T db psql -U portfolio -d portfolio -X -qAt -F $'\t' -c "SELECT \"Title\", \"Slug\", to_char(\"PublishedAt\", 'YYYY-MM-DD HH24:MI:SSOF') FROM \"Blogs\" ORDER BY \"PublishedAt\" DESC NULLS LAST LIMIT 10;"
```

The import is idempotent at the DB row level because it matches existing blog rows by embedded Notion page marker and slug before deciding between `INSERT` and `UPDATE`.

### Full stack browser regression

```bash
npm run test:e2e:stack
```

### Manual auth/browser check

```bash
npm run test:e2e:manual-auth
```

## Auth Configuration

Auth settings live in:

- `backend/src/WoongBlog.Api/appsettings.json`
- `backend/src/WoongBlog.Api/appsettings.Development.json`
- environment variables consumed by ASP.NET Core

Key fields:

- `Auth:Enabled`
- `Auth:Authority`
- `Auth:ClientId`
- `Auth:ClientSecret`
- `Auth:CallbackPath`
- `Auth:AdminEmails`
- `Auth:DataProtectionKeysPath`
- `Auth:MediaRoot`

The backend issues cookie sessions and validates them against persisted auth session records.

## Media Storage

Uploaded files are persisted by the backend and exposed under:

```text
/media/*
```

Compose volumes:

- `media-storage`
- `data-protection-keys`
- bind mount `POSTGRES_DATA_DIR` -> `/var/lib/postgresql/data`

Default bind mount paths:

- local compose: `./.docker-data/local/postgres`
- dev compose: `./.docker-data/dev/postgres`
- staging compose: `./.docker-data/staging/postgres`
- prod compose: `./.docker-data/prod/postgres`

## Local HTTPS (mkcert + nginx)

If you want to manually verify real Google login on localhost over HTTPS, use the local mkcert + nginx setup.

### 1. Generate trusted localhost certs

```bash
./scripts/setup-local-https.sh
```

This creates:
- `.local-certs/localhost.pem`
- `.local-certs/localhost-key.pem`

### 2. Start the HTTPS stack

```bash
./scripts/run-local-https.sh
```

Equivalent manual command:

```bash
NGINX_DEFAULT_CONF=./nginx/local-https.conf docker compose -f docker-compose.yml -f docker-compose.https.yml up -d --build
```

### 3. Open the app

```text
https://localhost/login
```

### 4. Google OAuth redirect URI
Register this exact URI in Google Cloud Console:

```text
https://localhost/api/auth/callback
```

Notes:
- this local HTTPS path keeps nginx as the browser-facing reverse proxy
- the HTTPS override forces secure auth cookies for this local verification path
- plain HTTP local Compose still works with the default `docker compose up -d --build` path

For reverse-proxied staging/production, set `Auth__PublicOrigin` to the exact browser-facing origin, for example `https://woonglab.com`. This keeps the OIDC `redirect_uri` stable even when forwarded host detection or multiple public hostnames would otherwise vary it. If nginx canonicalizes `www` to the apex domain, keep `Auth__PublicOrigin` on that apex origin too.

## Deployment

The **currently supported runtime architecture** is a containerized full stack:

- `frontend` (Next.js)
- `backend` (ASP.NET Core)
- `db` (PostgreSQL)
- `nginx`

The repository still contains a legacy GitHub Actions workflow that builds Docker and deploys the Next.js app to **Vercel**, but that workflow does **not** fully provision the current ASP.NET Core + PostgreSQL + nginx stack on its own.

So today:

- **Supported local run:** Docker Compose
- **Recommended full-stack deployment model:** Docker Compose (or an equivalent multi-container host/platform)
- **Current Vercel workflow:** useful historical artifact / partial deployment path, **not** the complete deployment story for the current architecture

For the actual deployment checklist and step-by-step runbook, see:

- [`DEPLOYMENT.md`](./DEPLOYMENT.md)

## Azure Backup Workflow

Production backups write two blobs to Azure Blob Storage:

- `media.tar.gz`
- `postgres.dump`

Run a backup with:

```bash
APP_ENV_FILE=.env.prod node scripts/azure-backup.mjs
```

Use `--dry-run` to validate the resolved paths and blob names without uploading anything.

Restore a selected backup with an explicit confirmation guard:

```bash
APP_ENV_FILE=.env.prod node scripts/azure-restore.mjs --backup-id 20260418T000000Z --confirm
```

Restore also supports `--dry-run`.

Install the daily cron entry with:

```bash
APP_ENV_FILE=.env.prod node scripts/install-azure-backup-cron.mjs
```

The cron job runs at `07:00` in `Asia/Seoul` and reuses the repository's `.env.prod` file.

## CI / CD Branch Rules

Current workflow intent:

- pushes and PRs to `feature/*`, `dev`, and `main` run CI/build checks
- pushes to `main` are the only path that publish runtime images to GHCR
- runtime-only promotion can be triggered manually through `Promote Main Runtime`

So the normal release path is:

1. `feature/* -> dev`
2. verify on `dev`
3. promote runtime-only tree to `release/main-promote`
4. merge `release/main-promote -> main`
5. publish runtime images from `main` to GHCR
6. pull those images from the cloud host and launch the docker services

## Repository Highlights

- `src/app/` — Next.js app router frontend
- `src/lib/api/` — frontend/backend API boundary helpers
- `backend/src/WoongBlog.Api/` — ASP.NET Core application
- `backend/tests/WoongBlog.Api.Tests/` — backend tests
- `tests/` — Playwright stack/browser regressions
- `nginx/default.conf` — edge routing contract
- `docker-compose.yml` — stack orchestration

## Current Architecture Rules

1. **Single runtime truth:** no active Supabase runtime dependency in `src/`
2. **Backend owns auth and uploads**
3. **Compose/nginx is the supported verification path**
4. **Docs must match the live stack**

## AI Features

Optional AI editor routes still exist under:

- `/api/ai/enrich-work`
- `/api/ai/fix-blog`

If needed, set:

```bash
OPENAI_API_KEY=...
OPENAI_MODEL=...
```

or Azure equivalents.

## Refactor Safety Notes

When changing architecture/runtime paths:

1. cut one authority boundary at a time
2. verify after each slice
3. prefer deletion over compatibility wrappers
4. keep commits rollback-safe and Lore-formatted

## Primary References

- Next.js Route Handlers: https://nextjs.org/docs/app/api-reference/file-conventions/route
- Next.js Data Fetching: https://nextjs.org/docs/app/getting-started/fetching-data
- Docker Compose Networking: https://docs.docker.com/compose/how-tos/networking/
- ASP.NET Core OIDC auth: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-oidc-web-authentication?view=aspnetcore-9.0
- ASP.NET Core Data Protection: https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/introduction?view=aspnetcore-9.0


## Authentication & Security

The application uses an **ASP.NET Core-owned cookie session** for browser authentication. Google social login is initiated from `/login`, completed on the backend, and the backend issues the app's own secure session cookie.

Security baseline:
- HttpOnly cookie session
- Secure cookie in production
- SameSite=Lax by default
- CSRF protection required for browser mutation requests
- trusted forwarded-header handling behind nginx/IIS
- HSTS / HTTPS redirection / security headers / auth rate limiting

See [`SECURITY.md`](./SECURITY.md) for the rationale, deployment checklist, and secure reverse-proxy sample config.

## License

This repository is not distributed under Apache, MIT, or another permissive
open-source license.

Copyright (c) 2026 Woonggon Kim. All rights reserved.

See [LICENSE](./LICENSE) for the repository license terms.
