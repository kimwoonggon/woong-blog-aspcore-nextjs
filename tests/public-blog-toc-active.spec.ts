import { expect, test } from '@playwright/test'

test('blog detail TOC updates its active heading as the reader moves deeper into the article', async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 900 })
  await page.goto('/blog/seeded-blog')

  const toc = page.getByTestId('blog-toc')
  await expect(toc).toBeVisible()

  const links = toc.getByRole('link')
  expect(await links.count()).toBeGreaterThan(1)

  const firstLink = links.first()
  const secondLink = links.nth(1)
  const targetId = (await secondLink.getAttribute('href'))?.replace(/^#/, '')

  expect(targetId).toBeTruthy()
  await expect.poll(() => firstLink.getAttribute('class')).toContain('bg-muted')

  await page.locator(`#${targetId}`).scrollIntoViewIfNeeded()
  await secondLink.click()

  await expect(page).toHaveURL(new RegExp(`#${targetId}$`))
  await expect.poll(() => secondLink.getAttribute('class')).toContain('bg-muted')
})
