# C# WinForms Desktop 구현 정리

이 문서는 `RamosPartGenerator/csharp-desktop/RamosPartGenerator.Desktop`에 추가된 C# WinForms 데스크톱 앱의 동작 구조, 적용 룰, 구현 범위를 정리한다.

## 목적

C# WinForms Desktop 앱은 기존 HTML/Web UI 없이도 Windows 실행 프로그램 형태로 Ramos Part Generator를 사용할 수 있게 만든 별도 UI이다.

핵심 Part 생성, 파싱, 검증 로직은 새로 만들지 않고 기존 C# Core 프로젝트를 그대로 참조한다. 따라서 Web UI와 Desktop UI는 화면은 다르지만 Part Code 생성 결과는 같은 Core 로직을 기준으로 한다.

## 프로젝트 위치

```text
C:\RPG\RamosPartGenerator\csharp-desktop\RamosPartGenerator.Desktop
```

주요 파일:

- `RamosPartGenerator.Desktop.csproj`: WinForms 프로젝트 파일
- `Program.cs`: WinForms 앱 진입점
- `MainForm.cs`: Incoming/Comp, Module 화면과 공통 버튼/Export 처리
- `MainForm.Batch.cs`: Batch Generate MDL 일괄 화면과 상태 관리
- `MainForm.BatchComp.cs`: Batch Generate Comp/Comp_MDL 일괄 화면과 상태 관리
- `DesktopLookupCatalog.cs`: Desktop UI용 필드와 선택 옵션 구성
- `DisplayHelpers.cs`: 표시값과 실제 코드값 변환 helper

## 실행 형태

현재 표시 버전:

- Part Generator `v1.0.0`
- Spec Rev `30.4`

프로그램 제목, 상단 헤더, EXE 파일 속성, 시작 로그에 프로그램 버전을 표시한다. Spec Rev는 `rev30.json`의 `display_revision`을 사용한다.

Target framework:

```text
net7.0-windows
```

실행:

```powershell
cd C:\RPG\RamosPartGenerator
dotnet run --project csharp-desktop\RamosPartGenerator.Desktop\RamosPartGenerator.Desktop.csproj
```

빌드:

```powershell
cd C:\RPG\RamosPartGenerator
dotnet build csharp-desktop\RamosPartGenerator.Desktop\RamosPartGenerator.Desktop.csproj
```

별도 exe 출력:

```powershell
cd C:\RPG\RamosPartGenerator
dotnet build csharp-desktop\RamosPartGenerator.Desktop\RamosPartGenerator.Desktop.csproj -o csharp-desktop\RamosPartGenerator.Desktop\bin\BuildCheck
```

## 참조 구조

Desktop 앱은 아래 기존 프로젝트를 직접 참조한다.

```text
RamosPartGenerator.Desktop
├─ RamosPartGenerator.Core
└─ RamosPartGenerator.Excel
```

사용하는 Core/Excel 클래스:

- `SpecProvider`
- `ProductTextService`
- `IncomingCompService`
- `ModuleService`
- `BatchGenerationService`
- `RegistrationExcelExporter`

Desktop 앱은 Web API를 호출하지 않는다. HTML/Vite 프론트엔드도 사용하지 않는다.

## 데이터와 룰 기준

실행 중 사용하는 기준 데이터는 `specs` JSON이다.

```text
C:\RPG\specs\shared.json
C:\RPG\specs\rev30.json
```

`RamosPartGenerator.Desktop.csproj`에서 위 JSON 파일을 실행 폴더의 `specs` 하위로 복사한다.

```xml
<Content Include="..\..\..\specs\*.json" Link="specs\%(Filename)%(Extension)" CopyToOutputDirectory="PreserveNewest" />
```

런타임에는 `AppContext.BaseDirectory\specs`를 기준으로 `SpecProvider`가 JSON을 읽는다.

PDF 파일은 실행 중 직접 파싱하지 않는다. PDF는 규격 확인용 원본 자료이고, 앱 동작은 JSON 룰에 의해 결정된다.

## 화면 구성

Desktop 앱은 세 개의 탭으로 구성된다.

- `Incoming & Comp`
- `Module`
- `Batch Generate`

공통 동작:

