import { expect, test } from '@playwright/test'

async function getWidth(page: import('@playwright/test').Page, testId: string) {
  return page.getByTestId(testId).evaluate((element) => element.getBoundingClientRect().width)
}

test('blog detail related content width stays aligned with the reading column', async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 900 })
  await page.goto('/blog/seeded-blog')
  await expect(page.getByTestId('related-blog-card').first()).toBeVisible()
  await expect(page.getByTestId('blog-detail-body')).toHaveClass(/max-w-3xl/)
  await expect(page.getByTestId('blog-related-shell')).toHaveClass(/max-w-3xl/)

  const bodyWidth = await getWidth(page, 'blog-detail-body')
  const relatedWidth = await getWidth(page, 'blog-related-shell')
  expect(Math.abs(bodyWidth - relatedWidth)).toBeLessThanOrEqual(4)
})

test('work detail related content width stays aligned with the reading column', async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 900 })
  await page.goto('/works/seeded-work')
  await expect(page.getByTestId('work-detail-body')).toHaveClass(/max-w-3xl/)
  await expect(page.getByTestId('work-related-shell')).toHaveClass(/max-w-3xl/)

  const bodyWidth = await getWidth(page, 'work-detail-body')
  const relatedWidth = await getWidth(page, 'work-related-shell')
  expect(Math.abs(bodyWidth - relatedWidth)).toBeLessThanOrEqual(4)
})
