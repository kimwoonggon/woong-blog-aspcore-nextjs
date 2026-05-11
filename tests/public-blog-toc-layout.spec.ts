import { expect, test } from './helpers/performance-test'
import { createBlogFixture } from './helpers/content-fixtures'

test('blog table of contents stays in a right rail instead of covering article content', async ({ page, request }, testInfo) => {
  const longHeading = 'Fixture Section With A Long Title That Should Still Have Enough Reading Width'
  const blog = await createBlogFixture(request, testInfo, {
    titlePrefix: 'TOC Layout Blog',
    html: `<h1>Body H1 Fixture</h1><p>Intro</p><h2>Fixture Section One</h2><p>Body</p><h2>${longHeading}</h2>`,
    tags: ['toc-layout', 'blog'],
  })

  await page.setViewportSize({ width: 1600, height: 900 })
  await page.goto(`/blog/${blog.slug}`)

  const body = page.getByTestId('blog-detail-body')
  const toc = page.getByTestId('blog-toc')
  const contentLayout = page.getByTestId('blog-article-content-layout')

  await expect(page.getByTestId('blog-detail-title')).toHaveText(blog.title)
  await expect(body).toBeVisible()
  await expect.poll(() => toc.getByRole('link').count()).toBeGreaterThan(0)
  await expect(toc).toBeVisible()

  const bodyBox = await body.boundingBox()
  const tocBox = await toc.boundingBox()
  const contentLayoutBox = await contentLayout.boundingBox()

  expect(bodyBox).toBeTruthy()
  expect(tocBox).toBeTruthy()
  expect(contentLayoutBox).toBeTruthy()
  expect(bodyBox!.x + bodyBox!.width).toBeLessThanOrEqual(tocBox!.x - 24)
  expect(tocBox!.y + tocBox!.height).toBeLessThanOrEqual(contentLayoutBox!.y + contentLayoutBox!.height + 2)
  expect(tocBox!.width).toBeGreaterThanOrEqual(360)
  expect(tocBox!.x + tocBox!.width).toBeLessThanOrEqual(page.viewportSize()!.width)

  const titleBox = await toc.getByText('On This Page').boundingBox()
  const collapseButton = toc.getByRole('button', { name: 'Collapse' })
  const collapseButtonBox = await collapseButton.boundingBox()
  expect(titleBox).toBeTruthy()
  expect(collapseButtonBox).toBeTruthy()
  expect(titleBox!.width).toBeGreaterThanOrEqual(248)
  expect(titleBox!.x + titleBox!.width).toBeLessThanOrEqual(collapseButtonBox!.x - 8)

  const longHeadingLinkBox = await toc.getByRole('link', { name: longHeading }).boundingBox()
  expect(longHeadingLinkBox).toBeTruthy()
  expect(longHeadingLinkBox!.width).toBeGreaterThanOrEqual(336)

  await collapseButton.click()
  await expect(toc.getByRole('button', { name: 'Expand' })).toBeVisible()
  await expect(toc.getByTestId('blog-toc-collapsed-summary')).toContainText('sections hidden')
  expect((await toc.boundingBox())!.width).toBeGreaterThanOrEqual(360)
})
