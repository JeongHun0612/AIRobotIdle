using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using SahurRaising.Core;
using UnityEngine;

namespace SahurRaising.GamePlay
{
    /// <summary>
    /// 몬스터 스폰 및 오브젝트 풀링을 담당하는 컴포넌트
    /// 
    /// 주요 기능:
    /// - Wave 포메이션 패턴 기반 스폰
    /// - 오브젝트 풀링을 통한 몬스터 재사용
    /// - 전투 범위 내 몬스터와 대기 중 몬스터 구분
    /// - Y축 수렴 시스템 (전투 돌입 전 CombatY 범위로 이동)
    /// </summary>
    public class MonsterSpawner : MonoBehaviour
    {
        [Header("풀 설정 (씬 전용)")]
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private int _poolInitialSize = 5;
        [SerializeField] private int _poolMaxSize = 15;

        private CombatSettings _settings;
        private ICombatService _combatService;

        // 활성 몬스터 리스트
        private readonly List<MonsterUnitView> _activeMonsters = new();

        // 전투 중인 몬스터 (공격 사거리 내)
        private readonly List<MonsterUnitView> _engagedMonsters = new();

        // 오브젝트 풀
        private readonly Dictionary<MonsterUnitView, MonoObjectPool<MonsterUnitView>> _monsterPools = new();

        // 스폰 상태
        private int _patternsCompletedThisWave = 0;  // 이번 웨이브에서 완료된 패턴 수
        private int _monstersSpawnedThisWave = 0;    // 이번 웨이브에서 스폰된 몬스터 수 (통계용)
        private bool _isSpawningEnabled = false;
        private Transform _playerTransform;
        private int _currentWaveIndex = 1;

        // 패턴 스폰 상태
        private SpawnPattern _currentPattern;
        private float _patternCooldownTimer;
        private Queue<PatternSpawnRequest> _pendingSpawns = new();

        // 읽기 전용 프로퍼티
        public IReadOnlyList<MonsterUnitView> ActiveMonsters => _activeMonsters;
        public IReadOnlyList<MonsterUnitView> EngagedMonsters => _engagedMonsters;
        public int PatternsCompletedThisWave => _patternsCompletedThisWave;
        public int MonstersSpawnedThisWave => _monstersSpawnedThisWave;
        public int ActiveMonsterCount => _activeMonsters.Count;
        public int EngagedMonsterCount => _engagedMonsters.Count;

        // 이벤트
        public event Action<MonsterUnitView> OnMonsterSpawned;
        public event Action<MonsterUnitView> OnMonsterReleased;
        public event Action<MonsterUnitView> OnMonsterEngaged;
        public event Action<MonsterUnitView> OnMonsterKilled;

        /// <summary>
        /// 스포너 초기화
        /// </summary>
        public void Initialize(CombatSettings settings, Transform spawnPoint, Transform playerTransform, ICombatService combatService)
        {
            _settings = settings;
            _combatService = combatService;
            _playerTransform = playerTransform;

            if (spawnPoint != null)
                _spawnPoint = spawnPoint;

            InitializePools();
        }

        /// <summary>
        /// 몬스터 풀 초기화
        /// </summary>
        private void InitializePools()
        {
            if (_settings?.MonsterVisuals == null) return;

            foreach (var entry in _settings.MonsterVisuals)
            {
                if (entry.Prefab == null) continue;
                if (_monsterPools.ContainsKey(entry.Prefab)) continue;

                var pool = new MonoObjectPool<MonsterUnitView>(
                    prefab: entry.Prefab,
                    parent: _spawnPoint,
                    initialSize: _poolInitialSize,
                    maxSize: _poolMaxSize,
                    onGet: OnMonsterGet,
                    onRelease: OnMonsterRelease
                );

                _monsterPools[entry.Prefab] = pool;
            }
        }

        /// <summary>
        /// 스폰 활성화
        /// </summary>
        public void StartSpawning(int waveIndex = 1)
        {
            _currentWaveIndex = waveIndex;
            
            // 스폰 비활성화 체크
            if (!_settings.IsMonsterSpawnEnabled)
            {
                _isSpawningEnabled = false;
                Debug.Log("[MonsterSpawner] 몬스터 스폰이 비활성화되어 있습니다. (CombatSettings 확인)");
                return;
            }

            _isSpawningEnabled = true;
            _patternCooldownTimer = 0f;
            _currentPattern = null;
            _pendingSpawns.Clear();
        }

        /// <summary>
        /// 스폰 비활성화
        /// </summary>
        public void StopSpawning()
        {
            _isSpawningEnabled = false;
            _pendingSpawns.Clear();
        }

