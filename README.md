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

단일 exe 생성:

```powershell
cd C:\RPG
dotnet publish RamosPartGenerator\csharp-desktop\RamosPartGenerator.Desktop\RamosPartGenerator.Desktop.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o RamosPartGenerator\publish
```

배포 산출물:

```text
C:\RPG\RamosPartGenerator\publish\RamosPartGenerator.Desktop.exe
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
