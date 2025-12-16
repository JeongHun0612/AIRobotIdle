# 전투/스탯/재화 데이터 파이프라인 진행 현황

## 완료된 작업
- 테이블 단위 SO 스키마 정의: `MonsterTable`, `UpgradeTable`, `StatsTable`, `EquipmentTable`(`GameDataSchemas.cs`).
- 헤더 기반 CSV 파서 및 빌더: `CsvUtil`(컬럼명 매핑), `DataTableBuilder`(Monster/Upgrade/Stats/Equipment) → 메뉴 `Tools/SahurRaising/Build Data Tables`.
- Equipment 테이블 지원: `EquipmentRow`, `EquipmentTable`, CSV 매핑 추가.
- Addressables 등록 가이드: 테이블 SO 4개를 그룹에 추가해 라벨/주소로 로드 가능.
- 오류 핸들링 강화: 파싱 실패 시 컬럼명/라인 정보 노출, 따옴표/공백 트림.
- 서비스 구현 및 등록
  - `ResourceService`: ScriptableObject 테이블 비동기 로드 헬퍼(`LoadTableAsync`) 추가.
  - `StatService`: 업그레이드 → 스탯레벨 매핑, `Stats.csv` Base/Pow 해석, 장비 모디파이어 합산 스냅샷 계산, 장비 옵션 타입 매핑/적용 추가.
  - `CurrencyService`: 잔액 Add/TryConsume, 저장/로드, 미접속 보상 계산(OFFT=테이블 누적 분, 기본 360분 대체, BaseGoldPerSecond=5.0 임시).
  - `CombatService`: 스테이지/웨이브 진행, 몬스터 HP/타이머, 자동/터치 공격, 보상 이벤트 발행, 진행도 저장/로드(`combat.json`) 추가.
  - `UpgradeService`: 4구간 비용 계산 + MaxLevel 클램프, 업그레이드 구매/저장/로드.
  - `GameManager`: 서비스 등록 및 `SaveAllAsync`에서 Currency/Upgrade/Combat 저장 호출.

## 앞으로 해야 할 작업
- 장비 데이터 채우기: `EquipmentTable` 옵션 타입 문자열을 매핑 규칙에 맞게 기입, 인벤토리/도감 경로에서 `ApplyEquippedItems` 연동.

- 검증/테스트
  - 에디터 Validation 메뉴(중복 키/누락 검사) 검토.
  - 플레이 모드에서 스탯 계산/업그레이드 적용/장비 옵션 반영 로그 확인.
  - 미접속 보상 계산식 검증.

