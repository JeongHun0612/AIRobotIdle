using System.Collections.Generic;
using SahurRaising.Core;
using UnityEngine;
using UnityEngine.UI;

using Cysharp.Threading.Tasks;

namespace SahurRaising.UI
{
    public class UI_SkillPopup : UI_Popup
    {
        [Header("Components")]
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private Transform _content;

        [Header("Prefabs")]
        [SerializeField] private UI_SkillSlot _slotPrefab;
        [SerializeField] private GameObject _linePrefab; // Image with simple color

        [Header("Layout")]
        [SerializeField] private Vector2 _slotSize = new Vector2(100, 100);
        [SerializeField] private Vector2 _spacing = new Vector2(150, 150);
        [SerializeField] private float _lineThickness = 5f;

        [Header("Background Settings")]
        [SerializeField] private List<SkillBackgroundMapping> _backgroundMappings;
        [SerializeField] private Sprite _defaultBackground;
        [SerializeField] private Sprite _currencyIcon; // 재화 아이콘 (에메랄드)

        [System.Serializable]
        public struct SkillBackgroundMapping
        {
            public string Prefix;
            public Sprite Background;
        }

        private ISkillService _skillService;
        private readonly List<UI_SkillSlot> _slots = new();
        private bool _isInitialized = false;

        public override async UniTask InitializeAsync()
        {
            await base.InitializeAsync();
            // _skillService 초기화는 OnShow에서 수행합니다.
            // UIManager 초기화 시점에는 아직 Service가 등록되지 않았을 수 있기 때문입니다.
        }

        public override void OnShow()
        {
            base.OnShow();
            
            if (_skillService == null)
            {
                if (ServiceLocator.HasService<ISkillService>())
                {
                    _skillService = ServiceLocator.Get<ISkillService>();
                }
                else
                {
                    Debug.LogError("[UI_SkillPopup] SkillService not found!");
                    return;
                }
            }

            if (_skillService != null)
            {
                _skillService.CheckResearchCompletion();
            }

            if (!_isInitialized)
            {
                BuildSkillTree();
                _isInitialized = true;
            }

            RefreshAllSlots();
        }

        private void BuildSkillTree()
        {
            // 기존 슬롯 제거 (FogOfWarManager는 보존)
            var childrenToDestroy = new System.Collections.Generic.List<GameObject>();
            foreach (Transform child in _content)
            {
                // FogOfWarManager 컴포넌트가 있는 오브젝트는 삭제하지 않음
                if (child.GetComponent<Rendering.FogOfWarManager>() == null)
                {
                    childrenToDestroy.Add(child.gameObject);
                }
            }
            foreach (var obj in childrenToDestroy)
            {
                Destroy(obj);
            }
            _slots.Clear();

            if (_skillService == null)
            {
                _skillService = ServiceLocator.Get<ISkillService>();
            }

            if (_skillService == null)
            {
                Debug.LogError("[UI_SkillPopup] SkillService is not initialized.");
                return;
            }

            var table = _skillService.GetTable();
            if (table == null) return;

            // 1. 슬롯 생성
            foreach (var pair in table.Index)
            {
                var row = pair.Value;
                var slot = Instantiate(_slotPrefab, _content);

                // 좌표 설정 (중앙 기준 0,0)
                // ScrollView Content의 Pivot이 0.5, 0.5라고 가정
                RectTransform rt = slot.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(row.XCoord * _spacing.x, row.YCoord * _spacing.y);

                // 배경 이미지 찾기
                Sprite bgSprite = _defaultBackground;
                if (_backgroundMappings != null)
                {
                    var mapping = _backgroundMappings.Find(m => m.Prefix == row.Prefix);
                    if (mapping.Background != null)
                    {
                        bgSprite = mapping.Background;
                    }
                }

                slot.Initialize(row, _skillService, RefreshAllSlots, bgSprite, _currencyIcon);
                _slots.Add(slot);
            }

            // 2. 연결 라인 생성
            // 모든 슬롯 쌍을 비교하여 인접한 경우 라인 생성
            // 중복 생성을 막기 위해 (A,B) 연결 시 A < B 인 경우만 생성하거나, 
            // 방향성(Right, Up)만 체크

            foreach (var pair in table.Index)
            {
                var row = pair.Value;

                // 오른쪽(X+1)과 위쪽(Y+1)만 체크하여 라인 생성 (중복 방지)
                CheckAndCreateLine(row, row.XCoord + 1, row.YCoord);
                CheckAndCreateLine(row, row.XCoord, row.YCoord + 1);
            }
        }

        private void CheckAndCreateLine(SkillRow fromRow, int targetX, int targetY)
        {
            // 타겟 좌표에 스킬이 있는지 확인
            // Table을 순회해서 찾거나, 미리 Dictionary로 매핑해두면 좋음.
            // 여기서는 간단히 Table 순회 (데이터가 많지 않으므로)

            var table = _skillService.GetTable();
            SkillRow targetRow = default;
            bool found = false;

            foreach (var pair in table.Index)
            {
                if (pair.Value.XCoord == targetX && pair.Value.YCoord == targetY)
                {
                    targetRow = pair.Value;
                    found = true;
                    break;
                }
            }

            if (found)
            {
                CreateLine(fromRow, targetRow);
            }
        }

        private void CreateLine(SkillRow from, SkillRow to)
        {
            var lineObj = Instantiate(_linePrefab, _content);
            lineObj.transform.SetAsFirstSibling(); // 슬롯 뒤로 보내기

            RectTransform rt = lineObj.GetComponent<RectTransform>();
            Image img = lineObj.GetComponent<Image>();

            Vector2 posA = new Vector2(from.XCoord * _spacing.x, from.YCoord * _spacing.y);
            Vector2 posB = new Vector2(to.XCoord * _spacing.x, to.YCoord * _spacing.y);

            Vector2 dir = (posB - posA).normalized;
            float distance = Vector2.Distance(posA, posB);

            rt.anchoredPosition = posA + dir * (distance * 0.5f);
            rt.sizeDelta = new Vector2(distance, _lineThickness);

            // 회전
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            rt.localRotation = Quaternion.Euler(0, 0, angle);
        }

        private void RefreshAllSlots()
        {
            foreach (var slot in _slots)
            {
                slot.RefreshState();
            }
        }
    }
}
