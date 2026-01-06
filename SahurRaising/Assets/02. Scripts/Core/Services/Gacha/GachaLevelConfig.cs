using System.Collections.Generic;
using UnityEngine;

namespace SahurRaising.Core
{
    [CreateAssetMenu(fileName = "GachaLevelConfig", menuName = "SahurRaising/Data/GachaLevelConfig")]
    public class GachaLevelConfig : ScriptableObject
    {
        [System.Serializable]
        public class LevelThreshold
        {
            public int Level;
            public int RequiredCount;  // 누적 뽑기 개수
        }

        [System.Serializable]
        public class GachaLevelData
        {
            public GachaType Type;
            public List<LevelThreshold> Levels = new();
        }

        [Header("가챠 타입별 레벨업 조건")]
        [SerializeField] private List<GachaLevelData> _gachaLevelDataList = new();

        // 런타임 인덱스 (OnEnable에서 빌드)
        private Dictionary<GachaType, List<LevelThreshold>> _levelDataIndex;

        private void OnEnable()
        {
            BuildIndex();
        }

        private void BuildIndex()
        {
            _levelDataIndex = new Dictionary<GachaType, List<LevelThreshold>>();

            if (_gachaLevelDataList == null)
                return;

            foreach (var data in _gachaLevelDataList)
            {
                if (data != null && data.Levels != null)
                {
                    _levelDataIndex[data.Type] = data.Levels;
                }
            }
        }

        /// <summary>
        /// 장비 뽑기의 최대 레벨을 반환합니다
        /// </summary>
        public int GetMaxLevel(GachaType type)
        {
            if (!_levelDataIndex.TryGetValue(type, out var thresholds) || thresholds == null || thresholds.Count == 0)
                return 1;

            int maxLevel = 1;
            foreach (var threshold in thresholds)
            {
                if (threshold.Level > maxLevel)
                {
                    maxLevel = threshold.Level;
                }
            }

            return maxLevel;
        }

        /// <summary>
        /// 현재 누적 개수에 따른 가챠 레벨을 반환합니다
        /// </summary>
        public int GetLevel(GachaType type, int totalCount)
        {
            if (!_levelDataIndex.TryGetValue(type, out var thresholds) || thresholds == null || thresholds.Count == 0)
                return 1;

            // 역순으로 검사하여 현재 개수보다 큰 최소 레벨을 찾음
            for (int i = thresholds.Count - 1; i >= 0; i--)
            {
                if (totalCount >= thresholds[i].RequiredCount)
                {
                    return thresholds[i].Level;
                }
            }

            return 1; // 기본값
        }

        /// <summary>
        /// 특정 레벨에 필요한 누적 뽑기 개수를 반환합니다
        /// </summary>
        public int GetRequiredCountForLevel(GachaType type, int level)
        {
            if (!_levelDataIndex.TryGetValue(type, out var thresholds) || thresholds == null)
                return 0;

            foreach (var threshold in thresholds)
            {
                if (threshold.Level == level)
                {
                    return threshold.RequiredCount;
                }
            }

            // 레벨이 없으면 마지막 레벨의 필요 개수 반환
            if (thresholds.Count > 0)
            {
                return thresholds[thresholds.Count - 1].RequiredCount;
            }

            return 0;
        }

        /// <summary>
        /// 다음 레벨업까지 필요한 뽑기 개수를 반환합니다
        /// </summary>
        public int GetRemainingCountToNextLevel(GachaType type, int totalCount)
        {
            var currentLevel = GetLevel(type, totalCount);
            var maxLevel = GetMaxLevel(type);

            if (currentLevel >= maxLevel)
                return 0; // 이미 최대 레벨

            if (!_levelDataIndex.TryGetValue(type, out var thresholds) || thresholds == null)
                return 0;

            // 다음 레벨의 필요 개수 찾기
            foreach (var threshold in thresholds)
            {
                if (threshold.Level == currentLevel + 1)
                {
                    return Mathf.Max(0, threshold.RequiredCount - totalCount);
                }
            }

            return 0;
        }

        // 에디터에서 초기화용 (Context Menu)
        [ContextMenu("Initialize All Gacha Levels")]
        private void InitializeAllGachaLevels()
        {
            _gachaLevelDataList = new List<GachaLevelData>
            {
                new GachaLevelData
                {
                    Type = GachaType.Equipment,
                    Levels = new List<LevelThreshold>
                    {
                        new LevelThreshold { Level = 1, RequiredCount = 0 },
                        new LevelThreshold { Level = 2, RequiredCount = 30 },
                        new LevelThreshold { Level = 3, RequiredCount = 100 },
                        new LevelThreshold { Level = 4, RequiredCount = 250 },
                        new LevelThreshold { Level = 5, RequiredCount = 450 },
                        new LevelThreshold { Level = 6, RequiredCount = 800 },
                        new LevelThreshold { Level = 7, RequiredCount = 1500 },
                        new LevelThreshold { Level = 8, RequiredCount = 3500 },
                        new LevelThreshold { Level = 9, RequiredCount = 10000 }
                    }
                },
                new GachaLevelData
                {
                    Type = GachaType.Drone,
                    Levels = new List<LevelThreshold>
                    {
                        new LevelThreshold { Level = 1, RequiredCount = 0 },
                        new LevelThreshold { Level = 2, RequiredCount = 30 },
                        new LevelThreshold { Level = 3, RequiredCount = 100 },
                        new LevelThreshold { Level = 4, RequiredCount = 200 },
                        new LevelThreshold { Level = 5, RequiredCount = 350 },
                        new LevelThreshold { Level = 6, RequiredCount = 600 },
                        new LevelThreshold { Level = 7, RequiredCount = 1000 },
                        new LevelThreshold { Level = 8, RequiredCount = 2000 },
                        new LevelThreshold { Level = 9, RequiredCount = 5000 }
                    }
                }
            };

            BuildIndex();
        }
    }
}