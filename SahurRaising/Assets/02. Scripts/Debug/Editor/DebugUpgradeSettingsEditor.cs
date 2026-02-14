#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Linq;
using BreakInfinity;

namespace SahurRaising.Core
{
    /// <summary>
    /// DebugUpgradeSettings의 커스텀 에디터
    /// 플레이 모드에서 업그레이드 레벨을 관리하고 캐릭터 스탯을 확인할 수 있습니다.
    /// 
    /// 빌드 영향: 없음 (#if UNITY_EDITOR + Editor 폴더)
    /// </summary>
    [CustomEditor(typeof(DebugUpgradeSettings))]
    public class DebugUpgradeSettingsEditor : Editor
    {
        private DebugUpgradeSettings _target;
        private Vector2 _scrollPosition;
        private bool _showAllUpgrades = true;
        private bool _showStatSnapshot = true;
        private string _filterText = "";
        private UpgradeTier? _tierFilter = null;
        
        // 캐시된 서비스 참조 (플레이 모드 중 유지)
        private IUpgradeService _cachedUpgradeService;
        private IStatService _cachedStatService;
        private UpgradeTable _cachedUpgradeTable;
        private bool _serviceInitialized = false;

        private void OnEnable()
        {
            _target = (DebugUpgradeSettings)target;
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
            _cachedUpgradeService = null;
            _cachedStatService = null;
            _cachedUpgradeTable = null;
            _serviceInitialized = false;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("⬆️ Debug Upgrade Controller", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("플레이 모드에서 업그레이드 레벨을 관리하고\n캐릭터 스탯 스냅샷을 확인할 수 있습니다.", MessageType.Info);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            // 플레이 모드 체크
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("⏸️ 플레이 모드에서만 업그레이드 조작이 가능합니다.", MessageType.Warning);
                ClearCache();
                serializedObject.ApplyModifiedProperties();
                return;
            }

            // 서비스 초기화 체크 (캐싱 활용)
            if (!TryGetServices())
            {
                EditorGUILayout.HelpBox("⏳ 서비스 초기화 대기 중...\n게임이 완전히 초기화될 때까지 잠시 기다려주세요.\n\n(Inspector 창을 클릭하면 수동 갱신됩니다)", MessageType.Warning);
                
                // 수동 갱신 버튼
                if (GUILayout.Button("🔄 서비스 상태 확인", GUILayout.Height(30)))
                {
                    ClearCache(); // 캐시 초기화하여 다시 체크
                }
                
                serializedObject.ApplyModifiedProperties();
                return;
            }

            DrawUpgradeControlPanel();

            serializedObject.ApplyModifiedProperties();
        }

        /// <summary>
        /// 서비스들을 안전하게 가져오기 (캐싱 활용)
        /// </summary>
        private bool TryGetServices()
        {
            // 이미 초기화되었으면 캐시 사용
            if (_serviceInitialized && _cachedUpgradeService != null && _cachedUpgradeTable != null)
                return true;

            // UpgradeService 체크
            if (!ServiceLocator.HasService<IUpgradeService>())
                return false;

            _cachedUpgradeService = ServiceLocator.Get<IUpgradeService>();
            if (_cachedUpgradeService == null)
                return false;

            _cachedUpgradeTable = _cachedUpgradeService.GetTable();
            if (_cachedUpgradeTable == null || _cachedUpgradeTable.Index == null)
                return false;

            // StatService 체크 (선택적 — 없어도 업그레이드 관리는 가능)
            if (ServiceLocator.HasService<IStatService>())
                _cachedStatService = ServiceLocator.Get<IStatService>();

            _serviceInitialized = true;
            return true;
        }

