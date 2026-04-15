# Home Production Runbook

이 문서는 `main` runtime-only 트리를 기준으로 집 서버 또는 개인 서버에서 이 프로젝트를 실제 서비스로 운영하기 위한 단계별 메모다.

기준 전제:
- 운영 배포는 source build가 아니라 `GHCR` 이미지 pull 방식이다.
- 외부 공개는 `nginx`가 `80/443`으로 처리한다.
- 인증서는 `Let's Encrypt + webroot` 방식으로 발급한다.
- PostgreSQL, media, data-protection은 Docker volume으로 유지한다.
- `AI_PROVIDER=codex`를 유지한다면 host의 Codex home을 container에 mount 한다.

관련 파일:
- [docker-compose.prod.yml](/mnt/d/woong-blog/woong-blog/docker-compose.prod.yml)
- [nginx/prod-bootstrap.conf](/mnt/d/woong-blog/woong-blog/nginx/prod-bootstrap.conf)
- [nginx/prod.conf](/mnt/d/woong-blog/woong-blog/nginx/prod.conf)
- [.env.prod.example](/mnt/d/woong-blog/woong-blog/.env.prod.example)
- [DEPLOYMENT.md](/mnt/d/woong-blog/woong-blog/DEPLOYMENT.md)

## 1. 사전 확인

먼저 아래가 가능한지 확인한다.

- 공인 IP가 있다.
- 공유기 또는 방화벽에서 `80`, `443` 포트를 서버로 포워딩할 수 있다.
- 사용할 도메인을 보유하고 있다.
- `ghcr.io`에서 이미지를 pull할 수 있는 GitHub token이 있다.
- Google OIDC client를 만들 수 있다.
- 서버 안에서 Docker와 Docker Compose를 실행할 수 있다.

집 인터넷이 `CGNAT`이면 `Let's Encrypt HTTP-01`이 실패할 수 있다. 이 경우엔 현재 문서 기준 배포가 바로 되지 않는다.

## 2. 도메인 준비

예시 도메인:
- `example.com`
- `www.example.com`

DNS에서 최소 아래를 맞춘다.

- `A example.com -> 공인 IP`
- `A www.example.com -> 공인 IP`

유동 IP면 DDNS를 같이 써야 한다.

DNS 반영 확인:

```bash
dig +short example.com
dig +short www.example.com
```

## 3. 서버 준비

운영 폴더를 하나 만든다.

```bash
mkdir -p ~/woong-blog-prod
cd ~/woong-blog-prod
```

`main` runtime-only 트리에서 필요한 파일만 가져온다.

```bash
cp /path/to/main-runtime/.env.prod.example .env.prod
cp /path/to/main-runtime/docker-compose.prod.yml .
cp -r /path/to/main-runtime/nginx ./nginx
mkdir -p certbot/www certbot/conf/live/current
```

이미 `main` runtime-only 트리를 그대로 checkout 해 둔 서버라면 `cp` 대신 그 디렉터리에서 작업하면 된다.

## 4. 서버 패키지 준비

최소 설치:

```bash
docker --version
docker compose version
```

없으면 Docker Engine + Compose plugin을 설치한다.

## 5. 포트포워딩

공유기 또는 방화벽에서 아래를 설정한다.

- `80 -> 서버 80`
- `443 -> 서버 443`

운영 서버 내부 방화벽도 허용해야 한다.

예:

```bash
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
```

## 6. 운영 env 작성

기본 파일:

```bash
cp .env.prod.example .env.prod
```

최소 수정 항목:

```env
FRONTEND_IMAGE=ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-frontend:main
BACKEND_IMAGE=ghcr.io/kimwoonggon/woong-blog-aspcore-nextjs-backend:main

POSTGRES_DB=portfolio
POSTGRES_USER=portfolio
POSTGRES_PASSWORD=<강한 비밀번호>

Auth__Enabled=true
Auth__Authority=https://accounts.google.com
Auth__ClientId=<google client id>
Auth__ClientSecret=<google client secret>
Auth__PublicOrigin=https://example.com
Auth__CallbackPath=/api/auth/callback
Auth__SignedOutRedirectPath=/
Auth__AdminEmails__0=<운영 관리자 이메일>

CODEX_HOME_DIR=/absolute/path/to/.codex
AI_PROVIDER=codex
NGINX_DEFAULT_CONF=./nginx/prod-bootstrap.conf
```

