using System.Collections.Generic;
using SahurRaising.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using Cysharp.Threading.Tasks;

namespace SahurRaising.UI
{
    /// <summary>
    /// 스킬 트리 팝업 UI
    /// 스킬 슬롯들을 트리 형태로 배치하고 관리
    /// </summary>
    public class UI_SkillPopup : UI_Popup
    {
        [Header("Components")]
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private Transform _content;
        [SerializeField] private TextMeshProUGUI _skillNameText; // 우상단 스킬 이름 표시

        [Header("Prefabs")]
        [SerializeField] private UI_SkillSlot _slotPrefab;
        [SerializeField] private GameObject _linePrefab;

        [Header("Layout")]
        [SerializeField, Tooltip("스킬 슬롯의 크기")] private Vector2 _slotSize = new Vector2(140, 160);
        [SerializeField, Tooltip("스킬 슬롯 간 간격")] private Vector2 _spacing = new Vector2(150, 150);
        [SerializeField, Tooltip("스킬 슬롯 간 연결선의 두께")] private float _lineThickness = 5f;

        private ISkillService _skillService;
        private readonly List<UI_SkillSlot> _slots = new();
        private readonly List<GameObject> _lines = new();
        private bool _isInitialized = false;

        public override async UniTask InitializeAsync()
        {
            await base.InitializeAsync();
        }

        public override void OnShow()
        {
            base.OnShow();
            
            // 서비스 초기화
            InitializeServices();
            
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
            
            // 스킬 이름 초기화
            UpdateSkillNameDisplay(null);
        }

        /// <summary>
        /// 서비스 초기화
        /// </summary>
        private void InitializeServices()
        {
            if (_skillService == null && ServiceLocator.HasService<ISkillService>())
            {
                _skillService = ServiceLocator.Get<ISkillService>();
            }

            if (_skillService == null)
            {
                Debug.LogError("[UI_SkillPopup] SkillService를 찾을 수 없습니다!");
            }
        }

        /// <summary>
        /// 스킬 트리 구축
        /// </summary>
        private void BuildSkillTree()
        {
            // 기존 슬롯 제거 (FogOfWarManager는 보존)
            ClearExistingSlots();

            if (_skillService == null)
            {
                _skillService = ServiceLocator.Get<ISkillService>();
            }

            if (_skillService == null)
            {
                Debug.LogError("[UI_SkillPopup] SkillService가 초기화되지 않았습니다.");
                return;
            }

            var table = _skillService.GetTable();
            if (table == null) return;

            // 1. 먼저 연결 라인 생성 (슬롯 뒤에 표시되도록)
            // 맨하탄 거리 1인 인접 노드끼리 연결 (중복 방지)
            var createdLines = new HashSet<string>();
            
            foreach (var pair in table.Index)
            {
                var row = pair.Value;
                // 4방향 인접 노드 체크 (맨하탄 거리 1)
                TryCreateLine(row, row.XCoord + 1, row.YCoord, table, createdLines);  // 오른쪽
                TryCreateLine(row, row.XCoord - 1, row.YCoord, table, createdLines);  // 왼쪽
                TryCreateLine(row, row.XCoord, row.YCoord + 1, table, createdLines);  // 위
                TryCreateLine(row, row.XCoord, row.YCoord - 1, table, createdLines);  // 아래
            }

            // 2. 슬롯 생성 (라인 위에 표시)
            foreach (var pair in table.Index)
            {
                var row = pair.Value;
                var slot = Instantiate(_slotPrefab, _content);

                // 좌표 설정
                RectTransform rt = slot.GetComponent<RectTransform>();
                rt.anchoredPosition = new Vector2(row.XCoord * _spacing.x, row.YCoord * _spacing.y);

                // 슬롯 초기화 - 클릭 콜백과 호버 콜백 전달
                slot.Initialize(row, _skillService, RefreshAllSlots, OnSlotClicked);
               // slot.OnSlotHovered += UpdateSkillNameDisplay;
                _slots.Add(slot);
            }
        }

        /// <summary>
        /// 기존 슬롯 정리
        /// </summary>
        private void ClearExistingSlots()
        {
            var childrenToDestroy = new List<GameObject>();
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
            _lines.Clear();
        }

