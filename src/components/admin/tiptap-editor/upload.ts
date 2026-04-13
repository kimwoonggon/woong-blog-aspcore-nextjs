import { fetchWithCsrf } from '@/lib/api/auth'
import { getBrowserApiBaseUrl } from '@/lib/api/browser'

export async function uploadEditorImage(file: File) {
  const formData = new FormData()
  formData.append('file', file)

  const response = await fetchWithCsrf(`${getBrowserApiBaseUrl()}/uploads`, {
    method: 'POST',
    body: formData,
  })

  if (!response.ok) {
    throw new Error('Failed to upload image')
  }

  const data = await response.json()
  return data.url as string
}
