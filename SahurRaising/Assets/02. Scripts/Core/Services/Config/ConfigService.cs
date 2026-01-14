using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SahurRaising.Core
{
    /// <summary>
    /// 공통 Config(ScriptableObject)를 로드/제공하는 서비스.
    /// </summary>
    public class ConfigService : IConfigService
    {
        private const string ITEM_VISUAL_CONFIG_KEY = "ItemVisualConfig";

        private readonly IResourceService _resourceService;

        public bool IsInitialized { get; private set; }
        public ItemVisualConfig ItemVisualConfig { get; private set; }

        public ConfigService(IResourceService resourceService)
        {
            _resourceService = resourceService;
        }

        public async UniTask InitializeAsync()
        {
            if (IsInitialized)
            {
                Debug.LogWarning("[ConfigService] 이미 초기화되었습니다.");
                return;
            }

            if (_resourceService == null)
            {
                Debug.LogError("[ConfigService] IResourceService가 null입니다.");
                return;
            }

            ItemVisualConfig = await _resourceService.LoadAssetAsync<ItemVisualConfig>(ITEM_VISUAL_CONFIG_KEY);
            if (ItemVisualConfig == null)
            {
                Debug.LogError("[ConfigService] ItemVisualConfig 로드 실패");
                return;
            }

            IsInitialized = true;
        }

        public Color GetColorForGrade(GachaType gachaType, string gradeKey)
        {
            if (ItemVisualConfig == null)
                return Color.white;

            return ItemVisualConfig.GetColorForGrade(gachaType, gradeKey);
        }

        public Sprite GetTypeIcon(GachaType gachaType, string typeKey)
        {
            if (ItemVisualConfig == null)
                return null;

            return ItemVisualConfig.GetTypeIcon(gachaType, typeKey);
        }
    }
}

