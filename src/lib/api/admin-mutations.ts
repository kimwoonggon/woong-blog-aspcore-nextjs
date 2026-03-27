'use client'

import { fetchWithCsrf } from '@/lib/api/auth'
import { getBrowserApiBaseUrl } from '@/lib/api/browser'

async function assertOk(response: Response, fallbackMessage: string) {
  if (response.ok) {
    return
  }

  const body = await response.text()
  throw new Error(body || fallbackMessage)
}

export async function deleteAdminBlog(id: string) {
  const response = await fetchWithCsrf(`${getBrowserApiBaseUrl()}/admin/blogs/${encodeURIComponent(id)}`, {
    method: 'DELETE',
  })
  await assertOk(response, 'Failed to delete blog.')
}

export async function deleteAdminWork(id: string) {
  const response = await fetchWithCsrf(`${getBrowserApiBaseUrl()}/admin/works/${encodeURIComponent(id)}`, {
    method: 'DELETE',
  })
  await assertOk(response, 'Failed to delete work.')
}

export async function deleteManyAdminBlogs(ids: string[]) {
  for (const id of ids) {
    await deleteAdminBlog(id)
  }
}

export async function deleteManyAdminWorks(ids: string[]) {
  for (const id of ids) {
    await deleteAdminWork(id)
  }
}
