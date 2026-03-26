"use client"

import React, { useMemo } from 'react'
import { ThreeJsScene } from './ThreeJsScene'

interface InteractiveRendererProps {
    html: string
}

// Consistent HTML entity decoding for both SSR and client (no hydration mismatch)
const decodeHtmlEntities = (str: string): string => {
    return str
        .replace(/&lt;/gi, '<')
        .replace(/&gt;/gi, '>')
        .replace(/&quot;/gi, '"')
        .replace(/&#39;/gi, "'")
        .replace(/&amp;/gi, '&')
}

// Strip <html>, <head>, <body> tags to prevent hydration errors
const stripHtmlWrappers = (content: string): string => {
    return content
        .replace(/<!DOCTYPE[^>]*>/gi, '')
        .replace(/<\s*\/?\s*html(?!-)(?:\s[^>]*)?>/gi, '')
        .replace(/<\s*\/?\s*head(?:\s[^>]*)?>/gi, '')
        .replace(/<\s*\/?\s*body(?:\s[^>]*)?>/gi, '')
}

// Extract html-snippet content - returns the decoded HTML content from all html-snippet blocks
const extractHtmlSnippetContent = (htmlContent: string): string | null => {
    // Match <html-snippet html="..."> or <html-snippet html='...'>
    // The html attribute contains HTML-encoded content
    const snippetRegex = /<html-snippet\s+html=(["'])([\s\S]*?)\1(?:\s*\/)?\s*>(?:<\/html-snippet>)?/gi

    const results: string[] = []
    let match

    while ((match = snippetRegex.exec(htmlContent)) !== null) {
        const encodedHtml = match[2]
        const decodedHtml = decodeHtmlEntities(encodedHtml)
        const sanitizedHtml = stripHtmlWrappers(decodedHtml)
        results.push(sanitizedHtml)
    }

    if (results.length > 0) {
        return results.join('')
    }

    return null
}

// Parse three-js-block elements
const parseThreeJsBlocks = (htmlContent: string): { hasBlock: boolean; height: number } => {
    const match = /<three-js-block(?:\s+height=["']?(\d+)["']?)?\s*(?:\/)?>/i.exec(htmlContent)
    if (match) {
        return { hasBlock: true, height: match[1] ? parseInt(match[1]) : 300 }
    }
    return { hasBlock: false, height: 300 }
}

export function InteractiveRenderer({ html }: InteractiveRendererProps) {
    const processedHtml = useMemo(() => {
        const sanitized = stripHtmlWrappers(html)
        return sanitized
    }, [html])

    // Check for custom blocks
    const hasHtmlSnippet = processedHtml.includes('html-snippet')
    const hasThreeJsBlock = processedHtml.includes('three-js-block')

    // Fast path: no custom blocks, render directly
    if (!hasHtmlSnippet && !hasThreeJsBlock) {
        return <div className="prose prose-lg max-w-none dark:prose-invert" dangerouslySetInnerHTML={{ __html: processedHtml }} />
    }

    // Handle three-js-block
    if (hasThreeJsBlock) {
        const { height } = parseThreeJsBlocks(processedHtml)
        return (
            <div className="prose prose-lg max-w-none dark:prose-invert">
                <ThreeJsScene height={height} />
            </div>
        )
    }

    // Handle html-snippet blocks - extract only the snippet content, ignore surrounding text
    if (hasHtmlSnippet) {
        const snippetContent = extractHtmlSnippetContent(processedHtml)
        if (snippetContent) {
            return (
                <div
                    className="prose prose-lg max-w-none dark:prose-invert"
                    dangerouslySetInnerHTML={{ __html: snippetContent }}
                />
            )
        }
    }

    // Fallback
    return <div className="prose prose-lg max-w-none dark:prose-invert" dangerouslySetInnerHTML={{ __html: processedHtml }} />
}
