# Combat System Architecture (v2.1)

다수 몬스터 동시 전투를 지원하는 전투 시스템의 구조와 주요 컴포넌트를 설명하는 문서입니다.

---

## 📁 파일 구조

```
Assets/02. Scripts/
├── Core/
│   ├── ObjectPool.cs
│   ├── Data/
│   │   └── GameDataSchemas.cs    # MonsterKind, CurrencyType 등 Enum 정의
│   └── Services/Combat/
│       ├── CombatInterfaces.cs   # ICombatService, MonsterSpawnInfo, CharacterStats
│       ├── CombatService.cs      # 스테이지/웨이브 진행, 공격 이벤트 발행
│       └── StatService.cs        # 스탯 계산, MaxTargetCount 관리
│
└── GamePlay/
    ├── CombatRunner.cs           # FSM 오케스트레이터, 다수 타겟 공격
    ├── CombatInputHandler.cs     # 터치 입력 처리
    ├── CombatSettings.cs         # SO 설정값 (웨이브별 구성)
    ├── MonsterSpawner.cs         # 스폰/풀링, 간격 유지 로직
    ├── BackgroundScroller.cs     # 배경 스크롤
    ├── UnitView.cs               # 유닛 기본 클래스
    ├── PlayerUnitView.cs         # 플레이어 전용
    └── MonsterUnitView.cs        # 몬스터 전용 (개별 HP 관리)
```

---

## 🏗️ 시스템 아키텍처

```
┌─────────────────────────────────────────────────────────────┐
│                      GameManager                             │
│  (MonoSingleton, 서비스 초기화 및 게임 흐름 진입점)           │
└───────────────────────────┬─────────────────────────────────┘
                            │ ServiceLocator 등록
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                     Service Layer (Core)                     │
│  ┌─────────────────────────────────────────────────────┐    │
│  │               CombatService (ICombatService)         │    │
│  │  - 스테이지/웨이브 진행 관리                         │    │
│  │  - 공격 게이지, 자동 공격 처리                       │    │
│  │  - 데미지 계산, 이벤트 발행                          │    │
│  │  - 몬스터 스탯 정보 제공 (MonsterSpawnInfo)          │    │
│  │  - MaxTargetCount 제공                               │    │
│  │  - SetWavesPerStage() 외부 설정 주입                 │    │
│  └─────────────────────────────────────────────────────┘    │
│                                                              │
│  ┌─────────────────────────────────────────────────────┐    │
│  │                   StatService                        │    │
│  │  - CharacterStats 빌드                               │    │
│  │  - Evolution 레벨 기반 MaxTargetCount 계산           │    │
│  │  - 기본값: 3, Evolution 레벨당 +1                    │    │
│  └─────────────────────────────────────────────────────┘    │
└─────────────────────────────┬───────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                  Presentation Layer (GamePlay)               │
│  ┌──────────────────────────────────────────────────────┐   │
│  │                CombatRunner (Orchestrator)            │   │
│  │  - FSM (Moving / Fighting / Transition)               │   │
│  │  - 다수 타겟 공격 처리                                │   │
│  │  - 몬스터 사망 처리 및 보상 요청                      │   │
│  │  - CombatSettings.WavesPerStage를 CombatService에 주입│   │
│  └───────────┬───────────────────────────┬──────────────┘   │
│              │                           │                   │
│   ┌──────────▼────────────┐   ┌──────────▼─────────────┐    │
│   │    MonsterSpawner     │   │   CombatInputHandler   │    │
│   │  - 오브젝트 풀링       │   │  - 터치 입력 처리      │    │
│   │  - 간격 유지 로직      │   │  - UI 충돌 방지        │    │
│   │  - EngagedMonsters    │   └────────────────────────┘    │
│   │  - WaveConfig 기반     │                                 │
│   │    몬스터 종류 결정    │                                 │
│   └───────────────────────┘                                  │
│                                                              │
│   ┌──────────────────────────────────────────────────────┐  │
│   │               MonsterUnitView                         │  │
│   │  - 개별 HP/방어력 관리                                │  │
│   │  - TakeDamage() 메서드                                │  │
│   │  - 간격 대기 시 Idle_A 애니메이션                     │  │
│   │  - 사망 후 자동 비활성화                              │  │
│   └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

---

## � 웨이브별 세부 설정 시스템

### WaveConfig 구조

```csharp
[Serializable]
public class WaveConfig
{
    [Header("=== 몬스터 종류별 스폰 개수 ===")]
    public int NormalCount = 10;   // 일반 몬스터 수
    public int EliteCount = 0;     // 엘리트 몬스터 수
    public int BossCount = 0;      // 보스 몬스터 수
    
