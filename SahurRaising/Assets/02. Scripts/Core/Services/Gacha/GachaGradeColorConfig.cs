using SahurRaising.Core;
using System.Collections.Generic;
using UnityEngine;

namespace SahurRaising
{
    [CreateAssetMenu(fileName = "GachaGradeColorConfig", menuName = "SahurRaising/Data/GachaGradeColorConfig")]
    public class GachaGradeColorConfig : ScriptableObject
    {
        [Header("가챠 타입별 등급 색상 및 타입 아이콘")]
        [SerializeField] private List<GachaTypeColorConfig> _gachaTypeConfigs = new List<GachaTypeColorConfig>();

        private Dictionary<GachaType, GachaTypeColorConfig> _gachaTypeConfigDict;

        private void OnEnable()
        {
            BuildDictionaries();
        }

        private void BuildDictionaries()
        {
            // 가챠 타입별 설정 딕셔너리 빌드
            _gachaTypeConfigDict = new Dictionary<GachaType, GachaTypeColorConfig>();
            foreach (var config in _gachaTypeConfigs)
            {
                if (!_gachaTypeConfigDict.ContainsKey(config.Type))
                {
                    config.BuildDictionary();
                    _gachaTypeConfigDict[config.Type] = config;
                }
            }
        }

        /// <summary>
        /// 가챠 타입과 등급 키에 따른 색상을 반환합니다.
        /// </summary>
        public Color GetColorForGrade(GachaType gachaType, string gradeKey)
        {
            if (_gachaTypeConfigDict == null)
                BuildDictionaries();

            if (_gachaTypeConfigDict != null && _gachaTypeConfigDict.TryGetValue(gachaType, out var config))
            {
                return config.GetColorForGrade(gradeKey);
            }

            // 기본 색상 반환
            //return GetDefaultColorForGrade(gradeKey);
            return Color.white;
        }

        /// <summary>
        /// 가챠 타입과 타입 키에 따른 아이콘을 반환합니다.
        /// </summary>
        public Sprite GetTypeIcon(GachaType gachaType, string typeKey)
        {
            if (_gachaTypeConfigDict == null)
                BuildDictionaries();

            if (_gachaTypeConfigDict != null && _gachaTypeConfigDict.TryGetValue(gachaType, out var config))
            {
                return config.GetTypeIcon(typeKey);
            }

            return null;
        }

