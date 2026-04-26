# E2E Latency Summary

Generated: 2026-04-25T15:14:15.186Z

- Tests with latency artifacts: 9
- Budget failures: 0
- Warnings: 1

## Slowest Tests

| Duration ms | Status | Project | Spec | Title |
| --- | --- | --- | --- | --- |
| 3485.53 | passed | chromium-public | /work/tests/e2e-response-time.spec.ts | response time: desktop public nav clicks meet budget |
| 1946.48 | passed | chromium-public | /work/tests/e2e-response-time.spec.ts | response time: public detail card opens meet budget |
| 1582.64 | passed | chromium-public | /work/tests/e2e-response-time.spec.ts | response time: AI Fix provider dropdown is ready within budget |
| 1293.02 | passed | chromium-public | /work/tests/e2e-response-time.spec.ts | response time: unified public search submit meets budget |
| 1097.88 | passed | chromium-public | /work/tests/e2e-response-time.spec.ts | response time: Study list direct load meets budget |
| 915.78 | passed | chromium-public | /work/tests/e2e-response-time.spec.ts | response time: admin site settings save refreshes public home within budget |
| 906.83 | passed | chromium-public | /work/tests/e2e-response-time.spec.ts | response time: Works mobile append next page meets budget |
| 604.1 | passed | chromium-public | /work/tests/e2e-response-time.spec.ts | response time: Study mobile append next page meets budget |
| 597.08 | passed | chromium-public | /work/tests/e2e-response-time.spec.ts | response time: Works list direct load meets budget |

## Slowest API Responses

| Duration ms | Status | Method | URL | Spec |
| --- | --- | --- | --- | --- |
| 54.18 | 200 | GET | http://woong-prod-latency-nginx-internal-1777129545/api/auth/session | /work/tests/e2e-response-time.spec.ts |
| 32.23 | 200 | POST | http://woong-prod-latency-nginx-internal-1777129545/revalidate-public | /work/tests/e2e-response-time.spec.ts |
| 21.84 | 200 | GET | http://woong-prod-latency-nginx-internal-1777129545/api/public/works?page=2&pageSize=10 | /work/tests/e2e-response-time.spec.ts |
| 21.51 | 200 | PUT | http://woong-prod-latency-nginx-internal-1777129545/api/admin/site-settings | /work/tests/e2e-response-time.spec.ts |
| 19.24 | 200 | GET | http://woong-prod-latency-nginx-internal-1777129545/api/admin/ai/runtime-config | /work/tests/e2e-response-time.spec.ts |
| 19.15 | 200 | GET | http://woong-prod-latency-nginx-internal-1777129545/api/auth/csrf | /work/tests/e2e-response-time.spec.ts |
| 11.69 | 200 | GET | http://woong-prod-latency-nginx-internal-1777129545/api/auth/session | /work/tests/e2e-response-time.spec.ts |
| 11.14 | 200 | GET | http://woong-prod-latency-nginx-internal-1777129545/api/auth/session | /work/tests/e2e-response-time.spec.ts |
| 8.06 | 200 | GET | http://woong-prod-latency-nginx-internal-1777129545/api/public/blogs?page=2&pageSize=10 | /work/tests/e2e-response-time.spec.ts |
| 7.54 | 200 | GET | http://woong-prod-latency-nginx-internal-1777129545/api/auth/session | /work/tests/e2e-response-time.spec.ts |
| 5.13 | 200 | GET | http://woong-prod-latency-nginx-internal-1777129545/api/auth/session | /work/tests/e2e-response-time.spec.ts |
| 1.63 | 200 | GET | http://woong-prod-latency-nginx-internal-1777129545/api/auth/session | /work/tests/e2e-response-time.spec.ts |
| 1.29 | 200 | GET | http://woong-prod-latency-nginx-internal-1777129545/api/auth/session | /work/tests/e2e-response-time.spec.ts |
| 1.08 | 200 | GET | http://woong-prod-latency-nginx-internal-1777129545/api/auth/session | /work/tests/e2e-response-time.spec.ts |
| 0.89 | 200 | GET | http://woong-prod-latency-nginx-internal-1777129545/api/auth/session | /work/tests/e2e-response-time.spec.ts |
| 0.72 | 200 | GET | http://woong-prod-latency-nginx-internal-1777129545/api/auth/session | /work/tests/e2e-response-time.spec.ts |

