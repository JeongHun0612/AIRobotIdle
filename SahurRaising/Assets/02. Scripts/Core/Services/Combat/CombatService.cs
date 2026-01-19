using System;
using System.IO;
using BreakInfinity;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SahurRaising.Core
{
    /// <summary>
    /// 스테이지/웨이브 진행 및 전투 보상 발행을 담당하는 서비스
    /// </summary>
    public class CombatService : ICombatService
    {
        private const string MonsterTableKey = nameof(MonsterTable);
        private const string SaveFileName = "combat.json";
        private const int WavesPerStage = 4;
        private const double EliteHpMultiplier = 10d;
        private const double BossHpMultiplier = 20d;
        private const float EliteTimeLimit = 20f;
        private const float BossTimeLimit = 30f;

        private readonly IResourceService _resourceService;
        private readonly IEventBus _eventBus;
        private readonly IStatService _statService;
        private readonly ICurrencyService _currencyService;

        private MonsterTable _monsterTable;
        private MonsterRow _currentMonster;
        private MonsterKind _currentKind;
        private CharacterStats _stats;

        private BigDouble _currentHp;
        private BigDouble _currentDef;
        private float _waveTimer;
        private float _attackGauge;

        private int _stageIndex = 1;
        private int _waveIndex = 1;
        private bool _isStageRunning;

        public CombatService(
            IResourceService resourceService,
            IEventBus eventBus,
            IStatService statService,
            ICurrencyService currencyService)
        {
            _resourceService = resourceService;
            _eventBus = eventBus;
            _statService = statService;
            _currencyService = currencyService;
        }

        public async UniTask InitializeAsync()
        {
            _monsterTable = await _resourceService.LoadTableAsync<MonsterTable>(MonsterTableKey);
            if (_monsterTable == null)
            {
                Debug.LogError("[CombatService] MonsterTable 로드 실패. Addressables 설정을 확인하세요.");
            }

            _stats = _statService.GetSnapshot();
            await LoadAsync();
        }

        public async UniTask StartStageAsync(int stageIndex, int waveIndex = 1)
        {
            _stageIndex = Mathf.Max(1, stageIndex);
            _waveIndex = Mathf.Clamp(waveIndex, 1, WavesPerStage);
            _stats = _statService.GetSnapshot();
            _isStageRunning = true;
            await StartWaveAsync(_waveIndex);
        }

        public event Action OnPlayerAttack;

        public void Tick(float deltaTime)
        {
            if (!_isStageRunning)
                return;

            if (_waveTimer > 0)
            {
                _waveTimer -= deltaTime;
                if (_waveTimer <= 0)
                {
                    FailStage();
                    return;
                }
            }

            ProcessAutoAttack(deltaTime);
        }

        public void ApplyTouchAttack()
        {
            if (!_isStageRunning)
                return;

            var damage = CalculateDamage(isTouch: true);
            DealDamage(damage);
            OnPlayerAttack?.Invoke();
        }
        
        public CombatProgress GetProgress()
        {
            return new CombatProgress
            {
                CurrentStage = Math.Max(1, _stageIndex),
                CurrentWave = Mathf.Clamp(_waveIndex, 1, WavesPerStage),
                IsStageRunning = _isStageRunning
            };
        }

        public async UniTask SaveAsync()
        {
            try
            {
                var data = new CombatSaveData
                {
                    CurrentStage = Math.Max(1, _stageIndex),
                    CurrentWave = Mathf.Clamp(_waveIndex, 1, WavesPerStage),
                    IsStageRunning = _isStageRunning
                };

                var path = GetSavePath();
                var json = JsonUtility.ToJson(data);
                await File.WriteAllTextAsync(path, json);
                Debug.Log($"[CombatService] 진행도 저장 완료: stage {_stageIndex}, wave {_waveIndex}, path {path}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CombatService] 저장 실패: {ex.Message}");
            }
        }

        public async UniTask LoadAsync()
        {
            try
            {
                var path = GetSavePath();
                if (!File.Exists(path))
                {
                    Debug.Log("[CombatService] 저장 파일이 없어 기본 진행도로 초기화합니다.");
                    await SaveAsync();
                    return;
                }

                var json = await File.ReadAllTextAsync(path);
                var data = JsonUtility.FromJson<CombatSaveData>(json);
                if (data == null)
                    return;

                _stageIndex = Math.Max(1, data.CurrentStage);
                _waveIndex = Mathf.Clamp(data.CurrentWave, 1, WavesPerStage);
                _isStageRunning = false; // 로드 후에는 수동 재시작
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CombatService] 로드 실패: {ex.Message}");
                _stageIndex = 1;
                _waveIndex = 1;
                _isStageRunning = false;
            }
        }

        private async UniTask StartWaveAsync(int waveIndex)
        {
            _waveIndex = Mathf.Clamp(waveIndex, 1, WavesPerStage);
            _stats = _statService.GetSnapshot();
            _attackGauge = 0f;

            var monsterLevel = GetMonsterLevel(_stageIndex, waveIndex);
            _currentMonster = GetMonsterRow(monsterLevel);
            _currentKind = DetermineMonsterKind(_stageIndex, waveIndex);

            _currentHp = CalculateMonsterHp(_currentMonster.MonsterHP, _currentKind);
            _currentDef = _currentMonster.MonsterDEF;
            _waveTimer = GetWaveTimer(_currentKind);

            await UniTask.Yield();
        }

        private MonsterRow GetMonsterRow(int monsterLevel)
        {
            if (_monsterTable == null || _monsterTable.Rows.Count == 0)
                return default;

            if (_monsterTable.Index.TryGetValue(monsterLevel, out var row))
                return row;

            return _monsterTable.Rows[^1];
        }

        private MonsterKind DetermineMonsterKind(int stageIndex, int waveIndex)
        {
            if (waveIndex < WavesPerStage)
                return MonsterKind.Normal;

            return stageIndex % 10 == 0 ? MonsterKind.Boss : MonsterKind.Elite;
        }

        private int GetMonsterLevel(int stageIndex, int waveIndex)
        {
            var waveForLevel = Mathf.Clamp(waveIndex, 1, 3);
            return (stageIndex - 1) * 3 + waveForLevel;
        }

        private BigDouble CalculateMonsterHp(BigDouble baseHp, MonsterKind kind)
        {
            return kind switch
            {
                MonsterKind.Elite => baseHp * EliteHpMultiplier,
                MonsterKind.Boss => baseHp * BossHpMultiplier,
                _ => baseHp
            };
        }

        private float GetWaveTimer(MonsterKind kind)
        {
            return kind switch
            {
                MonsterKind.Elite => EliteTimeLimit,
                MonsterKind.Boss => BossTimeLimit,
                _ => 0f
            };
        }

        private void ProcessAutoAttack(float deltaTime)
        {
            var attacksPerSecond = Math.Max(0.01f, (float)_stats.AttackSpeed);
            _attackGauge += deltaTime * attacksPerSecond;

            while (_attackGauge >= 1f)
            {
                var damage = CalculateDamage(isTouch: false);
                DealDamage(damage);
                _attackGauge -= 1f;

                if (!_isStageRunning)
                    break;
            }
        }

        private BigDouble CalculateDamage(bool isTouch)
        {
            var baseDamage = _stats.Attack * (1 + _stats.AttackRate) + _stats.AttackBonus;
            if (isTouch)
                baseDamage *= 1 + _stats.TouchDamageMultiplier;

            var defenseIgnore = Mathf.Clamp01((float)_stats.DefenseIgnore);
            var effectiveDef = _currentDef * (1 - defenseIgnore);
            var damage = BigDouble.Max(BigDouble.One, baseDamage - effectiveDef);

            var critChance = Mathf.Clamp01((float)(_stats.CritChance + _stats.UltraCritChance));
            if (UnityEngine.Random.value < critChance)
            {
                damage *= _stats.CritMultiplier + _stats.CritDamageBonus;
            }

            if (_currentKind == MonsterKind.Boss && _stats.BossDamageRate > 0)
                damage *= 1 + _stats.BossDamageRate;
            else if (_currentKind == MonsterKind.Elite && _stats.EliteDamageRate > 0)
                damage *= 1 + _stats.EliteDamageRate;

            return damage;
        }

        private void DealDamage(BigDouble damage)
        {
            if (_currentHp <= 0)
                return;

            _currentHp -= damage;
            if (_currentHp > 0)
                return;

            OnMonsterDefeated();
        }

        private async void OnMonsterDefeated()
        {
            GrantReward();

            _eventBus?.Publish(new EnemyDefeatedEvent
            {
                StageIndex = _stageIndex,
                WaveIndex = _waveIndex,
                MonsterKind = _currentKind,
                Reward = new CombatReward
                {
                    Gold = _currentMonster.Gold * (1 + _stats.GoldBonusRate),
                    Emerald = BigDouble.Zero,
                    Diamond = BigDouble.Zero,
                    Ticket = 0
                }
            });

            if (_waveIndex >= WavesPerStage)
            {
                _isStageRunning = false;
                _eventBus?.Publish(new StageResultEvent
                {
                    StageIndex = _stageIndex,
                    IsClear = true
                });
                return;
            }

            await StartWaveAsync(_waveIndex + 1);
        }

        private void GrantReward()
        {
            var goldReward = _currentMonster.Gold * (1 + _stats.GoldBonusRate);
            _currencyService.Add(CurrencyType.Gold, goldReward, "Combat");
        }

        private void FailStage()
        {
            _isStageRunning = false;
            _eventBus?.Publish(new StageResultEvent
            {
                StageIndex = _stageIndex,
                IsClear = false
            });
        }

        private string GetSavePath()
        {
            return Path.Combine(Application.persistentDataPath, SaveFileName);
        }
    }
}


