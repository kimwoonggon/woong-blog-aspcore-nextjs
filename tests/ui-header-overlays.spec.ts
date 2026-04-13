import { expect, test, type Locator } from '@playwright/test'

type Viewport = { width: number; height: number }
type Rect = { left: number; right: number; top: number; bottom: number; width: number; height: number }

const desktopViewport: Viewport = { width: 1440, height: 900 }

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

async function expectMenuBelowTrigger(trigger: Locator, menu: Locator, viewport: Viewport) {
  const [triggerBox, menuBox] = await Promise.all([box(trigger), box(menu)])

  expect(menuBox.top).toBeGreaterThanOrEqual(triggerBox.bottom - 4)
  expect(menuBox.left).toBeGreaterThanOrEqual(-1)
  expect(menuBox.right).toBeLessThanOrEqual(viewport.width + 1)
  expect(menuBox.bottom).toBeLessThanOrEqual(viewport.height + 1)
  expect(menuBox.height).toBeGreaterThan(0)
}

test('theme dropdown opens below the trigger without clipping', async ({ page }) => {
  await page.setViewportSize(desktopViewport)
  await page.goto('/')

  const themeToggle = page.getByTestId('theme-toggle')
  const menu = page.locator('[data-slot="dropdown-menu-content"]').first()

  await expect(themeToggle).toBeVisible()
  await themeToggle.click()
  await expect(menu).toBeVisible()

  await expectMenuBelowTrigger(themeToggle, menu, desktopViewport)
  await expect(menu.getByRole('menuitemradio', { name: 'Light' })).toBeVisible()
  await expect(menu.getByRole('menuitemradio', { name: 'Dark' })).toBeVisible()
  await expect(menu.getByRole('menuitemradio', { name: 'System' })).toBeVisible()
})

test.describe('signed-in menu', () => {
  test('signed-in dropdown opens below the trigger without clipping', async ({ page }) => {
    await page.setViewportSize(desktopViewport)
    await page.goto('/')

    const trigger = page.getByRole('button', { name: 'Open signed-in menu' })
    const menu = page.locator('[data-slot="dropdown-menu-content"]').first()

    await expect(trigger).toBeVisible()
    await trigger.click()
    await expect(menu).toBeVisible()

    await expectMenuBelowTrigger(trigger, menu, desktopViewport)
    await expect(menu.getByRole('menuitem', { name: 'My Page' })).toBeVisible()
    await expect(menu.getByRole('menuitem', { name: 'Logout' })).toBeVisible()
  })
})
