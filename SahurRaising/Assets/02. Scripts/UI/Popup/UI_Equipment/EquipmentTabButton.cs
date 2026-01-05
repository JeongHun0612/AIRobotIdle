using SahurRaising.Core;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace SahurRaising
{
    public class EquipmentTabButton : MonoBehaviour
    {
        [SerializeField] private EquipmentType _type;

        [SerializeField] private Button _button;
        [SerializeField] private GameObject _tabFocus;

        public EquipmentType Type  => _type;

        public void Register(Action<EquipmentType> callback)
        {
            if (_button == null)
                _button = gameObject.GetComponent<Button>();

            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => callback?.Invoke(_type));
        }

        public void OnShow(bool isShow)
        {
            _tabFocus.SetActive(isShow);
        }
    }
}
