# Ramos Part Generator 다음 세션 인수인계

최종 정리일: 2026-07-21

## 작업 원칙

- 현재 실행/개발 대상은 C# WinForms Desktop, Core, Excel exporter, specs, tests다.
- C# WinForms Desktop, Core, Excel exporter, specs, tests만 작업한다.
- exe 생성, 커밋, 푸시는 사용자가 명시적으로 요청한 경우에만 진행한다.
- C# 전체 리뷰, 최적화, 리팩토링, 영향 범위 확인 작업은 code graph review MCP가 사용 가능하면 먼저 사용한다.
- 커밋 시 빌드 산출물, publish 폴더, Excel export 결과 파일은 포함하지 않는다.

## 실행/검증 명령

개발 실행:

```powershell
dotnet run --project RamosPartGenerator/csharp-desktop/RamosPartGenerator.Desktop/RamosPartGenerator.Desktop.csproj
```

전체 테스트:

```powershell
dotnet test RamosPartGenerator/RamosPartGenerator.sln
```

데스크톱 빌드 확인:

```powershell
dotnet build RamosPartGenerator/csharp-desktop/RamosPartGenerator.Desktop/RamosPartGenerator.Desktop.csproj
```

실행 중인 exe가 잠겨 있으면 별도 출력 폴더로 빌드 확인:

```powershell
dotnet build RamosPartGenerator/csharp-desktop/RamosPartGenerator.Desktop/RamosPartGenerator.Desktop.csproj -o RamosPartGenerator/csharp-desktop/RamosPartGenerator.Desktop/bin/Verify/net7.0-windows
```

단일 exe publish:

```powershell
dotnet publish RamosPartGenerator/csharp-desktop/RamosPartGenerator.Desktop/RamosPartGenerator.Desktop.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=None -p:DebugSymbols=false -o Publish
```

## 현재 배포 구조

- WinForms 프로젝트: `RamosPartGenerator/csharp-desktop/RamosPartGenerator.Desktop`
- 배포 산출물: `Publish/RamosPartGenerator.Desktop.exe`
- exe 하나만 배포 가능하도록 Core의 `specs/*.json`은 embedded resource로 포함한다.
- 외부 `specs` 폴더가 있으면 우선 사용하고, 없으면 embedded specs로 fallback한다.
- exe 아이콘은 `RamosPartGenerator/csharp-desktop/RamosPartGenerator.Desktop/Assets/RamosPartGenerator.ico`를 사용한다.

## 운영 로그

- 로그 위치: `%LocalAppData%\RamosPartGenerator\logs\yyyyMMdd.log`
- 예: `C:\Users\<사용자>\AppData\Local\RamosPartGenerator\logs\20260510.log`
- 로그 형식: `[yyyy-MM-dd HH:mm:ss.fff] [LEVEL] Event ...`
- 기록 대상: 프로그램 시작/종료, spec 로딩, 모드 전환, Comp/MDL 파싱, 생성, Export, 예외
- 보관 정책: 최근 7일 로그만 보관하며, 프로그램 실행 중 첫 로그 기록 시 오래된 `.log` 파일을 자동 삭제한다.
- 유지보수용으로 PartCode, 모드, 결과 row 수, 에러 메시지 중심으로 남기고 전체 입력값 덤프는 피한다.

## 최근 반영된 주요 규칙

### UI/운영

- Incoming/Comp 화면의 Part Mode 라디오 버튼은 `Standard`, `TM` 표기를 사용한다.
- Incoming/Comp의 `Comp Full Part` 입력창과 `Parse` 버튼은 Module 화면과 같은 행 배치로 맞췄다.
- Incoming/Comp의 `Source`는 `Comp Source`로 표시하고, Comp 기준 `RC/TC/CC/BC`와 보조 입고 코드 `K/T/C/B`를 함께 안내한다.
- TM Source도 Comp 기준 `XC/ZC`를 표시하고 내부 생성에는 입고 코드 `X/Z`를 전달한다.
- 결과 테이블은 셀 단위 다중 선택과 드래그 선택을 지원하며, `Ctrl+C` 복사 시 헤더를 포함하지 않는다.
- `Delete Selected`는 선택 셀이 포함된 결과 행만 삭제하고, 기존 `Reset`은 입력과 전체 결과를 초기화한다.
- 로그 시간은 한국 로컬 사용 기준으로 timezone offset 없이 `[2026-05-10 10:25:16.536]` 형식으로 기록한다.

### 공통 표시/Export

