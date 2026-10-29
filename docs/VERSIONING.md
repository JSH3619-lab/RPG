# Ramos Part Generator 버전 관리

## 버전 구분

프로그램 버전과 규칙 버전을 별도로 관리한다.

- 프로그램 버전: `Part Generator vMAJOR.MINOR.PATCH`
- 규칙 버전: `Spec Rev NN.N`

현재 기준:

- Part Generator `v1.6.1`
- Spec Rev `32.2`

## 프로그램 버전 규칙

- MAJOR: 기존 Part 생성 결과나 저장 형식과 호환되지 않는 변경
- MINOR: 새로운 생성 기능, 화면, Export 기능 추가
- PATCH: 오류 수정, 문구, 화면 배치, 기존 규칙 보완

프로그램 버전은 `RamosPartGenerator.Desktop.csproj`에서 관리한다. CLI(`RamosPartGenerator.Cli.csproj`)도 같은 4개 값으로 맞춘다.

```xml
<Version>1.6.1</Version>
<AssemblyVersion>1.6.1.0</AssemblyVersion>
<FileVersion>1.6.1.0</FileVersion>
<InformationalVersion>1.6.1</InformationalVersion>
```

변경 시 화면 제목, 헤더, EXE 파일 속성, 시작 로그에 같은 버전이 반영된다.

## Spec Rev 규칙

Spec Rev 표시값은 `specs/rev30.json`의 `display_revision`에서 관리한다.

내부 API와 파일명 호환을 위해 규칙 키 `30`과 `rev30.json` 파일명은 유지한다. 사용자 화면에는 `display_revision`인 `31.0`을 표시한다.

Rev 31.0은 `RAMOS_DRAM_PART (Rev.31.0).pdf` 원본 규격(LPDDR 파트 규칙 포함)을 반영한 버전이다.

## 배포 체크리스트

1. 프로그램 변경 수준에 맞춰 버전을 증가한다.
2. Spec 변경이면 `display_revision`을 확인한다.
3. `CHANGELOG.md`에 변경 내역을 기록한다.
4. 전체 테스트와 Desktop 빌드를 실행한다.
5. Desktop·CLI 단일 EXE를 `Publish/X.Y.Z/`로 Publish하고 이전 버전 폴더를 삭제한다. (`-o Publish/X.Y.Z`, 명령은 `README.md` 참조)
6. EXE 파일 버전과 화면 표시를 확인한다.
7. 아무도 프로그램을 켜지 않은 상태에서 공유 폴더 `PGM\DRAMPartGenerator\`의 exe만 덮어쓴다. (`*.json` 데이터 파일 유지, `README.md`의 `팀 공용 배포` 참조)
8. 커밋 후 `git tag -a vX.Y.Z`로 릴리스를 태그한다.
9. 커밋과 태그를 원격 저장소에 Push한다.

## 빌드 아카이브 정책

- `Publish/`에는 최신 버전 폴더 하나만 보관한다. 이전 버전 폴더는 남기지 않는다.
- 릴리스 아카이브는 git 태그(`vX.Y.Z`)로 관리한다. 특정 버전 EXE가 필요하면 해당 태그를 체크아웃해 다시 Publish한다.