    [Header("=== 스폰 설정 ===")]
    public int MaxMonstersOnScreen = 5;
    public float SpawnInterval = 0.5f;
    
    public string Description;     // 메모 (에디터용)
    
    // 자동 계산 프로퍼티
    public int TotalMonstersToSpawn => NormalCount + EliteCount + BossCount;
    public bool HasBoss => BossCount > 0;
    public bool HasElite => EliteCount > 0;
}
```

### 웨이브 인덱스 매핑

```
Wave Configs 리스트 인덱스  →  웨이브 번호

Element 0  =  1웨이브
Element 1  =  2웨이브
Element 2  =  3웨이브
Element 3  =  4웨이브
(리스트에 없는 웨이브는 기본값 사용)
```

### 에디터 설정 예시

```
=== 웨이브별 세부 설정 ===
Wave Configs [Size: 4]

├── Element 0 (1웨이브 - 쉬운 시작)
│   ├── Normal Count: 5           ← 일반 5마리만
│   ├── Elite Count: 0
│   ├── Boss Count: 0
│   ├── Max Monsters On Screen: 3
│   ├── Spawn Interval: 0.3
│   └── Description: "튜토리얼 웨이브"

├── Element 1 (2웨이브 - 본격 시작)
│   ├── Normal Count: 8
│   ├── Elite Count: 2            ← 일반 8 + 엘리트 2 혼합!
│   ├── Boss Count: 0
│   └── ...

├── Element 2 (3웨이브 - 난이도 상승)
│   ├── Normal Count: 10
│   ├── Elite Count: 5            ← 일반 10 + 엘리트 5 혼합!
│   ├── Boss Count: 0
│   └── ...

└── Element 3 (4웨이브 - 보스전)
    ├── Normal Count: 0
    ├── Elite Count: 2            ← 엘리트 2마리 후
    ├── Boss Count: 1             ← 보스 1마리!
    ├── Max Monsters On Screen: 1
    └── Description: "보스 웨이브"
```

### 몬스터 스폰 순서

몬스터는 **Normal → Elite → Boss** 순서로 스폰됩니다:

```
예: Normal=8, Elite=2, Boss=0 설정

스폰 순서:
1~8번째   → Normal 몬스터
9~10번째  → Elite 몬스터
```

```csharp
// MonsterSpawner.DetermineMonsterKind()
private MonsterKind DetermineMonsterKind()
{
    var waveConfig = _settings.GetWaveConfig(_currentWaveIndex);
    
    int normalEnd = waveConfig.NormalCount;
    int eliteEnd = normalEnd + waveConfig.EliteCount;
    
    if (_monstersSpawnedThisWave < normalEnd)
        return MonsterKind.Normal;
    else if (_monstersSpawnedThisWave < eliteEnd)
        return MonsterKind.Elite;
    else
        return MonsterKind.Boss;
}
```

---

## �🎮 다수 몬스터 동시 전투

### MaxTargetCount

플레이어가 동시에 공격할 수 있는 최대 몬스터 수입니다.

```csharp
// StatService에서 계산
int evolutionLevel = 0; // Evolution 시스템에서 가져올 예정
int maxTargetCount = 3 + evolutionLevel;  // 기본값 3

// CharacterStats에 포함
public struct CharacterStats
{
    // ... 기존 스탯들 ...
    public int MaxTargetCount;  // 동시 공격 대상 수
}
```

**확장 계획:**
- Evolution 레벨 1당 MaxTargetCount +1
- 기본값: 3마리 동시 공격

---

### 몬스터 간격 유지

```
[몬스터 A] ─── minXSpacing ─── [몬스터 B] ─── minXSpacing ─── [몬스터 C]
     │                              │                              │
     └──── 전투 참여 (Engaged) ─────┴──────── 대기 중 (Idle_A) ────┘
```

**로직:**
1. 몬스터들을 플레이어와의 거리 기준으로 정렬
2. MaxTargetCount까지 전투 참여 허용
3. 이미 전투 중인 몬스터와 X 간격 체크
4. 간격이 좁으면 대기 상태로 전환 (Idle_A 애니메이션)

```csharp
// MonsterSpawner.UpdateMonsterStates()
for (int i = 0; i < _activeMonsters.Count; i++)
{
    bool canEngage = _engagedMonsters.Count < maxTargets && isInRange;
    
    if (canEngage && _engagedMonsters.Count > 0)
    {
        float xDiff = Mathf.Abs(monster.transform.position.x - lastEngagedX);
        if (xDiff < _settings.MonsterXSpacing)
        {
            canEngage = false;  // 간격이 좁으면 대기
        }
    }
    
    if (canEngage)
    {
        _engagedMonsters.Add(monster);
        monster.SetWaitingForSpace(false);
    }
    else if (isInRange)
    {
        monster.SetWaitingForSpace(true);  // Idle_A 애니메이션
    }
}
```

---

## 📦 주요 컴포넌트

### CombatSettings

웨이브별 세부 설정을 관리하는 ScriptableObject입니다.

```csharp
[CreateAssetMenu(fileName = "CombatSettings", menuName = "SahurRaising/Combat/CombatSettings")]
public class CombatSettings : ScriptableObject
{
    // 웨이브별 세부 설정
    List<WaveConfig> _waveConfigs;
    