        /// <summary>
        /// 웨이브 초기화
        /// </summary>
        public void ResetWave()
        {
            _patternsCompletedThisWave = 0;
            _monstersSpawnedThisWave = 0;
            _patternCooldownTimer = 0f;
            _currentPattern = null;
            _pendingSpawns.Clear();
        }

        private void Update()
        {
            if (!_isSpawningEnabled || _settings == null) return;

            UpdatePatternSpawning();
        }

        #region 패턴 기반 스폰 시스템

        /// <summary>
        /// 패턴 기반 스폰 로직
        /// </summary>
        private void UpdatePatternSpawning()
        {
            int patternsPerWave = _settings.GetPatternsPerWave(_currentWaveIndex);
            int maxOnScreen = _settings.GetMaxMonstersOnScreen(_currentWaveIndex);

            // 웨이브 완료 체크 (패턴 수 기반)
            if (_patternsCompletedThisWave >= patternsPerWave) return;

            // 화면 제한 체크
            if (_activeMonsters.Count >= maxOnScreen) return;

            // 패턴 쿨다운 처리
            if (_patternCooldownTimer > 0f)
            {
                _patternCooldownTimer -= Time.deltaTime;
                return;
            }

            // 대기 중인 스폰 요청 처리
            if (_pendingSpawns.Count > 0)
            {
                ProcessPendingSpawn();
                return;
            }

            // 새 패턴 시작 (아직 패턴 완료 수가 부족할 때만)
            StartNewPattern();
        }

        /// <summary>
        /// 새로운 패턴 시작
        /// </summary>
        private void StartNewPattern()
        {
            if (_settings.HasPatterns)
            {
                _currentPattern = _settings.GetRandomPattern();

                if (_currentPattern != null && _currentPattern.MonsterCount > 0)
                {
                    int slotsToUse = _currentPattern.MonsterCount;

                    // 패턴의 각 슬롯을 스폰 큐에 추가
                    for (int i = 0; i < slotsToUse; i++)
                    {
                        float delay = i * _currentPattern.SpawnDelay;
                        var slot = _currentPattern.SpawnSlots[i];

                        _pendingSpawns.Enqueue(new PatternSpawnRequest
                        {
                            Pattern = _currentPattern,
                            SlotIndex = i,
                            MonsterKind = slot.MonsterKind,
                            SpawnDelayRemaining = delay
                        });
                    }

                    Debug.Log($"[MonsterSpawner] 패턴 시작: {_currentPattern.PatternName} ({slotsToUse}마리)");
                    return;
                }
            }

            // 패턴이 없거나 실패 시 폴백 스폰
            if (_settings.UseFallbackSpawning)
            {
                SpawnSingleMonster();
                // 폴백 스폰도 하나의 패턴으로 취급
                _patternsCompletedThisWave++;
                _patternCooldownTimer = 0.5f; // 기본 폴백 쿨다운
            }
        }

        /// <summary>
        /// 대기 중인 스폰 요청 처리
        /// </summary>
        private void ProcessPendingSpawn()
        {
            if (_pendingSpawns.Count == 0) return;

            var request = _pendingSpawns.Peek();

            // 딜레이 처리
            request.SpawnDelayRemaining -= Time.deltaTime;

            if (request.SpawnDelayRemaining <= 0f)
            {
                _pendingSpawns.Dequeue();
                SpawnFromPattern(request);

                // 마지막 스폰이면 패턴 완료 처리
                if (_pendingSpawns.Count == 0 && _currentPattern != null)
                {
                    _patternsCompletedThisWave++;
                    _patternCooldownTimer = _currentPattern.PatternCooldown;
                    Debug.Log($"[MonsterSpawner] 패턴 완료: {_currentPattern.PatternName} ({_patternsCompletedThisWave}/{_settings.GetPatternsPerWave(_currentWaveIndex)}), 쿨다운: {_patternCooldownTimer}초");
                }
            }
        }

