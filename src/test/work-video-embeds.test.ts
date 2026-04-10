import { describe, expect, it } from 'vitest'
import {
  buildWorkVideoEmbedMarkup,
  extractWorkVideoEmbedIds,
  hasWorkVideoEmbeds,
  removeWorkVideoEmbedReferences,
  splitWorkVideoEmbedContent,
} from '@/lib/content/work-video-embeds'

describe('work video embeds helpers', () => {
  it('extracts and splits inline video embeds from html', () => {
    const html = `<p>Before</p>${buildWorkVideoEmbedMarkup('video-1')}<p>After</p>`

    expect(hasWorkVideoEmbeds(html)).toBe(true)
    expect(extractWorkVideoEmbedIds(html)).toEqual(['video-1'])
    expect(splitWorkVideoEmbedContent(html)).toEqual([
      { type: 'html', html: '<p>Before</p>' },
      { type: 'video', videoId: 'video-1' },
      { type: 'html', html: '<p>After</p>' },
    ])
  })

  it('removes only the matching embedded video reference', () => {
    const html = `<p>Before</p>${buildWorkVideoEmbedMarkup('video-1')}${buildWorkVideoEmbedMarkup('video-2')}`

    expect(removeWorkVideoEmbedReferences(html, 'video-1')).toContain('video-2')
    expect(removeWorkVideoEmbedReferences(html, 'video-1')).not.toContain('video-1')
  })
})
