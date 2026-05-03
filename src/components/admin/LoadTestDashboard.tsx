"use client"

import { useMemo, useRef, useState } from 'react'
import { Activity, AlertTriangle, Gauge, Play, Square } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import {
  DEFAULT_LOAD_TEST_CONFIG,
  appendLoadTestCacheBust,
  buildUserSteps,
  sanitizeLoadTestConfig,
  summarizeLoadTestSamples,
  type LoadTestConfig,
  type LoadTestSample,
  type LoadTestScenarioResult,
  type LoadTestTarget,
} from '@/lib/load-test-dashboard'

type LoadTestDashboardProps = {
  targets: LoadTestTarget[]
  targetLoadWarning?: string | null
}

type LoadTestStatus = {
  phase: 'idle' | 'running' | 'completed' | 'stopped'
  currentLabel: string
  completedRequests: number
  totalRequests: number
}

const numberFormatter = new Intl.NumberFormat('en-US')

function scenarioResultKey(result: Pick<LoadTestScenarioResult, 'targetId' | 'userCount'>) {
  return `${result.targetId}:${result.userCount}`
}

function upsertScenarioResult(
  current: LoadTestScenarioResult[],
  nextResult: LoadTestScenarioResult,
) {
  const nextKey = scenarioResultKey(nextResult)
  const existingIndex = current.findIndex((result) => scenarioResultKey(result) === nextKey)

  if (existingIndex === -1) {
    return [...current, nextResult]
  }

  return current.map((result, index) => index === existingIndex ? nextResult : result)
}

function formatMs(value: number) {
  return `${numberFormatter.format(value)} ms`
}

function formatPercent(value: number) {
  return `${numberFormatter.format(value)}%`
}

function formatScenarioState(state: LoadTestScenarioResult['state']) {
  if (state === 'running') {
    return 'Running'
  }

  if (state === 'stopped') {
    return 'Stopped'
  }

  return 'Completed'
}

function inputNumberValue(value: string) {
  return Number.isFinite(Number(value)) ? Number(value) : 0
}

async function measureLoadTestRequest(
  target: LoadTestTarget,
  runId: string,
  requestIndex: number,
  userCount: number,
  timeoutMs: number,
  registerController: (controller: AbortController) => void,
  unregisterController: (controller: AbortController) => void,
): Promise<LoadTestSample> {
  const controller = new AbortController()
  const started = performance.now()
  const timeout = window.setTimeout(() => controller.abort(), timeoutMs)
  registerController(controller)

  try {
    const response = await fetch(appendLoadTestCacheBust(target.path, runId, requestIndex, userCount), {
      cache: 'no-store',
      credentials: 'omit',
      signal: controller.signal,
    })
    await response.text()

    return {
      ok: response.ok,
      status: response.status,
      durationMs: performance.now() - started,
    }
  } catch (error) {
    return {
      ok: false,
      durationMs: performance.now() - started,
      error: error instanceof Error ? error.message : 'Request failed',
    }
  } finally {
    window.clearTimeout(timeout)
    unregisterController(controller)
  }
}

