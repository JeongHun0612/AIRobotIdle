using System;
using System.Collections.Generic;
using SahurRaising.Core;
using UnityEngine;

namespace SahurRaising.GamePlay
{
    /// <summary>
    /// 몬스터 스폰 및 오브젝트 풀링을 담당하는 컴포넌트
    /// 
    /// 주요 기능:
    /// - 오브젝트 풀링을 통한 몬스터 재사용
    /// - 몬스터 간 적절한 간격 유지 (Y: 랜덤, X: 겹침 방지)
    /// - 전투 범위 내 몬스터와 대기 중 몬스터 구분
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
        private int _monstersSpawnedThisWave = 0;
        private float _spawnTimer = 0f;
        private bool _isSpawningEnabled = false;
        private Transform _playerTransform;
        private int _currentWaveIndex = 1;

        // 사인파 스폰 패턴용 누적 스폰 카운터 (웨이브 리셋해도 유지)
        private int _totalSpawnCounter = 0;

        // 읽기 전용 프로퍼티
        public IReadOnlyList<MonsterUnitView> ActiveMonsters => _activeMonsters;
        public IReadOnlyList<MonsterUnitView> EngagedMonsters => _engagedMonsters;
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
            _isSpawningEnabled = true;
            _spawnTimer = _settings?.GetSpawnInterval(_currentWaveIndex) ?? 0f;
        }

        /// <summary>
        /// 스폰 비활성화
        /// </summary>
        public void StopSpawning()
        {
            _isSpawningEnabled = false;
        }

        /// <summary>
        /// 웨이브 초기화
        /// </summary>
        public void ResetWave()
        {
            _monstersSpawnedThisWave = 0;
            _spawnTimer = 0f;
        }

        private void Update()
        {
            if (!_isSpawningEnabled || _settings == null) return;

            UpdateSpawning();
        }

        /// <summary>
        /// 스폰 로직
        /// </summary>
        private void UpdateSpawning()
        {
            int monstersPerWave = _settings.GetMonstersPerWave(_currentWaveIndex);
            int maxOnScreen = _settings.GetMaxMonstersOnScreen(_currentWaveIndex);
            float spawnInterval = _settings.GetSpawnInterval(_currentWaveIndex);

            if (_monstersSpawnedThisWave >= monstersPerWave) return;
            if (_activeMonsters.Count >= maxOnScreen) return;

            _spawnTimer += Time.deltaTime;
            if (_spawnTimer >= spawnInterval)
            {
                _spawnTimer = 0f;
                SpawnMonster();
            }
        }

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
            if (_engagedMonsters.Remove(monster))
            {
                // 필요 시 추가 처리
            }
        }

        /// <summary>
        /// 몬스터 스폰
        /// </summary>
        public MonsterUnitView SpawnMonster()
        {
            MonsterUnitView prefab = _settings.GetRandomMonsterPrefab();
            if (prefab == null)
            {
                Debug.LogError("[MonsterSpawner] 몬스터 프리팹이 없습니다! CombatSettings를 확인하세요.");
                return null;
            }

            // 풀에서 가져오기
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

            var monster = pool.Get();

            // 스폰 위치 설정 (Y축 사인파 패턴, 기존 몬스터와 X 간격 유지)
            Vector3 spawnPos = CalculateSpawnPosition();
            monster.transform.position = spawnPos;

            // 몬스터 종류 결정 (WaveConfig 기반)
            var monsterKind = DetermineMonsterKind();

            // 스탯 설정 (CombatService에서 기본 스탯을 가져오되, Kind는 WaveConfig에서 결정)
            var spawnInfo = _combatService.GetMonsterSpawnInfo();
            monster.Initialize();
            monster.SetupStats(spawnInfo.MaxHp, spawnInfo.Defense, spawnInfo.Level, monsterKind);
            monster.Flip(true); // 몬스터는 왼쪽을 봄

            // 목표 Y 오프셋 설정 (사인파 기반으로 각 몬스터마다 다른 오프셋)
            // 전투 범위 진입 시 플레이어 Y + 이 오프셋 위치에서 고정됨
            float yOffset = _settings.CombatYOffsetRange *
                Mathf.Sin(_totalSpawnCounter * _settings.SineFrequencyRadians);
            monster.SetTargetYOffset(yOffset);

            _activeMonsters.Add(monster);
            _monstersSpawnedThisWave++;

            OnMonsterSpawned?.Invoke(monster);

            return monster;
        }

        /// <summary>
        /// 현재 웨이브의 WaveConfig를 기반으로 몬스터 종류 결정
        /// Normal -> Elite -> Boss 순서로 스폰
        /// </summary>
        private MonsterKind DetermineMonsterKind()
        {
            var waveConfig = _settings.GetWaveConfig(_currentWaveIndex);

            // 현재까지 스폰한 마리수 기준으로 종류 결정
            // Normal(0~N-1) -> Elite(N~N+E-1) -> Boss(N+E~끝)
            int normalEnd = waveConfig.NormalCount;
            int eliteEnd = normalEnd + waveConfig.EliteCount;

            if (_monstersSpawnedThisWave < normalEnd)
            {
                return MonsterKind.Normal;
            }
            else if (_monstersSpawnedThisWave < eliteEnd)
            {
                return MonsterKind.Elite;
            }
            else
            {
                return MonsterKind.Boss;
            }
        }

        /// <summary>
        /// 스폰 위치 계산 (사인파 패턴 Y + X 간격 유지)
        /// 
        /// Y축 위치는 스폰 순서에 따라 사인파 함수로 결정:
        /// Y = SineAmplitude * sin(spawnIndex * (2π / SinePeriodPerMonsters))
        /// 
        /// 플레이어로 가까워지면서 Y=플레이어Y로 수렴하는 것은 CombatRunner에서 처리
        /// </summary>
        private Vector3 CalculateSpawnPosition()
        {
            Vector3 basePos = _spawnPoint.position;

            // 사인파 기반 Y축 오프셋 계산
            // SineFrequencyRadians = 2π / SinePeriodPerMonsters
            float yOffset = _settings.SineAmplitude * Mathf.Sin(_totalSpawnCounter * _settings.SineFrequencyRadians);
            basePos.y += yOffset;

            // 맵 경계 보정
            basePos.y = _settings.ClampSpawnY(basePos.y);

            // 기존 몬스터들과 X 간격 체크
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

            // 기존 몬스터가 있으면 그 뒤에 스폰
            if (_activeMonsters.Count > 0)
            {
                basePos.x = Mathf.Max(basePos.x, requiredX);
            }

            // 누적 스폰 카운터 증가 (웨이브 리셋해도 계속 증가하여 사인파 연속성 유지)
            _totalSpawnCounter++;

            return basePos;
        }

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

            _engagedMonsters.Remove(monster);

            OnMonsterKilled?.Invoke(monster);

            // 딜레이 후 풀 반환 (사망 애니메이션 시간)
            ReleaseMonsterDelayedAsync(monster).Forget();
        }

        private async Cysharp.Threading.Tasks.UniTaskVoid ReleaseMonsterDelayedAsync(MonsterUnitView monster)
        {
            await Cysharp.Threading.Tasks.UniTask.Delay(
                TimeSpan.FromSeconds(_settings.DeathToSpawnDelay + 0.5f));

            if (monster != null)
            {
                _activeMonsters.Remove(monster);
                ReleaseMonster(monster);
            }
        }

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
        }

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
        }
    }
}
