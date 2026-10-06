# Changelog

## 1.5.2 - 2026-10-06

- (2026-10-06 추가) PDF Rev.32.1 반영 (Mining): Module Special Code 2와 Comp(DDD/DLK) Comp Type 2에 `M : Mining` 추가. Reball과 같은 프로세스로 처리(Dummy 없음, Module↔Comp 변환 시 `M` 유지). 끝자리 `M` 단독은 Mining, `MM`은 Mining + RMA(자산)로 파싱
- (2026-10-06 추가) Batch Generate에 `Mining`(작업 Part), `Mining Comp 관련`(Comp 관련) 체크박스 추가, 입력 파트 감지 상태에 Mining 표시
- (2026-10-06 추가) 화면 Spec 표시 Rev 31.0 → 32.1. MCP 등 Rev.32.1 기타 신규 항목은 미반영
- 단일 생성 탭(Incoming/Module/LPDDR) Generate 반복 시 같은 파트가 결과 표에 계속 쌓이던 문제 수정: 표에 이미 있는 Part Code는 추가하지 않고 상태줄에 `목록 중복 N건 제외` 표시. 기등록 경고 팝업도 실제 추가된 행 기준
- 팀 공용 운용 전환: 공유 폴더 `PGM\DRAMPartGenerator\`의 exe 하나를 함께 실행하고, ERP 업로드·PGM Export 이력(`erp-part-cache.json`, `pgm-exported-cache.json`)을 exe 폴더에 저장해 사용자 간 공유
- Export 이력 저장 시 파일을 다시 읽어 합친 뒤 저장(다른 사용자 이력 보존), 임시 파일 저장 후 교체(저장 중 단절 시 손상 방지)
- Generate·Export 직전에 ERP 캐시·Export 이력 파일이 바뀌었으면 다시 읽어 다른 사용자의 Export 결과를 즉시 반영
- 기존 사용자별 저장 위치(`%LocalAppData%\RamosPartGenerator\`)의 이력은 실행 시 exe 폴더로 자동 병합, ERP 캐시는 exe 폴더에 없을 때만 복사

## 1.5.1 - 2026-09-23

- (2026-10-01 추가) PDF Rev.31.0 재수령분 반영: Speed `TD`(DDR4-2666, Bank/VDD 4), `CP`(DDR5-6400 52/52/52, Bank/VDD 6) 추가, Special Code 1 Table 2 공백 설명에 DDR4(PMIC 없음 + Fudan SPD) 병기
- (2026-10-01 추가) Special Code 1 Table 2를 DIMM Type별로 제한: R→A/B, D/S/G→(None) 고정, C→X 고정. 화면 목록 거르기 + Core 검증(Batch 포함). A100(Table 1)은 수기 선택 유지
- (2026-10-01 추가) Module Full Part Parse 후 입력창 비움: 창에 값이 남아 필드 수정값이 생성에 반영되지 않던 문제 수정
- Batch Generate(MDL 일괄·Comp 일괄) 결과 표에 ERP 등록 대조 `상태` 열(등록됨/신규) 추가: 단일 생성 탭과 동일하게 ERP 스냅샷 + PGM Export 이력 기준으로 `등록됨` 표시
- Batch Generate에도 `중복 포함` 체크박스 추가: 미체크 시 Export에서 등록된 파트 자동 제외 (기존엔 Batch Export가 항상 전체 포함)
- RDIMM 품목규격 IC 수를 ECC 포함 80-bit 기준으로 계산: x8 32GB 16→20, x4 64GB 32→40. UDIMM/SODIMM/Comp는 64-bit 유지, 용량 계산은 변경 없음

## 1.5.0 - 2026-08-19

- LPDDR 파트 생성 지원 (PDF Rev.31.0): 새 LPDDR 탭에서 입고 + Product 베이스 + Product BIN 3개를 체인으로 생성. 현재 운용대로 입고가 Product(RP)로 바로 연결되며 Comp(RD)는 추후 변경 전까지 생성하지 않음
- 입고 형태 2종 지원: PKG 입고(K5/T5/C5/B5, 기본) / Die 입고(K4/T4/C4/B4, Die Density·Die Bit 별도 입력)
- Product(`RP/TP/CP/BP`)는 몸통에 조립/테스트/Speed/PCB Rev 포함. DRAM Module처럼 베이스 파트와 `-TN{Density}A00` BIN 파트를 함께 생성 (BIN은 Normal 1종)
- PKG 입고 시 Comp 조립 업체 기본 `0 : No Ass'y`, Third-party(T/B)는 Purchaser 필수
- ERP 기등록 LPDDR4 파트 3건(B5F1G324H1M-P*) 재현 테스트로 검증. 기등록 품목명의 `W`(Whole) 표기는 코드 `P`(Partial)와 불일치한 등록 오류로 확인 — 품목명=품목코드 원칙 유지
- LPDDR 탭도 ERP 중복 체크·Export 공용 흐름 사용
- LPDDR Product(RP)는 MDL 파트로 취급: Excel 내보내기에 영업코드 열 포함 (기존 MDL과 동일하게 빈칸으로 생성, 수기 기입), 품목일반정보도 Comp 판매용 MDL 패턴으로 생성 (`LPDDR4 Comp 2GB COO : KR`)
- LPDDR Full Part 역파싱: 코드 붙여넣기 → 계열(K4/K5 입고, RD Comp, RP Product) 자동 판별 후 필드 복원. 코드에 없는 필드(K5의 조립/테스트/Speed 등)는 비워두고 직접 선택 (RP의 `-TN…` BIN 태그는 재생성 시 자동 부여라 파싱 생략)
- 일반 Module 품목일반정보에 DDR 타입 접두 추가: `UDIMM 16GB COO : KR` → `DDR5 UDIMM 16GB COO : KR` (Comp 판매용 MDL은 기존 유지)
- ERP 파트 인덱스·PGM 등록 원장 저장 위치를 실행 폴더에서 `%LocalAppData%\RamosPartGenerator\`로 이동: 새 버전 빌드를 받아도 ERP 업로드·등록 파트가 초기화되지 않음 (첫 실행 시 기존 exe 폴더 cache 자동 이전)
- 표시 리비전 31.0으로 갱신 (PDF Rev.31.0 반영)

## 1.4.1 - 2026-08-13

- Comp 판매용 MDL(DIMM Type C) 품목규격에 A100 표기 누락 수정: Third-party + Vendor A + Purchaser A이면 규격에 `A100` 표기(그 외 third-party는 `TP`) — 일반 모듈/입고와 일치

## 1.4.0 - 2026-08-12

- ERP 중복 판정에 PGM 자체 생성 이력을 추가: Export한 파트코드를 `pgm-exported-cache.json`에 누적하고, ERP 스냅샷과 합쳐서 `등록됨` 판정
- ERP 재업로드 없이도 이미 내보낸 파트를 `등록됨`으로 인식 (같은/이전 세션 포함). 별도 상태 없이 기존 `등록됨`으로 병합
- 상태 라벨에 생성 이력 건수 표시

## 1.3.3 - 2026-08-12

- Gaming(DIMM Type G) 모듈의 품목일반정보·규격 표기를 `UDIMM`으로 변경 (코드 문자 `G` 대신). 예: `G 16GB COO : KR` → `UDIMM 16GB COO : KR`

## 1.3.2 - 2026-08-12

- 전체 필드 드롭다운 설명을 DRAM PART PDF(Rev.30.6) 원문에 맞춰 정리 (Sourcing, DRAM Type, Density, Interface, Comp Type, Die Brand, Vendor, Package, Tester, DIMM Type, Module Source, I.C Brand)
- 규격 생성 로직에는 영향 없음(규격 라벨은 별도 하드코딩). Speed는 규격에 직접 쓰이므로 기존 표기 유지, TM PART 계열과 Bank/VDD POD 표기는 기존 방침 유지

## 1.3.1 - 2026-08-12

- PCB 필드 드롭다운 설명을 PDF 원문 그대로 표기 (예: `6 - BrainPower(BP) PCB (Green)`, `7 - BrainPower(BP) PCB (Black)`)
- 품목규격의 PCB 표기에 색상 추가 (예: `BP PCB` → `BP PCB(Black)`, `DN PCB(Green)` 등)
- PCB `5` 규격 표기를 실제 값(`HJ PCB(Black, 11x11)`)으로 정정, Hammer는 `Hammer Pass`/`Hammer Fail`로 표기
- 버전 관리: Publish는 최신 버전만 보관, 릴리스는 git 태그(`v1.0.0`~)로 아카이브

## 1.3.0 - 2026-08-12

- RDIMM(S) Comp에 RDIMM 상위빈 `EA~EF` 생성 추가 (속도 매핑은 `CA~CF`와 동일: EA=7200 … EF=4800)
- `CA~CF`는 UDIMM 구제빈으로, UDIMM 조립이 가능한 x8일 때만 생성. x4 RDIMM은 `EA~EF`만 생성(UDIMM 불가)
- 품목규격 구분: `EA~EF`=`DDR5 RDIMM …`, `CA~CF`=`DDR5 …`(RDIMM 미표기)

## 1.2.0 - 2026-08-12

- 스펙 코드 옵션 편집기 추가: 헤더 `스펙 편집` 버튼 → 필드 옵션셋(code_options) 코드 추가/수정/삭제, `specs/shared.json`에 부분 저장
- 저장 시마다 `specs/backup/{타임스탬프}/` 스냅샷 자동 생성(최근 20개 유지), `백업에서 복원…`으로 시점 복원
- 단일 exe에서 specs가 없으면 첫 저장 시 현재 스펙을 디스크로 실체화하여 기준점 생성
- 저장/복원 후 재시작 없이 Incoming/Module 드롭다운에 즉시 반영
- 이미 등록된 파트 인식 강화: 결과 표에서 `등록됨` 행을 주황색으로 하이라이트하고, 생성 시 등록된 파트가 있으면 경고창으로 알림

## 1.1.0 - 2026-08-12

- Rev.30.6 규격 반영: Comp Dram Type `S - DDR5 RDIMM`, Module DIMM Type `R - x80 288pin RDIMM`, Module Density `3G/6G/DG` 추가, Spec Rev 표시 `30.6`
- Special Code 1 개편: A100 전용 게이팅 폐지, Vendor/Purchaser 조건에 따른 Table 1(A100+ADATA) / Table 2(일반) 전환, `X - N/A` 추가
- Comp 판매용 MDL(DIMM Type C)에 Special Code 1 `X` 자동 입력
- 입력 필드 순서를 파트 체계 순서와 일치하도록 재배열 (Incoming Package/Tester 위치, Module Composition/Die Density·Speed 위치)
- ERP 등록 파트 중복 체크: `품목정보등록(Multi)(S).xlsx` 업로드, 영구 캐시, 생성 그리드 `등록됨/신규` 표시, Export 시 중복 기본 제외 + `중복 포함` 옵션
- 필드 그룹명을 파트 코드 구역 기준으로 변경(Spec / Comp · Source / Process / Source · BIN), 필드명 정리(Bit Org, Test Site, Base Die Density)

## 1.0.2 - 2026-07-23

- Module A100 판정과 A100 Special 활성 조건에서 Comp Test Site 제외
- Third-party Module의 Vendor와 Purchaser가 모두 `A`이면 Comp Test Site와 관계없이 A100으로 처리

## 1.0.1 - 2026-07-23

- Bank/VDD `8` 표시에서 `POD` 문구 제거
- 현재 지원하는 모든 MDL Speed에 Clock과 Timing 정보 추가
- MDL Speed/Bank UI 필터를 JSON 규칙 기반으로 변경
- Module 화면에 A100 Special 활성 조건 안내 추가

## 1.0.0 - 2026-07-23

첫 프로그램 버전 관리 기준점.

- Incoming/Comp 및 MDL Part 생성과 파싱
- MDL Repair, Reball, 완제품 Retest 생성
- MDL/Comp 일괄 생성
- Comp_MDL 복수 Speed 생성
- 등록용 Excel Export
- 고객사 MDL Speed `CA - DDR5-6000 (48/48/48)` 추가
- Bank/VDD `8 - 32Bank / POD 1.25V` 추가
- Spec Rev 표시를 `30.4`로 변경
