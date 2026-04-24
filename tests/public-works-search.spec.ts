import { expect, test } from './helpers/performance-test'
import { measureStep } from './helpers/latency'

test('public study search submits query without searchMode', async ({ page }, testInfo) => {
  await page.goto('/blog')
  await expect.poll(() => new URL(page.url()).searchParams.get('pageSize')).not.toBeNull()

  const studySearchForm = page.getByRole('search')
  const studySearchInput = studySearchForm.getByRole('textbox', { name: 'Search studies' })
  await studySearchInput.fill('seeded')
  await expect(studySearchInput).toHaveValue('seeded')

  await measureStep(
    testInfo,
    'Study unified search submit',
    'publicSearch',
    async () => {
      await studySearchForm.getByRole('button', { name: 'Search studies' }).click()
    },
    async () => {
      await expect.poll(() => new URL(page.url()).searchParams.get('query')).toBe('seeded')
      await expect.poll(() => new URL(page.url()).searchParams.get('searchMode')).toBeNull()
      await expect(studySearchInput).toHaveValue('seeded')
    },
  )
})

test('public works search submits query without searchMode', async ({ page }, testInfo) => {
  await page.goto('/works')
  await expect.poll(() => new URL(page.url()).searchParams.get('pageSize')).not.toBeNull()

  const worksSearchForm = page.getByRole('search')
  const worksSearchInput = worksSearchForm.getByRole('textbox', { name: 'Search work' })
  await worksSearchInput.fill('Portfolio Platform')
  await expect(worksSearchInput).toHaveValue('Portfolio Platform')

  await measureStep(
    testInfo,
    'Works unified search submit',
    'publicSearch',
    async () => {
      await worksSearchForm.getByRole('button', { name: 'Search works' }).click()
    },
    async () => {
      await expect.poll(() => new URL(page.url()).searchParams.get('query')).toBe('Portfolio Platform')
      await expect.poll(() => new URL(page.url()).searchParams.get('searchMode')).toBeNull()
      await expect(worksSearchInput).toHaveValue('Portfolio Platform')
      await expect(page.getByTestId('work-card').first()).toContainText('Portfolio Platform Rebuild')
    },
  )
})
