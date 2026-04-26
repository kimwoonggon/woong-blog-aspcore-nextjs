# E2E Latency Summary

Generated: 2026-04-25T15:15:29.146Z

- Tests with latency artifacts: 9
- Budget failures: 0
- Warnings: 0

## Slowest Tests

| Duration ms | Status | Project | Spec | Title |
| --- | --- | --- | --- | --- |
| 2877.66 | passed | chromium-public | /work/tests/e2e-response-time.spec.ts | response time: desktop public nav clicks meet budget |
| 1705.43 | passed | chromium-public | /work/tests/e2e-response-time.spec.ts | response time: AI Fix provider dropdown is ready within budget |
| 1688 | passed | chromium-public | /work/tests/e2e-response-time.spec.ts | response time: public detail card opens meet budget |
| 1395.02 | passed | chromium-public | /work/tests/e2e-response-time.spec.ts | response time: admin site settings save refreshes public home within budget |
| 1356.69 | passed | chromium-public | /work/tests/e2e-response-time.spec.ts | response time: unified public search submit meets budget |
| 1083.81 | passed | chromium-public | /work/tests/e2e-response-time.spec.ts | response time: Study list direct load meets budget |
| 673.41 | passed | chromium-public | /work/tests/e2e-response-time.spec.ts | response time: Works mobile append next page meets budget |
| 659.08 | passed | chromium-public | /work/tests/e2e-response-time.spec.ts | response time: Works list direct load meets budget |
| 631.24 | passed | chromium-public | /work/tests/e2e-response-time.spec.ts | response time: Study mobile append next page meets budget |

## Slowest API Responses

| Duration ms | Status | Method | URL | Spec |
| --- | --- | --- | --- | --- |
| 43.06 | 200 | POST | http://woong-prod-latency-nginx-internal-1777129545/revalidate-public | /work/tests/e2e-response-time.spec.ts |
| 32.71 | 200 | GET | http://woong-prod-latency-nginx-internal-1777129545/api/admin/ai/runtime-config | /work/tests/e2e-response-time.spec.ts |
| 21.08 | 200 | GET | http://woong-prod-latency-nginx-internal-1777129545/api/public/works?page=2&pageSize=10 | /work/tests/e2e-response-time.spec.ts |
| 15.49 | 200 | PUT | http://woong-prod-latency-nginx-internal-1777129545/api/admin/site-settings | /work/tests/e2e-response-time.spec.ts |
| 14.59 | 200 | GET | http://woong-prod-latency-nginx-internal-1777129545/api/auth/session | /work/tests/e2e-response-time.spec.ts |
| 14.31 | 200 | GET | http://woong-prod-latency-nginx-internal-1777129545/api/auth/csrf | /work/tests/e2e-response-time.spec.ts |
| 13.59 | 200 | GET | http://woong-prod-latency-nginx-internal-1777129545/api/public/blogs?page=2&pageSize=10 | /work/tests/e2e-response-time.spec.ts |
| 13.32 | 200 | GET | http://woong-prod-latency-nginx-internal-1777129545/api/auth/session | /work/tests/e2e-response-time.spec.ts |
| 6.17 | 200 | GET | http://woong-prod-latency-nginx-internal-1777129545/api/auth/session | /work/tests/e2e-response-time.spec.ts |
| 5.98 | 200 | GET | http://woong-prod-latency-nginx-internal-1777129545/api/auth/session | /work/tests/e2e-response-time.spec.ts |
| 3.82 | 200 | GET | http://woong-prod-latency-nginx-internal-1777129545/api/auth/session | /work/tests/e2e-response-time.spec.ts |
| 2.87 | 200 | GET | http://woong-prod-latency-nginx-internal-1777129545/api/auth/session | /work/tests/e2e-response-time.spec.ts |
| 1.33 | 200 | GET | http://woong-prod-latency-nginx-internal-1777129545/api/auth/session | /work/tests/e2e-response-time.spec.ts |
| 0.66 | 200 | GET | http://woong-prod-latency-nginx-internal-1777129545/api/auth/session | /work/tests/e2e-response-time.spec.ts |
| 0.59 | 200 | GET | http://woong-prod-latency-nginx-internal-1777129545/api/auth/session | /work/tests/e2e-response-time.spec.ts |
| 0.43 | 200 | GET | http://woong-prod-latency-nginx-internal-1777129545/api/auth/session | /work/tests/e2e-response-time.spec.ts |

