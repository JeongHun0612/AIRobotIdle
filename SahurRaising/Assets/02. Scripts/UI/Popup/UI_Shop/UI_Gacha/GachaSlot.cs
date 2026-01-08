using Cysharp.Threading.Tasks;
using DG.Tweening;
using SahurRaising.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SahurRaising
{
    public class GachaSlot : MonoBehaviour
    {
        [Header("UI 요소")]
        [SerializeField] private RectTransform _rectTransform;
        [SerializeField] private Image _bgImage;
        [SerializeField] private Image _iconImage;
        [SerializeField] private Image _typeIcon;
        [SerializeField] private TMP_Text _gradeText;

        private GachaResult _result;

        private IGachaService _gachaService;
        private IEquipmentService _equipmentService;

        /// <summary>
        /// 아이템 정보를 담는 구조체
        /// </summary>
        private struct ItemInfo
        {
            public Sprite Icon;
            public string GradeKey;  // 색상 조회용 키
            public string TypeKey;   // 타입 아이콘 조회용 키
        }

        public void SetData(GachaResult result)
        {
            _result = result;
            UpdateUI();
        }

        /// <summary>
        /// 슬롯을 초기 상태로 리셋합니다
        /// </summary>
        public void OnReset()
        {
            if (_rectTransform != null)
            {
                _rectTransform.DOKill();
                _rectTransform.localScale = Vector3.zero;
            }
        }

        /// <summary>
        /// 슬롯을 최종 상태로 즉시 설정합니다 (애니메이션 스킵)
        /// </summary>
        public void SetFinalState()
        {
            if (_rectTransform != null)
            {
                _rectTransform.DOKill();
                _rectTransform.localScale = Vector3.one;
            }
        }

        /// <summary>
        /// 스케일 애니메이션으로 슬롯을 표시합니다
        /// </summary>
        public Tween ShowWithAnimation(float delay = 0f, float duration = 0.3f)
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            if (_rectTransform == null)
                return null;

            // 기존 트윈이 있으면 정리
            _rectTransform.DOKill();

            // 초기 스케일을 0으로 설정
            _rectTransform.localScale = Vector3.zero;

            // 딜레이 후 스케일 애니메이션 (0 -> 1)
            return _rectTransform.DOScale(Vector3.one, duration)
                .SetDelay(delay)
                .SetEase(Ease.OutBack);
        }

        private void UpdateUI()
        {
            if (string.IsNullOrEmpty(_result.ItemCode))
                return;

            if (_gachaService == null)
                _gachaService = ServiceLocator.Get<IGachaService>();

            if (_equipmentService == null)
                _equipmentService = ServiceLocator.Get<IEquipmentService>();


            // 타입별로 정보 추출 (각 타입의 특성에 맞게)
            ItemInfo? itemInfo = GetItemInfo(_result);
            if (itemInfo == null)
                return;

            // 공통 UI 업데이트 로직
            UpdateUIElements(itemInfo.Value);
        }

        private void UpdateUIElements(ItemInfo itemInfo)
        {
            var gradeColorConfig = _gachaService.GradeColorConfig;

            if (gradeColorConfig == null)
                return;

            // 배경 색깔 설정
            if (_bgImage != null)
            {
                _bgImage.color = gradeColorConfig.GetColorForGrade(_result.Type, itemInfo.GradeKey);
            }

            // 아이콘 설정
            if (_iconImage != null)
            {
                _iconImage.sprite = itemInfo.Icon;
                _iconImage.color = (itemInfo.Icon == null) ? Color.clear : Color.white;
            }

            // 등급 텍스트 설정
            if (_gradeText != null)
            {
                _gradeText.text = itemInfo.GradeKey;
            }

            // 타입 아이콘 설정
            if (_typeIcon != null)
            {
                var typeIconSprite = gradeColorConfig.GetTypeIcon(_result.Type, itemInfo.TypeKey);
       
                _typeIcon.sprite = typeIconSprite;
                _typeIcon.gameObject.SetActive(typeIconSprite != null);
            }
        }

        private ItemInfo? GetItemInfo(GachaResult result)
        {
            switch (result.Type)
            {
                case GachaType.Equipment:
                    return GetEquipmentInfo(result.ItemCode);

                case GachaType.Drone:
                    return GetDroneInfo(result.ItemCode);

                default:
                    Debug.LogWarning($"[GachaSlot] 지원하지 않는 가챠 타입: {result.Type}");
                    return null;
            }
        }

        private ItemInfo? GetEquipmentInfo(string itemCode)
        {
            if (_equipmentService == null || !_equipmentService.TryGetByCode(itemCode, out EquipmentRow equipment))
            {
                Debug.LogWarning($"[GachaSlot] 장비 코드를 찾을 수 없습니다: {itemCode}");
                return null;
            }

            return new ItemInfo
            {
                Icon = equipment.Icon,
                GradeKey = equipment.Grade.ToString(),
                TypeKey = equipment.Type.ToString()
            };
        }

        private ItemInfo? GetDroneInfo(string itemCode)
        {
            // TODO 추후 드론서비스로 처리

            return new ItemInfo
            {
                Icon = null, // TODO: 드론 아이콘 추가 시
                GradeKey = "",
                TypeKey = "" // TODO: 드론 타입이 생기면 설정
            };
        }
    }
}
