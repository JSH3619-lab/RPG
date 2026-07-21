# 일괄 생성 구현 진행 상황

최종 갱신일: 2026-07-21

## 확정 범위

Desktop 메인 화면에 `Batch Generate` 탭을 추가하고 내부에서 `MDL 일괄`, `Comp 일괄`을 전환한다.

MDL 일괄 선택 항목:

- 기본 PID
- 기본 MFGID
- Reball
- 1차 Repair
- 2차 Repair
- Reball Repair
- 완제품 Retest (`00/0Y`)
- 원본 Comp 관련
- Reball Comp 관련

Comp 관련 생성은 Repair 작업과 독립적이다. Repair 또는 Dummy 생성만 선택한 경우 Incoming/Comp/Comp BIN을 자동 생성하지 않는다.

## 공용 `00` 규칙

1차 Repair Dummy와 완제품 Retest Dummy는 같은 `{기본 MDL}00` Part를 사용한다.

- 두 작업을 동시에 선택해도 `00`은 한 번만 생성한다.
- 품목규격의 상태 표기는 `Dummy`로 통일한다.
- 1차 Repair의 `R`, `R-MFGID`와 완제품 Retest의 `0Y`, `0Y-MFGID`는 각각 생성한다.

## Comp 관련 규칙

원본 Comp 관련:

- Incoming
- Comp
- Comp BIN 전체
- Comp Type 2는 Normal

Reball Comp 관련:

- Reball Incoming
- Reball Comp
- Reball Comp BIN 전체
- Comp Type 2는 `B - Reball`

MDL에서 Comp로 변환할 때 다음 값을 자동 적용한다.

- Source: `RM→RC/K`, `TM→TC/T`, `CM→CC/C`, `BM→BC/B`, `XM→XC/X`, `ZM→ZC/Z`
- Package Type: `B - FBGA(Flip Chip)`
- DDR4 Bank/Interface: `5/W`
- DDR5 Bank/Interface: `6/V`
- Comp Density: DRAM Type과 Base Die Density 기준

지원하지 않는 조합은 추정하거나 대체하지 않고 해당 생성만 실패 처리한다.

## Phase 진행 상황

| Phase | 범위 | 상태 |
| --- | --- | --- |
| 1 | Core 일괄 생성, MDL→Comp 변환, 중복/행별 오류 처리, 단위 테스트 | 완료 |
| 2 | `Batch Generate` 탭과 MDL 일괄 화면 | 완료 |
| 3 | Comp 일괄, Comp_MDL, Speed 선택 | 대기 |
| 4 | Export, 로그, 문서, 버전, 전체 검증 | 대기 |

## Phase 1 구현

Core에 다음 항목을 추가했다.

- `BatchGenerationService`
- `MdlBatchOptions`
- `BatchGenerationResult`, `BatchItemResult`
- 입력 Part의 기존 작업 상태 감지
- 입력과 최종 Part Code 중복 제거
- 입력 하나의 오류가 전체 일괄 생성을 중단하지 않는 행별 결과 처리
- 기본 PID/MFGID 및 작업 Part 선택 생성
- 원본/Reball Comp 관련 선택 생성

Phase 1 완료 기준 전체 테스트는 76개다.

## Phase 2 구현

Desktop 메인 화면에 `Batch Generate` 탭과 MDL 일괄 화면을 추가했다.

- MDL Full Part를 한 줄에 하나씩 다량 입력
- 기본 PID/MFGID, 작업 Part, 원본/Reball Comp 관련 옵션 선택
- 모든 생성 옵션은 기본 미선택
- `Generate`에서 입력별 감지 상태, 처리 상태, 생성 수, 메시지 확인
- 일부 입력이 실패해도 정상 입력의 예상 결과 유지
- `Generate`에서 실제 생성 Part Code와 품목 정보를 생성 결과 테이블에 표시
- 일괄 생성 결과만 선택 행 삭제 및 Excel Export
- 입력 또는 옵션 변경 시 다시 `Generate`하도록 안내
- `Reset`에서 입력, 선택 옵션, 분석 결과, 생성 결과 초기화

Repair/Dummy 옵션은 Comp 관련 옵션을 자동 선택하지 않는다. 원본 Comp와 Reball Comp는 각각 명시적으로 선택한 경우에만 생성한다.

별도의 `Preview` 단계는 두지 않는다. 이 프로그램의 생성은 외부 시스템 등록이 아니라 결과 표시이므로 `Generate` 자체가 생성 결과 확인 단계다.

Phase 2 완료 기준 전체 테스트는 76개이며 Desktop 프로젝트 빌드에 성공했다. 실제 창 자동 실행 도구가 시간 초과되어 최종 화면 배치와 DPI별 표시는 배포 EXE에서 수동 확인이 필요하다.
