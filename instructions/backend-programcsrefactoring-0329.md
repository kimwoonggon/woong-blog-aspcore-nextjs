 
› $ralplan 나는 "Program.cs 에 설정 해석, 옵션 보정, 인증 구성, 쿠키 이벤트, DB 초기화, 미들웨어 배
  치, static files, OpenAPI, endpoint 매핑이 모두 들어 있 다. 문제는 단순히 길다는 것이 아니라, 운영
  정책과 조립 코드가 한 파일에 섞여 있다는 점이다. 이 상태에서는 인증 정책이나 배포 정책 변경이 있을
  때 Program.cs 가 계속 비대해진다."라는 점을 가지고 있다. 이것을 refactoring 해야 한다. 여기에 설치
  된 skill들 (dotnet backend patterns, design patterns), 너의 생각 등을 활용해서 리팩토링 계획을 먼저
  세워보아라.