        /// <summary>
        /// 스킬 이름 표시 업데이트
        /// </summary>
        private void UpdateSkillNameDisplay(SkillRow? skillData)
        {
            if (_skillNameText == null) return;
            
            if (skillData.HasValue && !string.IsNullOrEmpty(skillData.Value.Name))
            {
                _skillNameText.text = skillData.Value.Name;
                _skillNameText.gameObject.SetActive(true);
            }
            else
            {
                _skillNameText.text = "";
                _skillNameText.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// 슬롯 클릭 시 스킬 정보 팝업 표시
        /// </summary>
        private void OnSlotClicked(SkillRow skillData)
        {
            var state = _skillService.GetSkillState(skillData.ID);
            
            Debug.Log($"[UI_SkillPopup] 슬롯 클릭: {skillData.Name}, 상태: {state}");
            
            // 잠김 상태면 팝업 표시하지 않음
            if (state == SkillState.Locked)
            {
                Debug.Log($"[UI_SkillPopup] 아직 해금 불가능한 스킬: {skillData.Name}");
                return;
            }

            // 해금 가능 또는 해금 완료 상태 모두 정보 팝업 표시
            if (state == SkillState.Unlockable || state == SkillState.Unlocked)
            {
                ShowSkillInfoPopup(skillData, state);
            }
        }

        /// <summary>
        /// 스킬 정보 팝업 표시
        /// </summary>
        private void ShowSkillInfoPopup(SkillRow skillData, SkillState state)
        {
            if (UIManager.Instance == null)
            {
                Debug.LogError("[UI_SkillPopup] UIManager를 찾을 수 없어 팝업을 표시할 수 없습니다.");
                return;
            }

            Debug.Log($"[UI_SkillPopup] 스킬 정보 팝업 표시 시도: {skillData.Name}, 상태: {state}");

            // UI_Skill_Info 팝업 표시
            var skillInfoPopup = UIManager.Instance.ShowPopup<UI_SkillInfo>(EPopupUIType.SkillInfo, remember: false);
            if (skillInfoPopup != null)
            {
                // 상태에 따라 팝업에 다른 모드로 설정
                bool isUnlocked = (state == SkillState.Unlocked);
                skillInfoPopup.SetSkillData(skillData, OnSkillLearnConfirmed, isUnlocked);
                Debug.Log($"[UI_SkillPopup] 스킬 정보 팝업 표시 성공 (해금완료: {isUnlocked})");
            }
            else
            {
                Debug.LogError($"[UI_SkillPopup] 스킬 정보 팝업을 찾을 수 없습니다. UIRegistry에 SkillInfo 팝업이 등록되어 있는지 확인하세요.");
            }
        }

        /// <summary>
        /// 스킬 학습 확인 콜백
        /// </summary>
        private void OnSkillLearnConfirmed(SkillRow skillData)
        {
            // 해당 스킬의 슬롯을 찾아서 해금 시도
            var slot = _slots.Find(s => s.Data.ID == skillData.ID);
            if (slot != null && slot.TryUnlock())
            {
                Debug.Log($"[UI_SkillPopup] 스킬 연구 시작: {skillData.Name}");
            }
        }

        /// <summary>
        /// 대상 좌표에 노드가 있으면 라인 생성 (중복 방지)
        /// </summary>
        private void TryCreateLine(SkillRow fromRow, int targetX, int targetY, 
            SkillTable table, HashSet<string> createdLines)
        {
            // 대상 좌표에 노드 찾기
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

            if (!found) return;

            // 중복 방지 (A→B와 B→A가 같은 라인)
            string lineKey = GetLineKey(fromRow.XCoord, fromRow.YCoord, targetX, targetY);
            if (createdLines.Contains(lineKey)) return;
            
            createdLines.Add(lineKey);
            CreateLine(fromRow, targetRow);
        }

        /// <summary>
        /// 라인 키 생성 (양방향 동일 키 보장)
        /// </summary>
        private string GetLineKey(int x1, int y1, int x2, int y2)
        {
            // 좌표 정렬하여 (A→B)와 (B→A)가 동일한 키를 갖도록
            if (x1 < x2 || (x1 == x2 && y1 < y2))
            {
                return $"{x1},{y1}_{x2},{y2}";
            }
            return $"{x2},{y2}_{x1},{y1}";
        }

        private void CreateLine(SkillRow from, SkillRow to)
        {
            var lineObj = Instantiate(_linePrefab, _content);
            
            // 라인 리스트에 추가
            _lines.Add(lineObj);

            RectTransform rt = lineObj.GetComponent<RectTransform>();
            Image img = lineObj.GetComponent<Image>();

            Vector2 posA = new Vector2(from.XCoord * _spacing.x, from.YCoord * _spacing.y);
            Vector2 posB = new Vector2(to.XCoord * _spacing.x, to.YCoord * _spacing.y);

            Vector2 dir = (posB - posA).normalized;
            float distance = Vector2.Distance(posA, posB);

            // pivot이 중앙(0.5, 0.5)이므로 두 점의 중간에 배치
            rt.anchoredPosition = (posA + posB) / 2f;
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

        private void OnDestroy()
        {
            // 이벤트 해제
            foreach (var slot in _slots)
            {
                if (slot != null)
                {
                   // slot.OnSlotHovered -= UpdateSkillNameDisplay;
                }
            }
        }
    }
}
