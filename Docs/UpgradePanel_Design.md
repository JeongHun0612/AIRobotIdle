# 업그레이드 패널 (UIUpgradePanel) 디자인 및 요구사항 정의

## 1. 개요
메인 화면에서 캐릭터의 능력치를 강화하는 업그레이드 패널입니다.
사용자는 스크롤을 통해 다양한 등급(Tier)의 업그레이드 항목을 확인하고 강화할 수 있습니다.

## 2. 화면 구성 (Layout)

### 2.1. 전체 구조
- **Vertical Scroll View**: 전체 패널은 세로로 스크롤 가능한 영역입니다.
- **Content 구성 순서** (위에서 아래로):
  1.  **Normal Slots Root**: 기본 업그레이드 슬롯 목록
  2.  **Super Tier 영역**:
      - `Super Locked Panel`: 잠금 상태일 때 표시 (자물쇠 아이콘 + 해금 조건 텍스트)
      - `Super Slots Root`: 해금 시 표시 (슈퍼 등급 업그레이드 슬롯 목록)
  3.  **Ultra Tier 영역**:
      - `Ultra Locked Panel`: 잠금 상태일 때 표시
      - `Ultra Slots Root`: 해금 시 표시
  4.  **SuperUltra Tier 영역**: ... (이후 동일 패턴)

### 2.2. 슬롯 디자인 (UIUpgradeSlot)
각 업그레이드 항목은 하나의 독립적인 슬롯으로 구성되며, 다음 정보를 모두 포함합니다 (Master-Detail 방식 아님).
- **아이콘 (Icon)**: 업그레이드 종류를 나타내는 이미지
- **제목 (Title)**: 업그레이드 이름 (예: 공격력)
- **설명 (Description)**: 효과 설명 (예: 더욱 강한 공격을 가합니다.)
- **레벨 (Level)**: 현재 레벨 (예: LV 10274)
- **수치 변화 (Value Change)**: 현재 효과 수치 -> 다음 레벨 효과 수치 (예: 17.3B -> 18.4B)
- **강화 버튼 (Upgrade Button)**:
  - 비용 표시 (예: 7QZ)
  - 최대 레벨 도달 시 "MAX" 표시
  - 잠금 상태일 경우 비활성화

## 3. 기능 요구사항

### 3.1. 스크롤 및 리스트 동작
- **연속적인 리스트**: 스크롤을 내리면 현재 강화 가능한 목록이 쭉 이어집니다.
- **조건부 확장**:
  - 하위 티어(예: 슈퍼 강화)가 잠겨있을 때는 해당 위치에 **잠금 패널**이 표시됩니다.
  - 해금 조건(예: 캐릭터 레벨 5000 달성)을 만족하면, **잠금 패널이 사라지고 그 자리에 해당 티어의 업그레이드 슬롯들이 나타나** 리스트가 자연스럽게 이어집니다.

### 3.2. 데이터 연동
- **StatService 연동**: 단순 레벨 표시를 넘어, 실제 적용되는 스탯 수치를 계산하여 보여줍니다 (`GetStatValue`).
- **자동 갱신**: 강화 성공 시 해당 슬롯뿐만 아니라, 연관된 수치나 잠금 상태가 즉시 갱신되어야 합니다.

### 3.3. 잠금 (Lock) 시스템
- **티어별 잠금**: 특정 캐릭터 레벨 도달 시 다음 티어가 개방됩니다.
  - Super: Lv 5000
  - Ultra: Lv 15000
  - SuperUltra: Lv 30000
- **시각적 피드백**: 잠긴 티어는 명확한 자물쇠 아이콘과 함께 "레벨 OOOO에 개방됩니다"라는 문구를 표시합니다.

## 4. Unity 계층 구조 (Hierarchy) 예시
```text
UI_Upgrade_Panel
 └── Scroll View
      └── Viewport
           └── Content (Vertical Layout Group)
                ├── Normal_Slots_Root (Vertical Layout Group)
                ├── Super_Locked_Panel (Image, Text)
                ├── Super_Slots_Root (Vertical Layout Group)
                ├── Ultra_Locked_Panel (Image, Text)
                └── Ultra_Slots_Root (Vertical Layout Group)
```
*스크립트(`UIUpgradePanel`)에서 각 Root와 Panel 오브젝트를 연결하여 `SetActive`로 제어합니다.*
