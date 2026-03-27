'use client'

import { X } from 'lucide-react'
import { useEffect, useState } from 'react'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Label } from '@/components/ui/label'
import { fetchWithCsrf } from '@/lib/api/auth'
import { getBrowserApiBaseUrl } from '@/lib/api/browser'
import {
  fetchAdminAiRuntimeConfigBrowser,
  getBlogAiBatchJobBrowser,
  listBlogAiBatchJobsBrowser,
  type AdminAiRuntimeConfig,
  type BlogAiBatchJobDetail,
  type BlogAiBatchJobListPayload,
  type BlogAiBatchJobSummary,
} from '@/lib/api/admin-ai'
import { toast } from 'sonner'

interface BlogBatchCandidate {
  id: string
  title: string
  publishedAt?: string | null
  updatedAt?: string | null
}

interface AdminBlogBatchAiPanelProps {
  isOpen: boolean
  selectedBlogIds: string[]
  selectedBlogTitles: string[]
  availableBlogs: BlogBatchCandidate[]
  onApplied?: () => void
}

export function AdminBlogBatchAiPanel({
  isOpen,
  selectedBlogIds,
  selectedBlogTitles,
  availableBlogs,
  onApplied,
}: AdminBlogBatchAiPanelProps) {
  const [runtimeConfig, setRuntimeConfig] = useState<AdminAiRuntimeConfig | null>(null)
  const [recentJobs, setRecentJobs] = useState<BlogAiBatchJobSummary[]>([])
  const [jobCounts, setJobCounts] = useState<Pick<BlogAiBatchJobListPayload, 'runningCount' | 'queuedCount' | 'completedCount' | 'failedCount' | 'cancelledCount'>>({
    runningCount: 0,
    queuedCount: 0,
    completedCount: 0,
    failedCount: 0,
    cancelledCount: 0,
  })
  const [activeJobId, setActiveJobId] = useState<string | null>(null)
  const [activeJob, setActiveJob] = useState<BlogAiBatchJobDetail | null>(null)
  const [selectedJobItemId, setSelectedJobItemId] = useState<string | null>(null)
  const [mode, setMode] = useState<'selected' | 'range' | 'date'>('selected')
  const [rangeStart, setRangeStart] = useState('1')
  const [rangeCount, setRangeCount] = useState('10')
  const [dateStart, setDateStart] = useState('')
  const [dateEnd, setDateEnd] = useState('')
  const [codexModel, setCodexModel] = useState('gpt-5.4')
  const [codexReasoningEffort, setCodexReasoningEffort] = useState('medium')
  const [workerCount, setWorkerCount] = useState('2')
  const [autoApply, setAutoApply] = useState(false)
  const [isCreatingJob, setIsCreatingJob] = useState(false)
  const [isApplyingJob, setIsApplyingJob] = useState(false)
  const [isCancellingJob, setIsCancellingJob] = useState(false)
  const [isCleaningJobs, setIsCleaningJobs] = useState(false)
  const [removingJobId, setRemovingJobId] = useState<string | null>(null)

  const previewJobItem = activeJob?.items.find((item) => item.jobItemId === selectedJobItemId)
    ?? activeJob?.items.find((item) => item.fixedHtml)
  const rangeStartIndex = Math.max(1, Number(rangeStart || '1'))
  const rangeCountValue = Math.max(1, Number(rangeCount || '1'))
  const rangeBlogs = availableBlogs.slice(rangeStartIndex - 1, rangeStartIndex - 1 + rangeCountValue)
  const dateBlogs = availableBlogs.filter((blog) => {
    const effectiveDate = resolveBlogDate(blog)
    if (!effectiveDate) {
      return false
    }

    if (dateStart && effectiveDate < dateStart) {
      return false
    }

    if (dateEnd && effectiveDate > dateEnd) {
      return false
    }

    return true
  })
  const selectedIdsForJob = mode === 'range'
    ? rangeBlogs.map((blog) => blog.id)
    : mode === 'date'
      ? dateBlogs.map((blog) => blog.id)
      : selectedBlogIds
  const selectedTitlesForJob = mode === 'range'
    ? rangeBlogs.map((blog) => blog.title)
    : mode === 'date'
      ? dateBlogs.map((blog) => blog.title)
      : selectedBlogTitles
  const currentRunningJob = recentJobs.find((job) => job.status === 'running') ?? null
  const queuedJobs = recentJobs.filter((job) => job.status === 'queued')
  const selectionSummary = summarizeSelectionTitles(selectedTitlesForJob)
  const duplicateCounts = recentJobs.reduce<Record<string, number>>((acc, job) => {
    if (job.selectionKey) {
      acc[job.selectionKey] = (acc[job.selectionKey] ?? 0) + 1
    }
    return acc
  }, {})

  useEffect(() => {
    if (!isOpen) {
      return
    }

    let cancelled = false
    const savedModel = typeof window !== 'undefined' ? window.localStorage.getItem('admin-ai-codex-model') : null
    const savedReasoning = typeof window !== 'undefined' ? window.localStorage.getItem('admin-ai-codex-reasoning') : null

    void fetchAdminAiRuntimeConfigBrowser()
      .then((config) => {
        if (cancelled) {
          return
        }

        setRuntimeConfig(config)
        setCodexModel(savedModel || config.codexModel || 'gpt-5.4')
        setCodexReasoningEffort(savedReasoning || config.codexReasoningEffort || 'medium')
        setWorkerCount(String(config.batchConcurrency || 2))
      })
      .catch((error) => {
        if (!cancelled) {
          toast.error(error instanceof Error ? error.message : 'Failed to load AI runtime config')
        }
      })

    void loadRecentJobs()

    return () => {
      cancelled = true
    }
  }, [isOpen])

  useEffect(() => {
    if (!activeJobId) {
      setActiveJob(null)
      return
    }

    void loadJobDetail(activeJobId)
  }, [activeJobId])

  useEffect(() => {
    if (!isOpen || !activeJobId || !activeJob || !['queued', 'running'].includes(activeJob.status)) {
      return
    }

    const timer = window.setInterval(() => {
      void loadRecentJobs(activeJobId)
      void loadJobDetail(activeJobId)
    }, 2000)

    return () => {
      window.clearInterval(timer)
    }
  }, [activeJob, activeJobId, isOpen])

  async function loadRecentJobs(nextActiveJobId?: string | null) {
    try {
      const payload = await listBlogAiBatchJobsBrowser()
      const prioritized = [...payload.jobs].sort((left, right) => rankStatus(left.status) - rankStatus(right.status) || right.createdAt.localeCompare(left.createdAt))
      setRecentJobs(prioritized)
      setJobCounts({
        runningCount: payload.runningCount,
        queuedCount: payload.queuedCount,
        completedCount: payload.completedCount,
        failedCount: payload.failedCount,
        cancelledCount: payload.cancelledCount,
      })
      if (nextActiveJobId) {
        setActiveJobId(nextActiveJobId)
      } else if (!activeJobId && prioritized[0]) {
        setActiveJobId(prioritized[0].jobId)
      } else if (activeJobId) {
        const stillExists = prioritized.some((job) => job.jobId === activeJobId)
        if (!stillExists && prioritized[0]) {
          setActiveJobId(prioritized[0].jobId)
        }
      }
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'Failed to load AI batch jobs')
    }
  }

  async function loadJobDetail(jobId: string) {
    try {
      const job = await getBlogAiBatchJobBrowser(jobId)
      setActiveJob(job)
      if (job.items.length > 0 && !selectedJobItemId) {
        const firstPreviewable = job.items.find((item) => item.fixedHtml)
        if (firstPreviewable) {
          setSelectedJobItemId(firstPreviewable.jobItemId)
        }
      }
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'Failed to load AI batch job')
    }
  }

  async function createBatchAiJob() {
    if (mode === 'date' && !dateStart && !dateEnd) {
      toast.error('Set a start date or end date before creating a date-range batch job')
      return
    }

    if (mode === 'date' && dateStart && dateEnd && dateStart > dateEnd) {
      toast.error('Start date must be before or equal to end date')
      return
    }

    if (selectedIdsForJob.length === 0) {
      toast.error('No blog posts match the current batch selection')
      return
    }

    setIsCreatingJob(true)
    try {
      const response = await fetchWithCsrf(
        `${getBrowserApiBaseUrl()}/admin/ai/blog-fix-batch-jobs`,
        {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
            blogIds: selectedIdsForJob,
            all: false,
            selectionMode: mode,
            selectionLabel: mode === 'range'
              ? `range ${rangeStartIndex}-${rangeStartIndex + rangeBlogs.length - 1}`
              : mode === 'date'
                ? `date ${dateStart || 'start'} → ${dateEnd || 'end'}`
                : `${selectedIdsForJob.length} selected`,
            autoApply,
            workerCount: Number(workerCount || String(runtimeConfig?.batchConcurrency || 2)),
            codexModel,
            codexReasoningEffort,
          }),
        },
      )

      const payload = await readApiPayload(response)
      if (!response.ok) {
        throw new Error(payload.error || 'Failed to create AI batch job')
      }

      const jobId = payload.jobId as string
      const reusedExisting = recentJobs.some((job) => job.jobId === jobId)
      setSelectedJobItemId(null)
      await loadRecentJobs(jobId)
      await loadJobDetail(jobId)
      toast.success(reusedExisting ? 'Existing AI batch job reopened' : 'AI batch job started')
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'Failed to create AI batch job')
    } finally {
      setIsCreatingJob(false)
    }
  }

  async function applyJobResults(jobItemIds?: string[]) {
    if (!activeJobId) {
      return
    }

    setIsApplyingJob(true)
    try {
      const response = await fetchWithCsrf(
        `${getBrowserApiBaseUrl()}/admin/ai/blog-fix-batch-jobs/${encodeURIComponent(activeJobId)}/apply`,
        {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ jobItemIds }),
        },
      )

      const payload = await readApiPayload(response)
      if (!response.ok) {
        throw new Error(payload.error || 'Failed to apply AI batch results')
      }

      setActiveJob(payload as BlogAiBatchJobDetail)
      await loadRecentJobs(activeJobId)
      onApplied?.()
      toast.success('AI batch results applied')
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'Failed to apply AI batch results')
    } finally {
      setIsApplyingJob(false)
    }
  }

  async function cancelJob(jobId = activeJobId) {
    if (!jobId) {
      return
    }

    setIsCancellingJob(true)
    try {
      const response = await fetchWithCsrf(
        `${getBrowserApiBaseUrl()}/admin/ai/blog-fix-batch-jobs/${encodeURIComponent(jobId)}/cancel`,
        { method: 'POST' },
      )

      const payload = await readApiPayload(response)
      if (!response.ok) {
        throw new Error(payload.error || 'Failed to cancel AI batch job')
      }

      await loadRecentJobs(jobId)
      await loadJobDetail(jobId)
      toast.success('AI batch cancellation requested')
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'Failed to cancel AI batch job')
    } finally {
      setIsCancellingJob(false)
    }
  }

  async function cancelQueuedJobs() {
    setIsCancellingJob(true)
    try {
      const response = await fetchWithCsrf(
        `${getBrowserApiBaseUrl()}/admin/ai/blog-fix-batch-jobs/cancel-queued`,
        { method: 'POST' },
      )

      const payload = await readApiPayload(response)
      if (!response.ok) {
        throw new Error(payload.error || 'Failed to cancel queued AI batch jobs')
      }

      await loadRecentJobs()
      if (activeJobId) {
        await loadJobDetail(activeJobId)
      }
      toast.success(`Cancelled ${payload.cancelled ?? 0} queued AI job(s)`)
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'Failed to cancel queued AI batch jobs')
    } finally {
      setIsCancellingJob(false)
    }
  }

  async function clearCompletedJobs() {
    setIsCleaningJobs(true)
    try {
      const response = await fetchWithCsrf(
        `${getBrowserApiBaseUrl()}/admin/ai/blog-fix-batch-jobs/clear-completed`,
        { method: 'POST' },
      )

      const payload = await readApiPayload(response)
      if (!response.ok) {
        throw new Error(payload.error || 'Failed to clear completed AI batch jobs')
      }

      if (activeJob?.status === 'completed') {
        setActiveJobId(null)
        setActiveJob(null)
        setSelectedJobItemId(null)
      }

      await loadRecentJobs()
      toast.success(`Cleared ${payload.cleared ?? 0} completed AI job(s)`)
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'Failed to clear completed AI batch jobs')
    } finally {
      setIsCleaningJobs(false)
    }
  }

  async function removeTerminalJob(jobId: string) {
    setRemovingJobId(jobId)
    try {
      const response = await fetchWithCsrf(
        `${getBrowserApiBaseUrl()}/admin/ai/blog-fix-batch-jobs/${encodeURIComponent(jobId)}`,
        { method: 'DELETE' },
      )

      const payload = await readApiPayload(response)
      if (!response.ok) {
        throw new Error(payload.error || 'Failed to remove AI batch job')
      }

      if (activeJobId === jobId) {
        setActiveJobId(null)
        setActiveJob(null)
        setSelectedJobItemId(null)
      }

      await loadRecentJobs()
      toast.success('AI batch job removed')
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'Failed to remove AI batch job')
    } finally {
      setRemovingJobId(null)
    }
  }

  if (!isOpen) {
    return null
  }

  return (
    <div data-testid="admin-blog-batch-ai-panel" className="border-b border-gray-200 px-4 py-4 dark:border-gray-800">
      <div className="flex flex-wrap items-center gap-3">
        <div className="rounded-md border border-dashed border-border/80 bg-muted/20 px-3 py-2 text-sm">
          <p className="font-medium">{selectedIdsForJob.length} selected</p>
          <p className="text-xs text-muted-foreground">
            {selectedIdsForJob.length === 0
              ? 'Select blog rows to create a batch job.'
              : selectionSummary}
          </p>
        </div>
        <div className="flex flex-wrap items-center gap-2 rounded-md border border-input bg-background px-3 py-2 text-sm">
          <Label htmlFor="batch-mode">Mode</Label>
          <select
            id="batch-mode"
            value={mode}
            onChange={(event) => setMode(event.target.value as 'selected' | 'range' | 'date')}
            className="bg-transparent text-sm outline-none"
          >
            <option value="selected">Selected rows</option>
            <option value="range">Range</option>
            <option value="date">Date range</option>
          </select>
          {mode === 'range' ? (
            <>
              <Label htmlFor="batch-range-start">Start</Label>
              <input
                id="batch-range-start"
                aria-label="Batch range start"
                value={rangeStart}
                onChange={(event) => setRangeStart(event.target.value)}
                className="w-16 rounded-md border border-input bg-background px-2 py-1 text-sm"
              />
              <Label htmlFor="batch-range-count">Count</Label>
              <input
                id="batch-range-count"
                aria-label="Batch range count"
                value={rangeCount}
                onChange={(event) => setRangeCount(event.target.value)}
                className="w-16 rounded-md border border-input bg-background px-2 py-1 text-sm"
              />
            </>
          ) : null}
          {mode === 'date' ? (
            <>
              <Label htmlFor="batch-date-start">Start date</Label>
              <input
                id="batch-date-start"
                aria-label="Batch date start"
                type="date"
                value={dateStart}
                onChange={(event) => setDateStart(event.target.value)}
                className="rounded-md border border-input bg-background px-2 py-1 text-sm"
              />
              <Label htmlFor="batch-date-end">End date</Label>
              <input
                id="batch-date-end"
                aria-label="Batch date end"
                type="date"
                value={dateEnd}
                onChange={(event) => setDateEnd(event.target.value)}
                className="rounded-md border border-input bg-background px-2 py-1 text-sm"
              />
              <span className="text-xs text-muted-foreground">publishedAt, fallback updatedAt</span>
            </>
          ) : null}
        </div>
        <div className="flex flex-wrap items-center gap-2 rounded-md border border-input bg-background px-3 py-2 text-sm">
          <span className="text-xs text-muted-foreground">Provider</span>
          <span data-testid="admin-blog-batch-ai-provider" className="font-medium uppercase">{runtimeConfig?.provider ?? 'loading'}</span>
          {runtimeConfig?.provider === 'codex' ? (
            <>
              <Label htmlFor="batch-worker-count">Workers</Label>
              <input
                id="batch-worker-count"
                aria-label="Batch worker count"
                type="number"
                min={1}
                max={8}
                value={workerCount}
                onChange={(event) => setWorkerCount(event.target.value)}
                className="w-16 rounded-md border border-input bg-background px-2 py-1 text-sm"
              />
              <span className="text-xs text-muted-foreground">default {runtimeConfig.batchConcurrency}</span>
              <Label htmlFor="list-codex-model" className="sr-only">Codex model</Label>
              <select
                id="list-codex-model"
                aria-label="Blog batch codex model"
                value={codexModel}
                onChange={(event) => {
                  setCodexModel(event.target.value)
                  if (typeof window !== 'undefined') {
                    window.localStorage.setItem('admin-ai-codex-model', event.target.value)
                  }
                }}
                className="bg-transparent text-sm outline-none"
              >
                {(runtimeConfig.allowedCodexModels || []).map((model) => (
                  <option key={model} value={model}>{model}</option>
                ))}
              </select>
              <Label htmlFor="list-codex-reasoning" className="sr-only">Codex reasoning</Label>
              <select
                id="list-codex-reasoning"
                aria-label="Blog batch codex reasoning"
                value={codexReasoningEffort}
                onChange={(event) => {
                  setCodexReasoningEffort(event.target.value)
                  if (typeof window !== 'undefined') {
                    window.localStorage.setItem('admin-ai-codex-reasoning', event.target.value)
                  }
                }}
                className="bg-transparent text-sm outline-none"
              >
                {(runtimeConfig.allowedCodexReasoningEfforts || []).map((effort) => (
                  <option key={effort} value={effort}>{effort}</option>
                ))}
              </select>
              <label className="ml-2 flex items-center gap-2 text-sm">
                <input
                  type="checkbox"
                  checked={autoApply}
                  onChange={(event) => setAutoApply(event.target.checked)}
                />
                <span>Auto-apply successful results</span>
              </label>
            </>
          ) : null}
        </div>
        <Button type="button" onClick={() => void createBatchAiJob()} disabled={selectedIdsForJob.length === 0 || isCreatingJob}>
          {isCreatingJob ? 'Creating job...' : 'Generate AI Fix job'}
        </Button>
        {activeJob && ['queued', 'running'].includes(activeJob.status) ? (
          <Button type="button" variant="outline" onClick={() => void cancelJob()} disabled={isCancellingJob}>
            {isCancellingJob ? 'Cancelling...' : 'Cancel job'}
          </Button>
        ) : null}
      </div>

      <div className="mt-4 grid gap-4 lg:grid-cols-[320px_minmax(0,1fr)]">
        <aside className="space-y-3 rounded-2xl border border-border/80 bg-muted/10 p-3">
          <div className="flex items-start justify-between gap-3">
            <div>
              <p className="text-sm font-medium text-gray-900 dark:text-gray-100">Recent AI jobs</p>
              <p className="text-xs text-muted-foreground">Processing continues while you browse the blog list.</p>
              <p className="mt-1 text-xs text-muted-foreground">
                running {jobCounts.runningCount} · queued {jobCounts.queuedCount} · completed {jobCounts.completedCount} · failed {jobCounts.failedCount} · cancelled {jobCounts.cancelledCount}
              </p>
              {currentRunningJob ? (
                <p className="mt-1 text-xs text-amber-600" data-testid="admin-blog-current-running-job">
                  Running now: {currentRunningJob.selectionLabel || currentRunningJob.jobId}
                </p>
              ) : null}
            </div>
            <div className="flex flex-col items-end gap-2">
              {queuedJobs.length > 0 ? (
                <Button size="sm" variant="outline" type="button" onClick={() => void cancelQueuedJobs()} disabled={isCancellingJob}>
                  {isCancellingJob ? 'Cancelling queued...' : `Cancel queued (${queuedJobs.length})`}
                </Button>
              ) : null}
              {jobCounts.completedCount > 0 ? (
                <Button size="sm" variant="outline" type="button" onClick={() => void clearCompletedJobs()} disabled={isCleaningJobs}>
                  {isCleaningJobs ? 'Clearing completed...' : `Clear completed (${jobCounts.completedCount})`}
                </Button>
              ) : null}
            </div>
          </div>
          {recentJobs.length === 0 ? (
            <p className="text-xs text-muted-foreground">No AI jobs yet.</p>
          ) : recentJobs.map((job) => (
            <button
              key={job.jobId}
              type="button"
              onClick={() => {
                setActiveJobId(job.jobId)
                setSelectedJobItemId(null)
              }}
              className={`w-full rounded-xl border px-3 py-2 text-left text-sm transition ${
                activeJobId === job.jobId ? 'border-primary/40 bg-primary/5' : 'border-border/80 hover:bg-muted/30'
              }`}
            >
              <div className="flex items-center justify-between gap-3">
                <span className="font-medium uppercase">{job.status}</span>
                <div className="flex items-center gap-2">
                  {job.selectionKey && duplicateCounts[job.selectionKey] > 1 ? (
                    <Badge variant="secondary">duplicate x{duplicateCounts[job.selectionKey]}</Badge>
                  ) : null}
                  <span className="text-xs text-muted-foreground">{job.processedCount}/{job.totalCount}</span>
                  {['completed', 'failed', 'cancelled'].includes(job.status) ? (
                    <Button
                      size="icon"
                      variant="ghost"
                      type="button"
                      className="h-7 w-7"
                      aria-label={`Remove ${job.status} job`}
                      disabled={removingJobId === job.jobId}
                      onClick={(event) => {
                        event.stopPropagation()
                        void removeTerminalJob(job.jobId)
                      }}
                    >
                      <X className="h-4 w-4" />
                    </Button>
                  ) : null}
                </div>
              </div>
              <p className="mt-1 text-xs text-muted-foreground">
                {job.selectionLabel || job.selectionMode} · {job.provider} · workers {job.workerCount ?? runtimeConfig?.batchConcurrency ?? 2} · {job.model}{job.reasoningEffort ? ` · ${job.reasoningEffort}` : ''}{job.autoApply ? ' · auto-apply' : ''}
              </p>
              {['queued', 'running'].includes(job.status) ? (
                <Button
                  size="sm"
                  variant="outline"
                  type="button"
                  className="mt-2"
                  onClick={(event) => {
                    event.stopPropagation()
                    void cancelJob(job.jobId)
                  }}
                >
                  Cancel queued/running
                </Button>
              ) : null}
            </button>
          ))}
        </aside>

        <div className="space-y-3 rounded-2xl border border-border/80 bg-muted/10 p-3">
          {activeJob ? (
            <>
              <p data-testid="admin-blog-batch-ai-status" className="text-sm text-muted-foreground">
                {activeJob.status} · {activeJob.processedCount}/{activeJob.totalCount} processed · {activeJob.succeededCount} succeeded · {activeJob.failedCount} failed
              </p>
              {activeJob.autoApply ? (
                <p className="text-xs text-emerald-600">Auto-apply is enabled for this job. Successful results save automatically.</p>
              ) : null}
              {activeJob.status === 'completed' && !activeJob.autoApply ? (
                <Button
                  size="sm"
                  type="button"
                  onClick={() => void applyJobResults()}
                  disabled={isApplyingJob || !activeJob.items.some((item) => item.status === 'succeeded' && !item.appliedAt)}
                >
                  {isApplyingJob ? 'Applying...' : 'Apply all successful'}
                </Button>
              ) : null}
              <div className="max-h-64 space-y-2 overflow-y-auto">
                {activeJob.items.map((item) => (
                  <div key={item.jobItemId} className="rounded-xl border border-border/70 bg-background px-3 py-2 text-sm">
                    <div className="flex items-center justify-between gap-3">
                      <button
                        type="button"
                        className="min-w-0 flex-1 truncate text-left font-medium"
                        onClick={() => setSelectedJobItemId(item.jobItemId)}
                      >
                        {item.title}
                      </button>
                      <Badge variant="secondary">{item.status}</Badge>
                    </div>
                    {item.error ? <p className="mt-1 text-xs text-red-500">{item.error}</p> : null}
                    {item.status === 'succeeded' && !item.appliedAt && !activeJob.autoApply ? (
                      <Button size="sm" variant="outline" type="button" className="mt-2" onClick={() => void applyJobResults([item.jobItemId])} disabled={isApplyingJob}>
                        Apply this result
                      </Button>
                    ) : null}
                  </div>
                ))}
              </div>
              {previewJobItem?.fixedHtml ? (
                <div className="rounded-xl border border-border/70 bg-background p-3">
                  <p className="mb-2 text-xs font-medium text-muted-foreground">Preview</p>
                  <div className="prose prose-sm dark:prose-invert max-w-none" dangerouslySetInnerHTML={{ __html: previewJobItem.fixedHtml }} />
                </div>
              ) : null}
            </>
          ) : (
            <p className="text-sm text-muted-foreground">Open or create a batch AI job to see progress here.</p>
          )}
        </div>
      </div>
    </div>
  )
}

function rankStatus(status: string) {
  switch (status) {
    case 'running':
      return 0
    case 'queued':
      return 1
    case 'completed':
      return 2
    case 'failed':
      return 3
    case 'cancelled':
      return 4
    default:
      return 5
  }
}

function resolveBlogDate(blog: BlogBatchCandidate) {
  const value = blog.publishedAt ?? blog.updatedAt ?? null
  return value ? value.slice(0, 10) : null
}

function summarizeSelectionTitles(titles: string[]) {
  if (titles.length === 0) {
    return ''
  }

  if (titles.length <= 5) {
    return titles.join(' · ')
  }

  return `${titles.slice(0, 5).join(' · ')} · +${titles.length - 5} more`
}

async function readApiPayload(response: Response): Promise<Record<string, any>> {
  const text = await response.text()
  if (!text) {
    return {}
  }

  try {
    return JSON.parse(text) as Record<string, any>
  } catch {
    return { error: text }
  }
}
