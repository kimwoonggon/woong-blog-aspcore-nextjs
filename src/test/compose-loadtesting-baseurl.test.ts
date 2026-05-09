import { readFileSync } from 'node:fs'
import path from 'node:path'
import { describe, expect, it } from 'vitest'

const repoRoot = process.cwd()

function readRepoFile(relativePath: string) {
  return readFileSync(path.join(repoRoot, relativePath), 'utf8')
}

describe('LoadTesting__BaseUrl compose contract', () => {
  it.each([
    ['docker-compose.prod.yml', 'https://woonglab.com'],
    ['docker-compose.staging.yml', 'https://staging.example.com'],
  ])('%s defaults real backend tests to the public nginx origin', (composeFile, fallbackOrigin) => {
    const compose = readRepoFile(composeFile)

    expect(compose).toContain(
      `LoadTesting__BaseUrl: \${LoadTesting__BaseUrl:-\${NEXT_PUBLIC_SITE_URL:-${fallbackOrigin}}}`,
    )
    expect(compose).not.toContain('LoadTesting__BaseUrl: http://127.0.0.1:8080')
  })

  it.each([
    ['.env.prod.example', 'https://woonglab.com'],
    ['.env.staging.example', 'https://staging.example.com'],
  ])('%s documents the same public load-test origin default', (envFile, expectedOrigin) => {
    const envExample = readRepoFile(envFile)

    expect(envExample).toContain(`LoadTesting__BaseUrl=${expectedOrigin}`)
  })
})
