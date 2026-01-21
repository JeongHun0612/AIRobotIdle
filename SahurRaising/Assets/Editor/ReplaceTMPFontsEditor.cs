#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;

namespace SahurRaising.EditorTools
{
    /// <summary>
    /// 프로젝트 전체(현재 열린 씬 + 모든 프리팹)에 있는 TMP_Text 폰트를
    /// 지정한 TMP_FontAsset으로 일괄 교체하는 유틸리티 (EditorWindow 버전)
    /// </summary>
    public class ReplaceTMPFontsEditor : EditorWindow
    {
        // AutoAddLocalizedFontHandler 등 외부에서 참조할 수 있도록 상수/키 정의
        public const string DefaultTargetFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/DNFBitBitv2 SDF.asset";
        public const string DefaultCjkFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/BoutiqueBitmap9x9_Bold_2.asset";


        public const string PrefKeyTargetFont = "SahurRaising_TargetFontPath";
        public const string PrefKeyCjkFont = "SahurRaising_CjkFontPath";

        private TMP_FontAsset targetFont;
        private TMP_FontAsset cjkFont;

        [MenuItem("Tools/SahurRaising/UI/Replace TMP Fonts Tool", priority = 50)]
        public static void ShowWindow()
        {
            GetWindow<ReplaceTMPFontsEditor>("Replace TMP Fonts");
        }

        private void OnEnable()
        {
            // 저장된 경로가 있으면 불러오고, 없으면 기본값 사용
            string targetPath = EditorPrefs.GetString(PrefKeyTargetFont, DefaultTargetFontPath);
            string cjkPath = EditorPrefs.GetString(PrefKeyCjkFont, DefaultCjkFontPath);

            targetFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(targetPath);
            cjkFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(cjkPath);
        }

