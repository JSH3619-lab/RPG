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
- `MainForm.cs`: 전체 화면, 버튼 이벤트, 필드 상태 관리
- `DesktopLookupCatalog.cs`: Desktop UI용 필드와 선택 옵션 구성
- `DisplayHelpers.cs`: 표시값과 실제 코드값 변환 helper

## 실행 형태

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

Desktop 앱은 두 개의 탭으로 구성된다.

- `Incoming & Comp`
- `Module`

공통 동작:

- 그룹, 필드, 선택 옵션으로 구성된 3열 클릭 선택 방식
- 필드 선택과 옵션 변경 시 현재 필드 목록의 스크롤 위치 유지
- 표시값은 `코드 - 설명` 형태로 보여주고, Core 서비스 호출 시 실제 코드만 추출
- `Generate` 결과를 하단 테이블에 누적
- `Export Excel` 클릭 시 현재 결과 row를 Excel로 저장
- Ramos 회사 컬러 기반 테마 적용

## Incoming & Comp 룰

### 주요 입력

- Comp Full Part
- Source
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

Third-party Source:

- `T`
- `B`

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

Comp Full Part는 Module 기본값을 유도하는 용도로 사용한다. Module Full Part는 실제 Module 코드에서 필드값을 복원하는 용도로 사용한다.

### 생성

`Generate` 클릭 시:

```csharp
ModuleService.GeneratePreview(request)
```

생성 결과:

- Module row
- Module BIN row

### 화면 룰

Module DRAM Type은 화면에서는 `A - DDR4`, `R - DDR5`로 보인다. Core 서비스에는 DDR4를 `4`, DDR5를 `R` 코드로 전달한다.

Speed 제한:

DDR4:

- `WE - 3200 MT/s`

DDR5:

- `QK - 4800 MT/s`
- `WM - 5600 MT/s`
- `CM - 6000 MT/s`
- `CQ - 6400 MT/s`
- `CR - 6800 MT/s`
- `CS - 7200 MT/s`

Bank/VDD 자동 계산:

- DDR4 + `WE` -> `4`
- DDR5 + `QK` or `WM` -> `5`
- DDR5 + `CM` or `CQ` -> `6`
- DDR5 + `CR` or `CS` -> `7`

Third-party Module Source일 때만 Purchaser를 입력할 수 있다.

Third-party Module Source:

- `TM`
- `BM`

A100 Special은 아래 조건에서만 입력 가능하다.

- Third-party Module
- Vendor = `A`
- Purchaser = `A`

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
| Incoming & Comp 화면 | 1차 완료 | Parse, Generate, Export, Reset 구현 |
| Module 화면 | 1차 완료 | Comp parse, Module parse, Generate, Export, Reset 구현 |
| Excel Export | 완료 | 기존 exporter 재사용 |
| Ramos 색상 테마 | 1차 완료 | Header, Tab, Button, Table 색상 반영 |
| 필드 겹침/잘림 대응 | 1차 완료 | 상단 입력부와 필드 영역 layout 조정 |
| Web/API 없이 exe 실행 | 가능 | Desktop 앱은 API 호출 없음 |
| `.sln` 편입 | 미완료 | 현재 project path로 직접 빌드/실행 |
| Publish 패키징 | 완료 | 최상위 `Publish` 폴더에 self-contained 단일 exe 생성 |
| 설치 프로그램 | 미완료 | MSI/Setup 등은 아직 없음 |
| 전체 UI 수동 QA | 진행 필요 | 긴 option text, 작은 창 크기, DPI 확대 확인 필요 |
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
