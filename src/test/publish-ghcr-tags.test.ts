import { readFileSync } from 'node:fs'
import path from 'node:path'
import { describe, expect, it } from 'vitest'

const repoRoot = process.cwd()

function readWorkflow(relativePath: string) {
  return readFileSync(path.join(repoRoot, relativePath), 'utf8')
}

describe('GHCR publish workflow image tags', () => {
  it('publishes main runtime images under both current runtime names and legacy compose names', () => {
    const workflow = readWorkflow('.github/workflows/publish-ghcr-main.yml')

    expect(workflow).toContain('${{ steps.vars.outputs.repo_lc }}-runtime-${{ matrix.image_suffix }}:main')
    expect(workflow).toContain('${{ steps.vars.outputs.repo_lc }}-runtime-${{ matrix.image_suffix }}:sha-${{ steps.vars.outputs.sha_short }}')
    expect(workflow).toContain('${{ steps.vars.outputs.repo_lc }}-runtime-${{ matrix.image_suffix }}:latest')
    expect(workflow).toContain('${{ steps.vars.outputs.repo_lc }}-${{ matrix.image_suffix }}:main')
    expect(workflow).toContain('${{ steps.vars.outputs.repo_lc }}-${{ matrix.image_suffix }}:sha-${{ steps.vars.outputs.sha_short }}')
    expect(workflow).toContain('${{ steps.vars.outputs.repo_lc }}-${{ matrix.image_suffix }}:latest')
  })

  it('publishes dev staging images under both current runtime names and legacy compose names', () => {
    const workflow = readWorkflow('.github/workflows/publish-ghcr-dev.yml')

    expect(workflow).toContain('${{ steps.vars.outputs.repo_lc }}-runtime-${{ matrix.image_suffix }}:dev')
    expect(workflow).toContain('${{ steps.vars.outputs.repo_lc }}-runtime-${{ matrix.image_suffix }}:dev-sha-${{ steps.vars.outputs.sha_short }}')
    expect(workflow).toContain('${{ steps.vars.outputs.repo_lc }}-${{ matrix.image_suffix }}:dev')
    expect(workflow).toContain('${{ steps.vars.outputs.repo_lc }}-${{ matrix.image_suffix }}:dev-sha-${{ steps.vars.outputs.sha_short }}')
  })
})
