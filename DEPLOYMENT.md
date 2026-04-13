# Production Deployment

이 프로젝트의 운영 배포는 `main`에서 GHCR로 올라간 이미지를 서버에서 pull해서 실행하는 방식이다. 서버에서는 source build를 하지 않는다.

## 1. 서버 준비

1. 운영 서버에 `main` runtime-only 트리를 checkout 한다.
2. `docker`, `docker compose`를 설치한다.
3. 운영 env 파일을 준비한다.

```bash
cp .env.prod.example .env.prod
```

4. `.env.prod`에서 다음을 실제 값으로 수정한다.
   - `FRONTEND_IMAGE`
   - `BACKEND_IMAGE`
   - `POSTGRES_PASSWORD`
   - `Auth__ClientId`
   - `Auth__ClientSecret`
   - `Auth__AdminEmails__0`

## 2. GHCR 로그인

서버에서 GHCR pull 권한이 있는 토큰으로 로그인한다.

```bash
echo "$GHCR_TOKEN" | docker login ghcr.io -u YOUR_GITHUB_USERNAME --password-stdin
```

필요 권한:
- `read:packages`

## 3. 최초 bootstrap 기동

인증서가 아직 없을 때는 bootstrap nginx로 먼저 올린다.

```bash
mkdir -p certbot/www certbot/conf/live/current
docker compose --env-file .env.prod -f docker-compose.prod.yml pull
docker compose --env-file .env.prod -f docker-compose.prod.yml up -d db backend frontend nginx
```

기본값으로 `NGINX_DEFAULT_CONF=./nginx/prod-bootstrap.conf` 이므로, 이 상태에서는 `80` 포트와 ACME challenge가 동작한다.

## 4. Let’s Encrypt 인증서 발급

아래 예시는 단일 대표 도메인을 기준으로 한다.

```bash
docker run --rm \
  -v "$PWD/certbot/www:/var/www/certbot" \
  -v "$PWD/certbot/conf:/etc/letsencrypt" \
  certbot/certbot certonly \
  --webroot \
  -w /var/www/certbot \
  -d your-domain.com \
  --email you@example.com \
  --agree-tos \
  --no-eff-email
```

발급 뒤 nginx가 고정 경로를 보도록 symlink를 만든다.

```bash
mkdir -p certbot/conf/live/current
ln -sfn ../your-domain.com/fullchain.pem certbot/conf/live/current/fullchain.pem
ln -sfn ../your-domain.com/privkey.pem certbot/conf/live/current/privkey.pem
```

그 다음 `.env.prod`의 nginx config를 운영 SSL 설정으로 바꾼다.

```bash
sed -i 's#^NGINX_DEFAULT_CONF=.*#NGINX_DEFAULT_CONF=./nginx/prod.conf#' .env.prod
docker compose --env-file .env.prod -f docker-compose.prod.yml up -d nginx
```

## 5. 일반 배포

이후 운영 배포는 image pull만 수행한다.

```bash
docker compose --env-file .env.prod -f docker-compose.prod.yml pull
docker compose --env-file .env.prod -f docker-compose.prod.yml up -d
```

## 6. 인증서 갱신

갱신은 webroot 방식으로 수행한다.

```bash
docker run --rm \
  -v "$PWD/certbot/www:/var/www/certbot" \
  -v "$PWD/certbot/conf:/etc/letsencrypt" \
  certbot/certbot renew \
  --webroot \
  -w /var/www/certbot
```

symlink는 `current -> your-domain.com` 구조를 유지하므로 갱신 후 그대로 유효하다. 갱신 뒤에는 nginx를 reload 한다.

```bash
docker compose --env-file .env.prod -f docker-compose.prod.yml exec nginx nginx -s reload
```

## 7. 운영 주의사항

- `frontend`와 `backend`는 외부 포트를 열지 않는다.
- 외부 공개는 `nginx`만 `80/443`으로 처리한다.
- `data-protection-keys` volume을 삭제하면 로그인 세션과 antiforgery가 끊길 수 있다.
- `media-storage`와 `postgres-data`는 운영 데이터이므로 재배포 중 삭제하면 안 된다.

## Staging Deployment

`dev` CI 성공 후에는 staging용 GHCR 이미지가 별도 workflow로 publish된다.

예시 태그:

- `ghcr.io/<owner>/woong-blog-aspcore-nextjs-frontend:dev`
- `ghcr.io/<owner>/woong-blog-aspcore-nextjs-backend:dev`
- `ghcr.io/<owner>/woong-blog-aspcore-nextjs-frontend:dev-sha-<sha>`
- `ghcr.io/<owner>/woong-blog-aspcore-nextjs-backend:dev-sha-<sha>`

다른 폴더에서 staging 런타임을 올릴 때는 아래처럼 시작한다.

```bash
mkdir -p ~/woong-blog-staging
cd ~/woong-blog-staging
cp /path/to/repo/docker-compose.staging.yml .
cp /path/to/repo/.env.staging.example .env.staging
cp -r /path/to/repo/nginx ./nginx
mkdir -p certbot/www certbot/conf/live/current
```

`.env.staging`를 수정한 뒤 실행:

```bash
echo "$GHCR_TOKEN" | docker login ghcr.io -u YOUR_GITHUB_USERNAME --password-stdin
docker compose --env-file .env.staging -f docker-compose.staging.yml pull
docker compose --env-file .env.staging -f docker-compose.staging.yml up -d
```

로컬 홈서버에서 먼저 staging 검증을 하고, 그 다음에만 `main` promotion을 진행하는 흐름을 권장한다.
