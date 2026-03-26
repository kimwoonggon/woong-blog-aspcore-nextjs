
import { OpenAI, AzureOpenAI } from 'openai'
import { NextResponse } from 'next/server'

// Initialize OpenAI client
// Prioritize Azure configuration if available, otherwise fallback to standard OpenAI
const getClient = () => {
    const azureApiKey = process.env.AZURE_OPENAI_API_KEY
    const azureEndpoint = process.env.AZURE_OPENAI_ENDPOINT
    // Check for both variable names to be safe
    const azureDeployment = process.env.AZURE_OPENAI_DEPLOYMENT || process.env.AZURE_DEPLOYMENT_NAME || 'gpt-5.2-chat'
    const azureApiVersion = process.env.AZURE_OPENAI_API_VERSION || '2024-08-01-preview'
    const openAiApiKey = process.env.OPENAI_API_KEY

    // Azure OpenAI Configuration
    if (azureApiKey && azureEndpoint) {
        return {
            client: new AzureOpenAI({
                apiKey: azureApiKey,
                endpoint: azureEndpoint,
                apiVersion: azureApiVersion,
                deployment: azureDeployment,
            }),
            model: azureDeployment
        }
    }

    // Standard OpenAI Configuration
    if (openAiApiKey) {
        return {
            client: new OpenAI({
                apiKey: openAiApiKey,
            }),
            model: process.env.OPENAI_MODEL || 'gpt-4o'
        }
    }

    throw new Error('Missing OpenAI or Azure OpenAI credentials')
}

const SYSTEM_PROMPT = `
You are an expert technical blog editor.
Your task is to clean up and format the provided HTML content from a Tiptap editor.

Rules:
1. CODE BLOCKS: Identify text that looks like code (e.g., imports, function definitions, console commands) and wrap them in <pre><code class="language-xyz">...</code></pre>.
2. FORMATTING: Fix grammar, spelling, and punctuation. Improve paragraph structure.
3. IMAGES: You MUST preserve all <img> tags exactly as they are. Do not remove or alter \`src\`, \`alt\`, or \`class\` attributes.
4. STRUCTURE: Use proper HTML tags (h1, h2, p, ul, ol).
5. RETURN ONLY HTML: Do not include markdown fences or explanation. Return the raw HTML string.
`

export async function POST(req: Request) {
    try {
        const { html } = await req.json()

        if (!html) {
            return NextResponse.json(
                { error: 'HTML content is required' },
                { status: 400 }
            )
        }

        const { client, model } = getClient()

        const completion = await client.chat.completions.create({
            messages: [
                { role: 'system', content: SYSTEM_PROMPT },
                { role: 'user', content: html }
            ],
            model: model,
            // temperature: 1, // Default value, safer for Azure models
        })

        const fixedHtml = completion.choices[0]?.message?.content || ''

        // Clean up markdown fences if GPT adds them despite instructions
        const cleanHtml = fixedHtml.replace(/^```html\s*|\s*```$/g, '')

        return NextResponse.json({ fixedHtml: cleanHtml })
    } catch (error: unknown) {
        console.error('AI Fix Error:', error)
        const errorMessage = error instanceof Error ? error.message : 'Unknown error'
        return NextResponse.json(
            { error: errorMessage || 'Failed to process content, check server logs.' },
            { status: 500 }
        )
    }
}
