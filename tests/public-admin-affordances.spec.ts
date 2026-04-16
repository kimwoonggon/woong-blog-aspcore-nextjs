import { expect, test } from '@playwright/test'
import { loginAsLocalAdmin } from './helpers/auth'

const expectedShortcut = process.env.PLAYWRIGHT_EXPECT_LOCAL_ADMIN_SHORTCUT ?? 'hidden'

async function gotoAndExpectButton(page: import('@playwright/test').Page, url: string, buttonName: string) {
  await page.goto(url, { waitUntil: 'networkidle' })
  await expect(page.getByRole('button', { name: buttonName })).toBeVisible({ timeout: 15000 })
}

test('login page reflects the expected local admin shortcut policy', async ({ page }) => {
  await page.goto('/login')

  const shortcut = page.getByRole('link', { name: 'Continue as Local Admin' })
  if (expectedShortcut === 'visible') {
    await expect(shortcut).toBeVisible()
  } else {
    await expect(shortcut).toHaveCount(0)
  }
})

test('admin session sees navbar status and public edit affordances', async ({ page }) => {
  await loginAsLocalAdmin(page, '/')

  await expect(page.getByText('Signed in')).toBeVisible()
  await page.getByRole('button', { name: 'Open signed-in menu' }).click()
  await expect(page.getByRole('menuitem', { name: 'My Page' })).toBeVisible()
  await expect(page.getByRole('menuitem', { name: 'Admin Page' })).toBeVisible()
  await expect(page.getByRole('menuitem', { name: 'Logout' })).toBeVisible()

  await gotoAndExpectButton(page, '/introduction', '소개글 수정')
  await gotoAndExpectButton(page, '/contact', '문의글 수정')
  await gotoAndExpectButton(page, '/resume', '이력서 PDF 업로드')
  await gotoAndExpectButton(page, '/works', '새 작업 쓰기')
  await expect(page.getByRole('link', { name: '작업 관리' })).toBeVisible({ timeout: 15000 })

  await gotoAndExpectButton(page, '/blog', '새 글 쓰기')
  await expect(page.getByRole('link', { name: '글 관리' })).toBeVisible({ timeout: 15000 })

  await page.goto('/blog/seeded-blog', { waitUntil: 'networkidle' })
  await expect(page.getByRole('button', { name: '글 수정' })).toBeVisible({ timeout: 15000 })
  await expect(page.getByRole('button', { name: '삭제' })).toBeVisible({ timeout: 15000 })

  await page.goto('/works/seeded-work', { waitUntil: 'networkidle' })
  await expect(page.getByRole('button', { name: '작업 수정' })).toBeVisible({ timeout: 15000 })
  await expect(page.getByRole('button', { name: '삭제' })).toBeVisible({ timeout: 15000 })
})

test('logout from signed-in menu redirects back to the main page', async ({ page }) => {
  await loginAsLocalAdmin(page, '/')

  await page.getByRole('button', { name: 'Open signed-in menu' }).click()
  await page.getByRole('menuitem', { name: 'Logout' }).click()

  await expect(page).toHaveURL(/\/$/)
})

test('unauthenticated visitors do not see public edit or create affordances', async ({ page }) => {
  await page.goto('/works', { waitUntil: 'networkidle' })
  await expect(page.getByRole('button', { name: '새 작업 쓰기' })).toHaveCount(0)
  await expect(page.getByRole('link', { name: '작업 관리' })).toHaveCount(0)

  await page.goto('/blog', { waitUntil: 'networkidle' })
  await expect(page.getByRole('button', { name: '새 글 쓰기' })).toHaveCount(0)
  await expect(page.getByRole('link', { name: '글 관리' })).toHaveCount(0)

  await page.goto('/introduction', { waitUntil: 'networkidle' })
  await expect(page.getByRole('button', { name: '소개글 수정' })).toHaveCount(0)

  await page.goto('/contact', { waitUntil: 'networkidle' })
  await expect(page.getByRole('button', { name: '문의글 수정' })).toHaveCount(0)

  await page.goto('/resume', { waitUntil: 'networkidle' })
  await expect(page.getByRole('button', { name: '이력서 PDF 업로드' })).toHaveCount(0)
})
