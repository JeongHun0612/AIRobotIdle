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
    /// - 맵 경계 자동 계산 (배경 기반)
    /// - Wave 포메이션 패턴 시스템
    /// - Y축 수렴 시스템 (전투 돌입 전 Y범위 제한)
    /// </summary>
    [CreateAssetMenu(fileName = "CombatSettings", menuName = "SahurRaising/Combat/CombatSettings")]
    public class CombatSettings : ScriptableObject
    {
        [Header("=== 맵 경계 설정 (자동 계산) ===")]
        [Tooltip("배경 SpriteRenderer를 참조하면 자동으로 맵 경계를 계산합니다.\n" +
                 "런타임에 동적으로 설정되거나, 에디터에서 직접 참조할 수 있습니다.")]
        [System.NonSerialized] private SpriteRenderer _backgroundRenderer;

        [Tooltip("배경 기반 자동 계산 사용 여부\n" +
                 "true: 배경 bounds에서 자동 계산\n" +
                 "false: 수동으로 MapBoundsYMin/Max 사용")]
        [SerializeField] private bool _useAutoMapBounds = true;

        [Tooltip("자동 계산 시 Y축 마진 (배경 경계에서 안쪽으로)")]
        [SerializeField, Range(0f, 20f)] private float _autoMarginY = 2f;

        [Tooltip("수동 모드일 때 사용할 Y 최소값")]
        [SerializeField] private float _manualMapBoundsYMin = -15f;

        [Tooltip("수동 모드일 때 사용할 Y 최대값")]
        [SerializeField] private float _manualMapBoundsYMax = 15f;

        [Header("=== 전투 Y축 수렴 설정 ===")]
        [Tooltip("전투 상태 돌입 전 몬스터가 수렴해야 할 Y 최소값\n" +
                 "몬스터는 플레이어와 전투 상태에 돌입하기 전까지 이 범위 내로 이동합니다.")]
        [SerializeField, Range(-10f, 10f)] private float _combatYMin = 0f;

        [Tooltip("전투 상태 돌입 전 몬스터가 수렴해야 할 Y 최대값\n" +
                 "몬스터는 플레이어와 전투 상태에 돌입하기 전까지 이 범위 내로 이동합니다.")]
        [SerializeField, Range(-5f, 15f)] private float _combatYMax = 5f;

        [Tooltip("전투 Y축 수렴 기본값 (초기 목표 Y 위치)")]
        [SerializeField, Range(-5f, 10f)] private float _combatYDefault = 3f;

        [Tooltip("Y축 수렴 속도 배율 (1.0 = 기본 이동속도와 동일)")]
        [SerializeField, Range(0.1f, 3f)] private float _yConvergeSpeedMultiplier = 1f;

        [Tooltip("Y축 수렴 기능 사용 여부\n" +
                 "true: 몬스터가 전투 돌입 전 CombatY 범위로 이동함\n" +
                 "false: 몬스터가 스폰된 Y 높이를 유지하며 직선 이동함")]
        [SerializeField] private bool _enableYConvergence = true;

        [Header("=== 웨이브 설정 (패턴 기반) ===")]
        [Tooltip("각 웨이브별 패턴 수 및 제한 설정. 비어있으면 기본값 사용.\n" +
                 "몬스터 수와 위치는 각 패턴에서 결정됩니다.")]
        [SerializeField] private List<WaveConfig> _waveConfigs = new();

        [Header("=== 기본 웨이브 설정 (웨이브 설정이 없을 때 사용) ===")]
        [Tooltip("웨이브당 실행할 패턴 수 (기본값)\n" +
                 "각 패턴에 정의된 몬스터들이 순차적으로 스폰됩니다.")]
        [SerializeField, Range(1, 20)] private int _defaultPatternsPerWave = 3;

        [Tooltip("동시에 화면에 존재할 수 있는 최대 몬스터 수 (기본값)")]
        [SerializeField, Range(1, 10)] private int _defaultMaxMonstersOnScreen = 5;

        [Tooltip("스테이지당 웨이브 수")]
        [SerializeField, Range(1, 10)] private int _wavesPerStage = 4;

        [Header("=== 스폰 패턴 시스템 ===")]
        [Tooltip("사용 가능한 스폰 패턴들 (랜덤 선택)")]
        [SerializeField] private List<SpawnPattern> _spawnPatterns = new();

        [Tooltip("패턴이 없을 때 사용할 기본 스폰 방식\n" +
                 "true: 단일 몬스터씩 순차 스폰\n" +
                 "false: 스폰 실패 (에러 로그)")]
        [SerializeField] private bool _useFallbackSpawning = true;

        [Header("=== 몬스터 배치 설정 ===")]
        [Tooltip("몬스터 간 최소 X축 간격 (겹침 방지)")]
        [SerializeField, Range(0.5f, 5f)] private float _monsterXSpacing = 1.5f;

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

        [Header("=== 디버그/테스트 설정 ===")]
        [Tooltip("몬스터 스폰 활성화 여부 (테스트용)\n" +
                 "false: 몬스터가 스폰되지 않음 (배경만 이동)")]
        [SerializeField] private bool _isMonsterSpawnEnabled = true;

        // ===== 런타임 캐시 =====
        private float _cachedMapYMin;
        private float _cachedMapYMax;
        private bool _isBoundsCached = false;

        // ===== Properties =====

        // 스테이지/웨이브
        public int WavesPerStage => _wavesPerStage;
        public IReadOnlyList<WaveConfig> WaveConfigs => _waveConfigs;

        // 기본값 (웨이브 설정이 없을 때)
        public int DefaultPatternsPerWave => _defaultPatternsPerWave;
        public int DefaultMaxMonstersOnScreen => _defaultMaxMonstersOnScreen;

        // 패턴 시스템
        public IReadOnlyList<SpawnPattern> SpawnPatterns => _spawnPatterns;
        public bool HasPatterns => _spawnPatterns != null && _spawnPatterns.Count > 0;
        public bool UseFallbackSpawning => _useFallbackSpawning;

        // 배치
        public float MonsterXSpacing => _monsterXSpacing;

        // Y축 수렴 설정
        public float CombatYMin => _combatYMin;
        public float CombatYMax => _combatYMax;
        public float CombatYDefault => _combatYDefault;
        public float YConvergeSpeedMultiplier => _yConvergeSpeedMultiplier;
        public bool EnableYConvergence => _enableYConvergence;

        // 이동
        public float MonsterMoveSpeed => _monsterMoveSpeed;
        public float BackgroundScrollSpeed => _backgroundScrollSpeed;
        public float PlayerAdvanceDistance => _playerAdvanceDistance;
        public float PlayerMoveSpeed => _playerMoveSpeed;
        public float PlayerReturnSpeedMultiplier => _playerReturnSpeedMultiplier;

        // 전투
        public float AttackRange => _attackRange;
        public float DeathToSpawnDelay => _deathToSpawnDelay;
        public bool IsMonsterSpawnEnabled => _isMonsterSpawnEnabled;

        // 프리팹
        public IReadOnlyList<MonsterVisualEntry> MonsterVisuals => _monsterVisuals;

        #region 맵 경계 시스템

        /// <summary>
        /// 배경 렌더러 설정 (런타임에 동적으로 설정 가능)
        /// </summary>
        public void SetBackgroundRenderer(SpriteRenderer renderer)
        {
            _backgroundRenderer = renderer;
            InvalidateBoundsCache();
        }

        /// <summary>
        /// 경계 캐시 무효화 (배경이 변경될 때 호출)
        /// </summary>
        public void InvalidateBoundsCache()
        {
            _isBoundsCached = false;
        }

        /// <summary>
        /// 맵 Y 경계 계산 및 캐싱
        /// </summary>
        private void EnsureBoundsCalculated()
        {
            if (_isBoundsCached) return;

            if (_useAutoMapBounds && _backgroundRenderer != null)
            {
                // 배경 Sprite의 Bounds에서 자동 계산
                var bounds = _backgroundRenderer.bounds;
                _cachedMapYMin = bounds.min.y + _autoMarginY;
                _cachedMapYMax = bounds.max.y - _autoMarginY;
            }
            else
            {
                // 수동 설정값 사용
                _cachedMapYMin = _manualMapBoundsYMin;
                _cachedMapYMax = _manualMapBoundsYMax;
            }

            _isBoundsCached = true;
        }

        /// <summary>
        /// 맵 Y 최소값 (스폰/이동 가능 영역)
        /// </summary>
        public float MapBoundsYMin
        {
            get
            {
                EnsureBoundsCalculated();
                return _cachedMapYMin;
            }
        }

        /// <summary>
        /// 맵 Y 최대값 (스폰/이동 가능 영역)
        /// </summary>
        public float MapBoundsYMax
        {
            get
            {
                EnsureBoundsCalculated();
                return _cachedMapYMax;
            }
        }

        /// <summary>
        /// 맵 Y 중심점
        /// </summary>
        public float MapCenterY => (MapBoundsYMin + MapBoundsYMax) / 2f;

        /// <summary>
        /// 맵 Y 범위 (높이의 절반)
        /// </summary>
        public float MapYHalfRange => (MapBoundsYMax - MapBoundsYMin) / 2f;

        /// <summary>
        /// Y 위치를 맵 경계 내로 클램핑
        /// </summary>
        public float ClampY(float y)
        {
            return Mathf.Clamp(y, MapBoundsYMin, MapBoundsYMax);
        }

        /// <summary>
        /// Y 위치를 전투 가능 범위 내로 클램핑
        /// </summary>
        public float ClampToCombatY(float y)
        {
            return Mathf.Clamp(y, _combatYMin, _combatYMax);
        }

        /// <summary>
        /// Y 위치가 전투 가능 범위 내에 있는지 확인
        /// </summary>
        public bool IsWithinCombatYRange(float y)
        {
            return y >= _combatYMin && y <= _combatYMax;
        }

        #endregion

        #region 웨이브 설정

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
                PatternsPerWave = _defaultPatternsPerWave,
                MaxMonstersOnScreen = _defaultMaxMonstersOnScreen
            };
        }

        /// <summary>
        /// 해당 웨이브에서 실행할 패턴 수 반환
        /// </summary>
        public int GetPatternsPerWave(int waveIndex) => GetWaveConfig(waveIndex).PatternsPerWave;

        /// <summary>
        /// 해당 웨이브에서 동시에 화면에 있을 수 있는 최대 몬스터 수 반환
        /// </summary>
        public int GetMaxMonstersOnScreen(int waveIndex) => GetWaveConfig(waveIndex).MaxMonstersOnScreen;

        #endregion

        #region 패턴 선택

        /// <summary>
        /// 가중치 기반 랜덤 패턴 선택
        /// </summary>
        public SpawnPattern GetRandomPattern()
        {
            if (_spawnPatterns == null || _spawnPatterns.Count == 0)
                return null;

            float totalWeight = 0f;
            foreach (var pattern in _spawnPatterns)
            {
                if (pattern != null)
                    totalWeight += pattern.SelectionWeight;
            }

            if (totalWeight <= 0f)
                return _spawnPatterns[0];

            float randomValue = UnityEngine.Random.Range(0f, totalWeight);
            float cumulative = 0f;

            foreach (var pattern in _spawnPatterns)
            {
                if (pattern == null) continue;
                cumulative += pattern.SelectionWeight;
                if (randomValue <= cumulative)
                    return pattern;
            }

            return _spawnPatterns[0];
        }

        #endregion

        #region 몬스터 프리팹

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

        #endregion

        #region 에디터 지원

        private void OnValidate()
        {
            // 에디터에서 값 변경 시 캐시 무효화
            InvalidateBoundsCache();

            // 변경 사항 체크 (불필요한 Dirty 방지)
            bool isChanged = false;

            // 전투 Y 범위 검증
            if (_combatYMin > _combatYMax)
            {
                _combatYMin = _combatYMax;
                isChanged = true;
            }

            // Default값 클램핑
            float clampedDefault = Mathf.Clamp(_combatYDefault, _combatYMin, _combatYMax);
            if (Mathf.Abs(_combatYDefault - clampedDefault) > 0.0001f)
            {
                _combatYDefault = clampedDefault;
                isChanged = true;
            }

#if UNITY_EDITOR
            // 값이 변경된 경우에만 Dirty 처리 (수동 저장 유도)
            if (isChanged)
            {
                UnityEditor.EditorUtility.SetDirty(this);
            }
#endif
        }

        #endregion
    }

    /// <summary>
    /// 웨이브별 설정 - 패턴 기반 시스템
    /// 
    /// 웨이브는 "몇 개 패턴 실행"으로 결정됩니다.
    /// 몬스터 수, 종류, 위치는 각 패턴(SpawnPattern)에서 관리합니다.
    /// 
    /// 설정 방법:
    /// - Wave Configs 리스트의 Element 0 = 1웨이브, Element 1 = 2웨이브...
    /// - 리스트에 없는 웨이브는 기본값(Default~) 사용
    /// </summary>
    [Serializable]
    public class WaveConfig
    {
        [Header("=== 웨이브 패턴 설정 ===")]
        [Tooltip("이 웨이브에서 실행할 패턴 수\n" +
                 "각 패턴에 정의된 몬스터들이 순차적으로 스폰됩니다.")]
        [Range(1, 30)]
        public int PatternsPerWave = 3;

        [Tooltip("동시에 화면에 존재할 수 있는 최대 몬스터 수")]
        [Range(1, 15)]
        public int MaxMonstersOnScreen = 5;

        [Header("=== 메모 ===")]
        [Tooltip("이 웨이브의 설명/메모 (에디터용)")]
        [TextArea(1, 3)]
        public string Description;
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
