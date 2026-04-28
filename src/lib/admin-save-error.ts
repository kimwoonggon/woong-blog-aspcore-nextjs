const TECHNICAL_ERROR_PATTERN = /SQLSTATE|stack\s*trace|status\s*5\d\d|WoongBlog\.Api|Npgsql|System\.|Exception/i

export function sanitizeAdminSaveError(message: string, fallback: string) {
  const normalized = message.trim()

  if (!normalized || TECHNICAL_ERROR_PATTERN.test(normalized)) {
    return fallback
  }

  return normalized
}
