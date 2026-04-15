# Main Server Setup

운영 기준 핵심:

- `main` 기준 코드 사용
- `docker compose --env-file .env.prod -f docker-compose.prod.yml pull`
- bootstrap nginx로 ACME challenge 대응
- certbot 발급 후 `prod.conf` 로 전환
- 실서비스는 443만 담당

권장 bind mount:

```text
POSTGRES_DATA_DIR=/srv/woong-blog/postgres
MEDIA_DATA_DIR=/srv/woong-blog/media
DATA_PROTECTION_DIR=/srv/woong-blog/data-protection
```
