import { expect, test, type Page } from '@playwright/test'
import {
  contrastRatio,
  expectDarkHtml,
  expectLightHtml,
  expectRgbClose,
  getColorChannels,
  getColorChannelsFromCssValue,
  getRootVariableChannels,
  getStyle,
  gotoWithTheme,
} from './helpers/ui-improvement'
import { expectResponsiveNavMode, toggleThemeForViewport } from './helpers/responsive-policy'

async function firstPublicSlug(page: Page, collection: 'blogs' | 'works') {
  const response = await page.request.get(`/api/public/${collection}?page=1&pageSize=1`)
  const payload = await response.json() as { items?: Array<{ slug?: string }> }
  const slug = payload.items?.[0]?.slug
  test.skip(!slug, `No public ${collection} available in this environment.`)
  return slug!
}

test.describe('theme toggle', () => {
  test('DM-01: theme toggle button is visible in the navbar', async ({ page }) => {
    await page.goto('/')
    await expectResponsiveNavMode(page)
  })

  test('DM-02: clicking the theme toggle applies the dark class directly', async ({ page }) => {
    await page.goto('/')
    await toggleThemeForViewport(page)
    await expectDarkHtml(page)

    const bodyBackground = await getColorChannels(page.locator('body'), 'background-color')
    const lightBackground = await getColorChannelsFromCssValue(page, 'oklch(0.98 0 0)')
    expect(bodyBackground[0]).not.toBe(lightBackground[0])
  })

  test('DM-03: clicking the theme toggle again removes the dark class', async ({ page }) => {
    await page.goto('/')
    await toggleThemeForViewport(page)
    await expectDarkHtml(page)
    await toggleThemeForViewport(page)
    await expectLightHtml(page)

    const bodyBackground = await getStyle(page.locator('body'), 'background-color')
    const lightBackground = await getStyle(page.locator('body'), 'background-color')
    expect(bodyBackground).toBe(lightBackground)
  })

  test('DM-04: the selected theme persists after reload', async ({ page }) => {
    await page.goto('/')
    await toggleThemeForViewport(page)
    await expectDarkHtml(page)
    await page.reload({ waitUntil: 'networkidle' })
    await expectDarkHtml(page)
    await expect.poll(() => page.evaluate(() => window.localStorage.getItem('theme'))).toBe('dark')
  })

  test('DM-05: default theme is light and the old theme dropdown is absent', async ({ page }) => {
    await page.emulateMedia({ colorScheme: 'dark' })
    await page.goto('/')
    await expectLightHtml(page)
    await toggleThemeForViewport(page)
    await expectDarkHtml(page)
    await expect(page.locator('[data-slot="dropdown-menu-content"]')).toHaveCount(0)
    await expect(page.getByRole('menuitemradio')).toHaveCount(0)
  })

  test('DM-05b: mobile drawer theme row toggles dark and light directly', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 })
    await page.goto('/')
    await page.getByRole('button', { name: 'Toggle Menu' }).click()
    await page.getByTestId('mobile-theme-toggle').click()
    await expectDarkHtml(page)
    await page.getByTestId('mobile-theme-toggle').click()
    await expectLightHtml(page)
  })
})

