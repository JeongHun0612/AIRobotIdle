using System.Collections.Generic;
using UnityEngine;

namespace SahurRaising.Core
{
    /// <summary>
    /// 아이템 타입/등급에 따른 UI 시각 요소(색상/아이콘) 매핑 설정
    /// </summary>
    [CreateAssetMenu(fileName = "ItemVisualConfig", menuName = "SahurRaising/Config/ItemVisualConfig")]
    public class ItemVisualConfig : ScriptableObject
    {
        [Header("아이템 타입별 등급 색상 및 타입 아이콘")]
        // 기존 에셋(YAML) 호환을 위해 필드명은 유지합니다.
        [SerializeField] private List<ItemTypeVisualConfig> _gachaTypeConfigs = new List<ItemTypeVisualConfig>();

        private Dictionary<GachaType, ItemTypeVisualConfig> _gachaTypeConfigDict;

        private void OnEnable()
        {
            BuildDictionaries();
        }

        private void BuildDictionaries()
        {
            // 가챠 타입별 설정 딕셔너리 빌드
            _gachaTypeConfigDict = new Dictionary<GachaType, ItemTypeVisualConfig>();
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
        /// 아이템 타입과 등급 키에 따른 색상을 반환합니다.
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
        /// 아이템 타입과 타입 키에 따른 아이콘을 반환합니다.
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
        [ContextMenu("Initialize All Item Visual Configs")]
        private void InitializeAllItemVisualConfigs()
        {
            // 기존 아이콘 값들을 보존하기 위한 딕셔너리
            Dictionary<GachaType, Dictionary<string, Sprite>> existingIcons = new Dictionary<GachaType, Dictionary<string, Sprite>>();

            // 기존 설정에서 아이콘 값들을 추출
            foreach (var existingConfig in _gachaTypeConfigs)
            {
                if (!existingIcons.ContainsKey(existingConfig.Type))
                {
                    existingIcons[existingConfig.Type] = new Dictionary<string, Sprite>();
                }

                foreach (var typeIcon in existingConfig.TypeIcons)
                {
                    if (typeIcon.Icon != null && !existingIcons[existingConfig.Type].ContainsKey(typeIcon.TypeKey))
                    {
                        existingIcons[existingConfig.Type][typeIcon.TypeKey] = typeIcon.Icon;
                    }
                }
            }

            // 기존 아이콘을 가져오는 헬퍼 함수
            Sprite GetExistingIcon(GachaType gachaType, string typeKey)
            {
                return existingIcons.TryGetValue(gachaType, out var typeIcons) &&
                       typeIcons.TryGetValue(typeKey, out var icon)
                       ? icon : null;
            }

            _gachaTypeConfigs = new List<ItemTypeVisualConfig>
            {
                // Equipment 가챠 설정
                new ItemTypeVisualConfig
                {
                    Type = GachaType.Equipment,
                    GradeColors = new List<GradeColorEntry>
                    {
                        new GradeColorEntry { GradeKey = "F", Color = new Color(0.647f, 0.635f, 0.569f) },      // 회색
                        new GradeColorEntry { GradeKey = "D3", Color = new Color(0.992f, 0.992f, 0.855f) },     // 연한 노란색
                        new GradeColorEntry { GradeKey = "D2", Color = new Color(0.992f, 0.992f, 0.855f) },
                        new GradeColorEntry { GradeKey = "D1", Color = new Color(0.992f, 0.992f, 0.855f) },
                        new GradeColorEntry { GradeKey = "C3", Color = new Color(0.443f, 0.882f, 0.690f) },     // 초록색
                        new GradeColorEntry { GradeKey = "C2", Color = new Color(0.443f, 0.882f, 0.690f) },
                        new GradeColorEntry { GradeKey = "C1", Color = new Color(0.443f, 0.882f, 0.690f) },
                        new GradeColorEntry { GradeKey = "B3", Color = new Color(0.310f, 0.522f, 0.863f) },     // 파란색
                        new GradeColorEntry { GradeKey = "B2", Color = new Color(0.310f, 0.522f, 0.863f) },
                        new GradeColorEntry { GradeKey = "B1", Color = new Color(0.310f, 0.522f, 0.863f) },
                        new GradeColorEntry { GradeKey = "A3", Color = new Color(0.792f, 0.357f, 1.0f) },       // 보라색
                        new GradeColorEntry { GradeKey = "A2", Color = new Color(0.792f, 0.357f, 1.0f) },
                        new GradeColorEntry { GradeKey = "A1", Color = new Color(0.792f, 0.357f, 1.0f) },
                        new GradeColorEntry { GradeKey = "S3", Color = new Color(0.973f, 0.914f, 0.227f) },     // 노란색
                        new GradeColorEntry { GradeKey = "S2", Color = new Color(0.973f, 0.914f, 0.227f) },
                        new GradeColorEntry { GradeKey = "S1", Color = new Color(0.973f, 0.914f, 0.227f) },
                        new GradeColorEntry { GradeKey = "SS3", Color = new Color(0.980f, 0.561f, 0.133f) },    // 주황색
                        new GradeColorEntry { GradeKey = "SS2", Color = new Color(0.980f, 0.561f, 0.133f) },
                        new GradeColorEntry { GradeKey = "SS1", Color = new Color(0.980f, 0.561f, 0.133f) },
                        new GradeColorEntry { GradeKey = "SSS3", Color = new Color(1f, 0.306f, 0.306f) }        // 빨간색
                    },
                    TypeIcons = new List<TypeIconEntry>
                    {
                        // EquipmentType enum의 모든 값에 대한 엔트리 생성 (기존 아이콘이 있으면 유지, 없으면 null)
                        new TypeIconEntry { TypeKey = "Processor", Icon = GetExistingIcon(GachaType.Equipment, "Processor") },
                        new TypeIconEntry { TypeKey = "Wheel", Icon = GetExistingIcon(GachaType.Equipment, "Wheel") },
                        new TypeIconEntry { TypeKey = "Battery", Icon = GetExistingIcon(GachaType.Equipment, "Battery") },
                        new TypeIconEntry { TypeKey = "Antenna", Icon = GetExistingIcon(GachaType.Equipment, "Antenna") },
                        new TypeIconEntry { TypeKey = "Memory", Icon = GetExistingIcon(GachaType.Equipment, "Memory") },
                        new TypeIconEntry { TypeKey = "RobotArm", Icon = GetExistingIcon(GachaType.Equipment, "RobotArm") }
                    }
                },
                // Drone 가챠 설정
                new ItemTypeVisualConfig
                {
                    Type = GachaType.Drone,
                    GradeColors = new List<GradeColorEntry>
                    {
                        // 드론 ID 기반 색상 (예시 - 실제 드론 ID에 맞게 조정 필요)
                        new GradeColorEntry { GradeKey = "1", Color = new Color(0.647f, 0.635f, 0.569f) },      // 회색
                        new GradeColorEntry { GradeKey = "2", Color = new Color(0.992f, 0.992f, 0.855f) },     // 연한 노란색
                        new GradeColorEntry { GradeKey = "3", Color = new Color(0.992f, 0.992f, 0.855f) },
                        new GradeColorEntry { GradeKey = "4", Color = new Color(0.443f, 0.882f, 0.690f) },     // 초록색
                        new GradeColorEntry { GradeKey = "5", Color =new Color(0.443f, 0.882f, 0.690f) },
                        new GradeColorEntry { GradeKey = "6", Color = new Color(0.310f, 0.522f, 0.863f) },     // 파란색
                        new GradeColorEntry { GradeKey = "7", Color = new Color(0.310f, 0.522f, 0.863f) },
                        new GradeColorEntry { GradeKey = "8", Color = new Color(0.792f, 0.357f, 1.0f) },       // 보라색
                        new GradeColorEntry { GradeKey = "9", Color = new Color(0.792f, 0.357f, 1.0f) },
                        new GradeColorEntry { GradeKey = "10A", Color = new Color(0.973f, 0.914f, 0.227f) },
                        new GradeColorEntry { GradeKey = "10B", Color = new Color(0.973f, 0.914f, 0.227f) },
                        new GradeColorEntry { GradeKey = "10C", Color = new Color(0.973f, 0.914f, 0.227f) },
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
        public class ItemTypeVisualConfig
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

                if (string.IsNullOrEmpty(gradeKey))
                    return Color.white;

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