- 그룹, 필드, 선택 옵션으로 구성된 3열 클릭 선택 방식
- 필드 선택과 옵션 변경 시 현재 필드 목록의 스크롤 위치 유지
- 표시값은 `코드 - 설명` 형태로 보여주고, Core 서비스 호출 시 실제 코드만 추출
- `Generate` 결과를 하단 테이블에 누적
- 결과 테이블의 셀 단위 다중/드래그 선택과 헤더 없는 `Ctrl+C` 복사
- `Delete Selected`로 선택 셀이 포함된 결과 행만 삭제
- `Export Excel` 클릭 시 현재 결과 row를 Excel로 저장
- Ramos 회사 컬러 기반 테마 적용

## Incoming & Comp 룰

### 주요 입력

- Comp Full Part
- Comp Source
- DRAM Type
- Density
- Bit
- Bank
- Interface
- Part Revision
- Comp Type
- Die Brand
- Vendor
- Purchaser
- Comp Type 2
- Package
- Tester

Die Brand는 `V - GIGA S1(SV)`, `P - GIGA S1(SP)`를 포함한다.

### 파싱

`Comp Full Part` 입력 후 `Parse` 클릭 시:

```csharp
IncomingCompService.ParseCompPart(revision, partCode)
```

결과는 화면 필드에 다시 표시된다.

### 생성

`Generate` 클릭 시:

```csharp
IncomingCompService.GeneratePreview(request)
```

생성 결과:

- Incoming row
- Comp row
- Comp BIN row

DDR5의 경우 `shared.json`의 `comp_bin_speed_map` 기준으로 여러 BIN speed row를 만든다.

### 화면 룰

DRAM Type에 따라 Density, Bank, Interface가 제한된다.

DDR4:

- DRAM Type: `A`
- Density: `4G`, `8G`, `AG`
- Bank: `5 - 16Bank`
- Interface: `W - POD 1.2V`

DDR5:

- DRAM Type: `R`
- Density: `AH`, `HE`, `BH`
- Bank: `6 - 32Bank`
- Interface: `V - POD 1.1V`

Source가 Third-party 계열일 때만 Purchaser를 입력할 수 있다.

Comp Source는 Comp 생성을 중심으로 표시하고, 함께 생성되는 Incoming Source를 보조 정보로 안내한다. Core 서비스에는 기존 Incoming Source 코드로 변환해서 전달한다.

- `RC - RAmos Memory (입고: K)`
- `TC - Ramos TP (입고: T)`
- `CC - CTST Memory (입고: C)`
- `BC - CTST TP (입고: B)`

Third-party Comp Source:

- `TC` (내부 Incoming Source `T`)
- `BC` (내부 Incoming Source `B`)

## Module 룰

### 주요 입력

- Comp Full Part
- Module Full Part
- Source
- DRAM Type
- DIMM Type
- Module Density
- Bank / VDD
- Die Density
- Composition
- Rank
- Generation
- I.C Brand
- Comp Type
- Comp Test Site
- SMT Site
- Module Test Site
- Speed
- PCB
- Vendor
- Purchaser
- A100 Special
- Special Code 2
- Special Code 3
- Grade Code
- Product Bin
- 완제품 Retest (00/0Y)

I.C Brand는 `V - GIGA S1(SV)`, `P - GIGA S1(SP)`를 포함한다.

### 파싱

Comp Full Part 파싱:

```csharp
ModuleService.ParseCompPart(revision, partCode)
```

Module Full Part 파싱:

```csharp
ModuleService.ParseModuleFullPart(revision, partCode)
```

Comp Full Part는 Module 기본값을 유도하는 용도로 사용한다. Module Full Part는 실제 Module 코드에서 필드값을 복원하는 용도로 사용한다. 두 번째 `-` 앞 구간이 `00` 또는 `0Y`로 끝나면 해당 2자리를 제외하고 필드를 파싱하며 `완제품 Retest (00/0Y)` 옵션을 자동으로 켠다.

### 생성

`Generate` 클릭 시:

```csharp
ModuleService.GeneratePreview(request)
```

생성 결과:

- Module row
- Module BIN row

`완제품 Retest (00/0Y)` 옵션을 켠 경우 생성 결과:

