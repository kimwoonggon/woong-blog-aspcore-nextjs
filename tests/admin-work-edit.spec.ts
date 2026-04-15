import { expect, test } from '@playwright/test'

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })


test('admin can edit an existing work entry with mixed special input', async ({ page }) => {
  const updatedTitle = `수정된 작업! ${Date.now()} ###`
  const updatedBody = `작업 본문 한국어 + English + !!! ${Date.now()}`

  await page.goto('/admin/works')
  const editHref = await page
    .getByTestId('admin-work-row')
    .first()
    .locator('td')
    .nth(2)
    .getByRole('link')
    .getAttribute('href')
  expect(editHref).toMatch(/\/admin\/works\//)
  await page.goto(editHref!)
  await expect(page).toHaveURL(/\/admin\/works\//)
  await expect(page.getByLabel('Title')).toBeVisible()

  await page.getByLabel('Title').fill(updatedTitle)
  await page.locator('.tiptap.ProseMirror').first().fill(updatedBody)

  const [response] = await Promise.all([
    page.waitForResponse((res) => res.url().includes('/api/admin/works/') && res.request().method() === 'PUT' && res.ok()),
    page.getByRole('button', { name: 'Update Work' }).click(),
  ])

  const payload = await response.json()
  await expect(page).toHaveURL(/\/admin\/works(?:\?.*)?$/)
  await expect(page.getByText(updatedTitle)).toBeVisible()
  await page.goto(`/works/${payload.slug}`)
  await expect(page.getByRole('heading', { name: updatedTitle })).toBeVisible()
  await expect(page.getByText(updatedBody).first()).toBeVisible()
})
