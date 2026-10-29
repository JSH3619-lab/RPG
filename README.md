# Ramos Part Generator

RAMOS DRAM/Module Part Code를 규칙 기반으로 생성, 파싱, 검증하고 등록용 Excel 파일로 내보내는 WinForms 프로그램입니다.

현재 관리 버전:

- Part Generator: `v1.6.1`
- Spec: `Rev 32.2`

## 주요 기능

- Incoming / Comp Part Code 생성
- Module / Module BIN Part Code 생성
- Module 완제품 Retest용 `00` Dummy / `0Y` Part 자동 생성
- MDL Full Part 다중 입력과 선택 작업 Part 일괄 생성
- MDL 기준 원본/Reball Incoming, Comp, Comp BIN 일괄 생성
- Comp Full Part 다중 입력과 Incoming/Comp/Comp BIN 일괄 생성
- 복수 선택 Speed 기준 Comp_MDL/MDL BIN 일괄 생성
- LPDDR 입고 + Product + Product BIN 생성과 LPDDR Full Part 역파싱
- ERP 등록 파트 중복 체크: 단일/일괄 생성 결과에 `등록됨`/`신규` 표시, Export 시 등록 파트 기본 제외
- Comp Full Part 파싱
- Module Full Part 파싱
- 그룹 → 필드 → 옵션의 3열 클릭 선택 UI 제공
- DRAM Type, Density, Bank, Interface, Speed 등 조합 검증
- MDL `CA - DDR5-6000 (48/48/48)`과 Bank/VDD `8 - 1.25V` 지원
- Rev 30 기준 Vendor / Purchaser 기반 A100 Special 조건 반영
- Existing PART / TM PART 모드 전환
- 생성 결과 미리보기
- 결과 테이블 셀 단위 다중 선택, 복사, 선택 행 삭제
- 등록용 Excel Export
- 구형(Rev.27 이하) Part → 신형 Part 변환 (`Batch Generate > 구형→신형 변환`, I.C Brand S/G 선택)
- 헤드리스 CLI(`RamosPartGenerator.Cli.exe`): generate / parse / validate / convert-legacy

## 실행

개발 실행:

```powershell
cd C:\RPG
dotnet run --project RamosPartGenerator\csharp-desktop\RamosPartGenerator.Desktop\RamosPartGenerator.Desktop.csproj
```

빌드 확인:

```powershell
cd C:\RPG
dotnet build RamosPartGenerator\csharp-desktop\RamosPartGenerator.Desktop\RamosPartGenerator.Desktop.csproj
```

테스트:

```powershell
cd C:\RPG
dotnet test RamosPartGenerator\RamosPartGenerator.sln
```

단일 exe 생성:

```powershell
cd C:\RPG
dotnet publish RamosPartGenerator\csharp-desktop\RamosPartGenerator.Desktop\RamosPartGenerator.Desktop.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=None -p:DebugSymbols=false -o Publish\1.6.1
dotnet publish RamosPartGenerator\src\RamosPartGenerator.Cli\RamosPartGenerator.Cli.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=None -p:DebugSymbols=false -o Publish\1.6.1
```

