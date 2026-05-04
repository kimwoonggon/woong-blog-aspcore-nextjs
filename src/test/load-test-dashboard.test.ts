import { describe, expect, it } from 'vitest'
import {
  DEFAULT_LOAD_TEST_CONFIG,
  DEFAULT_REAL_BACKEND_TEST_CONFIG,
  MAX_CONCURRENCY,
  MAX_USERS,
  buildDiagnosticsSnapshotSummary,
  buildLoadTestTargets,
  buildSoakUserTimeline,
  buildSpikeUserTimeline,
  buildUserSteps,
  estimatePatternRequestCount,
  evaluateHttpScenarioHealth,
  evaluateRuntimeDiagnosticsHealth,
  extractRealBackendLatencyBreakdown,
  isRuntimeDiagnosticsPayload,
  runWithConcurrency,
  sanitizeRealBackendTestConfig,
  sanitizeLoadTestConfig,
  summarizeRealBackendRunSnapshot,
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

  it('models step, soak, and spike scenario planning', () => {
    expect(buildUserSteps({
      ...DEFAULT_LOAD_TEST_CONFIG,
      pattern: 'step',
      startUsers: 100,
      maxUsers: 300,
      stepUsers: 100,
    })).toEqual([100, 200, 300])

    expect(buildUserSteps({
      ...DEFAULT_LOAD_TEST_CONFIG,
      pattern: 'soak',
      startUsers: 100,
      maxUsers: 500,
    })).toEqual([500])

    expect(buildUserSteps({
      ...DEFAULT_LOAD_TEST_CONFIG,
      pattern: 'spike',
      startUsers: 100,
      maxUsers: 1000,
    })).toEqual([1000])
  })

  it('clamps unsafe or invalid user input to the supported dashboard range', () => {
    expect(sanitizeLoadTestConfig({
      startUsers: -10,
      maxUsers: 20_000,
      stepUsers: 0,
      requestsPerUser: 0,
      concurrency: 5000,
      timeoutMs: 100,
    })).toEqual({
      startUsers: 1,
      maxUsers: MAX_USERS,
      stepUsers: 1,
      requestsPerUser: 1,
      concurrency: MAX_CONCURRENCY,
      timeoutMs: 1000,
      pattern: 'step',
      soakDurationSeconds: 300,
      spikeRampSeconds: 60,
    })
  })

  it('builds soak and spike timelines from duration seconds', () => {
    expect(buildSoakUserTimeline({
      ...DEFAULT_LOAD_TEST_CONFIG,
      pattern: 'soak',
      maxUsers: 250,
      soakDurationSeconds: 12,
    })).toEqual(Array.from({ length: 12 }, () => 250))

    expect(buildSpikeUserTimeline({
      ...DEFAULT_LOAD_TEST_CONFIG,
      pattern: 'spike',
      startUsers: 100,
      maxUsers: 220,
      spikeRampSeconds: 10,
    })).toEqual([100, 113, 127, 140, 153, 167, 180, 193, 207, 220])
  })

  it('estimates pattern request counts using time-based soak and spike execution', () => {
    expect(estimatePatternRequestCount({
      ...DEFAULT_LOAD_TEST_CONFIG,
      pattern: 'step',
      startUsers: 100,
      maxUsers: 300,
      stepUsers: 100,
      requestsPerUser: 2,
    })).toBe(1200)

    expect(estimatePatternRequestCount({
      ...DEFAULT_LOAD_TEST_CONFIG,
      pattern: 'soak',
      startUsers: 1,
      maxUsers: 5,
      requestsPerUser: 3,
      soakDurationSeconds: 10,
    })).toBe(150)

    expect(estimatePatternRequestCount({
      ...DEFAULT_LOAD_TEST_CONFIG,
      pattern: 'spike',
      startUsers: 2,
      maxUsers: 4,
      requestsPerUser: 1,
      spikeRampSeconds: 10,
    })).toBe(30)
  })

  it('never exceeds configured in-flight concurrency in runWithConcurrency', async () => {
    let inFlight = 0
    let peakInFlight = 0

    const tasks = Array.from({ length: 20 }, (_, index) => async () => {
      inFlight += 1
      peakInFlight = Math.max(peakInFlight, inFlight)
      await new Promise((resolve) => setTimeout(resolve, 10))
      inFlight -= 1
      return index
    })

    const results = await runWithConcurrency(tasks, 4)

    expect(peakInFlight).toBeLessThanOrEqual(4)
    expect(results).toEqual(Array.from({ length: 20 }, (_, index) => index))
  })

  it('handles concurrency larger than task count without dropping tasks', async () => {
    const tasks = Array.from({ length: 3 }, (_, index) => async () => index + 1)
    await expect(runWithConcurrency(tasks, 1000)).resolves.toEqual([1, 2, 3])
  })

  it('allows max users up to 10,000', () => {
    expect(sanitizeLoadTestConfig({
      startUsers: 500,
      maxUsers: 10000,
      stepUsers: 500,
      requestsPerUser: 1,
      concurrency: 10,
      timeoutMs: 10_000,
    })).toEqual({
      startUsers: 500,
      maxUsers: 10000,
      stepUsers: 500,
      requestsPerUser: 1,
      concurrency: 10,
      timeoutMs: 10_000,
      pattern: 'step',
      soakDurationSeconds: 300,
      spikeRampSeconds: 60,
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
      http5xxCount: 1,
      status429Count: 0,
      status503Count: 0,
      timeoutCount: 0,
      abortedCount: 0,
    })
  })

  it('separates 429/503, timeout, and aborted failures', () => {
    const result = summarizeLoadTestSamples(
      { id: 'works-list', label: 'Work list', path: '/api/public/works', group: 'work' },
      10,
      [
        { ok: false, status: 429, durationMs: 40 },
        { ok: false, status: 503, durationMs: 50 },
        { ok: false, status: 500, durationMs: 60 },
        { ok: false, durationMs: 70, error: 'Request timed out' },
        { ok: false, durationMs: 80, error: 'The operation was aborted' },
      ],
    )

    expect(result).toMatchObject({
      failureCount: 5,
      http5xxCount: 2,
      status429Count: 1,
      status503Count: 1,
      timeoutCount: 1,
      abortedCount: 1,
    })
  })

  it('scores HTTP result health with initial green yellow red thresholds', () => {
    const target = { id: 'work-read', label: 'Work read', path: '/works/demo', group: 'work' as const }

    expect(evaluateHttpScenarioHealth({
      ...summarizeLoadTestSamples(target, 100, [{ ok: true, durationMs: 250 }]),
    })).toMatchObject({ status: 'green' })

    expect(evaluateHttpScenarioHealth({
      ...summarizeLoadTestSamples(target, 100, [{ ok: true, durationMs: 500 }]),
    })).toMatchObject({ status: 'yellow' })

    expect(evaluateHttpScenarioHealth({
      ...summarizeLoadTestSamples(target, 100, [{ ok: false, status: 500, durationMs: 900 }]),
    })).toMatchObject({ status: 'red' })
  })

  it('validates runtime diagnostics payloads and summarizes current peak delta values', () => {
    const first = {
      timestamp: '2026-05-04T00:00:00Z',
      process: { memoryBytes: 1000, processorCount: 8 },
      gc: { heapSizeBytes: 500, gen0Collections: 1, gen1Collections: 0, gen2Collections: 0, timeInGcPercent: 1 },
      threadPool: { workerThreads: 2, pendingWorkItemCount: 0, completedWorkItemCount: 20, availableWorkerThreads: 98, maxWorkerThreads: 100 },
      database: {
        status: 'available' as const,
        latencyMs: 10,
        openConnections: 3,
        activeConnections: 1,
        idleConnections: 2,
        idleInTransactionConnections: 0,
        timeoutCount: 0,
      },
    }
    const second = {
      ...first,
      timestamp: '2026-05-04T00:01:00Z',
      process: { memoryBytes: 1800, processorCount: 8 },
      gc: { heapSizeBytes: 900, gen0Collections: 3, gen1Collections: 1, gen2Collections: 2, timeInGcPercent: 12 },
      threadPool: { workerThreads: 8, pendingWorkItemCount: 40, completedWorkItemCount: 120, availableWorkerThreads: 92, maxWorkerThreads: 100 },
      database: {
        status: 'available' as const,
        latencyMs: 260,
        openConnections: 20,
        activeConnections: 16,
        idleConnections: 4,
        idleInTransactionConnections: 2,
        commandLatency: { sampleCount: 3, p50Ms: 8, p95Ms: 45, p99Ms: 90 },
        connectionOpenLatency: { sampleCount: 3, p50Ms: 4, p95Ms: 30, p99Ms: 60 },
        slowQueryCount: 2,
        recentSlowQueries: [{ capturedAt: '2026-05-04T00:01:00Z', durationMs: 355.2, sqlPreview: "select * from works where slug='?'" }],
        timeoutCount: 1,
        errorCount: 1,
      },
    }

    expect(isRuntimeDiagnosticsPayload(first)).toBe(true)
    expect(isRuntimeDiagnosticsPayload({ timestamp: 'bad' })).toBe(false)
    expect(buildDiagnosticsSnapshotSummary([first, second])).toMatchObject({
      sampleCount: 2,
      memoryBytes: { current: 1800, peak: 1800, delta: 800 },
      gcHeapBytes: { current: 900, peak: 900, delta: 400 },
      gen2Collections: { current: 2, peak: 2, delta: 2 },
      threadPoolWorkerThreads: { current: 8, peak: 8, delta: 6 },
      threadPoolQueueLength: { current: 40, peak: 40, delta: 40 },
      threadPoolCompletedWorkItemCount: { current: 120, peak: 120, delta: 100 },
      databaseLatencyMs: { current: 260, peak: 260, delta: 250 },
      databaseTimeoutCount: { current: 1, peak: 1, delta: 1 },
      dbCommandP95Ms: { current: 45, peak: 45, delta: 45 },
      dbConnectionOpenP95Ms: { current: 30, peak: 30, delta: 30 },
      dbSlowQueryCount: { current: 2, peak: 2, delta: 2 },
      dbIdleConnections: { current: 4, peak: 4, delta: 2 },
      dbIdleInTransactionConnections: { current: 2, peak: 2, delta: 2 },
    })
  })

  it('flags runtime and DB pressure from diagnostics snapshots without failing on unavailable metrics', () => {
    expect(evaluateRuntimeDiagnosticsHealth([])).toMatchObject({ status: 'unavailable' })

    const summary = buildDiagnosticsSnapshotSummary([
      {
        timestamp: '2026-05-04T00:00:00Z',
        process: { memoryBytes: 1000, processorCount: 8 },
        gc: { heapSizeBytes: 500, gen0Collections: 1, gen1Collections: 0, gen2Collections: 0, timeInGcPercent: 1 },
        threadPool: { workerThreads: 2, pendingWorkItemCount: 0, completedWorkItemCount: 20, availableWorkerThreads: 98, maxWorkerThreads: 100 },
        database: {
          status: 'unavailable',
          latencyMs: null,
          openConnections: null,
          activeConnections: null,
          idleConnections: null,
          idleInTransactionConnections: null,
          timeoutCount: 0,
        },
      },
      {
        timestamp: '2026-05-04T00:01:00Z',
        process: { memoryBytes: 1800, processorCount: 8 },
        gc: { heapSizeBytes: 900, gen0Collections: 3, gen1Collections: 1, gen2Collections: 2, timeInGcPercent: 12 },
        threadPool: { workerThreads: 8, pendingWorkItemCount: 40, completedWorkItemCount: 120, availableWorkerThreads: 92, maxWorkerThreads: 100 },
        database: {
          status: 'available',
          latencyMs: 260,
          openConnections: 20,
          activeConnections: 16,
          idleConnections: 4,
          idleInTransactionConnections: 1,
          commandLatency: { sampleCount: 3, p50Ms: 8, p95Ms: 45, p99Ms: 90 },
          connectionOpenLatency: { sampleCount: 3, p50Ms: 4, p95Ms: 30, p99Ms: 60 },
          slowQueryCount: 2,
          timeoutCount: 1,
          errorCount: 1,
        },
      },
    ])

    expect(evaluateRuntimeDiagnosticsHealth(summary)).toMatchObject({
      status: 'red',
    })
  })

  it('sanitizes real backend test config values to safe defaults and bounds', () => {
    expect(sanitizeRealBackendTestConfig({
      scenario: '   ',
      target: '',
      runner: '',
      rate: -10,
      durationSeconds: 999999,
      maxVUs: 0,
    })).toEqual({
      ...DEFAULT_REAL_BACKEND_TEST_CONFIG,
      rate: 1,
      durationSeconds: 3600,
      maxVUs: 1,
    })
  })

  it('summarizes real backend run status and metrics with latency breakdown + http counts', () => {
    const snapshot = summarizeRealBackendRunSnapshot(
      'run-42',
      {
        runId: 'run-42',
        status: 'running',
        totalRequests: 50,
        currentRps: 119.8,
        p95Ms: 88.8,
        statusCounts: {
          '2xx': 49,
          '3xx': 0,
          '4xx': 0,
          '5xx': 1,
        },
      },
      {
        runId: 'run-42',
        metrics: [
          {
            elapsedSeconds: 1.2,
            totalRequests: 50,
            currentRps: 120.45,
            p95Ms: 140,
            p99Ms: 210,
            maxMs: 340,
            statusCounts: {
              '2xx': 49,
              '3xx': 0,
              '4xx': 0,
              '5xx': 1,
            },
          },
        ],
        latencyBreakdown: {
          minMs: 10,
          p50Ms: 80,
          p95Ms: 140,
          p99Ms: 210,
          maxMs: 340,
        },
      },
    )

    expect(snapshot).toMatchObject({
      runId: 'run-42',
      status: 'running',
      requests: 50,
      throughputRps: 120.5,
      latencyMs: 140,
      latencyBreakdown: {
        available: true,
        minMs: 10,
        p50Ms: 80,
        p95Ms: 140,
        p99Ms: 210,
        maxMs: 340,
      },
      httpCounts: {
        total: 50,
        success: 49,
        failed: 1,
        status2xx: 49,
        status5xx: 1,
      },
    })
  })

  it('returns an unavailable latency breakdown fallback when metrics are missing', () => {
    expect(extractRealBackendLatencyBreakdown({ status: 'running' })).toEqual({
      available: false,
      reason: 'Latency breakdown is unavailable for this run.',
      minMs: null,
      p50Ms: null,
      p95Ms: null,
      p99Ms: null,
      maxMs: null,
    })
  })
})
