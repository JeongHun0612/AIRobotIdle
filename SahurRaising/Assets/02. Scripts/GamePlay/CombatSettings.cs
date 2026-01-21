using System;
using System.Collections.Generic;
using SahurRaising.Core;
using UnityEngine;

namespace SahurRaising.GamePlay
{
    /// <summary>
    /// 전투 설정을 담당하는 ScriptableObject
    /// 
    /// 모든 전투 관련 설정값을 중앙에서 관리합니다.
    /// - 몬스터 스폰/배치 설정
    /// - 플레이어/배경 이동 설정
    /// - 전투 메커니즘 설정
    /// </summary>
    [CreateAssetMenu(fileName = "CombatSettings", menuName = "SahurRaising/Combat/CombatSettings")]
    public class CombatSettings : ScriptableObject
    {
        [Header("=== 웨이브별 세부 설정 ===")]
        [Tooltip("각 웨이브별 몬스터 수 및 스폰 설정. 비어있으면 기본값 사용.")]
        [SerializeField] private List<WaveConfig> _waveConfigs = new();

        [Header("=== 기본 웨이브 설정 (웨이브 설정이 없을 때 사용) ===")]
        [Tooltip("웨이브당 스폰할 총 몬스터 수 (기본값)")]
        [SerializeField, Range(1, 30)] private int _defaultMonstersPerWave = 10;

        [Tooltip("동시에 화면에 존재할 수 있는 최대 몬스터 수 (기본값)")]
        [SerializeField, Range(1, 10)] private int _defaultMaxMonstersOnScreen = 5;

        [Tooltip("몬스터 스폰 간격 - 초 (기본값)")]
        [SerializeField, Range(0.1f, 3f)] private float _defaultSpawnInterval = 0.5f;

        [Tooltip("스테이지당 웨이브 수")]
        [SerializeField, Range(1, 10)] private int _wavesPerStage = 4;

        [Header("=== 몬스터 배치 설정 ===")]
        [Tooltip("몬스터 간 최소 X축 간격 (겹침 방지)")]
        [SerializeField, Range(0.5f, 5f)] private float _monsterXSpacing = 1.5f;

        [Header("=== 몬스터 Y축 사인파 스폰 설정 ===")]
        [Tooltip("사인파 진폭 (Y축 최대 변위).\n" +
                 "예: 20 = 스폰포인트 기준 위아래로 최대 20 유닛까지 이동")]
        [SerializeField, Range(0f, 100f)] private float _sineAmplitude = 20f;

        [Tooltip("사인파 주기 (몬스터 N마리당 1주기).\n" +
                 "예: 4 = 4마리마다 사인파가 한 바퀴 돔\n" +
                 "예: 8 = 8마리마다 사인파가 한 바퀴 돔 (더 느리게 변화)")]
        [SerializeField, Range(1f, 20f)] private float _sinePeriodPerMonsters = 6f;

        [Tooltip("플레이어 Y로 수렴하는 속도.\n" +
                 "몬스터가 플레이어에게 가까워질수록 Y축이 플레이어 Y + 오프셋으로 수렴합니다.\n" +
                 "높을수록 빠르게 수렴 (10 = 빠름, 1 = 느림)")]
        [SerializeField, Range(0.5f, 20f)] private float _yConvergenceSpeed = 3f;

        [Tooltip("전투 시 몬스터 Y 오프셋 범위.\n" +
                 "플레이어 Y ± 이 값 범위 내에서 각 몬스터의 목표 Y가 결정됩니다.\n" +
                 "예: 10 = 플레이어 Y -10 ~ +10 사이에 분포")]
        [SerializeField, Range(0f, 50f)] private float _combatYOffsetRange = 15f;

        [Header("=== 맵 경계 설정 (월드 좌표 기준) ===")]
        [Tooltip("전투 영역 Y축 최소값 (하단 경계) - 월드 좌표 기준.\n" +
                 "Unity 씬에서 맵 하단 경계의 Y 좌표를 입력하세요.")]
        [SerializeField] private float _mapBoundsYMin = -30f;

        [Tooltip("전투 영역 Y축 최대값 (상단 경계) - 월드 좌표 기준.\n" +
                 "Unity 씬에서 맵 상단 경계의 Y 좌표를 입력하세요.")]
        [SerializeField] private float _mapBoundsYMax = 30f;

