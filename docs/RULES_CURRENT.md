# 현재 확정 규칙

원본 문서는 아래 두 파일을 기준으로 한다.

- `RAMOS_DRAM_PART (Rev.30.6).pdf`
- `RAMOS_DRAM_TM_PART (Rev 2.1).pdf`

## 지원 Rev
- 현재 운영 키: `30`
- 표시 기준: DRAM PART `Rev.30.6`, TM PART `Rev 2.1`

## Rev.30.6 반영 차이
- Comp Dram Type에 `S : DDR5 RDIMM`을 추가한다. 생성 규칙은 DDR5(`R`)와 동일하게 적용한다 (Density AH/HE/BH, Bank 6, Interface V, Comp BIN CA~CF).
- Module DIMM Type에 `R : x80 288pin Registered DIMM`을 추가한다. RDIMM 전용 규칙은 아직 없다.
- Module Density에 `3G : 24GB`, `6G : 48GB`, `DG : 128GB`를 추가한다. 96GB는 코드 확정 전이라 보류한다.
- Tester `S : PTS (Package test Stock)`는 `S : No-Test`와 코드가 충돌하여 미반영한다 (PDF 정정 대기).
- LPDDR 계열(신규 K5 입고 Comp 포함)은 계속 범위에서 제외한다.

## 기본 원칙
- LPDDR 계열은 범위에서 제외한다.
- 품목명은 품목코드와 동일하게 생성한다.
- COO 기본값은 `KR`이다.
- PDF는 원본 확인용이고, 실행 규칙은 `specs/shared.json`, `specs/rev30.json`, Core 코드, API/Frontend 순서로 반영한다.

## 입고/Comp 규칙
- 입고 Part, Comp Part, Comp BIN을 함께 생성한다.
- TM PART도 입고 Part, Comp Part, Comp BIN을 함께 생성한다.
- DDR4는 `-CA` BIN 1개만 생성한다.
- DDR5(R)는 `-CA`부터 `-CF`까지 생성한다.

## RDIMM(S) Comp BIN 규칙
- RDIMM Comp는 RDIMM 상위빈 `EA~EF`를 생성한다. 속도 매핑은 `CA~CF`와 동일하다 (EA=7200 … EF=4800 MT/s).
- `CA~CF`는 RDIMM ATE FAIL 시 UDIMM으로 쓰기 위한 구제빈이며, UDIMM 조립이 가능한 x8(`08`)일 때만 생성한다.
- x4 RDIMM은 UDIMM/SODIMM으로 만들 수 없어 상위빈 `EA~EF`만 생성한다 (FAIL은 폐기).
- 품목규격은 `EA~EF`에만 RDIMM을 표기한다: `EA~EF`=`DDR5 RDIMM …`, `CA~CF`=`DDR5 …`.
- 입고 Part는 RDIMM을 구분하지 않고 `R`(DDR5)로 생성한다.
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

## Rev 30.4 / TM Rev 2.1 확인 차이
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
- DDR5 Speed는 `QK`, `WM`, `CM`, `CA`, `CQ`, `CR`, `CS`를 허용한다.
- Bank/VDD 매핑:
  - DDR4 `WE` -> `16Bank / 1.2V`
  - DDR5 `QK`, `WM` -> `32Bank / 1.1V`
  - DDR5 `CM`, `CQ` -> `32Bank / 1.35V`
  - DDR5 `CA` -> `32Bank / 1.25V`
  - DDR5 `CR`, `CS` -> `32Bank / 1.4V`
- `CA`는 `DDR5-6000 (3000MHz @ 48/48/48)` 조건이다.

## Module 완제품 Retest 규칙
- Module 화면의 `완제품 Retest (00/0Y)` 옵션은 기본적으로 꺼져 있다.
- 옵션을 켜면 기본 MDL/MDL BIN 대신 다음 3개 행을 순서대로 생성한다.
  - 기본 MDL 코드 끝에 `00`을 붙인 `MDL Dummy`; 품목규격 끝에 `Dummy`를 붙인다.
  - 기본 MDL 코드 끝에 `0Y`를 붙인 `MDL`; 품목규격은 접미사가 없는 기본 MDL과 같다.
  - `0Y` MDL 뒤에 BIN suffix를 붙인 `MDL BIN`.
- `00`과 `0Y`는 두 번째 `-` 앞 구간의 마지막 2자리로 판별한다.
- `00` 또는 `0Y` Part를 파싱하면 완제품 Retest 옵션을 자동으로 켠다.
- 기존 Module Repair Dummy 조건과 동시에 적용되면 완제품 Retest를 우선하고 별도의 Repair Dummy 행은 추가하지 않는다.

## Module 일괄 생성 규칙
- 기본 PID와 기본 MFGID는 각각 선택해서 생성한다.
- Reball, 1차 Repair, 2차 Repair, Reball Repair, 완제품 Retest는 필요한 작업만 선택한다.
- Repair 작업 선택은 Incoming/Comp/Comp BIN 생성을 자동으로 포함하지 않는다.
- Comp 관련은 `원본 Comp 관련`, `Reball Comp 관련` 두 종류를 별도로 선택한다.
- 원본 Comp 관련은 Comp Type 2를 비우고, Reball Comp 관련은 Comp Type 2에 `B`를 사용한다.
- 1차 Repair Dummy와 완제품 Retest Dummy의 `{기본 MDL}00`은 같은 Part다.
- 1차 Repair와 완제품 Retest를 동시에 생성할 때 `{기본 MDL}00`은 한 번만 생성하고 품목규격 끝은 `Dummy`로 통일한다.
- 2차 Repair Dummy `R0`와 Reball Repair Dummy `B0`는 기존 개별 규칙을 유지한다.
- 지원하지 않는 MDL→Comp 변환 조합은 임의로 대체하지 않고 해당 생성 실패로 처리한다.

