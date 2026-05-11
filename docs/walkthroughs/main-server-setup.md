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
