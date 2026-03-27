1. $ralplan nextjs skill과 aspnet skill을 활용해서 guideline에 맞기 백엔드와 프론트를 개선한다. git branch를 적절히 써서 피처를 개선할때마다
브랜치에서 개발하고 테스트수행후 문제없으면 local의 매인에 merge하는 전략으로 간다. nextjs는 server side와 client side가 문제없이 잘 나뉘고 테스트까지 되어야 하며 aspcore는 guideline에 나온대로 minimal api 기반으로 잘 만들면서 openai api까지 제공해야한다. 계획을 짜줘

2. https://github.com/kimwoonggon/woong-notion-supabase-migration 에 예시가 있음. 여기서 코드를 이해한다음에 내 notion 개인페이지에 있는 내용을 게시물별로 이미지 파일을 싹 받아다가 현재 내 홈페이지 blogs에 데이터를 마이그레이션 하고 싶음 (이 브랜치를 받은다음에 gitignore에 넣고 참고만 하면서 계획 세우고 싶음)
  - notion에서 필요한 키는 env에 제공 예정 (.env에 NOTIONMCP 참고)
  - mcp 서버 config.toml에 연동해 두었음
  - 전기간 올라와있는 글을 다 노션 api를 통해 받는게 좋음

3. 다운 받은 게시물들이 사진 글 제목 빠짐없이 현재 홈페이지의 blogs에 마이그레이션 되어야 함. 추후에 수정은 내가 할 거임
4. 현재 홈페이지에 개발되어있는 ai fix generator를 모든 blog에 batch, each로 적용할 수 있는 gpt 등 api 연동 기반 ai fix generator 기능 개발. admin page에서 글들을 체크해서 ai fix generator를 글들을 한번에 적용할 수 있는 기능도 개발.
5. openai codex를 subprocess로 해서 api를 대신해서 저 글들을 입력하면 gpt처럼 api call 하는 것처럼 처리해주는 기능 개발