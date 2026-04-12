import { expect, test } from '@playwright/test'

test('works detail exposes previous or next navigation links', async ({ page }) => {
  await page.goto('/works/seeded-work')

  const nav = page.getByTestId('work-prev-next')
  await expect(nav).toBeVisible()

  const links = nav.getByRole('link')
  await expect(links).toHaveCount(2)

  const link = links.first()
  await expect(link).toHaveAttribute('href', /\/works\/.+/)
  await expect(link).toContainText(/Previous|Next/)
})
