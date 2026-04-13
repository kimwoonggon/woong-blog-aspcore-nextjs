import { expect, test, type Locator } from '@playwright/test'

type Viewport = { width: number; height: number }
type Rect = { left: number; right: number; top: number; bottom: number; width: number; height: number }

const drawerOnlyViewports: Viewport[] = [
  { width: 390, height: 844 },
  { width: 768, height: 1024 },
  { width: 1024, height: 768 },
]

const desktopInlineNavViewports: Viewport[] = [
  { width: 1280, height: 800 },
  { width: 1440, height: 900 },
  { width: 1728, height: 1117 },
]

async function box(locator: Locator) {
  const rect = await locator.boundingBox()
  expect(rect).toBeTruthy()
  return {
    left: rect!.x,
    right: rect!.x + rect!.width,
    top: rect!.y,
    bottom: rect!.y + rect!.height,
    width: rect!.width,
    height: rect!.height,
  } satisfies Rect
}

test.describe('responsive header', () => {
  for (const viewport of drawerOnlyViewports) {
    test(`uses drawer-only navigation below xl at ${viewport.width}x${viewport.height}`, async ({ page }) => {
      await page.setViewportSize(viewport)
      await page.goto('/')

      const banner = page.getByRole('banner')
      const desktopNav = banner.getByRole('navigation')
      const menuButton = page.getByRole('button', { name: 'Toggle Menu' })

      await expect(desktopNav).toBeHidden()
      await expect(menuButton).toBeVisible()

      await menuButton.click()
      const drawer = page.getByRole('dialog')
      await expect(drawer).toBeVisible()

      for (const label of ['Home', 'Introduction', 'Works', 'Blog', 'Contact', 'Resume']) {
        await expect(drawer.getByRole('link', { name: label, exact: true })).toBeVisible()
      }
    })
  }

  for (const viewport of desktopInlineNavViewports) {
    test(`keeps the inline nav non-overlapping at ${viewport.width}x${viewport.height}`, async ({ page }) => {
      await page.setViewportSize(viewport)
      await page.goto('/')

      const banner = page.getByRole('banner')
      const brand = banner.getByRole('link').first()
      const nav = banner.getByRole('navigation')
      const themeToggle = page.getByTestId('theme-toggle')

      await expect(nav).toBeVisible()
      await expect(themeToggle).toBeVisible()

      for (const label of ['Home', 'Introduction', 'Works', 'Blog', 'Contact', 'Resume']) {
        await expect(nav.getByRole('link', { name: label, exact: true })).toBeVisible()
      }

      const [brandBox, navBox, themeBox, headerBox] = await Promise.all([
        box(brand),
        box(nav),
        box(themeToggle),
        box(banner),
      ])

      expect(brandBox.right).toBeLessThan(navBox.left)
      expect(navBox.right).toBeLessThan(themeBox.left)
      expect(Math.abs((navBox.left + navBox.right) / 2 - viewport.width / 2)).toBeLessThanOrEqual(24)

      const overflow = await banner.evaluate((element: HTMLElement) => element.scrollWidth - element.clientWidth)
      expect(overflow).toBeLessThanOrEqual(1)
      expect(headerBox.width).toBeGreaterThan(0)
    })
  }
})