        [Tooltip("경계 여유 공간 (몬스터가 경계에 너무 가깝지 않도록).\n" +
                 "예: 5 = 경계에서 5유닛 안쪽까지만 스폰")]
        [SerializeField, Range(0f, 20f)] private float _boundsMargin = 5f;

        [Header("=== 몬스터 프리팹 목록 ===")]
        [Tooltip("랜덤으로 선택될 몬스터 프리팹들 (외형만 다름, 스펙은 테이블에서 관리)")]
        [SerializeField] private List<MonsterVisualEntry> _monsterVisuals = new();

        [Header("=== 이동 설정 ===")]
        [Tooltip("몬스터 이동 속도")]
        [SerializeField, Range(1f, 10f)] private float _monsterMoveSpeed = 3f;

        [Tooltip("배경 스크롤 속도")]
        [SerializeField, Range(0.5f, 50f)] private float _backgroundScrollSpeed = 5f;

        [Tooltip("플레이어 전진 거리 (이동 모드에서 살짝 앞으로)")]
        [SerializeField, Range(0f, 20f)] private float _playerAdvanceDistance = 3f;

        [Tooltip("플레이어 이동 속도")]
        [SerializeField, Range(1f, 10f)] private float _playerMoveSpeed = 3f;

        [Tooltip("플레이어 복귀 속도 배율 (전진 속도 대비)")]
        [SerializeField, Range(1f, 3f)] private float _playerReturnSpeedMultiplier = 1.5f;

        [Header("=== 전투 설정 ===")]
        [Tooltip("공격 사거리 (플레이어-몬스터 거리)")]
        [SerializeField, Range(0.5f, 5f)] private float _attackRange = 1.5f;

        [Tooltip("몬스터 사망 후 풀 반환까지 대기 시간 (사망 애니메이션 시간)")]
        [SerializeField, Range(0f, 2f)] private float _deathToSpawnDelay = 0.5f;

        // ===== Properties =====

        // 스테이지/웨이브
        public int WavesPerStage => _wavesPerStage;
        public IReadOnlyList<WaveConfig> WaveConfigs => _waveConfigs;

        // 기본값 (웨이브 설정이 없을 때)
        public int DefaultMonstersPerWave => _defaultMonstersPerWave;
        public int DefaultMaxMonstersOnScreen => _defaultMaxMonstersOnScreen;
        public float DefaultSpawnInterval => _defaultSpawnInterval;

        /// <summary>
        /// 특정 웨이브의 설정을 반환합니다.
        /// 해당 웨이브 설정이 없으면 기본값으로 WaveConfig를 생성하여 반환합니다.
        /// </summary>
        /// <param name="waveIndex">1부터 시작하는 웨이브 인덱스</param>
        public WaveConfig GetWaveConfig(int waveIndex)
        {
            // 인덱스 변환 (1-based -> 0-based)
            int index = waveIndex - 1;

            if (_waveConfigs != null && index >= 0 && index < _waveConfigs.Count)
            {
                return _waveConfigs[index];
            }

            // 설정이 없으면 기본값으로 생성
            return new WaveConfig
            {
                NormalCount = _defaultMonstersPerWave,
                EliteCount = 0,
                BossCount = 0,
                MaxMonstersOnScreen = _defaultMaxMonstersOnScreen,
                SpawnInterval = _defaultSpawnInterval
            };
        }

        /// <summary>
        /// 해당 웨이브에서 스폰할 총 몬스터 수 반환 (Normal + Elite + Boss)
        /// </summary>
        public int GetMonstersPerWave(int waveIndex) => GetWaveConfig(waveIndex).TotalMonstersToSpawn;

        /// <summary>
        /// 해당 웨이브에서 동시에 화면에 있을 수 있는 최대 몬스터 수 반환
        /// </summary>
        public int GetMaxMonstersOnScreen(int waveIndex) => GetWaveConfig(waveIndex).MaxMonstersOnScreen;

        /// <summary>
        /// 해당 웨이브의 스폰 간격 반환
        /// </summary>
        public float GetSpawnInterval(int waveIndex) => GetWaveConfig(waveIndex).SpawnInterval;

        // 배치
        public float MonsterXSpacing => _monsterXSpacing;