- 생성창/결과창/Export의 주요 컬럼은 한글 기준으로 정리했다.
- `PartCode`는 `품목코드`, `NAME`은 `품목명`, `Generalinfo`는 `품목일반정보`, `Specification`은 `품목규격` 의미다.
- 비고 필드는 코드와 결과창에서 제거했다.
- Export 파일명 기본값은 `DRAM 품목정보(yyMMdd).xlsx` 형식이다.
- Export 글꼴은 Arial이다.
- Export 열 폭은 헤더와 데이터 중 가장 긴 텍스트 기준으로 자동 계산한다.
- `품목규격` 열은 Excel 최대 열 폭까지 확장 가능하며, 본문 셀은 강제 줄바꿈하지 않는다.
- MDL 행이 포함된 Export에는 `품목명`과 `품목일반정보` 사이에 `영업코드` 컬럼을 추가한다.
- 결과/Export 구분 표시는 `Comp`, `Comp BIN`, `MDL`, `MDL BIN`, `MDL Dummy`를 사용한다.

### Incoming/Comp

- Part Revision 위치 오류를 수정해서 `TCRAH086VP-GBGWGH`는 `P-die`로 나온다.
- Comp Type 2 `B`는 `Reball`로 표기한다.
- Reball은 Incoming, Comp 기본행, Comp BIN 행 모두 품목규격에 붙는다.
- BIN 행에서는 Reball이 속도 바로 앞에 붙는다.
- A100 Incoming/Comp 규칙:
  - Vendor `A` + Purchaser `A` + Third-party 조건이면 `A100`으로 표기한다.
  - A100은 `TP` 표현을 쓰지 않는다.
  - 품목규격 순서는 `Gen`, `A100`, `I.C Brand`, `Comp Type`, `Comp`, `Reball`, `Speed` 기준이다.
- I.C Brand 표기는 `S -> S1`, `G -> GIGA S1`, `V -> GIGA S1(SV)`, `P -> GIGA S1(SP)`, `H -> S2`, `M -> S3`, `C -> S6`, `N -> S9`만 사용한다.

### Module

- MDL Composition 선택 옵션은 규격서 기준으로 `4 - x4`, `8 - x8`, `6 - x16`을 표시한다.
- DDR4/DDR5에 맞지 않는 Speed, Bank/VDD, Die Density 조합은 막는다.
- Module Density, Die Density, Composition, Rank 조합이 계산상 맞지 않으면 생성하지 않는다.
- IC count 표기는 `64 / compositionWidth * rankCount` 기준이다.
- A100 Module 규칙:
  - Third-party Module에서 `Comp Test Site = A`, `Vendor = A`, `Purchaser = A`가 모두 맞을 때만 `A100`으로 표기한다.
  - Purchaser만 `A`이거나 Vendor/Purchaser만 `A`인 경우는 A100이 아니며 Source 기준 표기를 사용한다.
  - A100은 `TP` 표현을 쓰지 않는다.
- Third-party Module Source 기준 표기:
  - `TM`은 `RAmos TP`
  - `BM`은 `CT TP`
  - Purchaser 값은 이 표기를 바꾸지 않는다.
- PCB 규격 표기:
  - Brain Power 계열은 `BP PCB`
  - HJ/선진 계열은 `HJ PCB`
  - `AD5U8C0(ADATA/BP)`도 규격에는 `BP PCB`
- Module 품목규격은 용량/Comp 개수 괄호 뒤에 I.C Brand를 붙인다.
- I.C Brand 표기는 `S -> S1`, `G -> GIGA S1`, `V -> GIGA S1(SV)`, `P -> GIGA S1(SP)`, `H -> S2`, `M -> S3`, `C -> S6`, `N -> S9`만 사용한다.

### 출하 Comp Module

`DIMM Type = C - Comp`는 Comp 판매용 Module로 처리한다.

- `Comp Full Part`에서 가져온 Die Density가 GB로 환산 가능한 경우 Module Density를 자동 입력한다.
  - `8Gb -> 1GB`, `16Gb -> 2GB`, `32Gb -> 4GB`
- 환산 가능한 Module Density 코드가 없으면 자동 입력하지 않는다.
- `Rank`, `SMT Site`, `Module Test Site`, `PCB`는 `0` 값을 자동 입력한다.
- `Speed`는 자동 선택하지 않고 사용자가 선택한다.

### Module Repair Dummy

Module Full Part에서 Special Code 2가 특정 값이면 기본 MDL 행 뒤에 `MDL Dummy` 행을 추가한다.

- `R` (`1st Repair`):
  - 예: `BMRDAG58A1A-CPARRWMAAAR`
  - Dummy code: `BMRDAG58A1A-CPARRWMAAA00`
  - 규격 끝: `Dummy`
- `S` (`2nd Repair`):
  - 예: `BMRDAG58A1A-CPARRWMAAAS`
  - Dummy code: `BMRDAG58A1A-CPARRWMAAAR0`
  - 규격 끝: `2nd Repair Dummy`
- `C` (`Reball Repair`):
  - 예: `BMRDAG58A1A-CPARRWMAAAC`
  - Dummy code: `BMRDAG58A1A-CPARRWMAAAB0`
  - 규격 끝: `Reball Repair Dummy`
