# Ramos Part Generator 다음 세션 인수인계

최종 정리일: 2026-05-04

## 작업 원칙

- 이 프로젝트에서는 Web/frontend 코드는 수정하지 않는다.
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
dotnet publish RamosPartGenerator/csharp-desktop/RamosPartGenerator.Desktop/RamosPartGenerator.Desktop.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o RamosPartGenerator/publish
```

## 현재 배포 구조

- WinForms 프로젝트: `RamosPartGenerator/csharp-desktop/RamosPartGenerator.Desktop`
- 배포 산출물: `RamosPartGenerator/publish/RamosPartGenerator.Desktop.exe`
- exe 하나만 배포 가능하도록 Core의 `specs/*.json`은 embedded resource로 포함한다.
- 외부 `specs` 폴더가 있으면 우선 사용하고, 없으면 embedded specs로 fallback한다.
- exe 아이콘은 `RamosPartGenerator/csharp-desktop/RamosPartGenerator.Desktop/Assets/RamosPartGenerator.ico`를 사용한다.

## 최근 반영된 주요 규칙

### 공통 표시/Export

- 생성창/결과창/Export의 주요 컬럼은 한글 기준으로 정리했다.
- `PartCode`는 `품목코드`, `NAME`은 `품목명`, `Generalinfo`는 `품목일반정보`, `Specification`은 `품목규격` 의미다.
- 비고 필드는 코드와 결과창에서 제거했다.
- Export 파일명 기본값은 `DRAM 품목정보(yyMMdd).xlsx` 형식이다.
- Export 글꼴은 Arial이다.
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
- I.C Brand 표기는 `S/G -> S1`, `H -> S2`, `M -> S3`, `C -> S6`, `N -> S9`만 사용한다.

### Module

- MDL Composition 드롭다운은 규격서 기준으로 `4 - x4`, `8 - x8`, `6 - x16`을 표시한다.
- DDR4/DDR5에 맞지 않는 Speed, Bank/VDD, Die Density 조합은 막는다.
- Module Density, Die Density, Composition, Rank 조합이 계산상 맞지 않으면 생성하지 않는다.
- IC count 표기는 `64 / compositionWidth * rankCount` 기준이다.
- A100 Module 규칙:
  - 품목규격의 ADATA 계열 표기는 `A100`으로 바꾼다.
  - A100은 `TP` 표현을 쓰지 않는다.
- PCB 규격 표기:
  - Brain Power 계열은 `BP PCB`
  - HJ/선진 계열은 `HJ PCB`
  - `AD5U8C0(ADATA/BP)`도 규격에는 `BP PCB`
- Module 품목규격은 용량/Comp 개수 괄호 뒤에 I.C Brand를 붙인다.
- I.C Brand 표기는 `S/G -> S1`, `H -> S2`, `M -> S3`, `C -> S6`, `N -> S9`만 사용한다.

### Module Repair Dummy

Module Full Part에서 Special Code 2가 특정 값이면 기본 MDL 행 뒤에 `MDL Dummy` 행을 추가한다.

- `R` (`1st Repair`):
  - 예: `BMRDAG58A1A-CPARRWMAAAR`
  - Dummy code: `BMRDAG58A1A-CPARRWMAAA00`
  - 규격 끝: `1st Repair Dummy`
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

## UI 안정화 메모

- WinForms ComboBox에서 선택 중 자기 자신의 `Items`를 다시 지우고 채우면 네이티브 오류가 날 수 있다.
- 현재는 변경된 필드 기준으로 필요한 종속 옵션만 갱신한다.
- 옵션 목록을 실제로 바꿀 때는 AutoComplete를 잠깐 끄고 `Items`를 재구성한다.

## 테스트 기준

현재 기준 전체 테스트는 33개다.

주요 테스트 범위:

- DDR5 Comp BIN 생성
- Part Revision 기반 die label
- A100 Incoming/Comp 규격
- Module A100/PCB/IC Brand 규격
- Module Repair Dummy 생성/미생성
- Export `영업코드`, Arial, `MDL Dummy`
- embedded specs fallback

## 커밋/배포 주의

- `RamosPartGenerator/publish/`는 배포 산출물이라 커밋하지 않는다.
- `DRAM 품목정보*.xlsx`는 Export 결과 파일이라 커밋하지 않는다.
- `.code-review-graph/`는 MCP 로컬 산출물이라 커밋하지 않는다.
- exe 생성 후 산출물 확인은 `Get-ChildItem RamosPartGenerator/publish`로 한다.
