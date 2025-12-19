# Upgrade 패널(메인 상시 UI) 작업 정리 / 미완료 체크리스트

## 현재 목표(요청사항)
- **메인 화면에서 상시 노출되는 Upgrade 패널 프리팹** 제작
- **장비(Equipment) UI의 기존 리소스(이미지/폰트/룩앤필)를 그대로 재사용**해서 전체 UI 톤 통일
  - 주의: 장비쪽 프리팹/스크립트는 **참고만** 하고 **수정 금지**
- 슬롯에 있는 **아이콘/레벨은 ScriptableObject(UpgradeTable)에서 조회**해서 바인딩
- 조건 미달 시 슬롯/티어는 **자물쇠 UI로 잠금 표시**
- 첨부된 이미지 느낌(상단 슬롯 + 하단 상세/강화 버튼 + 잠금 섹션)으로 구성

---

## 지금까지 확인/발견한 것(기반 구조)

### 이미 존재하는 코드/데이터(활용 가능)
- **`Assets/02. Scripts/UI/Scene/UI_Upgrade/UIUpgradePanel.cs`**
  - 메인 상시 패널로 쓰도록 이미 분리 설계되어 있었음
- **`Assets/02. Scripts/UI/Scene/UI_Main/UIMainRootScene.cs`**
  - 메인 씬 루트에서 Upgrade 패널을 “상시 UI”로 인스턴스/Show 하도록 설계되어 있음
  - 단, **`UI_Main.prefab`에서 `_upgradePanelRoot`, `_upgradePanelPrefab` 레퍼런스가 아직 null**
- **`Assets/02. Scripts/Core/Services/Combat/UpgradeService.cs`**
  - `TryUpgrade(code, levels, ...)`, `GetLevel(code)`, `GetNextCost(code)` 구현되어 있음
- **`Assets/06. ScriptableObject/Data/UpgradeTable.asset`**
  - `UpgradeRow` 기반 업그레이드 목록이 존재 (Code/Name/Description/Tier/MaxLevel/비용곡선 등)
  - `UpgradeRow` 스키마에 `Sprite Icon` 필드가 있음 (`Assets/02. Scripts/Core/Data/GameDataSchemas.cs`)

### 기획 잠금 조건(이미 코드에 반영되어 있던 값)
`Docs/UI_Design.md` 기준:
- 슈퍼: Lv 5000
- 울트라: Lv 15000
- 슈퍼울트라: Lv 30000

### UI 리소스(장비 UI와 통일 가능한 스프라이트/폰트) 위치 확인
아래는 “이미 프로젝트에 존재”하고, **Equipment UI에서 쓰는 동일 계열 리소스**:
- **자물쇠 아이콘**
  - `Assets/Layer Lab/GUI-CasualFantasy/ResourcesData/Sprites/Components/IconMisc/Icon_Lock01.png`
- **버튼 배경(그린)**
  - `Assets/Layer Lab/GUI Pro-SuperCasual/ResourcesData/Sprites/Components/Button/Button01_s_Green.png`
- **패널 배경(라운드 프레임)**
  - `Assets/Layer Lab/GUI Pro-SuperCasual/ResourcesData/Sprites/Components/Frame/BasicFrame_Round12.png`
- **슬롯 프레임(스퀘어 아웃라인 계열)**
  - `Assets/Layer Lab/GUI-CasualFantasy/ResourcesData/Sprites/Components/Frame/_BasicFrame_SquareOutline02_Demo02 1.png`
- **폰트(TMP)**
  - `Assets/Layer Lab/GUI Pro-SuperCasual/ResourcesData/Fonts/Sen_Line_s_Black SDF_Light.asset`

---

## 이번 세션에서 실제로 반영된 코드 변경

### 1) 업그레이드 슬롯 컴포넌트 추가
- **추가됨**: `Assets/02. Scripts/UI/Scene/UI_Upgrade/UIUpgradeSlot.cs`
  - 역할:
    - 슬롯 1칸의 **아이콘/레벨/선택/잠금 상태** 표시
    - 클릭 시 `Action<string>`으로 `UpgradeCode`를 패널로 전달
  - 잠금 UI:
    - `LockRoot`(오버레이), `LockIcon`, `LockText`
  - 선택 UI(옵션):
    - `SelectedRoot` 활성/비활성

### 2) UIUpgradePanel을 “SO 기반 슬롯 생성/갱신”으로 확장
- **갱신됨(파일 전체 갱신 방식)**: `Assets/02. Scripts/UI/Scene/UI_Upgrade/UIUpgradePanel.cs`
  - 주요 동작:
    - `OnShow()`에서 `UpgradeTable`을 `ResourceService.LoadTableAsync<UpgradeTable>()`로 로드
    - 테이블의 `Rows`를 순회하며 `UIUpgradeSlot`을 티어별 Root에 Instantiate
    - 슬롯 레벨은 `UpgradeService.GetLevel(code)`로 갱신
    - 티어 잠금은 캐릭터 레벨 기준(`_superUnlockLevel`, `_ultraUnlockLevel`, `_superUltraUnlockLevel`)
    - 선택된 업그레이드는 `Title/Description`을 테이블에서 읽어 표시
  - 참고:
    - 이 파일은 `apply_patch`가 컨텍스트 매칭에 실패해서, MCP의 `script-updateorcreate`로 내용 전체를 교체하는 방식으로 반영됨

