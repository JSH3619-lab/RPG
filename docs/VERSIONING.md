# Ramos Part Generator 버전 관리

## 버전 구분

프로그램 버전과 규칙 버전을 별도로 관리한다.

- 프로그램 버전: `Part Generator vMAJOR.MINOR.PATCH`
- 규칙 버전: `Spec Rev NN.N`

현재 기준:

- Part Generator `v1.3.0`
- Spec Rev `30.6`

## 프로그램 버전 규칙

- MAJOR: 기존 Part 생성 결과나 저장 형식과 호환되지 않는 변경
- MINOR: 새로운 생성 기능, 화면, Export 기능 추가
- PATCH: 오류 수정, 문구, 화면 배치, 기존 규칙 보완

프로그램 버전은 `RamosPartGenerator.Desktop.csproj`에서 관리한다.

```xml
<Version>1.0.2</Version>
<AssemblyVersion>1.0.2.0</AssemblyVersion>
<FileVersion>1.0.2.0</FileVersion>
<InformationalVersion>1.0.2</InformationalVersion>
```

변경 시 화면 제목, 헤더, EXE 파일 속성, 시작 로그에 같은 버전이 반영된다.

## Spec Rev 규칙

Spec Rev 표시값은 `specs/rev30.json`의 `display_revision`에서 관리한다.

내부 API와 파일명 호환을 위해 규칙 키 `30`과 `rev30.json` 파일명은 유지한다. 사용자 화면에는 `display_revision`인 `30.6`을 표시한다.

Rev 30.6은 `RAMOS_DRAM_PART (Rev.30.6).pdf` 원본 규격을 반영한 버전이다.

## 배포 체크리스트

1. 프로그램 변경 수준에 맞춰 버전을 증가한다.
2. Spec 변경이면 `display_revision`을 확인한다.
3. `CHANGELOG.md`에 변경 내역을 기록한다.
4. 전체 테스트와 Desktop 빌드를 실행한다.
5. 단일 EXE를 Publish한다.
6. EXE 파일 버전과 화면 표시를 확인한다.
7. 커밋 후 최종 Phase에서 원격 저장소에 Push한다.
