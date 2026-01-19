using BreakInfinity;
using SahurRaising.Core;
using UnityEngine;

namespace SahurRaising
{
    /// <summary>
    /// Debug용 재화 컨트롤 ScriptableObject
    /// 에디터에서 재화를 추가/소모할 수 있도록 하는 디버그 도구
    /// </summary>
    [CreateAssetMenu(fileName = "DebugCurrencySettings", menuName = "SahurRaising/Debug/DebugCurrencySettings")]
    public class DebugCurrencySettings : ScriptableObject
    {
        [Header("재화 추가/소모 설정")]
        [Tooltip("추가할 재화 타입")]
        public CurrencyType AddCurrencyType = CurrencyType.Gold;

        [Tooltip("추가할 재화 양 (문자열 형태: \"1.23e45\" 또는 \"1000\")")]
        public string AddAmount = "1000";

        [Tooltip("소모할 재화 타입")]
        public CurrencyType ConsumeCurrencyType = CurrencyType.Gold;

        [Tooltip("소모할 재화 양 (문자열 형태: \"1.23e45\" 또는 \"1000\")")]
        public string ConsumeAmount = "100";

        [Header("재화 설정 (현재 보유량 설정)")]
        [Tooltip("Gold 보유량")]
        public string GoldAmount = "0";

        [Tooltip("Emerald 보유량")]
        public string EmeraldAmount = "0";

        [Tooltip("Diamond 보유량")]
        public string DiamondAmount = "0";

        [Tooltip("Ticket 보유량")]
        public string TicketAmount = "0";

        [Tooltip("Ruby 보유량")]
        public string RubyAmount = "0";

        /// <summary>
        /// 재화 타입에 따른 보유량 문자열 반환
        /// </summary>
        public string GetAmount(CurrencyType type)
        {
            return type switch
            {
                CurrencyType.Gold => GoldAmount,
                CurrencyType.Emerald => EmeraldAmount,
                CurrencyType.Diamond => DiamondAmount,
                CurrencyType.Ticket => TicketAmount,
                CurrencyType.Ruby => RubyAmount,
                _ => "0"
            };
        }

        /// <summary>
        /// 재화 타입에 따른 보유량 설정
        /// </summary>
        public void SetAmount(CurrencyType type, string amount)
        {
            switch (type)
            {
                case CurrencyType.Gold:
                    GoldAmount = amount;
                    break;
                case CurrencyType.Emerald:
                    EmeraldAmount = amount;
                    break;
                case CurrencyType.Diamond:
                    DiamondAmount = amount;
                    break;
                case CurrencyType.Ticket:
                    TicketAmount = amount;
                    break;
                case CurrencyType.Ruby:
                    RubyAmount = amount;
                    break;
            }
        }

        /// <summary>
        /// BigDouble로 변환
        /// </summary>
        public BigDouble ParseBigDouble(string value)
        {
            if (string.IsNullOrEmpty(value))
                return BigDouble.Zero;

            try
            {
                return BigDouble.Parse(value);
            }
            catch
            {
                return BigDouble.Zero;
            }
        }
    }
}