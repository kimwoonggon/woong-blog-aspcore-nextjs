현재 ContentJson, AllPropertiesJson 은 문자열 JSON 으로 저장된다. 이것 자체가 반드시 나쁜 건 아니지만, 현재는 schema 검증이 거의 없고 파 싱 실패를 조용히 무시하는 코드가 있다.

예를 들어 AdminContentJson 은 잘못된 JSON 을 만나도 빈 값으로 처리한다. 이 방식은 운영 장애를 늦게 발견하게 만든다.

개선 방향:

최소한 입력 schema validation 추가
가능하면 typed DTO 또는 value object 로 축소
파싱 실패는 로깅 또는 validation failure 로 드러내기
기타 내가 생각 못한 점

이것을 refactoring 해야 한다. 여기에 설치
  된 skill들 (dotnet backend patterns, design patterns), 너의 생각 등을 활용해서 리팩토링 계획을 먼저
  세워보아라.