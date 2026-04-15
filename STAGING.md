# Staging Runbook

별도 staging 폴더에서 `dev` 이미지를 pull해서 올리는 가장 짧은 절차다.

## 1. 폴더 준비

```bash
mkdir -p ~/woong-blog-staging
cd ~/woong-blog-staging
cp /path/to/repo/docker-compose.staging.yml .
cp /path/to/repo/.env.staging.example .env.staging
cp -r /path/to/repo/nginx ./nginx
mkdir -p certbot/www certbot/conf/live/current
```

## 2. 이미지 태그와 env 수정

`.env.staging`에서 최소 이 값들을 맞춘다.

```env
FRONTEND_IMAGE=ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-frontend:dev
BACKEND_IMAGE=ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-backend:dev
POSTGRES_PASSWORD=change-me
Auth__ClientId=replace-me
Auth__ClientSecret=replace-me
Auth__AdminEmails__0=admin@example.com
CODEX_HOME_DIR=/absolute/path/to/.codex
```

## 3. GHCR 로그인

```bash
echo "$GHCR_TOKEN" | docker login ghcr.io -u YOUR_GITHUB_USERNAME --password-stdin
```

## 4. pull / up

```bash
docker compose --env-file .env.staging -f docker-compose.staging.yml pull
docker compose --env-file .env.staging -f docker-compose.staging.yml up -d
docker compose --env-file .env.staging -f docker-compose.staging.yml ps
```

If staging uses `AI_PROVIDER=codex`, `CODEX_HOME_DIR` must point at a writable Codex home that already contains `auth.json`.

## 5. 최소 확인

```bash
curl -I http://localhost/
curl -I http://localhost/login
curl -I http://localhost/works
curl -I http://localhost/blog
```

운영 전 점검 포인트:
- `/login`에 `Continue as Local Admin`가 없어야 함
- `/api/auth/test-login`은 `404`여야 함
- `frontend`와 `backend`는 외부 포트를 직접 열지 않아야 함
