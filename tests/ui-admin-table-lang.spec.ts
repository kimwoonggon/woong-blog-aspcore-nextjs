import { expect, test, type Page } from '@playwright/test'

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })

async function expectEnglishTableChrome(page: Page, path: string, searchLabel: string) {
  await page.goto(path)

  await expect(page.getByLabel(searchLabel)).toHaveAttribute('placeholder', /Search by/i)
  await expect(page.getByText('First', { exact: true })).toBeVisible()
  await expect(page.getByText('Previous', { exact: true })).toBeVisible()
  await expect(page.getByText('Next', { exact: true })).toBeVisible()
  await expect(page.getByText('Last', { exact: true })).toBeVisible()

  const paginationText = await page
    .locator('div.flex.flex-wrap.items-center.justify-center.gap-2')
    .last()
    .innerText()

  expect(paginationText).not.toMatch(/[가-힣]/)
}

test('admin blog table chrome uses English labels', async ({ page }) => {
  await expectEnglishTableChrome(page, '/admin/blog', 'Search blog titles')
})

test('admin works table chrome uses English labels', async ({ page }) => {
  await expectEnglishTableChrome(page, '/admin/works', 'Search work titles')
})
