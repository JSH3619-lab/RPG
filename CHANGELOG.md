# Changelog

## 1.1.0 - 2026-08-12

- Rev.30.6 규격 반영: Comp Dram Type `S - DDR5 RDIMM`, Module DIMM Type `R - x80 288pin RDIMM`, Module Density `3G/6G/DG` 추가, Spec Rev 표시 `30.6`
- Special Code 1 개편: A100 전용 게이팅 폐지, Vendor/Purchaser 조건에 따른 Table 1(A100+ADATA) / Table 2(일반) 전환, `X - N/A` 추가
- Comp 판매용 MDL(DIMM Type C)에 Special Code 1 `X` 자동 입력
- 입력 필드 순서를 파트 체계 순서와 일치하도록 재배열 (Incoming Package/Tester 위치, Module Composition/Die Density·Speed 위치)
- ERP 등록 파트 중복 체크: `품목정보등록(Multi)(S).xlsx` 업로드, 영구 캐시, 생성 그리드 `등록됨/신규` 표시, Export 시 중복 기본 제외 + `중복 포함` 옵션

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