    // 기본값 (웨이브 설정이 없을 때 사용)
    int _defaultMonstersPerWave = 10;
    int _defaultMaxMonstersOnScreen = 5;
    float _defaultSpawnInterval = 0.5f;
    
    // 스테이지 설정
    int _wavesPerStage = 4;
    
    // 주요 API
    WaveConfig GetWaveConfig(int waveIndex);
    int GetMonstersPerWave(int waveIndex);
    int GetMaxMonstersOnScreen(int waveIndex);
    float GetSpawnInterval(int waveIndex);
}
```

### MonsterUnitView

개별 몬스터의 HP와 상태를 관리합니다.

```csharp
public class MonsterUnitView : UnitView
{
    // 런타임 데이터
    private BigDouble _maxHp;
    private BigDouble _currentHp;
    private BigDouble _defense;
    private bool _isWaitingForSpace;
    private int _monsterLevel;
    private MonsterKind _monsterKind;
    
    // 스탯 초기화 (스폰 시 호출)
    public void SetupStats(BigDouble maxHp, BigDouble defense, int level, MonsterKind kind);
    
    // 데미지 적용 (방어력 고려)
    public BigDouble TakeDamage(BigDouble damage, double defenseIgnoreRate = 0);
    
    // 간격 대기 상태 (Idle_A 애니메이션)
    public void SetWaitingForSpace(bool waiting);
    
    // 풀 반환 준비
    public void ResetForPool();
}
```

**애니메이션 해시:**
- `Walk`: 이동 중
- `Idle_A`: 간격 대기 중
- `Attack`: 공격 중
- `Death`: 사망

---

### MonsterSpawner

몬스터 스폰과 상태 관리를 담당합니다.

```csharp
public class MonsterSpawner : MonoBehaviour
{
    // 웨이브 인덱스 추적
    private int _currentWaveIndex = 1;
    
    // 읽기 전용 프로퍼티
    IReadOnlyList<MonsterUnitView> ActiveMonsters { get; }
    IReadOnlyList<MonsterUnitView> EngagedMonsters { get; }
    int EngagedMonsterCount { get; }
    
    // 주요 API
    void Initialize(CombatSettings settings, Transform spawnPoint, 
                    Transform playerTransform, ICombatService combatService);
    void StartSpawning(int waveIndex = 1);  // 웨이브 인덱스 전달
    MonsterUnitView SpawnMonster();
    void HandleMonsterDeath(MonsterUnitView monster);
    IReadOnlyList<MonsterUnitView> GetEngagedTargets();
    
    // 내부 로직
    private MonsterKind DetermineMonsterKind();  // WaveConfig 기반 종류 결정
}
```

---

### CombatService

스테이지 진행과 공격 이벤트를 관리합니다.

```csharp
public interface ICombatService
{
    // 스테이지 관리
    UniTask StartStageAsync(int stageIndex, int waveIndex = 1);
    void CheckWaveComplete(int requiredKills);
    CombatProgress GetProgress();
    
    // 웨이브 설정 (외부에서 주입)
    void SetWavesPerStage(int count);
    int GetCurrentWaveIndex();
    
    // 몬스터 정보
    MonsterSpawnInfo GetMonsterSpawnInfo();
    int GetMaxTargetCount();
    
    // 전투
    void Tick(float deltaTime);
    void ApplyTouchAttack();
    BigDouble CalculateDamage(bool isTouch, out bool isCritical);
    double GetDefenseIgnoreRate();
    
    // 피해
    void OnMonsterKilled(MonsterKind kind, BigDouble goldReward);
    void DealDamageToPlayer(BigDouble damage);
    
    // 이벤트
    event Action<AttackEvent> OnAttack;
}
```

---

## 🔄 전투 흐름

### 초기화 흐름

```
CombatRunner.InitializeAsync()
    │
    ├─► GameManager 초기화 대기
    │
    ├─► 서비스 연결 (CombatService, EventBus)
    │
    ├─► 플레이어 스폰
    │
    ├─► 하위 컴포넌트 초기화
    │       ├─► MonsterSpawner.Initialize()
    │       └─► CombatInputHandler.Initialize()
    │
    ├─► CombatService.SetWavesPerStage(settings.WavesPerStage)
    │
    └─► CombatService.StartStageAsync(1)
