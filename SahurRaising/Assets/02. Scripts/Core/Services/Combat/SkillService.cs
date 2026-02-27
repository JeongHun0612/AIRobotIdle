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
        private const string SaveKey = "skills";
        private const string SkillTableKey = nameof(SkillTable);

        private readonly IResourceService _resourceService;
        private readonly ICurrencyService _currencyService;
        private readonly IStatService _statService;
        private readonly IEventBus _eventBus;
        private readonly IDataService _dataService;

        private SkillTable _skillTable;
        private HashSet<string> _unlockedSkillIds = new();
        private HashSet<string> _newSkillIds = new();
        private List<ResearchInfo> _researchingSkills = new();

        public SkillService(
            IResourceService resourceService,
            ICurrencyService currencyService,
            IStatService statService,
            IEventBus eventBus,
            IDataService dataService)
        {
            _resourceService = resourceService;
            _currencyService = currencyService;
            _statService = statService;
            _eventBus = eventBus;
            _dataService = dataService;
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
            
            // 로드된 연구 중인 스킬들의 타이머 복원
            RestoreResearchTimers();
        }

        /// <summary>
        /// 앱 재시작 후 진행 중인 연구 타이머 복원
        /// </summary>
        private void RestoreResearchTimers()
        {
            long now = DateTime.Now.Ticks;
            var completedSkills = new List<string>();
            
            foreach (var research in _researchingSkills)
            {
                if (now >= research.EndTimeTicks)
                {
                    // 이미 완료된 연구 - 즉시 처리
                    completedSkills.Add(research.SkillID);
                }
                else
                {
                    // 아직 진행 중인 연구 - 남은 시간만큼 타이머 시작
                    double remainingSeconds = TimeSpan.FromTicks(research.EndTimeTicks - now).TotalSeconds;
                    StartResearchTimerAsync(research.SkillID, (int)Math.Ceiling(remainingSeconds)).Forget();
                    Debug.Log($"[SkillService] 연구 타이머 복원: {research.SkillID}, 남은 시간: {remainingSeconds:F1}초");
                }
            }
            
            // 완료된 연구들 처리
            foreach (var skillId in completedSkills)
            {
                CompleteResearch(skillId);
            }
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

            // 연구 중인 경우 불가
            if (_researchingSkills.Exists(r => r.SkillID == skillId))
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

            // 해금 처리 또는 연구 시작
            if (row.Time > 0)
            {
                // 연구 시작
                long endTime = DateTime.Now.AddSeconds(row.Time).Ticks;
                _researchingSkills.Add(new ResearchInfo { SkillID = skillId, EndTimeTicks = endTime });
                
                _eventBus.Publish(new SkillStateChangedEvent(skillId, SkillState.Researching));
                
                // 비동기 타이머 시작 (연구 완료 시 자동 처리)
                StartResearchTimerAsync(skillId, row.Time).Forget();
            }
            else
            {
                // 즉시 해금
                _unlockedSkillIds.Add(skillId);
                _newSkillIds.Add(skillId);
                // 효과 적용
                ApplyPassiveStats();
                
                _eventBus.Publish(new SkillStateChangedEvent(skillId, SkillState.Unlocked));
            }

            // 저장
            SaveAsync().Forget();

            return true;
        }

        /// <summary>
        /// 연구 완료 비동기 타이머
        /// Update 폴링 대신 UniTask 기반 비동기 대기 사용
        /// </summary>
        private async UniTaskVoid StartResearchTimerAsync(string skillId, int researchTimeSeconds)
        {
            try
            {
                // 연구 시간만큼 대기
                await UniTask.Delay(TimeSpan.FromSeconds(researchTimeSeconds), ignoreTimeScale: true);
                
                // 연구 완료 처리
                CompleteResearch(skillId);
            }
            catch (OperationCanceledException)
            {
                // 취소된 경우 (앱 종료 등) 무시
                Debug.Log($"[SkillService] 연구 타이머 취소됨: {skillId}");
            }
        }

        /// <summary>
        /// 연구 완료 처리 (단일 스킬)
        /// </summary>
        private void CompleteResearch(string skillId)
        {
            var research = _researchingSkills.Find(r => r.SkillID == skillId);
            if (string.IsNullOrEmpty(research.SkillID)) return;
            
            _researchingSkills.RemoveAll(r => r.SkillID == skillId);
            _unlockedSkillIds.Add(skillId);
            _newSkillIds.Add(skillId);
            
            ApplyPassiveStats();
            SaveAsync().Forget();
            
            _eventBus.Publish(new SkillStateChangedEvent(skillId, SkillState.Unlocked));
            Debug.Log($"[SkillService] 연구 완료: {skillId}");
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

        public SkillState GetSkillState(string skillId)
        {
            if (IsUnlocked(skillId)) return SkillState.Unlocked;
            if (_researchingSkills.Exists(r => r.SkillID == skillId)) return SkillState.Researching;
            if (CanUnlock(skillId)) return SkillState.Unlockable;
            return SkillState.Locked;
        }

        public double GetRemainingTime(string skillId)
        {
            var research = _researchingSkills.Find(r => r.SkillID == skillId);
            if (string.IsNullOrEmpty(research.SkillID)) return 0;

            long ticksRemaining = research.EndTimeTicks - DateTime.Now.Ticks;
            if (ticksRemaining <= 0) return 0;

            return TimeSpan.FromTicks(ticksRemaining).TotalSeconds;
        }

        public void CheckResearchCompletion()
        {
            bool changed = false;
            long now = DateTime.Now.Ticks;

            for (int i = _researchingSkills.Count - 1; i >= 0; i--)
            {
                if (now >= _researchingSkills[i].EndTimeTicks)
                {
                    var info = _researchingSkills[i];
                    _researchingSkills.RemoveAt(i);
                    _unlockedSkillIds.Add(info.SkillID);
                    _newSkillIds.Add(info.SkillID);
                    changed = true;

                    _eventBus.Publish(new SkillStateChangedEvent(info.SkillID, SkillState.Unlocked));
                }
            }

            if (changed)
            {
                ApplyPassiveStats();
                SaveAsync().Forget();
            }
        }

        public async UniTask SaveAsync()
        {
            try
            {
                var data = new SkillSaveData();
                data.UnlockedSkillIDs.AddRange(_unlockedSkillIds);
                data.ResearchingSkills.AddRange(_researchingSkills);
                data.NewSkillIDs.AddRange(_newSkillIds);

                var json = JsonUtility.ToJson(data);
                await _dataService.SaveAsync(SaveKey, json);
                Debug.Log($"[SkillService] 저장 완료: {SaveKey}");
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
                var json = await _dataService.LoadAsync(SaveKey);
                if (string.IsNullOrEmpty(json))
                {
                    Debug.Log("[SkillService] 저장 파일이 없어 기본값으로 초기화합니다.");
                    await SaveAsync();
                    return;
                }

                var data = JsonUtility.FromJson<SkillSaveData>(json);

                if (data != null)
                {
                    if (data.UnlockedSkillIDs != null)
                    {
                        foreach (var id in data.UnlockedSkillIDs)
                        {
                            _unlockedSkillIds.Add(id);
                        }
                    }

                    if (data.ResearchingSkills != null)
                    {
                        _researchingSkills = data.ResearchingSkills;
                    }

                    if (data.NewSkillIDs != null)
                    {
                        foreach (var id in data.NewSkillIDs)
                        {
                            _newSkillIds.Add(id);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SkillService] 로드 실패: {ex.Message}");
            }
        }

        public bool IsNewSkill(string skillId)
        {
            return _newSkillIds.Contains(skillId);
        }

        public void AcknowledgeSkill(string skillId)
        {
            if (_newSkillIds.Remove(skillId))
            {
                SaveAsync().Forget();
            }
        }

        #region 디버그 전용 메서드

        /// <summary>
        /// 특정 스킬 강제 해금 (디버그용, 비용/선행조건 무시)
        /// </summary>
        public void ForceUnlock(string skillId)
        {
            if (string.IsNullOrEmpty(skillId)) return;
            if (!_skillTable.Index.ContainsKey(skillId))
            {
                Debug.LogWarning($"[SkillService] 존재하지 않는 스킬 ID: {skillId}");
                return;
            }

            // 연구 중인 경우 먼저 제거
            _researchingSkills.RemoveAll(r => r.SkillID == skillId);
            
            if (_unlockedSkillIds.Add(skillId))
            {
                ApplyPassiveStats();
                _eventBus.Publish(new SkillStateChangedEvent(skillId, SkillState.Unlocked));
                SaveAsync().Forget();
                Debug.Log($"[SkillService] 스킬 강제 해금: {skillId}");
            }
        }

        /// <summary>
        /// 특정 스킬 해금 취소 (디버그용)
        /// </summary>
        public void ForceLock(string skillId)
        {
            if (string.IsNullOrEmpty(skillId)) return;

            bool changed = false;

            // 해금 상태 제거
            if (_unlockedSkillIds.Remove(skillId))
            {
                changed = true;
            }

            // 연구 중인 경우도 제거
            if (_researchingSkills.RemoveAll(r => r.SkillID == skillId) > 0)
            {
                changed = true;
            }

            // NEW 태그도 제거
            _newSkillIds.Remove(skillId);

            if (changed)
            {
                ApplyPassiveStats();
                _eventBus.Publish(new SkillStateChangedEvent(skillId, SkillState.Locked));
                SaveAsync().Forget();
                Debug.Log($"[SkillService] 스킬 잠금 처리: {skillId}");
            }
        }

        /// <summary>
        /// 모든 스킬 데이터 초기화 (디버그용)
        /// </summary>
        public void ResetAllSkills()
        {
            _unlockedSkillIds.Clear();
            _researchingSkills.Clear();
            _newSkillIds.Clear();

            // 스탯 초기화
            _statService.ApplySkillModifiers(new List<StatModifier>());

            // 각 스킬에 대해 상태 변경 이벤트 발행
            if (_skillTable != null)
            {
                foreach (var pair in _skillTable.Index)
                {
                    _eventBus.Publish(new SkillStateChangedEvent(pair.Key, 
                        pair.Value.IsFirstNode ? SkillState.Unlockable : SkillState.Locked));
                }
            }

            SaveAsync().Forget();
            Debug.Log("[SkillService] 모든 스킬 초기화 완료");
        }

        /// <summary>
        /// 현재 해금된 모든 스킬 ID 목록 조회 (디버그용)
        /// </summary>
        public IReadOnlyCollection<string> GetUnlockedSkillIds()
        {
            return _unlockedSkillIds;
        }

        #endregion
    }
}
