import { spawnSync } from 'node:child_process'
import { chmodSync, mkdirSync, mkdtempSync, writeFileSync } from 'node:fs'
import os from 'node:os'
import path from 'node:path'
import { describe, expect, it } from 'vitest'

const repoRoot = path.resolve(__dirname, '../..')
const scriptPath = path.join(repoRoot, 'scripts/prod-runtime-preflight.sh')

type FakeRuntimeOptions = {
  loadTestingBaseUrl?: string
  includeNginxRequestTime?: boolean
  diagnosticsSampleCount?: number
}

function makeExecutable(filePath: string, content: string) {
  writeFileSync(filePath, content)
  chmodSync(filePath, 0o755)
}

function createFakeRuntime(options: FakeRuntimeOptions = {}) {
  const root = mkdtempSync(path.join(os.tmpdir(), 'prod-runtime-preflight-'))
  const fakeBin = path.join(root, 'bin')
  const adminCookieFile = path.join(root, 'admin.cookies')
  const loadTestingBaseUrl = options.loadTestingBaseUrl ?? 'https://woonglab.test'
  const includeNginxRequestTime = options.includeNginxRequestTime ?? true
  const diagnosticsSampleCount = options.diagnosticsSampleCount ?? 12

  mkdirSync(fakeBin, { recursive: true })
  writeFileSync(adminCookieFile, 'admin-session')

  makeExecutable(path.join(fakeBin, 'docker'), `#!/usr/bin/env bash
set -euo pipefail
if [[ "$*" == *" config"* ]]; then
  cat <<'OUT'
services:
  backend:
    environment:
      LoadTesting__BaseUrl: ${loadTestingBaseUrl}
      ConnectionStrings__Postgres: Host=db;Password=super-secret;Maximum Pool Size=40
OUT
  exit 0
fi
if [[ "$*" == *" ps --status running --services"* ]]; then
  printf 'backend\\nfrontend\\nnginx\\ndb\\n'
  exit 0
fi
if [[ "$*" == *" exec -T backend printenv"* ]]; then
  cat <<'OUT'
ASPNETCORE_ENVIRONMENT=Production
LoadTesting__BaseUrl=${loadTestingBaseUrl}
POSTGRES_MAX_POOL_SIZE=40
ConnectionStrings__Postgres=Host=db;Password=super-secret;Maximum Pool Size=40
OUT
  exit 0
fi
if [[ "$*" == *" exec -T backend sh -lc"* ]]; then
  cat <<'OUT'
processor_count=2
memory_max=8589934592
cpu_max=200000 100000
OUT
  exit 0
fi
echo "unexpected docker call: $*" >&2
exit 1
`)

  makeExecutable(path.join(fakeBin, 'curl'), `#!/usr/bin/env bash
set -euo pipefail
headers=''
output=''
url=''
while [[ "$#" -gt 0 ]]; do
  case "$1" in
    -D) headers="$2"; shift 2 ;;
    -o) output="$2"; shift 2 ;;
    -w) shift 2 ;;
    -H|-b) shift 2 ;;
    -k|-s|-S|-f|-L|-I) shift ;;
    --*) shift ;;
    http*) url="$1"; shift ;;
    *) shift ;;
  esac
done
if [[ -n "$headers" ]]; then
  {
    printf 'HTTP/2 200\\r\\n'
    printf 'X-App-Elapsed-Ms: 4.2\\r\\n'
    ${includeNginxRequestTime ? "printf 'X-Nginx-Request-Time: 0.006\\\\r\\\\n'" : ':'}
    printf 'X-Nginx-Upstream-Time: 0.005\\r\\n'
    if [[ "$url" == *"/api/public/works"* ]]; then
      printf 'Content-Encoding: gzip\\r\\n'
    fi
    printf '\\r\\n'
  } > "$headers"
fi
if [[ -n "$output" ]]; then
  if [[ "$url" == *"/api/admin/load-test/diagnostics"* ]]; then
    cat > "$output" <<'OUT'
{"process":{"processorCount":2,"memoryBytes":200000000},"database":{"status":"available","commandLatency":{"sampleCount":${diagnosticsSampleCount},"p95Ms":5.1},"connectionOpenLatency":{"sampleCount":${diagnosticsSampleCount},"p95Ms":0.4},"pool":{"dbContextPoolSize":128,"npgsqlMaximumPoolSize":40,"npgsqlPoolLimitSource":"connection-string"}}}
OUT
  else
    printf '{"status":"ok"}' > "$output"
  fi
fi
printf '200'
`)

  return { root, fakeBin, adminCookieFile }
}