        /// <summary>
        /// 업그레이드 컨트롤 패널 그리기
        /// </summary>
        private void DrawUpgradeControlPanel()
        {
            // ===== 빠른 설정 섹션 =====
            EditorGUILayout.LabelField("⚡ 빠른 설정", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            // 전체 초기화 버튼 (레벨 0으로)
            GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
            if (GUILayout.Button("🔄 전체 초기화\n(레벨 0)", GUILayout.Height(45)))
            {
                if (EditorUtility.DisplayDialog("⚠️ 업그레이드 전체 초기화", 
                    "모든 업그레이드를 레벨 0으로 되돌립니다.\n\n• 모든 업그레이드 레벨 초기화\n• 스탯 변경 즉시 적용\n• 되돌릴 수 없습니다!\n\n정말 초기화하시겠습니까?", 
                    "초기화", "취소"))
                {
                    _cachedUpgradeService.ResetAllLevels();
                    Debug.Log("[DebugUpgradeSettings] ✅ 모든 업그레이드가 레벨 0으로 초기화됨");
                }
            }
            
            // 전체 최대 레벨 버튼
            GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
            if (GUILayout.Button("⬆️ 전체 최대 레벨\n(MAX)", GUILayout.Height(45)))
            {
                _cachedUpgradeService.MaxAllLevels();
                Debug.Log("[DebugUpgradeSettings] ✅ 모든 업그레이드가 최대 레벨로 설정됨");
            }
            
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            
            // ===== 현황 섹션 =====
            var allLevels = _cachedUpgradeService.GetAllLevels();
            int totalMaxLevel = 0;
            int totalCurrentLevel = 0;
            
            foreach (var pair in _cachedUpgradeTable.Index)
            {
                totalMaxLevel += pair.Value.MaxLevel;
                if (allLevels.TryGetValue(pair.Key, out var level))
                    totalCurrentLevel += level;
            }
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"📊 총 레벨 합계: {totalCurrentLevel:N0} / {totalMaxLevel:N0}", EditorStyles.boldLabel);
            
            // 진행률 바
            float progress = totalMaxLevel > 0 ? (float)totalCurrentLevel / totalMaxLevel : 0f;
            Rect progressRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            EditorGUI.ProgressBar(progressRect, progress, $"{progress * 100:F1}%");
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            // ===== 스탯 스냅샷 섹션 =====
            DrawStatSnapshot();

            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
            
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
            
            // 티어 필터
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("📦 티어:", GUILayout.Width(50));
            
            GUI.backgroundColor = _tierFilter == null ? new Color(0.7f, 0.9f, 1f) : Color.white;
            if (GUILayout.Button("전체", GUILayout.Width(50)))
                _tierFilter = null;
                
            GUI.backgroundColor = _tierFilter == UpgradeTier.Normal ? new Color(0.7f, 0.9f, 1f) : Color.white;
            if (GUILayout.Button("Normal", GUILayout.Width(60)))
                _tierFilter = UpgradeTier.Normal;
                
            GUI.backgroundColor = _tierFilter == UpgradeTier.Super ? new Color(0.7f, 0.9f, 1f) : Color.white;
            if (GUILayout.Button("Super", GUILayout.Width(55)))
                _tierFilter = UpgradeTier.Super;
                
            GUI.backgroundColor = _tierFilter == UpgradeTier.Ultra ? new Color(0.7f, 0.9f, 1f) : Color.white;
            if (GUILayout.Button("Ultra", GUILayout.Width(50)))
                _tierFilter = UpgradeTier.Ultra;
                
            GUI.backgroundColor = _tierFilter == UpgradeTier.SuperUltra ? new Color(0.7f, 0.9f, 1f) : Color.white;
            if (GUILayout.Button("SuperUltra", GUILayout.Width(80)))
                _tierFilter = UpgradeTier.SuperUltra;
                
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // ===== 업그레이드 목록 섹션 =====
            _showAllUpgrades = EditorGUILayout.Foldout(_showAllUpgrades, "📋 업그레이드 목록", true);
            
            if (_showAllUpgrades)
            {
                DrawUpgradeList();
            }
            
            EditorGUILayout.Space();
            
            // 저장 버튼
            if (GUILayout.Button("💾 현재 상태 저장", GUILayout.Height(30)))
            {
                SaveUpgradeState();
            }
        }

