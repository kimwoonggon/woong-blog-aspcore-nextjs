export type LoadTestGroup = 'work' | 'study'

export type LoadTestTarget = {
  id: string
  label: string
  path: string
  group: LoadTestGroup
}

export type LoadTestConfig = {
  startUsers: number
  maxUsers: number
  stepUsers: number
  requestsPerUser: number
  concurrency: number
  timeoutMs: number
}

export type LoadTestSample = {
  ok: boolean
  durationMs: number
  status?: number
  error?: string
}

export type LoadTestScenarioResult = {
  targetId: string
  targetLabel: string
  targetPath: string
  group: LoadTestGroup
  state?: 'running' | 'completed' | 'stopped'
  userCount: number
  plannedRequestCount?: number
  requestCount: number
  successCount: number
  failureCount: number
  errorRate: number
  minMs: number
  avgMs: number
  p50Ms: number
  p95Ms: number
  maxMs: number
}

export type LoadTestTargetInput = {
  workSlugs?: string[]
  blogSlugs?: string[]
}

export const DEFAULT_LOAD_TEST_CONFIG: LoadTestConfig = {
  startUsers: 100,
  maxUsers: 1000,
  stepUsers: 100,
  requestsPerUser: 1,
  concurrency: 25,
  timeoutMs: 10_000,
}

const MIN_USERS = 1
export const MAX_USERS = 10_000
const MIN_TIMEOUT_MS = 1000
const MAX_TIMEOUT_MS = 60_000
const MAX_CONCURRENCY = 100
const MAX_REQUESTS_PER_USER = 5

function toInteger(value: unknown, fallback: number) {
  const numberValue = typeof value === 'number' ? value : Number(value)
  return Number.isFinite(numberValue) ? Math.trunc(numberValue) : fallback
}

function clamp(value: number, min: number, max: number) {
  return Math.min(Math.max(value, min), max)
}

function roundMetric(value: number) {
  return Math.round(value * 10) / 10
}

export function sanitizeLoadTestConfig(config: Partial<LoadTestConfig>): LoadTestConfig {
  const startUsers = clamp(toInteger(config.startUsers, DEFAULT_LOAD_TEST_CONFIG.startUsers), MIN_USERS, MAX_USERS)
  const maxUsers = clamp(toInteger(config.maxUsers, DEFAULT_LOAD_TEST_CONFIG.maxUsers), startUsers, MAX_USERS)

  return {
    startUsers,
    maxUsers,
    stepUsers: clamp(toInteger(config.stepUsers, DEFAULT_LOAD_TEST_CONFIG.stepUsers), 1, MAX_USERS),
    requestsPerUser: clamp(toInteger(config.requestsPerUser, DEFAULT_LOAD_TEST_CONFIG.requestsPerUser), 1, MAX_REQUESTS_PER_USER),
    concurrency: clamp(toInteger(config.concurrency, DEFAULT_LOAD_TEST_CONFIG.concurrency), 1, MAX_CONCURRENCY),
    timeoutMs: clamp(toInteger(config.timeoutMs, DEFAULT_LOAD_TEST_CONFIG.timeoutMs), MIN_TIMEOUT_MS, MAX_TIMEOUT_MS),
  }
}

export function buildUserSteps(config: LoadTestConfig) {
  const safeConfig = sanitizeLoadTestConfig(config)
  const steps: number[] = []

  for (let users = safeConfig.startUsers; users <= safeConfig.maxUsers; users += safeConfig.stepUsers) {
    steps.push(users)
  }

  if (steps.at(-1) !== safeConfig.maxUsers) {
    steps.push(safeConfig.maxUsers)
  }

  return steps
}

export function buildLoadTestTargets({ workSlugs = [], blogSlugs = [] }: LoadTestTargetInput): LoadTestTarget[] {
  const targets: LoadTestTarget[] = [
    { id: 'works-list', label: 'Work list', path: '/api/public/works?page=1&pageSize=12', group: 'work' },
  ]

  const firstWorkSlug = workSlugs.find(Boolean)
  if (firstWorkSlug) {
    targets.push({ id: 'work-read', label: 'Work read', path: `/api/public/works/${encodeURIComponent(firstWorkSlug)}`, group: 'work' })
  }

  targets.push({ id: 'study-list', label: 'Study list', path: '/api/public/blogs?page=1&pageSize=12', group: 'study' })

  const firstBlogSlug = blogSlugs.find(Boolean)
  if (firstBlogSlug) {
    targets.push({ id: 'study-read', label: 'Study read', path: `/api/public/blogs/${encodeURIComponent(firstBlogSlug)}`, group: 'study' })
  }

  return targets
}

export function percentile(values: number[], percentileRank: number) {
  if (!values.length) {
    return 0
  }

  const sorted = [...values].sort((a, b) => a - b)
  const index = Math.ceil((percentileRank / 100) * sorted.length) - 1
  return sorted[clamp(index, 0, sorted.length - 1)]
}

export function summarizeLoadTestSamples(
  target: LoadTestTarget,
  userCount: number,
  samples: LoadTestSample[],
): LoadTestScenarioResult {
  const durations = samples.map((sample) => sample.durationMs)
  const successCount = samples.filter((sample) => sample.ok).length
  const requestCount = samples.length
  const failureCount = requestCount - successCount
  const totalDuration = durations.reduce((sum, duration) => sum + duration, 0)

  return {
    targetId: target.id,
    targetLabel: target.label,
    targetPath: target.path,
    group: target.group,
    userCount,
    requestCount,
    successCount,
    failureCount,
    errorRate: requestCount ? roundMetric((failureCount / requestCount) * 100) : 0,
    minMs: durations.length ? roundMetric(Math.min(...durations)) : 0,
    avgMs: durations.length ? roundMetric(totalDuration / durations.length) : 0,
    p50Ms: roundMetric(percentile(durations, 50)),
    p95Ms: roundMetric(percentile(durations, 95)),
    maxMs: durations.length ? roundMetric(Math.max(...durations)) : 0,
  }
}

export function appendLoadTestCacheBust(path: string, runId: string, requestIndex: number, userCount = 1) {
  const separator = path.includes('?') ? '&' : '?'
  const safeUserCount = Math.max(1, Math.trunc(userCount))
  const virtualUser = (requestIndex % safeUserCount) + 1
  const iteration = Math.floor(requestIndex / safeUserCount) + 1

  return `${path}${separator}__loadTestRun=${encodeURIComponent(runId)}&__loadTestUser=${virtualUser}&__loadTestRequest=${requestIndex}&__loadTestIteration=${iteration}`
}
