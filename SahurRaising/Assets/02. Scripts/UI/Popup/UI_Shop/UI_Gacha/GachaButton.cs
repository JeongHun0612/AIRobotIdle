using BreakInfinity;
using SahurRaising.Core;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SahurRaising
{
    public class GachaButton : MonoBehaviour
    {
        [Header("가챠 변수")]
        [SerializeField] private GachaType _gachaType;
        [SerializeField] private int _pullCount;
        [SerializeField] private double _costValue;

        [Header("UI 요소")]
        [SerializeField] private Button _button;
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _costText;
        [SerializeField] private TMP_Text _pullCountText;
        [SerializeField] private GameObject _disabledPanel;

        private BigDouble _cost;

        private IGachaService _gachaService;
        private ICurrencyService _currencyService;

        public GachaType GachaType => _gachaType;
        public int PullCount => _pullCount;
        public BigDouble Cost => _cost;

        private void Awake()
        {
            if (_button == null)
            {
                _button = gameObject.GetComponent<Button>();
            }

            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(OnClick);

            if (_costText != null)
            {
                _costText.text = _costValue.ToString("F0");
            }

            if (_pullCountText != null)
            {
                _pullCountText.text = $"{_pullCount} Count";
            }

            _cost = new BigDouble(_costValue);
        }

        public void Refresh(GachaType type = GachaType.None)
        {
            if (!TryBindService())
                return;

            if (type != GachaType.None)
            {
                _gachaType = type;
            }    

            UpdateButtonState();
        }

        private bool TryBindService()
        {
            if (_gachaService == null && ServiceLocator.HasService<IGachaService>())
            {
                _gachaService = ServiceLocator.Get<IGachaService>();
            }

            if (_currencyService == null && ServiceLocator.HasService<ICurrencyService>())
            {
                _currencyService = ServiceLocator.Get<ICurrencyService>();
            }

            return _gachaService != null && _currencyService != null;
        }

        private void UpdateButtonState()
        {
            if (_button == null)
                return;

            var currencyType = _gachaService.GetCurrencyType(_gachaType);
            var balance = _currencyService.Get(currencyType);
            bool isInteractable = balance >= _cost;

            _button.interactable = isInteractable;

            // 비활성화 이미지 표시/숨김
            if (_disabledPanel != null)
            {
                _disabledPanel.SetActive(!isInteractable);
            }
        }

        public void OnClick()
        {
            if (_gachaService == null)
                _gachaService = ServiceLocator.Get<IGachaService>();

            if (_gachaService == null || !_gachaService.IsInitialized)
            {
                Debug.LogWarning("[GachaButton] GachaService를 찾을 수 없거나 초기화되지 않았습니다.");
                return;
            }

            // 가챠 실행
            _gachaService.Pull(_gachaType, _pullCount, _cost);

            // UI 갱신
            Refresh();
        }
    }
}
