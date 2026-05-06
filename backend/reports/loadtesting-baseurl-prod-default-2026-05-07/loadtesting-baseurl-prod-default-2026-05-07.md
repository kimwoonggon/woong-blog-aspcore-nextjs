# LoadTesting BaseUrl Prod Default Guard - 2026-05-07

## Scope
- Objective item covered: nginx/app/DB timing observability for Real Backend Test attribution.
- This slice fixes production compose configuration drift where Real Backend Test could default to backend-direct `http://127.0.0.1:8080` when `LoadTesting__BaseUrl` was omitted.
- The target behavior is that production/main runtime load tests go through the public/nginx origin by default, so client/nginx/app/DB timing attribution can be collected from the same path users exercise.

## RED Evidence
- Before the change, a focused prod compose config check with `NEXT_PUBLIC_SITE_URL=https://example.test` and no explicit `LoadTesting__BaseUrl` resolved to:
  - `LoadTesting__BaseUrl: http://127.0.0.1:8080`
- That bypasses nginx and explains why Real Backend Test can show nginx timing as unavailable even when nginx headers exist on the public path.

## Changes Made
- `docker-compose.prod.yml`
  - Changed backend `LoadTesting__BaseUrl` default from backend-direct `http://127.0.0.1:8080` to `${NEXT_PUBLIC_SITE_URL:-https://woonglab.com}`.
- `scripts/main-up.sh`
  - Added `NEXT_PUBLIC_SITE_URL=http://localhost` to generated `.env.prod.local`.
  - Changed local main bootstrap `LoadTesting__BaseUrl` from `http://127.0.0.1:8080` to `http://localhost`.
- `scripts/ci-compose-smoke.sh`
  - Writes `NEXT_PUBLIC_SITE_URL=${base_url}` into generated main smoke env.
  - Captures `docker compose config` output.
  - Fails main smoke early if `LoadTesting__BaseUrl` does not resolve to the nginx/public base URL.
  - Fails main smoke early if the backend-direct default `http://127.0.0.1:8080` appears.
- `todolist-2026-05-07.md`
  - Recorded the slice plan, RED result, GREEN changes, and validation evidence.

## Validation Performed
- Focused prod compose config check with `NEXT_PUBLIC_SITE_URL=https://example.test` and no explicit `LoadTesting__BaseUrl`:
  - Result: `LoadTesting__BaseUrl: https://example.test`
- Focused prod compose fallback config with no `NEXT_PUBLIC_SITE_URL` and no explicit `LoadTesting__BaseUrl`:
  - Result: `LoadTesting__BaseUrl: https://woonglab.com`
- Script syntax check:
  - `bash -n scripts/main-up.sh scripts/ci-compose-smoke.sh`
  - Result: pass
- Diff whitespace/static check:
  - `git diff --check -- docker-compose.prod.yml scripts/main-up.sh scripts/ci-compose-smoke.sh todolist-2026-05-07.md`
  - Result: pass

## Intentionally Not Changed
- No backend production code was changed.
- No frontend UI code was changed.
- No load-test scenario shape, target selection, page size, RPS, duration, or max VUs behavior was changed.
- No cache behavior was introduced.
- No nginx upstream timing header implementation was changed.

## Risks And Yellow Flags
- This closes the backend-direct default configuration hole for production/main compose and local main bootstrap.
- It does not by itself make `X-Nginx-Upstream-Time` parseable when nginx returns `-`; k6 will still use `runner.http_waiting_fallback` for upstream attribution in that case.
- Full `scripts/ci-compose-smoke.sh main` was not run locally because it starts a production-like compose stack and can conflict with the existing dev compose ports. The new guard is exercised by the existing Main Runtime CI path.

## Recommendation
- Push this config/CI guard to `dev`, let CI verify the main compose smoke path, then promote to `main` if CI is green.
- After deployment, rerun Real Backend Test and confirm `client`, `nginx request_time`, `ASP.NET app elapsed`, and DB command diagnostics are populated when `LoadTesting__BaseUrl` is omitted.
