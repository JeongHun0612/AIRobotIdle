using System;
using System.Collections.Generic;
using UnityEngine;

namespace SahurRaising.Core
{
    [Serializable]
    public struct CurrencyData
    {
        public CurrencyType Type;
        public string Name;
        public Sprite Icon;
        [TextArea] public string Description;
    }

    [CreateAssetMenu(fileName = "CurrencyTable", menuName = "SahurRaising/Data/CurrencyTable")]
    public class CurrencyTable : TableBase<CurrencyType, CurrencyData>
    {
        protected override CurrencyType GetKey(CurrencyData value) => value.Type;

        public Sprite GetIcon(CurrencyType type)
        {
            if (Index.TryGetValue(type, out var data))
            {
                return data.Icon;
            }
            return null;
        }

        public string GetName(CurrencyType type)
        {
            if (Index.TryGetValue(type, out var data))
            {
                return data.Name;
            }
            return type.ToString();
        }
    }
}
