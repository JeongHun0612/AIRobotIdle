using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SahurRaising.Core
{
    public class SkillService : ISkillService
    {
        private const string SaveFileName = "skills.json";
        private const string SkillTableKey = nameof(SkillTable);

        private readonly IResourceService _resourceService;
        private readonly ICurrencyService _currencyService;
        private readonly IStatService _statService;

        private SkillTable _skillTable;
        private HashSet<string> _unlockedSkillIds = new();

        public SkillService(
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
            _skillTable = await _resourceService.LoadTableAsync<SkillTable>(SkillTableKey);
            if (_skillTable == null)
            {
                Debug.LogError("[SkillService] SkillTable 로드 실패. Addressables 키를 확인하세요.");
                return;
            }

            await LoadAsync();

            // 초기화 시 효과 적용
            ApplyPassiveStats();
        }

        public SkillTable GetTable() => _skillTable;

        public bool IsUnlocked(string skillId)
        {
            return _unlockedSkillIds.Contains(skillId);
        }

        public bool CanUnlock(string skillId)
        {
            // 1. 이미 해금된 경우 불가
            if (IsUnlocked(skillId))
                return false;

            if (!_skillTable.Index.TryGetValue(skillId, out var row))
            {
                Debug.LogWarning($"[SkillService] 존재하지 않는 스킬 ID: {skillId}");
                return false;
            }

            // 2. 비용 체크
            if (_currencyService.Get(CurrencyType.Emerald) < row.Cost)
                return false;

            // 3. 선행 조건 체크 (인접 노드 해금 여부)
            // 시작 노드인 경우 무조건 가능
            if (row.IsFirstNode)
                return true;

            // 인접 노드(상하좌우) 중 하나라도 해금되어 있어야 함
            return HasUnlockedNeighbor(row.XCoord, row.YCoord);
        }

        public bool TryUnlock(string skillId)
        {
            if (!CanUnlock(skillId))
                return false;

            if (!_skillTable.Index.TryGetValue(skillId, out var row))
                return false;

            // 비용 소모
            if (!_currencyService.TryConsume(CurrencyType.Emerald, row.Cost, $"UnlockSkill_{skillId}"))
                return false;

            // 해금 처리
            _unlockedSkillIds.Add(skillId);

            // 효과 적용
            ApplyPassiveStats();

            // 저장
            SaveAsync().Forget();

            return true;
        }

        private bool HasUnlockedNeighbor(int targetX, int targetY)
        {
            foreach (var unlockedId in _unlockedSkillIds)
            {
                if (_skillTable.Index.TryGetValue(unlockedId, out var unlockedRow))
                {
                    int dx = Mathf.Abs(unlockedRow.XCoord - targetX);
                    int dy = Mathf.Abs(unlockedRow.YCoord - targetY);

                    // 맨해튼 거리 1 (상하좌우)
                    if (dx + dy == 1)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 해금된 스킬들의 효과를 적용
        /// </summary>
        private void ApplyPassiveStats()
        {
            if (_skillTable == null) return;

            var modifiers = new List<StatModifier>();

            foreach (var skillId in _unlockedSkillIds)
            {
                if (!_skillTable.Index.TryGetValue(skillId, out var row))
                    continue;

                // Stat 타입인 경우 StatService에 적용
                if (row.EffectType == SkillEffectType.Stat)
                {
                    // SkillTable의 Value는 이미 0.1, 0.05 등으로 변환되어 있음.
                    // 따라서 대부분 Rate로 적용하면 됨.
                    modifiers.Add(new StatModifier
                    {
                        Stat = row.TargetStat,
                        Rate = row.Value,
                        Flat = 0
                    });
                }
            }

            _statService.ApplySkillModifiers(modifiers);
        }

        public double GetSpecialValue(SkillSpecialType type)
        {
            double total = 0;
            if (_skillTable == null) return total;

            foreach (var skillId in _unlockedSkillIds)
            {
                if (!_skillTable.Index.TryGetValue(skillId, out var row))
                    continue;

                if (row.EffectType == SkillEffectType.Special &&
                    row.TargetSpecial == type)
                {
                    total += row.Value;
                }
            }
            return total;
        }

        public async UniTask SaveAsync()
        {
            try
            {
                var data = new SkillSaveData();
                data.UnlockedSkillIDs.AddRange(_unlockedSkillIds);

                var path = GetSavePath();
                var json = JsonUtility.ToJson(data);
                await File.WriteAllTextAsync(path, json);
                Debug.Log($"[SkillService] 저장 완료: {path}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SkillService] 저장 실패: {ex.Message}");
            }
        }

        public async UniTask LoadAsync()
        {
            _unlockedSkillIds.Clear();

            try
            {
                var path = GetSavePath();
                if (!File.Exists(path))
                {
                    return;
                }

                var json = await File.ReadAllTextAsync(path);
                var data = JsonUtility.FromJson<SkillSaveData>(json);

                if (data != null && data.UnlockedSkillIDs != null)
                {
                    foreach (var id in data.UnlockedSkillIDs)
                    {
                        _unlockedSkillIds.Add(id);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SkillService] 로드 실패: {ex.Message}");
            }
        }

        private string GetSavePath()
        {
            return Path.Combine(Application.persistentDataPath, SaveFileName);
        }
    }
}
