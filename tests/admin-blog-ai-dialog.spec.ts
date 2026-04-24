import { expect, test } from './helpers/performance-test'
import { measureStep } from './helpers/latency'

test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })

test('blog AI fix dialog loads runtime config, applies a fixed draft, and keeps editing local', async ({ page }, testInfo) => {
  await page.route('**/api/admin/ai/runtime-config', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        provider: 'codex',
        availableProviders: ['openai', 'codex'],
        defaultModel: 'gpt-5.4',
        codexModel: 'gpt-5.4',
        codexReasoningEffort: 'medium',
        allowedCodexModels: ['gpt-5.4', 'gpt-5.4-mini'],
        allowedCodexReasoningEfforts: ['low', 'medium', 'high'],
        batchConcurrency: 2,
        batchCompletedRetentionDays: 7,
      }),
    })
  })

  await page.route('**/api/admin/ai/blog-fix', async (route) => {
    if (route.request().method() !== 'POST') {
      await route.fallback()
      return
    }

    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        fixedHtml: '<h2>AI polished body</h2><p>Applied from mocked AI response.</p>',
      }),
    })
  })

  await page.goto('/admin/blog/new')
  await page.getByLabel('Title').fill(`AI Fix Coverage ${Date.now()}`)
  await page.locator('form .tiptap.ProseMirror').first().click()
  await page.keyboard.type('rough draft before ai fix')

  await measureStep(
    testInfo,
    'AI Fix dialog open to provider dropdown ready',
    'aiDialogOpen',
    async () => {
      await Promise.all([
        page.waitForResponse((res) => res.url().includes('/api/admin/ai/runtime-config') && res.request().method() === 'GET' && res.ok()),
        page.getByRole('button', { name: 'AI Content Fixer' }).click(),
      ])
    },
    async () => {
      await expect(page.getByRole('heading', { name: 'AI Content Fixer' })).toBeVisible()
      await expect(page.getByLabel('AI provider')).toBeVisible()
      await expect(page.getByRole('option', { name: 'OPENAI' })).toBeAttached()
      await expect(page.getByRole('option', { name: 'CODEX' })).toBeAttached()
      await expect(page.locator('#codex-model')).toHaveValue('gpt-5.4')
      await expect(page.locator('#codex-reasoning')).toHaveValue('medium')
    },
  )

  await Promise.all([
    page.waitForResponse((res) => res.url().includes('/api/admin/ai/blog-fix') && res.request().method() === 'POST' && res.ok()),
    page.locator('[data-slot="dialog-content"]').getByRole('button', { name: 'Start AI Fix' }).dispatchEvent('click'),
  ])

  const dialogContent = page.locator('[data-slot="dialog-content"]')
  await expect(dialogContent.getByText('AI polished body')).toBeVisible()
  await expect(dialogContent.getByRole('button', { name: 'Apply Changes' })).toBeVisible()

  await dialogContent.getByRole('button', { name: 'Apply Changes' }).dispatchEvent('click')

  await expect(dialogContent).toHaveCount(0)

  await expect(page.locator('form .tiptap.ProseMirror').first()).toContainText('AI polished body')
  await expect(page.locator('form .tiptap.ProseMirror').first()).toContainText('Applied from mocked AI response.')
  await expect(page).toHaveURL(/\/admin\/blog\/new/)
})
