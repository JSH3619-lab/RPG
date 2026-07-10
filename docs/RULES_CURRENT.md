# 현재 확정 규칙

원본 문서는 아래 두 파일을 기준으로 한다.

- `RAMOS_DRAM_PART (Rev.30.3).pdf`
- `RAMOS_DRAM_TM_PART (Rev 2.1).pdf`

## 지원 Rev
- 현재 운영 키: `30`
- 표시 기준: DRAM PART `Rev.30.3`, TM PART `Rev 2.1`

## 기본 원칙
- LPDDR 계열은 범위에서 제외한다.
- 품목명은 품목코드와 동일하게 생성한다.
- COO 기본값은 `KR`이다.
- PDF는 원본 확인용이고, 실행 규칙은 `specs/shared.json`, `specs/rev30.json`, Core 코드, API/Frontend 순서로 반영한다.

## 입고/Comp 규칙
- 입고 Part, Comp Part, Comp BIN을 함께 생성한다.
- TM PART도 입고 Part, Comp Part, Comp BIN을 함께 생성한다.
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
- TM manufacturing family: 입고 `X`, `Z`; Comp `XC`, `ZC`; Module `XM`, `ZM`
- Rev 30에서는 Third-party 계열에 `Purchaser`가 필수다.
- Internal family에서는 Third-party 전용 필드를 비활성화한다.
- TM manufacturing 계열은 `Purchaser`를 비워 두고 Vendor는 `X - RAMBO`만 사용한다.

## Rev 30 주요 차이
- 입고/Comp에서는 `Vendor`와 `Purchaser`를 분리한다.
- Module에서는 `I.C Brand`와 `Comp Type`을 분리한다.
- Rev 30 Module의 `DIMM Type`에는 `Comp`가 포함된다.
- Rev 30 Module의 `Rank`에는 `0 : Comp`가 포함된다.

## Rev 30.3 / TM Rev 2.1 확인 차이
- DRAM PART Rev.30.3의 Product Bin에는 `B00 : H/T (Hammer Test)`가 포함된다.
- DRAM PART Rev.30.3의 Module Density에는 `CG : 64GB`가 포함된다.
- TM PART Rev 2.1의 입고 Source는 `X : RAmos TM`, `Z : CTST TM`이다.
- TM PART Rev 2.1의 Comp Source는 `XC : RAmos I.C TM`, `ZC : CTST I.C TM`이다.
- TM PART Rev 2.1의 Module Source는 `XM : Ramos Module TM`, `ZM : CTST Module TM`이다.
- TM PART Rev 2.1의 Comp Type은 `0`부터 `7`까지의 제조 공정 코드다.
- TM PART Rev 2.1에는 Bit `48 : x8 (x4 -> x8)` 및 Module Composition `9 : x8 (x4 -> x8)`가 추가된다.
- Comp Type 2에는 `1 : Reball / EMC`가 포함된다.

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

## Module 완제품 Retest 규칙
- Module 화면의 `완제품 Retest (00/0Y)` 옵션은 기본적으로 꺼져 있다.
- 옵션을 켜면 기본 MDL/MDL BIN 대신 다음 3개 행을 순서대로 생성한다.
  - 기본 MDL 코드 끝에 `00`을 붙인 `MDL Dummy`; 품목규격 끝에 `Dummy`를 붙인다.
  - 기본 MDL 코드 끝에 `0Y`를 붙인 `MDL`; 품목규격은 접미사가 없는 기본 MDL과 같다.
  - `0Y` MDL 뒤에 BIN suffix를 붙인 `MDL BIN`.
- `00`과 `0Y`는 두 번째 `-` 앞 구간의 마지막 2자리로 판별한다.
- `00` 또는 `0Y` Part를 파싱하면 완제품 Retest 옵션을 자동으로 켠다.
- 기존 Module Repair Dummy 조건과 동시에 적용되면 완제품 Retest를 우선하고 별도의 Repair Dummy 행은 추가하지 않는다.

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
- I.C Brand 표기는 `S -> S1`, `G -> GIGA S1`, `V -> GIGA S1(SV)`, `P -> GIGA S1(SP)`, `H -> S2`, `M -> S3`, `C -> S6`, `N -> S9`를 사용한다.

## 출하 Comp Module 규칙
- Module 생성에서 `DIMM Type = C - Comp`를 선택하면 Comp 판매용 Module로 처리한다.
- `Comp Full Part`에서 가져온 Die Density가 GB로 환산 가능한 경우 Module Density를 자동 입력한다.
  - `8Gb -> 1GB`, `16Gb -> 2GB`, `32Gb -> 4GB`
- 환산 가능한 Module Density 코드가 없으면 자동 입력하지 않는다.
- `Rank`, `SMT Site`, `Module Test Site`, `PCB`는 `0` 값을 자동 입력한다.
- `Speed`는 자동 선택하지 않고 사용자가 선택한다.

## Excel Export 규칙
- 등록용 Excel의 열 폭은 헤더와 데이터 중 가장 긴 텍스트를 기준으로 Export 시점에 자동 계산한다.
- `품목규격` 열은 긴 규격을 한 줄로 볼 수 있도록 Excel 최대 열 폭까지 확장 가능하다.
- 본문 셀에는 강제 줄바꿈을 적용하지 않는다.

## 빠른 입력 규칙
- `Comp Full Part`, `Module Full Part` 직접 입력은 필드 자동 채움 보조 기능이다.
- 직접 입력 후에도 길이, 구분자, 허용 코드, Rev별 필드 구조, Third-party 필수값을 검증한다.
