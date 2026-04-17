import { fetchWithCsrf } from '@/lib/api/auth'
import { getBrowserApiBaseUrl } from '@/lib/api/browser'

const MAX_EDITOR_IMAGE_EDGE = 1600
const EDITOR_IMAGE_QUALITY = 0.82

function canResizeImage(file: File) {
  return ['image/jpeg', 'image/png', 'image/webp'].includes(file.type)
}

function getOutputType(file: File) {
  return file.type === 'image/png' ? 'image/png' : 'image/webp'
}

function getOutputName(file: File, outputType: string) {
  if (outputType === file.type) {
    return file.name
  }

  return `${file.name.replace(/\.[^.]+$/, '') || 'image'}.webp`
}

async function canvasToBlob(canvas: HTMLCanvasElement, type: string, quality: number) {
  return new Promise<Blob | null>((resolve) => {
    canvas.toBlob(resolve, type, quality)
  })
}

export async function resizeEditorImageFile(file: File) {
  if (!canResizeImage(file) || typeof createImageBitmap !== 'function') {
    return file
  }

  let bitmap: ImageBitmap
  try {
    bitmap = await createImageBitmap(file)
  } catch {
    return file
  }
  try {
    const longestEdge = Math.max(bitmap.width, bitmap.height)
    if (longestEdge <= MAX_EDITOR_IMAGE_EDGE) {
      return file
    }

    const scale = MAX_EDITOR_IMAGE_EDGE / longestEdge
    const width = Math.max(1, Math.round(bitmap.width * scale))
    const height = Math.max(1, Math.round(bitmap.height * scale))
    const canvas = document.createElement('canvas')
    canvas.width = width
    canvas.height = height

    const context = canvas.getContext('2d')
    if (!context) {
      return file
    }

    context.drawImage(bitmap, 0, 0, width, height)
    const outputType = getOutputType(file)
    const blob = await canvasToBlob(canvas, outputType, EDITOR_IMAGE_QUALITY)
    if (!blob) {
      return file
    }

    return new File([blob], getOutputName(file, outputType), {
      type: outputType,
      lastModified: Date.now(),
    })
  } finally {
    bitmap.close()
  }
}

export async function uploadEditorImage(file: File) {
  const uploadFile = await resizeEditorImageFile(file)
  const formData = new FormData()
  formData.append('file', uploadFile)
  formData.append('bucket', 'blogs/inline')

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
