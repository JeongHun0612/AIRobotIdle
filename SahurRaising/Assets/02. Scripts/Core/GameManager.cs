using System;
using Cysharp.Threading.Tasks;
using SahurRaising.Core;
using SahurRaising.UI;
using UnityEngine;

namespace SahurRaising.Core
{
    public class GameManager : MonoSingleton<GameManager>
    {
        [Header("Loading")]
        [SerializeField, Min(0f)] private float _minLoadingSeconds = 2f;
        private void Start()
        {
            InitializeGameAsync().Forget();
        }

        private async UniTaskVoid InitializeGameAsync()
        {
            try
            {
                Debug.Log("[GameManager] 게임 초기화 시작...");

                await UIManager.Instance.InitializeAsync();

                Debug.Log($"[GameManager] UIManager 초기화 상태: {UIManager.Instance.IsInitialized}");

                if (UIManager.Instance.IsInitialized)
                {
                    Debug.Log("[GameManager] Loading Scene 표시 시도...");
                    var loadingScene = UIManager.Instance.ShowScene<UILoadingScene>(ESceneUIType.Loading);
                    
                    if (loadingScene == null)
                    {
                        Debug.LogError("[GameManager] 로딩 UI를 표시할 수 없습니다. UIRegistry에 Loading 씬을 등록했는지 확인하세요.");
                        
                        // 추가 디버깅 정보 출력
                        Debug.LogError($"[GameManager] 현재 등록된 씬 타입 확인 필요: {ESceneUIType.Loading}");
                        return;
                    }
                    
                    Debug.Log($"[GameManager] Loading Scene 표시 성공: {loadingScene.name}");
                    loadingScene.SetProgress(0f);

                    await RunInitializationWithProgressAsync(loadingScene);

                    // 페이드와 함께 메인 전투 씬 전환
                    await UIManager.Instance.ShowSceneWithFadeAsync(ESceneUIType.MainBattle);
                    Debug.Log("[GameManager] 메인 전투 UI 표시 완료");
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

        /// <summary>
        /// 초기화 순서를 실행하며 로딩 씬에 진행도를 보고한다.
        /// </summary>
        private async UniTask RunInitializationWithProgressAsync(UILoadingScene loadingScene)
        {
            float startTime = Time.realtimeSinceStartup;

            // 서비스 등록: 0~0.85
            var serviceProgress = new Progress<float>(value =>
            {
                float mapped = Mathf.Lerp(0f, 0.85f, Mathf.Clamp01(value));
                loadingScene.SetProgress(mapped);
            });
            await ServiceRegisterAsync(serviceProgress);

            // 추가 초기화가 필요하면 여기(0.85~1)에서 처리
            loadingScene.SetProgress(0.95f);

            // 최소 로딩 시간 보장
            float elapsed = Time.realtimeSinceStartup - startTime;
            if (elapsed < _minLoadingSeconds)
                await UniTask.Delay(TimeSpan.FromSeconds(_minLoadingSeconds - elapsed));

            loadingScene.SetProgress(1f);
        }

        private async UniTask ServiceRegisterAsync(IProgress<float> progress = null)
        {
            Debug.Log("[GameManager] 서비스 등록 시작...");

            int step = 0;
            const int totalSteps = 7;
            void Report() => progress?.Report((float)step / totalSteps);

            // 1. 기본 서비스들 등록
            var resourceManager = new ResourceManager();
            resourceManager.Initialize();
            ServiceLocator.Register<IResourceService, ResourceManager>(resourceManager);
            step++; Report();

            var eventBus = new EventBus();
            eventBus.Initialize();
            ServiceLocator.Register<IEventBus, EventBus>(eventBus);
            step++; Report();

            // Localization 서비스 등록 (UI에서 공통 사용 - 다국어 대응)
            var localizationService = new UnityLocalizationService();
            ServiceLocator.Register<ILocalizationService, UnityLocalizationService>(localizationService);
            step++; Report();

            // 설정에 저장된 로캘 적용
            //if (!string.IsNullOrEmpty(settingsService.LocaleCode))
            //    localizationService.SetLocale(settingsService.LocaleCode);

            // 전투/스탯/재화 서비스 등록
            var statService = new StatService(resourceManager, eventBus);
            ServiceLocator.Register<IStatService, StatService>(statService);
            await statService.InitializeAsync();
            step++; Report();

            var currencyService = new CurrencyService(eventBus, statService);
            ServiceLocator.Register<ICurrencyService, CurrencyService>(currencyService);
            await currencyService.InitializeAsync();
            step++; Report();

            var upgradeService = new UpgradeService(resourceManager, currencyService, statService);
            ServiceLocator.Register<IUpgradeService, UpgradeService>(upgradeService);
            await upgradeService.InitializeAsync();
            step++; Report();

            var combatService = new CombatService(resourceManager, eventBus, statService, currencyService);
            ServiceLocator.Register<ICombatService, CombatService>(combatService);
            await combatService.InitializeAsync();
            step++; Report();

            Debug.Log("[GameManager] 서비스 등록 완료");
            progress?.Report(1f);





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
            if (ServiceLocator.HasService<ICurrencyService>())
                await ServiceLocator.Get<ICurrencyService>().SaveAsync();
            if (ServiceLocator.HasService<IUpgradeService>())
                await ServiceLocator.Get<IUpgradeService>().SaveAsync();
            if (ServiceLocator.HasService<ICombatService>())
                await ServiceLocator.Get<ICombatService>().SaveAsync();
            Debug.Log("[GameManager] SaveAllAsync 완료");
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