        // 에디터에서 초기화용 (Context Menu)
        [ContextMenu("Initialize All Gacha Grade Colors")]
        private void InitializeAllGachaGradeColors()
        {
            _gachaTypeConfigs = new List<GachaTypeColorConfig>
            {
                // Equipment 가챠 설정
                new GachaTypeColorConfig
                {
                    Type = GachaType.Equipment,
                    GradeColors = new List<GradeColorEntry>
                    {
                        new GradeColorEntry { GradeKey = "F", Color = new Color(0.5f, 0.5f, 0.5f) }, // 회색
                        new GradeColorEntry { GradeKey = "D3", Color = new Color(0.4f, 0.4f, 0.6f) }, // 어두운 파란색
                        new GradeColorEntry { GradeKey = "D2", Color = new Color(0.4f, 0.4f, 0.6f) },
                        new GradeColorEntry { GradeKey = "D1", Color = new Color(0.4f, 0.4f, 0.6f) },
                        new GradeColorEntry { GradeKey = "C3", Color = new Color(0.2f, 0.6f, 0.2f) }, // 초록색
                        new GradeColorEntry { GradeKey = "C2", Color = new Color(0.2f, 0.6f, 0.2f) },
                        new GradeColorEntry { GradeKey = "C1", Color = new Color(0.2f, 0.6f, 0.2f) },
                        new GradeColorEntry { GradeKey = "B3", Color = new Color(0.2f, 0.4f, 0.8f) }, // 파란색
                        new GradeColorEntry { GradeKey = "B2", Color = new Color(0.2f, 0.4f, 0.8f) },
                        new GradeColorEntry { GradeKey = "B1", Color = new Color(0.2f, 0.4f, 0.8f) },
                        new GradeColorEntry { GradeKey = "A3", Color = new Color(0.8f, 0.2f, 0.8f) }, // 보라색
                        new GradeColorEntry { GradeKey = "A2", Color = new Color(0.8f, 0.2f, 0.8f) },
                        new GradeColorEntry { GradeKey = "A1", Color = new Color(0.8f, 0.2f, 0.8f) },
                        new GradeColorEntry { GradeKey = "S3", Color = new Color(1f, 0.84f, 0f) }, // 금색
                        new GradeColorEntry { GradeKey = "S2", Color = new Color(1f, 0.84f, 0f) },
                        new GradeColorEntry { GradeKey = "S1", Color = new Color(1f, 0.84f, 0f) },
                        new GradeColorEntry { GradeKey = "SS3", Color = new Color(1f, 0.5f, 0f) }, // 주황색
                        new GradeColorEntry { GradeKey = "SS2", Color = new Color(1f, 0.5f, 0f) },
                        new GradeColorEntry { GradeKey = "SS1", Color = new Color(1f, 0.5f, 0f) },
                        new GradeColorEntry { GradeKey = "SSS3", Color = new Color(1f, 0f, 0f) } // 빨간색
                    },
                    TypeIcons = new List<TypeIconEntry>
                    {
                        // EquipmentType enum의 모든 값에 대한 엔트리 생성 (아이콘은 나중에 할당)
                        new TypeIconEntry { TypeKey = "Processor", Icon = null },
                        new TypeIconEntry { TypeKey = "Wheel", Icon = null },
                        new TypeIconEntry { TypeKey = "Battery", Icon = null },
                        new TypeIconEntry { TypeKey = "Antenna", Icon = null },
                        new TypeIconEntry { TypeKey = "Memory", Icon = null },
                        new TypeIconEntry { TypeKey = "RobotArm", Icon = null }
                    }
                },
                // Drone 가챠 설정
                new GachaTypeColorConfig
                {
                    Type = GachaType.Drone,
                    GradeColors = new List<GradeColorEntry>
                    {
                        // 드론 ID 기반 색상 (예시 - 실제 드론 ID에 맞게 조정 필요)
                        new GradeColorEntry { GradeKey = "1", Color = new Color(0.5f, 0.5f, 0.5f) }, // 회색
                        new GradeColorEntry { GradeKey = "2", Color = new Color(0.5f, 0.5f, 0.5f) },
                        new GradeColorEntry { GradeKey = "3", Color = new Color(0.5f, 0.5f, 0.5f) },
                        new GradeColorEntry { GradeKey = "4", Color = new Color(0.2f, 0.6f, 0.2f) }, // 초록색
                        new GradeColorEntry { GradeKey = "5", Color = new Color(0.2f, 0.6f, 0.2f) },
                        new GradeColorEntry { GradeKey = "6", Color = new Color(0.2f, 0.6f, 0.2f) },
                        new GradeColorEntry { GradeKey = "7", Color = new Color(0.2f, 0.4f, 0.8f) }, // 파란색
                        new GradeColorEntry { GradeKey = "8", Color = new Color(0.2f, 0.4f, 0.8f) },
                        new GradeColorEntry { GradeKey = "9", Color = new Color(1f, 0.84f, 0f) }, // 금색
                        new GradeColorEntry { GradeKey = "10A", Color = new Color(1f, 0.84f, 0f) },
                        new GradeColorEntry { GradeKey = "10B", Color = new Color(1f, 0.84f, 0f) }
                    },
                    TypeIcons = new List<TypeIconEntry>
                    {
                        // 드론 타입이 생기면 여기에 추가
                        // 현재는 비어있음
                    }
                }
            };

            BuildDictionaries();

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }


        [System.Serializable]
        public class GradeColorEntry
        {
            public string GradeKey;
            public Color Color = Color.white;
        }

        [System.Serializable]
        public class TypeIconEntry
        {
            public string TypeKey;
            public Sprite Icon;
        }

        [System.Serializable]
        public class GachaTypeColorConfig
        {
            public GachaType Type;
            [SerializeField] private List<GradeColorEntry> _gradeColors = new List<GradeColorEntry>();
            [SerializeField] private List<TypeIconEntry> _typeIcons = new List<TypeIconEntry>();

            private Dictionary<string, Color> _gradeColorDict;
            private Dictionary<string, Sprite> _typeIconDict;

            public List<GradeColorEntry> GradeColors
            {
                get => _gradeColors;
                set => _gradeColors = value ?? new List<GradeColorEntry>();
            }

            public List<TypeIconEntry> TypeIcons
            {
                get => _typeIcons;
                set => _typeIcons = value ?? new List<TypeIconEntry>();
            }

            public void BuildDictionary()
            {
                _gradeColorDict = new Dictionary<string, Color>();
                foreach (var entry in _gradeColors)
                {
                    if (!_gradeColorDict.ContainsKey(entry.GradeKey))
                    {
                        _gradeColorDict[entry.GradeKey] = entry.Color;
                    }
                }

                _typeIconDict = new Dictionary<string, Sprite>();
                foreach (var entry in _typeIcons)
                {
                    if (!_typeIconDict.ContainsKey(entry.TypeKey))
                    {
                        _typeIconDict[entry.TypeKey] = entry.Icon;
                    }
                }
            }

            public Color GetColorForGrade(string gradeKey)
            {
                if (_gradeColorDict == null)
                    BuildDictionary();

                if (_gradeColorDict != null && _gradeColorDict.TryGetValue(gradeKey, out var color))
                {
                    return color;
                }

                return Color.white;
            }

            public Sprite GetTypeIcon(string typeKey)
            {
                if (_typeIconDict == null)
                    BuildDictionary();

                if (_typeIconDict != null && _typeIconDict.TryGetValue(typeKey, out var icon))
                {
                    return icon;
                }

                return null;
            }
        }
    }
}
