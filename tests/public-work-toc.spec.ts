import { expect, test } from './helpers/performance-test'
import { createWorkFixture } from './helpers/content-fixtures'

const tocFixtureHtml = [
  '<h1>Work Body H1 Fixture</h1>',
  '<p>Opening body copy for the work fixture.</p>',
  '<h2>Work Fixture Section One</h2>',
  ...Array.from({ length: 8 }, (_, index) => `<p>Work section one filler ${index + 1} keeps scroll positions stable.</p>`),
  '<h2>Work Fixture Section With A Long Title That Should Still Have Enough Reading Width</h2>',
  '<p>Work section two body copy.</p>',
  '<h3>Work Fixture Nested Section</h3>',
].join('')

test('work detail exposes a desktop table of contents that anchors into the article body', async ({ page, request }, testInfo) => {
  const work = await createWorkFixture(request, testInfo, {
    titlePrefix: 'TOC Work',
    html: tocFixtureHtml,
    tags: ['toc-active', 'work'],
    allPropertiesJson: '{}',
  })

  await page.setViewportSize({ width: 1600, height: 900 })
  await page.goto(`/works/${work.slug}`)

  const contentLayout = page.getByTestId('work-article-content-layout')
  const toc = page.getByTestId('work-toc')
  const rail = page.getByTestId('work-toc-rail')
  await expect(page.getByTestId('work-detail-title')).toHaveText(work.title)
  await expect(page.locator('#work-detail-content h1')).toHaveText('Work Body H1 Fixture')
  await expect(toc).toBeVisible()
  await expect(toc).toContainText('On This Work')
  await expect(toc).not.toHaveClass(/overflow-y-auto/)

  const firstLink = toc.getByRole('link').first()
  const targetId = (await firstLink.getAttribute('href'))?.replace(/^#/, '')

  expect(targetId).toBeTruthy()

  await firstLink.click()
  await expect(page).toHaveURL(new RegExp(`#${targetId}$`))
  await expect(page.locator(`#${targetId}`)).toBeVisible()
  await expect(rail).toHaveAttribute('data-range-state', 'active')

  const tocBox = await rail.boundingBox()
  const bodyBox = await page.getByTestId('work-detail-body').boundingBox()
  const contentLayoutBox = await contentLayout.boundingBox()
  expect(tocBox).toBeTruthy()
  expect(bodyBox).toBeTruthy()
  expect(contentLayoutBox).toBeTruthy()
  const bodyCenter = bodyBox!.x + bodyBox!.width / 2
  expect(Math.abs(bodyCenter - page.viewportSize()!.width / 2)).toBeLessThanOrEqual(2)
  expect(tocBox!.y + tocBox!.height).toBeLessThanOrEqual(contentLayoutBox!.y + contentLayoutBox!.height + 2)
  expect(tocBox!.width).toBeGreaterThanOrEqual(360)
  expect(tocBox!.x + tocBox!.width).toBeLessThanOrEqual(page.viewportSize()!.width)

  const titleBox = await toc.getByText('On This Work').boundingBox()
  const collapseButton = toc.getByRole('button', { name: 'Collapse' })
  const collapseButtonBox = await collapseButton.boundingBox()
  expect(titleBox).toBeTruthy()
  expect(collapseButtonBox).toBeTruthy()
  expect(titleBox!.width).toBeGreaterThanOrEqual(248)
  expect(titleBox!.x + titleBox!.width).toBeLessThanOrEqual(collapseButtonBox!.x - 8)

  const longHeadingLinkBox = await toc
    .getByRole('link', { name: 'Work Fixture Section With A Long Title That Should Still Have Enough Reading Width' })
    .boundingBox()
  expect(longHeadingLinkBox).toBeTruthy()
  expect(longHeadingLinkBox!.width).toBeGreaterThanOrEqual(336)

  await collapseButton.click()
  await expect(toc.getByRole('button', { name: 'Expand' })).toBeVisible()
  await expect(toc.getByTestId('work-toc-nav-collapsed-summary')).toContainText('sections hidden')
  expect((await toc.boundingBox())!.width).toBeGreaterThanOrEqual(360)

  await page.getByTestId('work-related-shell').scrollIntoViewIfNeeded()
  await expect(rail).toHaveAttribute('data-range-state', 'after')
})