        private void OnGUI()
        {
            GUILayout.Label("TMP Font Batch Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            targetFont = (TMP_FontAsset)EditorGUILayout.ObjectField("Target Font (KR/EN)", targetFont, typeof(TMP_FontAsset), false);
            cjkFont = (TMP_FontAsset)EditorGUILayout.ObjectField("CJK Font", cjkFont, typeof(TMP_FontAsset), false);


            if (EditorGUI.EndChangeCheck())
            {
                // 변경 시 즉시 저장
                string targetPath = targetFont ? AssetDatabase.GetAssetPath(targetFont) : "";
                string cjkPath = cjkFont ? AssetDatabase.GetAssetPath(cjkFont) : "";

                EditorPrefs.SetString(PrefKeyTargetFont, targetPath);
                EditorPrefs.SetString(PrefKeyCjkFont, cjkPath);
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("설정된 폰트는 EditorPrefs에 저장되어 'AutoAddLocalizedFontHandler' 등 자동화 스크립트에서도 사용됩니다.", MessageType.Info);
            EditorGUILayout.Space();

            if (GUILayout.Button("1. Replace All TMP Fonts to Target Font"))
            {
                if (targetFont == null)
                {
                    EditorUtility.DisplayDialog("Error", "Target Font를 설정해주세요.", "OK");
                    return;
                }
                ReplaceAllTMPFonts(targetFont);
            }

            if (GUILayout.Button("2. Add LocalizedTMPFont To All TMP_Text"))
            {
                if (targetFont == null || cjkFont == null)
                {
                    EditorUtility.DisplayDialog("Error", "Target Font와 CJK Font를 모두 설정해주세요.", "OK");
                    return;
                }
                AddLocalizedTMPFontToAllTMPTexts(targetFont, cjkFont);
            }
        }

        private static void ReplaceAllTMPFonts(TMP_FontAsset font)
        {
            int changedCount = 0;

            // 1) (제거됨) 현재 열린 씬들 내의 TMP_Text 교체 로직 제거
            // 사용자의 요청으로 03. Prefabs 폴더 하위만 대상으로 변경함.

            // 2) 프로젝트 내 "Assets/03. Prefabs" 폴더 하위의 프리팹 안의 TMP_Text 교체
            string[] searchFolders = new[] { "Assets/03. Prefabs" };
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", searchFolders);


            for (int i = 0; i < prefabGuids.Length; i++)
            {
                string guid = prefabGuids[i];
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                    continue;

                bool prefabChanged = false;
                var texts = prefab.GetComponentsInChildren<TMP_Text>(true);
                foreach (var text in texts)
                {
                    if (text.font == font)
                        continue;

                    Undo.RecordObject(text, "Replace TMP Font (Prefab)");
                    text.font = font;
                    EditorUtility.SetDirty(text);
                    changedCount++;
                    prefabChanged = true;
                }

                if (prefabChanged)
                {
                    EditorUtility.SetDirty(prefab);
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[ReplaceTMPFontsEditor] TMP_Text 폰트 일괄 교체 완료 (Target: Assets/03. Prefabs). 변경된 오브젝트 수: {changedCount}");
            EditorUtility.DisplayDialog("Complete", $"TMP_Text 폰트 일괄 교체 완료.\n(Target: Assets/03. Prefabs)\n변경된 오브젝트 수: {changedCount}", "OK");
        }

        private static void AddLocalizedTMPFontToAllTMPTexts(TMP_FontAsset koreanEnglishFont, TMP_FontAsset cjkFont)
        {
            int addedOrUpdatedCount = 0;

            // 1) (제거됨) 현재 열린 씬들 내의 TMP_Text 처리 로직 제거
            // 사용자의 요청으로 03. Prefabs 폴더 하위만 대상으로 변경함.

            // 2) "Assets/03. Prefabs" 폴더 내의 프리팹 처리
            string[] searchFolders = new[] { "Assets/03. Prefabs" };
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", searchFolders);

            for (int i = 0; i < prefabGuids.Length; i++)
            {
                string guid = prefabGuids[i];
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                    continue;

                bool prefabChanged = false;
                var texts = prefab.GetComponentsInChildren<TMP_Text>(true);
                foreach (var text in texts)
                {
                    int result = AddOrUpdateLocalizedTMPFontOnText(text, koreanEnglishFont, cjkFont);
                    if (result > 0)
                    {
                        prefabChanged = true;
                        addedOrUpdatedCount += result;
                    }
                }

                if (prefabChanged)
                {
                    EditorUtility.SetDirty(prefab);
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[ReplaceTMPFontsEditor] LocalizedTMPFont 자동 추가/업데이트 완료 (Target: Assets/03. Prefabs). 처리된 컴포넌트 수: {addedOrUpdatedCount}");
            EditorUtility.DisplayDialog("Complete", $"LocalizedTMPFont 자동 추가/업데이트 완료.\n(Target: Assets/03. Prefabs)\n처리된 컴포넌트 수: {addedOrUpdatedCount}", "OK");
        }

        /// <summary>
        /// 단일 TMP_Text에 대해 LocalizedTMPFont를 추가하거나 기존 설정을 업데이트한다.
        /// </summary>
        private static int AddOrUpdateLocalizedTMPFontOnText(TMP_Text text, TMP_FontAsset koreanEnglishFont, TMP_FontAsset cjkFont)
        {
            if (text == null)
                return 0;

            var existing = text.GetComponent<SahurRaising.UI.LocalizedTMPFont>();
            if (existing == null)
            {
                Undo.RecordObject(text.gameObject, "Add LocalizedTMPFont");
                existing = text.gameObject.AddComponent<SahurRaising.UI.LocalizedTMPFont>();
            }
            else
            {
                Undo.RecordObject(existing, "Update LocalizedTMPFont");
            }

            var so = new SerializedObject(existing);
            var propKorean = so.FindProperty("koreanEnglishFont");
            var propCjk = so.FindProperty("cjkFont");

            bool changed = false;
            // 프로퍼티가 null이 아닐 때만 값 할당 (스크립트 컴파일 직후 등 타이밍 이슈 방지)
            if (propKorean != null && propKorean.objectReferenceValue != koreanEnglishFont)
            {
                propKorean.objectReferenceValue = koreanEnglishFont;
                changed = true;
            }

            if (propCjk != null && propCjk.objectReferenceValue != cjkFont)
            {
                propCjk.objectReferenceValue = cjkFont;
                changed = true;
            }

            if (changed)
            {
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(existing);
                return 1;
            }

            return 0;
        }
    }
}
#endif
