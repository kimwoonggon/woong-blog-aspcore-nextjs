import { expect, test } from '@playwright/test'

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })

test('login route renders admin login shell', async ({ page }) => {
  await page.goto('/login')
  await expect(page.getByRole('heading', { name: 'Admin Login' })).toBeVisible()
  await expect(page.getByRole('button', { name: 'Sign in with Google' })).toBeVisible()
})

test('login route shows local admin shortcut', async ({ page }) => {
  await page.goto('/login')
  await expect(page.getByRole('button', { name: 'Continue as Local Admin' })).toBeVisible()
})

test('admin dashboard route renders heading', async ({ page }) => {
  await page.goto('/admin/dashboard')
  await expect(page.getByRole('heading', { name: 'Dashboard' })).toBeVisible()
})

test('admin blog route renders heading', async ({ page }) => {
  await page.goto('/admin/blog')
  await expect(page.getByRole('heading', { name: 'Blog Posts' })).toBeVisible()
})

test('admin blog new route renders heading', async ({ page }) => {
  await page.goto('/admin/blog/new')
  await expect(page.getByRole('heading', { name: 'New Post' })).toBeVisible()
})

test('admin blog notion route renders heading', async ({ page }) => {
  await page.goto('/admin/blog/notion')
  await expect(page.getByRole('heading', { name: 'Blog Notion View' }).first()).toBeVisible()
})

test('admin works route renders heading', async ({ page }) => {
  await page.goto('/admin/works')
  await expect(page.getByRole('heading', { name: 'Works' })).toBeVisible()
})

test('admin works new route renders heading', async ({ page }) => {
  await page.goto('/admin/works/new')
  await expect(page.getByRole('heading', { name: 'New Work' })).toBeVisible()
})

test('admin members route renders heading', async ({ page }) => {
  await page.goto('/admin/members')
  await expect(page.getByRole('heading', { name: 'Members' })).toBeVisible()
})
