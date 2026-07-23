# Part 규칙 관리 UI 구현 계획

## 목적

새로운 Part Code, Speed, Bank/VDD 같은 규칙을 코드 수정과 재빌드 없이 화면에서 추가·수정·비활성화한다.

현재 `specs/shared.json`, `specs/rev30.json`이 규칙 DB 역할을 하고 있으므로 별도 SQLite DB를 바로 도입하지 않고 JSON 기반 관리 계층을 추가한다.

## 권장 저장 구조

- 기본 규칙: EXE에 포함된 `shared.json`, `rev30.json`
- 사용자 변경분: `%LocalAppData%\RamosPartGenerator\config\custom-specs.json`
- 백업: `%LocalAppData%\RamosPartGenerator\config\backup\yyyyMMdd-HHmmss.json`

프로그램은 기본 규칙을 읽은 다음 사용자 변경분을 덮어써서 최종 규칙을 구성한다. 기본 규칙 파일은 보존하고 사용자가 추가하거나 변경한 항목만 별도 저장한다.

## 관리 대상 모델 예시

MDL Speed:

- Code
- DRAM Type
- Data Rate
- Clock
- Timing
- 연결 Bank/VDD Code
- 사용 여부
- 표시 순서

Bank/VDD:

- Code
- Bank
- Voltage
- 사용 여부
- 표시 순서

삭제는 실제 데이터 삭제 대신 `사용 안 함`으로 처리한다. 기존 Part 재현과 변경 이력 확인을 위해 비활성 항목도 보관한다.

## 관리 화면

메인 화면에 `Rule Management` 버튼을 추가하고 별도 창에서 관리한다.

- 탭: MDL Speed, Bank/VDD, 기타 코드
- 표: Code, 설명, 연결 규칙, 사용 여부
- 기능: 추가, 수정, 비활성화, 복원
- 저장 전 변경 내용 비교
- JSON Import/Export
- 기본값으로 되돌리기

Speed 편집 시 연결할 Bank/VDD를 드롭다운으로 선택한다. 하나의 화면에서 Speed 코드와 Bank/VDD 매핑을 함께 확인할 수 있게 한다.

## 저장 검증

- Code 중복 금지
- 필수값 누락 금지
- DRAM Type별 Speed 허용값 확인
- Speed가 참조하는 Bank/VDD Code 존재 확인
- 사용 중인 Bank/VDD를 먼저 비활성화하지 못하도록 차단
- 저장 전 대표 Part Code 생성 미리보기
- 검증 성공 후 임시 파일 작성 및 원본 교체
- 저장 전 자동 백업

## 버전과 이력

- 규칙 버전 표시
- 최종 변경 일시
- 변경 사유 입력
- 추가/수정/비활성화 내역 기록
- 프로그램 화면에서 현재 적용 중인 기본 버전과 사용자 규칙 버전 표시

## 구현 Phase

### A. 규칙 Overlay Core

- `custom-specs.json` 모델
- 기본 규칙과 사용자 변경분 병합
- 중복/참조/필수값 검증
- 단위 테스트

### B. 관리 화면 읽기 기능

- Rule Management 창
- Speed와 Bank/VDD 목록/검색
- 기본 규칙과 사용자 규칙 구분 표시

### C. 편집과 저장

- 추가, 수정, 비활성화, 복원
- 원자적 저장과 자동 백업
- 저장 전 Part Code 미리보기

### D. 운영 기능

- Import/Export
- 변경 이력과 규칙 버전
- 현재 적용 규칙 표시
- 전체 회귀 테스트

## 권장 범위

첫 구현은 MDL Speed와 Bank/VDD 두 항목만 대상으로 한다. 구조가 검증된 후 Density, Vendor, Tester 같은 다른 코드 테이블로 확장한다.
