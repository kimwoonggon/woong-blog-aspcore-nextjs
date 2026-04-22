import { expect, test } from './helpers/performance-test'
import { measureStep } from './helpers/latency'

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })


test('admin can create and publish a work that appears on public works page', async ({ page }, testInfo) => {
  const title = `Playwright Work ${Date.now()}`

  await page.goto('/admin/works/new')
  await expect(page).toHaveURL(/\/admin\/works\/new/)
  await expect(page.getByLabel('Category')).toHaveValue('Uncategorized')
  await expect(page.getByLabel('Title')).toBeVisible()
  await page.evaluate((titleValue) => {
    const setInputValue = (selector: string, value: string) => {
      const element = document.querySelector(selector) as HTMLInputElement | null
      if (!element) throw new Error(`Missing input: ${selector}`)
      const prototype = Object.getPrototypeOf(element)
      const descriptor = Object.getOwnPropertyDescriptor(prototype, 'value')
      descriptor?.set?.call(element, value)
      element.dispatchEvent(new Event('input', { bubbles: true }))
      element.dispatchEvent(new Event('change', { bubbles: true }))
    }

    setInputValue('input[name="title"]', titleValue)
    setInputValue('input[name="period"]', '2026.03 - 2026.03')
    setInputValue('input[name="tags"]', 'playwright, works')
  }, title)

  await expect(page.locator('input[name="title"]')).toHaveValue(title)
  await expect(page.locator('input[name="category"]')).toHaveValue('Uncategorized')
  await expect(page.locator('input[name="period"]')).toHaveValue('2026.03 - 2026.03')
  await expect(page.locator('input[name="tags"]')).toHaveValue('playwright, works')
  await expect(page.getByText('New works go live immediately. Staged videos attach automatically after creation.')).toBeVisible()

  await page.locator('.tiptap.ProseMirror').first().fill(`This is a browser-driven published work for ${title}.`)

  await expect(page.getByRole('button', { name: /Create Work/i })).toBeEnabled()
  await measureStep(
    testInfo,
    'Admin work create to public detail refresh',
    'adminMutationPublicRefresh',
    async () => {
      const [saveResponse] = await Promise.all([
        page.waitForResponse((res) => res.url().includes('/api/admin/works') && res.request().method() === 'POST' && res.ok()),
        page.waitForResponse((res) => res.url().includes('/revalidate-public') && res.request().method() === 'POST' && res.ok()),
        page.getByRole('button', { name: /Create Work/i }).click(),
      ])

      return await saveResponse.json() as { slug: string }
    },
    async (payload) => {
      await expect(page).toHaveURL(/\/admin\/works(?:\?.*)?$/, { timeout: 20000 })
      await expect(page.getByRole('link', { name: title }).first()).toBeVisible()
      await page.goto(`/works/${payload.slug}`)
      await expect(page.locator('main h1', { hasText: title })).toBeVisible()
      await expect(page.getByText('Uncategorized').first()).toBeVisible()
    },
  )
  await page.screenshot({ path: 'test-results/playwright/admin-work-publish.png', fullPage: true })
})

