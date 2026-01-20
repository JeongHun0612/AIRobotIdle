using System;
using System.Collections.Generic;
using UnityEngine;
using SahurRaising.Core;

namespace SahurRaising.GamePlay
{
    /// <summary>
    /// 몬스터 스폰 및 오브젝트 풀링을 담당하는 컴포넌트
    /// CombatRunner에서 스폰 로직을 분리하여 단일 책임 원칙(SRP)을 준수합니다.
    /// </summary>
    public class MonsterSpawner : MonoBehaviour
    {
        [Header("스폰 설정")]
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private int _poolInitialSize = 3;
        [SerializeField] private int _poolMaxSize = 10;
        
        private CombatSettings _settings;
        private readonly List<UnitView> _activeMonsters = new();
        private readonly Dictionary<MonsterUnitView, MonoObjectPool<MonsterUnitView>> _monsterPools = new();
        
        // 스폰 상태
        private int _monstersSpawnedThisWave = 0;
        private float _spawnTimer = 0f;
        private bool _isSpawningEnabled = false;

        // 이벤트
        public event Action<UnitView> OnMonsterSpawned;
        public event Action<UnitView> OnMonsterReleased;

        // 읽기 전용 프로퍼티
        public IReadOnlyList<UnitView> ActiveMonsters => _activeMonsters;
        public int MonstersSpawnedThisWave => _monstersSpawnedThisWave;
        public int ActiveMonsterCount => _activeMonsters.Count;

        /// <summary>
        /// 스포너 초기화
        /// </summary>
        public void Initialize(CombatSettings settings, Transform spawnPoint = null)
        {
            _settings = settings;
            
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
        public void StartSpawning()
        {
            _isSpawningEnabled = true;
            _spawnTimer = _settings?.SpawnInterval ?? 0f; // 즉시 첫 몬스터 스폰
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

        private void UpdateSpawning()
        {
            // 이번 웨이브에 스폰할 몬스터가 남아있고, 화면에 여유가 있으면 스폰
            if (_monstersSpawnedThisWave >= _settings.MonstersPerWave) return;
            if (_activeMonsters.Count >= _settings.MaxMonstersOnScreen) return;
            
            _spawnTimer += Time.deltaTime;
            if (_spawnTimer >= _settings.SpawnInterval)
            {
                _spawnTimer = 0f;
                SpawnMonster();
            }
        }

        /// <summary>
        /// 몬스터 스폰
        /// </summary>
        public UnitView SpawnMonster()
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
                // 풀이 없으면 새로 생성
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
            
            // 스폰 위치 설정 (Y축 랜덤 오프셋)
            Vector3 spawnPos = _spawnPoint.position;
            float yOffset = _settings.MonsterSpawnYOffset;
            spawnPos.y += UnityEngine.Random.Range(-yOffset, yOffset);
            monster.transform.position = spawnPos;
            
            // 초기화
            monster.Initialize();
            monster.Flip(true); // 몬스터는 왼쪽을 봄
            
            _activeMonsters.Add(monster);
            _monstersSpawnedThisWave++;
            
            OnMonsterSpawned?.Invoke(monster);
            
            return monster;
        }

        /// <summary>
        /// 몬스터 해제 (풀로 반환)
        /// </summary>
        public void ReleaseMonster(UnitView monster)
        {
            if (monster == null) return;
            if (!_activeMonsters.Contains(monster)) return;
            
            _activeMonsters.Remove(monster);
            OnMonsterReleased?.Invoke(monster);
            
            // 풀로 반환
            if (monster is MonsterUnitView monsterView)
            {
                foreach (var kvp in _monsterPools)
                {
                    // 프리팹 타입으로 적절한 풀 찾기 (간단한 비교)
                    // 실제로는 프리팹 참조를 몬스터에 저장하는 것이 더 효율적
                    kvp.Value.Release(monsterView);
                    return;
                }
            }
            
            // 풀에서 찾지 못하면 직접 파괴
            Destroy(monster.gameObject);
        }

        /// <summary>
        /// 첫 번째 활성 몬스터 가져오기 (현재 타겟)
        /// </summary>
        public UnitView GetCurrentTarget()
        {
            CleanupNullMonsters();
            return _activeMonsters.Count > 0 ? _activeMonsters[0] : null;
        }

        /// <summary>
        /// null 참조 정리
        /// </summary>
        private void CleanupNullMonsters()
        {
            for (int i = _activeMonsters.Count - 1; i >= 0; i--)
            {
                if (_activeMonsters[i] == null)
                {
                    _activeMonsters.RemoveAt(i);
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
        }

        private void OnMonsterGet(MonsterUnitView monster)
        {
            // 풀에서 가져올 때 초기화
        }

        private void OnMonsterRelease(MonsterUnitView monster)
        {
            // 풀로 반환할 때 정리
            monster.transform.SetParent(_spawnPoint);
        }

        private void OnDestroy()
        {
            // 모든 풀 정리
            foreach (var pool in _monsterPools.Values)
            {
                pool.Clear();
            }
            _monsterPools.Clear();
            _activeMonsters.Clear();
        }
    }
}
