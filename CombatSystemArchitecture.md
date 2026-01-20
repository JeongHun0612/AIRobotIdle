# Combat System Architecture

전투 시스템의 구조와 주요 컴포넌트를 설명하는 문서입니다.

---

## 📁 파일 구조

```
Assets/02. Scripts/
├── Core/
│   ├── ObjectPool.cs
│   └── Services/Combat/
│       ├── CombatInterfaces.cs
│       ├── CombatService.cs
│       └── StatService.cs
│
└── GamePlay/
    ├── CombatRunner.cs
    ├── CombatInputHandler.cs
    ├── CombatSettings.cs
    ├── MonsterSpawner.cs
    ├── BackgroundScroller.cs
    ├── UnitView.cs
    ├── PlayerUnitView.cs
    └── MonsterUnitView.cs
```

---

## 🏗️ 계층 구조

```
┌─────────────────────────────────────────────────────────────┐
│                      GameManager                             │
│  (MonoSingleton, 서비스 초기화 및 게임 흐름 진입점)           │
└───────────────────────────┬─────────────────────────────────┘
                            │ ServiceLocator 등록
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                     Service Layer (Core)                     │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐  │
│  │  EventBus   │  │ StatService │  │  CurrencyService    │  │
│  └──────┬──────┘  └──────┬──────┘  └──────────┬──────────┘  │
│         │                │                     │            │
│         ▼                ▼                     ▼            │
│  ┌──────────────────────────────────────────────────────┐   │
│  │               CombatService (ICombatService)          │   │
│  │  - 스테이지/웨이브 로직                               │   │
│  │  - 데미지 계산, 보상 처리                             │   │
│  │  - 자동공격/터치공격 게이지                           │   │
│  │  - OnAttack 이벤트 발행                               │   │
│  └──────────────────────────────────────────────────────┘   │
│  ┌──────────────────────────────────────────────────────┐   │
│  │               ObjectPool<T>                           │   │
│  │  - MonoObjectPool<MonoBehaviour>                      │   │
│  │  - GC 할당 최소화를 위한 오브젝트 재사용              │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────┬───────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                  Presentation Layer (GamePlay)               │
│  ┌──────────────────────────────────────────────────────┐   │
│  │                CombatRunner (Orchestrator)            │   │
│  │  - FSM (Moving / Fighting / Transition)               │   │
│  │  - 하위 컴포넌트 조율                                  │   │
│  │  - 배경/플레이어 이동 제어                             │   │
│  └───────────┬───────────────────────────┬──────────────┘   │
│              │                           │                   │
│   ┌──────────▼────────────┐   ┌──────────▼─────────────┐    │
│   │    MonsterSpawner     │   │   CombatInputHandler   │    │
│   │  - ObjectPool 통합     │   │  - 터치 입력 처리      │    │
│   │  - 스폰/해제 관리      │   │  - UI 충돌 방지        │    │
│   │  - 현재 타겟 관리      │   └────────────────────────┘    │
│   └───────────────────────┘                                  │
│                                                              │
│   ┌───────────────────────┐   ┌────────────────────────┐    │
│   │  BackgroundScroller   │   │ UnitView (Base)        │    │
│   │  - 패럴랙스 배경       │   │   ├─ PlayerUnitView    │    │
│   │  - 부드러운 속도 전환  │   │   └─ MonsterUnitView   │    │
│   └───────────────────────┘   └────────────────────────┘    │
│                                                              │
│   ┌──────────────────────────────────────────────────────┐  │
│   │               CombatSettings (ScriptableObject)       │  │
│   │  - 스폰 설정, 이동 속도, 몬스터 프리팹 등             │  │
│   └──────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

---

## 📦 주요 컴포넌트

### CombatService (Core)

스테이지 진행, 데미지 계산, 이벤트 발행을 담당하는 서비스 클래스입니다.

```csharp
public interface ICombatService
{
    UniTask InitializeAsync();
    UniTask StartStageAsync(int stageIndex, int waveIndex = 1);
    void Tick(float deltaTime);
    void ApplyTouchAttack();
    CombatProgress GetProgress();
    UniTask SaveAsync();
    UniTask LoadAsync();
    