- `00` Module Dummy row (`Specification` 끝에 `Dummy` 추가)
- `0Y` Module row (기본 Module과 동일한 `Specification`)
- `0Y` Module BIN row

완제품 Retest는 기존 Module Repair Dummy 자동 생성보다 우선한다.

### 화면 룰

Module DRAM Type은 화면에서는 `A - DDR4`, `R - DDR5`로 보인다. Core 서비스에는 DDR4를 `4`, DDR5를 `R` 코드로 전달한다.

Speed 제한:

DDR4:

- `WE - 3200 MT/s`

DDR5:

- `QK - 4800 MT/s`
- `WM - 5600 MT/s`
- `CM - 6000 MT/s`
- `CA - 6000 MT/s (3000MHz @ 48/48/48)`
- `CQ - 6400 MT/s`
- `CR - 6800 MT/s`
- `CS - 7200 MT/s`

Bank/VDD 자동 계산:

- DDR4 + `WE` -> `4`
- DDR5 + `QK` or `WM` -> `5`
- DDR5 + `CM` or `CQ` -> `6`
- DDR5 + `CA` -> `8` (`32Bank / POD 1.25V`)
- DDR5 + `CR` or `CS` -> `7`

Third-party Module Source일 때만 Purchaser를 입력할 수 있다.

Third-party Module Source:

- `TM`
- `BM`

A100 Special은 아래 조건에서만 입력 가능하다.

- Third-party Module
- Vendor = `A`
- Purchaser = `A`

## Batch Generate 룰

`Batch Generate` 탭 안에서 `MDL 일괄`과 `Comp 일괄`을 전환한다.

MDL 일괄 동작:

- MDL Full Part를 한 줄에 하나씩 입력한다.
- 기본 PID, 기본 MFGID, Reball, 1차/2차 Repair, Reball Repair, 완제품 Retest를 선택할 수 있다.
- 원본 Comp 관련과 Reball Comp 관련은 작업 Part와 별도로 선택한다.
- 모든 옵션은 기본 미선택이다.
- `Generate`는 입력 Part의 기존 상태와 입력별 성공/실패, 생성 수, 메시지를 보여준다.
- 같은 동작에서 실제 생성 Part Code와 품목 정보를 하단 결과 테이블에 표시한다.
- 생성은 외부 시스템을 변경하지 않으므로 별도 `Preview` 버튼은 두지 않는다.
- 입력 하나가 실패해도 다른 입력의 정상 결과는 유지한다.
- 중복되는 최종 Part Code는 한 번만 표시한다.
- 1차 Repair와 완제품 Retest가 함께 선택된 경우 공용 `00` Dummy도 한 번만 생성한다.
- 일괄 결과의 선택 행 삭제와 Excel Export를 지원한다.

Comp 일괄 동작:

- Comp Full Part를 한 줄에 하나씩 입력한다.
- Incoming, Comp, Comp BIN 전체는 항상 생성한다.
- `Comp_MDL 생성`을 선택하면 Comp 판매용 MDL/MDL BIN을 추가한다.
- Comp_MDL Speed는 체크박스로 복수 선택하며 기본값을 자동 지정하지 않는다.
- 선택한 각 Speed마다 Comp_MDL과 MDL BIN을 생성한다.
- 우측 생성 옵션은 `Comp 관련`, `Comp_MDL` 그룹으로 구분하고 옵션 영역에 세로 스크롤을 적용한다.
- Reball Comp는 Reball Comp_MDL로 변환한다.
- Speed 누락/불일치 또는 Module Density 결정 불가 시 Comp 관련 결과는 유지하고 Comp_MDL 오류를 입력 분석에 표시한다.
- 중복 입력과 최종 Part Code는 한 번만 표시한다.
- Comp 일괄 결과의 선택 행 삭제와 Excel Export를 지원한다.

### DIMM Type

현재 `shared.json` 기준 DIMM Type 옵션:

- `D - UDIMM 288pin`
- `S - SODIMM 262pin`
- `G - x64 288pin GamingUDIMM (RGB)`
- `C - Comp`, Rev30 addition으로 추가 표시

`C - Comp` 선택 시 출하 Comp Module 기본값을 자동 입력한다.

