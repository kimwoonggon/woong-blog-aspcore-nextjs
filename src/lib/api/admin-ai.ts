import { getBrowserApiBaseUrl } from '@/lib/api/browser'

export interface AdminAiRuntimeConfig {
  provider: string
  defaultModel: string
  codexModel: string
  codexReasoningEffort: string
  allowedCodexModels: string[]
  allowedCodexReasoningEfforts: string[]
  batchConcurrency: number
  batchCompletedRetentionDays: number
}

export interface BlogAiBatchJobSummary {
  jobId: string
  status: string
  selectionMode: string
  selectionLabel: string
  selectionKey: string
  autoApply: boolean
  workerCount?: number | null
  totalCount: number
  processedCount: number
  succeededCount: number
  failedCount: number
  provider: string
  model: string
  reasoningEffort?: string | null
  createdAt: string
  startedAt?: string | null
  finishedAt?: string | null
  cancelRequested: boolean
}

export interface BlogAiBatchJobItem {
  jobItemId: string
  blogId: string
  title: string
  status: string
  fixedHtml?: string | null
  error?: string | null
  provider?: string | null
  model?: string | null
  reasoningEffort?: string | null
  appliedAt?: string | null
}

export interface BlogAiBatchJobDetail extends BlogAiBatchJobSummary {
  items: BlogAiBatchJobItem[]
}

export interface BlogAiBatchJobListPayload {
  jobs: BlogAiBatchJobSummary[]
  runningCount: number
  queuedCount: number
  completedCount: number
  failedCount: number
  cancelledCount: number
}

export function getAdminAiErrorMessage(payload: Record<string, unknown> | null | undefined, fallback: string) {
  if (!payload) {
    return fallback
  }

  const direct = payload.error
  if (typeof direct === 'string' && direct.trim()) {
    return direct
  }

  const detail = payload.detail
  if (typeof detail === 'string' && detail.trim()) {
    return detail
  }

  const title = payload.title
  if (typeof title === 'string' && title.trim()) {
    return title
  }

  return fallback
}

export async function fetchAdminAiRuntimeConfigBrowser() {
  const response = await fetch(`${getBrowserApiBaseUrl()}/admin/ai/runtime-config`, {
    credentials: 'include',
    cache: 'no-store',
  })

  if (!response.ok) {
    throw new Error('Failed to load AI runtime config.')
  }

  return response.json() as Promise<AdminAiRuntimeConfig>
}

export async function listBlogAiBatchJobsBrowser() {
  const response = await fetch(`${getBrowserApiBaseUrl()}/admin/ai/blog-fix-batch-jobs`, {
    credentials: 'include',
    cache: 'no-store',
  })

  if (!response.ok) {
    throw new Error('Failed to load AI batch jobs.')
  }

  const payload = await response.json() as Partial<BlogAiBatchJobListPayload>
  return {
    jobs: payload.jobs ?? [],
    runningCount: payload.runningCount ?? 0,
    queuedCount: payload.queuedCount ?? 0,
    completedCount: payload.completedCount ?? 0,
    failedCount: payload.failedCount ?? 0,
    cancelledCount: payload.cancelledCount ?? 0,
  } satisfies BlogAiBatchJobListPayload
}

export async function getBlogAiBatchJobBrowser(jobId: string) {
  const response = await fetch(`${getBrowserApiBaseUrl()}/admin/ai/blog-fix-batch-jobs/${encodeURIComponent(jobId)}`, {
    credentials: 'include',
    cache: 'no-store',
  })

  if (!response.ok) {
    throw new Error('Failed to load AI batch job.')
  }

  return response.json() as Promise<BlogAiBatchJobDetail>
}
