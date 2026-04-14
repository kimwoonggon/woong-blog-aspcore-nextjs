import { expect, test } from '@playwright/test'

test('blog table of contents stays in a right rail instead of covering article content', async ({ page }) => {
  await page.setViewportSize({ width: 1600, height: 900 })
  await page.goto('/blog')

  const firstPost = page.getByTestId('blog-card').first()
  await firstPost.click()

  const body = page.getByTestId('blog-detail-body')
  const toc = page.getByTestId('blog-toc')

  await expect(body).toBeVisible()
  await expect(toc).toBeVisible()

  const bodyBox = await body.boundingBox()
  const tocBox = await toc.boundingBox()

  expect(bodyBox).toBeTruthy()
  expect(tocBox).toBeTruthy()
  expect(bodyBox!.x + bodyBox!.width).toBeLessThanOrEqual(tocBox!.x - 16)
})
