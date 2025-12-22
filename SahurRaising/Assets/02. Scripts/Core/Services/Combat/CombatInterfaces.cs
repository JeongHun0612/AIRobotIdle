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
        public double BossDamageRate; // 보스 피해 추가 배율(기획 데이터 존재 시 사용)
        public double EliteDamageRate; // 엘리트 피해 추가 배율(현재 기획 데이터 없음)
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

    // 서비스 인터페이스
    public interface ICurrencyService
    {
        UniTask InitializeAsync();
        BigDouble Get(CurrencyType type);
        bool TryConsume(CurrencyType type, BigDouble amount, string reason = null);
        void Add(CurrencyType type, BigDouble amount, string reason = null);
        UniTask SaveAsync();
        UniTask LoadAsync();
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
        void ApplyEquippedItems(IEnumerable<string> equipmentCodes, int level = 1);
        bool TryMapOptionType(string optionType, out StatType statType);
        BigDouble GetStatValue(string upgradeCode, int level);
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
    }
}

