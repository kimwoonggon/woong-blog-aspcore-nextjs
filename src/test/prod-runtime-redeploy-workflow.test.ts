import { readFileSync } from 'node:fs'
import path from 'node:path'
import { describe, expect, it } from 'vitest'

const repoRoot = process.cwd()

function readWorkflow(relativePath: string) {
  return readFileSync(path.join(repoRoot, relativePath), 'utf8')
}

describe('production runtime redeploy workflow', () => {
  it('provides a manual SSH deploy path that runs preflight before optional real load', () => {
    const workflow = readWorkflow('.github/workflows/prod-runtime-redeploy.yml')

    expect(workflow).toContain('workflow_dispatch:')
    expect(workflow).not.toContain('push:')
    expect(workflow).not.toContain('pull_request:')
    expect(workflow).not.toContain('StrictHostKeyChecking=no')
    expect(workflow).toContain('PROD_SSH_HOST: ${{ secrets.PROD_SSH_HOST }}')
    expect(workflow).toContain('PROD_SSH_USER: ${{ secrets.PROD_SSH_USER }}')
    expect(workflow).toContain('PROD_SSH_PRIVATE_KEY: ${{ secrets.PROD_SSH_PRIVATE_KEY }}')
    expect(workflow).toContain('PROD_SSH_KNOWN_HOSTS: ${{ secrets.PROD_SSH_KNOWN_HOSTS }}')
    expect(workflow).toContain('ssh-keygen -F "${PROD_SSH_HOST}"')
    expect(workflow).toContain('ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-frontend:main')
    expect(workflow).toContain('ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-backend:main')
    expect(workflow).toContain('REQUIRE_PUBLIC_WORK_VIDEO_CONTRACT=1')
    expect(workflow).toContain('./scripts/prod-runtime-preflight.sh')
    expect(workflow).toContain('RUN_REAL_LOAD')
    expect(workflow).toContain('./scripts/prod-real-load-steps.sh')
    expect(workflow).toContain('/api/public/works/smoke-fluid-simulation')
    expect(workflow).toContain('/api/public/blogs?page=1&pageSize=12')
    expect(workflow).toContain('reject_seed_path WORK_READ_PATH')
    expect(workflow).toContain('reject_seed_path STUDY_READ_PATH')
    expect(workflow).not.toMatch(/cache/i)

    const allowlist = readWorkflow('scripts/main-runtime-allowlist.txt')
    expect(allowlist).toContain('.github/workflows/prod-runtime-redeploy.yml')
  })
})
