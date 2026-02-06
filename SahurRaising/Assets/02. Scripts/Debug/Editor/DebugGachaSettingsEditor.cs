#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Reflection;
using System.Collections.Generic;

namespace SahurRaising.Core
{
    /// <summary>
    /// DebugGachaSettings의 커스텀 에디터
    /// 플레이 모드에서 가챠 레벨과 카운트를 설정할 수 있는 버튼 제공
    /// </summary>
    [CustomEditor(typeof(DebugGachaSettings))]
    public class DebugGachaSettingsEditor : Editor
    {
        private DebugGachaSettings _target;

        private void OnEnable()
        {
            _target = (DebugGachaSettings)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Debug Gacha Controller", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("플레이 모드에서만 동작합니다. 가챠 타입별 레벨과 카운트를 설정할 수 있습니다.", MessageType.Info);
            EditorGUILayout.Space();

            // 기본 인스펙터 표시
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            // 플레이 모드 체크
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("플레이 모드에서만 가챠 조작이 가능합니다.", MessageType.Warning);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            // GachaService 가져오기
            if (!ServiceLocator.HasService<IGachaService>())
            {
                EditorGUILayout.HelpBox("GachaService를 찾을 수 없습니다. 게임이 초기화되었는지 확인하세요.", MessageType.Error);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            var gachaService = ServiceLocator.Get<IGachaService>();

            if (!gachaService.IsInitialized)
            {
                EditorGUILayout.HelpBox("GachaService가 아직 초기화되지 않았습니다.", MessageType.Warning);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("현재 가챠 상태", EditorStyles.boldLabel);

            // 현재 상태 표시
            DrawCurrentGachaStatus(GachaType.Equipment, gachaService);
            DrawCurrentGachaStatus(GachaType.Drone, gachaService);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.LabelField("가챠 레벨 및 카운트 설정", EditorStyles.boldLabel);

            // Equipment 가챠 설정
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("장비 가챠 (Equipment)", EditorStyles.boldLabel);
            DrawGachaSettings(GachaType.Equipment, _target.EquipmentGacha, gachaService);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("드론 가챠 (Drone)", EditorStyles.boldLabel);
            DrawGachaSettings(GachaType.Drone, _target.DroneGacha, gachaService);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.LabelField("빠른 설정", EditorStyles.boldLabel);

            // 빠른 설정 버튼들
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("모든 가챠 초기화 (레벨 1, 카운트 0)"))
            {
                SetGachaData(GachaType.Equipment, 1, 0, gachaService);
                SetGachaData(GachaType.Drone, 1, 0, gachaService);
                _target.EquipmentGacha.Level = 1;
                _target.EquipmentGacha.Count = 0;
                _target.DroneGacha.Level = 1;
                _target.DroneGacha.Count = 0;
                EditorUtility.SetDirty(_target);
                Debug.Log("[DebugGachaSettings] 모든 가챠를 초기화했습니다.");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // 저장 버튼
            if (GUILayout.Button("현재 가챠 데이터 저장", GUILayout.Height(25)))
            {
                SaveGachaData(gachaService);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawCurrentGachaStatus(GachaType type, IGachaService service)
        {
            int currentLevel = service.GetGachaLevel(type);
            int currentCount = service.GetGachaCount(type);
            int nextLevelRequired = service.GetRequiredCountForNextLevel(type);
            int maxLevel = service.LevelConfig != null ? service.LevelConfig.GetMaxLevel(type) : 1;

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"{type} 가챠", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("현재 레벨:", GUILayout.Width(100));
            EditorGUILayout.LabelField($"Lv.{currentLevel} / {maxLevel}", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("현재 카운트:", GUILayout.Width(100));
            EditorGUILayout.LabelField($"{currentCount}", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            if (currentLevel < maxLevel)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("다음 레벨 필요:", GUILayout.Width(100));
                EditorGUILayout.LabelField($"{nextLevelRequired}개", EditorStyles.boldLabel);
                EditorGUILayout.EndHorizontal();

                int progress = nextLevelRequired > 0 ? (currentCount * 100 / nextLevelRequired) : 0;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("진행도:", GUILayout.Width(100));
                EditorGUILayout.LabelField($"{progress}% ({currentCount}/{nextLevelRequired})");
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.LabelField("최대 레벨 도달", EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawGachaSettings(GachaType type, DebugGachaSettings.GachaDebugData data, IGachaService service)
        {
            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("레벨:", GUILayout.Width(60));
            data.Level = EditorGUILayout.IntField(data.Level);
            if (data.Level < 1)
                data.Level = 1;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("카운트:", GUILayout.Width(60));
            data.Count = EditorGUILayout.IntField(data.Count);
            if (data.Count < 0)
                data.Count = 0;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button($"적용: {type} (Lv.{data.Level}, Count:{data.Count})", GUILayout.Height(30)))
            {
                SetGachaData(type, data.Level, data.Count, service);
                EditorUtility.SetDirty(_target);
                Debug.Log($"[DebugGachaSettings] {type} 가챠 설정 완료 - 레벨: {data.Level}, 카운트: {data.Count}");
            }

            // 현재 값으로 동기화 버튼
            if (GUILayout.Button("현재 값으로 동기화", GUILayout.Height(30)))
            {
                data.Level = service.GetGachaLevel(type);
                data.Count = service.GetGachaCount(type);
                EditorUtility.SetDirty(_target);
                Debug.Log($"[DebugGachaSettings] {type} 가챠 값을 현재 상태로 동기화했습니다.");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void SetGachaData(GachaType type, int level, int count, IGachaService service)
        {
            // 리플렉션을 사용하여 GachaService의 내부 필드에 접근
            var serviceType = service.GetType();
            var gachaDataField = serviceType.GetField("_gachaData", BindingFlags.NonPublic | BindingFlags.Instance);

            if (gachaDataField != null)
            {
                var gachaData = gachaDataField.GetValue(service) as Dictionary<GachaType, GachaTypeSaveData>;
                if (gachaData != null)
                {
                    // 최대 레벨 체크
                    int maxLevel = service.LevelConfig != null ? service.LevelConfig.GetMaxLevel(type) : 1;
                    if (level > maxLevel)
                    {
                        level = maxLevel;
                        Debug.LogWarning($"[DebugGachaSettings] {type} 가챠 레벨이 최대 레벨({maxLevel})을 초과하여 {maxLevel}로 설정했습니다.");
                    }

                    // 레벨에 맞게 카운트 조정
                    if (level >= maxLevel)
                    {
                        count = 0; // 최대 레벨일 때는 카운트를 0으로 설정
                    }
                    else
                    {
                        // 다음 레벨 필요 개수보다 작게 조정
                        int nextLevelRequired = service.GetRequiredCountForLevel(type, level + 1);
                        if (count >= nextLevelRequired)
                        {
                            count = nextLevelRequired - 1;
                            Debug.LogWarning($"[DebugGachaSettings] {type} 가챠 카운트가 다음 레벨 필요 개수({nextLevelRequired})를 초과하여 {count}로 조정했습니다.");
                        }
                    }

                    var newData = new GachaTypeSaveData(type, count, level);
                    gachaData[type] = newData;
                    Debug.Log($"[DebugGachaSettings] {type} 가챠 데이터 설정 완료 - 레벨: {level}, 카운트: {count}");
                }
                else
                {
                    Debug.LogError("[DebugGachaSettings] _gachaData Dictionary를 찾을 수 없습니다.");
                }
            }
            else
            {
                Debug.LogError("[DebugGachaSettings] GachaService의 _gachaData 필드를 찾을 수 없습니다.");
            }
        }

        private async void SaveGachaData(IGachaService service)
        {
            await service.SaveAsync();
            Debug.Log("[DebugGachaSettings] 가챠 데이터 저장 완료");
        }
    }
}
#endif