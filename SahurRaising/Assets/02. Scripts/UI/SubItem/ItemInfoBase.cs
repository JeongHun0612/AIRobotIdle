using SahurRaising.Core;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace SahurRaising
{
    /// <summary>
    /// 아이템 정보 표시의 공통 기능을 제공하는 추상 부모 클래스
    /// </summary>
    public abstract class ItemInfoBase<TData, IService> : MonoBehaviour
    {
        [Header("아이템 장착 스탯")]
        [SerializeField] protected OptionStatPanel _equipOptionStatPanel;

        [Header("장착 버튼")]
        [SerializeField] protected Button _equipButton;
        [SerializeField] protected GameObject _equipButtonPanel;
        [SerializeField] protected GameObject _unEquipButtonPanel;

        [Header("레벨업 버튼")]
        [SerializeField] protected Button _levelUpButton;

        protected IService _service;
        protected TData _currentData;
        protected Action _onEquipChanged; // 갱신 콜백

        public virtual void Initialize(Action onEquipChanged = null)
        {
            _onEquipChanged = onEquipChanged;

            if (_equipButton != null)
            {
                _equipButton.onClick.RemoveAllListeners();
                _equipButton.onClick.AddListener(OnClickEquip);
            }

            if (_levelUpButton != null)
            {
                _levelUpButton.onClick.RemoveAllListeners();
                _levelUpButton.onClick.AddListener(OnClickLevelUp);
            }
        }

        protected virtual bool TryBindService()
        {
            if (_service == null && ServiceLocator.HasService<IService>())
            {
                _service = ServiceLocator.Get<IService>();
            }

            return _service != null;
        }

        protected virtual bool IsValidData()
        {
            return _currentData != null && !string.IsNullOrEmpty(GetItemID());
        }

        public virtual void UpdateItemInfo(TData data)
        {
            _currentData = data;

            if (!TryBindService() || !IsValidData())
                return;

            // ItemSlot 업데이트
            UpdateItemSlot(data);

            // 레벨 가져오기
            int level = GetItemLevel();

            // 장착 스탯 설정
            UpdateEquipOptionStat(data, level);

            // 보유 스탯 설정
            UpdateHeldOptionStats(data, level);

            // 장착 버튼 상태 업데이트
            UpdateEquipButtonState();
        }

        public void OnClickEquip()
        {
            if (!TryBindService() || !IsValidData())
                return;

            // 현재 장착 상태 확인
            bool isEquipped = GetIsEquipped();

            // 장착/해제 처리
            if (isEquipped)
            {
                Unequip();
            }
            else
            {
                Equip();
            }

            // 버튼 상태 업데이트
            UpdateEquipButtonState();

            // UI 갱신 요청
            _onEquipChanged?.Invoke();
        }

        public void OnClickLevelUp()
        {
            if (!TryBindService() || !IsValidData())
                return;

            // 레벨업 수행
            bool success = LevelUp();

            if (success)
            {
                // UI 갱신
                UpdateItemInfo(_currentData);

                // 인벤토리 갱신
                _onEquipChanged?.Invoke();
            }
        }

        protected virtual void UpdateEquipButtonState()
        {
            if (_equipButton == null || !IsValidData())
                return;

            // 현재 장착 상태 확인
            bool isEquipped = GetIsEquipped();

            // 버튼 업데이트
            UpdateEquipButtonUI(isEquipped);
        }

        protected virtual void UpdateEquipButtonUI(bool isEquipped)
        {
            if (_equipButtonPanel != null)
                _equipButtonPanel.SetActive(!isEquipped);

            if (_unEquipButtonPanel != null)
                _unEquipButtonPanel.SetActive(isEquipped);
        }

        protected abstract string GetItemID();
        protected abstract int GetItemLevel();
        protected abstract void UpdateItemSlot(TData data);
        protected abstract void UpdateEquipOptionStat(TData data, int level);
        protected abstract void UpdateHeldOptionStats(TData data, int level);
        protected abstract bool GetIsEquipped();
        protected abstract void Equip();
        protected abstract void Unequip();
        protected abstract bool LevelUp();
    }
}