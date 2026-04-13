import { expect, test } from '@playwright/test'
import { getStyle } from './helpers/ui-improvement'

test('Recent posts section uses the default page background', async ({ page }) => {
  await page.goto('/')

  const section = page.getByRole('heading', { name: 'Recent posts' }).locator('xpath=ancestor::section[1]')
  const sectionBackground = await getStyle(section, 'background-color')
  const bodyBackground = await getStyle(page.locator('body'), 'background-color')

  expect(sectionBackground).toBe(bodyBackground)
})

test('Recent posts heading uses bold weight', async ({ page }) => {
  await page.goto('/')

  const heading = page.getByRole('heading', { name: 'Recent posts' })
  const fontWeight = await getStyle(heading, 'font-weight')
  expect(Number.parseInt(fontWeight, 10)).toBeGreaterThanOrEqual(700)
})

test('Recent posts heading and copy keep a readable type hierarchy', async ({ page }) => {
  await page.goto('/')

  const section = page.getByTestId('recent-posts-section')
  const eyebrow = section.getByText('Notes and essays', { exact: true })
  const heading = section.getByRole('heading', { name: 'Recent posts' })
  const description = section.getByText(/Writing about product decisions/i)

  const [eyebrowSize, headingSize, descriptionSize, descriptionLineHeight] = await Promise.all([
    getStyle(eyebrow, 'font-size'),
    getStyle(heading, 'font-size'),
    getStyle(description, 'font-size'),
    getStyle(description, 'line-height'),
  ])

  expect(Number.parseFloat(headingSize)).toBeGreaterThan(Number.parseFloat(descriptionSize))
  expect(Number.parseFloat(descriptionSize)).toBeGreaterThan(Number.parseFloat(eyebrowSize))
  expect(Number.parseFloat(descriptionLineHeight)).toBeGreaterThan(Number.parseFloat(descriptionSize) * 1.45)
})

test('Recent posts section preserves consistent vertical spacing between heading groups', async ({ page }) => {
  await page.goto('/')

  const section = page.getByTestId('recent-posts-section')
  const eyebrow = section.getByText('Notes and essays', { exact: true })
  const heading = section.getByRole('heading', { name: 'Recent posts' })
  const description = section.getByText(/Writing about product decisions/i)

  const [eyebrowBox, headingBox, descriptionBox] = await Promise.all([
    eyebrow.boundingBox(),
    heading.boundingBox(),
    description.boundingBox(),
  ])

  expect(eyebrowBox).toBeTruthy()
  expect(headingBox).toBeTruthy()
  expect(descriptionBox).toBeTruthy()

  expect(headingBox!.y).toBeGreaterThan(eyebrowBox!.y)
  expect(descriptionBox!.y).toBeGreaterThan(headingBox!.y)
  expect(descriptionBox!.y - headingBox!.y).toBeGreaterThanOrEqual(24)
})