        /// <summary>
        /// 현재 캐릭터 스탯 스냅샷을 표시하는 섹션
        /// </summary>
        private void DrawStatSnapshot()
        {
            _showStatSnapshot = EditorGUILayout.Foldout(_showStatSnapshot, "🎯 캐릭터 스탯 스냅샷", true);
            
            if (!_showStatSnapshot)
                return;

            if (_cachedStatService == null)
            {
                EditorGUILayout.HelpBox("StatService를 사용할 수 없습니다.", MessageType.Warning);
                return;
            }

            var snapshot = _cachedStatService.GetSnapshot();

            // 배경색 설정
            var bgColor = new Color(0.15f, 0.2f, 0.3f, 0.3f);
            var boxRect = EditorGUILayout.BeginVertical();
            EditorGUI.DrawRect(boxRect, bgColor);
            
            EditorGUILayout.Space(4);

            // --- 기본 전투 스탯 ---
            EditorGUILayout.LabelField("⚔️ 기본 전투 스탯", EditorStyles.boldLabel);

            DrawStatRow("캐릭터 레벨", snapshot.CharacterLevel.ToString("N0"));
            DrawStatRow("공격력 (ATK)", FormatBigNumber(snapshot.Attack));
            DrawStatRow("최대 HP", FormatBigNumber(snapshot.MaxHP));
            DrawStatRow("방어력 (DEF)", FormatBigNumber(snapshot.Defense));
            DrawStatRow("초당 회복량 (HPREC)", FormatBigNumber(snapshot.HealthRegen));
            
            EditorGUILayout.Space(4);
            
            // --- 공격 관련 스탯 ---
            EditorGUILayout.LabelField("🎯 공격 관련", EditorStyles.boldLabel);
            
            DrawStatRow("공격 속도", $"{snapshot.AttackSpeed:F4}");
            DrawStatRow("크리티컬 확률", $"{snapshot.CritChance:F2}%");
            DrawStatRow("크리티컬 배율", $"{snapshot.CritMultiplier:F2}x");
            DrawStatRow("터치 공격 배율", $"{snapshot.TouchDamageMultiplier:F2}x");
            DrawStatRow("울트라 크리티컬 확률", $"{snapshot.UltraCritChance:F2}%");

            EditorGUILayout.Space(4);
            
            // --- 보너스/비율 스탯 ---
            EditorGUILayout.LabelField("📈 보너스 스탯", EditorStyles.boldLabel);
            
            DrawStatRow("공격력 증가율 (ATKR)", $"{snapshot.AttackRate:F2}%");
            DrawStatRow("골드 획득 증가 (GOLDR)", $"{snapshot.GoldBonusRate:F2}%");
            DrawStatRow("미접속 보너스 시간 (OFFT)", $"{snapshot.OfflineTimeMinutes:F1}분");
            DrawStatRow("미접속 획득량 (OFFA)", $"{snapshot.OfflineAmountRate:F2}%");
            DrawStatRow("쿨타임 감소 (RCD)", $"{snapshot.CooldownReduction:F4}%");
            DrawStatRow("공격 보너스 (ATKB)", $"{snapshot.AttackBonus:F2}%");
            DrawStatRow("크리 데미지 보너스 (CD)", $"{snapshot.CritDamageBonus:F2}%");
            DrawStatRow("방어력 비율 (DEFR)", $"{snapshot.DefenseRate:F2}%");
            DrawStatRow("방어 무시 (IGNDEF)", $"{snapshot.DefenseIgnore:F2}%");
            DrawStatRow("보스 데미지 (BOSS)", $"{snapshot.BossDamageRate:F2}%");
            DrawStatRow("동시 타겟 수", $"{snapshot.MaxTargetCount}");
            
            EditorGUILayout.Space(4);
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 스탯 행 하나를 그리는 헬퍼
        /// </summary>
        private void DrawStatRow(string label, string value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"  {label}", GUILayout.Width(200));
            EditorGUILayout.LabelField(value, EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 업그레이드 목록 그리기
        /// </summary>
        private void DrawUpgradeList()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(400));
            
            // 필터링
            var filteredUpgrades = _cachedUpgradeTable.Index
                .Where(pair => 
                    (string.IsNullOrEmpty(_filterText) || 
                     pair.Value.Name.Contains(_filterText) ||
                     pair.Key.Contains(_filterText)) &&
                    (_tierFilter == null || pair.Value.Tier == _tierFilter))
                .OrderBy(pair => pair.Value.Tier)
                .ThenBy(pair => pair.Key)
                .ToList();

            if (filteredUpgrades.Count == 0)
            {
                EditorGUILayout.HelpBox("검색 결과가 없습니다.", MessageType.Info);
            }

            foreach (var pair in filteredUpgrades)
            {
                DrawUpgradeItem(pair.Key, pair.Value);
            }
            
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 개별 업그레이드 항목 그리기
        /// </summary>
        private void DrawUpgradeItem(string code, UpgradeRow upgradeRow)
        {
            var currentLevel = _cachedUpgradeService.GetLevel(code);
            var maxLevel = upgradeRow.MaxLevel;
            var isMaxLevel = currentLevel >= maxLevel;
            
            // 티어별 배경색
            Color bgColor = upgradeRow.Tier switch
            {
                UpgradeTier.Super => new Color(0.4f, 0.6f, 1f, 0.2f),
                UpgradeTier.Ultra => new Color(0.8f, 0.5f, 1f, 0.2f),
                UpgradeTier.SuperUltra => new Color(1f, 0.7f, 0.3f, 0.2f),
                _ => new Color(0.5f, 0.5f, 0.5f, 0.15f)
            };
            
            // 최대 레벨인 경우 진한 녹색
            if (isMaxLevel)
                bgColor = new Color(0.3f, 0.8f, 0.3f, 0.25f);
            
            string tierIcon = upgradeRow.Tier switch
            {
                UpgradeTier.Super => "🔵",
                UpgradeTier.Ultra => "🟣",
                UpgradeTier.SuperUltra => "🟠",
                _ => "⚪"
            };
            
            Rect boxRect = EditorGUILayout.BeginHorizontal();
            EditorGUI.DrawRect(boxRect, bgColor);
            
            // 업그레이드 정보
            EditorGUILayout.BeginVertical(GUILayout.Width(200));
            EditorGUILayout.LabelField($"{tierIcon} {upgradeRow.Name}", EditorStyles.boldLabel);
            
            var nextCost = _cachedUpgradeService.GetNextCost(code);
            string costText = isMaxLevel ? "MAX" : $"다음 비용: {FormatBigNumber(nextCost)}";
            
            // 현재 스탯 값 표시
            string statText = "";
            if (_cachedStatService != null)
            {
                var statValue = _cachedStatService.GetStatValue(code, currentLevel);
                statText = $" | 스탯: {FormatBigNumber(statValue)}";
            }
            
            EditorGUILayout.LabelField($"스탯: {upgradeRow.Stat} | {costText}{statText}", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            
            GUILayout.FlexibleSpace();
            
            // 레벨 표시 및 슬라이더
            EditorGUILayout.BeginVertical(GUILayout.Width(180));
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Lv. {currentLevel} / {maxLevel}", GUILayout.Width(80));
            
            // 레벨 진행률 바
            float levelProgress = maxLevel > 0 ? (float)currentLevel / maxLevel : 0f;
            Rect levelProgressRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight, GUILayout.Width(80));
            EditorGUI.ProgressBar(levelProgressRect, levelProgress, "");
            EditorGUILayout.EndHorizontal();
            
            // 레벨 슬라이더
            var newLevel = EditorGUILayout.IntSlider(currentLevel, 0, maxLevel);
            if (newLevel != currentLevel)
            {
                _cachedUpgradeService.ForceSetLevel(code, newLevel);
            }
            
            EditorGUILayout.EndVertical();
            
            // 빠른 버튼들
            EditorGUILayout.BeginVertical(GUILayout.Width(100));
            
            EditorGUILayout.BeginHorizontal();
            
            // -10 버튼
            GUI.enabled = currentLevel > 0;
            if (GUILayout.Button("-10", GUILayout.Width(35), GUILayout.Height(20)))
            {
                _cachedUpgradeService.ForceSetLevel(code, currentLevel - 10);
            }
            
            // +10 버튼
            GUI.enabled = currentLevel < maxLevel;
            if (GUILayout.Button("+10", GUILayout.Width(35), GUILayout.Height(20)))
            {
                _cachedUpgradeService.ForceSetLevel(code, currentLevel + 10);
            }
            GUI.enabled = true;
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            // 0으로 버튼
            GUI.backgroundColor = new Color(1f, 0.7f, 0.7f);
            if (GUILayout.Button("0", GUILayout.Width(35), GUILayout.Height(20)))
            {
                _cachedUpgradeService.ForceSetLevel(code, 0);
            }
            
            // MAX 버튼
            GUI.backgroundColor = new Color(0.7f, 1f, 0.7f);
            if (GUILayout.Button("MAX", GUILayout.Width(35), GUILayout.Height(20)))
            {
                _cachedUpgradeService.ForceSetLevel(code, maxLevel);
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(3);
        }

        private async void SaveUpgradeState()
        {
            await _cachedUpgradeService.SaveAsync();
            Debug.Log("[DebugUpgradeSettings] 💾 업그레이드 저장 완료");
        }

        private string FormatBigNumber(BigDouble value)
        {
            if (value < 1000)
                return value.ToString("F2");
            if (value < 1e6)
                return $"{(value / 1e3):F2}K";
            if (value < 1e9)
                return $"{(value / 1e6):F2}M";
            if (value < 1e12)
                return $"{(value / 1e9):F2}B";
            if (value < 1e15)
                return $"{(value / 1e12):F2}T";
            
            return value.ToString("E2");
        }
    }
}
#endif

