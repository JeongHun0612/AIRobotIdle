#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace SahurRaising.Core
{
    /// <summary>
    /// DebugEquipmentSettings의 커스텀 에디터
    /// 플레이 모드에서 장비를 추가/장착/레벨업할 수 있는 버튼 제공
    /// </summary>
    [CustomEditor(typeof(DebugEquipmentSettings))]
    public class DebugEquipmentSettingsEditor : Editor
    {
        private DebugEquipmentSettings _target;
        private Vector2 _inventoryScrollPos;
        private int _selectedTabIndex = 0;
        private string[] _tabNames = { "Processor", "Wheel", "Battery", "Antenna", "Memory", "RobotArm" };

        private void OnEnable()
        {
            _target = (DebugEquipmentSettings)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Debug Equipment Controller", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("플레이 모드에서만 동작합니다. 장비를 추가, 장착, 레벨업할 수 있습니다.", MessageType.Info);
            EditorGUILayout.Space();

            // 기본 인스펙터 표시
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            // 플레이 모드 체크
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("플레이 모드에서만 장비 조작이 가능합니다.", MessageType.Warning);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            // EquipmentService 가져오기
            if (!ServiceLocator.HasService<IEquipmentService>())
            {
                EditorGUILayout.HelpBox("EquipmentService를 찾을 수 없습니다. 게임이 초기화되었는지 확인하세요.", MessageType.Error);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            var equipmentService = ServiceLocator.Get<IEquipmentService>();

            if (!equipmentService.IsInitialized)
            {
                EditorGUILayout.HelpBox("EquipmentService가 아직 초기화되지 않았습니다.", MessageType.Warning);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("인벤토리 정보", EditorStyles.boldLabel);

            // 타입별 탭으로 인벤토리 정보 표시
            _selectedTabIndex = GUILayout.Toolbar(_selectedTabIndex, _tabNames);

            // 탭별 클리어 버튼
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button($"전체 클리어: {_tabNames[_selectedTabIndex]}", GUILayout.Height(25)))
            {
                ClearAllInventoryByType(equipmentService, (EquipmentType)_selectedTabIndex);
            }
            EditorGUILayout.EndHorizontal();

            DrawInventoryInfoByType(equipmentService, (EquipmentType)_selectedTabIndex);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.LabelField("현재 장착된 장비", EditorStyles.boldLabel);
            DrawEquippedEquipment(equipmentService);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("장비 승급", EditorStyles.boldLabel);

            // 장비 승급 UI - 현재 선택된 탭 타입 사용
            var currentAdvanceType = (EquipmentType)_selectedTabIndex;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("타입:", GUILayout.Width(50));
            EditorGUILayout.LabelField($"{currentAdvanceType}", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button($"승급: {currentAdvanceType} (모든 가능한 장비)", GUILayout.Height(30)))
            {
                var result = equipmentService.AdvanceAllAvailable(currentAdvanceType);
                if (result.HasValue)
                {
                    EditorUtility.SetDirty(_target);
                }
                else
                {
                    Debug.LogWarning($"[DebugEquipmentSettings] {currentAdvanceType} 타입 장비 승급 불가 (조건 불만족)");
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // 저장 버튼
            if (GUILayout.Button("현재 장비 데이터 저장", GUILayout.Height(25)))
            {
                SaveEquipmentData(equipmentService);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawEquippedEquipment(IEquipmentService service)
        {
            EditorGUILayout.BeginVertical("box");
            foreach (EquipmentType type in System.Enum.GetValues(typeof(EquipmentType)))
            {
                var equippedCode = service.GetEquippedCode(type);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{type}:", GUILayout.Width(120));
                if (string.IsNullOrEmpty(equippedCode))
                {
                    EditorGUILayout.LabelField("(장착 안 됨)", EditorStyles.centeredGreyMiniLabel);
                }
                else
                {
                    EditorGUILayout.LabelField(equippedCode, EditorStyles.boldLabel);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawInventoryInfoByType(IEquipmentService service, EquipmentType type)
        {
            _inventoryScrollPos = EditorGUILayout.BeginScrollView(_inventoryScrollPos, GUILayout.Height(200));

            var equipmentList = service.GetByType(type);
            var inventoryList = new List<(string code, EquipmentInventoryInfo info, EquipmentRow row)>();

            foreach (var equipment in equipmentList)
            {
                var info = service.GetInventoryInfo(equipment.Code);
                inventoryList.Add((equipment.Code, info, equipment));
            }

            if (inventoryList.Count == 0)
            {
                EditorGUILayout.LabelField("해당 타입의 장비가 없습니다.", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                EditorGUILayout.BeginVertical("box");
                foreach (var (code, info, row) in inventoryList)
                {
                    EditorGUILayout.BeginHorizontal("box");

                    // 코드
                    EditorGUILayout.LabelField($"{code}", GUILayout.Width(50));

                    // 등급
                    EditorGUILayout.LabelField($"[{row.Grade}]", GUILayout.Width(50));

                    // 레벨
                    EditorGUILayout.LabelField($"Lv.{info.Level}", GUILayout.Width(35));

                    // 레벨 + 버튼
                    if (GUILayout.Button("+", GUILayout.Width(25)))
                    {
                        LevelUp(service, code);
                    }

                    // 레벨 - 버튼
                    if (GUILayout.Button("-", GUILayout.Width(25)))
                    {
                        LevelDown(service, code);
                    }

                    // 간격
                    EditorGUILayout.Space(5);

                    // 개수
                    EditorGUILayout.LabelField($"x{info.Count}", GUILayout.Width(35));

                    // 개수 + 버튼
                    if (GUILayout.Button("+", GUILayout.Width(25)))
                    {
                        AddToInventory(service, code);
                    }

                    // 개수 - 버튼
                    if (GUILayout.Button("-", GUILayout.Width(25)))
                    {
                        RemoveFromInventory(service, code);
                    }

                    // 간격
                    EditorGUILayout.Space(5);

                    // 장착 표시
                    var equippedCode = service.GetEquippedCode(type);
                    bool isEquipped = code == equippedCode;

                    // 장착 버튼
                    if (isEquipped)
                    {
                        GUI.enabled = false;
                        GUILayout.Button("장착", GUILayout.Width(50));
                        GUI.enabled = true;
                    }
                    else
                    {
                        if (GUILayout.Button("장착", GUILayout.Width(50)))
                        {
                            if (info.IsOwned)
                            {
                                var success = service.Equip(type, code);
                                if (success)
                                {
                                    Debug.Log($"[DebugEquipmentSettings] {type}에 {code} 장착됨");
                                    EditorUtility.SetDirty(_target);
                                }
                                else
                                {
                                    Debug.LogWarning($"[DebugEquipmentSettings] 장비 장착 실패: {code}");
                                }
                            }
                            else
                            {
                                Debug.LogWarning($"[DebugEquipmentSettings] 장착 실패: {code} (인벤토리에 없음)");
                            }
                        }
                    }

                    // 해제 버튼
                    if (isEquipped)
                    {
                        if (GUILayout.Button("해제", GUILayout.Width(50)))
                        {
                            var success = service.Unequip(type);
                            if (success)
                            {
                                Debug.Log($"[DebugEquipmentSettings] {type} 해제됨");
                                EditorUtility.SetDirty(_target);
                            }
                            else
                            {
                                Debug.LogWarning($"[DebugEquipmentSettings] 장비 해제 실패: {type}");
                            }
                        }
                    }
                    else
                    {
                        GUI.enabled = false;
                        GUILayout.Button("해제", GUILayout.Width(50));
                        GUI.enabled = true;
                    }

                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();
        }

        private void AddToInventory(IEquipmentService service, string code)
        {
            var success = service.AddToInventory(code, 1);
            if (success)
            {
                Debug.Log($"[DebugEquipmentSettings] {code} x1 추가됨");
                EditorUtility.SetDirty(_target);
            }
            else
            {
                Debug.LogWarning($"[DebugEquipmentSettings] 장비 추가 실패: {code}");
            }
        }

        private void RemoveFromInventory(IEquipmentService service, string code)
        {
            var success = service.RemoveFromInventory(code, 1);
            if (success)
            {
                Debug.Log($"[DebugEquipmentSettings] {code} x1 제거됨");
                EditorUtility.SetDirty(_target);
            }
            else
            {
                Debug.LogWarning($"[DebugEquipmentSettings] 장비 제거 실패: {code}");
            }
        }

        private void LevelUp(IEquipmentService service, string code)
        {
            var success = service.LevelUp(code);
            if (success)
            {
                Debug.Log($"[DebugEquipmentSettings] {code} 레벨업 완료");
                EditorUtility.SetDirty(_target);
            }
            else
            {
                Debug.LogWarning($"[DebugEquipmentSettings] 레벨업 실패: {code}");
            }
        }

        private void LevelDown(IEquipmentService service, string code)
        {
            var success = service.LevelDown(code);
            if (success)
            {
                Debug.Log($"[DebugEquipmentSettings] {code} 레벨 다운 완료");
                EditorUtility.SetDirty(_target);
            }
            else
            {
                Debug.LogWarning($"[DebugEquipmentSettings] 레벨 다운 실패: {code}");
            }
        }

        private void ClearAllInventoryByType(IEquipmentService service, EquipmentType type)
        {
            service.ClearAllInventoryByType(type);
            Debug.Log($"[DebugEquipmentSettings] {type} 타입의 모든 장비 클리어 완료");
            EditorUtility.SetDirty(_target);
        }

        private async void SaveEquipmentData(IEquipmentService service)
        {
            await service.SaveAsync();
            Debug.Log("[DebugEquipmentSettings] 장비 데이터 저장 완료");
        }
    }
}
#endif