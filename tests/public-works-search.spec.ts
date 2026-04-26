import { expect, test } from './helpers/performance-test'
import { measureStep } from './helpers/latency'
import { createWorkFixture } from './helpers/content-fixtures'

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

test('public works search submits query without searchMode', async ({ page, request }, testInfo) => {
  const work = await createWorkFixture(request, testInfo, {
    titlePrefix: 'Searchable Work',
    html: '<p>Searchable work fixture body.</p>',
    category: 'Search Fixture',
    tags: ['search-fixture'],
  })

  await page.goto('/works')
  await expect.poll(() => new URL(page.url()).searchParams.get('pageSize')).not.toBeNull()

  const worksSearchForm = page.getByRole('search')
  const worksSearchInput = worksSearchForm.getByRole('textbox', { name: 'Search work' })
  await worksSearchInput.fill(work.title)
  await expect(worksSearchInput).toHaveValue(work.title)

  await measureStep(
    testInfo,
    'Works unified search submit',
    'publicSearch',
    async () => {
      await worksSearchForm.getByRole('button', { name: 'Search works' }).click()
    },
    async () => {
      await expect.poll(() => new URL(page.url()).searchParams.get('query')).toBe(work.title)
      await expect.poll(() => new URL(page.url()).searchParams.get('searchMode')).toBeNull()
      await expect(worksSearchInput).toHaveValue(work.title)
      await expect(page.getByTestId('work-card').first()).toContainText(work.title)
    },
  )
})
