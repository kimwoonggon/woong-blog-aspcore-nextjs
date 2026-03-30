import { expect, test } from '@playwright/test'

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })

test('admin dashboard shows Open Site quick action', async ({ page }) => {
  await page.goto('/admin/dashboard')
  await expect(page.getByText('Open Site').first()).toBeVisible()
})

test('admin dashboard shows Members quick action', async ({ page }) => {
  await page.goto('/admin/dashboard')
  await expect(page.getByText('Members').first()).toBeVisible()
})

test('admin dashboard shows Blog Notion View quick action', async ({ page }) => {
  await page.goto('/admin/dashboard')
  await expect(page.getByText('Blog Notion View').first()).toBeVisible()
})

test('admin blog list shows search box', async ({ page }) => {
  await page.goto('/admin/blog')
  await expect(page.getByPlaceholder('Search blog titles')).toBeVisible()
})

test('admin blog list shows Batch AI Fix after selecting rows', async ({ page }) => {
  await page.goto('/admin/blog')
  const checkboxes = page.getByTestId('admin-blog-row').getByRole('checkbox')
  await checkboxes.nth(0).click()
  await expect(page.getByRole('button', { name: 'Batch AI Fix' })).toBeVisible()
})

test('admin blog list shows Delete Selected after selecting rows', async ({ page }) => {
  await page.goto('/admin/blog')
  const checkboxes = page.getByTestId('admin-blog-row').getByRole('checkbox')
  await checkboxes.nth(0).click()
  await expect(page.getByRole('button', { name: 'Delete Selected' })).toBeVisible()
})

test('admin works list shows search box', async ({ page }) => {
  await page.goto('/admin/works')
  await expect(page.getByPlaceholder('Search work titles')).toBeVisible()
})

test('admin works list shows Delete Selected after selecting rows', async ({ page }) => {
  await page.goto('/admin/works')
  const checkboxes = page.getByTestId('admin-work-row').getByRole('checkbox')
  await checkboxes.nth(0).click()
  await expect(page.getByRole('button', { name: 'Delete Selected' })).toBeVisible()
})

test('admin members table shows Member header', async ({ page }) => {
  await page.goto('/admin/members')
  await expect(page.getByRole('columnheader', { name: 'Member' })).toBeVisible()
})

test('admin members table shows Active Sessions header', async ({ page }) => {
  await page.goto('/admin/members')
  await expect(page.getByRole('columnheader', { name: 'Active Sessions' })).toBeVisible()
})

test('admin work new page shows save guidance copy', async ({ page }) => {
  await page.goto('/admin/works/new')
  await expect(page.getByText('New works publish immediately when you save.')).toBeVisible()
})

test('admin blog new page shows publish guidance copy', async ({ page }) => {
  await page.goto('/admin/blog/new')
  await expect(page.getByText('New posts publish immediately when you save.')).toBeVisible()
})