        // 사인파 스폰 (새로운 방식)
        public float SineAmplitude => _sineAmplitude;
        /// <summary>
        /// 사인파 주기 (몬스터 N마리당 1주기)를 라디안 주파수로 변환
        /// </summary>
        public float SineFrequencyRadians => (Mathf.PI * 2f) / _sinePeriodPerMonsters;
        public float SinePeriodPerMonsters => _sinePeriodPerMonsters;
        public float YConvergenceSpeed => _yConvergenceSpeed;
        public float CombatYOffsetRange => _combatYOffsetRange;

        // 맵 경계
        public float MapBoundsYMin => _mapBoundsYMin;
        public float MapBoundsYMax => _mapBoundsYMax;
        public float BoundsMargin => _boundsMargin;

        /// <summary>
        /// 스폰용 유효 Y 경계 (마진 적용)
        /// </summary>
        public float EffectiveYMin => _mapBoundsYMin + _boundsMargin;
        public float EffectiveYMax => _mapBoundsYMax - _boundsMargin;

        /// <summary>
        /// Y 위치를 맵 경계 내로 클램핑 (단순 클램핑)
        /// </summary>
        public float ClampY(float y)
        {
            return Mathf.Clamp(y, EffectiveYMin, EffectiveYMax);
        }

        /// <summary>
        /// Y 위치를 맵 경계 내로 클램핑 (스폰 시 사용)
        /// </summary>
        public float ClampSpawnY(float y)
        {
            return Mathf.Clamp(y, EffectiveYMin, EffectiveYMax);
        }

        // 이동
        public float MonsterMoveSpeed => _monsterMoveSpeed;
        public float BackgroundScrollSpeed => _backgroundScrollSpeed;
        public float PlayerAdvanceDistance => _playerAdvanceDistance;
        public float PlayerMoveSpeed => _playerMoveSpeed;
        public float PlayerReturnSpeedMultiplier => _playerReturnSpeedMultiplier;

        // 전투
        public float AttackRange => _attackRange;
        public float DeathToSpawnDelay => _deathToSpawnDelay;

        // 프리팹
        public IReadOnlyList<MonsterVisualEntry> MonsterVisuals => _monsterVisuals;

        /// <summary>
        /// 가중치 기반 랜덤 몬스터 프리팹 반환
        /// </summary>
        public MonsterUnitView GetRandomMonsterPrefab()
        {
            if (_monsterVisuals == null || _monsterVisuals.Count == 0)
                return null;

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
    /// 웨이브별 설정 - 각 웨이브마다 다른 몬스터 구성을 정의
    /// 
    /// 설정 방법:
    /// - Wave Configs 리스트의 Element 0 = 1웨이브, Element 1 = 2웨이브...
    /// - 리스트에 없는 웨이브는 기본값(Default~) 사용
    /// </summary>
    [Serializable]
    public class WaveConfig
    {
        [Header("=== 몬스터 종류별 스폰 개수 ===")]
        [Tooltip("이 웨이브에서 스폰할 일반 몬스터 수")]
        [Range(0, 50)]
        public int NormalCount = 10;

        [Tooltip("이 웨이브에서 스폰할 엘리트 몬스터 수")]
        [Range(0, 20)]
        public int EliteCount = 0;

        [Tooltip("이 웨이브에서 스폰할 보스 몬스터 수")]
        [Range(0, 5)]
        public int BossCount = 0;

        [Header("=== 스폰 설정 ===")]
        [Tooltip("동시에 화면에 존재할 수 있는 최대 몬스터 수")]
        [Range(1, 15)]
        public int MaxMonstersOnScreen = 5;

        [Tooltip("몬스터 스폰 간격 (초)")]
        [Range(0.1f, 5f)]
        public float SpawnInterval = 0.5f;

        [Header("=== 메모 ===")]
        [Tooltip("이 웨이브의 설명/메모 (에디터용)")]
        [TextArea(1, 3)]
        public string Description;

        /// <summary>
        /// 총 스폰할 몬스터 수 (Normal + Elite + Boss)
        /// </summary>
        public int TotalMonstersToSpawn => NormalCount + EliteCount + BossCount;

        /// <summary>
        /// 이 웨이브에 보스가 있는지 여부
        /// </summary>
        public bool HasBoss => BossCount > 0;

        /// <summary>
        /// 이 웨이브에 엘리트가 있는지 여부
        /// </summary>
        public bool HasElite => EliteCount > 0;
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
