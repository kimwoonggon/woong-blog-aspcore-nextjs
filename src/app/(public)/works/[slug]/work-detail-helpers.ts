export function parseWorkContentHtml(contentJson: string) {
  try {
    const parsed = JSON.parse(contentJson) as unknown
    if (!parsed || typeof parsed !== 'object' || Array.isArray(parsed)) {
      return ''
    }

    const html = (parsed as Record<string, unknown>).html
    return typeof html === 'string' ? html : ''
  } catch {
    return ''
  }
}

export function formatDetailPublishDate(value?: string | null) {
  if (!value) {
    return 'Unknown Date'
  }

  const date = new Date(value)
  if (Number.isNaN(date.getTime())) {
    return 'Unknown Date'
  }

  return date.toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}
