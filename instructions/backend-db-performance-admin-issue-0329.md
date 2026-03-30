공개 조회 서비스들은 자산 테이블을 통째로 메모리로 가져오는 패턴이 반복된다.

PublicHomeService
PublicWorkService
PublicBlogService
데이터가 작을 때는 버틸 수 있지만, 자산 수가 늘면 불필요한 메모리 사용과 조회 비용이 커진다.

또한 DatabaseBootstrapper 는 재시도 로직이 있지만 실패 원인을 남기지 않고 넘기는 구간이 있어 운영 추적성이 떨어진다.

개선 방향:

필요한 Asset 만 projection/join 으로 가져오기
bootstrap 실패 원인 로깅
무음 실패 제거