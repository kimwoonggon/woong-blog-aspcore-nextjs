import { expect, test } from '@playwright/test'

const ctaCards = [
  { label: 'Works', href: '/works', heading: 'Works' },
  { label: 'Study', href: '/blog', heading: 'Study' },
  { label: 'Introduction', href: '/introduction', heading: 'Introduction' },
] as const

test('home CTA section exposes quick navigation cards for works, study, and introduction', async ({ page }) => {
  await page.goto('/')

  const section = page.locator('section').filter({
    has: page.getByRole('heading', { name: 'Move through the portfolio with intent' }),
  })

  await expect(section).toBeVisible()

  for (const item of ctaCards) {
    const link = section.locator(`a[href="${item.href}"]`)
    await expect(link).toBeVisible()
    await expect(link).toContainText(item.label)
    await expect(link).toHaveAttribute('href', item.href)
  }
})

test('home CTA section cards navigate to their destination pages', async ({ page }) => {
  for (const item of ctaCards) {
    await page.goto('/')

    const section = page.locator('section').filter({
      has: page.getByRole('heading', { name: 'Move through the portfolio with intent' }),
    })

    await section.locator(`a[href="${item.href}"]`).click()
    await expect(page).toHaveURL(new RegExp(`${item.href.replace('/', '\\/')}(\\?.*)?$`))
    await expect(page.locator('main h1')).toContainText(item.heading)
  }
})