    event Action<AttackEvent> OnAttack;
}
```

**공격 속도 공식:**
```
최종 공격속도 = BASE_ATTACK_SPEED × (1 + stats.AttackSpeed)

- BASE_ATTACK_SPEED = 0.5 (2초당 1회)
- stats.AttackSpeed = 업그레이드 테이블 수치 (1 = 100%, 0.0005 = 0.05%)
```

---

### AttackEvent (Core)

공격 발생 시 발행되는 이벤트 구조체입니다.

```csharp
public enum AttackType
{
    Auto,       // 자동 공격
    Touch,      // 터치 공격
    Skill,      // 스킬 공격
    Counter,    // 반격
    DoT,        // 도트 데미지
}

public struct AttackEvent
{
    public bool IsPlayerAttack;      // true = 플레이어, false = 몬스터
    public BigDouble Damage;         // 데미지량
    public bool IsCritical;          // 크리티컬 여부
    public AttackType AttackType;    // 공격 유형
    public int TargetIndex;          // 다중 타겟 인덱스
    public int HitIndex;             // 다중 공격 히트 인덱스
    public bool IsLastHit;           // 마지막 히트 여부
}
```

**다중 공격 활용 예시:**
```csharp
// 3회 연속 공격
for (int i = 0; i < 3; i++)
{
    OnAttack?.Invoke(new AttackEvent
    {
        AttackType = AttackType.Skill,
        HitIndex = i,
        IsLastHit = (i == 2)
    });
}
```

---

### CombatRunner (GamePlay)

전투 흐름을 관리하는 오케스트레이터 컴포넌트입니다.

**전투 페이즈 FSM:**
```
┌─────────────┐    몬스터 도달    ┌─────────────┐
│   Moving    │ ───────────────► │  Fighting   │
│ (이동 중)   │                   │ (전투 중)   │
└──────┬──────┘                   └──────┬──────┘
       ▲                                 │
       │         몬스터 전멸             │
       └─────────────────────────────────┘
               (TransitionToMove)
```

| 페이즈 | 배경 스크롤 | 플레이어 위치 | 몬스터 스폰 |
|--------|-----------|--------------|------------|
| Moving | ON | 전진 (Advanced) | ON |
| Fighting | OFF | 복귀 (Idle) | OFF |
| Transition | - | - | - |

---

### MonsterSpawner (GamePlay)

몬스터 스폰과 오브젝트 풀링을 담당합니다.

```csharp
public class MonsterSpawner : MonoBehaviour
{
    // 주요 API
    void Initialize(CombatSettings settings, Transform spawnPoint);
    void StartSpawning();
    void StopSpawning();
    void ResetWave();
    
    UnitView SpawnMonster();           // 풀에서 가져옴
    void ReleaseMonster(UnitView m);   // 풀로 반환
    UnitView GetCurrentTarget();       // 현재 타겟
    
    // 프로퍼티
    IReadOnlyList<UnitView> ActiveMonsters { get; }
    int ActiveMonsterCount { get; }
}
```

---

### CombatInputHandler (GamePlay)

터치/클릭 입력을 처리합니다.

```csharp
public class CombatInputHandler : MonoBehaviour
{
    void Initialize(ICombatService combatService);
    void SetEnabled(bool enabled);
}
```

**입력 처리 흐름:**
1. `Input.GetMouseButtonDown(0)` 감지
2. `EventSystem.IsPointerOverGameObject()` 체크 (UI 충돌 방지)
3. `ICombatService.ApplyTouchAttack()` 호출

---

### ObjectPool<T> (Core)

GC 할당을 최소화하기 위한 오브젝트 풀 시스템입니다.

```csharp
public interface IObjectPool<T> where T : class
{
    T Get();
    void Release(T obj);
    void Clear();
    int CountActive { get; }
    int CountInactive { get; }
}