## Slowest Interactions

| Duration ms | Name | Source | Target | Spec |
| --- | --- | --- | --- | --- |
| 104 | pointerout | performance-observer | div | /work/tests/e2e-response-time.spec.ts |
| 104 | pointerover | performance-observer | textarea[aria-label="AI system prompt"] | /work/tests/e2e-response-time.spec.ts |
| 104 | pointerenter | performance-observer | textarea[aria-label="AI system prompt"] | /work/tests/e2e-response-time.spec.ts |
| 104 | pointerenter | performance-observer |  | /work/tests/e2e-response-time.spec.ts |
| 104 | mouseout | performance-observer | div | /work/tests/e2e-response-time.spec.ts |
| 104 | mouseover | performance-observer | textarea[aria-label="AI system prompt"] | /work/tests/e2e-response-time.spec.ts |
| 96 | pointerover | performance-observer |  | /work/tests/e2e-response-time.spec.ts |
| 96 | pointerenter | performance-observer |  | /work/tests/e2e-response-time.spec.ts |
| 96 | pointerenter | performance-observer | html | /work/tests/e2e-response-time.spec.ts |
| 96 | pointerenter | performance-observer | body | /work/tests/e2e-response-time.spec.ts |
| 96 | pointerenter | performance-observer | div | /work/tests/e2e-response-time.spec.ts |
| 96 | pointerenter | performance-observer | main | /work/tests/e2e-response-time.spec.ts |
| 96 | pointerenter | performance-observer |  | /work/tests/e2e-response-time.spec.ts |
| 96 | mouseover | performance-observer |  | /work/tests/e2e-response-time.spec.ts |
| 96 | pointerout | performance-observer | div | /work/tests/e2e-response-time.spec.ts |
| 96 | pointerleave | performance-observer | div | /work/tests/e2e-response-time.spec.ts |
| 96 | pointerleave | performance-observer | div | /work/tests/e2e-response-time.spec.ts |
| 96 | pointerleave | performance-observer | [data-testid="tiptap-editor-surface"] | /work/tests/e2e-response-time.spec.ts |
| 96 | pointerleave | performance-observer | [data-testid="tiptap-editor-shell"] | /work/tests/e2e-response-time.spec.ts |
| 96 | pointerover | performance-observer | button | /work/tests/e2e-response-time.spec.ts |

## Slowest Measured Steps

| Duration ms | Status | Step | Spec |
| --- | --- | --- | --- |
| 841.62 | passed | Public nav click to Works | /work/tests/e2e-response-time.spec.ts |
| 581.88 | passed | Study unified search submit response-time path | /work/tests/e2e-response-time.spec.ts |
| 565.21 | passed | Public Study card opens detail | /work/tests/e2e-response-time.spec.ts |
| 564.97 | passed | Study list direct load to primary content visible | /work/tests/e2e-response-time.spec.ts |
| 522.39 | passed | Public nav click to Introduction | /work/tests/e2e-response-time.spec.ts |
| 520.84 | passed | Admin site settings save response-time path | /work/tests/e2e-response-time.spec.ts |
| 502.52 | passed | Public Work card opens detail | /work/tests/e2e-response-time.spec.ts |
| 476.86 | passed | Public nav click to Contact | /work/tests/e2e-response-time.spec.ts |
| 460.37 | passed | Public nav click to Study | /work/tests/e2e-response-time.spec.ts |
| 394.46 | passed | Works list direct load to primary content visible | /work/tests/e2e-response-time.spec.ts |
| 219 | passed | AI Fix provider dropdown response-time path | /work/tests/e2e-response-time.spec.ts |
| 179.71 | passed | Study mobile auto-append | /work/tests/e2e-response-time.spec.ts |
| 174.98 | passed | Works mobile auto-append | /work/tests/e2e-response-time.spec.ts |

## Budget Failures

_None._

