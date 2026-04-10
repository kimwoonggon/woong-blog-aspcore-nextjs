export function parseWorkContentHtml(contentJson: string) {
  try {
    const parsed = JSON.parse(contentJson) as { html?: string }
    return parsed.html ?? ''
  } catch {
    return ''
  }
}

export function formatDetailPublishDate(value?: string | null) {
  return value
    ? new Date(value).toLocaleDateString('en-US', {
        year: 'numeric',
        month: 'long',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
      })
    : 'Unknown Date'
}
