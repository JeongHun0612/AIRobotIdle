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
        [SerializeField] private GameObject _lockIcon;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _costText;
        [SerializeField] private Button _button;

        [Header("Colors")]
        [SerializeField] private Color _lockedColor = Color.gray;
        [SerializeField] private Color _unlockableColor = Color.yellow;
        [SerializeField] private Color _unlockedColor = Color.white;

        private SkillRow _data;
        private ISkillService _skillService;
        private System.Action _onStateChanged;

        public SkillRow Data => _data;

        public void Initialize(SkillRow data, ISkillService skillService, System.Action onStateChanged)
        {
            _data = data;
            _skillService = skillService;
            _onStateChanged = onStateChanged;

            if (_nameText != null) _nameText.text = data.Name;
            if (_costText != null) _costText.text = data.Cost.ToString();
            if (_iconImage != null && data.Icon != null) _iconImage.sprite = data.Icon;

            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(OnClick);

            RefreshState();
        }

        public void RefreshState()
        {
            bool isUnlocked = _skillService.IsUnlocked(_data.ID);
            bool canUnlock = _skillService.CanUnlock(_data.ID);

            if (isUnlocked)
            {
                // 해금됨
                _frameImage.color = _unlockedColor;
                _lockIcon.SetActive(false);
                if (_costText != null) _costText.gameObject.SetActive(false);
            }
            else if (canUnlock)
            {
                // 해금 가능
                _frameImage.color = _unlockableColor;
                _lockIcon.SetActive(true); // 자물쇠는 켜두되 색상 등으로 구분 가능
                if (_costText != null) _costText.gameObject.SetActive(true);
            }
            else
            {
                // 잠김 (해금 불가)
                _frameImage.color = _lockedColor;
                _lockIcon.SetActive(true);
                if (_costText != null) _costText.gameObject.SetActive(true);
            }
        }

        private void OnClick()
        {
            if (_skillService.IsUnlocked(_data.ID))
            {
                // 이미 해금됨 - 정보 팝업 등을 띄울 수 있음
                Debug.Log($"[UI_SkillSlot] Already unlocked: {_data.Name}");
                return;
            }

            if (_skillService.CanUnlock(_data.ID))
            {
                if (_skillService.TryUnlock(_data.ID))
                {
                    // 성공 시 전체 갱신 요청
                    _onStateChanged?.Invoke();
                }
                else
                {
                    // 실패 (돈 부족 등 - CanUnlock 통과했으면 발생 안해야 함)
                    Debug.LogWarning($"[UI_SkillSlot] Unlock failed: {_data.Name}");
                }
            }
            else
            {
                // 해금 불가 - 선행 스킬 필요 등
                Debug.Log($"[UI_SkillSlot] Cannot unlock yet: {_data.Name}");
            }
        }
    }
}
