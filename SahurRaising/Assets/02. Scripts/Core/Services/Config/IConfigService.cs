using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SahurRaising.Core
{
    /// <summary>
    /// 공통으로 사용되는 Config(ScriptableObject) 접근을 제공하는 서비스.
    /// </summary>
    public interface IConfigService
    {
        UniTask InitializeAsync();
        bool IsInitialized { get; }

        ItemVisualConfig ItemVisualConfig { get; }

        Color GetColorForGrade(GachaType gachaType, string gradeKey);
        Sprite GetTypeIcon(GachaType gachaType, string typeKey);
    }
}

