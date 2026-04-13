import { expect, test } from '@playwright/test'

test('public home page renders the full primary navigation', async ({ page }) => {
  await page.goto('/')

  const headerNav = page.getByRole('banner').getByRole('navigation')
  await expect(headerNav.getByRole('link', { name: 'Home', exact: true })).toBeVisible()
  await expect(headerNav.getByRole('link', { name: 'Introduction', exact: true })).toHaveAttribute('href', '/introduction')
  await expect(headerNav.getByRole('link', { name: 'Works', exact: true })).toHaveAttribute('href', '/works')
  await expect(headerNav.getByRole('link', { name: 'Blog', exact: true })).toHaveAttribute('href', '/blog')
  await expect(headerNav.getByRole('link', { name: 'Contact', exact: true })).toHaveAttribute('href', '/contact')
  await expect(headerNav.getByRole('link', { name: 'Resume', exact: true })).toHaveAttribute('href', '/resume')
})

test('primary navbar routes to every public destination', async ({ page }) => {
  const cases = [
    { label: 'Home', href: '/', assertion: () => expect(page).toHaveURL(/\/$/) },
    { label: 'Introduction', href: '/introduction', assertion: () => expect(page.locator('main h1')).toContainText('Introduction') },
    { label: 'Works', href: '/works', assertion: () => expect(page.locator('main h1')).toHaveText('Works') },
    { label: 'Blog', href: '/blog', assertion: () => expect(page.locator('main h1')).toHaveText('Blog') },
    { label: 'Contact', href: '/contact', assertion: () => expect(page.locator('main h1')).toHaveText('Contact') },
    { label: 'Resume', href: '/resume', assertion: () => expect(page.locator('main h1')).toHaveText('Resume') },
  ] as const

  for (const item of cases) {
    await page.goto('/')
    await page.getByRole('banner').getByRole('navigation').getByRole('link', { name: item.label, exact: true }).click()
    await expect(page).toHaveURL(new RegExp(`${item.href === '/' ? '/$' : item.href.replace('/', '\\/')}(\\?.*)?$`))
    await item.assertion()
  }
})
