using BreakInfinity;
using SahurRaising.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SahurRaising
{
    public class GachaButton : MonoBehaviour
    {
        [Header("뽑기 설정")]
        [SerializeField] private GachaType _gachaType;
        [SerializeField] private int _drawCount;
        [SerializeField] private double _costValue;
        [SerializeField] private CurrencyType _currencyType;

        [Header("UI 요소")]
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _costText;
        [SerializeField] private TMP_Text _drawCountText;
        [SerializeField] private GameObject _disabledPanel;

        private BigDouble _cost;

        private IGachaService _gachaService;
        private ICurrencyService _currencyService;

        public GachaType GachaType => _gachaType;
        public int DrawCount => _drawCount;
        public BigDouble Cost => _cost;
        public CurrencyType CurrencyType => _currencyType;

        public void Initialize()
        {
            TryBindService();

            // 버튼 클릭 이벤트 등록
            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(OnClick);
            }

            if (_costText != null)
            {
                _costText.text = _costValue.ToString("F0");
            }

            if (_drawCountText != null)
            {
                _drawCountText.text = $"{_drawCount} Count";
            }

            if (_disabledPanel != null)
            {
                _disabledPanel.SetActive(false);
            }

            _cost = new BigDouble(_costValue);

            // 초기 UI 갱신
            Refresh();
        }

        public void Refresh()
        {
            if (!TryBindService())
                return;

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

            if (_currencyService == null)
                _currencyService = ServiceLocator.Get<ICurrencyService>();

            var balance = _currencyService.Get(_currencyType);
            bool isInteractable = balance >= _cost;

            _button.interactable = isInteractable;

            // 비활성화 이미지 표시/숨김
            if (_disabledPanel != null)
            {
                _disabledPanel.SetActive(!isInteractable);
            }
        }

        public async void OnClick()
        {
            if (_gachaService == null)
                _gachaService = ServiceLocator.Get<IGachaService>();

            if (_gachaService == null || !_gachaService.IsInitialized)
            {
                Debug.LogWarning("[GachaButton] GachaService를 찾을 수 없거나 초기화되지 않았습니다.");
                return;
            }

            Debug.Log($"[GachaButton] 가챠 시작 - Type: {_gachaType}, Count: {_drawCount}, Cost: {_costValue}");

            // 가챠 실행
            await _gachaService.DrawAsync(_gachaType, _drawCount, _cost, _currencyType);

            // UI 갱신
            Refresh();
        }
    }
}