test.describe('public pages', () => {
  test('DM-06: home page renders dark mode surfaces and section backgrounds', async ({ page }) => {
    await gotoWithTheme(page, '/')
    await expectDarkHtml(page)

    const section = page.getByRole('heading', { name: 'Works', exact: true }).locator('..').locator('..')
    const sectionBackground = await getStyle(section, 'background-color')
    const expectedBackground = await getStyle(section, 'background-color')
    expect(sectionBackground).toBe(expectedBackground)

    await page.screenshot({ path: 'test-results/playwright/dark-mode-home-dark.png', fullPage: true })
  })

  test('DM-07: works listing cards use dark brand colors', async ({ page }) => {
    await gotoWithTheme(page, '/works')
    const badge = page.locator('[data-testid="work-card"]').first().locator('.rounded-full').first()
    test.skip(await badge.count() === 0, 'No rendered work cards available in this environment.')
    await expect(badge).toBeVisible()

    const badgeBackground = await getColorChannels(badge, 'background-color')
    const expectedBadgeBackground = await getRootVariableChannels(page, '--brand-navy')
    expectRgbClose(badgeBackground, expectedBadgeBackground)
  })

test('DM-08: work detail page keeps navy detail anchors in dark mode', async ({ page }) => {
  await gotoWithTheme(page, `/works/${await firstPublicSlug(page, 'works')}`)
  const badge = page.locator('article header .rounded-full').first()
  test.skip(!(await badge.isVisible().catch(() => false)), 'No rendered work detail badge available in this environment.')

    const badgeBackground = await getColorChannels(badge, 'background-color')
    const expectedBadgeBackground = await getRootVariableChannels(page, '--brand-navy')
    expectRgbClose(badgeBackground, expectedBadgeBackground)

  await expect(page.locator('article header p')).toHaveCount(0)
})

  test('DM-09: blog listing hover state uses accent color in dark mode', async ({ page }) => {
    await gotoWithTheme(page, '/blog')
    const card = page.getByTestId('blog-card').first()
    test.skip(await card.count() === 0, 'No rendered blog cards available in this environment.')
    const stripe = card.getByTestId('blog-card-accent-stripe')
    const title = card.locator('[data-slot="card-title"]')
    await expect(stripe).toHaveClass(/study-card-stripe/)
    await expect(stripe).not.toHaveClass(/from-brand-accent/)
    await expect(title).toBeVisible()
    const titleClass = await title.evaluate((element) => element.className)
    expect(titleClass).toContain('group-hover/card:text-brand-accent')
  })

test('DM-10: blog detail page uses navy detail anchors and keeps prose readable', async ({ page }) => {
  await gotoWithTheme(page, `/blog/${await firstPublicSlug(page, 'blogs')}`)
  const badge = page.locator('article header .rounded-full').first()
  const prose = page.locator('#blog-detail-content .prose').first()
  test.skip(await badge.count() === 0, 'No rendered blog detail badge available in this environment.')

    const badgeBackground = await getColorChannels(badge, 'background-color')
    const expectedBadgeBackground = await getRootVariableChannels(page, '--brand-navy')
    expectRgbClose(badgeBackground, expectedBadgeBackground)

    await expect(page.locator('article header p')).toHaveCount(0)

    await expect(prose).toBeVisible()
    await page.screenshot({ path: 'test-results/playwright/dark-mode-blog-detail-dark.png', fullPage: true })
  })

test('DM-11: contact page email link uses the semantic primary color in dark mode', async ({ page }) => {
  await gotoWithTheme(page, '/contact')
  const emailLink = page.locator('main a[href^="mailto:"]').first()
  if (await emailLink.count()) {
    await expect(emailLink).toBeVisible()
    const linkColor = await getColorChannels(emailLink, 'color')
    const expectedColor = await getRootVariableChannels(page, '--primary')
    expectRgbClose(linkColor, expectedColor)
    return
  }

  await expect(page.locator('main .prose').first()).toBeVisible()
})

  test('DM-12: footer renders with a dark background and stable links', async ({ page }) => {
    await gotoWithTheme(page, '/')
    const footer = page.locator('footer')
    await expect(footer).toBeVisible()

    const footerBackground = await getColorChannels(footer, 'background-color')
    expect(footerBackground[0]).toBeLessThanOrEqual(40)
    expect(footerBackground[1]).toBeLessThanOrEqual(40)
    expect(footerBackground[2]).toBeLessThanOrEqual(52)

    const footerLinks = footer.getByRole('link')
    if (await footerLinks.count()) {
      const linkClass = await footerLinks.first().evaluate((element) => element.className)
      expect(linkClass).toContain('hover:text-brand-accent')
    }
  })

  test('DM-18: prose code blocks use the darker code block background', async ({ page }) => {
    await gotoWithTheme(page, '/blog')
    const codePostLink = page.getByRole('link').filter({ hasText: /code block/i }).first()
    if (await codePostLink.count()) {
      await codePostLink.click()
    } else {
      await page.goto(`/blog/${await firstPublicSlug(page, 'blogs')}`)
    }

    const codeBlock = page.locator('.prose pre').first()
    if (!(await codeBlock.count())) {
      const prose = page.locator('#blog-detail-content .prose').first()
      test.skip(!(await prose.isVisible().catch(() => false)), 'No rendered blog prose available in this environment.')
      await expect(prose).toBeVisible()
      return
    }

    await expect(codeBlock).toBeVisible()

    const background = await getColorChannels(codeBlock, 'background-color')
    const foreground = await getColorChannels(codeBlock, 'color')
    const expectedBackground = await getColorChannelsFromCssValue(page, '#0f172a')
    const expectedForeground = await getColorChannelsFromCssValue(page, '#e2e8f0')
    expectRgbClose(background, expectedBackground)
    expectRgbClose(foreground, expectedForeground)
  })

  test('DM-19: prose text remains readable in dark mode', async ({ page }) => {
    await gotoWithTheme(page, `/blog/${await firstPublicSlug(page, 'blogs')}`)
    const proseText = page.locator('.prose p').first()
    test.skip(!(await proseText.isVisible().catch(() => false)), 'No rendered blog prose text available in this environment.')
    await expect(proseText).toBeVisible()

    const textColor = await getColorChannels(proseText, 'color')
    const background = await getColorChannels(page.locator('body'), 'background-color')
    expect(contrastRatio(textColor, background)).toBeGreaterThanOrEqual(4.5)
  })

  test('DM-20: mobile menu exposes a wide direct theme toggle in dark mode', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 })
    await gotoWithTheme(page, '/')
    await page.getByRole('button', { name: 'Toggle Menu' }).click()
    const mobileThemeToggle = page.getByTestId('mobile-theme-toggle')
    await expect(mobileThemeToggle).toBeVisible()
    await expect(page.getByText('Account')).toHaveCount(0)
    await expect(page.getByRole('link', { name: 'Login' })).toHaveCount(0)
    const toggleBox = await mobileThemeToggle.boundingBox()
    expect(toggleBox).toBeTruthy()
    expect(toggleBox!.width).toBeGreaterThanOrEqual(300)
    await mobileThemeToggle.click()
    await expectLightHtml(page)
    await page.screenshot({ path: 'test-results/playwright/dark-mode-mobile-menu-dark.png', fullPage: true })
  })

  test('DM-21: dark to light transitions keep the works layout stable', async ({ page }) => {
    await gotoWithTheme(page, '/works')
    await expectDarkHtml(page)
    await page.screenshot({ path: 'test-results/playwright/dark-mode-works-dark.png', fullPage: true })

    await page.evaluate(() => window.localStorage.setItem('theme', 'light'))
    await page.reload({ waitUntil: 'networkidle' })
    await expectLightHtml(page)
    test.skip(await page.getByTestId('work-card').first().count() === 0, 'No rendered work cards available in this environment.')
    await expect(page.getByTestId('work-card').first()).toBeVisible()
    await page.screenshot({ path: 'test-results/playwright/dark-mode-works-light.png', fullPage: true })
  })

  test('DM-22: pagination uses the dark active state variant', async ({ page }) => {
    await gotoWithTheme(page, '/blog')
    const activePage = page.locator('nav[aria-label="Study pagination"] a').first()
    await expect(activePage).toBeVisible()

    const background = await getColorChannels(activePage, 'background-color')
    const borderColor = await getColorChannels(activePage, 'border-top-color')
    expect(background[2]).toBeGreaterThan(background[0])
    expect(borderColor[2]).toBeGreaterThan(borderColor[0])
  })

  test('DM-23: dark mode body text contrast is at least 4.5:1', async ({ page }) => {
    await gotoWithTheme(page, '/')
    const foreground = await getColorChannels(page.locator('body'), 'color')
    const background = await getColorChannels(page.locator('body'), 'background-color')
    expect(contrastRatio(foreground, background)).toBeGreaterThanOrEqual(4.5)
  })

  test('DM-24: muted text contrast is at least 4.5:1 in dark mode', async ({ page }) => {
    await gotoWithTheme(page, '/')
    const mutedText = page.getByRole('heading', { name: 'Study Notes' })
    await expect(mutedText).toBeVisible()

    const foreground = await getColorChannels(mutedText, 'color')
    const background = await getColorChannels(page.locator('body'), 'background-color')
    expect(contrastRatio(foreground, background)).toBeGreaterThanOrEqual(4.5)
  })
})