- `B` (`Reball`)은 Dummy를 만들지 않는다.
- 이 규칙은 A100 한정이 아니라 전체 Module에 적용한다.
- 1차 Repair의 `00` Dummy는 완제품 Retest의 `00` Dummy와 같은 Part로 사용한다.

### Module 완제품 Retest

- Module 화면에 `완제품 Retest (00/0Y)` 체크박스를 추가했다.
- 옵션을 켜면 기본 MDL/MDL BIN 대신 다음 순서로 3개 행을 생성한다.
  - `{기본 MDL}00`: `MDL Dummy`, 규격 끝에 `Dummy`
  - `{기본 MDL}0Y`: `MDL`, 기본 MDL과 동일한 규격
  - `{기본 MDL}0Y-{BIN suffix}`: `MDL BIN`
- 예: `RMRDAG58A1P-GPWRRWM7G`는 `00`, `0Y`, `0Y-TNAGA00` 형태로 생성한다.
- Parse는 두 번째 `-` 앞 구간의 마지막 2자리로 `00/0Y`를 판별하고 옵션을 자동으로 켠다.
- 기존 Repair Dummy 조건과 겹치면 완제품 Retest를 우선하며 별도 Repair Dummy 행은 생성하지 않는다.

## UI 안정화 메모

- 입력 UI는 그룹, 필드, 선택 옵션의 3열 `ListBox` 클릭 방식이다.
- 필드와 옵션 상태만 바뀐 경우 필드 목록을 다시 만들지 않고 다시 그려 스크롤 위치를 유지한다.
- 그룹이 바뀌어 표시할 필드 구성이 달라질 때만 필드 목록 항목을 재구성한다.

## 테스트 기준

현재 기준 전체 테스트는 83개다.

주요 테스트 범위:

- DDR5 Comp BIN 생성
- Part Revision 기반 die label
- A100 Incoming/Comp 규격
- Module A100/PCB/IC Brand 규격
- Module Repair Dummy 생성/미생성
- Module 완제품 Retest 생성/파싱 및 Repair Dummy 우선순위
- MDL 일괄 기본 PID/MFGID와 작업 Part 조합 생성
- 1차 Repair/완제품 Retest 공용 `00` 중복 제거
- MDL→원본/Reball Incoming, Comp, Comp BIN 변환
- 일괄 입력 중복 제거와 입력별 오류 격리
- Comp Full Part 일괄 Incoming/Comp/Comp BIN 생성
- Comp_MDL 선택 생성, Speed 복수 적용, Reball 변환
- Comp_MDL Speed 누락과 Module Density 결정 불가 시 부분 성공 처리
- Export `영업코드`, Arial, `MDL Dummy`, 자동 열 폭
- embedded specs fallback

## Batch Generate 진행 상황

- Phase 1 Core 구현 완료
  - `BatchGenerationService`
  - `MdlBatchOptions`
  - `BatchGenerationResult`, `BatchItemResult`
  - MDL 작업 Part 조합 및 원본/Reball Comp 관련 생성
  - Part Code 중복 제거와 입력별 오류 처리
- Phase 2 Desktop MDL 일괄 UI 구현 완료
  - 메인 화면의 `Batch Generate` 탭
  - MDL Full Part 줄 단위 다중 입력
  - 기본 PID/MFGID, 작업 Part, 원본/Reball Comp 관련 체크 옵션
  - `Generate` 한 번으로 입력별 감지/처리 상태와 실제 생성 Part 목록 표시
  - Batch 결과 전용 선택 행 삭제와 Excel Export
  - 모든 옵션 기본 미선택, 별도 Preview 버튼 없음
- Phase 3 Comp 일괄/Comp_MDL UI 구현 완료
  - `Batch Generate` 내부 `MDL 일괄 / Comp 일괄` 전환
  - Comp Full Part 줄 단위 다중 입력
  - Incoming/Comp/Comp BIN 항상 생성
  - Comp_MDL 선택 생성과 Speed 복수 체크
  - Reball Comp의 Reball Comp_MDL 변환
  - 확정할 수 없는 값은 추정하지 않고 입력별 오류 처리
- 상세 범위는 `docs/BATCH_GENERATION.md`를 기준으로 한다.

Phase 3 보완 코드 빌드와 전체 83개 테스트는 통과했다. 자동 UI 실행 도구는 시간 초과되었으므로 배포 EXE에서 Batch 탭의 실제 화면 배치와 DPI별 표시는 수동 확인한다.

## 커밋/배포 주의

- `Publish/`는 배포 산출물이라 커밋하지 않는다.
- `DRAM 품목정보*.xlsx`는 Export 결과 파일이라 커밋하지 않는다.
- `.code-review-graph/`는 MCP 로컬 산출물이라 커밋하지 않는다.
- exe 생성 후 산출물 확인은 `Get-ChildItem Publish`로 한다.
