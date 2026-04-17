# Web Architecture Plan

## 목표
- `Rev 27`, `Rev 30` 기준의 반자동 Part 생성기를 웹으로 재구성한다.
- 현재 C# `Core`, `Excel`, `specs/*.json` 자산은 유지한다.
- WinForms UI는 더 이상 확장하지 않는다.
- DB는 두지 않는다.
- 사내망 로컬 환경에서 실행하는 것을 기본으로 한다.

## 기본 원칙
- 규칙 정본:
  - 사람용: `docs/RULES_CURRENT.md`
  - 프로그램용: `specs/shared.json`, `specs/rev27.json`, `specs/rev30.json`
- PDF는 원본 확인이 필요할 때만 다시 본다.
- 프론트는 상태 관리와 입력 UX만 담당한다.
- 백엔드는 규칙 해석, 검증, 생성, Excel 출력만 담당한다.

## 권장 스택
- Backend: `ASP.NET Core Web API`
- Frontend: `React + TypeScript`
- Rule source: `specs/*.json`
- Export: `RamosPartGenerator.Excel`
- DB: 없음

## 디렉터리 구조
```text
RamosPartGenerator/
├── backend/
│   ├── RamosPartGenerator.Api
│   ├── RamosPartGenerator.Core
│   ├── RamosPartGenerator.Excel
│   └── specs/
├── frontend/
│   ├── src/
│   │   ├── app/
│   │   ├── components/
│   │   ├── features/
│   │   │   ├── incoming-comp/
│   │   │   └── module/
│   │   ├── services/
│   │   ├── types/
│   │   └── utils/
│   └── package.json
└── docs/
    ├── RULES_CURRENT.md
    └── WEB_ARCHITECTURE_PLAN.md
```

## 백엔드 역할
### Core
- `Rev 27`, `Rev 30` 규칙 로딩
- Incoming / Comp / Module 생성
- BIN 생성
- 품목명 / 품목일반정보 / 품목규격 생성
- Full Part 파서
- 입력 검증

### Api
- 드롭다운/직접입력용 lookup 데이터 제공
- preview/generate API 제공
- parse API 제공
- export API 제공
- 프론트 에러 메시지 표준화

### Excel
- 전송용 Excel 파일 생성
- 사내 등록 양식 출력

## 프론트 역할
- `Rev 27 / Rev 30` 선택
- `입고 & Comp`, `Module` 탭
- 드롭다운 + 직접입력 혼용 UI
- 잘못된 코드/길이/조합의 즉시 검증
- Full Part 입력 후 필드 자동 채움
- 결과 표 렌더링
- Excel 다운로드

## 페이지 구조
### 입고 & Comp
- 상단:
  - Spec Rev
  - Comp Full Part
  - Parse
- 본문:
  - 공통 입력
  - Comp 전용 입력
- 하단:
  - Generate
  - Reset
  - 결과 테이블

### Module
- 상단 1행:
  - Spec Rev
  - Comp Full Part
  - Parse
- 상단 2행:
  - Module Full Part
  - Parse
- 본문:
  - Module base
  - Structure
  - Output / Special
- 하단:
  - Generate
  - Reset
  - 결과 테이블

## API 초안
### Meta / Lookup
- `GET /api/meta/revisions`
- `GET /api/lookups/incoming/{revision}`
- `GET /api/lookups/module/{revision}`

### Incoming & Comp
- `POST /api/incoming-comp/preview`
- `POST /api/incoming-comp/parse`

### Module
- `POST /api/module/preview`
- `POST /api/module/parse-comp`
- `POST /api/module/parse-full`

### Export
- `POST /api/export/registration`

## 진행 순서
### Phase 1
- API 프로젝트 생성
- `SpecProvider`를 API에서 로딩
- lookup / preview 엔드포인트 구현

### Phase 2
- Incoming / Comp parser 구현
- Module parser 구현
- 프론트 시작

### Phase 3
- React UI 구성
- 드롭다운 + 직접입력 혼용
- 조건부 비활성 / 즉시 검증

### Phase 4
- Excel export 구현
- 예시 파트 기준 회귀 검증

## 운영 기준
- DB는 두지 않는다.
- ERP / MES가 최종 정본이다.
- 이 앱은 생성 / 검증 / 전송용 Excel 출력만 담당한다.
- 규칙 변경 시 순서는 아래를 따른다.
  1. `docs/RULES_CURRENT.md`
  2. `specs/*.json`
  3. `Core`
  4. `Api`
  5. `Frontend`
