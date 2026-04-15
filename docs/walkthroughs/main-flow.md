# Main Flow

기본 운영 흐름:

1. 작업 브랜치에서 개발
2. 작업 브랜치를 `dev` 에 반영
3. `dev` 기준 검증 통과
4. `scripts/promote-main-runtime.sh` 실행
5. `release/main-promote` 브랜치 생성/푸시
6. `release/main-promote -> main` 반영
7. `main` 기준 runtime CI / GHCR publish / 운영 서버 pull-up

한 줄 요약:

```text
dev 통과 -> promote-main-runtime.sh -> release/main-promote -> main
```
