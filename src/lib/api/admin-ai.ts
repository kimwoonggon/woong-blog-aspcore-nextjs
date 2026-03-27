import { getBrowserApiBaseUrl } from '@/lib/api/browser'

export interface AdminAiRuntimeConfig {
  provider: string
  defaultModel: string
  codexModel: string
  codexReasoningEffort: string
  allowedCodexModels: string[]
  allowedCodexReasoningEfforts: string[]
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
