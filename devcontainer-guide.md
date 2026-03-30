# Devcontainer 동작 원리 완전 해부

## 핵심 개념: "개발환경을 컨테이너로 통일"

보통 개발하면 로컬 PC에 Node.js, .NET SDK, PostgreSQL 등을 직접 설치한다.
**devcontainer는 이걸 Docker 컨테이너 안에 미리 세팅해놓고, VS Code가 그 컨테이너에 원격 접속하는 방식**이다.

```
┌─ 호스트 PC (Windows) ──────────────────────────────┐
│                                                      │
│  VS Code (UI만 여기서 실행)                           │
│     │                                                │
│     │  SSH/원격 접속 (자동)                            │
│     ▼                                                │
│  ┌─ Docker ──────────────────────────────────────┐   │
│  │                                                │   │
│  │  workspace 컨테이너  ← VS Code가 여기서 작업    │   │
│  │  (터미널, 코드 편집, 디버깅 전부 여기서 실행)      │   │
│  │                                                │   │
│  │  db 컨테이너  ← PostgreSQL                     │   │
│  │                                                │   │
│  └────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────┘
```

---

## devcontainer 구성요소가 연결되는 방식

devcontainer는 여러 파일과 메커니즘이 **계층적으로** 연결된다. 각각의 역할은 다르지만 최종적으로 하나의 개발환경을 만든다.

```
devcontainer.json  (총괄 설계도)
  │
  ├─ dockerComposeFile ──→ .devcontainer/docker-compose.yml (컨테이너 정의)
  │                            ├─ workspace 서비스: .NET SDK 이미지 + 소스코드 마운트
  │                            └─ db 서비스: PostgreSQL
  │
  ├─ features ──→ 컨테이너 위에 추가 도구 설치 (레이어 방식)
  │   ├─ ghcr.io/devcontainers/features/node:1 { version: "22" }
  │   │     → Node.js 22 + npm 설치 (이것만으로는 Next.js/React 없음)
  │   └─ ghcr.io/devcontainers/features/docker-outside-of-docker:1
  │         → Docker CLI 설치 + 호스트 Docker 소켓 연결
  │
  ├─ postCreateCommand ──→ "npm install && dotnet restore ..."
  │   │  컨테이너 최초 생성 시 1회 실행
  │   ├─ npm install: package.json을 읽고 Next.js, React 등 프론트엔드 의존성 설치
  │   └─ dotnet restore: WoongBlog.sln의 NuGet 패키지 복원
  │
  ├─ forwardPorts ──→ [3000, 5121]
  │     컨테이너 내부 포트를 호스트로 포워딩 (브라우저에서 localhost로 접속 가능)
  │
  └─ customizations.vscode.extensions ──→ VS Code 확장 자동 설치
        C# 확장, Docker 확장, ESLint 등
```

### 핵심: Next.js/React는 어디서 설치되는가?

Next.js와 React는 **feature가 아니라 npm install에서 설치**된다. 흐름:

```
[1] docker-compose.yml
    → workspace 이미지: .NET 10 SDK만 있는 Ubuntu
    → 이 시점: Node.js 없음, Next.js 없음

[2] features: node:1 { version: "22" }
    → workspace 컨테이너에 Node.js 22 + npm 바이너리 설치
    → 이 시점: node, npm 명령어 사용 가능. 하지만 Next.js는 아직 없음

[3] postCreateCommand: "npm install"
    → /workspaces/woong-blog/package.json을 읽음
    → dependencies에 있는 것들을 node_modules/에 설치:
       - next (Next.js 프레임워크)
       - react, react-dom
       - @tanstack/react-query
       - @tiptap/* (에디터)
       - three.js 등등
    → 이 시점: Next.js/React 사용 가능. npm run dev 실행 가능

[4] postCreateCommand: "dotnet restore backend/WoongBlog.sln"
    → 백엔드 NuGet 패키지 복원
    → 이 시점: dotnet run 실행 가능
```

**정리:**

| 단계 | 메커니즘 | 설치되는 것 |
|------|----------|------------|
| docker-compose.yml | Docker 이미지 | Ubuntu + .NET 10 SDK |
| features/node | devcontainer feature | Node.js 22 런타임 + npm |
| features/docker-outside-of-docker | devcontainer feature | Docker CLI |
| postCreateCommand → `npm install` | npm (package.json) | Next.js, React, 모든 프론트엔드 라이브러리 |
| postCreateCommand → `dotnet restore` | NuGet (.sln) | 백엔드 C# 패키지들 |
| customizations.vscode.extensions | VS Code | C#, Docker, ESLint 등 에디터 확장 |

