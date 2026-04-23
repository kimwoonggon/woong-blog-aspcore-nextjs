import { expect, test } from './helpers/performance-test'

test('work detail exposes a desktop table of contents that anchors into the article body', async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 900 })
  await page.goto('/works/seeded-work')

  const contentLayout = page.getByTestId('work-article-content-layout')
  const toc = page.getByTestId('blog-toc')
  await expect(toc).toBeVisible()

  const firstLink = toc.getByRole('link').first()
  const targetId = (await firstLink.getAttribute('href'))?.replace(/^#/, '')

  expect(targetId).toBeTruthy()

  await firstLink.click()
  await expect(page).toHaveURL(new RegExp(`#${targetId}$`))
  await expect(page.locator(`#${targetId}`)).toBeVisible()

  const tocBox = await toc.boundingBox()
  const contentLayoutBox = await contentLayout.boundingBox()
  expect(tocBox).toBeTruthy()
  expect(contentLayoutBox).toBeTruthy()
  expect(tocBox!.y + tocBox!.height).toBeLessThanOrEqual(contentLayoutBox!.y + contentLayoutBox!.height + 2)
})
