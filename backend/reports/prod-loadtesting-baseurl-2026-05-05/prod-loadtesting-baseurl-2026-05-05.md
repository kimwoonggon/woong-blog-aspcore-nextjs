# Prod Real Backend LoadTesting BaseUrl Audit - 2026-05-05

## Scope
Fix prod configuration so Real Backend Test does not silently fall back to the code default `http://127.0.0.1:3000` when `LoadTesting__BaseUrl` is missing.

## Changed
- Added `LoadTesting__BaseUrl: ${LoadTesting__BaseUrl:-http://127.0.0.1:8080}` to the `backend` service in `docker-compose.prod.yml`.
- Added `.env.prod.example` documentation and default example `LoadTesting__BaseUrl=https://woonglab.com` for measuring the public HTTPS/nginx path.
- Added `LoadTesting__BaseUrl=http://127.0.0.1:8080` to `scripts/main-up.sh` for generated local `.env.prod.local` bootstrap files.
- Added a dated TODO entry in `todolist-2026-05-05.md`.

## Intentionally Not Changed
- Did not create or commit a real `.env.prod` or `.env.prod.local` because those are deployment/secret-bearing runtime files.
- Did not change the application code default in `LoadTestingOptions`; this task is focused on prod deployment plumbing.
- Did not change nginx routing or load-test scenario logic.

## Goal Verification
- Prod compose now explicitly passes `LoadTesting__BaseUrl` to the backend container.
- If the prod env file defines `LoadTesting__BaseUrl`, that value is used.
- If the value is absent from the compose interpolation environment, prod compose falls back to `http://127.0.0.1:8080` instead of the application default `http://127.0.0.1:3000`.
- `.env.prod.example` documents the preferred public HTTPS value and the direct-backend alternative.

## Validations
- `env APP_ENV_FILE=.env.prod.example docker compose --env-file .env.prod.example -f docker-compose.prod.yml config` rendered `LoadTesting__BaseUrl: https://woonglab.com` for the backend service.
- A no-LoadTesting env fixture rendered the fallback `LoadTesting__BaseUrl: http://127.0.0.1:8080`.
- `rg -n "LoadTesting__BaseUrl" docker-compose.prod.yml .env.prod.example scripts/main-up.sh todolist-2026-05-05.md` confirmed all intended references.

## Risks And Follow-Ups
- The real server must still add `LoadTesting__BaseUrl` to its actual `.env.prod` or `.env.prod.local` and restart the backend service.
- `https://woonglab.com` measures the full public path, including nginx/TLS and any external network path. Use it for production-public measurements.
- `http://127.0.0.1:8080` measures the backend container directly and avoids public network dependencies, but it bypasses nginx/TLS.

## Recommendation
Add `LoadTesting__BaseUrl=https://woonglab.com` to the server's actual prod env file when the goal is public HTTPS Real Backend Test. Then restart `backend` and confirm with `printenv` and a health curl from inside the backend container.