메모:
- `CODEX_HOME_DIR`는 실제 host 절대경로가 더 안전하다.
- `Auth__AdminEmails__0`는 Google 로그인 후 admin으로 인정할 이메일이다.
- `Auth__PublicOrigin`는 Google에 보낼 `redirect_uri`의 기준 origin이다. 프록시/호스트 인식이 흔들릴 수 있는 운영 환경에서는 실제 서비스 도메인으로 고정하는 편이 안전하다.
- `NGINX_DEFAULT_CONF`는 처음엔 `prod-bootstrap.conf`로 둔다.

## 7. Google OIDC 설정

Google Cloud Console에서 OAuth client를 만든 뒤 아래를 등록한다.

Authorized redirect URI:

```text
https://example.com/api/auth/callback
```

현재 권장 운영 정책은 `www`를 대표 origin으로 직접 서비스하지 않고 apex로 `301` canonical redirect 하는 것이다.
그래서 Google OAuth redirect URI도 apex만 대표값으로 쓰는 편이 안전하다.

`www`까지 직접 서비스할 계획이 아니라면 아래 URI는 굳이 등록하지 않는다.

```text
https://www.example.com/api/auth/callback
```

필요하면 다음도 같이 검토한다.

Authorized JavaScript origins:

```text
https://example.com
https://www.example.com
```

실제 서비스 도메인과 정확히 맞아야 한다.

## 8. GHCR 로그인

서버에서 package read 권한이 있는 token으로 로그인한다.

```bash
echo "$GHCR_TOKEN" | docker login ghcr.io -u YOUR_GITHUB_USERNAME --password-stdin
```

권한:
- `read:packages`

## 9. 최초 bootstrap 기동

인증서가 아직 없을 때는 `80` 포트 bootstrap nginx로 먼저 올린다.

```bash
docker compose --env-file .env.prod -f docker-compose.prod.yml pull
docker compose --env-file .env.prod -f docker-compose.prod.yml up -d db backend frontend nginx
docker compose --env-file .env.prod -f docker-compose.prod.yml ps
```

이 상태의 목적:
- `/.well-known/acme-challenge/` 응답
- 앱 기본 health 응답 확인

간단 확인:

```bash
curl -I http://example.com/
curl -I http://example.com/login
curl -fsS http://example.com/api/health
```

## 10. Let's Encrypt 발급

단일 도메인 예시:

```bash
docker run --rm \
  -v "$PWD/certbot/www:/var/www/certbot" \
  -v "$PWD/certbot/conf:/etc/letsencrypt" \
  certbot/certbot certonly \
  --webroot \
  -w /var/www/certbot \
  -d example.com \
  -d www.example.com \
  --email you@example.com \
  --agree-tos \
  --no-eff-email
```

발급 뒤 current symlink를 맞춘다.

```bash
mkdir -p certbot/conf/live/current
ln -sfn ../example.com/fullchain.pem certbot/conf/live/current/fullchain.pem
ln -sfn ../example.com/privkey.pem certbot/conf/live/current/privkey.pem
```

인증서 폴더명이 `example.com-0001`처럼 나왔으면 그 이름으로 symlink를 맞춘다.

## 11. HTTPS 전환

이제 nginx config를 SSL 모드로 바꾼다.

```bash
sed -i 's#^NGINX_DEFAULT_CONF=.*#NGINX_DEFAULT_CONF=./nginx/prod.conf#' .env.prod
docker compose --env-file .env.prod -f docker-compose.prod.yml up -d nginx
```

확인:

```bash
curl -I http://example.com/
curl -I https://example.com/
curl -I https://example.com/login
curl -fsS https://example.com/api/health
```

기대 결과:
- `http://example.com`은 `301` 또는 `308`으로 HTTPS로 이동
- `https://example.com`은 정상 응답
- `https://www.example.com`은 `https://example.com`으로 `301`

## 12. 운영 확인

최소 확인 순서:

