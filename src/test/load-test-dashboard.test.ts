import { describe, expect, it } from 'vitest'
import {
  DEFAULT_LOAD_TEST_CONFIG,
  buildLoadTestTargets,
  buildUserSteps,
  sanitizeLoadTestConfig,
  summarizeLoadTestSamples,
} from '@/lib/load-test-dashboard'

describe('load test dashboard planning', () => {
  it('uses 100-user intervals up to 1000 users by default', () => {
    expect(buildUserSteps(DEFAULT_LOAD_TEST_CONFIG)).toEqual([
      100,
      200,
      300,
      400,
      500,
      600,
      700,
      800,
      900,
      1000,
    ])
  })

  it('clamps unsafe or invalid user input to the supported dashboard range', () => {
    expect(sanitizeLoadTestConfig({
      startUsers: -10,
      maxUsers: 2500,
      stepUsers: 0,
      requestsPerUser: 0,
      concurrency: 999,
      timeoutMs: 100,
    })).toEqual({
      startUsers: 1,
      maxUsers: 1000,
      stepUsers: 1,
      requestsPerUser: 1,
      concurrency: 100,
      timeoutMs: 1000,
    })
  })

  it('builds Work and Study list/read targets from public slugs', () => {
    expect(buildLoadTestTargets({
      workSlugs: ['portfolio-api'],
      blogSlugs: ['nextjs-study'],
    })).toEqual([
      { id: 'works-list', label: 'Work list', path: '/api/public/works?page=1&pageSize=12', group: 'work' },
      { id: 'work-read', label: 'Work read', path: '/api/public/works/portfolio-api', group: 'work' },
      { id: 'study-list', label: 'Study list', path: '/api/public/blogs?page=1&pageSize=12', group: 'study' },
      { id: 'study-read', label: 'Study read', path: '/api/public/blogs/nextjs-study', group: 'study' },
    ])
  })

  it('adds distinct virtual-user identity to every load-test request URL', async () => {
    const { appendLoadTestCacheBust } = await import('@/lib/load-test-dashboard')

    expect(appendLoadTestCacheBust('/api/public/works/demo', 'run-1', 0, 3)).toBe(
      '/api/public/works/demo?__loadTestRun=run-1&__loadTestUser=1&__loadTestRequest=0&__loadTestIteration=1',
    )
    expect(appendLoadTestCacheBust('/api/public/works/demo', 'run-1', 3, 3)).toBe(
      '/api/public/works/demo?__loadTestRun=run-1&__loadTestUser=1&__loadTestRequest=3&__loadTestIteration=2',
    )
    expect(appendLoadTestCacheBust('/api/public/works?page=1', 'run-1', 1, 3)).toContain(
      '/api/public/works?page=1&__loadTestRun=run-1&__loadTestUser=2',
    )
  })

  it('summarizes request samples with percentiles and error rate', () => {
    const result = summarizeLoadTestSamples(
      { id: 'work-read', label: 'Work read', path: '/works/demo', group: 'work' },
      200,
      [
        { ok: true, status: 200, durationMs: 100 },
        { ok: true, status: 200, durationMs: 120 },
        { ok: true, status: 200, durationMs: 500 },
        { ok: false, status: 500, durationMs: 900 },
      ],
    )

    expect(result).toMatchObject({
      targetId: 'work-read',
      targetLabel: 'Work read',
      targetPath: '/works/demo',
      userCount: 200,
      requestCount: 4,
      successCount: 3,
      failureCount: 1,
      errorRate: 25,
      minMs: 100,
      avgMs: 405,
      p50Ms: 120,
      p95Ms: 900,
      maxMs: 900,
    })
  })
})