> **feature ≠ 앱 프레임워크 설치**
> - feature는 **런타임/CLI 도구** 설치 (Node.js, Docker CLI, Python 등)
> - **앱 프레임워크**(Next.js, React)는 해당 런타임의 패키지 매니저(npm)로 설치
> - feature가 Node.js를 깔아줘야 npm이 생기고, npm이 있어야 npm install로 Next.js가 설치되는 **의존 관계**

---

## 각 파일의 역할

### 1. `devcontainer.json` — "개발환경 설계도"

VS Code에게 **"어떤 컨테이너를 어떻게 만들고, 뭘 설치하고, 어떻게 연결할지"** 알려주는 설정 파일.

```jsonc
{
  // 이 devcontainer를 어떻게 만들지 = docker-compose 파일 사용
  "dockerComposeFile": ["docker-compose.yml"],

  // compose 서비스 중 VS Code가 접속할 컨테이너
  "service": "workspace",

  // compose 서비스 중 함께 띄울 컨테이너들
  "runServices": ["workspace", "db"],

  // 컨테이너 안에서의 작업 폴더 경로
  "workspaceFolder": "/workspaces/woong-blog",

  // 컨테이너에 추가 설치할 도구 (플러그인 같은 개념)
  "features": {
    "node:1": { "version": "22" },           // Node.js 22 설치
    "docker-outside-of-docker:1": {}          // 호스트 Docker 사용 가능하게
  },

  // 호스트 → 컨테이너 포트 연결
  "forwardPorts": [3000, 5121],

  // 컨테이너 최초 생성 시 실행할 명령
  "postCreateCommand": "npm install && dotnet restore ...",

  // VS Code에 설치할 확장
  "customizations": { "vscode": { "extensions": [...] } }
}
```

**동작 순서:**
1. VS Code에서 "Reopen in Container" 클릭
2. VS Code가 `devcontainer.json`을 읽음
3. `dockerComposeFile`에 지정된 compose 파일로 컨테이너들을 띄움
4. `service: "workspace"` 컨테이너에 원격 접속
5. `features`로 Node.js, Docker CLI 등 추가 설치
6. `postCreateCommand` 실행 (npm install, dotnet restore)
7. VS Code 확장 설치
8. 준비 완료 → 터미널이 **컨테이너 안**에서 열림

---

### 2. `.devcontainer/docker-compose.yml` — "컨테이너 구성도"

**어떤 컨테이너를 몇 개 만들지** 정의한다.

```yaml
services:
  workspace:                     # ← VS Code가 접속할 컨테이너
    image: mcr.microsoft.com/devcontainers/dotnet:dev-10.0-noble
    #       ↑ .NET 10 SDK가 설치된 Ubuntu 이미지
    command: /bin/sh -c "while sleep 1000; do :; done"
    #        ↑ 컨테이너가 꺼지지 않게 무한 대기 (VS Code가 접속하려면 살아있어야 함)
    volumes:
      - ..:/workspaces/woong-blog:cached
      #  ↑ 호스트의 프로젝트 폴더를 컨테이너 안에 마운트
      - /var/run/docker.sock:/var/run/docker.sock
      #  ↑ 호스트의 Docker 소켓 공유 (컨테이너 안에서 docker 명령 가능)

  db:                            # ← DB 전용 컨테이너
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: portfolio
      POSTGRES_USER: portfolio
      POSTGRES_PASSWORD: portfolio
```

**핵심 포인트:**
- `workspace` 컨테이너는 **계속 살아있는 빈 서버** → VS Code가 접속해서 쓰는 용도
- `db` 컨테이너는 **PostgreSQL만 돌리는 용도**
- 소스코드는 볼륨 마운트(`..:/workspaces/woong-blog`)로 호스트 ↔ 컨테이너 간 공유

---

### 3. `.vscode/launch.json` — "F5 디버깅 설정"

devcontainer와는 **별개 개념**. VS Code에서 F5를 누르면 **뭘 어떻게 실행할지** 정의한다.

```
devcontainer.json  → 개발환경(컨테이너)을 만드는 설정
launch.json        → 그 환경 안에서 앱을 실행/디버깅하는 설정
```

```jsonc
{
  "configurations": [
    {
      "name": "Backend Debug (HTTP)",
      // dotnet build 후 → ASP.NET Core를 :5121에서 실행
      // DB 연결: Host=db ← compose의 db 컨테이너를 hostname으로 접근
    },
    {
      "name": "Frontend Debug (HTTP)",
      // next dev를 :3000에서 실행
      // API 프록시: DEV_PROXY_ORIGIN=http://localhost:5121
    }
  ],
  "compounds": [
    {
      "name": "Full Stack Debug (HTTP)",
      // 위 두 개를 동시에 실행
    }
  ]
}
```

