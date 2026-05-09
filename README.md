# Ramos Part Generator

RAMOS DRAM/Module Part Code를 규칙 기반으로 생성, 파싱, 검증하고 등록용 Excel 파일로 내보내는 WinForms 프로그램입니다.

## 주요 기능

- Incoming / Comp Part Code 생성
- Module / Module BIN Part Code 생성
- Comp Full Part 파싱
- Module Full Part 파싱
- 스펙 코드 드롭다운 제공
- DRAM Type, Density, Bank, Interface, Speed 등 조합 검증
- Rev 30 기준 Vendor / Purchaser / A100 Special 조건 반영
- Existing PART / TM PART 모드 전환
- 생성 결과 미리보기
- 등록용 Excel Export

## 실행

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

테스트:

```powershell
cd C:\RPG\RamosPartGenerator
dotnet test RamosPartGenerator.sln
```

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
- `SpecProvider`: `specs` JSON 로딩
- `ProductTextService`: 품명, General Info, Specification 텍스트 생성
- `RegistrationExcelExporter`: 등록용 Excel 파일 생성

## 요구 환경

- Windows
- .NET SDK 7.x

## 참고

현재 기준 실행 대상은 WinForms Desktop 앱입니다.