## Comp 일괄 생성 규칙
- Comp Full Part마다 Incoming, Comp, Comp BIN 전체를 생성한다.
- `Comp_MDL 생성`을 선택한 경우에만 Comp 판매용 MDL과 MDL BIN을 추가한다.
- Comp_MDL은 `DIMM Type = C - Comp`를 사용한다.
- Module Density는 Base Die Density 기준으로 `8Gb -> 1GB`, `16Gb -> 2GB`, `32Gb -> 4GB`를 적용한다.
- `Rank`, `SMT Site`, `Module Test Site`, `PCB`는 `0`을 사용한다.
- Comp Type 2가 `B - Reball`이면 Reball Comp_MDL로 생성한다.
- Comp_MDL Speed는 자동 선택하지 않고 사용자가 복수 선택한다.
- 선택한 각 Speed마다 Comp_MDL과 MDL BIN 한 쌍을 생성한다.
- Speed가 없거나 입력과 맞지 않거나 Module Density를 결정할 수 없으면 임의 값을 사용하지 않고 Comp_MDL만 실패 처리한다.
- Comp_MDL 실패 시에도 정상 생성된 Incoming/Comp/Comp BIN 결과는 유지한다.

## Special Code 1 규칙 (구 A100 Special)
- Rev.30.6부터 `Only A100` 게이팅을 폐지하고 조건부 테이블을 적용한다.
- `Vendor = A(A100)` 그리고 `Purchaser = A(ADATA)`이면 Table 1을 적용한다.
  - `0(Blank) : MPS PMIC + Renesas SPD`, `1 : Richtek + Renesas`, `2 : Richtek + Montage`, `X : N/A`
- 그 외 모든 경우 Table 2를 적용한다.
  - `0(Blank) : anpec + anpec`, `A : Richtek / Montage / Montage Gen2 RCD / Montage TS`, `B : Rambus / Rambus / Rambus Gen2 RCD / Rambus TS`, `X : N/A`
- Comp 판매용 MDL(DIMM Type `C`)은 SPD류 BOM이 없으므로 Special Code 1을 `X(N/A)`로 자동 입력한다.
- Comp Test Site는 Table 판정에 사용하지 않는다.
- 파싱 시 마지막 한 글자가 Special Code 1/2 양쪽에 존재하는 코드(예: `B`)면 기존 파트 호환을 위해 Special Code 2로 해석한다.

## 품목 텍스트 규칙
- DRAM / Comp / Comp BIN의 품목일반정보는 비운다.
- 일반 Module 품목일반정보: `{UDIMM|SODIMM} {용량} COO : KR` (Gaming DIMM Type `G`도 `UDIMM`으로 표기, RDIMM `R`은 `RDIMM`)
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

## ERP 등록 파트 중복 체크 규칙
- ERP에서 다운로드한 `품목정보등록(Multi)(S).xlsx`를 `ERP 품목 업로드` 버튼으로 업로드한다.
- A열 `품목코드`를 마지막 헤더 행 이후부터 전량 파싱하고, 생성 파트와 **완전 일치**로 비교한다.
- 파싱 결과는 실행 폴더의 `erp-part-cache.json`에 저장되어 재실행 후에도 유지된다. 새 파트 등록 후에만 재업로드하면 된다.
- 생성 그리드의 `상태` 컬럼에 `등록됨` / `신규`를 표시한다.
- Export 시 `등록됨` 파트는 기본 제외하며, `중복 포함` 체크박스로 예외 진행할 수 있다.
- 캐시가 없으면 중복 표시·필터링 없이 기존과 동일하게 동작한다.
- PGM이 Export한 파트코드는 `pgm-exported-cache.json`에 누적되어, ERP 스냅샷과 합쳐서 `등록됨`으로 판정한다. ERP 재업로드 없이도 이미 내보낸 파트를 잡아낸다(같은/이전 세션 포함).

## Excel Export 규칙
- 등록용 Excel의 열 폭은 헤더와 데이터 중 가장 긴 텍스트를 기준으로 Export 시점에 자동 계산한다.
- `품목규격` 열은 긴 규격을 한 줄로 볼 수 있도록 Excel 최대 열 폭까지 확장 가능하다.
- 본문 셀에는 강제 줄바꿈을 적용하지 않는다.

## 스펙 코드 옵션 편집 규칙
- 헤더 `스펙 편집` 버튼으로 필드 옵션셋(`code_options`)의 코드를 추가/수정/삭제한다.
- 저장은 `{실행폴더}/specs/shared.json`의 해당 옵션 배열만 부분 수정한다. 단일 exe라 specs가 없으면 첫 저장 시 현재 스펙을 디스크로 실체화한다.
- 저장/복원 때마다 `specs/backup/{타임스탬프}/` 스냅샷을 남기고 최근 20개만 유지한다. `백업에서 복원…`으로 특정 시점 specs 전체로 되돌린다.
- 저장/복원 후 재시작 없이 Incoming/Module 드롭다운에 즉시 반영한다.
- 검증은 형식(`코드 - 설명`)과 옵션셋 내 코드 중복만 확인한다. 생성 로직이 특정 코드에 의존하는 조건 규칙(예: Speed→Bank/VDD)은 옵션을 바꿔도 자동으로 따라오지 않으므로 코드 추가에 우선 사용한다.
- PDF는 읽기 전용 원본이며 편집기가 PDF를 수정하지 않는다.

## 빠른 입력 규칙
- `Comp Full Part`, `Module Full Part` 직접 입력은 필드 자동 채움 보조 기능이다.
- 직접 입력 후에도 길이, 구분자, 허용 코드, Rev별 필드 구조, Third-party 필수값을 검증한다.