---

## 프로젝트의 2가지 Docker 환경

이 프로젝트에는 **2가지 독립적인 Docker 환경**이 있다.

### 개발 환경: `.devcontainer/` (VS Code에서 코딩할 때)

VS Code "Reopen in Container"로 열면 작동하는 환경.

`.devcontainer/docker-compose.yml` → 2개 서비스:

| 서비스 | 역할 |
|--------|------|
| `workspace` | .NET 10 SDK 기반. VS Code가 여기에 접속해서 코딩. Node 22 + Docker CLI는 devcontainer features로 추가 설치 |
| `db` | PostgreSQL 16. 개발용 DB (`portfolio/portfolio/portfolio`) |

### 프로덕션 시뮬레이션: 루트 `docker-compose.yml`

devcontainer **안에서** `docker compose up`을 실행하면 작동하는 환경 (Docker-outside-of-Docker 덕분에 호스트 Docker 사용).

루트 `docker-compose.yml` → 4개 서비스:

| 서비스 | 이미지/빌드 | 역할 |
|--------|-------------|------|
| `frontend` | 루트 `Dockerfile` (Next.js standalone 빌드) | 프론트엔드 프로덕션 서버 (:3000) |
| `backend` | `backend/Dockerfile` (.NET publish) | API 서버 (:8080) |
| `db` | postgres:16-alpine | DB |
| `nginx` | nginx:1.27-alpine | 리버스 프록시 (:80→frontend/backend) |

오버레이 파일:
- `docker-compose.https.yml`: HTTPS 모드 추가 (nginx에 443 포트 + 인증서 마운트)
- `docker-compose.codex.yml`: Codex AI 도구 연동용 볼륨 마운트

---

## 전체 흐름 타임라인

```
[1] "Reopen in Container" 클릭
         │
[2] devcontainer.json 읽기
         │
[3] docker-compose.yml로 컨테이너 생성
         ├─ workspace 컨테이너 (Ubuntu + .NET SDK)
         └─ db 컨테이너 (PostgreSQL)
         │
[4] workspace에 features 설치 (Node.js 22, Docker CLI)
         │
[5] postCreateCommand 실행 (npm install, dotnet restore)
         │
[6] VS Code가 workspace 컨테이너에 접속 완료
    (이제 터미널 = 컨테이너 안, 파일 탐색기 = 마운트된 소스코드)
         │
[7] F5 누르면 → launch.json 읽고 → 컨테이너 안에서 앱 실행
         ├─ Backend :5121  (DB는 "db"라는 hostname으로 접근)
         └─ Frontend :3000 (API는 localhost:5121로 프록시)
         │
[8] forwardPorts [3000, 5121] 덕분에
    호스트 브라우저에서 localhost:3000 접속 가능
```

---

## 앱 실행 방법

### 방법 1: Full Stack 한번에 (추천)

VS Code 디버그 패널 (Ctrl+Shift+D) → **"Full Stack Debug (HTTP)"** 선택 → **F5**

백엔드 + 프론트엔드가 동시에 뜬다:

| 구성 | 포트 | 동작 |
|------|------|------|
| Backend Debug (HTTP) | `:5121` | `dotnet build` 후 ASP.NET Core 실행, DB는 `db` 컨테이너 연결 |
| Frontend Debug (HTTP) | `:3000` | `next dev` 실행, API 요청을 `localhost:5121`로 프록시 |

### 방법 2: 따로 실행

- **백엔드만**: 디버그 패널에서 **"Backend Debug (HTTP)"** → F5
- **프론트엔드만**: 디버그 패널에서 **"Frontend Debug (HTTP)"** → F5

### 방법 3: 터미널에서 수동 실행

```bash
# 백엔드
cd backend/src/WoongBlog.Api
dotnet run --urls http://0.0.0.0:5121

# 프론트엔드 (별도 터미널)
npm run dev
```

---

## 비유로 정리

| 파일 | 비유 |
|------|------|
| `devcontainer.json` | 사무실 임대 계약서 (어떤 건물, 몇 층, 인테리어 어떻게) |
| `docker-compose.yml` | 건물 설계도 (방 몇 개, 각 방에 뭘 설치) |
| `launch.json` | 업무 매뉴얼 (사무실 들어가서 어떤 프로그램을 어떻게 실행) |

`devcontainer.json`이 `docker-compose.yml`을 **참조해서** 컨테이너를 만들고, 만들어진 환경 안에서 `launch.json`이 **앱을 실행**하는 관계이다.
