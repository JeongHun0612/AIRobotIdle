using BreakInfinity;
using SahurRaising.Core;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SahurRaising
{
    public class GachaButton : MonoBehaviour
    {
        [System.Serializable]
        private class GachaButtonItem
        {
            public Image _iconImage;
            public TMP_Text costText;
            public TMP_Text pullCountText;
        }

        [Header("가챠 변수")]
        [SerializeField] private GachaType _gachaType;
        [SerializeField] private int _pullCount;
        [SerializeField] private double _costValue;

        [Header("UI 요소")]
        [SerializeField] private Button _button;
        [SerializeField] private GameObject _disabledPanel;

        [SerializeField] private List<GachaButtonItem> _items = new();

        private BigDouble _cost;

        private IGachaService _gachaService;
        private ICurrencyService _currencyService;
        private ICloudCodeService _cloudCodeService;

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

            foreach (var item in _items)
            {
                item.costText.text = _costValue.ToString("F0");
                item.pullCountText.text = $"{_pullCount} Count";
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

            if (_cloudCodeService == null && ServiceLocator.HasService<ICloudCodeService>())
            {
                _cloudCodeService = ServiceLocator.Get<ICloudCodeService>();
            }

            return _gachaService != null && _currencyService != null && _cloudCodeService != null;
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

        public async void OnClick()
        {
            //if (_cloudCodeService == null)
            //    _cloudCodeService = ServiceLocator.Get<ICloudCodeService>();

            //if (_cloudCodeService == null)
            //{
            //    Debug.LogWarning("[GachaButton] CloudCodeService를 찾을 수 없습니다.");
            //    return;
            //}

            //// RollDice Cloud Code 함수 호출 테스트
            //try
            //{
            //    Debug.Log("[GachaButton] RollDice Cloud Code 함수 호출 시작...");

            //    var request = new RollDiceRequest();
            //    var response = await _cloudCodeService.CallFunctionAsync<RollDiceRequest, RollDiceResponse>(
            //        "RollDice",
            //        request);

            //    if (response != null)
            //    {
            //        Debug.Log($"[GachaButton] RollDice 결과: sides={response.sides}, roll={response.roll}");
            //    }
            //    else
            //    {
            //        Debug.LogError("[GachaButton] RollDice 응답이 null입니다.");
            //    }
            //}
            //catch (System.Exception ex)
            //{
            //    Debug.LogError($"[GachaButton] RollDice 호출 중 오류 발생: {ex.Message}");
            //}


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