export function LoadTestDashboard({ targets, targetLoadWarning }: LoadTestDashboardProps) {
  const [config, setConfig] = useState<LoadTestConfig>(DEFAULT_LOAD_TEST_CONFIG)
  const [editableTargets, setEditableTargets] = useState<LoadTestTarget[]>(targets)
  const [results, setResults] = useState<LoadTestScenarioResult[]>([])
  const [status, setStatus] = useState<LoadTestStatus>({
    phase: 'idle',
    currentLabel: '',
    completedRequests: 0,
    totalRequests: 0,
  })
  const cancelledRef = useRef(false)
  const controllersRef = useRef(new Set<AbortController>())

  const safeConfig = useMemo(() => sanitizeLoadTestConfig(config), [config])
  const userSteps = useMemo(() => buildUserSteps(safeConfig), [safeConfig])
  const runnableTargets = useMemo(
    () => editableTargets
      .map((target) => ({ ...target, path: target.path.trim() }))
      .filter((target) => target.path.length > 0),
    [editableTargets],
  )
  const totalPlannedRequests = useMemo(
    () => runnableTargets.length * userSteps.reduce((sum, users) => sum + (users * safeConfig.requestsPerUser), 0),
    [safeConfig.requestsPerUser, runnableTargets.length, userSteps],
  )
  const latestResult = results.at(-1)
  const avgP95 = results.length
    ? results.reduce((sum, result) => sum + result.p95Ms, 0) / results.length
    : 0
  const totalFailures = results.reduce((sum, result) => sum + result.failureCount, 0)

  function updateNumberField(field: keyof LoadTestConfig, value: string) {
    setConfig((current) => sanitizeLoadTestConfig({
      ...current,
      [field]: inputNumberValue(value),
    }))
  }

  function updateTargetPath(targetId: string, value: string) {
    setEditableTargets((current) => current.map((target) => target.id === targetId ? { ...target, path: value } : target))
  }

  function registerController(controller: AbortController) {
    controllersRef.current.add(controller)
  }

  function unregisterController(controller: AbortController) {
    controllersRef.current.delete(controller)
  }

  async function runScenario(target: LoadTestTarget, userCount: number, runId: string) {
    const requestCount = userCount * safeConfig.requestsPerUser
    const samples: LoadTestSample[] = []
    let nextRequestIndex = 0
    const workerCount = Math.min(safeConfig.concurrency, requestCount)

    function publishScenarioResult(state: NonNullable<LoadTestScenarioResult['state']>) {
      const result = {
        ...summarizeLoadTestSamples(target, userCount, samples),
        plannedRequestCount: requestCount,
        state,
      }
      setResults((current) => upsertScenarioResult(current, result))
      return result
    }

    publishScenarioResult('running')

    async function worker() {
      while (!cancelledRef.current && nextRequestIndex < requestCount) {
        const requestIndex = nextRequestIndex
        nextRequestIndex += 1
        const sample = await measureLoadTestRequest(
          target,
          runId,
          requestIndex,
          userCount,
          safeConfig.timeoutMs,
          registerController,
          unregisterController,
        )
        samples.push(sample)
        publishScenarioResult(cancelledRef.current ? 'stopped' : 'running')
        setStatus((current) => ({
          ...current,
          completedRequests: current.completedRequests + 1,
        }))
      }
    }

    await Promise.all(Array.from({ length: workerCount }, () => worker()))
    return publishScenarioResult(cancelledRef.current ? 'stopped' : 'completed')
  }

  async function runLoadTest() {
    if (!runnableTargets.length || status.phase === 'running') {
      return
    }

    cancelledRef.current = false
    const runId = `${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 8)}`
    setResults([])
    setStatus({
      phase: 'running',
      currentLabel: 'Starting',
      completedRequests: 0,
      totalRequests: totalPlannedRequests,
    })

    for (const userCount of userSteps) {
      for (const target of runnableTargets) {
        if (cancelledRef.current) {
          setStatus((current) => ({ ...current, phase: 'stopped', currentLabel: 'Stopped' }))
          return
        }

        setStatus((current) => ({
          ...current,
          currentLabel: `${target.label} · ${numberFormatter.format(userCount)} users`,
        }))

        const result = await runScenario(target, userCount, runId)
        setResults((current) => upsertScenarioResult(current, result))
      }
    }

    setStatus((current) => ({ ...current, phase: 'completed', currentLabel: 'Completed' }))
  }

  function stopLoadTest() {
    cancelledRef.current = true
    controllersRef.current.forEach((controller) => controller.abort())
  }

  return (
    <div className="flex flex-col gap-8">
      <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <h1 className="text-3xl font-bold text-foreground">Load Test Dashboard</h1>
          <p className="mt-2 max-w-3xl text-sm text-muted-foreground">
            Measure synthetic read latency for Work and Study pages as virtual users rise in fixed steps.
          </p>
        </div>
        <div className="flex flex-wrap gap-2">
          <Button
            type="button"
            className="gap-2"
            disabled={status.phase === 'running' || !runnableTargets.length}
            onClick={() => void runLoadTest()}
          >
            <Play aria-hidden="true" size={16} />
            Run load test
          </Button>
          <Button
            type="button"
            variant="outline"
            className="gap-2"
            disabled={status.phase !== 'running'}
            onClick={stopLoadTest}
          >
            <Square aria-hidden="true" size={16} />
            Stop
          </Button>
        </div>
      </div>

      {targetLoadWarning ? (
        <div className="flex items-start gap-3 rounded-lg border border-amber-200 bg-amber-50 p-4 text-sm text-amber-900 dark:border-amber-900/60 dark:bg-amber-950/40 dark:text-amber-100">
          <AlertTriangle aria-hidden="true" className="mt-0.5 h-4 w-4 shrink-0" />
          <p>{targetLoadWarning}</p>
        </div>
      ) : null}

      <div className="grid gap-4 lg:grid-cols-[minmax(0,1fr)_minmax(280px,360px)]">
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-base">
              <Gauge aria-hidden="true" size={18} />
              Scenario
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
              <div className="space-y-2">
                <Label htmlFor="load-test-start-users">Start users</Label>
                <Input
                  id="load-test-start-users"
                  type="number"
                  min={1}
                  max={1000}
                  value={safeConfig.startUsers}
                  onChange={(event) => updateNumberField('startUsers', event.target.value)}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="load-test-max-users">Max users</Label>
                <Input
                  id="load-test-max-users"
                  type="number"
                  min={1}
                  max={1000}
                  value={safeConfig.maxUsers}
                  onChange={(event) => updateNumberField('maxUsers', event.target.value)}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="load-test-step-users">Step users</Label>
                <Input
                  id="load-test-step-users"
                  type="number"
                  min={1}
                  max={1000}
                  value={safeConfig.stepUsers}
                  onChange={(event) => updateNumberField('stepUsers', event.target.value)}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="load-test-requests-per-user">Requests per user</Label>
                <Input
                  id="load-test-requests-per-user"
                  type="number"
                  min={1}
                  max={5}
                  value={safeConfig.requestsPerUser}
                  onChange={(event) => updateNumberField('requestsPerUser', event.target.value)}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="load-test-concurrency">Concurrency</Label>
                <Input
                  id="load-test-concurrency"
                  type="number"
                  min={1}
                  max={100}
                  value={safeConfig.concurrency}
                  onChange={(event) => updateNumberField('concurrency', event.target.value)}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="load-test-timeout-ms">Timeout ms</Label>
                <Input
                  id="load-test-timeout-ms"
                  type="number"
                  min={1000}
                  max={60000}
                  value={safeConfig.timeoutMs}
                  onChange={(event) => updateNumberField('timeoutMs', event.target.value)}
                />
              </div>
            </div>

            <div className="mt-6 grid gap-3 md:grid-cols-2">
              {editableTargets.map((target) => (
                <div key={target.id} className="rounded-lg border border-border bg-muted/30 p-3">
                  <div className="flex items-center justify-between gap-3">
                    <p className="font-medium text-foreground">{target.label}</p>
                    <span className="rounded-md bg-background px-2 py-1 text-xs font-medium uppercase text-muted-foreground">
                      {target.group}
                    </span>
                  </div>
                  <div className="mt-3 space-y-2">
                    <Label htmlFor={`load-test-target-${target.id}`} className="text-xs text-muted-foreground">
                      {target.label} URL
                    </Label>
                    <Input
                      id={`load-test-target-${target.id}`}
                      value={target.path}
                      onChange={(event) => updateTargetPath(target.id, event.target.value)}
                    />
                  </div>
                </div>
              ))}
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-base">
              <Activity aria-hidden="true" size={18} />
              Run Status
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div>
              <p className="text-sm text-muted-foreground">Current</p>
              <p className="mt-1 min-h-6 font-medium text-foreground" data-testid="load-test-live-status">
                {status.phase} · {status.currentLabel || 'Idle'}
              </p>
            </div>
            <div>
              <p className="text-sm text-muted-foreground">Requests</p>
              <p className="mt-1 font-medium text-foreground">
                {numberFormatter.format(status.completedRequests)} / {numberFormatter.format(status.totalRequests || totalPlannedRequests)}
              </p>
            </div>
            <div>
              <p className="text-sm text-muted-foreground">Planned scenarios</p>
              <p className="mt-1 font-medium text-foreground">
                {numberFormatter.format(userSteps.length * runnableTargets.length)} scenarios · {numberFormatter.format(totalPlannedRequests)} requests
              </p>
            </div>
            <div className="h-2 overflow-hidden rounded-full bg-muted">
              <div
                className="h-full bg-primary transition-all"
                style={{
                  width: `${Math.min(100, ((status.completedRequests / (status.totalRequests || totalPlannedRequests || 1)) * 100))}%`,
                }}
              />
            </div>
          </CardContent>
        </Card>
      </div>

      <div className="grid gap-4 md:grid-cols-3">
        <Card>
          <CardHeader>
            <CardTitle className="text-sm font-medium">Latest P95</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-2xl font-bold text-foreground">{latestResult ? formatMs(latestResult.p95Ms) : '—'}</p>
            <p className="mt-1 text-xs text-muted-foreground">{latestResult?.targetLabel ?? 'No run yet'}</p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader>
            <CardTitle className="text-sm font-medium">Average P95</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-2xl font-bold text-foreground">{results.length ? formatMs(Math.round(avgP95)) : '—'}</p>
            <p className="mt-1 text-xs text-muted-foreground" data-testid="load-test-result-count">
              {numberFormatter.format(results.length)} scenarios
            </p>
          </CardContent>
        </Card>
        <Card>
          <CardHeader>
            <CardTitle className="text-sm font-medium">Failures</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-2xl font-bold text-foreground">{numberFormatter.format(totalFailures)}</p>
            <p className="mt-1 text-xs text-muted-foreground">HTTP failures and aborted requests</p>
          </CardContent>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Results</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="overflow-x-auto">
            <table data-testid="load-test-summary-table" className="w-full min-w-[840px] text-sm">
              <thead className="border-b border-border text-left text-xs uppercase text-muted-foreground">
                <tr>
                  <th className="py-3 pr-4 font-medium">Target</th>
                  <th className="py-3 pr-4 font-medium">State</th>
                  <th className="py-3 pr-4 font-medium">Users</th>
                  <th className="py-3 pr-4 font-medium">Requests</th>
                  <th className="py-3 pr-4 font-medium">Error rate</th>
                  <th className="py-3 pr-4 font-medium">P50</th>
                  <th className="py-3 pr-4 font-medium">P95</th>
                  <th className="py-3 pr-4 font-medium">Avg</th>
                  <th className="py-3 pr-4 font-medium">Max</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                {results.length ? results.map((result) => (
                  <tr key={`${result.targetId}-${result.userCount}`}>
                    <td className="py-3 pr-4">
                      <p className="font-medium text-foreground">{result.targetLabel}</p>
                      <p className="break-all text-xs text-muted-foreground">{result.targetPath}</p>
                    </td>
                    <td className="py-3 pr-4">{formatScenarioState(result.state)}</td>
                    <td className="py-3 pr-4">{numberFormatter.format(result.userCount)}</td>
                    <td className="py-3 pr-4">
                      {numberFormatter.format(result.requestCount)}
                      {result.plannedRequestCount ? ` / ${numberFormatter.format(result.plannedRequestCount)}` : ''}
                    </td>
                    <td className="py-3 pr-4">{formatPercent(result.errorRate)}</td>
                    <td className="py-3 pr-4">{formatMs(result.p50Ms)}</td>
                    <td className="py-3 pr-4">{formatMs(result.p95Ms)}</td>
                    <td className="py-3 pr-4">{formatMs(result.avgMs)}</td>
                    <td className="py-3 pr-4">{formatMs(result.maxMs)}</td>
                  </tr>
                )) : (
                  <tr>
                    <td className="py-6 text-muted-foreground" colSpan={9}>
                      No load-test results yet.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </CardContent>
      </Card>
    </div>
  )
}
