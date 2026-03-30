import { expect, test } from '@playwright/test'

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })

test('admin pages route renders main heading', async ({ page }) => {
  await page.goto('/admin/pages')
  await expect(page.getByRole('heading', { name: 'Pages & Settings' })).toBeVisible()
})

test('admin pages route exposes quick link for site settings', async ({ page }) => {
  await page.goto('/admin/pages')
  await expect(page.getByRole('link', { name: 'Edit site settings' })).toBeVisible()
})

test('admin pages route exposes quick link for home page', async ({ page }) => {
  await page.goto('/admin/pages')
  await expect(page.getByRole('link', { name: 'Edit home page' })).toBeVisible()
})

test('admin pages route exposes quick link for introduction page', async ({ page }) => {
  await page.goto('/admin/pages')
  await expect(page.getByRole('link', { name: 'Edit introduction page' })).toBeVisible()
})

test('admin pages route exposes quick link for contact page', async ({ page }) => {
  await page.goto('/admin/pages')
  await expect(page.getByRole('link', { name: 'Edit contact page' })).toBeVisible()
})

test('admin pages route exposes quick link for resume upload', async ({ page }) => {
  await page.goto('/admin/pages')
  await expect(page.getByRole('link', { name: 'Edit resume upload' })).toBeVisible()
})

test('admin pages route renders site settings section heading', async ({ page }) => {
  await page.goto('/admin/pages')
  await expect(page.getByText('Site Settings').first()).toBeVisible()
})

test('admin pages route renders home section heading', async ({ page }) => {
  await page.goto('/admin/pages')
  await expect(page.getByText('Home').first()).toBeVisible()
})

test('admin pages route renders introduction section heading', async ({ page }) => {
  await page.goto('/admin/pages')
  await expect(page.getByText('Introduction').first()).toBeVisible()
})

test('admin pages route renders contact section heading', async ({ page }) => {
  await page.goto('/admin/pages')
  await expect(page.getByText('Contact').first()).toBeVisible()
})

test('admin pages route renders resume section heading', async ({ page }) => {
  await page.goto('/admin/pages')
  await expect(page.getByText('Resume').first()).toBeVisible()
})
