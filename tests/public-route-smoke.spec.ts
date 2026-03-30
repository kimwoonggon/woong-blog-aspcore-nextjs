import { expect, test } from '@playwright/test'

test('public home exposes core navigation links', async ({ page }) => {
  await page.goto('/')
  await expect(page.getByRole('link', { name: 'Works' })).toBeVisible()
  await expect(page.getByRole('link', { name: 'Blog' })).toBeVisible()
  await expect(page.getByRole('link', { name: 'Resume' })).toBeVisible()
})

test('public introduction route renders heading', async ({ page }) => {
  await page.goto('/introduction')
  await expect(page.getByRole('heading', { name: 'Introduction' })).toBeVisible()
})

test('public contact route renders heading', async ({ page }) => {
  await page.goto('/contact')
  await expect(page.getByRole('heading', { name: 'Contact' })).toBeVisible()
})

test('public resume route renders heading', async ({ page }) => {
  await page.goto('/resume')
  await expect(page.getByRole('heading', { name: 'Resume', exact: true })).toBeVisible()
})

test('public blog listing route renders heading', async ({ page }) => {
  await page.goto('/blog')
  await expect(page.getByRole('heading', { name: 'Blog', exact: true })).toBeVisible()
})

test('public works listing route renders heading', async ({ page }) => {
  await page.goto('/works')
  await expect(page.getByRole('heading', { name: 'Works', exact: true })).toBeVisible()
})

test('public blog detail route renders seeded title', async ({ page }) => {
  await page.goto('/blog/seeded-blog')
  await expect(page.getByRole('heading', { name: 'Designing a Seed-First Migration Strategy' })).toBeVisible()
})

test('public work detail route renders seeded title', async ({ page }) => {
  await page.goto('/works/seeded-work')
  await expect(page.getByRole('heading', { name: 'Portfolio Platform Rebuild' })).toBeVisible()
})