public class MonoObjectPool<T> : IObjectPool<T> where T : MonoBehaviour
{
    // 생성자
    MonoObjectPool(
        T prefab,
        Transform parent,
        int initialSize = 0,
        int maxSize = 100,
        Action<T> onGet = null,
        Action<T> onRelease = null
    );
}
```

---

### CombatSettings (GamePlay)

전투 관련 설정값을 저장하는 ScriptableObject입니다.

| 카테고리 | 프로퍼티 | 설명 |
|---------|---------|------|
| 몬스터 스폰 | MonstersPerWave | 웨이브당 몬스터 수 |
| | MaxMonstersOnScreen | 동시 최대 몬스터 수 |
| | SpawnInterval | 스폰 간격 (초) |
| | MonsterSpawnYOffset | Y축 랜덤 오프셋 |
| 이동 | MonsterMoveSpeed | 몬스터 이동 속도 |
| | PlayerMoveSpeed | 플레이어 이동 속도 |
| | PlayerAdvanceDistance | 플레이어 전진 거리 |
| | PlayerReturnSpeedMultiplier | 복귀 속도 배율 |
| | BackgroundScrollSpeed | 배경 스크롤 속도 |
| 전투 | AttackRange | 공격 사거리 |
| | DeathToSpawnDelay | 사망 후 풀 반환 대기 |

---

### UnitView 상속 구조 (GamePlay)

```
UnitView (Base)
├── Initialize()
├── PlayMove(bool isMoving)
├── PlayAttack()
├── PlayDie()
├── Flip(bool isLeft)
│
├── PlayerUnitView
│   └── 바퀴 회전(Wobble/Bounce) 로직
│
└── MonsterUnitView
    └── 확장용 빈 클래스
```

---

## 🔄 이벤트 흐름

### 공격 이벤트 흐름

```
CombatService.ProcessAutoAttack()
    │
    ├─► CalculateDamage() → damage, isCritical
    │
    ├─► DealDamage(damage)
    │
    └─► OnAttack.Invoke(AttackEvent)
            │
            ▼
CombatRunner.OnAttack(AttackEvent evt)
    │
    ├─► evt.IsPlayerAttack → _playerInstance.PlayAttack()
    │      └─► evt.IsCritical → 크리티컬 이펙트
    │
    └─► !evt.IsPlayerAttack → _currentTarget.PlayAttack()
```

### 몬스터 처치 이벤트 흐름

```
CombatService.DealDamage() → HP ≤ 0
    │
    └─► EventBus.Publish(EnemyDefeatedEvent)
            │
            ▼
CombatRunner.OnEnemyDefeated()
    │
    ├─► _currentTarget.PlayDie()
    │
    ├─► ReleaseMonsterDelayed() → MonsterSpawner.ReleaseMonster()
    │
    └─► ActiveMonsterCount == 0 → EnterTransitionPhase()
```

---

## 📝 에디터 설정

### CombatRunner 인스펙터

```
CombatRunner
├── 필수 레퍼런스
│   ├── Settings: CombatSettings (SO)
│   ├── Background Scroller: BackgroundScroller
│   ├── Player Spawn Point: Transform
│   ├── Monster Spawn Point: Transform
│   └── Player Prefab: PlayerUnitView
│
├── 하위 컴포넌트 (자동 AddComponent 지원)
│   ├── Monster Spawner: MonsterSpawner (Optional)
│   └── Input Handler: CombatInputHandler (Optional)
│
└── 디버그
    └── Show Debug Logs: bool
```

### MonsterSpawner 인스펙터

```
MonsterSpawner
├── 스폰 설정
│   ├── Spawn Point: Transform
│   ├── Pool Initial Size: 3
│   └── Pool Max Size: 10
```

---

## 🧩 확장 포인트

| 기능 | 확장 방법 |
|------|----------|
| 새로운 공격 유형 | `AttackType` enum에 추가 |
| 스킬 시스템 | `AttackType.Skill` 사용, `SkillService` 연동 |
| 다중 타겟 공격 | `TargetIndex` 활용 |
| 다중 연속 공격 | `HitIndex`, `IsLastHit` 활용 |
| 몬스터 AI | `MonsterUnitView` 확장 |
| 새로운 입력 방식 | `CombatInputHandler` 수정 또는 교체 |
