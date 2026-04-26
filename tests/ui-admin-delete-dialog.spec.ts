import { expect, test, type Page } from './helpers/performance-test'
import { createBlogFixture, createWorkFixture } from './helpers/content-fixtures'

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })

async function expectDeleteDialogWithoutBrowserPrompt(page: Page, path: string, rowTestId: string, rowTitle: string) {
  let browserDialogTriggered = false
  let deleteRequests = 0
  page.on('dialog', async (dialog) => {
    browserDialogTriggered = true
    await dialog.dismiss()
  })
  page.on('request', (request) => {
    if (request.method() === 'DELETE' && request.url().includes('/api/admin/')) {
      deleteRequests += 1
    }
  })

  await page.goto(path)
  await page.getByLabel(/Search .* titles/).fill(rowTitle)

  const row = page.getByTestId(rowTestId).filter({ hasText: rowTitle }).first()
  await expect(row).toBeVisible()

  await row.getByRole('button', { name: 'Delete' }).click()

  const dialog = page.getByRole('dialog')
  await expect(dialog).toBeVisible()
  await expect(dialog.getByRole('button', { name: 'Cancel' })).toBeVisible()
  await expect(dialog.getByRole('button', { name: 'Delete' })).toBeVisible()
  await expect(dialog.getByRole('button', { name: 'Delete' })).toHaveAttribute('data-variant', 'destructive')
  expect(deleteRequests).toBe(0)
  await page.waitForTimeout(100)
  expect(browserDialogTriggered).toBeFalsy()

  await dialog.getByRole('button', { name: 'Cancel' }).click()
  await expect(dialog).not.toBeVisible()
  await expect(row).toBeVisible()
  await expect(page.getByText(rowTitle)).toBeVisible()
  expect(deleteRequests).toBe(0)
  await expect(page.getByText(/deleted successfully/i)).toHaveCount(0)
}

test('blog delete uses an in-app dialog instead of browser confirm', async ({ page, request }, testInfo) => {
  const fixture = await createBlogFixture(request, testInfo, {
    titlePrefix: 'Delete Dialog Blog',
    published: false,
  })

  await expectDeleteDialogWithoutBrowserPrompt(page, '/admin/blog', 'admin-blog-row', fixture.title)
})

test('works delete uses an in-app dialog instead of browser confirm', async ({ page, request }, testInfo) => {
  const fixture = await createWorkFixture(request, testInfo, {
    titlePrefix: 'Delete Dialog Work',
    published: false,
  })

  await expectDeleteDialogWithoutBrowserPrompt(page, '/admin/works', 'admin-work-row', fixture.title)
})