---

## 현재 UI 프리팹 상태(중요)

### `Assets/03. Prefabs/UI/Scene/UI_Main/UI_Upgrade.prefab`
- 현재 프리팹은 사실상 **임시 오브젝트 1개(`tempLockImage`)만 있는 상태**
- 즉, **첨부 이미지 느낌의 레이아웃/슬롯/버튼/텍스트가 아직 구성되지 않음**

### 프리팹 자동 생성(에디터 스크립트) 시도 결과
- Unity Editor에서 임시로 “슬롯 프리팹 + 업그레이드 프리팹”을 자동 생성/배선하는 스크립트를 실행하려 했으나,
  - `RectTransform` 생성 방식 문제로 런타임 에러가 발생해 중단됨
  - 따라서 **`UI_UpgradeSlot.prefab` 같은 실제 프리팹 생성은 아직 완료되지 않음**

---

## 아직 완료되지 않은 작업(체크리스트)

### A. UI 프리팹 제작(핵심 미완료)
- [ ] `UI_Upgrade.prefab`를 첨부 이미지처럼 레이아웃 구성
  - 상단: 티어별 슬롯 영역(기본/슈퍼/울트라/슈퍼울트라)
  - 하단: 선택된 업그레이드 상세(이름/설명/레벨/비용) + “강화” 버튼
  - 잠금: 슈퍼/울트라/슈퍼울트라 섹션에 자물쇠 오버레이 표시
- [ ] `UIUpgradePanel` 인스펙터 필드 배선
  - `_slotPrefab`
  - `_normalSlotsRoot`, `_superSlotsRoot`, `_ultraSlotsRoot`, `_superUltraSlotsRoot`
  - `_fallbackIcon`, `_lockIcon`
  - `_titleText`, `_descriptionText`, `_levelText`, `_costText`
  - `_upgradeButton`
  - `_superLockedRoot/_ReasonText`, `_ultraLockedRoot/_ReasonText`, `_superUltraLockedRoot/_ReasonText`

### B. 메인 연결(UIMainRootScene)
- [ ] `Assets/03. Prefabs/UI/Scene/UI_Main/UI_Main.prefab`에서 `UIMainRootScene`의
  - `_upgradePanelRoot` (Transform)
  - `_upgradePanelPrefab` (`UIUpgradePanel` 프리팹 레퍼런스)
  를 실제로 할당
- [ ] UI 상단바/하단바 영역을 침범하지 않도록(Stretch 영역) 배치 조정

### C. UpgradeTable 아이콘(데이터) 세팅
- [ ] `Assets/06. ScriptableObject/Data/UpgradeTable.asset`의 각 `UpgradeRow.Icon` 세팅
  - 요구사항상 “슬롯 아이콘은 SO에서 조회”이므로, **아이콘이 null이면 슬롯이 비어 보이거나 fallback만 보임**
- [ ] 아이콘이 준비되기 전 임시 대응(선택)
  - `_fallbackIcon`을 보여주되, 최종적으로는 테이블 아이콘을 채우는 방향 권장

### D. 잠금 상태 UX 디테일
- [ ] 잠금 상태에서 슬롯 클릭/강화 클릭이 막히는지 확인(현재 슬롯 버튼은 `interactable=false` 처리)
- [ ] 티어 잠금 오버레이 문구/정렬/가독성(폰트/색상/줄바꿈) 조정

---

## 파일/리소스 목록(빠른 링크)

### 수정/추가된 스크립트
- `Assets/02. Scripts/UI/Scene/UI_Upgrade/UIUpgradePanel.cs`
- `Assets/02. Scripts/UI/Scene/UI_Upgrade/UIUpgradeSlot.cs`

### 관련 프리팹(미완료)
- `Assets/03. Prefabs/UI/Scene/UI_Main/UI_Upgrade.prefab` (현재 거의 비어있음)
- (예정) `Assets/03. Prefabs/UI/Scene/UI_Main/UI_UpgradeSlot.prefab`

### 데이터(SO)
- `Assets/06. ScriptableObject/Data/UpgradeTable.asset`

### 참고(수정 금지, 참고만)
- `Assets/03. Prefabs/UI/Popup/UI_Equipment/*`
- `Assets/02. Scripts/UI/Popup/UI_Equipment/*`

---

## 다음 작업을 시작할 때 권장 진행 순서
1. `UI_UpgradeSlot.prefab` 먼저 만들고(`UIUpgradeSlot` 필드 배선 포함), 장비 UI와 같은 프레임/자물쇠/폰트 적용
2. `UI_Upgrade.prefab`에서 슬롯 Root/텍스트/버튼 구성 및 `UIUpgradePanel` 인스펙터 배선
3. `UI_Main.prefab`에서 `UIMainRootScene`에 `_upgradePanelRoot/_upgradePanelPrefab` 연결
4. `UpgradeTable.asset`에 아이콘 채우기(요구사항 충족)


