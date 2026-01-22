using System;
using System.Collections.Generic;
using UnityEngine;

namespace SahurRaising.GamePlay
{
    /// <summary>
    /// 몬스터 스폰 포메이션 패턴을 정의하는 ScriptableObject
    /// 
    /// 레오슬 스타일의 Wave 패턴 시스템:
    /// - 미리 정의된 위치 offsets에 따라 몬스터 스폰
    /// - 순차 등장 딜레이로 연출 효과
    /// - 다양한 포메이션 (일렬, V자, 대각선 등)
    /// </summary>
    [CreateAssetMenu(fileName = "SpawnPattern", menuName = "SahurRaising/Combat/SpawnPattern")]
    public class SpawnPattern : ScriptableObject
    {
        [Header("=== 패턴 정보 ===")]
        [Tooltip("패턴 이름 (에디터용)")]
        [SerializeField] private string _patternName = "New Pattern";

        [Tooltip("패턴 설명")]
        [TextArea(2, 4)]
        [SerializeField] private string _description;

        [Header("=== 스폰 위치 ===")]
        [Tooltip("각 몬스터의 스폰 위치 오프셋 (스폰 포인트 기준)\n" +
                 "X: 시간/순서상 오프셋 (0=첫번째, 1=그 다음...)\n" +
                 "Y: 상하 오프셋 (-1 ~ 1, 맵 높이에 비례)")]
        [SerializeField] private List<SpawnSlot> _spawnSlots = new();

        [Header("=== 타이밍 설정 ===")]
        [Tooltip("각 몬스터 간 등장 딜레이 (초)")]
        [SerializeField, Range(0f, 1f)] private float _spawnDelay = 0.15f;

        [Tooltip("이 패턴의 모든 몬스터가 스폰된 후, 다음 패턴까지 대기 시간 (초)")]
        [SerializeField, Range(0f, 3f)] private float _patternCooldown = 0.5f;

        [Header("=== 선택 가중치 ===")]
        [Tooltip("이 패턴이 선택될 확률 가중치 (높을수록 자주 선택)")]
        [SerializeField, Range(0.1f, 10f)] private float _selectionWeight = 1f;

        // Properties
        public string PatternName => _patternName;
        public string Description => _description;
        public IReadOnlyList<SpawnSlot> SpawnSlots => _spawnSlots;
        public int MonsterCount => _spawnSlots.Count;
        public float SpawnDelay => _spawnDelay;
        public float PatternCooldown => _patternCooldown;
        public float SelectionWeight => _selectionWeight;

        /// <summary>
        /// 특정 인덱스의 스폰 위치 반환
        /// </summary>
        /// <param name="index">슬롯 인덱스</param>
        /// <param name="basePosition">스폰 포인트 위치</param>
        /// <param name="mapYMin">맵 Y 최소값</param>
        /// <param name="mapYMax">맵 Y 최대값</param>
        /// <param name="xSpacing">X축 간격</param>
        public Vector3 GetSpawnPosition(int index, Vector3 basePosition, float mapYMin, float mapYMax, float xSpacing)
        {
            if (index < 0 || index >= _spawnSlots.Count)
            {
                return basePosition;
            }

            var slot = _spawnSlots[index];
            
            // X 오프셋: 순서 * 간격
            float xOffset = slot.OrderOffset * xSpacing;
            
            // Y 오프셋: -1~1 범위를 맵의 실제 Y 범위로 매핑
            float mapYCenter = (mapYMin + mapYMax) / 2f;
            float mapYRange = (mapYMax - mapYMin) / 2f;
            float yOffset = slot.YNormalized * mapYRange;
            
            return new Vector3(
                basePosition.x + xOffset,
                mapYCenter + yOffset,
                basePosition.z
            );
        }

        /// <summary>
        /// 에디터 미리보기용 - 패턴 시각화
        /// </summary>
        public void OnValidate()
        {
            if (string.IsNullOrEmpty(_patternName))
            {
                _patternName = name;
            }
        }
    }

    /// <summary>
    /// 개별 스폰 슬롯 정보
    /// </summary>
    [Serializable]
    public class SpawnSlot
    {
        [Tooltip("등장 순서 오프셋 (0=맨 처음, 1=그 다음 위치...)")]
        [Range(0f, 5f)]
        public float OrderOffset;

        [Tooltip("Y축 위치 (-1=맵 하단, 0=중앙, 1=맵 상단)")]
        [Range(-1f, 1f)]
        public float YNormalized;

        [Tooltip("이 슬롯의 몬스터 종류 (기본=Normal)")]
        public Core.MonsterKind MonsterKind = Core.MonsterKind.Normal;

        [Tooltip("이 슬롯 설명 (에디터용)")]
        public string Note;

        public SpawnSlot()
        {
            OrderOffset = 0f;
            YNormalized = 0f;
            MonsterKind = Core.MonsterKind.Normal;
        }

        public SpawnSlot(float orderOffset, float yNormalized, Core.MonsterKind kind = Core.MonsterKind.Normal)
        {
            OrderOffset = orderOffset;
            YNormalized = yNormalized;
            MonsterKind = kind;
        }
    }
}
