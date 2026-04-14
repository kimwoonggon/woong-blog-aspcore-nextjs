import { expect, test, type Locator } from '@playwright/test'

type Box = { left: number; right: number; top: number; bottom: number }

async function readBox(locator: Locator): Promise<Box> {
  const rect = await locator.boundingBox()
  expect(rect).toBeTruthy()
  return {
    left: rect!.x,
    right: rect!.x + rect!.width,
    top: rect!.y,
    bottom: rect!.y + rect!.height,
  }
}

test('header collapses navigation before action controls overlap on medium desktop widths', async ({ page }) => {
  await page.setViewportSize({ width: 1366, height: 900 })
  await page.goto('/')

  const header = page.locator('header').first()
  await expect(header.locator('nav')).toBeHidden()

  const themeToggle = page.getByTestId('theme-toggle')
  const loginButton = page.getByRole('link', { name: 'Login' })
  const menuButton = page.getByRole('button', { name: 'Toggle Menu' })

  await expect(themeToggle).toBeVisible()
  await expect(loginButton).toBeVisible()
  await expect(menuButton).toBeVisible()

  const themeBox = await readBox(themeToggle)
  const loginBox = await readBox(loginButton)
  const menuBox = await readBox(menuButton)

  expect(themeBox.right).toBeLessThanOrEqual(loginBox.left - 8)
  expect(loginBox.right).toBeLessThanOrEqual(menuBox.left - 8)
})

test('header keeps desktop nav separated from action controls on wide screens', async ({ page }) => {
  await page.setViewportSize({ width: 1600, height: 900 })
  await page.goto('/')

  const header = page.locator('header').first()
  const nav = header.locator('nav')
  const themeToggle = page.getByTestId('theme-toggle')
  const loginButton = page.getByRole('link', { name: 'Login' })
  const menuButton = page.getByRole('button', { name: 'Toggle Menu' })

  await expect(nav).toBeVisible()
  await expect(themeToggle).toBeVisible()
  await expect(loginButton).toBeVisible()
  await expect(menuButton).toBeHidden()

  const navBox = await readBox(nav)
  const themeBox = await readBox(themeToggle)
  const loginBox = await readBox(loginButton)

  expect(navBox.right).toBeLessThanOrEqual(themeBox.left - 16)
  expect(themeBox.right).toBeLessThanOrEqual(loginBox.left - 8)
})
