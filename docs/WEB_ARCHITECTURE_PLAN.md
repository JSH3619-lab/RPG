# Web Architecture Plan

## 목표
- Rev 30 기준 RAMOS Part 생성기를 웹 기반 로컬 앱으로 운영한다.
- C# Core, Excel export, `specs/*.json` 자산을 유지한다.
- WinForms UI는 더 이상 확장하지 않는다.
- DB 없이 사내 로컬 환경에서 실행하는 것을 기본으로 한다.

## 기본 원칙
- 규칙 정본:
  - 운영 문서: `docs/RULES_CURRENT.md`
  - 프로그램 규칙: `specs/shared.json`, `specs/rev30.json`
- 프론트엔드는 입력 UX와 상태 관리만 담당한다.
- 백엔드는 규칙 해석, 검증, 생성, Excel 출력을 담당한다.

## 권장 스택
- Backend: ASP.NET Core Web API
- Frontend: React + TypeScript
- Rule source: `specs/*.json`
- Export: `RamosPartGenerator.Excel`
- DB: 없음

## Backend 역할
- Rev 30 규칙 로딩
- Incoming / Comp / Module 생성
- BIN 생성
- 품목명 / 품목일반정보 / 품목규격 생성
- Full Part 파서
- 입력 검증
- 등록용 Excel 생성

## API
- `GET /api/status`
- `GET /api/meta/revisions`
- `GET /api/lookups/incoming/{revision}`
- `GET /api/lookups/module/{revision}`
- `POST /api/incoming-comp/preview`
- `POST /api/incoming-comp/parse`
- `POST /api/module/preview`
- `POST /api/module/parse-comp`
- `POST /api/module/parse-full`
- `POST /api/export/registration`

## Frontend 역할
- Incoming & Comp 화면
- Module 화면
- 드롭다운 + 직접 입력 병행
- Full Part parse 후 필드 자동 채움
- 조건부 비활성화와 즉시 검증 메시지 표시
- 결과 테이블 누적
- Excel 다운로드

## 페이지 구조
### Incoming & Comp
- Spec Rev
- Comp Full Part 입력 및 Parse
- 공통 입력
- Comp 전용 입력
- Generate / Reset / Export Excel
- 결과 테이블

### Module
- Comp Full Part 입력 및 Parse
- Module Full Part 입력 및 Parse
- Module Base 입력
- Structure 입력
- Output / Special 입력
- Generate / Reset / Export Excel
- 결과 테이블

## 운영 기준
- localhost 단독 실행을 기본으로 한다.
- ERP / MES가 최종 정본이며, 이 앱은 품목 생성, 검증, 전송용 Excel 출력만 담당한다.
- 규칙 변경 순서:
  1. `docs/RULES_CURRENT.md`
  2. `specs/*.json`
  3. `Core`
  4. `Api`
  5. `Frontend`
