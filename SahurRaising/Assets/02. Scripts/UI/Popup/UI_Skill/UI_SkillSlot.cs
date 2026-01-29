using Cysharp.Threading.Tasks;
using SahurRaising.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace SahurRaising.UI
{
    /// <summary>
    /// 스킬 슬롯 UI 컴포넌트
    /// 새로운 디자인: 아이콘 중심, 우하단 로마자 레벨, 상태별 오버레이
    /// </summary>
    public class UI_SkillSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("핵심 UI 컴포넌트")]
        [SerializeField] private Image _skillIconImage;        // 스킬 아이콘 (항상 표시)
        [SerializeField] private TextMeshProUGUI _levelText;   // 우하단 로마자 레벨
        
        [Header("상태 오버레이")]
        [SerializeField] private GameObject _dimOverlay;       // 딤 처리 (잠김 상태)
        [SerializeField] private Image _lockIconImage;         // 우상단 자물쇠 아이콘
        [SerializeField] private GameObject _focusGlow;        // 해금 가능 상태 포커스
        [SerializeField] private Image _progressBorder;        // Radial fill 프로그레스
        
        [SerializeField] private Button _button;

        private SkillRow _data;
        private ISkillService _skillService;
        private IEventBus _eventBus;
        
        private System.Action _onStateChanged;
        private System.Action<SkillRow> _onSlotClicked;
        private Rendering.FogRevealer _fogRevealer;

        public SkillRow Data => _data;
        
        // 호버 이벤트 (스킬 이름 표시용)
       // public event System.Action<SkillRow?> OnSlotHovered;

        // 로마자 변환 배열 (1~10 레벨)
        private static readonly string[] ROMAN_NUMERALS = 
        { 
            "I", "II", "III", "IV", "V", 
            "VI", "VII", "VIII", "IX", "X" 
        };

        private void OnDestroy()
        {
            if (_eventBus != null)
            {
                _eventBus.Unsubscribe<SkillStateChangedEvent>(OnSkillStateChanged);
            }
        }

        /// <summary>
        /// 스킬 슬롯 초기화
        /// </summary>
        /// <param name="data">스킬 데이터</param>
        /// <param name="skillService">스킬 서비스</param>
        /// <param name="onStateChanged">상태 변경 콜백</param>
        /// <param name="onSlotClicked">슬롯 클릭 콜백 (정보 팝업용)</param>
        public void Initialize(SkillRow data, ISkillService skillService, 
            System.Action onStateChanged, System.Action<SkillRow> onSlotClicked = null)
        {
            _data = data;
            _skillService = skillService;
            _onStateChanged = onStateChanged;
            _onSlotClicked = onSlotClicked;
            
            // 이벤트 버스 연결
            _eventBus = ServiceLocator.Get<IEventBus>();
            if (_eventBus != null)
            {
                // 재활용 시 중복 구독 방지
                _eventBus.Unsubscribe<SkillStateChangedEvent>(OnSkillStateChanged);
                _eventBus.Subscribe<SkillStateChangedEvent>(OnSkillStateChanged);
            }

            _fogRevealer = GetComponent<Rendering.FogRevealer>();

            // 스킬 아이콘 설정 (항상 표시)
            if (_skillIconImage != null && data.Icon != null)
            {
                _skillIconImage.sprite = data.Icon;
            }

            // 스킬 레벨 로마자 표시
            UpdateLevelText();

            // 버튼 클릭 이벤트 설정
            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(OnClick);
            }

            RefreshState();
        }

        private void OnSkillStateChanged(SkillStateChangedEvent evt)
        {
            if (string.IsNullOrEmpty(_data.ID) || evt.SkillId != _data.ID) return;
            
            RefreshState();
            _onStateChanged?.Invoke();
        }

        /// <summary>
        /// 마우스 진입 시 스킬 이름 표시
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
           // OnSlotHovered?.Invoke(_data);
        }

        /// <summary>
        /// 마우스 퇴장 시 스킬 이름 숨김
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
          //  OnSlotHovered?.Invoke(null);
        }

        /// <summary>
        /// 스킬 이름에서 로마자를 추출하여 표시
        /// SkillTable의 Name 필드에 이미 로마자가 포함되어 있음 (예: "자동화 I", "병렬처리 II")
        /// </summary>
        private void UpdateLevelText()
        {
            if (_levelText == null) return;
            
            string romanNumeral = ExtractRomanNumeralFromName(_data.Name);
            _levelText.text = romanNumeral;
        }

        /// <summary>
        /// 스킬 이름에서 로마자(I, II, III 등) 추출
        /// </summary>
        private string ExtractRomanNumeralFromName(string skillName)
        {
            if (string.IsNullOrEmpty(skillName)) return "I";
            
            // 스킬 이름의 마지막 공백 이후 로마자를 추출 (예: "자동화 VIII" -> "VIII")
            int lastSpaceIndex = skillName.LastIndexOf(' ');
            if (lastSpaceIndex >= 0 && lastSpaceIndex < skillName.Length - 1)
            {
                string potentialRoman = skillName.Substring(lastSpaceIndex + 1).Trim();
                
                // 로마자 유효성 검사 (I, V, X만 포함)
                if (IsValidRomanNumeral(potentialRoman))
                {
                    return potentialRoman;
                }
            }
            
            // 버전 형태도 체크 (예: "드론 펌웨어 v1.0")
            if (skillName.Contains("v") || skillName.Contains("V"))
            {
                int vIndex = skillName.LastIndexOf('v');
                if (vIndex < 0) vIndex = skillName.LastIndexOf('V');
                if (vIndex >= 0 && vIndex < skillName.Length - 1)
                {
                    return skillName.Substring(vIndex);
                }
            }
            
            return "I";
        }
        
        /// <summary>
        /// 유효한 로마자인지 체크
        /// </summary>
        private bool IsValidRomanNumeral(string str)
        {
            if (string.IsNullOrEmpty(str)) return false;
            
            // 모든 문자가 I, V, X인지 체크
            foreach (char c in str)
            {
                if (c != 'I' && c != 'V' && c != 'X')
                    return false;
            }
            return true;
        }

        /// <summary>
        /// UI 상태 갱신
        /// </summary>
        public void RefreshState()
        {
            var state = _skillService.GetSkillState(_data.ID);

            // Fog of War 시야 처리
            UpdateFogRevealer(state);

            // 모든 오버레이 초기화
            ResetOverlays();

            switch (state)
            {
                case SkillState.Unlocked:
                    // 해금됨: 딤 없음, 자물쇠 없음
                    // 버튼은 클릭 가능해야 함
                    break;

                case SkillState.Researching:
                    // 연구 중: 프로그레스 바 표시
                    if (_progressBorder != null)
                    {
                        if (_lockIconImage != null) _lockIconImage.gameObject.SetActive(false);
                        if (_dimOverlay != null) _dimOverlay.SetActive(true);
                        _progressBorder.gameObject.SetActive(true);
                        // 즉시 현재 진행도 반영 (잔상 방지)
                        UpdateResearchProgress();
                    }
                    break;

                case SkillState.Unlockable:
                    // 해금 가능: 포커스 + 자물쇠
                    if (_focusGlow != null) _focusGlow.SetActive(true);
                    if (_dimOverlay != null) _dimOverlay.SetActive(true);
                    if (_lockIconImage != null) _lockIconImage.gameObject.SetActive(true);
                    break;

                case SkillState.Locked:
                    // 잠김: 딤 + 자물쇠
                    if (_dimOverlay != null) _dimOverlay.SetActive(true);
                    if (_lockIconImage != null) _lockIconImage.gameObject.SetActive(true);
                    break;
            }
            
            // 버튼이 항상 클릭 가능하도록 보장 (프리팹 구조와 무관하게)
            if (_button != null)
            {
                _button.interactable = true;
            }
        }

        /// <summary>
        /// Fog of War 업데이트
        /// </summary>
        private void UpdateFogRevealer(SkillState state)
        {
            if (_fogRevealer == null) return;
            
            bool shouldReveal = (state == SkillState.Unlocked || state == SkillState.Researching);
            if (_fogRevealer.enabled != shouldReveal)
            {
                _fogRevealer.enabled = shouldReveal;
                _fogRevealer.RequestFogUpdate();
            }
        }

        /// <summary>
        /// 모든 오버레이 초기화
        /// </summary>
        private void ResetOverlays()
        {
            if (_dimOverlay != null) _dimOverlay.SetActive(false);
            if (_lockIconImage != null) _lockIconImage.gameObject.SetActive(false);
            if (_focusGlow != null) _focusGlow.SetActive(false);
            if (_progressBorder != null) 
            {
                _progressBorder.gameObject.SetActive(false);
                _progressBorder.fillAmount = 0f; // 잔상 방지
            }
        }

        private void Update()
        {
            if (_skillService == null || string.IsNullOrEmpty(_data.ID)) return;

            // 연구 중일 때만 프로그레스 업데이트
            if (_skillService.GetSkillState(_data.ID) == SkillState.Researching)
            {
                UpdateResearchProgress();
            }
        }

        /// <summary>
        /// 연구 진행도 업데이트 (Radial fill)
        /// </summary>
        private void UpdateResearchProgress()
        {
            if (_progressBorder == null || _data.Time <= 0) return;

            double remaining = _skillService.GetRemainingTime(_data.ID);
            float progress = 1f - (float)(remaining / _data.Time);
            _progressBorder.fillAmount = Mathf.Clamp01(progress);
        }

        /// <summary>
        /// 슬롯 클릭 이벤트
        /// </summary>
        private void OnClick()
        {
            var state = _skillService.GetSkillState(_data.ID);
            
            Debug.Log($"[UI_SkillSlot] 클릭됨: {_data.Name}, 상태: {state}, 콜백존재: {_onSlotClicked != null}");

            if (state == SkillState.Unlocked)
            {
                // 이미 해금된 스킬 - NEW 태그 처리
                if (_skillService.IsNewSkill(_data.ID))
                {
                    _skillService.AcknowledgeSkill(_data.ID);
                    RefreshState();
                }
                
                // 해금 완료된 스킬도 클릭 시 정보 팝업 표시
                if (_onSlotClicked != null)
                {
                    Debug.Log($"[UI_SkillSlot] 해금 완료 스킬 정보 팝업 표시: {_data.Name}");
                    _onSlotClicked.Invoke(_data);
                }
                else
                {
                    Debug.LogWarning($"[UI_SkillSlot] 해금 완료 스킬이지만 콜백이 null: {_data.Name}");
                }
                return;
            }

            if (state == SkillState.Researching)
            {
                Debug.Log($"[UI_SkillSlot] 연구 중인 스킬: {_data.Name}");
                return;
            }

            // 해금 가능 또는 잠김 상태 - 정보 팝업 표시
            if (_onSlotClicked != null)
            {
                Debug.Log($"[UI_SkillSlot] 슬롯 클릭 콜백 호출: {_data.Name}");
                _onSlotClicked.Invoke(_data);
            }
            else
            {
               Debug.Log($"[UI_SkillSlot] 슬롯 없음");
            }
        }

        /// <summary>
        /// 외부에서 해금 시도 (정보 팝업에서 호출)
        /// </summary>
        public bool TryUnlock()
        {
            var state = _skillService.GetSkillState(_data.ID);
            
            if (state != SkillState.Unlockable)
            {
                Debug.Log($"[UI_SkillSlot] 해금 불가 상태: {_data.Name}");
                return false;
            }

            if (_skillService.TryUnlock(_data.ID))
            {
                Debug.Log($"[UI_SkillSlot] 연구 시작 성공: {_data.Name}");
                // 즉시 상태 갱신 (연구 중 상태로 변경)
                RefreshState();
                _onStateChanged?.Invoke();
                return true;
            }
            
            return false;
        }
    }
}
