using System;
using System.Collections.Generic;
using BreakInfinity;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SahurRaising.Core
{
    /// <summary>
    /// 업그레이드/장비/스킬 모디파이어를 합산해 캐릭터 스냅샷을 생성하는 서비스
    /// 스탯 값은 StatsTable에서 레벨별로 조회합니다.
    /// </summary>
    public class StatService : IStatService
    {
        private const string UpgradeTableKey = nameof(UpgradeTable);
        private const string StatsTableKey = nameof(StatsTable);
        private const string EquipmentTableKey = nameof(EquipmentTable);

        private readonly IResourceService _resourceService;
        private readonly IEventBus _eventBus;

        private readonly Dictionary<string, int> _upgradeLevels = new();
        private readonly Dictionary<StatType, int> _statLevels = new();
        private readonly List<StatModifier> _equipmentModifiers = new();
        private readonly List<StatModifier> _skillModifiers = new();
        private readonly Dictionary<string, StatType> _optionTypeMap = new(StringComparer.OrdinalIgnoreCase);

        private UpgradeTable _upgradeTable;
        private StatsTable _statsTable;
        private EquipmentTable _equipmentTable;
        private CharacterStats _snapshot;

        public StatService(IResourceService resourceService, IEventBus eventBus)
        {
            _resourceService = resourceService;
            _eventBus = eventBus;
        }

        public async UniTask InitializeAsync()
        {
            _upgradeTable = await _resourceService.LoadTableAsync<UpgradeTable>(UpgradeTableKey);
            _statsTable = await _resourceService.LoadTableAsync<StatsTable>(StatsTableKey);
            _equipmentTable = await _resourceService.LoadTableAsync<EquipmentTable>(EquipmentTableKey);

            if (_upgradeTable == null)
                Debug.LogError("[StatService] UpgradeTable을 로드하지 못했습니다. Addressables 설정을 확인하세요.");

            if (_statsTable == null)
                Debug.LogError("[StatService] StatsTable을 로드하지 못했습니다. Addressables 설정을 확인하세요.");

            if (_equipmentTable == null)
                Debug.LogWarning("[StatService] EquipmentTable을 로드하지 못했습니다. 장비 스탯은 제외됩니다.");

            BuildOptionTypeMap();
            RecalculateSnapshot();
        }

        public CharacterStats GetSnapshot() => _snapshot;

        public void ApplyUpgrades(IReadOnlyDictionary<string, int> upgrades)
        {
            _upgradeLevels.Clear();
            _statLevels.Clear();

            if (upgrades != null)
            {
                foreach (var pair in upgrades)
                {
                    if (_upgradeTable != null && _upgradeTable.Index.TryGetValue(pair.Key, out var upgradeRow))
                    {
                        var clamped = ClampLevel(upgradeRow, pair.Value);
                        _upgradeLevels[pair.Key] = clamped;
                        _statLevels[upgradeRow.Stat] = clamped;
                    }
                }
            }

            RecalculateSnapshot();
        }

        public void ApplyEquipmentModifiers(IEnumerable<StatModifier> modifiers)
        {
            _equipmentModifiers.Clear();

            if (modifiers != null)
                _equipmentModifiers.AddRange(modifiers);

            RecalculateSnapshot();
        }

        public void ApplySkillModifiers(IEnumerable<StatModifier> modifiers)
        {
            _skillModifiers.Clear();

            if (modifiers != null)
                _skillModifiers.AddRange(modifiers);

            RecalculateSnapshot();
        }

        public void ApplyEquippedItems(IEnumerable<string> equipmentCodes, int level = 1)
        {
            if (_equipmentTable == null)
            {
                Debug.LogWarning("[StatService] EquipmentTable 미로딩 상태에서 장비 적용을 요청했습니다.");
                return;
            }

            if (equipmentCodes == null)
            {
                _equipmentModifiers.Clear();
                RecalculateSnapshot();
                return;
            }

            var modifiers = new List<StatModifier>();
            var safeLevel = Mathf.Max(1, level);

            foreach (var code in equipmentCodes)
            {
                if (!_equipmentTable.Index.TryGetValue(code, out var row))
                {
                    Debug.LogWarning($"[StatService] 알 수 없는 장비 코드: {code}");
                    continue;
                }

                TryAppendOption(row.EquipOption, safeLevel, modifiers);
                TryAppendOption(row.HeldOption1, safeLevel, modifiers);
                TryAppendOption(row.HeldOption2, safeLevel, modifiers);
                TryAppendOption(row.HeldOption3, safeLevel, modifiers);
            }

            ApplyEquipmentModifiers(modifiers);
        }

        public bool TryMapOptionType(string optionType, out StatType statType)
        {
            return _optionTypeMap.TryGetValue(optionType ?? string.Empty, out statType);
        }

        /// <summary>
        /// 특정 업그레이드 코드의 해당 레벨 스탯 값을 StatsTable에서 조회합니다.
        /// </summary>
        public BigDouble GetStatValue(string upgradeCode, int level)
        {
            if (_upgradeTable == null || !_upgradeTable.Index.TryGetValue(upgradeCode, out var upgradeRow))
                return BigDouble.Zero;

            var statsRow = GetStatsRow(Mathf.Max(0, level));
            return GetValueFromStatsRow(statsRow, upgradeRow.Stat);
        }

        // ──────────────────────────────
        // 내부 구현
        // ──────────────────────────────

        /// <summary>
        /// StatsRow에서 특정 StatType에 해당하는 값을 BigDouble로 반환합니다.
        /// </summary>
        private BigDouble GetValueFromStatsRow(StatsRow row, StatType statType)
        {
            switch (statType)
            {
                case StatType.ATK: return ToBigDouble(row.ATK_Base, row.ATK_Pow);
                case StatType.HP: return ToBigDouble(row.HP_Base, row.HP_Pow);
                case StatType.DEF: return ToBigDouble(row.DEF_Base, row.DEF_Pow);
                case StatType.HPREC: return ToBigDouble(row.HPREC_Base, row.HPREC_Pow);
                case StatType.CR: return ToBigDouble(row.CR_Base, row.CR_Pow);
                case StatType.ATKT: return ToBigDouble(row.ATKT_Base, row.ATKT_Pow);
                case StatType.OFFT: return row.OFFT;
                case StatType.GOLDR: return row.GOLDR;
                case StatType.ATKR: return row.ATKR;
                case StatType.OFFA: return row.OFFA;
                case StatType.ATKSP: return row.ATKSP;
                case StatType.RCD: return row.RCD;
                case StatType.UCR: return ToBigDouble(row.UCR_Base, row.UCR_Pow);
                case StatType.ATKB: return row.ATKB;
                case StatType.CD: return row.CD;
                case StatType.DEFR: return row.DEFR;
                case StatType.IGNDEF: return ToBigDouble(row.IGNDEF_Base, row.IGNDEF_Pow);
                default: return BigDouble.Zero;
            }
        }

        private void RecalculateSnapshot()
        {
            var stats = BuildBaseStatsFromLevels();
            ApplyModifiers(ref stats, _equipmentModifiers);
            ApplyModifiers(ref stats, _skillModifiers);

            _snapshot = stats;
        }

        /// <summary>
        /// StatsTable에서 해당 레벨의 행을 조회합니다.
        /// 정확히 일치하는 레벨이 없으면 마지막 행을 반환합니다.
        /// </summary>
        private StatsRow GetStatsRow(int level)
        {
            if (_statsTable == null)
                return default;

            if (_statsTable.Index.TryGetValue(level, out var row))
                return row;

            if (_statsTable.Rows.Count > 0)
                return _statsTable.Rows[^1];

            return default;
        }

        /// <summary>
        /// 각 StatType별 업그레이드 레벨에 해당하는 StatsRow를 조회하여 기본 스탯을 빌드합니다.
        /// </summary>
        private CharacterStats BuildBaseStatsFromLevels()
        {
            // 업그레이드 레벨이 지정되지 않은 스탯은 0레벨로 처리
            StatsRow Row(int level) => GetStatsRow(Mathf.Max(0, level));

            int atkLv = GetLevelOrDefault(StatType.ATK);
            int hpLv = GetLevelOrDefault(StatType.HP);
            int defLv = GetLevelOrDefault(StatType.DEF);
            int hprecLv = GetLevelOrDefault(StatType.HPREC);
            int crLv = GetLevelOrDefault(StatType.CR);
            int atktLv = GetLevelOrDefault(StatType.ATKT);
            int offtLv = GetLevelOrDefault(StatType.OFFT);
            int goldrLv = GetLevelOrDefault(StatType.GOLDR);
            int atkrLv = GetLevelOrDefault(StatType.ATKR);
            int offaLv = GetLevelOrDefault(StatType.OFFA);
            int atkspLv = GetLevelOrDefault(StatType.ATKSP);
            int rcdLv = GetLevelOrDefault(StatType.RCD);
            int ucrLv = GetLevelOrDefault(StatType.UCR);
            int atkbLv = GetLevelOrDefault(StatType.ATKB);
            int cdLv = GetLevelOrDefault(StatType.CD);
            int defrLv = GetLevelOrDefault(StatType.DEFR);
            int igndefLv = GetLevelOrDefault(StatType.IGNDEF);

            var rowATK = Row(atkLv);
            var rowHP = Row(hpLv);
            var rowDEF = Row(defLv);
            var rowHPREC = Row(hprecLv);
            var rowCR = Row(crLv);
            var rowATKT = Row(atktLv);
            var rowOFFT = Row(offtLv);
            var rowGOLDR = Row(goldrLv);
            var rowATKR = Row(atkrLv);
            var rowOFFA = Row(offaLv);
            var rowATKSP = Row(atkspLv);
            var rowRCD = Row(rcdLv);
            var rowUCR = Row(ucrLv);
            var rowATKB = Row(atkbLv);
            var rowCD = Row(cdLv);
            var rowDEFR = Row(defrLv);
            var rowIGNDEF = Row(igndefLv);
            var characterLevel = GetTotalStatLevel();

            // Evolution 레벨 기반 MaxTargetCount 계산
            // 기본값: 3, Evolution 레벨당 +1
            // TODO: Evolution 시스템 구현 시 evolutionLevel 변수로 교체
            int evolutionLevel = 0; // 추후 Evolution 서비스에서 가져올 예정
            int maxTargetCount = 3 + evolutionLevel;

            return new CharacterStats
            {
                Attack = ToBigDouble(rowATK.ATK_Base, rowATK.ATK_Pow),
                MaxHP = ToBigDouble(rowHP.HP_Base, rowHP.HP_Pow),
                Defense = ToBigDouble(rowDEF.DEF_Base, rowDEF.DEF_Pow),
                HealthRegen = ToBigDouble(rowHPREC.HPREC_Base, rowHPREC.HPREC_Pow),
                CharacterLevel = characterLevel,

                AttackSpeed = rowATKSP.ATKSP,
                CritChance = ToDouble(rowCR.CR_Base, rowCR.CR_Pow),
                CritMultiplier = 2.0 + rowCD.CD,
                TouchDamageMultiplier = ToDouble(rowATKT.ATKT_Base, rowATKT.ATKT_Pow),

                GoldBonusRate = rowGOLDR.GOLDR,
                AttackRate = rowATKR.ATKR,
                OfflineTimeMinutes = rowOFFT.OFFT,
                OfflineAmountRate = rowOFFA.OFFA,
                CooldownReduction = rowRCD.RCD,
                UltraCritChance = ToDouble(rowUCR.UCR_Base, rowUCR.UCR_Pow),
                AttackBonus = rowATKB.ATKB,
                CritDamageBonus = rowCD.CD,
                DefenseRate = rowDEFR.DEFR,
                DefenseIgnore = ToDouble(rowIGNDEF.IGNDEF_Base, rowIGNDEF.IGNDEF_Pow),
                BossDamageRate = 0,
                EliteDamageRate = 0,

                // 동시 공격 가능한 최대 타겟 수
                MaxTargetCount = maxTargetCount,
            };
        }

        private void ApplyModifiers(ref CharacterStats stats, List<StatModifier> modifiers)
        {
            if (modifiers == null || modifiers.Count == 0)
                return;

            foreach (var modifier in modifiers)
            {
                switch (modifier.Stat)
                {
                    case StatType.ATK:
                        ApplyModifier(ref stats.Attack, modifier);
                        break;
                    case StatType.HP:
                        ApplyModifier(ref stats.MaxHP, modifier);
                        break;
                    case StatType.DEF:
                        ApplyModifier(ref stats.Defense, modifier);
                        break;
                    case StatType.HPREC:
                        ApplyModifier(ref stats.HealthRegen, modifier);
                        break;
                    case StatType.CR:
                        stats.CritChance += modifier.Flat;
                        stats.CritChance += modifier.Rate;
                        break;
                    case StatType.ATKT:
                        stats.TouchDamageMultiplier += modifier.Flat + modifier.Rate;
                        break;
                    case StatType.OFFT:
                        stats.OfflineTimeMinutes += modifier.Flat + modifier.Rate;
                        break;
                    case StatType.GOLDR:
                        stats.GoldBonusRate += modifier.Flat + modifier.Rate;
                        break;
                    case StatType.ATKR:
                        stats.AttackRate += modifier.Flat + modifier.Rate;
                        break;
                    case StatType.OFFA:
                        stats.OfflineAmountRate += modifier.Flat + modifier.Rate;
                        break;
                    case StatType.ATKSP:
                        stats.AttackSpeed += modifier.Flat + modifier.Rate;
                        break;
                    case StatType.RCD:
                        stats.CooldownReduction += modifier.Flat + modifier.Rate;
                        break;
                    case StatType.UCR:
                        stats.UltraCritChance += modifier.Flat + modifier.Rate;
                        break;
                    case StatType.ATKB:
                        stats.AttackBonus += modifier.Flat + modifier.Rate;
                        break;
                    case StatType.CD:
                        stats.CritDamageBonus += modifier.Flat + modifier.Rate;
                        break;
                    case StatType.DEFR:
                        stats.DefenseRate += modifier.Flat + modifier.Rate;
                        break;
                    case StatType.IGNDEF:
                        stats.DefenseIgnore += modifier.Flat + modifier.Rate;
                        break;
                }
            }
        }

        private void ApplyModifier(ref BigDouble value, StatModifier modifier)
        {
            if (Math.Abs(modifier.Flat) > double.Epsilon)
                value += modifier.Flat;

            if (Math.Abs(modifier.Rate) > double.Epsilon)
                value *= 1 + modifier.Rate;
        }

        private int GetLevelOrDefault(StatType stat)
        {
            return _statLevels.TryGetValue(stat, out var lv) ? lv : 0;
        }

        private int ClampLevel(UpgradeRow row, int level)
        {
            return Mathf.Clamp(level, 0, Math.Max(0, row.MaxLevel));
        }

        private void BuildOptionTypeMap()
        {
            _optionTypeMap.Clear();

            _optionTypeMap["ATK"] = StatType.ATK;
            _optionTypeMap["HP"] = StatType.HP;
            _optionTypeMap["DEF"] = StatType.DEF;
            _optionTypeMap["HPREC"] = StatType.HPREC;
            _optionTypeMap["CR"] = StatType.CR;
            _optionTypeMap["ATKT"] = StatType.ATKT;
            _optionTypeMap["OFFT"] = StatType.OFFT;
            _optionTypeMap["GOLDR"] = StatType.GOLDR;
            _optionTypeMap["ATKR"] = StatType.ATKR;
            _optionTypeMap["OFFA"] = StatType.OFFA;
            _optionTypeMap["ATKSP"] = StatType.ATKSP;
            _optionTypeMap["RCD"] = StatType.RCD;
            _optionTypeMap["UCR"] = StatType.UCR;
            _optionTypeMap["ATKB"] = StatType.ATKB;
            _optionTypeMap["CD"] = StatType.CD;
            _optionTypeMap["DEFR"] = StatType.DEFR;
            _optionTypeMap["IGNDEF"] = StatType.IGNDEF;
        }

        private void TryAppendOption(OptionValue option, int level, ICollection<StatModifier> output)
        {
            if (!TryCreateModifier(option, level, out var modifier))
                return;

            output.Add(modifier);
        }

        private bool TryCreateModifier(OptionValue option, int level, out StatModifier modifier)
        {
            modifier = default;

            if (!TryMapOptionType(option.Type, out var stat))
                return false;

            var value = option.Base + option.Up * Math.Max(0, level - 1);
            if (Math.Abs(value) <= double.Epsilon)
                return false;

            if (IsRateStat(stat))
            {
                modifier = new StatModifier
                {
                    Stat = stat,
                    Rate = value
                };
            }
            else
            {
                modifier = new StatModifier
                {
                    Stat = stat,
                    Flat = value
                };
            }

            return true;
        }

        private bool IsRateStat(StatType stat)
        {
            return stat == StatType.CR
                   || stat == StatType.ATKT
                   || stat == StatType.GOLDR
                   || stat == StatType.ATKR
                   || stat == StatType.OFFA
                   || stat == StatType.ATKSP
                   || stat == StatType.RCD
                   || stat == StatType.UCR
                   || stat == StatType.DEFR
                   || stat == StatType.IGNDEF;
        }

        private BigDouble ToBigDouble(double mantissa, double pow)
        {
            // Pow는 10의 지수부, Base는 가수부
            var exponent = new BigDouble(Math.Pow(10d, pow));
            return new BigDouble(mantissa) * exponent;
        }

        private double ToDouble(double mantissa, double pow)
        {
            return mantissa * Math.Pow(10d, pow);
        }

        private int GetTotalStatLevel()
        {
            var total = 0;
            foreach (var lv in _statLevels.Values)
            {
                total += Mathf.Max(0, lv);
            }
            return total;
        }
    }
}
