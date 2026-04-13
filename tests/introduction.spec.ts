import { expect, test } from '@playwright/test'

test('introduction page renders backend-managed content', async ({ page, request }) => {
  const response = await request.get('http://localhost/api/public/pages/introduction')
  expect(response.ok()).toBeTruthy()

  const payload = await response.json()
  const contentJson = JSON.parse(payload.contentJson as string) as { html?: string }
  const expectedSnippet = (contentJson.html ?? '')
    .replace(/<[^>]+>/g, ' ')
    .replace(/\s+/g, ' ')
    .trim()
    .slice(0, 60)

  await page.goto('/introduction')

  const shell = page.getByTestId('static-public-shell')

  await expect(shell.getByText('About the work')).toBeVisible()
  await expect(page.getByRole('heading', { name: 'Introduction' })).toBeVisible()
  await expect(shell.getByText(/A short framing of who I am, what kind of problems I like to solve/i)).toBeVisible()
  await expect(page.getByText(new RegExp(expectedSnippet.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'), 'i'))).toBeVisible()
  await page.screenshot({ path: 'test-results/playwright/introduction-page.png', fullPage: true })
})
