
import { OpenAI, AzureOpenAI } from 'openai'
import { NextResponse } from 'next/server'

// Reuse client initialization logic
const getClient = () => {
    const azureApiKey = process.env.AZURE_OPENAI_API_KEY
    const azureEndpoint = process.env.AZURE_OPENAI_ENDPOINT
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

const SYSTEM_PROMPT = (title: string) => `
You are an expert technical portfolio editor and career coach.
Your task is to take a raw, potentially brief or unstructured project description for a project titled "${title}" and transform it into a professional, compelling, and well-structured portfolio entry.

**Goal:**
Enrich the content to highlight technical depth, problem-solving skills, and professional value, specifically for "${title}".

**Rules:**
1. **Tone:** Professional, confident, and technical. Avoid overly flowery language but make it sound impressive.
2. **Structure & Formatting:**
    - Format usage: mimic a clean **Markdown** style but return valid HTML.
    - **Headings:** Use <h2> tags for section headers to make them big and distinct (e.g., <h2>Overview</h2>).
    - **Lists:** Use <ul> and <li> for structured points.
    - **Bolding:** Use <strong> or <b> for key terms.
    - **Sections:**
        - <h2>Overview</h2>: A compelling summary of the project "${title}".
        - <h2>Key Features</h2>: A bulleted list.
        - <h2>Technical Stack</h2>: A detailed breakdown.
        - <h2>Challenges & Solutions</h2>: (Optional)
3. **Enhancement:**
    - Expand on vague terms (e.g., change "used React" to "Leveraged React for building a dynamic and responsive user interface").
    - Infer standard technical practices where appropriate.
    - Fix grammar, spelling, and punctuation.
    - **CRITICAL:** Start with a standard text introduction. Do NOT start with a heading like 'Overview' immediately if it feels abrupt, but generally follow the structure.
6. **Images:**
    - **PRESERVE ALL IMAGES:** If the input HTML contains <img> tags, you MUST include them in the output at appropriate locations (usually where they originally appeared or better placed contextually). Do NOT remove any <img> tags.
7. **Format:** Return ONLY valid HTML. Do not include markdown code fences (e.g. \`\`\`html).
5. **Language:** Keep the output in the SAME language as the input (Korean or English).
`

export async function POST(req: Request) {
    try {
        const { html, title } = await req.json()

        if (!html) {
            return NextResponse.json(
                { error: 'Content is required' },
                { status: 400 }
            )
        }

        const { client, model } = getClient()

        const completion = await client.chat.completions.create({
            messages: [
                { role: 'system', content: SYSTEM_PROMPT(title || 'Untitled Project') },
                { role: 'user', content: html }
            ],
            model: model,
        })

        const fixedHtml = completion.choices[0]?.message?.content || ''

        // Clean up markdown fences if GPT adds them
        const cleanHtml = fixedHtml.replace(/^```html\s*|\s*```$/g, '')

        return NextResponse.json({ fixedHtml: cleanHtml })
    } catch (error: unknown) {
        console.error('AI Enrich Error:', error)
        const errorMessage = error instanceof Error ? error.message : 'Unknown error'
        return NextResponse.json(
            { error: errorMessage || 'Failed to enrich content' },
            { status: 500 }
        )
    }
}