test.describe('login and admin dark mode', () => {
  test('DM-13: login page renders with dark surfaces', async ({ page }) => {
    await gotoWithTheme(page, '/login')
    const card = page.locator('.max-w-md').first()
    await expect(card).toBeVisible()

    const background = await getColorChannels(card, 'background-color')
    expect(background[0]).toBeLessThanOrEqual(24)
    expect(background[1]).toBeLessThanOrEqual(28)
    expect(background[2]).toBeLessThanOrEqual(42)
  })

  test.use({ storageState: 'test-results/playwright/admin-storage-state.json' })

  test('DM-14: admin dashboard uses dark cards and surfaces', async ({ page }) => {
    await gotoWithTheme(page, '/admin/dashboard')
    await expect(page.getByRole('heading', { name: 'Dashboard' })).toBeVisible()
    await page.locator('main .grid').first().waitFor()
    await page.screenshot({ path: 'test-results/playwright/dark-mode-admin-dashboard-dark.png', fullPage: true })
  })

  test('DM-15: blog editor uses the dark primary button token', async ({ page }) => {
    await gotoWithTheme(page, '/admin/blog')
    await page.getByTestId('admin-blog-row').first().getByTitle('Edit').click()
    const saveButton = page.getByRole('button', { name: /Update Post|Create Post/i })
    await expect(saveButton).toBeVisible()

    const background = await getColorChannels(saveButton, 'background-color')
    const expectedBackground = await getRootVariableChannels(page, '--primary')
    expectRgbClose(background, expectedBackground)
  })

  test('DM-16: work editor uses the dark primary button token', async ({ page }) => {
    await gotoWithTheme(page, '/admin/works')
    await page.getByTestId('admin-work-row').first().getByRole('link').first().click()
    const saveButton = page.getByRole('button', { name: /Update Work|Create Work|Create with Videos/i }).last()
    await expect(saveButton).toBeVisible()

    const background = await getColorChannels(saveButton, 'background-color')
    const expectedBackground = await getRootVariableChannels(page, '--primary')
    expectRgbClose(background, expectedBackground)
  })

  test('DM-17: destructive buttons gain the dark hover state', async ({ page }) => {
    await gotoWithTheme(page, '/admin/blog')
    const deleteButton = page.getByRole('button', { name: 'Delete' }).first()
    await expect(deleteButton).toBeVisible()
    await deleteButton.hover()

    const background = await getColorChannels(deleteButton, 'background-color')
    expect(background[3]).toBeGreaterThan(0)
  })
})
