using System.Collections.Generic;
using SahurRaising.Core;
using UnityEngine;
using UnityEngine.UI;

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

        private ISkillService _skillService;
        private readonly List<UI_SkillSlot> _slots = new();
        private bool _isInitialized = false;

        public override void Initialize()
        {
            base.Initialize();
            _skillService = ServiceLocator.Get<ISkillService>();
        }

        public override void OnShow()
        {
            base.OnShow();

            if (!_isInitialized)
            {
                BuildSkillTree();
                _isInitialized = true;
            }

            RefreshAllSlots();
        }

        private void BuildSkillTree()
        {
            // 기존 슬롯 제거
            foreach (Transform child in _content)
            {
                Destroy(child.gameObject);
            }
            _slots.Clear();

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

                slot.Initialize(row, _skillService, RefreshAllSlots);
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