1. `https://example.com/`
2. `https://example.com/blog`
3. `https://example.com/works`
4. `https://example.com/login`
5. Google 로그인
6. admin 진입

운영 정책 확인:
- `/login`에 `Continue as Local Admin`가 보이면 안 된다.
- `/api/auth/test-login`은 `404`여야 한다.
- backend/frontend가 직접 외부 포트를 열지 않아야 한다.
- `www`로 접속해도 apex로 canonical redirect 되어야 한다.
- Google redirect URI는 실제 운영 origin과 정확히 일치해야 한다.

## 13. 업데이트 절차

새 `main` 이미지가 올라오면 아래만 수행한다.

```bash
docker compose --env-file .env.prod -f docker-compose.prod.yml pull
docker compose --env-file .env.prod -f docker-compose.prod.yml up -d
docker compose --env-file .env.prod -f docker-compose.prod.yml ps
```

업데이트 후 확인:

```bash
curl -I https://example.com/
curl -I https://example.com/login
curl -I https://example.com/blog
curl -I https://example.com/works
```

주의:
- 이 절차는 컨테이너 recreate가 들어가므로 완전한 무중단 배포는 아니다.
- 무중단이 필요하면 새 backend/frontend 세트를 별도 service name으로 먼저 올린 뒤 nginx upstream만 전환하는 blue-green 구조가 필요하다.

## 14. 인증서 갱신

수동 갱신:

```bash
docker run --rm \
  -v "$PWD/certbot/www:/var/www/certbot" \
  -v "$PWD/certbot/conf:/etc/letsencrypt" \
  certbot/certbot renew \
  --webroot \
  -w /var/www/certbot
docker compose --env-file .env.prod -f docker-compose.prod.yml exec nginx nginx -s reload
```

운영이면 `cron` 또는 systemd timer로 자동화하는 것을 권장한다.

예:

```cron
0 5 * * * cd /home/USER/woong-blog-prod && docker run --rm -v "$PWD/certbot/www:/var/www/certbot" -v "$PWD/certbot/conf:/etc/letsencrypt" certbot/certbot renew --webroot -w /var/www/certbot && docker compose --env-file .env.prod -f docker-compose.prod.yml exec nginx nginx -s reload
```

## 15. 백업

운영에서 최소 백업 대상:
- `postgres-data`
- `media-storage`
- `certbot/conf`
- `.env.prod`

주의:
- `data-protection-keys`를 지우면 로그인 세션이 모두 끊긴다.
- `postgres-data`와 `media-storage`를 삭제하면 운영 데이터가 사라진다.

볼륨 목록 확인:

```bash
docker volume ls | grep woong-blog
```

## 16. 자주 막히는 부분

### 1. 인증서 발급 실패
- DNS가 아직 안 퍼졌거나
- `80` 포트포워딩이 안 됐거나
- CGNAT 환경일 수 있다

### 2. Google 로그인 후 callback 실패
- Google Console의 redirect URI가 실제 도메인과 다르다
- `https://example.com/api/auth/callback` 오탈자 확인

### 3. Codex 관련 401
- `AI_PROVIDER=codex`인데 `CODEX_HOME_DIR` mount가 비었거나
- `auth.json`이 없는 경로를 mount 했다

### 4. 로그인은 되는데 세션이 자꾸 끊김
- `data-protection-keys` volume이 유지되지 않음
- 도메인/https 설정이 꼬여 secure cookie가 정상 적용되지 않음

### 5. 갱신 후 HTTPS가 안 붙음
- `current/fullchain.pem`, `current/privkey.pem` symlink 대상이 틀렸을 수 있다

## 17. 최소 운영 체크리스트

- [ ] 도메인 DNS가 공인 IP를 가리킨다
- [ ] 공유기에서 `80`, `443` 포트포워딩 완료
- [ ] `.env.prod` 실제 값 입력 완료
- [ ] Google redirect URI 등록 완료
- [ ] GHCR 로그인 완료
- [ ] bootstrap nginx 기동 완료
- [ ] Let's Encrypt 발급 완료
- [ ] `prod.conf` 전환 완료
- [ ] `https://example.com` 정상 접속
- [ ] Google admin 로그인 정상
- [ ] backup/renew 절차 준비 완료
