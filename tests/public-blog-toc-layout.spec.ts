import { expect, test } from './helpers/performance-test'

test('blog table of contents stays in a right rail instead of covering article content', async ({ page }) => {
  await page.setViewportSize({ width: 1600, height: 900 })
  await page.goto('/blog/seeded-blog')

  const body = page.getByTestId('blog-detail-body')
  const toc = page.getByTestId('blog-toc')

  await expect(page.locator('main h1')).toBeVisible()
  await expect(body).toBeVisible()
  await expect.poll(() => toc.getByRole('link').count()).toBeGreaterThan(0)
  await expect(toc).toBeVisible()

  const bodyBox = await body.boundingBox()
  const tocBox = await toc.boundingBox()

  expect(bodyBox).toBeTruthy()
  expect(tocBox).toBeTruthy()
  expect(bodyBox!.x + bodyBox!.width).toBeLessThanOrEqual(tocBox!.x - 16)
})
