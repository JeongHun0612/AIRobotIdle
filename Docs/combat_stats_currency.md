# 전투·스탯·재화 기술 정리

## 1. 데이터 파이프라인
- CSV는 소스 오브 트루스(`Assets/10.Data/*.csv`), 런타임 CSV 파싱 금지.
- 에디터 변환기에서 테이블 단위 ScriptableObject로 직렬화(바이너리/JSON 압축 가능) 후 Addressable 배포.
- 테이블 SO 내부는 `List<RowStruct>` + 사전 빌드된 `Dictionary` 인덱스(키: ID/Level)로 조회 O(1) 유지.
- BigDouble은 문자열 형태("1.23e45")로 직렬화/역직렬화.
- 샤딩 필요 시 기능별/구간별 테이블 분할(예: MonsterTable_1_5000, MonsterTable_5001_10000).

## 2. 테이블 스키마 매핑
- Monster (사후르 키우기 DB - Monster.csv)
  - `MonsterLevel`, `BenchmarkLevel`, `ATKLevel`, `DEFLevel`, `MonsterHP`, `MonsterATK`, `MonsterDEF`, `MonsterDEFR`, `Gold`
- Upgrade (사후르 키우기 DB - Upgrade.csv)
  - `Code`, `Name`, `Description`, `Stat`, `Tier`, `MaxLevel`, `GoldBase`, `GoldPow`, `Segment1~4(MaxLevel, Growth)`
- Stats (사후르 키우기 DB - Stats.csv)
  - `Level`, `ATK_Base/Pow`, `HP_Base/Pow`, `DEF_Base/Pow`, `HPREC_Base/Pow`, `CR_Base/Pow`, `ATKT_Base/Pow`, `OFFT`, `GOLDR`, `ATKR`, `OFFA`, `ATKSP`, `RCD`, `UCR_Base/Pow`, `ATKB`, `CD`, `DEFR`, `IGNDEF_Base/Pow`
- Equipment/Skill 테이블은 동일 패턴(테이블 단위 SO + 인덱스)으로 확장.

## 3. 공식 명세
### 3.1 스테이지·몬스터
- 스테이지별 몬스터 레벨: `(stageIndex - 1) * 3 + waveIndex` (waveIndex: 1~3 일반, 4는 엘리트/보스).
- 엘리트 HP 배율: `hp * 10` (기획의 “10+a”에서 a=0으로 시작, 추후 조정).
- 보스 HP 배율: `hp * 20`. ATK/DEF 배율은 기본 1로 시작, 필요 시 배율 테이블 추가.
- 제한시간: 엘리트 20초, 보스 30초.

### 3.2 스탯 합산
- 기본 스탯: `StatsTable[level]` 값을 베이스로 사용.
- 강화/장비/버프 누적: 퍼센트 계열은 곱적용(1 + Σ), 고정값은 합산.  
  - 예) `finalATK = (baseATK + flatATK) * (1 + atkRate + atkr + touchBonus)`  
  - 방어: `effectiveDEF = baseDEF * (1 + defRate) * (1 - defIgnoreRate)`
- 크리: `critChance = clamp(baseCR + crUpgrades, 0, cap)`  
  `critDamage = baseCritMul * (1 + cdRate)`, 울트라 크리는 `critDamage *= critDamage` 적용.
- 공속/쿨감: `finalAttackInterval = baseInterval * (1 - atkspRate)` (하한 필요 시 추가), `finalCooldown = baseCooldown * (1 - rcdRate)`.

### 3.3 피해 계산
- `damage = max(1, atk * critMultiplier * bossOrEliteMul - mitigatedDefense)`
- `mitigatedDefense = targetDEF * (1 - defIgnoreRate) * (1 - defPenetration)`; 방무/방관 중첩 규칙은 곱적용으로 시작.

### 3.4 강화 비용 (4구간)
- 구간별 누적 곱:  
  `cost(level) = GoldBase * GoldPow^(levelInTier0) * grow1^(levelInTier1) * grow2^(levelInTier2) * grow3^(levelInTier3) * grow4^(levelInTier4)`  
  - `levelInTierX`는 해당 구간에서 소비된 레벨 수(0 이상).
  - 예시(UP001): `10 * 1.015^3000 * 1.025^5000 * 1.04^7000 * 1.05^10000`.

### 3.5 보상/드롭
- 기본 골드: MonsterRow.Gold. 엘리트/보스 보상 배율은 MonsterKind별 배율 테이블로 관리(초기값 1, 추후 기획 반영).
- 골드 획득량 증가: `rewardGold = baseGold * (1 + goldr)`.

## 4. 서비스 계약 개요
- CombatService: 스테이지/웨이브 진행, 몬스터 스폰/HP 관리, 터치 공격 훅, 시간제한 판정, 보상 이벤트 발행.
- StatService: StatsTable + Upgrade/장비/버프를 합산하여 실시간 스냅샷 반환.
- CurrencyService: 재화 잔액 관리(Add/TryConsume/Get), 미접속 보상 지급, 저장/로드.

## 5. 미접속 보상
- 최대 누적 시간: `maxOfflineHours = 6h + offtUpgradeHours`.
- 보상 계산: `elapsed = clamp(now - lastSave, 0, maxOfflineHours)`, `reward = baseIncomePerSec * elapsedSeconds * (1 + offaRate) * (1 + goldr)`.
- 지급 경로: CurrencyService가 계산 후 Add, EventBus로 `RewardGranted` 발행.

