using SahurRaising.Core;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace SahurRaising
{
    public class ShopTabButton : MonoBehaviour
    {
        [SerializeField] private ShopType _type;
        [SerializeField] private string _titleText;

        [SerializeField] private Image _image;
        [SerializeField] private Button _button;

        [SerializeField] private GameObject _alretIcon;
        [SerializeField] private GameObject _lockIcon;

        [SerializeField] private bool _isAlret;
        [SerializeField] private bool _isLock;

        [Header("활성화/비활성화 컬러")]
        [SerializeField] private Color _normalColor;
        [SerializeField] private Color _selectedColor;

        public ShopType Type => _type;
        public string TitleText => _titleText;
        public bool IsLock => _isLock;

        public void Initialize()
        {
            _alretIcon.SetActive(_isAlret);
            _lockIcon.SetActive(_isLock);

            _button.enabled = !_isLock;
        }

        public void Register(Action<ShopType> callback)
        {
            if (_button == null)
                _button = gameObject.GetComponent<Button>();

            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => callback?.Invoke(_type));
        }

        public void OnShow(bool isShow)
        {
            _image.color = (isShow) ? _selectedColor : _normalColor;
        }
    }
}
