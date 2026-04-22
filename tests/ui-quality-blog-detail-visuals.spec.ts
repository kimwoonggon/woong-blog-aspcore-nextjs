import { expect, test } from './helpers/performance-test'

function px(value: string) {
  return Number.parseFloat(value.replace('px', ''))
}

test('VA-120 blog TOC stays visually separated from the article body', async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 900 })
  await page.goto('/blog/seeded-blog')

  const toc = page.getByTestId('blog-toc')
  await expect(toc).toBeVisible()

  const styles = await toc.evaluate((element) => {
    const style = getComputedStyle(element)
    return {
      borderTopWidth: style.borderTopWidth,
      backgroundColor: style.backgroundColor,
      position: style.position,
    }
  })

  expect(px(styles.borderTopWidth)).toBeGreaterThan(0)
  expect(styles.backgroundColor).not.toBe('rgba(0, 0, 0, 0)')
  expect(styles.position).toBe('sticky')
})

test('VA-121 blog body keeps readable paragraph line-height and spacing', async ({ page }) => {
  await page.goto('/blog/seeded-blog')

  const paragraph = page.locator('#blog-detail-content p').filter({ hasText: /\S/ }).first()
  await expect(paragraph).toBeVisible()

  const metrics = await paragraph.evaluate((element) => {
    const style = getComputedStyle(element)
    return {
      fontSize: style.fontSize,
      lineHeight: style.lineHeight,
      marginTop: style.marginTop,
      marginBottom: style.marginBottom,
    }
  })

  const ratio = px(metrics.lineHeight) / px(metrics.fontSize)
  expect(ratio).toBeGreaterThanOrEqual(1.5)
  expect(px(metrics.marginBottom)).toBeGreaterThan(0)
  expect(px(metrics.marginTop)).toBeGreaterThanOrEqual(0)
})

test('VA-122 blog previous and next cards keep balanced sizing and shared chrome', async ({ page }) => {
  await page.goto('/blog/seeded-blog')

  const nav = page.getByTestId('blog-prev-next')
  await expect(nav).toBeVisible()
  const links = nav.getByRole('link')
  expect(await links.count()).toBeGreaterThanOrEqual(1)

  if (await links.count() > 1) {
    const [first, second] = await Promise.all([links.nth(0).boundingBox(), links.nth(1).boundingBox()])
    expect(first).toBeTruthy()
    expect(second).toBeTruthy()
    expect(Math.abs((first?.height ?? 0) - (second?.height ?? 0))).toBeLessThanOrEqual(12)
  }

  const firstLink = links.first()
  const chrome = await firstLink.evaluate((element) => {
    const style = getComputedStyle(element)
    return {
      borderTopWidth: style.borderTopWidth,
      backgroundColor: style.backgroundColor,
    }
  })

  expect(px(chrome.borderTopWidth)).toBeGreaterThan(0)
  expect(chrome.backgroundColor).not.toBe('rgba(0, 0, 0, 0)')
})
