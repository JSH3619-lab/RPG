# Changelog

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
