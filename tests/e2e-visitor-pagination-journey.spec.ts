import { expect, test } from '@playwright/test'

test('E2E-004 visitor can keep a paginated reading path stable across list and detail pages', async ({ page }) => {
  await page.setViewportSize({ width: 1280, height: 960 })
  await page.goto('/blog?page=2&pageSize=2&__qaTagged=1')

  const pagination = page.getByLabel('Blog pagination')
  await expect(pagination).toBeVisible()
  await expect.poll(() => new URL(page.url()).searchParams.get('page')).toBe('2')
  await expect.poll(() => new URL(page.url()).searchParams.get('pageSize')).toBeTruthy()
  const visibleBlogCards = await page.getByTestId('blog-card').count()
  expect(visibleBlogCards).toBeGreaterThan(1)
  await expect(page.getByTestId('blog-card').first()).toContainText(/playwright|seed|migration|qa/i)

  await pagination.getByRole('link', { name: '이전' }).click()
  await expect.poll(() => new URL(page.url()).searchParams.get('page')).toBe('1')
  await expect.poll(() => new URL(page.url()).searchParams.get('pageSize')).toBeTruthy()

  await pagination.getByRole('link', { name: '다음' }).click()
  await expect.poll(() => new URL(page.url()).searchParams.get('page')).toBe('2')
  await expect.poll(() => new URL(page.url()).searchParams.get('pageSize')).toBeTruthy()

  const secondPageCard = page.getByTestId('blog-card').first()
  await expect(secondPageCard).toBeVisible()
  await secondPageCard.click()

  await expect(page).toHaveURL(/\/blog\/.+(?:\?|&)returnTo=%2Fblog%3Fpage%3D2%26pageSize%3D\d+(?:&|%26)relatedPage=2/)
  await expect(page.getByTestId('blog-related-shell')).toBeVisible()
  await expect(page.locator('section').filter({ has: page.getByRole('heading', { name: 'More Posts' }) }).getByText(/^Page 2 of \d+$/)).toBeVisible()
})

test('E2E-004 visitor can continue the same paginated path through the works archive', async ({ page }) => {
  await page.setViewportSize({ width: 1280, height: 960 })
  await page.goto('/works?page=2&pageSize=2')

  const pagination = page.getByLabel('Works pagination')
  await expect(pagination).toBeVisible()
  await expect.poll(() => new URL(page.url()).searchParams.get('page')).toBe('2')
  await expect.poll(() => new URL(page.url()).searchParams.get('pageSize')).toBeTruthy()
  const visibleWorkCards = await page.getByTestId('work-card').count()
  expect(visibleWorkCards).toBeGreaterThan(1)

  await pagination.getByRole('link', { name: '이전' }).click()
  await expect.poll(() => new URL(page.url()).searchParams.get('page')).toBe('1')
  await expect.poll(() => new URL(page.url()).searchParams.get('pageSize')).toBeTruthy()

  await pagination.getByRole('link', { name: '다음' }).click()
  await expect.poll(() => new URL(page.url()).searchParams.get('page')).toBe('2')
  await expect.poll(() => new URL(page.url()).searchParams.get('pageSize')).toBeTruthy()

  await page.getByTestId('work-card').first().click()
  await expect(page).toHaveURL(/\/works\/.+(?:\?|&)returnTo=%2Fworks%3Fpage%3D2%26pageSize%3D\d+/)
  await expect(page.getByTestId('work-related-shell')).toBeVisible()
  await expect(page.locator('section').filter({ has: page.getByRole('heading', { name: 'More Works' }) }).getByText(/^Page 2 of \d+$/)).toBeVisible()
})
