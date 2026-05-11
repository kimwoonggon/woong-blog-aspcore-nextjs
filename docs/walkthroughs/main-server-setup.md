# Main Server Setup

운영 기준 핵심:

- `main` 기준 코드 사용
- `docker compose --env-file .env.prod -f docker-compose.prod.yml pull`
- bootstrap nginx로 ACME challenge 대응
- certbot 발급 후 `prod.conf` 로 전환
- 실서비스는 443만 담당

서버에서 필요한 최소 흐름:

```bash
cp .env.prod.example .env.prod
vi .env.prod
docker compose --env-file .env.prod -f docker-compose.prod.yml pull
docker compose --env-file .env.prod -f docker-compose.prod.yml up -d
curl -i https://woonglab.com/api/health
```

기동 후 production preflight:

```bash
BASE_URL=https://woonglab.com \
./scripts/prod-runtime-preflight.sh
```

Docker/SSH 없이 public origin만 먼저 확인할 때:

```bash
BASE_URL=https://woonglab.com \
WORK_READ_PATH=/api/public/works/<real-work-slug> \
STUDY_READ_PATH=/api/public/blogs/<real-study-slug> \
./scripts/prod-public-origin-preflight.sh
```

이 단계가 `iconUrl`, `contentJson`, `originalFileName`, `fileSize`, missing `X-Nginx-Request-Time` 등으로 실패하면 아직 최신 `main` runtime으로 볼 수 없으므로 Real Backend Test 결과를 해석하지 않는다.

실제 Work/Study read target을 지정한 Real Backend Test:

```bash
BASE_URL=https://woonglab.com \
WORK_READ_PATH=/api/public/works/<real-work-slug> \
STUDY_READ_PATH=/api/public/blogs/<real-study-slug> \
RATES="100 200 300 400" \
DURATION_SECONDS=30 \
MAX_VUS=500 \
PRE_ALLOCATED_VUS=100 \
./scripts/prod-real-load-steps.sh
```

Real Backend Test 조건:

- list target은 `/api/public/works?page=1&pageSize=12`, `/api/public/blogs?page=1&pageSize=12`로 고정한다.
- `WORK_READ_PATH`, `STUDY_READ_PATH`는 실제 public detail API 경로를 넣는다.
- `seed`, `seeded`, `fixture` 경로는 script가 실패 처리한다.
- cache shortcut은 사용하지 않는다. k6 script가 요청마다 `__k6Vu`, `__k6Iter`를 붙여 동일 URL cache 착시를 피한다.

`main` runtime image 값은 `.env.prod.example`에 이미 실제 GHCR 경로로 들어 있다.
서버에서는 비밀값과 서버별 경로만 채우면 된다.

필수 확인 값:

```text
FRONTEND_IMAGE=ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-frontend:main
BACKEND_IMAGE=ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-runtime-backend:main
NEXT_PUBLIC_SITE_URL=https://woonglab.com
LoadTesting__BaseUrl=https://woonglab.com
POSTGRES_DB=portfolio
POSTGRES_USER=portfolio
POSTGRES_PASSWORD=<server-secret>
Auth__ClientId=<google-client-id>
Auth__ClientSecret=<google-client-secret>
Auth__AdminEmails__0=<admin-email>
```

권장 bind mount:

```text
POSTGRES_DATA_DIR=/srv/woong-blog/postgres
MEDIA_DATA_DIR=/srv/woong-blog/media
DATA_PROTECTION_DIR=/srv/woong-blog/data-protection
```
