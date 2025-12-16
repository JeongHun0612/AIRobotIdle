#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace SahurRaising.EditorTools
{
    /// <summary>
    /// Addressables 데이터 키를 자동으로 설정하는 유틸리티
    /// - 프로젝트 내 ScriptableObject 테이블들을 Addressable로 등록하고 주소를 지정
    /// </summary>
    public static class AddressablesSetup
    {
        private const string DataLabel = "Data";

        // 실제 프로젝트에서 사용하는 SO 테이블 경로/키
        private const string MonsterTablePath = "Assets/06. ScriptableObject/Data/MonsterTable.asset";
        private const string MonsterTableKey = "MonsterTable";

        private const string UpgradeTablePath = "Assets/06. ScriptableObject/Data/UpgradeTable.asset";
        private const string UpgradeTableKey = "UpgradeTable";

        private const string StatsTablePath = "Assets/06. ScriptableObject/Data/StatsTable.asset";
        private const string StatsTableKey = "StatsTable";

        private const string EquipmentTablePath = "Assets/06. ScriptableObject/Data/EquipmentTable.asset";
        private const string EquipmentTableKey = "EquipmentTable";

        [MenuItem("Tools/SahurRaising/Addressables/Setup Data Keys", priority = 10)]
        public static void SetupDataKeys()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null)
            {
                Debug.LogError("[AddressablesSetup] AddressableAssetSettings 를 생성/가져올 수 없습니다.");
                return;
            }

            // 기본 그룹 보장
            var group = settings.DefaultGroup;
            if (group == null)
            {
                group = settings.CreateGroup("Default Local Group", false, false, false, null);
                settings.DefaultGroup = group;
            }

            // 라벨 보장
            settings.AddLabel(DataLabel, true);

            // 등록 함수
            void AddOrUpdate(string assetPath, string address)
            {
                string guid = AssetDatabase.AssetPathToGUID(assetPath);
                if (string.IsNullOrEmpty(guid))
                {
                    Debug.LogWarning($"[AddressablesSetup] 에셋을 찾을 수 없습니다: {assetPath}");
                    return;
                }

                var entry = settings.FindAssetEntry(guid);
                if (entry == null)
                    entry = settings.CreateOrMoveEntry(guid, group);

                if (entry.address != address)
                    entry.address = address;

                entry.SetLabel(DataLabel, true, true);
            }

            AddOrUpdate(MonsterTablePath, MonsterTableKey);
            AddOrUpdate(UpgradeTablePath, UpgradeTableKey);
            AddOrUpdate(StatsTablePath, StatsTableKey);
            AddOrUpdate(EquipmentTablePath, EquipmentTableKey);

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log("[AddressablesSetup] 데이터 키 설정 완료: .....");
        }

        [InitializeOnLoadMethod]
        private static void AutoSetupOnLoad()
        {
            // 에디터 세션에서 한 번만 자동 적용
            if (SessionState.GetBool("RE_AutoAddrSetupDone", false))
                return;

            try
            {
                SetupDataKeys();
                SessionState.SetBool("RE_AutoAddrSetupDone", true);
            }
            catch
            {
                // 컴파일 직후 Addressables 세팅이 아직 준비되지 않았을 수 있으므로 조용히 무시
            }
        }
    }
}
#endif


