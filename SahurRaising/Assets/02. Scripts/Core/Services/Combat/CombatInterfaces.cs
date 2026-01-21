using System.Collections.Generic;
using BreakInfinity;
using Cysharp.Threading.Tasks;

namespace SahurRaising.Core
{
    // 스냅샷/보상/이벤트용 구조체
    public struct CharacterStats
    {
        public BigDouble Attack;
        public BigDouble MaxHP;
        public BigDouble Defense;
        public BigDouble HealthRegen;
        public int CharacterLevel;

        public double AttackSpeed;
        public double CritChance;
        public double CritMultiplier;
        public double TouchDamageMultiplier;

        public double GoldBonusRate;
        public double AttackRate;
        public double OfflineTimeMinutes;
        public double OfflineAmountRate;
        public double CooldownReduction;
        public double UltraCritChance;
        public double AttackBonus;
        public double CritDamageBonus;
        public double DefenseRate;
        public double DefenseIgnore;
        public double BossDamageRate;
        public double EliteDamageRate;
        
        /// <summary>
        /// 동시에 공격할 수 있는 최대 몬스터 수 (Evolution 레벨 기반)
        /// 기본값: 3, Evolution 레벨당 +1
        /// </summary>
        public int MaxTargetCount;
    }

    /// <summary>
    /// 스탯 보정 단위(정수/비율) 전달용
    /// </summary>
    public struct StatModifier
    {
        public StatType Stat;
        public double Flat;
        public double Rate;
    }

    /// <summary>
    /// 전투 보상(재화) 묶음
    /// </summary>
    public struct CombatReward
    {
        public BigDouble Gold;
        public BigDouble Emerald;
        public BigDouble Diamond;
        public int Ticket;
    }

    /// <summary>
    /// 몬스터 처치 시 발행하는 이벤트 payload
    /// </summary>
    public struct EnemyDefeatedEvent
    {
        public int StageIndex;
        public int WaveIndex;
        public MonsterKind MonsterKind;
        public CombatReward Reward;
    }

    /// <summary>
    /// 스테이지 클리어/실패 결과 이벤트 payload
    /// </summary>
    public struct StageResultEvent
    {
        public int StageIndex;
        public bool IsClear;
    }

    /// <summary>
    /// 재화 지급 시 발행하는 이벤트 payload(미접속 보상 등)
    /// </summary>
    public struct RewardGrantedEvent
    {
        public CurrencyType CurrencyType;
        public BigDouble Amount;
        public string Source;
    }

    /// <summary>
    /// 재화 소모 시 발행하는 이벤트 payload
    /// </summary>
    public struct CurrencyConsumedEvent
    {
        public CurrencyType CurrencyType;
        public BigDouble Amount;
        public string Reason;
    }

    // 서비스 인터페이스
    public interface ICurrencyService
    {
        UniTask InitializeAsync();
        BigDouble Get(CurrencyType type);
        bool TryConsume(CurrencyType type, BigDouble amount, string reason = null);
        void Add(CurrencyType type, BigDouble amount, string reason = null);
        UniTask SaveAsync();
        UniTask LoadAsync();
        CurrencyData GetCurrencyData(CurrencyType type);
    }

    public interface IUpgradeService
    {
        UniTask InitializeAsync();
        int GetLevel(string code);
        IReadOnlyDictionary<string, int> GetAllLevels();
        BigDouble GetNextCost(string code);
        bool TryUpgrade(string code, int levels, out int appliedLevels, out BigDouble totalCost);
        UniTask SaveAsync();
        UniTask LoadAsync();
    }

    public interface IStatService
    {
        UniTask InitializeAsync();
        CharacterStats GetSnapshot();
        void ApplyUpgrades(IReadOnlyDictionary<string, int> upgrades);
        void ApplyEquipmentModifiers(IEnumerable<StatModifier> modifiers);
        void ApplySkillModifiers(IEnumerable<StatModifier> modifiers);
        void ApplyEquippedItems(IEnumerable<string> equipmentCodes, int level = 1);
        bool TryMapOptionType(string optionType, out StatType statType);
        BigDouble GetStatValue(string upgradeCode, int level);
    }

    /// <summary>
    /// 공격 유형 (확장 가능)
    /// </summary>
    public enum AttackType
    {
        Auto,       // 자동 공격
        Touch,      // 터치 공격
        Skill,      // 스킬 공격 (추후 확장)
        Counter,    // 반격 (추후 확장 예시)
        DoT,        // 도트 데미지 (추후 확장 예시)
    }

    /// <summary>
    /// 공격 발생 시 발행하는 이벤트 payload
    /// 공격자, 데미지, 크리티컬 여부 등을 포함합니다.
    /// </summary>
    public struct AttackEvent
    {
        /// <summary>true면 플레이어 공격, false면 몬스터 공격</summary>
        public bool IsPlayerAttack;
        /// <summary>실제 적용된 데미지량</summary>
        public BigDouble Damage;
        /// <summary>크리티컬 히트 여부</summary>
        public bool IsCritical;
        /// <summary>공격 유형</summary>
        public AttackType AttackType;
        /// <summary>다중 타겟 공격 시 타겟 인덱스 (0 = 첫 번째/단일 타겟)</summary>
        public int TargetIndex;
        /// <summary>다중 공격 시 연속 공격 인덱스 (0 = 첫 번째 공격)</summary>
        public int HitIndex;
        /// <summary>이 공격이 다중 공격 중 마지막인지 여부</summary>
        public bool IsLastHit;
    }

    public interface ICombatService
    {
        UniTask InitializeAsync();
        UniTask StartStageAsync(int stageIndex, int waveIndex = 1);
        void Tick(float deltaTime);
        void ApplyTouchAttack();
        CombatProgress GetProgress();
        UniTask SaveAsync();
        UniTask LoadAsync();
        
        /// <summary>공격 발생 시 이벤트 (플레이어/몬스터 모두 포함)</summary>
        event System.Action<AttackEvent> OnAttack;
        
        /// <summary>동시 공격 가능한 최대 타겟 수</summary>
        int GetMaxTargetCount();
        
        /// <summary>현재 스테이지에 맞는 몬스터 스폰 정보</summary>
        MonsterSpawnInfo GetMonsterSpawnInfo();
        
        /// <summary>몬스터 처치 시 호출</summary>
        void OnMonsterKilled(MonsterKind kind, BreakInfinity.BigDouble goldReward);
        
        /// <summary>웨이브 완료 체크</summary>
        void CheckWaveComplete(int requiredKills);
        
        /// <summary>방어 무시율 반환</summary>
        double GetDefenseIgnoreRate();
        
        /// <summary>플레이어에게 데미지 적용</summary>
        void DealDamageToPlayer(BreakInfinity.BigDouble damage);
        
        /// <summary>데미지 계산</summary>
        BreakInfinity.BigDouble CalculateDamage(bool isTouch, out bool isCritical);
        
        /// <summary>스테이지당 웨이브 수 설정 (CombatSettings에서 주입)</summary>
        void SetWavesPerStage(int count);
        
        /// <summary>현재 웨이브 인덱스 반환</summary>
        int GetCurrentWaveIndex();
    }
    
    /// <summary>
    /// 몬스터 스폰 정보
    /// </summary>
    public struct MonsterSpawnInfo
    {
        public int Level;
        public MonsterKind Kind;
        public BreakInfinity.BigDouble MaxHp;
        public BreakInfinity.BigDouble Defense;
        public BreakInfinity.BigDouble Attack;
        public BreakInfinity.BigDouble GoldReward;
        public float TimeLimit;
    }
}