        /// <summary>
        /// 패턴 슬롯에서 몬스터 스폰
        /// </summary>
        private void SpawnFromPattern(PatternSpawnRequest request)
        {
            MonsterUnitView prefab = _settings.GetRandomMonsterPrefab();
            if (prefab == null)
            {
                Debug.LogError("[MonsterSpawner] 몬스터 프리팹이 없습니다! CombatSettings를 확인하세요.");
                return;
            }

            var monster = GetMonsterFromPool(prefab);
            if (monster == null) return;

            // 스폰 위치 계산 (패턴 기반)
            Vector3 spawnPos = request.Pattern.GetSpawnPosition(
                request.SlotIndex,
                _spawnPoint.position,
                _settings.MapBoundsYMin,
                _settings.MapBoundsYMax,
                _settings.MonsterXSpacing
            );

            // 기존 몬스터와 X 간격 유지
            spawnPos = AdjustSpawnPositionForSpacing(spawnPos);

            monster.transform.position = spawnPos;

            // 몬스터 종류: 패턴 슬롯에서 지정한 것을 우선 사용
            var monsterKind = request.MonsterKind;

            // 스탯 설정
            var spawnInfo = _combatService.GetMonsterSpawnInfo();
            monster.Initialize();
            monster.SetupStats(spawnInfo.MaxHp, spawnInfo.Defense, spawnInfo.Level, monsterKind);
            monster.Flip(true);

            // 전투 Y 범위로 수렴할 목표 Y 설정
            // 패턴의 Y 위치를 기반으로 하되, CombatY 범위 내로 클램핑
            float patternY = spawnPos.y;
            float targetCombatY = _settings.ClampToCombatY(patternY);
            float yOffset = targetCombatY - _playerTransform.position.y;
            monster.SetTargetYOffset(yOffset);

            FinalizeSpawn(monster);
        }

        /// <summary>
        /// 단일 몬스터 스폰 (폴백)
        /// </summary>
        private void SpawnSingleMonster()
        {
            MonsterUnitView prefab = _settings.GetRandomMonsterPrefab();
            if (prefab == null)
            {
                Debug.LogError("[MonsterSpawner] 몬스터 프리팹이 없습니다! CombatSettings를 확인하세요.");
                return;
            }

            var monster = GetMonsterFromPool(prefab);
            if (monster == null) return;

            // 기본 스폰 위치 (랜덤 Y - 맵 경계 내)
            Vector3 spawnPos = _spawnPoint.position;
            float randomY = UnityEngine.Random.Range(_settings.MapBoundsYMin, _settings.MapBoundsYMax);
            spawnPos.y = randomY;

            // 간격 조정
            spawnPos = AdjustSpawnPositionForSpacing(spawnPos);

            monster.transform.position = spawnPos;

            // 몬스터 종류: 폴백은 Normal
            var monsterKind = MonsterKind.Normal;

            // 스탯 설정
            var spawnInfo = _combatService.GetMonsterSpawnInfo();
            monster.Initialize();
            monster.SetupStats(spawnInfo.MaxHp, spawnInfo.Defense, spawnInfo.Level, monsterKind);
            monster.Flip(true);

            // 전투 Y 범위로 수렴할 목표 Y 설정 (기본값 사용)
            float targetCombatY = _settings.CombatYDefault;
            float yOffset = targetCombatY - _playerTransform.position.y;
            monster.SetTargetYOffset(yOffset);

            FinalizeSpawn(monster);
        }

        /// <summary>
        /// 스폰 완료 처리
        /// </summary>
        private void FinalizeSpawn(MonsterUnitView monster)
        {
            _activeMonsters.Add(monster);
            _monstersSpawnedThisWave++;

            OnMonsterSpawned?.Invoke(monster);

            Debug.Log($"[MonsterSpawner] 몬스터 스폰: 위치={monster.transform.position}, 종류={monster.MonsterKind}");
        }

        /// <summary>
        /// 풀에서 몬스터 가져오기
        /// </summary>
        private MonsterUnitView GetMonsterFromPool(MonsterUnitView prefab)
        {
            if (!_monsterPools.TryGetValue(prefab, out var pool))
            {
                pool = new MonoObjectPool<MonsterUnitView>(
                    prefab: prefab,
                    parent: _spawnPoint,
                    initialSize: 1,
                    maxSize: _poolMaxSize,
                    onGet: OnMonsterGet,
                    onRelease: OnMonsterRelease
                );
                _monsterPools[prefab] = pool;
            }

            return pool.Get();
        }

        /// <summary>
        /// 기존 몬스터와 X 간격 조정
        /// </summary>
        private Vector3 AdjustSpawnPositionForSpacing(Vector3 basePos)
        {
            float requiredX = basePos.x;

            foreach (var existingMonster in _activeMonsters)
            {
                if (existingMonster == null) continue;

                float existingX = existingMonster.transform.position.x;
                float minRequiredX = existingX + _settings.MonsterXSpacing;

                if (requiredX < minRequiredX)
                {
                    requiredX = minRequiredX;
                }
            }

            if (_activeMonsters.Count > 0)
            {
                basePos.x = Mathf.Max(basePos.x, requiredX);
            }

            return basePos;
        }

        #endregion

        #region 전투 참여 관리

