#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace SahurRaising.Core
{
    /// <summary>
    /// DebugSkillSettings의 커스텀 에디터
    /// 플레이 모드에서 스킬 해금 상태를 관리할 수 있는 버튼 제공
    /// 
    /// 빌드 영향: 없음 (#if UNITY_EDITOR + Editor 폴더)
    /// </summary>
    [CustomEditor(typeof(DebugSkillSettings))]
    public class DebugSkillSettingsEditor : Editor
    {
        private DebugSkillSettings _target;
        private Vector2 _scrollPosition;
        private bool _showAllSkills = true;
        private string _filterText = "";
        
        // 캐시된 서비스 참조 (플레이 모드 중 유지)
        private ISkillService _cachedSkillService;
        private SkillTable _cachedSkillTable;
        private bool _serviceInitialized = false;

        private void OnEnable()
        {
            _target = (DebugSkillSettings)target;
            ClearCache();
            
            // 플레이 모드 변경 감지
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }

        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            // 플레이 모드 종료 또는 시작 시 캐시 초기화
            ClearCache();
        }

        private void ClearCache()
        {
            _cachedSkillService = null;
            _cachedSkillTable = null;
            _serviceInitialized = false;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("🎮 Debug Skill Controller", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("플레이 모드에서 스킬 해금 상태를 관리할 수 있습니다.\n서비스 초기화가 완료되면 자동으로 활성화됩니다.", MessageType.Info);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            // 플레이 모드 체크
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("⏸️ 플레이 모드에서만 스킬 조작이 가능합니다.", MessageType.Warning);
                ClearCache();
                serializedObject.ApplyModifiedProperties();
                return;
            }

            // 서비스 초기화 체크 (캐싱 활용)
            if (!TryGetSkillService())
            {
                EditorGUILayout.HelpBox("⏳ SkillService 초기화 대기 중...\n게임이 완전히 초기화될 때까지 잠시 기다려주세요.\n\n(Inspector 창을 클릭하면 수동 갱신됩니다)", MessageType.Warning);
                
                // 수동 갱신 버튼
                if (GUILayout.Button("🔄 서비스 상태 확인", GUILayout.Height(30)))
                {
                    ClearCache(); // 캐시 초기화하여 다시 체크
                }
                
                serializedObject.ApplyModifiedProperties();
                return;
            }

            DrawSkillControlPanel();

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 스킬 서비스를 안전하게 가져오기 (캐싱 활용)
        /// </summary>
        private bool TryGetSkillService()
        {
            // 이미 초기화되었으면 캐시 사용
            if (_serviceInitialized && _cachedSkillService != null && _cachedSkillTable != null)
                return true;

            // 서비스 체크
            if (!ServiceLocator.HasService<ISkillService>())
                return false;

            _cachedSkillService = ServiceLocator.Get<ISkillService>();
            if (_cachedSkillService == null)
                return false;

            _cachedSkillTable = _cachedSkillService.GetTable();
            if (_cachedSkillTable == null || _cachedSkillTable.Index == null)
                return false;

            _serviceInitialized = true;
            return true;
        }

        /// <summary>
        /// 스킬 컨트롤 패널 그리기
        /// </summary>
        private void DrawSkillControlPanel()
        {
            // ===== 빠른 설정 섹션 =====
            EditorGUILayout.LabelField("⚡ 빠른 설정", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            // 전체 초기화 버튼 (학습 전 상태로)
            GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
            if (GUILayout.Button("🔄 전체 초기화\n(학습 전 상태)", GUILayout.Height(45)))
            {
                if (EditorUtility.DisplayDialog("⚠️ 스킬 전체 초기화", 
                    "모든 스킬을 학습 전 상태로 되돌립니다.\n\n• 모든 해금 상태 제거\n• 진행 중인 연구 취소\n• 되돌릴 수 없습니다!\n\n정말 초기화하시겠습니까?", 
                    "초기화", "취소"))
                {
                    _cachedSkillService.ResetAllSkills();
                    Debug.Log("[DebugSkillSettings] ✅ 모든 스킬이 학습 전 상태로 초기화됨");
                }
            }
            
            // 전체 해금 버튼
            GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
            if (GUILayout.Button("✅ 전체 해금\n(모든 스킬 습득)", GUILayout.Height(45)))
            {
                foreach (var pair in _cachedSkillTable.Index)
                {
                    _cachedSkillService.ForceUnlock(pair.Key);
                }
                Debug.Log("[DebugSkillSettings] ✅ 모든 스킬 해금됨");
            }
            
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            
            // ===== 현황 섹션 =====
            var unlockedIds = _cachedSkillService.GetUnlockedSkillIds();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"📊 현재 해금: {unlockedIds.Count} / {_cachedSkillTable.Index.Count}", EditorStyles.boldLabel);
            
            // 진행률 바
            float progress = (float)unlockedIds.Count / _cachedSkillTable.Index.Count;
            Rect progressRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            EditorGUI.ProgressBar(progressRect, progress, $"{progress * 100:F1}%");
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // ===== 필터 섹션 =====
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("🔍 검색:", GUILayout.Width(50));
            _filterText = EditorGUILayout.TextField(_filterText);
            if (GUILayout.Button("✕", GUILayout.Width(25)))
            {
                _filterText = "";
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // ===== 스킬 목록 섹션 =====
            _showAllSkills = EditorGUILayout.Foldout(_showAllSkills, "📋 스킬 목록", true);
            
            if (_showAllSkills)
            {
                DrawSkillList();
            }
            
            EditorGUILayout.Space();
            
            // 저장 버튼
            if (GUILayout.Button("💾 현재 상태 저장", GUILayout.Height(30)))
            {
                SaveSkillState();
            }
        }

        /// <summary>
        /// 스킬 목록 그리기
        /// </summary>
        private void DrawSkillList()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(350));
            
            // 필터링
            var filteredSkills = _cachedSkillTable.Index
                .Where(pair => string.IsNullOrEmpty(_filterText) || 
                               pair.Value.Name.Contains(_filterText) ||
                               pair.Key.Contains(_filterText))
                .OrderBy(pair => pair.Value.XCoord)
                .ThenBy(pair => pair.Value.YCoord)
                .ToList();

            if (filteredSkills.Count == 0)
            {
                EditorGUILayout.HelpBox("검색 결과가 없습니다.", MessageType.Info);
            }

            foreach (var pair in filteredSkills)
            {
                DrawSkillItem(pair.Key, pair.Value);
            }
            
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 개별 스킬 항목 그리기
        /// </summary>
        private void DrawSkillItem(string skillId, SkillRow skillRow)
        {
            var state = _cachedSkillService.GetSkillState(skillId);
            
            // 상태별 배경색
            Color bgColor = state switch
            {
                SkillState.Unlocked => new Color(0.3f, 0.8f, 0.3f, 0.3f),
                SkillState.Researching => new Color(1f, 0.8f, 0.2f, 0.3f),
                SkillState.Unlockable => new Color(0.4f, 0.6f, 1f, 0.3f),
                _ => new Color(0.5f, 0.5f, 0.5f, 0.2f)
            };
            
            // 상태 아이콘
            string stateIcon = state switch
            {
                SkillState.Unlocked => "✅",
                SkillState.Researching => "⏳",
                SkillState.Unlockable => "🔓",
                _ => "🔒"
            };
            
            Rect boxRect = EditorGUILayout.BeginHorizontal();
            EditorGUI.DrawRect(boxRect, bgColor);
            
            // 스킬 정보
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField($"{stateIcon} {skillRow.Name}", EditorStyles.boldLabel);
            
            string stateText = state switch
            {
                SkillState.Unlocked => "해금됨",
                SkillState.Researching => $"연구 중 (남은 시간: {_cachedSkillService.GetRemainingTime(skillId):F1}초)",
                SkillState.Unlockable => "해금 가능",
                _ => "잠김"
            };
            EditorGUILayout.LabelField($"좌표: ({skillRow.XCoord}, {skillRow.YCoord}) | {stateText}", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            
            // 버튼
            GUILayout.FlexibleSpace();
            
            if (state == SkillState.Unlocked || state == SkillState.Researching)
            {
                GUI.backgroundColor = new Color(1f, 0.7f, 0.7f);
                if (GUILayout.Button("🔒 잠금", GUILayout.Width(70), GUILayout.Height(35)))
                {
                    _cachedSkillService.ForceLock(skillId);
                    Debug.Log($"[DebugSkillSettings] 🔒 스킬 잠금: {skillRow.Name}");
                }
            }
            else
            {
                GUI.backgroundColor = new Color(0.7f, 1f, 0.7f);
                if (GUILayout.Button("✅ 해금", GUILayout.Width(70), GUILayout.Height(35)))
                {
                    _cachedSkillService.ForceUnlock(skillId);
                    Debug.Log($"[DebugSkillSettings] ✅ 스킬 해금: {skillRow.Name}");
                }
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(3);
        }

        private async void SaveSkillState()
        {
            await _cachedSkillService.SaveAsync();
            Debug.Log("[DebugSkillSettings] 💾 스킬 저장 완료");
        }
    }
}
#endif