배포 산출물 (`Publish\` 아래에는 최신 버전 폴더 하나만 보관):

```text
C:\RPG\Publish\1.6.1\RamosPartGenerator.Desktop.exe
C:\RPG\Publish\1.6.1\RamosPartGenerator.Cli.exe
```

## 팀 공용 배포

팀원은 공유 폴더의 exe 하나를 함께 실행합니다. 개인 PC로 복사해서 쓰지 않습니다.

```text
Z:\연구소 DataBase\4. 팀별자료\4. DRAM 개발1팀\PGM\DRAMPartGenerator\
├─ RamosPartGenerator.Desktop.exe
├─ erp-part-cache.json       ERP 품목 업로드 스냅샷 (누가 올려도 공용)
├─ pgm-exported-cache.json   PGM Export 이력 (공용)
└─ specs\                    Spec Editor로 수정한 경우에만 생성
```

- 업데이트는 아무도 프로그램을 켜지 않은 상태에서 exe 파일만 덮어씁니다. `*.json` 데이터 파일은 덮어쓰거나 지우지 않습니다.
- `specs\` 폴더가 있으면 exe 내장 규칙보다 우선합니다. 규칙 변경이 담긴 새 버전을 배포할 때는 Spec Editor 수정분을 저장소에 먼저 반영하고 `specs\`를 정리합니다.
- ERP 품목 업로드는 ERP에서 새로 내려받은 `품목정보등록(Multi)(S).xlsx`로 한 사람만 하면 됩니다.

## CLI

GUI와 같은 Core 로직을 명령줄에서 호출합니다. 쓰기는 `--xlsx` 출력 파일뿐이며 ERP 캐시·PGM Export 이력(`pgm-exported-cache.json`)은 읽거나 쓰지 않습니다. Export 이력 기록은 GUI만 합니다.

```powershell
RamosPartGenerator.Cli.exe generate RMRDAG58A1A-NZWRRQKCG --xlsx out.xlsx --json
RamosPartGenerator.Cli.exe generate --in parts.txt --xlsx out.xlsx
RamosPartGenerator.Cli.exe parse RCAAG485WB-PMSRS --kind comp
RamosPartGenerator.Cli.exe validate --in parts.txt --json
RamosPartGenerator.Cli.exe convert-legacy RM4D8G4881E-PRRRWE1B --ic-brand S --json
```

- `--kind module|comp|lpddr` (기본 module), `--specs <dir>` (기본: exe 옆 `specs\`, 없는 파일은 내장 규칙), `--in` 목록 파일은 한 줄에 하나(`#` 주석)
- `--xlsx`는 generate/validate에서만 쓰며, 같은 파트는 한 번만 넣고 기존 파일은 덮어쓰지 않습니다.
- `convert-legacy`는 `--ic-brand S|G`가 필수입니다(기본값 없음).
- 종료 코드: `0` 성공 / `1` 일부 실패(확인 필요 포함) / `2` 입력·실행 오류
- `--json` 출력: `{"command":"generate","items":[{"input":"...","status":"ok|check|error","rows":[{"kind":"MDL","code":"...","name":"...","generalInfo":"...","spec":"..."}],"messages":[]}]}`

## 선택 UI

- 값은 직접 타이핑하지 않고 `그룹 → 필드 → 선택 옵션` 순서로 클릭해서 선택합니다.
- 필드 선택과 옵션 변경 시 현재 필드 목록의 스크롤 위치를 유지합니다.
- Parse 결과는 각 필드의 현재 값과 오른쪽 선택 옵션에 즉시 반영됩니다.
- Incoming/Comp의 Source는 Comp 중심으로 `RC/TC/CC/BC`를 표시하고, 함께 생성되는 입고 코드는 `K/T/C/B`로 안내합니다.
- 결과 테이블은 셀 하나 또는 드래그한 여러 셀을 선택할 수 있으며, `Ctrl+C`로 헤더 없이 복사합니다.
- `Delete Selected`는 선택한 셀이 포함된 결과 행만 삭제하고, `Reset`은 입력과 전체 결과를 초기화합니다.
- Module의 `완제품 Retest (00/0Y)`를 선택하면 기본 Part 대신 `00` Dummy, `0Y` MDL, `0Y` MDL BIN을 생성합니다.
- `Batch Generate` 탭은 MDL Full Part를 한 줄에 하나씩 입력하고 필요한 기본/작업/Comp 관련 항목만 체크해 일괄 생성합니다.
- Repair/Dummy 선택만으로 Comp 관련 Part를 만들지 않으며, 원본 Comp 또는 Reball Comp 옵션을 별도로 선택해야 합니다.
- `Comp 일괄`은 Comp Full Part마다 Incoming/Comp/Comp BIN을 만들고, 선택한 각 Speed의 Comp_MDL/MDL BIN을 추가합니다.
- Comp_MDL 값을 확정할 수 없으면 임의 값을 넣지 않고 해당 입력의 오류로 표시합니다.
- 결과 표의 `상태` 열은 ERP 업로드 품목과 PGM Export 이력 기준으로 `등록됨`/`신규`를 표시하며, `중복 포함`을 체크하지 않으면 Export에서 `등록됨` 파트를 제외합니다. (단일/일괄 탭 공통)

## 프로젝트 구조

```text
C:\RPG
├─ docs\
├─ specs\
│  ├─ shared.json
│  └─ rev30.json
└─ RamosPartGenerator\
   ├─ src\RamosPartGenerator.Core\       Part 생성/파싱/검증 공통 로직
   ├─ src\RamosPartGenerator.Excel\      Excel Export
   ├─ src\RamosPartGenerator.Cli\        헤드리스 CLI (Agent 호출용)
   ├─ csharp-desktop\RamosPartGenerator.Desktop\  WinForms UI
   └─ tests\RamosPartGenerator.Tests\    Core/Excel 테스트
```

## 스펙 데이터

프로그램은 실행 중 `specs` 폴더의 JSON 파일을 기준으로 코드 옵션과 규칙을 읽습니다.

- `specs/shared.json`: 공통 코드 옵션, speed rule, family rule
- `specs/rev30.json`: Rev 30 기준 tail model, module field rule, UI option

## 공통 로직

- `IncomingCompService`: Incoming / Comp 생성 및 Comp Full Part 파싱
- `ModuleService`: Module / Module BIN 생성 및 Module 관련 파싱
- `BatchGenerationService`: MDL 일괄 작업 조합, MDL→Comp 변환, 중복/입력별 오류 처리
- `SpecProvider`: `specs` JSON 로딩
- `ProductTextService`: 품명, General Info, Specification 텍스트 생성
- `RegistrationExcelExporter`: 등록용 Excel 파일 생성

## 운영 로그

프로그램 실행 로그는 사용자 PC의 LocalAppData 아래에 일자별로 남습니다.

```text
%LocalAppData%\RamosPartGenerator\logs\yyyyMMdd.log
```

예:

```text
C:\Users\<사용자>\AppData\Local\RamosPartGenerator\logs\20260510.log
```

- 로그 형식: `[yyyy-MM-dd HH:mm:ss.fff] [LEVEL] Event ...`
- 기록 대상: 프로그램 시작/종료, spec 로딩, 모드 전환, Comp/MDL 파싱, 생성, Export, 예외
- 보관 정책: 최근 7일 로그만 보관하며, 프로그램 실행 중 첫 로그 기록 시 오래된 `.log` 파일을 자동 삭제
- 입력값 전체 덤프는 남기지 않고 PartCode, 모드, 결과 row 수, 에러 메시지 중심으로 기록

## 요구 환경

- Windows
- .NET SDK 7.x

## 참고

현재 기준 실행 대상은 WinForms Desktop 앱입니다.

프로그램/규칙 버전 관리 기준은 `docs/VERSIONING.md`, 변경 내역은 `CHANGELOG.md`를 참조합니다.