```

### 공격 처리 흐름

```
CombatService.ProcessAutoAttack()
    │
    └─► OnAttack.Invoke(AttackEvent)
            │
            ▼
CombatRunner.OnAttack(AttackEvent evt)
    │
    ├─► _playerInstance.PlayAttack()
    │
    ├─► 전투 중인 몬스터들 순회 (최대 MaxTargetCount)
    │       │
    │       ├─► monster.TakeDamage(damage, defenseIgnore)
    │       │
    │       └─► HP ≤ 0 → HandleMonsterDeath(monster)
    │               │
    │               ├─► monster.PlayDie()
    │               ├─► _combatService.OnMonsterKilled()
    │               └─► _monsterSpawner.HandleMonsterDeath()
    │
    └─► 크리티컬 이펙트 (evt.IsCritical)
```

### 웨이브 진행 흐름

```
CombatRunner.HandleMonsterDeath()
    │
    ├─► _monstersKilledThisWave++
    │
    ├─► monstersPerWave = _settings.GetMonstersPerWave(_currentWaveIndex)
    │
    ├─► _combatService.CheckWaveComplete(monstersPerWave)
    │
    └─► if (_monstersKilledThisWave >= monstersPerWave)
            │
            ├─► _currentWaveIndex++
            ├─► _monsterSpawner.ResetWave()
            └─► _monstersKilledThisWave = 0
```

---

## 📝 에디터 설정

### CombatSettings 설정값

| 카테고리 | 프로퍼티 | 기본값 | 설명 |
|---------|---------|-------|------|
| **웨이브별 설정** | WaveConfigs | [] | 웨이브별 상세 구성 |
| **기본값** | DefaultMonstersPerWave | 10 | 기본 웨이브당 몬스터 수 |
| | DefaultMaxMonstersOnScreen | 5 | 기본 동시 화면 최대 수 |
| | DefaultSpawnInterval | 0.5s | 기본 스폰 간격 |
| **스테이지** | WavesPerStage | 4 | 스테이지당 웨이브 수 |
| **간격** | MonsterXSpacing | 1.5 | 몬스터 간 X 거리 |
| | MonsterYRandomRange | 0.5 | Y축 랜덤 범위 |
| **전투** | AttackRange | 1.5 | 공격 사거리 |

### WaveConfig 설정값

| 프로퍼티 | 기본값 | 설명 |
|---------|-------|------|
| NormalCount | 10 | 일반 몬스터 수 |
| EliteCount | 0 | 엘리트 몬스터 수 |
| BossCount | 0 | 보스 몬스터 수 |
| MaxMonstersOnScreen | 5 | 동시 화면 최대 수 |
| SpawnInterval | 0.5s | 스폰 간격 |
| Description | "" | 메모 (에디터용) |

### MonsterUnitView 애니메이터

필요한 애니메이션 상태:
- `Walk` (이동)
- `Idle_A` (간격 대기) ← **새로 추가 필요**
- `Attack` (공격)
- `Death` (사망)

---

## 🧩 확장 포인트

| 기능 | 확장 방법 |
|------|----------|
| Evolution 시스템 | StatService에서 evolutionLevel 변수 연동 |
| 다중 히트 공격 | AttackEvent.HitIndex 활용 |
| 보스 전용 연출 | MonsterKind.Boss 체크, WaveConfig.HasBoss 활용 |
| 범위 공격 | MaxTargetCount를 공격 범위용으로 활용 |
| 몬스터 특수 능력 | MonsterUnitView 확장 |
| 웨이브별 특수 효과 | WaveConfig에 추가 필드 정의 |

---

## ⚠️ 주의사항

1. **Idle_A 애니메이션 필요**: 몬스터 Animator에 `Idle_A` 상태를 추가해야 합니다.
2. **Evolution 시스템 연동**: 현재 evolutionLevel은 0으로 고정. 추후 Evolution 서비스 구현 시 연동 필요.
3. **오브젝트 풀 크기**: MaxMonstersOnScreen보다 큰 값으로 poolMaxSize 설정 권장.
4. **웨이브 인덱스 주의**: 코드 내부는 1-based 인덱스 사용 (1웨이브 = waveIndex 1).
저장된 데이터를 불러와서 시작하려면, 이 1 부분을 _combatService.GetProgress().CurrentStage 등으로 변경해야 합니다.
5. **스폰 순서**: 몬스터는 Normal → Elite → Boss 순서로 스폰됨.
