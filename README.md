# Woong Blog — Next.js + ASP.NET Core + PostgreSQL

Woong Blog is a split-stack portfolio/blog application:

- **Frontend:** Next.js 16 App Router
- **Backend:** ASP.NET Core (.NET 10)
- **Database:** PostgreSQL 16
- **Edge/runtime routing:** nginx + Docker Compose
- **Auth/session authority:** ASP.NET Core cookie + OpenID Connect
- **Media authority:** ASP.NET Core local file storage under `/media/*`

The current repository standard is **compose-first**:

- use `docker compose up -d --build` for full-stack work
- use `npm run test:e2e:stack` for real regression checks
- `npm run dev` may still help with isolated UI work, but it is **not** the supported verification path for auth, admin mutations, uploads, or final release checks
- GitHub Actions must validate the same compose scenario, not isolated `docker build` steps

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

### 1. Start the full stack

```bash
docker compose up -d --build
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

### Supported full-stack mode

```bash
docker compose up -d --build
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

- `docker-compose.yml` keeps both flags `false`
- `./scripts/dev-up.sh` turns both flags `true`
- `./scripts/main-up.sh` keeps both flags `false`
- `.github/workflows/ci.yml` resolves branch policy automatically:
  - `feature/*`, `dev`, and PRs targeting `dev` run in `dev` mode
  - `main` and PRs targeting `main` run in `main` mode
- `.github/workflows/publish-ghcr-main.yml` runs only after the `CI` workflow succeeds on `main`, then reruns the `main` compose smoke before GHCR push

This means:

- `main` / production-safe runs do **not** expose the local admin shortcut
- local development can still opt in explicitly
- CI proves the policy through the full compose stack:
  - `dev` mode must expose `Continue as Local Admin` and keep `test-login` enabled
  - `main` mode must hide `Continue as Local Admin` and return `404` from `test-login`

### Compose-first CI policy

The repository no longer treats standalone image builds as sufficient release evidence.

- CI must boot the compose stack: `db`, `backend`, `frontend`, `nginx`
- CI must pass branch-policy auth flags into the runtime
- `dev` mode:
  - `ENABLE_LOCAL_ADMIN_SHORTCUT=true`
  - `Auth__EnableTestLoginEndpoint=true`
- `main` mode:
  - `ENABLE_LOCAL_ADMIN_SHORTCUT=false`
  - `Auth__EnableTestLoginEndpoint=false`

The shared smoke entrypoint is:

```bash
./scripts/ci-compose-smoke.sh dev
./scripts/ci-compose-smoke.sh main
```

It verifies:

- `docker compose config`
- `docker compose up -d --build db backend frontend nginx`
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
   - runs compose-first CI in `dev` mode
2. `dev` push
   - runs compose-first CI in `dev` mode
3. `release/main-promote -> main` PR
   - runs compose-first CI in `main` mode
4. `main` push
   - runs compose-first CI in `main` mode
5. successful `CI` completion on `main`
   - triggers `publish-ghcr-main.yml`
   - reruns `main` compose smoke
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
- `postgres-data`

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