function runPreflight(fakeBin: string, extraEnv: Record<string, string> = {}) {
  return spawnSync('bash', [scriptPath], {
    cwd: repoRoot,
    env: {
      ...process.env,
      PATH: `${fakeBin}${path.delimiter}${process.env.PATH ?? ''}`,
      BASE_URL: 'https://woonglab.test',
      APP_ENV_FILE: '.env.prod',
      COMPOSE_FILE: 'docker-compose.prod.yml',
      REQUIRE_ADMIN_DIAGNOSTICS: '1',
      CURL_INSECURE: '1',
      ...extraEnv,
    },
    encoding: 'utf8',
  })
}

describe('production runtime preflight script', () => {
  it('probes compose, nginx/app headers, gzip, cgroup resources, and admin diagnostics without leaking secrets', () => {
    const runtime = createFakeRuntime()

    const result = runPreflight(runtime.fakeBin, { ADMIN_COOKIE_FILE: runtime.adminCookieFile })

    expect(result.status, `${result.stdout}\n${result.stderr}`).toBe(0)
    expect(result.stdout).toContain('[prod-runtime-preflight] PASS')
    expect(result.stdout).toContain('LoadTesting__BaseUrl=https://woonglab.test')
    expect(result.stdout).toContain('nginx request_time header: available')
    expect(result.stdout).toContain('app elapsed header: available')
    expect(result.stdout).toContain('gzip public response: available')
    expect(result.stdout).toContain('db command samples: 12')
    expect(result.stdout).toContain('npgsql max pool: 40')
    expect(result.stdout).toContain('processor_count=2')
    expect(result.stdout).toContain('memory_max=8589934592')
    expect(result.stdout).not.toContain('super-secret')
    expect(result.stderr).not.toContain('super-secret')
  })

  it('fails when load testing would bypass nginx through a backend-direct URL', () => {
    const runtime = createFakeRuntime({ loadTestingBaseUrl: 'http://127.0.0.1:8080' })

    const result = runPreflight(runtime.fakeBin, { ADMIN_COOKIE_FILE: runtime.adminCookieFile })

    expect(result.status).not.toBe(0)
    expect(result.stderr).toContain('backend-direct')
    expect(result.stdout).not.toContain('super-secret')
    expect(result.stderr).not.toContain('super-secret')
  })

  it('fails when nginx request timing is missing from public API responses', () => {
    const runtime = createFakeRuntime({ includeNginxRequestTime: false })

    const result = runPreflight(runtime.fakeBin, { ADMIN_COOKIE_FILE: runtime.adminCookieFile })

    expect(result.status).not.toBe(0)
    expect(result.stderr).toContain('X-Nginx-Request-Time header is missing')
    expect(result.stdout).not.toContain('super-secret')
    expect(result.stderr).not.toContain('super-secret')
  })

  it('fails when admin diagnostics are required but DB command samples are unavailable', () => {
    const runtime = createFakeRuntime({ diagnosticsSampleCount: 0 })

    const result = runPreflight(runtime.fakeBin, { ADMIN_COOKIE_FILE: runtime.adminCookieFile })

    expect(result.status).not.toBe(0)
    expect(result.stderr).toContain('DB command latency samples')
    expect(result.stdout).not.toContain('super-secret')
    expect(result.stderr).not.toContain('super-secret')
  })
})
