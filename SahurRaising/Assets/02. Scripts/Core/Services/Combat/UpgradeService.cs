using System;
using System.Collections.Generic;
using System.IO;
using BreakInfinity;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SahurRaising.Core
{
    /// <summary>
    /// 업그레이드 비용 계산, 구매, 저장/로드를 담당
    /// </summary>
    public class UpgradeService : IUpgradeService
    {
        private const string SaveFileName = "upgrades.json";
        private const string UpgradeTableKey = nameof(UpgradeTable);

        private readonly IResourceService _resourceService;
        private readonly ICurrencyService _currencyService;
        private readonly IStatService _statService;

        private readonly Dictionary<string, int> _levels = new();
        private UpgradeTable _upgradeTable;

        public UpgradeService(
            IResourceService resourceService,
            ICurrencyService currencyService,
            IStatService statService)
        {
            _resourceService = resourceService;
            _currencyService = currencyService;
            _statService = statService;
        }

        public async UniTask InitializeAsync()
        {
            _upgradeTable = await _resourceService.LoadTableAsync<UpgradeTable>(UpgradeTableKey);
            if (_upgradeTable == null)
            {
                Debug.LogError("[UpgradeService] UpgradeTable 로드 실패. Addressables 키를 확인하세요.");
                return;
            }

            await LoadAsync();
            _statService.ApplyUpgrades(_levels);
        }

        public int GetLevel(string code)
        {
            return _levels.TryGetValue(code, out var level) ? level : 0;
        }

        public IReadOnlyDictionary<string, int> GetAllLevels() => _levels;

        public BigDouble GetNextCost(string code)
        {
            if (!TryGetRow(code, out var row))
                return BigDouble.Zero;

            var current = GetLevel(code);
            if (current >= row.MaxLevel)
                return BigDouble.Zero;

            var nextLevel = current + 1;
            return CalculateCost(row, nextLevel);
        }

        public bool TryUpgrade(string code, int levels, out int appliedLevels, out BigDouble totalCost)
        {
            appliedLevels = 0;
            totalCost = BigDouble.Zero;

            if (levels <= 0)
                return false;

            if (!TryGetRow(code, out var row))
                return false;

            var currentLevel = GetLevel(code);
            var targetLevel = Mathf.Min(row.MaxLevel, currentLevel + levels);

            for (var nextLevel = currentLevel + 1; nextLevel <= targetLevel; nextLevel++)
            {
                var cost = CalculateCost(row, nextLevel);
                if (!_currencyService.TryConsume(CurrencyType.Gold, cost, "Upgrade"))
                    break;

                currentLevel = nextLevel;
                appliedLevels++;
                totalCost += cost;
            }

            if (appliedLevels == 0)
                return false;

            _levels[code] = currentLevel;
            _statService.ApplyUpgrades(_levels);

            return true;
        }

        public async UniTask SaveAsync()
        {
            try
            {
                var data = new UpgradeSaveData();
                foreach (var pair in _levels)
                {
                    data.Levels.Add(new UpgradeLevelEntry
                    {
                        Code = pair.Key,
                        Level = pair.Value
                    });
                }

                var path = GetSavePath();
                var json = JsonUtility.ToJson(data);
                await File.WriteAllTextAsync(path, json);
                Debug.Log($"[UpgradeService] 저장 완료: {path}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UpgradeService] 저장 실패: {ex.Message}");
            }
        }

        public async UniTask LoadAsync()
        {
            _levels.Clear();

            try
            {
                var path = GetSavePath();
                if (!File.Exists(path))
                {
                    Debug.Log("[UpgradeService] 저장 파일이 없어 기본값으로 초기화합니다.");
                    await SaveAsync();
                    return;
                }

                var json = await File.ReadAllTextAsync(path);
                var data = JsonUtility.FromJson<UpgradeSaveData>(json);
                if (data?.Levels == null)
                    return;

                foreach (var entry in data.Levels)
                {
                    if (!TryGetRow(entry.Code, out var row))
                        continue;

                    var clamped = Mathf.Clamp(entry.Level, 0, Math.Max(0, row.MaxLevel));
                    _levels[entry.Code] = clamped;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UpgradeService] 로드 실패: {ex.Message}");
            }
        }

        private bool TryGetRow(string code, out UpgradeRow row)
        {
            row = default;

            if (_upgradeTable == null || _upgradeTable.Index == null)
                return false;

            return _upgradeTable.Index.TryGetValue(code, out row);
        }

        private BigDouble CalculateCost(UpgradeRow row, int nextLevel)
        {
            if (nextLevel <= 0 || row.MaxLevel <= 0)
                return BigDouble.Zero;

            var clampedLevel = Mathf.Clamp(nextLevel, 1, row.MaxLevel);
            var breakdown = CalculateBreakdown(clampedLevel, row);

            var cost = new BigDouble(row.GoldBase);
            cost *= BigDouble.Pow(row.GoldPow, breakdown.PowLevels);
            cost *= BigDouble.Pow(breakdown.Segment1Growth, breakdown.Segment1Levels);
            cost *= BigDouble.Pow(breakdown.Segment2Growth, breakdown.Segment2Levels);
            cost *= BigDouble.Pow(breakdown.Segment3Growth, breakdown.Segment3Levels);
            cost *= BigDouble.Pow(breakdown.Segment4Growth, breakdown.Segment4Levels);

            return cost;
        }

        private UpgradeCostBreakdown CalculateBreakdown(int level, UpgradeRow row)
        {
            var tier0 = Math.Min(level, row.Segment1.MaxLevel);
            var tier1 = Mathf.Clamp(level - row.Segment1.MaxLevel, 0, row.Segment2.MaxLevel - row.Segment1.MaxLevel);
            var tier2 = Mathf.Clamp(level - row.Segment2.MaxLevel, 0, row.Segment3.MaxLevel - row.Segment2.MaxLevel);
            var tier3 = Mathf.Clamp(level - row.Segment3.MaxLevel, 0, row.Segment4.MaxLevel - row.Segment3.MaxLevel);
            var tier4 = Math.Max(0, level - row.Segment4.MaxLevel);

            return new UpgradeCostBreakdown(
                tier0,
                tier1,
                tier2,
                tier3,
                tier4,
                row.Segment1.Growth,
                row.Segment2.Growth,
                row.Segment3.Growth,
                row.Segment4.Growth);
        }

        private string GetSavePath()
        {
            return Path.Combine(Application.persistentDataPath, SaveFileName);
        }

        private readonly struct UpgradeCostBreakdown
        {
            public readonly int PowLevels;
            public readonly int Segment1Levels;
            public readonly int Segment2Levels;
            public readonly int Segment3Levels;
            public readonly int Segment4Levels;

            public readonly double Segment1Growth;
            public readonly double Segment2Growth;
            public readonly double Segment3Growth;
            public readonly double Segment4Growth;

            public UpgradeCostBreakdown(
                int powLevels,
                int segment1Levels,
                int segment2Levels,
                int segment3Levels,
                int segment4Levels,
                double segment1Growth,
                double segment2Growth,
                double segment3Growth,
                double segment4Growth)
            {
                PowLevels = powLevels;
                Segment1Levels = segment1Levels;
                Segment2Levels = segment2Levels;
                Segment3Levels = segment3Levels;
                Segment4Levels = segment4Levels;
                Segment1Growth = segment1Growth;
                Segment2Growth = segment2Growth;
                Segment3Growth = segment3Growth;
                Segment4Growth = segment4Growth;
            }
        }
    }
}

