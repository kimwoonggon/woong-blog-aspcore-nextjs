import { expect, test, type Page } from './helpers/performance-test'

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })

async function expectDeleteDialogWithoutBrowserPrompt(page: Page, path: string, rowTestId: string) {
  let browserDialogTriggered = false
  page.on('dialog', async (dialog) => {
    browserDialogTriggered = true
    await dialog.dismiss()
  })

  await page.goto(path)

  const rows = page.getByTestId(rowTestId)
  test.skip((await rows.count()) === 0, `No rows available at ${path}`)

  await rows.first().getByRole('button', { name: 'Delete' }).click()

  const dialog = page.getByRole('dialog')
  await expect(dialog).toBeVisible()
  await expect(dialog.getByRole('button', { name: 'Cancel' })).toBeVisible()
  await expect(dialog.getByRole('button', { name: 'Delete' })).toBeVisible()
  await page.waitForTimeout(100)
  expect(browserDialogTriggered).toBeFalsy()

  await dialog.getByRole('button', { name: 'Cancel' }).click()
  await expect(dialog).not.toBeVisible()
}

test('blog delete uses an in-app dialog instead of browser confirm', async ({ page }) => {
  await expectDeleteDialogWithoutBrowserPrompt(page, '/admin/blog', 'admin-blog-row')
})

test('works delete uses an in-app dialog instead of browser confirm', async ({ page }) => {
  await expectDeleteDialogWithoutBrowserPrompt(page, '/admin/works', 'admin-work-row')
})
