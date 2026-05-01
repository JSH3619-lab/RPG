# 현재 확정 규칙

원본 문서는 `RAMOS_DRAM_PART (Rev.30).pdf`만 기준으로 한다.

## 지원 Rev
- 현재 운영: `30`
- UI에는 `30`만 노출한다.

## 기본 원칙
- LPDDR 계열은 범위에서 제외한다.
- 품목명은 품목코드와 동일하게 생성한다.
- COO 기본값은 `KR`이다.
- PDF는 원본 확인용이고, 실행 규칙은 `specs/shared.json`, `specs/rev30.json`, Core 코드, API/Frontend 순서로 반영한다.

## 입고/Comp 규칙
- 입고 Part, Comp Part, Comp BIN을 함께 생성한다.
- DDR4는 `-CA` BIN 1개만 생성한다.
- DDR5는 `-CA`부터 `-CF`까지 생성한다.
- Comp BIN 속도:
  - `CA = 7200 MT/s`
  - `CB = 6800 MT/s`
  - `CC = 6400 MT/s`
  - `CD = 6000 MT/s`
  - `CE = 5600 MT/s`
  - `CF = 4800 MT/s`

## Third-party 규칙
- Third-party family: `TC`, `BC`, `TM`, `BM`
- Internal family: `RC`, `CC`, `RM`, `CM`
- Rev 30에서는 Third-party 계열에 `Purchaser`가 필수다.
- Internal family에서는 Third-party 전용 필드를 비활성화한다.

## Rev 30 주요 차이
- 입고/Comp에서는 `Vendor`와 `Purchaser`를 분리한다.
- Module에서는 `I.C Brand`와 `Comp Type`을 분리한다.
- Rev 30 Module의 `DIMM Type`에는 `Comp`가 포함된다.
- Rev 30 Module의 `Rank`에는 `0 : Comp`가 포함된다.

## Module 규칙
- Module은 `Comp Full Part`를 해석해 공통 정보를 먼저 채울 수 있다.
- 이후 사용자가 `DIMM Type`, `Module Density`, `PCB` 등 Module 전용 항목을 결정한다.
- DDR4 Speed는 현재 `WE`만 허용한다.
- DDR5 Speed는 `QK`, `WM`, `CM`, `CQ`, `CR`, `CS`를 허용한다.
- Bank/VDD 매핑:
  - DDR4 `WE` -> `16Bank / 1.2V`
  - DDR5 `QK`, `WM` -> `32Bank / 1.1V`
  - DDR5 `CM`, `CQ` -> `32Bank / 1.35V`
  - DDR5 `CR`, `CS` -> `32Bank / 1.4V`

## A100 규칙
- `Only A100` 계열 필드는 아래 조건에서만 사용할 수 있다.
  - Third-party family
  - `Vendor = A`
  - `Purchaser = A`

## 품목 텍스트 규칙
- DRAM / Comp / Comp BIN의 품목일반정보는 비운다.
- 일반 Module 품목일반정보: `{UDIMM|SODIMM} {용량} COO : KR`
- Comp 판매용 Module 품목일반정보: `{DDR타입} Comp {용량} COO : KR`
- DRAM / Comp / Comp BIN 품목규격은 `Comp Type` 설명을 포함한다.
- Module / Module BIN 품목규격은 Module 관점으로 작성하고 `Comp Type` 설명은 넣지 않는다.

## 빠른 입력 규칙
- `Comp Full Part`, `Module Full Part` 직접 입력은 필드 자동 채움 보조 기능이다.
- 직접 입력 후에도 길이, 구분자, 허용 코드, Rev별 필드 구조, Third-party 필수값을 검증한다.
