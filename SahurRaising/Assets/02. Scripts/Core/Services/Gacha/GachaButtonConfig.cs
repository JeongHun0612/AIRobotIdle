using SahurRaising.Core;
using System.Collections.Generic;
using UnityEngine;

namespace SahurRaising
{
    [CreateAssetMenu(fileName = "GachaButtonConfig", menuName = "SahurRaising/Gacha/GachaButtonConfig")]
    public class GachaButtonConfig : ScriptableObject
    {
        [System.Serializable]
        public class GachaButtonData
        {
            [Header("버튼 식별")]
            [SerializeField] private string _buttonId;

            [Header("가챠 설정")]
            [SerializeField] private GachaType _gachaType;
            [SerializeField] private int _pullCount;
            [SerializeField] private double _costValue;

            [Header("UI 설정")]
            [SerializeField] private Sprite _iconSprite;

            public string ButtonId => _buttonId;
            public GachaType GachaType => _gachaType;
            public int PullCount => _pullCount;
            public double CostValue => _costValue;
            public Sprite IconSprite => _iconSprite;
        }

        [Header("장비 가챠 버튼 설정")]
        [SerializeField] private List<GachaButtonData> _equipmentButtonConfigs = new List<GachaButtonData>();

        [Header("드론 가챠 버튼 설정")]
        [SerializeField] private List<GachaButtonData> _droneButtonConfigs = new List<GachaButtonData>();

        /// <summary>
        /// 가챠 타입에 해당하는 버튼 설정 리스트를 반환합니다.
        /// </summary>
        public List<GachaButtonData> GetButtonConfigs(GachaType gachaType)
        {
            return gachaType switch
            {
                GachaType.Equipment => _equipmentButtonConfigs,
                GachaType.Drone => _droneButtonConfigs,
                _ => new List<GachaButtonData>()
            };
        }

        /// <summary>
        /// 버튼 ID로 설정을 찾습니다.
        /// </summary>
        public GachaButtonData GetButtonConfigById(string buttonId, GachaType gachaType)
        {
            var configs = GetButtonConfigs(gachaType);
            return configs?.Find(config => config.ButtonId == buttonId);
        }
    }
}
