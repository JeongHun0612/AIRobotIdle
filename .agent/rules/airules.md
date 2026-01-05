---
trigger: always_on
---

모든 대답은 한국어로할것

# SahurRaising 프로젝트 AI 개발 가이드라인

이 문서는 **SahurRaising** 프로젝트의 개발을 돕는 AI를 위한 시스템 프롬프트 및 가이드라인입니다. 프로젝트의 구조, 아키텍처, 코딩 컨벤션을 정의합니다.

- 게임 이름이 AI 로봇 키우기로 변경됨. SahurRasing 이름을 가진것들과 기능적으로 크게 변경된게 없음

## 1. 프로젝트 개요
- **엔진**: Unity 6000.0.60f1
- **언어**: C#
- **주요 라이브러리**: 
  - **UniTask**: 비동기 처리를 위해 Coroutine 대신 전적으로 사용
  - **Addressables**: 리소스 관리 및 로딩
  - **Unity Localization**: 다국어 처리
  - **DOTween**: 애니메이션 (UniTask.DOTween 존재로 확인)
  - **TextMeshPro**: 텍스트 렌더링

## 2. 아키텍처 및 디자인 패턴
### 2.1. 코어 시스템
- **ServiceLocator 패턴**: 주요 서비스(`IResourceService`, `IEventBus`, `ILocalizationService` 등)는 `ServiceLocator`를 통해 등록하고 접근합니다.
  - 예: `ServiceLocator.Get<IResourceService>()`
- **GameManager**: 게임의 진입점이며, 서비스 초기화(`ServiceRegisterAsync`) 및 게임 흐름을 제어합니다. `MonoSingleton<GameManager>`를 상속받습니다.
- **EventBus**: 시스템 간 느슨한 결합을 위해 `EventBus`를 사용하여 이벤트를 발행하고 구독합니다.
  - 이벤트 데이터는 `struct`로 정의하여 GC 할당을 최소화합니다.
  - `Subscribe<T>`, `Publish<T>` 인터페이스를 사용합니다.

### 2.2. 데이터 및 설정 (ScriptableObject)
- **ScriptableObject**: 정적 데이터, 설정 값, 프리팹 참조(Addressables) 등은 `ScriptableObject`로 관리합니다.
- **대규모 CSV 처리** (`Assets/10.Data/*.csv`):
  - CSV는 소스 오브 트루스로 유지하되 **행 단위 SO 금지**. 기능별 테이블(예: Monster/Stage/Upgrade/Equipment)을 **테이블 단위 SO 1~N개**로 묶습니다.
  - 에디터 전용 변환기로 CSV → 직렬화된 테이블 SO(바이너리/JSON 압축 가능)로 빌드하고, 결과 테이블 SO만 Addressable로 배포합니다. 런타임 CSV 파싱 금지.
  - 테이블 SO 내부는 `List<RowStruct>`와 주요 키(예: ID) 기준 `Dictionary` 인덱스를 빌드 단계에서 생성해 조회 O(1)을 유지합니다.
  - BigDouble 필드는 string 형태("1.23e45")로 직렬화/역직렬화합니다.
- **UIRegistry**: UI 프리팹과 Enum 타입 간의 매핑을 관리하는 중앙 레지스트리입니다.
- **생성 메뉴**: `[CreateAssetMenu]` 속성을 사용하여 에디터에서 생성 가능하도록 합니다.

### 2.3. UI 시스템
- **UI_Base**: 모든 UI 컴포넌트의 기본 클래스입니다. `Show()`, `Hide()`, `InitializeAsync()` 등의 가상 메서드를 가집니다.
- **UIManager**: UI의 생성, 표시, 숨김, 스택 관리를 담당합니다.
- **구조**: `Assets/02. Scripts/UI` 폴더 내에 위치하며, `UI_Popup`, `UI_Scene` 등으로 구분됩니다.

### 2.4. 리소스 관리
- **ResourceManager**: Addressables를 래핑하여 리소스를 로드합니다.
- 리소스 로딩은 비동기(`UniTask`)로 처리해야 합니다.

### 2.5. 큰 수 처리 (Big Number Handling)
- **라이브러리**: `BreakInfinity.cs`를 사용합니다 (`Assets/Plugins/BreakInfinity`).
- **타입**: `BigDouble` 구조체를 사용하여 큰 수를 처리합니다.
- **GC 최적화**: `struct`이므로 이벤트 버스나 연산 시 GC 할당이 발생하지 않습니다.
- **데이터 저장**: JSON 직렬화 시 정밀도 유지를 위해 반드시 **String** 형태("1.23e45")로 변환하여 저장/로드합니다.

## 3. 코딩 컨벤션 및 규칙
### 3.1. 네이밍 규칙
- **클래스/메서드/프로퍼티**: `PascalCase` 사용
- **private 필드**: `_camelCase` 사용 (예: `_canvas`)
- **변수/필드**: camelCase (예: `_currentScene`, `isInitialized`)
- **상수**: UPPER_CASE (예: `MAX_LEVEL`, `DEFAULT_VALUE`)
- **인터페이스**: `I` 접두사 사용 (예: `IResourceService`)
- **비동기 메서드**: 접미사 `Async` 권장 (예: `InitializeAsync`)

### 3.2. 비동기 처리 (Critical)
- **Coroutine 사용 금지**: 특별한 이유가 없다면 모든 비동기 로직은 `UniTask`를 사용합니다.
- `async void`는 이벤트 핸들러(Unity 생명주기 포함)에서만 사용하고, 그 외에는 `async UniTask` 또는 `async UniTaskVoid`를 사용합니다.

### 3.3. 네임스페이스
- 폴더 구조에 따라 네임스페이스를 지정합니다.
  - Core 로직: `namespace SahurRaising.Core`
  - UI 로직: `namespace SahurRaising.UI`
  - 유틸리티: `namespace SahurRaising.Utils`

### 3.4. 주석 및 문서화
- **언어**: 한국어(Korean) 사용
- 복잡한 로직이나 주요 메서드에는 주석을 작성합니다.

## 4. 작업 워크플로우 예시

### 4.1. 새로운 UI 추가 시
1. `UI_Base` (또는 `UI_Popup`, `UI_Scene`)를 상속받는 스크립트 작성 (`SahurRaising.UI` 네임스페이스).
2. `UIManager`를 통해 해당 UI를 로드하고 표시하는 로직 추가.
3. 필요한 데이터나 로직은 `ServiceLocator`를 통해 매니저/서비스에 접근하여 처리.

### 4.2. 데이터 저장/로드
1. `GameManager`의 `SaveAllAsync` 패턴을 따름.
2. 각 서비스(예를들어 `ICurrencyService` 등)는 자신의 데이터를 저장하는 로직을 구현해야 함.
3. JSON 기반 데이터 저장
4. 클라우드 저장 지원
5. 데이터 검증 및 백업

### 4.3. 리소스 관리
- 모든 리소스는 Addressables 사용
- `ResourceManager`를 통해 비동기 로딩
- 메모리 관리 및 언로딩 적절히 처리

## 5. 주의사항
- `MonoBehaviour`의 생명주기(`Start`, `Update` 등) 내에서 무거운 로직은 피하고, 필요한 경우 비동기 초기화 패턴을 사용하세요.
- `Debug.Log`는 개발 빌드에서만 유효하도록 조건부 컴파일을 고려하거나, 프로젝트의 로깅 정책을 따르세요.