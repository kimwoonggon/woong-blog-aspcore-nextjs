import { expect, test } from '@playwright/test'

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })

test('admin blog page supports title search and compact pagination controls', async ({ page }) => {
  await page.goto('/admin/blog')

  const rows = page.getByTestId('admin-blog-row')
  await expect(rows.first()).toBeVisible()

  const titles = await rows.locator('td:nth-child(2) a').allTextContents()
  const searchTitle = titles.find(Boolean)?.trim() ?? ''
  const hiddenTitle = titles.find((title) => title.trim() && title.trim() !== searchTitle)?.trim() ?? ''

  await page.getByLabel('Search blog titles').fill(searchTitle)
  await expect(rows.locator('td:nth-child(2) a', { hasText: searchTitle }).first()).toBeVisible()
  if (hiddenTitle) {
    await expect(page.getByRole('link', { name: hiddenTitle })).toHaveCount(0)
  }

  await page.getByLabel('Search blog titles').clear()

  const counter = page.getByText(/^Page \d+ of \d+$/).first()
  const initialCounter = (await counter.textContent())?.trim() ?? 'Page 1 of 1'
  const match = initialCounter.match(/^Page\s+(\d+)\s+of\s+(\d+)$/)
  const initialTotal = Number(match?.[2] ?? '1')

  await expect(page.getByRole('button', { name: 'Previous page' })).toBeVisible()
  await expect(page.getByRole('button', { name: 'Next page' })).toBeVisible()
  await expect(page.getByRole('button', { name: 'First' })).toHaveCount(0)
  await expect(page.getByRole('button', { name: 'Last' })).toHaveCount(0)

  if (initialTotal > 1) {
    await page.getByRole('button', { name: 'Next page' }).click()
    await expect(counter).not.toHaveText(initialCounter)
    await page.getByRole('button', { name: 'Previous page' }).click()
    await expect(counter).toHaveText(/Page 1 of \d+/)
  }
})

test('admin works page supports title search and compact pagination controls', async ({ page }) => {
  await page.goto('/admin/works')

  const rows = page.getByTestId('admin-work-row')
  await expect(rows.first()).toBeVisible()

  const titles = await rows.locator('td:nth-child(3) a').allTextContents()
  const searchTitle = titles.find(Boolean)?.trim() ?? ''
  const hiddenTitle = titles.find((title) => title.trim() && title.trim() !== searchTitle)?.trim() ?? ''

  await page.getByLabel('Search work titles').fill(searchTitle)
  await expect(rows.locator('td:nth-child(3) a', { hasText: searchTitle }).first()).toBeVisible()
  if (hiddenTitle) {
    await expect(page.getByRole('link', { name: hiddenTitle })).toHaveCount(0)
  }

  await page.getByLabel('Search work titles').clear()

  const counter = page.getByText(/^Page \d+ of \d+$/).first()
  const initialCounter = (await counter.textContent())?.trim() ?? 'Page 1 of 1'
  const match = initialCounter.match(/^Page\s+(\d+)\s+of\s+(\d+)$/)
  const initialTotal = Number(match?.[2] ?? '1')

  await expect(page.getByRole('button', { name: 'Previous page' })).toBeVisible()
  await expect(page.getByRole('button', { name: 'Next page' })).toBeVisible()
  await expect(page.getByRole('button', { name: 'First' })).toHaveCount(0)
  await expect(page.getByRole('button', { name: 'Last' })).toHaveCount(0)

  if (initialTotal > 1) {
    await page.getByRole('button', { name: 'Next page' }).click()
    await expect(counter).not.toHaveText(initialCounter)
    await page.getByRole('button', { name: 'Previous page' }).click()
    await expect(counter).toHaveText(/Page 1 of \d+/)
  }
})

test('admin dashboard supports independent title search for works and blog collections', async ({ page }) => {
  await page.goto('/admin/dashboard')

  const worksSection = page.locator('section').filter({ has: page.getByRole('heading', { name: 'Works' }) }).first()
  const blogsSection = page.locator('section').filter({ has: page.getByRole('heading', { name: 'Blog Posts' }) }).first()

  const workCards = worksSection.locator('[data-testid="works-card-link"] h3')
  const blogCards = blogsSection.locator('[data-testid="blog-posts-card-link"] h3')

  await expect(workCards.first()).toBeVisible()
  await expect(blogCards.first()).toBeVisible()

  const workTitles = await workCards.allTextContents()
  const searchWorkTitle = workTitles.find(Boolean)?.trim() ?? ''
  const hiddenWorkTitle = workTitles.find((title) => title.trim() && title.trim() !== searchWorkTitle)?.trim() ?? ''
  await worksSection.getByLabel('Works title search').fill(searchWorkTitle)
  await expect(worksSection.getByRole('heading', { name: searchWorkTitle })).toBeVisible()
  if (hiddenWorkTitle) {
    await expect(worksSection.getByRole('heading', { name: hiddenWorkTitle })).toHaveCount(0)
  }

  const blogTitles = await blogCards.allTextContents()
  const searchBlogTitle = blogTitles.find(Boolean)?.trim() ?? ''
  const hiddenBlogTitle = blogTitles.find((title) => title.trim() && title.trim() !== searchBlogTitle)?.trim() ?? ''
  await blogsSection.getByLabel('Blog Posts title search').fill(searchBlogTitle)
  await expect(blogsSection.getByRole('heading', { name: searchBlogTitle })).toBeVisible()
  if (hiddenBlogTitle) {
    await expect(blogsSection.getByRole('heading', { name: hiddenBlogTitle })).toHaveCount(0)
  }
})
