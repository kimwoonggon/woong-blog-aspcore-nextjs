import { expect, test } from './helpers/performance-test'
import { measureStep } from './helpers/latency'

test('public study search submits query without searchMode', async ({ page }, testInfo) => {
  await page.goto('/blog')
  await page.getByRole('textbox', { name: 'Search studies' }).fill('seeded')

  await measureStep(
    testInfo,
    'Study unified search submit',
    'publicSearch',
    async () => {
      await page.getByRole('button', { name: 'Search studies' }).click()
    },
    async () => {
      await expect.poll(() => new URL(page.url()).searchParams.get('query')).toBe('seeded')
      await expect.poll(() => new URL(page.url()).searchParams.get('searchMode')).toBeNull()
      await expect(page.getByRole('textbox', { name: 'Search studies' })).toHaveValue('seeded')
    },
  )
})

test('public works search submits query without searchMode', async ({ page }, testInfo) => {
  await page.goto('/works')
  await page.getByRole('textbox', { name: 'Search work' }).fill('Portfolio Platform')

  await measureStep(
    testInfo,
    'Works unified search submit',
    'publicSearch',
    async () => {
      await page.getByRole('button', { name: 'Search works' }).click()
    },
    async () => {
      await expect.poll(() => new URL(page.url()).searchParams.get('query')).toBe('Portfolio Platform')
      await expect.poll(() => new URL(page.url()).searchParams.get('searchMode')).toBeNull()
      await expect(page.getByRole('textbox', { name: 'Search work' })).toHaveValue('Portfolio Platform')
      await expect(page.getByTestId('work-card').first()).toContainText('Portfolio Platform Rebuild')
    },
  )
})
