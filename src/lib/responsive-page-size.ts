const MOBILE_WIDTH = 640
const TABLET_WIDTH = 1024
const WIDE_DESKTOP_WIDTH = 1440
const MIN_VIEWPORT_HEIGHT = 720
const MAX_VIEWPORT_HEIGHT = 1800

function clamp(value: number, min: number, max: number) {
  return Math.min(max, Math.max(min, value))
}

function interpolatePageSize(height: number, minPageSize: number, maxPageSize: number) {
  if (maxPageSize <= minPageSize) {
    return minPageSize
  }

  const ratio = clamp(
    (height - MIN_VIEWPORT_HEIGHT) / (MAX_VIEWPORT_HEIGHT - MIN_VIEWPORT_HEIGHT),
    0,
    1,
  )

  return clamp(
    Math.round(minPageSize + ((maxPageSize - minPageSize) * ratio)),
    minPageSize,
    maxPageSize,
  )
}

interface ResponsivePageSizeOptions {
  width: number
  height: number
  desktopPageSize: number
  tabletPageSize: number
  mobilePageSize: number
}

export function resolveResponsivePageSize({
  width,
  height,
  desktopPageSize,
  tabletPageSize,
  mobilePageSize,
}: ResponsivePageSizeOptions) {
  if (width < MOBILE_WIDTH) {
    return mobilePageSize
  }

  if (width < TABLET_WIDTH) {
    return interpolatePageSize(height, mobilePageSize, tabletPageSize)
  }

  if (width >= WIDE_DESKTOP_WIDTH) {
    return Math.max(desktopPageSize, mobilePageSize)
  }

  return interpolatePageSize(height, mobilePageSize, desktopPageSize)
}