        /// <summary>
        /// 몬스터 전투 참여 시도 (CombatRunner에서 호출)
        /// </summary>
        public bool TryEngageMonster(MonsterUnitView monster)
        {
            if (monster == null || monster.IsDead) return false;
            if (_engagedMonsters.Contains(monster)) return true;

            int maxTargets = _combatService?.GetMaxTargetCount() ?? 1;
            if (_engagedMonsters.Count >= maxTargets) return false;

            _engagedMonsters.Add(monster);
            monster.SetWaitingForSpace(false);
            OnMonsterEngaged?.Invoke(monster);

            return true;
        }

        /// <summary>
        /// 몬스터 전투 이탈 처리
        /// </summary>
        public void DisengageMonster(MonsterUnitView monster)
        {
            if (monster == null) return;
            _engagedMonsters.Remove(monster);
        }

        #endregion

        #region 몬스터 해제/사망

        /// <summary>
        /// 몬스터 해제 (풀로 반환)
        /// </summary>
        public void ReleaseMonster(MonsterUnitView monster)
        {
            if (monster == null) return;

            _activeMonsters.Remove(monster);
            _engagedMonsters.Remove(monster);

            OnMonsterReleased?.Invoke(monster);

            // 풀로 반환
            foreach (var kvp in _monsterPools)
            {
                kvp.Value.Release(monster);
                return;
            }

            // 풀에서 찾지 못하면 비활성화만
            monster.gameObject.SetActive(false);
        }

        /// <summary>
        /// 몬스터 사망 처리
        /// </summary>
        public void HandleMonsterDeath(MonsterUnitView monster)
        {
            if (monster == null) return;

            // 즉시 활성/전투 리스트에서 제거 (웨이브 클리어 조건 충족을 위해)
            _activeMonsters.Remove(monster);
            _engagedMonsters.Remove(monster);

            OnMonsterKilled?.Invoke(monster);

            // 딜레이 후 풀 반환 (사망 애니메이션 시간)
            ReleaseMonsterDelayedAsync(monster).Forget();
        }

        private async UniTaskVoid ReleaseMonsterDelayedAsync(MonsterUnitView monster)
        {
            await UniTask.Delay(TimeSpan.FromSeconds(_settings.DeathToSpawnDelay + 0.5f));

            if (monster != null)
            {
                // 풀 반환만 수행 (이미 리스트에서 제거됨)
                ReleaseMonster(monster);
            }
        }

        #endregion

        #region 타겟 조회

        /// <summary>
        /// 현재 전투 중인 첫 번째 몬스터 (주 타겟)
        /// </summary>
        public MonsterUnitView GetCurrentTarget()
        {
            CleanupDeadMonsters();
            return _engagedMonsters.Count > 0 ? _engagedMonsters[0] : null;
        }

        /// <summary>
        /// 전투 중인 모든 몬스터 반환
        /// </summary>
        public IReadOnlyList<MonsterUnitView> GetEngagedTargets()
        {
            CleanupDeadMonsters();
            return _engagedMonsters;
        }

        /// <summary>
        /// 죽은 몬스터 정리
        /// </summary>
        private void CleanupDeadMonsters()
        {
            for (int i = _activeMonsters.Count - 1; i >= 0; i--)
            {
                var monster = _activeMonsters[i];
                if (monster == null || !monster.gameObject.activeInHierarchy)
                {
                    _activeMonsters.RemoveAt(i);
                }
            }

            for (int i = _engagedMonsters.Count - 1; i >= 0; i--)
            {
                var monster = _engagedMonsters[i];
                if (monster == null || monster.IsDead || !monster.gameObject.activeInHierarchy)
                {
                    _engagedMonsters.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// 모든 활성 몬스터 해제
        /// </summary>
        public void ReleaseAllMonsters()
        {
            for (int i = _activeMonsters.Count - 1; i >= 0; i--)
            {
                var monster = _activeMonsters[i];
                if (monster != null)
                {
                    ReleaseMonster(monster);
                }
            }
            _activeMonsters.Clear();
            _engagedMonsters.Clear();
            _pendingSpawns.Clear();
        }

        #endregion

        #region 풀 콜백

        private void OnMonsterGet(MonsterUnitView monster)
        {
            monster.gameObject.SetActive(true);
        }

        private void OnMonsterRelease(MonsterUnitView monster)
        {
            monster.ResetForPool();
            monster.transform.SetParent(_spawnPoint);
            monster.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            foreach (var pool in _monsterPools.Values)
            {
                pool.Clear();
            }
            _monsterPools.Clear();
            _activeMonsters.Clear();
            _engagedMonsters.Clear();
            _pendingSpawns.Clear();
        }

        #endregion
    }

    /// <summary>
    /// 패턴 스폰 요청 데이터
    /// </summary>
    internal class PatternSpawnRequest
    {
        public SpawnPattern Pattern;
        public int SlotIndex;
        public MonsterKind MonsterKind;
        public float SpawnDelayRemaining;
    }
}