- Module Density는 Comp Full Part의 Die Density를 GB로 환산 가능한 경우 자동 입력
- `Rank`, `SMT Site`, `Module Test Site`, `PCB`는 `0` 값 자동 선택
- `Speed`는 사용자가 선택

## Excel Export

Desktop 앱은 `RegistrationExcelExporter`를 직접 호출한다.

```csharp
RegistrationExcelExporter.Export(rows)
```

사용자가 `SaveFileDialog`에서 저장 위치를 고르면 `.xlsx` 파일로 저장한다.

Export 대상 row:

- 현재 화면 하단 결과 테이블에 누적된 rows

Export 표시 기준:

- 열 폭은 헤더와 데이터 중 가장 긴 텍스트를 기준으로 자동 계산한다.
- `품목규격` 열은 Excel 최대 열 폭까지 확장 가능하다.
- 본문 셀은 강제 줄바꿈하지 않는다.

결과 row가 없으면 Export하지 않고 오류 메시지를 표시한다.

## 구현 정도

| 항목 | 상태 | 비고 |
| --- | --- | --- |
| WinForms 프로젝트 생성 | 완료 | `csharp-desktop` 하위로 기존 프로젝트와 분리 |
| Core 로직 연결 | 완료 | `IncomingCompService`, `ModuleService` 직접 사용 |
| Spec JSON 로딩 | 완료 | 실행 폴더 `specs` 기준 |
| Incoming & Comp 화면 | 1차 완료 | Parse, Generate, Export, 선택 행 삭제, Reset 구현 |
| Module 화면 | 1차 완료 | Comp parse, Module parse, Generate, Export, 선택 행 삭제, Reset 구현 |
| Batch Generate MDL 화면 | 완료 | 다중 입력, 옵션 선택, Generate, Export, 선택 행 삭제, Reset 구현 |
| Batch Generate Comp 화면 | 완료 | Comp 다중 입력, Comp_MDL/Speed 복수 선택, Generate, Export, 선택 행 삭제, Reset 구현 |
| Excel Export | 완료 | 기존 exporter 재사용 |
| Ramos 색상 테마 | 1차 완료 | Header, Tab, Button, Table 색상 반영 |
| 필드 겹침/잘림 대응 | 1차 완료 | 상단 입력부와 필드 영역 layout 조정 |
| Web/API 없이 exe 실행 | 가능 | Desktop 앱은 API 호출 없음 |
| `.sln` 편입 | 미완료 | 현재 project path로 직접 빌드/실행 |
| Publish 패키징 | 완료 | 최상위 `Publish` 폴더에 self-contained 단일 exe 생성 |
| 설치 프로그램 | 미완료 | MSI/Setup 등은 아직 없음 |
| 전체 UI 수동 QA | 진행 필요 | Batch 탭 포함 긴 option text, 작은 창 크기, DPI 확대 확인 필요 |
| Desktop UI 자동 테스트 | 미구현 | 현재 Core 단위 테스트 중심 |

## 현재 주의사항

1. 앱 실행 중에는 exe/dll이 잠길 수 있다.

   같은 출력 폴더로 다시 빌드하려면 실행 중인 Desktop 앱을 먼저 닫아야 한다.

2. Desktop 앱은 Windows WinForms 기반이다.

   Windows 환경에서 실행해야 한다.

3. 화면 룰과 Core 검증 룰은 분리되어 있다.

   화면에서는 사용자가 고르기 쉽게 option을 제한하고, 최종 검증은 Core 서비스에서 다시 수행한다.

4. 규격 변경 시 JSON 갱신이 필요하다.

   PDF를 자동으로 읽지 않으므로, 규격이 바뀌면 `specs/shared.json` 또는 `specs/rev30.json`을 수정해야 한다.

5. 일부 Core 오류 메시지는 기존 소스의 문자열 상태를 따른다.

   필요하면 별도 작업으로 사용자 표시용 오류 메시지를 정리할 수 있다.

## 남은 작업 제안

우선순위 높은 작업:

- 작은 화면 / 고 DPI / 긴 텍스트 기준 UI 재점검
- Core 오류 메시지 한글 인코딩 및 문구 정리

배포 단계 작업:

- 설치 프로그램 또는 압축 배포 패키지 구성
