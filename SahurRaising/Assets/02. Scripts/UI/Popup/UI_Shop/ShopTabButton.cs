using SahurRaising.Core;
using SahurRaising.UI;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace SahurRaising
{
    public class ShopTabButton : MonoBehaviour
    {
        [SerializeField] private EPopupUIType _type;
        [SerializeField] private bool _isAlret;
        [SerializeField] private bool _isDisabled;

        [Header("UI 요소")]
        [SerializeField] private Button _button;
        [SerializeField] private GameObject _tabFocus;

        [SerializeField] private GameObject _alretObj;
        [SerializeField] private GameObject _disableObj;

        public EPopupUIType Type => _type;

        public void Initialize()
        {
            _alretObj.SetActive(_isAlret);
            _disableObj.SetActive(_isDisabled);

            if (_button == null)
                _button = gameObject.GetComponent<Button>();

            _button.enabled = !_isDisabled;
        }

        public void Register(Action<EPopupUIType> callback)
        {
            if (_button != null)
            {
                _button.onClick.RemoveAllListeners();
                _button.onClick.AddListener(() => callback?.Invoke(_type));
            }
        }

        public void OnShow(bool isShow)
        {
            _tabFocus.SetActive(isShow);
        }
    }
}
