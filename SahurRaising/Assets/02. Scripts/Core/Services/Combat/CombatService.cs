using System;
using System.IO;
using BreakInfinity;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SahurRaising.Core
{
    /// <summary>
    /// 스테이지/웨이브 진행 및 전투 로직을 담당하는 서비스
    /// 다수 몬스터 동시 전투를 지원합니다.
    /// 
    /// 주요 책임:
    /// - 스테이지/웨이브 진행 관리
    /// - 공격 게이지 및 자동 공격 처리
    /// - 데미지 계산 및 이벤트 발행
    /// - 몬스터 스탯 정보 제공 (실제 HP 관리는 MonsterUnitView에서)
    /// </summary>
    public class CombatService : ICombatService
    {
        private const string MonsterTableKey = nameof(MonsterTable);
        private const string SaveFileName = "combat.json";
        private const double EliteHpMultiplier = 10d;
        private const double BossHpMultiplier = 20d;
        private const float EliteTimeLimit = 20f;
        private const float BossTimeLimit = 30f;
        
        // 외부에서 설정 가능한 웨이브 수 (기본값 4)
        private int _wavesPerStage = 4;
        
        /// <summary>
        /// 기본 공격 속도 (초당 공격 횟수)
        /// 2초당 1회 = 0.5 공격/초
        /// </summary>
        private const float BASE_ATTACK_SPEED = 0.5f;

        private readonly IResourceService _resourceService;
        private readonly IEventBus _eventBus;
        private readonly IStatService _statService;
        private readonly ICurrencyService _currencyService;

        private MonsterTable _monsterTable;
        private CharacterStats _stats;

        // 플레이어 상태
        private BigDouble _playerCurrentHp;
        private float _attackGauge;
        
        // 스테이지/웨이브 상태
        private int _stageIndex = 1;
        private int _waveIndex = 1;
        private int _monstersKilledThisWave = 0;
        private bool _isStageRunning;
        private float _waveTimer;

        // 이벤트
        public event Action<AttackEvent> OnAttack;

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

        #region Initialization

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

        #endregion

        #region Stage/Wave Management

        public async UniTask StartStageAsync(int stageIndex, int waveIndex = 1)
        {
            _stageIndex = Mathf.Max(1, stageIndex);
            _waveIndex = Mathf.Clamp(waveIndex, 1, _wavesPerStage);
            _monstersKilledThisWave = 0;
            
            RefreshStats();
            _playerCurrentHp = _stats.MaxHP;
            _isStageRunning = true;
            
            await UniTask.Yield();
            
            Debug.Log($"[CombatService] 스테이지 {_stageIndex}, 웨이브 {_waveIndex} 시작");
        }

        /// <summary>
        /// 몬스터 처치 시 호출 (MonsterUnitView에서 사망 확인 후)
        /// </summary>
        public void OnMonsterKilled(MonsterKind kind, BigDouble goldReward)
        {
            if (!_isStageRunning) return;
            
            _monstersKilledThisWave++;
            
            // 보상 지급
            var goldBonus = goldReward * (1 + _stats.GoldBonusRate);
            _currencyService.Add(CurrencyType.Gold, goldBonus, "Combat");
            
            // 이벤트 발행
            _eventBus?.Publish(new EnemyDefeatedEvent
            {
                StageIndex = _stageIndex,
                WaveIndex = _waveIndex,
                MonsterKind = kind,
                Reward = new CombatReward
                {
                    Gold = goldBonus,
                    Emerald = BigDouble.Zero,
                    Diamond = BigDouble.Zero,
                    Ticket = 0
                }
            });
            
            Debug.Log($"[CombatService] 몬스터 처치! 이번 웨이브: {_monstersKilledThisWave}마리");
        }

        /// <summary>
        /// 웨이브 완료 체크 (CombatRunner에서 호출)
        /// </summary>
        public void CheckWaveComplete(int requiredKills)
        {
            if (_monstersKilledThisWave >= requiredKills)
            {
                AdvanceWave();
            }
        }

        private void AdvanceWave()
        {
            Debug.Log($"[CombatService] 웨이브 {_waveIndex} 클리어!");
            
            if (_waveIndex >= _wavesPerStage)
            {
                // 스테이지 클리어
                _eventBus?.Publish(new StageResultEvent
                {
                    StageIndex = _stageIndex,
                    IsClear = true
                });
                
                // 다음 스테이지 준비
                _stageIndex++;
                _waveIndex = 1;
                _monstersKilledThisWave = 0;
                
                Debug.Log($"[CombatService] 스테이지 클리어! 다음 스테이지: {_stageIndex}");
            }
            else
            {
                // 다음 웨이브
                _waveIndex++;
                _monstersKilledThisWave = 0;
                
                Debug.Log($"[CombatService] 다음 웨이브: {_waveIndex}");
            }
        }

        #endregion

        #region Monster Stats Provider

        /// <summary>
        /// 현재 스테이지/웨이브에 맞는 몬스터 스탯 정보 반환
        /// </summary>
        public MonsterSpawnInfo GetMonsterSpawnInfo()
        {
            var level = GetMonsterLevel(_stageIndex, _waveIndex);
            var row = GetMonsterRow(level);
            var kind = DetermineMonsterKind(_stageIndex, _waveIndex);
            
            return new MonsterSpawnInfo
            {
                Level = level,
                Kind = kind,
                MaxHp = CalculateMonsterHp(row.MonsterHP, kind),
                Defense = row.MonsterDEF,
                Attack = row.MonsterATK,
                GoldReward = row.Gold,
                TimeLimit = GetWaveTimer(kind)
            };
        }

        /// <summary>
        /// 동시 공격 가능한 최대 타겟 수
        /// </summary>
        public int GetMaxTargetCount()
        {
            RefreshStats();
            return Mathf.Max(1, _stats.MaxTargetCount);
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
            if (waveIndex < _wavesPerStage)
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

        #endregion

        #region Combat Tick

        public void Tick(float deltaTime)
        {
            if (!_isStageRunning) return;

            // 웨이브 타이머 (엘리트/보스 전용)
            if (_waveTimer > 0)
            {
                _waveTimer -= deltaTime;
                if (_waveTimer <= 0)
                {
                    FailStage();
                    return;
                }
            }

            // HP 회복
            if (_playerCurrentHp < _stats.MaxHP && _stats.HealthRegen > 0)
            {
                _playerCurrentHp += _stats.HealthRegen * deltaTime;
                if (_playerCurrentHp > _stats.MaxHP)
                    _playerCurrentHp = _stats.MaxHP;
            }

            // 자동 공격 처리
            ProcessAutoAttack(deltaTime);
        }

        private void ProcessAutoAttack(float deltaTime)
        {
            var attackSpeedMultiplier = 1.0 + _stats.AttackSpeed;
            var attacksPerSecond = Math.Max(0.01f, (float)(BASE_ATTACK_SPEED * attackSpeedMultiplier));
            _attackGauge += deltaTime * attacksPerSecond;

            while (_attackGauge >= 1f)
            {
                // 자동 공격 이벤트 발행 (실제 데미지는 CombatRunner가 처리)
                OnAttack?.Invoke(new AttackEvent
                {
                    IsPlayerAttack = true,
                    Damage = CalculateDamage(isTouch: false, out bool isCritical),
                    IsCritical = isCritical,
                    AttackType = AttackType.Auto,
                    TargetIndex = 0,
                    HitIndex = 0,
                    IsLastHit = true
                });
                
                _attackGauge -= 1f;

                if (!_isStageRunning)
                    break;
            }
        }

        #endregion

        #region Damage Calculation

        public void ApplyTouchAttack()
        {
            if (!_isStageRunning) return;

            var damage = CalculateDamage(isTouch: true, out bool isCritical);
            
            OnAttack?.Invoke(new AttackEvent
            {
                IsPlayerAttack = true,
                Damage = damage,
                IsCritical = isCritical,
                AttackType = AttackType.Touch,
                TargetIndex = 0,
                HitIndex = 0,
                IsLastHit = true
            });
        }

        /// <summary>
        /// 데미지 계산 (외부에서도 사용 가능)
        /// </summary>
        public BigDouble CalculateDamage(bool isTouch, out bool isCritical)
        {
            RefreshStats();
            
            var baseDamage = _stats.Attack * (1 + _stats.AttackRate) + _stats.AttackBonus;
            if (isTouch)
                baseDamage *= 1 + _stats.TouchDamageMultiplier;

            var critChance = Mathf.Clamp01((float)(_stats.CritChance + _stats.UltraCritChance));
            isCritical = UnityEngine.Random.value < critChance;
            
            if (isCritical)
            {
                baseDamage *= _stats.CritMultiplier + _stats.CritDamageBonus;
            }

            return baseDamage;
        }

        /// <summary>
        /// 방어 무시율 반환
        /// </summary>
        public double GetDefenseIgnoreRate()
        {
            RefreshStats();
            return Mathf.Clamp01((float)_stats.DefenseIgnore);
        }

        /// <summary>
        /// 플레이어가 데미지를 받음
        /// </summary>
        public void DealDamageToPlayer(BigDouble damage)
        {
            if (_playerCurrentHp <= 0) return;

            var effectiveDamage = BigDouble.Max(1, damage - _stats.Defense);
            _playerCurrentHp -= effectiveDamage;

            if (_playerCurrentHp <= 0)
            {
                _playerCurrentHp = 0;
                FailStage();
            }
        }

        #endregion

        #region Stage Result

        private void FailStage()
        {
            _isStageRunning = false;
            _eventBus?.Publish(new StageResultEvent
            {
                StageIndex = _stageIndex,
                IsClear = false
            });
            
            Debug.Log($"[CombatService] 스테이지 {_stageIndex} 실패!");
        }

        #endregion

        #region Progress & Save/Load

        public CombatProgress GetProgress()
        {
            return new CombatProgress
            {
                CurrentStage = Math.Max(1, _stageIndex),
                CurrentWave = Mathf.Clamp(_waveIndex, 1, _wavesPerStage),
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
                    CurrentWave = Mathf.Clamp(_waveIndex, 1, _wavesPerStage),
                    IsStageRunning = _isStageRunning
                };

                var path = GetSavePath();
                var json = JsonUtility.ToJson(data);
                await File.WriteAllTextAsync(path, json);
                Debug.Log($"[CombatService] 진행도 저장 완료: stage {_stageIndex}, wave {_waveIndex}");
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
                _waveIndex = Mathf.Clamp(data.CurrentWave, 1, _wavesPerStage);
                _isStageRunning = false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CombatService] 로드 실패: {ex.Message}");
                _stageIndex = 1;
                _waveIndex = 1;
                _isStageRunning = false;
            }
        }

        private string GetSavePath()
        {
            return Path.Combine(Application.persistentDataPath, SaveFileName);
        }

        private void RefreshStats()
        {
            _stats = _statService.GetSnapshot();
        }

        #endregion
        
        #region Wave Configuration
        
        /// <summary>
        /// 스테이지당 웨이브 수 설정 (CombatSettings에서 주입)
        /// </summary>
        public void SetWavesPerStage(int count)
        {
            _wavesPerStage = Mathf.Max(1, count);
            Debug.Log($"[CombatService] 스테이지당 웨이브 수 설정: {_wavesPerStage}");
        }
        
        /// <summary>
        /// 현재 웨이브 인덱스 반환
        /// </summary>
        public int GetCurrentWaveIndex() => _waveIndex;
        
        #endregion
    }
}
