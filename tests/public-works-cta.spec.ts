import { expect, test } from '@playwright/test'

const worksCtas = [
  { label: 'Start a conversation', href: '/contact', heading: 'Contact' },
  { label: 'Read the notes', href: '/blog', heading: 'Blog' },
] as const

test('works archive header exposes the conversation and notes CTAs', async ({ page }) => {
  await page.goto('/works')

  for (const item of worksCtas) {
    const link = page.getByRole('link', { name: item.label, exact: true })
    await expect(link).toBeVisible()
    await expect(link).toHaveAttribute('href', item.href)
  }
})

test('works archive header CTAs route to the expected public destinations', async ({ page }) => {
  for (const item of worksCtas) {
    await page.goto('/works')
    await page.getByRole('link', { name: item.label, exact: true }).click()
    await expect(page).toHaveURL(new RegExp(`${item.href.replace('/', '\\/')}(\\?.*)?$`))
    await expect(page.locator('main h1')).toContainText(item.heading)
  }
})
