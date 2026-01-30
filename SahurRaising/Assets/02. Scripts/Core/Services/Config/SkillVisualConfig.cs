using System.Collections.Generic;
using UnityEngine;

namespace SahurRaising.Core
{
    /// <summary>
    /// 스킬 카테고리(Prefix)별 시각적 요소(색상, 아이콘 등)를 관리하는 설정
    /// 에디터에서 ScriptableObject로 생성하여 사용
    /// </summary>
    [CreateAssetMenu(fileName = "SkillVisualConfig", menuName = "SahurRaising/Config/SkillVisualConfig")]
    public class SkillVisualConfig : ScriptableObject
    {
        [Header("기본 설정")]
        [SerializeField, Tooltip("매핑이 없는 스킬에 적용될 기본 프레임 색상")]
        private Color _defaultFrameColor = Color.white;

        [Header("카테고리별 시각 설정")]
        [SerializeField, Tooltip("스킬 카테고리(Prefix)별 시각 설정 리스트")]
        private List<SkillCategoryVisual> _categoryVisuals = new List<SkillCategoryVisual>();

        // 빠른 조회를 위한 딕셔너리
        private Dictionary<SkillIdPrefix, SkillCategoryVisual> _categoryVisualDict;

        public Color DefaultFrameColor => _defaultFrameColor;

        private void OnEnable()
        {
            BuildDictionary();
        }

        /// <summary>
        /// 딕셔너리 빌드 (OnEnable 및 필요시 호출)
        /// </summary>
        private void BuildDictionary()
        {
            _categoryVisualDict = new Dictionary<SkillIdPrefix, SkillCategoryVisual>();
            
            if (_categoryVisuals == null) return;

            foreach (var visual in _categoryVisuals)
            {
                if (visual.Prefix != SkillIdPrefix.None && !_categoryVisualDict.ContainsKey(visual.Prefix))
                {
                    _categoryVisualDict[visual.Prefix] = visual;
                }
            }
        }

        /// <summary>
        /// 스킬 카테고리(Prefix)에 해당하는 프레임 색상 반환
        /// </summary>
        /// <param name="prefix">스킬 ID 접두사</param>
        /// <returns>프레임 색상 (없으면 기본 색상)</returns>
        public Color GetFrameColor(SkillIdPrefix prefix)
        {
            if (_categoryVisualDict == null)
                BuildDictionary();

            if (_categoryVisualDict != null && _categoryVisualDict.TryGetValue(prefix, out var visual))
            {
                return visual.FrameColor;
            }

            return _defaultFrameColor;
        }

        /// <summary>
        /// 스킬 ID 문자열에서 Prefix를 파싱하여 프레임 색상 반환
        /// </summary>
        /// <param name="skillIdOrPrefix">스킬 ID (예: "AUTO001") 또는 Prefix 문자열 (예: "AUTO")</param>
        /// <returns>프레임 색상</returns>
        public Color GetFrameColorByString(string skillIdOrPrefix)
        {
            if (string.IsNullOrEmpty(skillIdOrPrefix))
                return _defaultFrameColor;

            // 숫자를 제거하여 Prefix만 추출 (예: "AUTO001" -> "AUTO")
            string prefixStr = System.Text.RegularExpressions.Regex.Match(skillIdOrPrefix, "^[A-Z]+").Value;

            if (!string.IsNullOrEmpty(prefixStr) && 
                System.Enum.TryParse(prefixStr, true, out SkillIdPrefix prefixEnum))
            {
                return GetFrameColor(prefixEnum);
            }

            return _defaultFrameColor;
        }

        /// <summary>
        /// 스킬 카테고리(Prefix)에 해당하는 배경 스프라이트 반환 (확장용)
        /// </summary>
        /// <param name="prefix">스킬 ID 접두사</param>
        /// <returns>배경 스프라이트 (없으면 null)</returns>
        public Sprite GetBackgroundSprite(SkillIdPrefix prefix)
        {
            if (_categoryVisualDict == null)
                BuildDictionary();

            if (_categoryVisualDict != null && _categoryVisualDict.TryGetValue(prefix, out var visual))
            {
                return visual.BackgroundSprite;
            }

            return null;
        }

        #region Editor Helper
#if UNITY_EDITOR
        /// <summary>
        /// 에디터에서 모든 SkillIdPrefix에 대한 기본 엔트리 생성
        /// </summary>
        [ContextMenu("모든 Prefix 엔트리 초기화")]
        private void InitializeAllPrefixEntries()
        {
            // 기존 설정 보존용 딕셔너리
            var existingVisuals = new Dictionary<SkillIdPrefix, SkillCategoryVisual>();
            foreach (var visual in _categoryVisuals)
            {
                if (!existingVisuals.ContainsKey(visual.Prefix))
                {
                    existingVisuals[visual.Prefix] = visual;
                }
            }

            _categoryVisuals = new List<SkillCategoryVisual>();

            // 모든 SkillIdPrefix에 대해 엔트리 생성
            foreach (SkillIdPrefix prefix in System.Enum.GetValues(typeof(SkillIdPrefix)))
            {
                if (prefix == SkillIdPrefix.None) continue;

                if (existingVisuals.TryGetValue(prefix, out var existing))
                {
                    // 기존 설정 유지
                    _categoryVisuals.Add(existing);
                }
                else
                {
                    // 새 엔트리 생성 (기본 색상 적용)
                    _categoryVisuals.Add(new SkillCategoryVisual
                    {
                        Prefix = prefix,
                        FrameColor = _defaultFrameColor,
                        BackgroundSprite = null
                    });
                }
            }

            BuildDictionary();
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[SkillVisualConfig] {_categoryVisuals.Count}개의 Prefix 엔트리가 초기화되었습니다.");
        }
#endif
        #endregion
    }

    /// <summary>
    /// 스킬 카테고리별 시각 설정 데이터
    /// </summary>
    [System.Serializable]
    public class SkillCategoryVisual
    {
        [Tooltip("스킬 ID 접두사 (예: AUTO, PARR, HERR 등)")]
        public SkillIdPrefix Prefix;

        [Tooltip("해당 카테고리의 프레임 색상")]
        public Color FrameColor = Color.white;

        [Tooltip("해당 카테고리의 배경 스프라이트 (선택사항)")]
        public Sprite BackgroundSprite;
    }
}
