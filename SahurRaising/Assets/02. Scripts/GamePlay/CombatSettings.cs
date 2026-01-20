using System;
using System.Collections.Generic;
using UnityEngine;

namespace SahurRaising.GamePlay
{
    /// <summary>
    /// 전투 설정을 담당하는 ScriptableObject
    /// 몬스터 스폰 갯수, 프리팹 목록, 웨이브 설정 등을 관리합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "CombatSettings", menuName = "SahurRaising/Combat/CombatSettings")]
    public class CombatSettings : ScriptableObject
    {
        [Header("=== 몬스터 스폰 설정 ===")]
        [Tooltip("웨이브당 스폰할 몬스터 수")]
        [SerializeField, Range(1, 10)] private int _monstersPerWave = 3;
        
        [Tooltip("동시에 화면에 존재할 수 있는 최대 몬스터 수")]
        [SerializeField, Range(1, 5)] private int _maxMonstersOnScreen = 2;
        
        [Tooltip("몬스터 스폰 간격 (초)")]
        [SerializeField, Range(0.1f, 3f)] private float _spawnInterval = 0.5f;
        
        [Header("=== 몬스터 프리팹 목록 ===")]
        [Tooltip("랜덤으로 선택될 몬스터 프리팹들 (외형만 다름, 스펙은 테이블에서 관리)")]
        [SerializeField] private List<MonsterVisualEntry> _monsterVisuals = new();
        
        [Header("=== 이동 설정 ===")]
        [Tooltip("몬스터 이동 속도")]
        [SerializeField, Range(1f, 10f)] private float _monsterMoveSpeed = 3f;
        
        [Tooltip("배경 스크롤 속도")]
        [SerializeField, Range(0.5f, 50f)] private float _backgroundScrollSpeed = 5f;
        
        [Tooltip("플레이어 전진 거리 (이동 중 살짝 앞으로)")]
        [SerializeField, Range(0f, 20f)] private float _playerAdvanceDistance = 3f;
        
        [Tooltip("플레이어 전진/복귀 속도")]
        [SerializeField, Range(1f, 10f)] private float _playerMoveSpeed = 3f;
        
        [Header("=== 전투 설정 ===")]
        [Tooltip("공격 사거리")]
        [SerializeField, Range(0.5f, 3f)] private float _attackRange = 1.5f;
        
        [Tooltip("몬스터 사망 후 다음 스폰까지 대기 시간")]
        [SerializeField, Range(0f, 1f)] private float _deathToSpawnDelay = 0.3f;

        [Header("=== 플레이어 위치 설정 ===")]
        [Tooltip("몬스터 스폰 시 Y축 랜덤 오프셋 범위 (±)")]
        [SerializeField, Range(0f, 1f)] private float _monsterSpawnYOffset = 0.3f;
        
        [Tooltip("플레이어 복귀 속도 배율 (전진 속도 대비)")]
        [SerializeField, Range(1f, 3f)] private float _playerReturnSpeedMultiplier = 1.5f;

        // Properties
        public int MonstersPerWave => _monstersPerWave;
        public int MaxMonstersOnScreen => _maxMonstersOnScreen;
        public float SpawnInterval => _spawnInterval;
        public float MonsterMoveSpeed => _monsterMoveSpeed;
        public float BackgroundScrollSpeed => _backgroundScrollSpeed;
        public float PlayerAdvanceDistance => _playerAdvanceDistance;
        public float PlayerMoveSpeed => _playerMoveSpeed;
        public float AttackRange => _attackRange;
        public float DeathToSpawnDelay => _deathToSpawnDelay;
        public float MonsterSpawnYOffset => _monsterSpawnYOffset;
        public float PlayerReturnSpeedMultiplier => _playerReturnSpeedMultiplier;
        public IReadOnlyList<MonsterVisualEntry> MonsterVisuals => _monsterVisuals;

        /// <summary>
        /// 랜덤한 몬스터 프리팹을 반환합니다.
        /// </summary>
        public MonsterUnitView GetRandomMonsterPrefab()
        {
            if (_monsterVisuals == null || _monsterVisuals.Count == 0)
                return null;
            
            // 가중치 기반 랜덤 선택
            float totalWeight = 0f;
            foreach (var entry in _monsterVisuals)
            {
                totalWeight += entry.SpawnWeight;
            }
            
            float randomValue = UnityEngine.Random.Range(0f, totalWeight);
            float cumulative = 0f;
            
            foreach (var entry in _monsterVisuals)
            {
                cumulative += entry.SpawnWeight;
                if (randomValue <= cumulative)
                    return entry.Prefab;
            }
            
            return _monsterVisuals[0].Prefab;
        }
    }

    /// <summary>
    /// 몬스터 비주얼 엔트리 - 프리팹과 스폰 가중치를 정의
    /// </summary>
    [Serializable]
    public class MonsterVisualEntry
    {
        [Tooltip("몬스터 프리팹")]
        public MonsterUnitView Prefab;
        
        [Tooltip("스폰 확률 가중치 (높을수록 자주 등장)")]
        [Range(0.1f, 10f)]
        public float SpawnWeight = 1f;
        
        [Tooltip("이 몬스터의 설명 (에디터용)")]
        public string Description;
    }
}
