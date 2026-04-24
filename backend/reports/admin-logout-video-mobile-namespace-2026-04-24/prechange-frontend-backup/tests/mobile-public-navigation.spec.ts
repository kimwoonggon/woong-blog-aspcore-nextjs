import { expect, test } from './helpers/performance-test'

test('mobile header controls and bottom tabs render with all destinations', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 820 })
  await page.goto('/')

  await expect(page.getByRole('button', { name: 'Toggle Menu' })).toBeVisible()
  await expect(page.getByRole('button', { name: 'Open search' })).toBeVisible()
  await expect(page.getByTestId('mobile-header-theme-toggle')).toBeVisible()

  const bottomNav = page.getByTestId('mobile-bottom-nav')
  await expect(bottomNav).toBeVisible()
  await expect(bottomNav.getByRole('link', { name: 'Home' })).toBeVisible()
  await expect(bottomNav.getByRole('link', { name: 'Intro' })).toBeVisible()
  await expect(bottomNav.getByRole('link', { name: 'Works' })).toBeVisible()
  await expect(bottomNav.getByRole('link', { name: 'Study' })).toBeVisible()
  await expect(bottomNav.getByRole('link', { name: 'Contact' })).toBeVisible()
  await expect(bottomNav.getByRole('link', { name: 'Resume' })).toBeVisible()

  const routes = [
    { label: 'Intro', expectedPath: '/introduction', heading: 'Introduction' },
    { label: 'Works', expectedPath: '/works', heading: 'Works' },
    { label: 'Study', expectedPath: '/blog', heading: 'Study' },
    { label: 'Contact', expectedPath: '/contact', heading: 'Contact' },
    { label: 'Resume', expectedPath: '/resume', heading: 'Resume' },
    { label: 'Home', expectedPath: '/', heading: null },
  ] as const

  for (const route of routes) {
    await bottomNav.getByRole('link', { name: route.label, exact: true }).click()
    await expect(page).toHaveURL(new RegExp(route.expectedPath === '/' ? '/$' : `${route.expectedPath.replace('/', '\\/')}(\\?.*)?$`))

    if (route.heading) {
      await expect(page.locator('main h1')).toContainText(route.heading)
    }
  }
})

test('mobile search button focuses list search inputs or routes to study', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 820 })

  await page.goto('/blog')
  await page.getByRole('button', { name: 'Open search' }).click()
  await expect(page.getByRole('textbox', { name: 'Search studies' })).toBeFocused()

  await page.goto('/works')
  await page.getByRole('button', { name: 'Open search' }).click()
  await expect(page.getByRole('textbox', { name: 'Search work' })).toBeFocused()

  await page.goto('/contact')
  await page.getByRole('button', { name: 'Open search' }).click()
  await expect(page).toHaveURL(/\/blog(\?.*)?$/)
  await expect(page.getByRole('textbox', { name: 'Search studies' })).toBeFocused()
})

test('desktop keeps inline navigation and hides mobile bottom tabs', async ({ page }) => {
  await page.setViewportSize({ width: 1366, height: 900 })
  await page.goto('/blog')

  await expect(page.getByRole('banner').getByRole('navigation')).toBeVisible()
  await expect(page.getByTestId('mobile-bottom-nav')).toBeHidden()
  await expect(page.getByLabel('Study pagination')).toBeVisible()
})
