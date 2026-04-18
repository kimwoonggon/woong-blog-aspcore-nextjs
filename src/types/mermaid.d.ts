declare module 'mermaid' {
  export interface MermaidConfig {
    startOnLoad?: boolean
    securityLevel?: string
    theme?: string
    fontFamily?: string
  }

  export interface MermaidRuntime {
    initialize: (config: MermaidConfig) => void
    render: (id: string, definition: string) => Promise<{ svg: string }>
  }

  const mermaid: MermaidRuntime
  export default mermaid
}
