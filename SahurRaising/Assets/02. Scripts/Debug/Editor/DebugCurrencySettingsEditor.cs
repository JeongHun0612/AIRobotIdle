#if UNITY_EDITOR
using BreakInfinity;
using UnityEditor;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace SahurRaising.Core
{
    /// <summary>
    /// DebugCurrencySettings의 커스텀 에디터
    /// 플레이 모드에서 재화를 추가/소모할 수 있는 버튼 제공
    /// </summary>
    [CustomEditor(typeof(DebugCurrencySettings))]
    public class DebugCurrencySettingsEditor : Editor
    {
        private DebugCurrencySettings _target;

        private void OnEnable()
        {
            _target = (DebugCurrencySettings)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Debug Currency Controller", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("플레이 모드에서만 동작합니다. 재화를 추가하거나 소모할 수 있습니다.", MessageType.Info);
            EditorGUILayout.Space();

            // 기본 인스펙터 표시
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            // 플레이 모드 체크
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("플레이 모드에서만 재화 조작이 가능합니다.", MessageType.Warning);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            // CurrencyService 가져오기
            var currencyService = ServiceLocator.Get<ICurrencyService>();
            if (currencyService == null)
            {
                EditorGUILayout.HelpBox("CurrencyService를 찾을 수 없습니다. 게임이 초기화되었는지 확인하세요.", MessageType.Error);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("현재 보유량", EditorStyles.boldLabel);

            // 현재 보유량 표시
            DrawCurrentBalance(CurrencyType.Gold, currencyService);
            DrawCurrentBalance(CurrencyType.Emerald, currencyService);
            DrawCurrentBalance(CurrencyType.Diamond, currencyService);
            DrawCurrentBalance(CurrencyType.Ticket, currencyService);
            DrawCurrentBalance(CurrencyType.Ruby, currencyService);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.LabelField("재화 추가", EditorStyles.boldLabel);

            // 재화 추가 UI
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button($"추가: {_target.AddCurrencyType} +{_target.AddAmount}", GUILayout.Height(30)))
            {
                var amount = _target.ParseBigDouble(_target.AddAmount);
                if (amount > 0)
                {
                    currencyService.Add(_target.AddCurrencyType, amount, "DebugCurrencySettings");
                    Debug.Log($"[DebugCurrencySettings] {_target.AddCurrencyType} {amount} 추가됨");
                    EditorUtility.SetDirty(_target);
                }
                else
                {
                    Debug.LogWarning("[DebugCurrencySettings] 추가할 양이 0보다 커야 합니다.");
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("재화 소모", EditorStyles.boldLabel);

            // 재화 소모 UI
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button($"소모: {_target.ConsumeCurrencyType} -{_target.ConsumeAmount}", GUILayout.Height(30)))
            {
                var amount = _target.ParseBigDouble(_target.ConsumeAmount);
                if (amount > 0)
                {
                    var success = currencyService.TryConsume(_target.ConsumeCurrencyType, amount, "DebugCurrencySettings");
                    if (success)
                    {
                        Debug.Log($"[DebugCurrencySettings] {_target.ConsumeCurrencyType} {amount} 소모됨");
                    }
                    else
                    {
                        Debug.LogWarning($"[DebugCurrencySettings] {_target.ConsumeCurrencyType} 보유량이 부족합니다. (현재: {currencyService.Get(_target.ConsumeCurrencyType)})");
                    }
                    EditorUtility.SetDirty(_target);
                }
                else
                {
                    Debug.LogWarning("[DebugCurrencySettings] 소모할 양이 0보다 커야 합니다.");
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            EditorGUILayout.LabelField("빠른 설정", EditorStyles.boldLabel);

            // 빠른 설정 버튼들
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("모든 재화 0으로 설정"))
            {
                SetAllCurrencies(currencyService, BigDouble.Zero);
            }
            if (GUILayout.Button("모든 재화 1000으로 설정"))
            {
                SetAllCurrencies(currencyService, new BigDouble(1000));
            }
            if (GUILayout.Button("모든 재화 1e10으로 설정"))
            {
                SetAllCurrencies(currencyService, new BigDouble(1e10));
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // 저장 버튼
            if (GUILayout.Button("현재 보유량 저장", GUILayout.Height(25)))
            {
                SaveCurrentBalances(currencyService);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawCurrentBalance(CurrencyType type, ICurrencyService service)
        {
            var balance = service.Get(type);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"{type}:", GUILayout.Width(100));
            EditorGUILayout.LabelField(balance.ToString(), EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
        }

        private void SetAllCurrencies(ICurrencyService service, BigDouble amount)
        {
            foreach (CurrencyType type in System.Enum.GetValues(typeof(CurrencyType)))
            {
                var current = service.Get(type);
                var diff = amount - current;
                if (diff > 0)
                {
                    service.Add(type, diff, "DebugCurrencySettings");
                }
                else if (diff < 0)
                {
                    service.TryConsume(type, -diff, "DebugCurrencySettings");
                }
            }
            Debug.Log($"[DebugCurrencySettings] 모든 재화를 {amount}로 설정했습니다.");
        }

        private async void SaveCurrentBalances(ICurrencyService service)
        {
            await service.SaveAsync();
            Debug.Log("[DebugCurrencySettings] 재화 저장 완료");
        }
    }
}
#endif