test('admin can keep a work as draft and publish it later', async ({ page }) => {
  const title = `Playwright Draft Work ${Date.now()}`

  await page.goto('/admin/works/new')
  await page.getByLabel('Title').fill(title)
  await page.getByLabel('Category').fill('draft')
  await page.getByRole('checkbox', { name: 'Published' }).uncheck()
  await page.locator('.tiptap.ProseMirror').first().fill(`This work starts as a draft for ${title}.`)

  const [createResponse] = await Promise.all([
    page.waitForResponse((res) => new URL(res.url()).pathname === '/api/admin/works' && res.request().method() === 'POST' && res.ok()),
    page.getByRole('button', { name: /Create Work/i }).click(),
  ])

  const created = await createResponse.json() as { id: string; slug: string }
  await expect(page).toHaveURL(/\/admin\/works(?:\?.*)?$/)

  await page.getByLabel('Search work titles').fill(title)
  const row = page.getByTestId('admin-work-row').filter({ hasText: title }).first()
  await expect(row).toBeVisible()
  await expect(row.locator('[data-slot="badge"]').filter({ hasText: /^Draft$/ }).first()).toBeVisible()

  await page.goto(`/works/${created.slug}`)
  await expect(page.getByRole('heading', { name: title })).toHaveCount(0)
  await expect(page.getByRole('heading', { name: '404 - Page Not Found' })).toBeVisible()

  await page.goto(`/admin/works/${created.id}`)
  const publishedCheckbox = page.getByRole('checkbox', { name: 'Published' })
  await expect(publishedCheckbox).not.toBeChecked()
  await publishedCheckbox.check()

  await Promise.all([
    page.waitForResponse((res) => new URL(res.url()).pathname === `/api/admin/works/${created.id}` && res.request().method() === 'PUT' && res.ok()),
    page.getByRole('button', { name: 'Update Work' }).click(),
  ])

  await expect(page).toHaveURL(/\/admin\/works(?:\?.*)?$/)
  await page.getByLabel('Search work titles').fill(title)
  const publishedRow = page.getByTestId('admin-work-row').filter({ hasText: title }).first()
  await expect(publishedRow).toBeVisible()
  await expect(publishedRow.locator('[data-slot="badge"]').filter({ hasText: /^Published$/ }).first()).toBeVisible()

  await page.goto(`/works/${created.slug}`)
  await expect(page.locator('main h1', { hasText: title })).toBeVisible()
})

test('admin can toggle a published work back to draft and publish it again', async ({ page }) => {
  const title = `Playwright Toggle Work ${Date.now()}`

  await page.goto('/admin/works/new')
  await page.getByLabel('Title').fill(title)
  await page.getByLabel('Category').fill('toggle')
  await page.locator('.tiptap.ProseMirror').first().fill(`This work exercises publish toggles for ${title}.`)

  const [createResponse] = await Promise.all([
    page.waitForResponse((res) => new URL(res.url()).pathname === '/api/admin/works' && res.request().method() === 'POST' && res.ok()),
    page.getByRole('button', { name: /Create Work/i }).click(),
  ])

  const created = await createResponse.json() as { id: string; slug: string }

  await page.goto(`/works/${created.slug}`)
  await expect(page.locator('main h1', { hasText: title })).toBeVisible()

  await page.goto(`/admin/works/${created.id}`)
  const publishedCheckbox = page.getByRole('checkbox', { name: 'Published' })
  await expect(publishedCheckbox).toBeChecked()
  await publishedCheckbox.uncheck()

  await Promise.all([
    page.waitForResponse((res) => new URL(res.url()).pathname === `/api/admin/works/${created.id}` && res.request().method() === 'PUT' && res.ok()),
    page.getByRole('button', { name: 'Update Work' }).click(),
  ])

  await expect(page).toHaveURL(/\/admin\/works(?:\?.*)?$/)
  await page.getByLabel('Search work titles').fill(title)
  const draftRow = page.getByTestId('admin-work-row').filter({ hasText: title }).first()
  await expect(draftRow).toBeVisible()
  await expect(draftRow.locator('[data-slot="badge"]').filter({ hasText: /^Draft$/ }).first()).toBeVisible()

  await page.goto(`/works/${created.slug}`)
  await expect(page.getByRole('heading', { name: title })).toHaveCount(0)
  await expect(page.getByRole('heading', { name: '404 - Page Not Found' })).toBeVisible()

  await page.goto(`/admin/works/${created.id}`)
  await page.getByRole('checkbox', { name: 'Published' }).check()

  await Promise.all([
    page.waitForResponse((res) => new URL(res.url()).pathname === `/api/admin/works/${created.id}` && res.request().method() === 'PUT' && res.ok()),
    page.getByRole('button', { name: 'Update Work' }).click(),
  ])

  await expect(page).toHaveURL(/\/admin\/works(?:\?.*)?$/)
  await page.getByLabel('Search work titles').fill(title)
  const publishedRow = page.getByTestId('admin-work-row').filter({ hasText: title }).first()
  await expect(publishedRow).toBeVisible()
  await expect(publishedRow.locator('[data-slot="badge"]').filter({ hasText: /^Published$/ }).first()).toBeVisible()

  await page.goto(`/works/${created.slug}`)
  await expect(page.locator('main h1', { hasText: title })).toBeVisible()
})
