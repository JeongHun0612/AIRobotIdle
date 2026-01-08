using SahurRaising.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SahurRaising.UI
{
    public class UI_SkillSlot : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private Image _iconImage;
        [SerializeField] private Image _frameImage;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _costText;
        [SerializeField] private Image _costIconImage; // 재화 아이콘
        [SerializeField] private TextMeshProUGUI _timeText;
        [SerializeField] private Image _researchProgressBar; // 연구 진행도 바 (Image Type: Filled)
        [SerializeField] private GameObject _openOverlay;
        [SerializeField] private GameObject _lockIcon; // This is LockOverlay
        [SerializeField] private GameObject _researchIcon; // This is ResearchOverlay
        [SerializeField] private GameObject _newTag; // New Tag Overlay
        [SerializeField] private GameObject _focusObject; // Unlockable Focus Overlay
        [SerializeField] private Button _button;

        private SkillRow _data;
        private ISkillService _skillService;
        private System.Action _onStateChanged;

        public SkillRow Data => _data;

        public void Initialize(SkillRow data, ISkillService skillService, System.Action onStateChanged, Sprite backgroundSprite = null, Sprite currencyIcon = null)
        {
            _data = data;
            _skillService = skillService;
            _onStateChanged = onStateChanged;

            if (_nameText != null) _nameText.text = data.Name;
            if (_costText != null) _costText.text = data.Cost.ToString();
            if (_iconImage != null && data.Icon != null) _iconImage.sprite = data.Icon;
            
            // 비용 아이콘 적용
            if (_costIconImage != null && currencyIcon != null)
            {
                _costIconImage.sprite = currencyIcon;
            }
            
            // 배경 이미지 적용
            if (backgroundSprite != null && _frameImage != null)
            {
                _frameImage.sprite = backgroundSprite;
            }

            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(OnClick);

            RefreshState();
        }

        public void RefreshState()
        {
            var state = _skillService.GetSkillState(_data.ID);

            switch (state)
            {
                case SkillState.Unlocked:
                    if (_openOverlay != null) _openOverlay.SetActive(true);
                    if (_lockIcon != null) _lockIcon.SetActive(false);
                    if (_researchIcon != null) _researchIcon.SetActive(false);
                    if (_focusObject != null) _focusObject.SetActive(false);
                    
                    if (_costText != null) _costText.gameObject.SetActive(false);
                    if (_timeText != null) _timeText.gameObject.SetActive(false);

                    // NEW 태그 표시 여부 결정
                    bool isNew = _skillService.IsNewSkill(_data.ID);
                    if (_newTag != null) _newTag.SetActive(isNew);
                    break;

                case SkillState.Researching:
                    if (_openOverlay != null) _openOverlay.SetActive(false);
                    if (_lockIcon != null) _lockIcon.SetActive(false);
                    if (_researchIcon != null) _researchIcon.SetActive(true);
                    if (_focusObject != null) _focusObject.SetActive(false);

                    if (_costText != null) _costText.gameObject.SetActive(false);
                    if (_timeText != null) _timeText.gameObject.SetActive(true);
                    break;

                case SkillState.Unlockable:
                    if (_openOverlay != null) _openOverlay.SetActive(false);
                    if (_lockIcon != null) _lockIcon.SetActive(true);
                    if (_researchIcon != null) _researchIcon.SetActive(false);
                    if (_focusObject != null) _focusObject.SetActive(true);

                    if (_costText != null) _costText.gameObject.SetActive(true);
                    if (_timeText != null) _timeText.gameObject.SetActive(false);
                    break;

                case SkillState.Locked:
                    if (_openOverlay != null) _openOverlay.SetActive(false);
                    if (_lockIcon != null) _lockIcon.SetActive(true);
                    if (_researchIcon != null) _researchIcon.SetActive(false);
                    if (_focusObject != null) _focusObject.SetActive(false);

                    if (_costText != null) _costText.gameObject.SetActive(true);
                    if (_timeText != null) _timeText.gameObject.SetActive(false);
                    break;
            }
        }

        private void Update()
        {
            if (_skillService == null || string.IsNullOrEmpty(_data.ID)) return;

            if (_skillService.GetSkillState(_data.ID) == SkillState.Researching)
            {
                double remaining = _skillService.GetRemainingTime(_data.ID);
                
                // 텍스트 업데이트
                if (_timeText != null)
                {
                    System.TimeSpan ts = System.TimeSpan.FromSeconds(remaining);
                    if (ts.TotalHours >= 1)
                        _timeText.text = string.Format("{0:D2}:{1:D2}:{2:D2}", (int)ts.TotalHours, ts.Minutes, ts.Seconds);
                    else
                        _timeText.text = string.Format("{0:D2}:{1:D2}", ts.Minutes, ts.Seconds);
                }

                // 프로그레스 바 업데이트
                if (_researchProgressBar != null && _data.Time > 0)
                {
                    // 남은 시간 / 전체 시간 = 남은 비율
                    // 진행률 = 1 - (남은 시간 / 전체 시간)
                    float progress = 1f - (float)(remaining / _data.Time);
                    _researchProgressBar.fillAmount = progress;
                }
            }
        }

        private void OnClick()
        {
            var state = _skillService.GetSkillState(_data.ID);

            if (state == SkillState.Unlocked)
            {
                // NEW 상태라면 확인 처리
                if (_skillService.IsNewSkill(_data.ID))
                {
                    _skillService.AcknowledgeSkill(_data.ID); // 데이터 갱신 (저장)
                    RefreshState(); // UI 갱신 (NEW 태그 끄기)
                }
                
                // 이미 해금됨
                Debug.Log($"[UI_SkillSlot] Already unlocked: {_data.Name}");
                return;
            }

            if (state == SkillState.Researching)
            {
                // 연구 중
                Debug.Log($"[UI_SkillSlot] Researching: {_data.Name}");
                return;
            }

            if (state == SkillState.Unlockable)
            {
                if (_skillService.TryUnlock(_data.ID))
                {
                    // 성공 시 전체 갱신 요청
                    _onStateChanged?.Invoke();
                }
                else
                {
                    Debug.LogWarning($"[UI_SkillSlot] Unlock failed: {_data.Name}");
                }
            }
            else
            {
                // 해금 불가
                Debug.Log($"[UI_SkillSlot] Cannot unlock yet: {_data.Name}");
            }
        }
    }
}