## Slowest Interactions

| Duration ms | Name | Source | Target | Spec |
| --- | --- | --- | --- | --- |
| 120 | pointerover | performance-observer | a | /work/tests/e2e-response-time.spec.ts |
| 120 | pointerenter | performance-observer |  | /work/tests/e2e-response-time.spec.ts |
| 120 | pointerenter | performance-observer | html | /work/tests/e2e-response-time.spec.ts |
| 120 | pointerenter | performance-observer | body | /work/tests/e2e-response-time.spec.ts |
| 120 | pointerenter | performance-observer | div | /work/tests/e2e-response-time.spec.ts |
| 120 | pointerenter | performance-observer | header | /work/tests/e2e-response-time.spec.ts |
| 120 | pointerenter | performance-observer | nav | /work/tests/e2e-response-time.spec.ts |
| 120 | pointerenter | performance-observer | div | /work/tests/e2e-response-time.spec.ts |
| 120 | pointerenter | performance-observer | a | /work/tests/e2e-response-time.spec.ts |
| 120 | mouseover | performance-observer | a | /work/tests/e2e-response-time.spec.ts |
| 120 | pointerdown | performance-observer | a | /work/tests/e2e-response-time.spec.ts |
| 120 | mousedown | performance-observer | a | /work/tests/e2e-response-time.spec.ts |
| 120 | pointerup | performance-observer | a | /work/tests/e2e-response-time.spec.ts |
| 120 | mouseup | performance-observer | a | /work/tests/e2e-response-time.spec.ts |
| 120 | click | performance-observer | a | /work/tests/e2e-response-time.spec.ts |
| 88 | keyup | performance-observer | div | /work/tests/e2e-response-time.spec.ts |
| 80 | keydown | performance-observer | div | /work/tests/e2e-response-time.spec.ts |
| 80 | keypress | performance-observer | div | /work/tests/e2e-response-time.spec.ts |
| 80 | beforeinput | performance-observer | div | /work/tests/e2e-response-time.spec.ts |
| 80 | input | performance-observer | div | /work/tests/e2e-response-time.spec.ts |

## Slowest Measured Steps

| Duration ms | Status | Step | Spec |
| --- | --- | --- | --- |
| 1137.2 | passed | Public nav click to Study | /work/tests/e2e-response-time.spec.ts |
| 948.71 | passed | Public nav click to Works | /work/tests/e2e-response-time.spec.ts |
| 674.5 | passed | Public Work card opens detail | /work/tests/e2e-response-time.spec.ts |
| 654.12 | passed | Public Study card opens detail | /work/tests/e2e-response-time.spec.ts |
| 585.39 | passed | Study list direct load to primary content visible | /work/tests/e2e-response-time.spec.ts |
| 524.08 | passed | Public nav click to Introduction | /work/tests/e2e-response-time.spec.ts |
| 438.91 | passed | Public nav click to Contact | /work/tests/e2e-response-time.spec.ts |
| 431.95 | passed | Admin site settings save response-time path | /work/tests/e2e-response-time.spec.ts |
| 402.11 | passed | Works list direct load to primary content visible | /work/tests/e2e-response-time.spec.ts |
| 323.37 | passed | Study unified search submit response-time path | /work/tests/e2e-response-time.spec.ts |
| 169.75 | passed | Works mobile auto-append | /work/tests/e2e-response-time.spec.ts |
| 166.03 | passed | Study mobile auto-append | /work/tests/e2e-response-time.spec.ts |
| 158.95 | passed | AI Fix provider dropdown response-time path | /work/tests/e2e-response-time.spec.ts |

## Budget Failures

_None._

