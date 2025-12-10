using Cysharp.Threading.Tasks;
using SahurRaising.Core;
using SahurRaising.UI;
using UnityEngine;

namespace SahurRaising.Core
{
    public class GameManager : MonoSingleton<GameManager>
    {
        private void Start()
        {
            InitializeGameAsync().Forget();
        }

        private async UniTaskVoid InitializeGameAsync()
        {
            try
            {
                Debug.Log("[GameManager] 게임 초기화 시작...");

                await ServiceRegisterAsync();
                await UIManager.Instance.InitializeAsync();

                // UIManager 초기화가 완료된 후 Title 씬 표시
                if (UIManager.Instance.IsInitialized)
                {
                    Debug.Log("[GameManager] 타이틀 UI 표시 중...");
                    UIManager.Instance.ShowScene(ESceneUIType.Title);
                    Debug.Log("[GameManager] 타이틀 UI 표시 완료");
                }
                else
                {
                    Debug.LogError("UIManager 초기화가 완료되지 않았습니다.");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GameManager] 게임 초기화 중 오류 발생: {ex.Message}");
                Debug.LogError($"[GameManager] 스택 트레이스: {ex.StackTrace}");
            }
        }

        private async UniTask ServiceRegisterAsync()
        {
            Debug.Log("[GameManager] 서비스 등록 시작...");

            // 1. 기본 서비스들 등록
            var resourceManager = new ResourceManager();
            resourceManager.Initialize();
            ServiceLocator.Register<IResourceService, ResourceManager>(resourceManager);

            var eventBus = new EventBus();
            eventBus.Initialize();
            ServiceLocator.Register<IEventBus, EventBus>(eventBus);

            // Localization 서비스 등록 (UI에서 공통 사용 - 다국어 대응)
            var localizationService = new UnityLocalizationService();
            ServiceLocator.Register<ILocalizationService, UnityLocalizationService>(localizationService);

            // 설정에 저장된 로캘 적용
            //if (!string.IsNullOrEmpty(settingsService.LocaleCode))
            //    localizationService.SetLocale(settingsService.LocaleCode);

            // TODO 서비스 등록 추가

            Debug.Log("[GameManager] 서비스 등록 완료");





        }

        private async void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                await SaveAllAsync();
            }
        }

        private async void OnApplicationQuit()
        {
            await SaveAllAsync();
        }

        private async UniTask SaveAllAsync()
        {
            // 등록 여부를 검사하고 저장을 호출
            //if (ServiceLocator.HasService<ISettingsService>())
            //    await ServiceLocator.Get<ISettingsService>().SaveAsync();
            //if (ServiceLocator.HasService<ICurrencyService>())
            //    await ServiceLocator.Get<ICurrencyService>().SaveDataAsync();
            //if (ServiceLocator.HasService<IGrowthActionService>())
            //    await ServiceLocator.Get<IGrowthActionService>().SaveDataAsync();
            //if (ServiceLocator.HasService<ICollectionService>())
            //    await ServiceLocator.Get<ICollectionService>().SaveDataAsync();
            //if (ServiceLocator.HasService<IEvolutionService>())
            //    await ServiceLocator.Get<IEvolutionService>().SaveDataAsync();
            //if (ServiceLocator.HasService<IStageService>())
            //    await ServiceLocator.Get<IStageService>().SaveDataAsync();
            //TODO 저장로직
        }
    }
}


