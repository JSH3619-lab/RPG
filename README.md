# Ramos Part Generator

Ramos Part Generator는 RAMOS DRAM/Module Part Code를 규칙 기반으로 생성, 파싱, 검증하고 등록용 Excel 파일로 내보내는 프로그램입니다.

현재 동일한 핵심 로직을 두 가지 UI에서 사용할 수 있습니다.

- HTML/Web UI: React + Vite 화면과 .NET API 조합
- C# Desktop UI: WinForms 기반 Windows 실행 프로그램

## 주요 역할

이 프로그램의 목적은 RAMOS Part Code 업무에서 반복되는 코드 조합과 검증 과정을 자동화하는 것입니다.

- Incoming / Comp Part Code 생성
- Module / Module BIN Part Code 생성
- Comp Full Part 파싱
- Module Full Part 파싱
- 규격 코드 드롭다운 제공
- DRAM Type, Density, Bank, Interface, Speed 등 조합 검증
- Rev 30 기준 Vendor / Purchaser / A100 Special 조건 반영
- 생성 결과 미리보기
- 등록용 Excel 파일 Export

## 지원 화면

### Incoming & Comp

Incoming / Comp 품목 생성을 위한 화면입니다.

- `Comp Full Part`를 입력해 필드 자동 파싱
- Source, DRAM Type, Density, Bit, Bank, Interface, Revision 입력
- Comp Type, Die Brand, Vendor, Purchaser, Package, Tester 입력
- DDR4 / DDR5에 따른 Bank / Interface 기본값 자동 반영
- Comp BIN speed row 자동 생성
- Excel Export

### Module

Module 및 Module BIN 품목 생성을 위한 화면입니다.

- `Comp Full Part` 파싱으로 Module 기본값 자동 채움
- `Module Full Part` 파싱으로 Module 필드 복원
- Source, DRAM Type, DIMM Type, Module Density, Bank/VDD, Die Density, Rank 입력
- I.C Brand, Comp Type, Test Site, Speed, PCB 입력
- Vendor, Purchaser, A100 Special, Special Code, Grade, Product Bin 입력
- Speed에 따른 Bank/VDD 자동 계산
- Excel Export

## 실행 방식

### 1. HTML/Web UI로 실행

API 서버를 먼저 실행합니다.

```powershell
cd C:\RPG\RamosPartGenerator
dotnet run --project src\RamosPartGenerator.Api\RamosPartGenerator.Api.csproj
```

프론트엔드를 실행합니다.

```powershell
cd C:\RPG\RamosPartGenerator\frontend
npm run dev
```

기본 접속 주소:

- Web UI: `http://localhost:5173`
- API: `http://localhost:5000`

Vite 개발 서버는 `/api` 요청을 `http://localhost:5000`으로 proxy합니다.

### 2. C# Desktop exe로 실행

개발 실행:

```powershell
cd C:\RPG\RamosPartGenerator
dotnet run --project csharp-desktop\RamosPartGenerator.Desktop\RamosPartGenerator.Desktop.csproj
```

빌드 확인:

```powershell
cd C:\RPG\RamosPartGenerator
dotnet build csharp-desktop\RamosPartGenerator.Desktop\RamosPartGenerator.Desktop.csproj
```

별도 출력 폴더로 exe 생성:

```powershell
cd C:\RPG\RamosPartGenerator
dotnet build csharp-desktop\RamosPartGenerator.Desktop\RamosPartGenerator.Desktop.csproj -o csharp-desktop\RamosPartGenerator.Desktop\bin\BuildCheck
```

생성된 실행 파일 예:

```text
C:\RPG\RamosPartGenerator\csharp-desktop\RamosPartGenerator.Desktop\bin\BuildCheck\RamosPartGenerator.Desktop.exe
```

## 프로젝트 구조

```text
C:\RPG
├─ docs\
│  ├─ RAMOS_DRAM_PART (Rev.30).pdf
│  ├─ RAMOS_DRAM_TM_PART (Rev 1.0) (2).pdf
│  ├─ RULES_CURRENT.md
│  └─ WEB_ARCHITECTURE_PLAN.md
├─ specs\
│  ├─ shared.json
│  └─ rev30.json
└─ RamosPartGenerator\
   ├─ frontend\                         HTML/Web UI
   ├─ src\RamosPartGenerator.Api\        Web API
   ├─ src\RamosPartGenerator.Core\       Part 생성/파싱/검증 핵심 로직
   ├─ src\RamosPartGenerator.Excel\      Excel Export
   ├─ csharp-desktop\RamosPartGenerator.Desktop\  C# WinForms UI
   └─ tests\RamosPartGenerator.Tests\    Core 테스트
```

## 핵심 데이터 기준

프로그램 실행 중 파트 구조와 코드 옵션은 `specs` 폴더의 JSON 파일을 기준으로 읽습니다.

- `specs/shared.json`: 공통 코드 옵션, speed rule, family rule
- `specs/rev30.json`: Rev 30 기준 tail model, module field rule, UI option

`docs` 폴더의 PDF와 Markdown은 원본 규격 및 개발 참고 문서입니다. 현재 프로그램은 실행 중 PDF를 매번 열어 파싱하지 않습니다. 규격이 변경되면 PDF를 참고해 `specs` JSON을 갱신해야 앱 동작에 반영됩니다.

## 공통 로직

HTML/Web UI와 C# Desktop UI는 서로 다른 화면을 사용하지만, Part 생성과 검증은 같은 C# Core 로직을 사용합니다.

- `IncomingCompService`: Incoming / Comp 생성 및 Comp Full Part 파싱
- `ModuleService`: Module / Module BIN 생성 및 Module 관련 파싱
- `SpecProvider`: `specs` JSON 로딩
- `ProductTextService`: 품명, General Info, Specification 텍스트 생성
- `RegistrationExcelExporter`: 등록용 Excel 파일 생성

## 개발/검증 명령

전체 C# 테스트:

```powershell
cd C:\RPG\RamosPartGenerator
dotnet test RamosPartGenerator.sln
```

Web frontend build:

```powershell
cd C:\RPG\RamosPartGenerator\frontend
npm run build
```

C# Desktop build:

```powershell
cd C:\RPG\RamosPartGenerator
dotnet build csharp-desktop\RamosPartGenerator.Desktop\RamosPartGenerator.Desktop.csproj
```

## 요구 환경

- Windows
- .NET SDK 7.x
- Node.js / npm, Web UI 개발 시 필요

## 현재 주의사항

- C# Desktop 프로젝트는 Windows WinForms 기반이므로 Windows 환경에서 실행해야 합니다.
- `specs` JSON은 빌드 시 실행 폴더의 `specs` 하위로 복사됩니다.
- 앱 실행 중에는 해당 exe/dll 파일이 잠길 수 있으므로, 같은 출력 폴더로 다시 빌드하려면 실행 중인 앱을 먼저 종료해야 합니다.
