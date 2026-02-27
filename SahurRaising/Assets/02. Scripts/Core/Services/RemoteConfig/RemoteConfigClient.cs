using Cysharp.Threading.Tasks;
using Unity.Services.Core;
using Unity.Services.RemoteConfig;
using UnityEngine;
using System;

namespace SahurRaising.Core
{
    public class RemoteConfigClient : IRemoteConfigService
    {
        public bool IsInitialized { get; private set; }

        public async UniTask InitializeAsync()
        {
            if (IsInitialized)
            {
                Debug.LogWarning("[RemoteConfigClient] 이미 초기화되었습니다.");
                return;
            }

            try
            {
                // Unity Services가 초기화되어 있는지 확인
                if (UnityServices.State != ServicesInitializationState.Initialized)
                {
                    Debug.LogError("[RemoteConfigClient] Unity Services가 초기화되지 않았습니다.");
                    return;
                }

                // Unity의 RemoteConfigService를 명시적으로 참조
                var configService = RemoteConfigService.Instance;

                // 기본 설정 (필요시 커스터마이즈)
                configService.SetEnvironmentID("production"); // 또는 "development"

                // Config 가져오기
                await configService.FetchConfigsAsync(new UserAttributes(), new AppAttributes());

                IsInitialized = true;
                Debug.Log("[RemoteConfigClient] 초기화 완료");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RemoteConfigClient] 초기화 실패: {ex.Message}");
            }
        }

        public string GetString(string key, string defaultValue = "")
        {
            if (!IsInitialized)
            {
                Debug.LogWarning($"[RemoteConfigClient] 초기화되지 않았습니다. 기본값 반환: {defaultValue}");
                return defaultValue;
            }

            try
            {
                var configService = RemoteConfigService.Instance;
                return configService.appConfig.GetString(key, defaultValue);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RemoteConfigClient] GetString 실패 ({key}): {ex.Message}");
                return defaultValue;
            }
        }

        public int GetInt(string key, int defaultValue = 0)
        {
            if (!IsInitialized)
            {
                Debug.LogWarning($"[RemoteConfigClient] 초기화되지 않았습니다. 기본값 반환: {defaultValue}");
                return defaultValue;
            }

            try
            {
                var configService = RemoteConfigService.Instance;
                return configService.appConfig.GetInt(key, defaultValue);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RemoteConfigClient] GetInt 실패 ({key}): {ex.Message}");
                return defaultValue;
            }
        }

        public float GetFloat(string key, float defaultValue = 0f)
        {
            if (!IsInitialized)
            {
                Debug.LogWarning($"[RemoteConfigClient] 초기화되지 않았습니다. 기본값 반환: {defaultValue}");
                return defaultValue;
            }

            try
            {
                var configService = RemoteConfigService.Instance;
                return configService.appConfig.GetFloat(key, defaultValue);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RemoteConfigClient] GetFloat 실패 ({key}): {ex.Message}");
                return defaultValue;
            }
        }

        public bool GetBool(string key, bool defaultValue = false)
        {
            if (!IsInitialized)
            {
                Debug.LogWarning($"[RemoteConfigClient] 초기화되지 않았습니다. 기본값 반환: {defaultValue}");
                return defaultValue;
            }

            try
            {
                var configService = RemoteConfigService.Instance;
                return configService.appConfig.GetBool(key, defaultValue);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RemoteConfigClient] GetBool 실패 ({key}): {ex.Message}");
                return defaultValue;
            }
        }

        public T GetJson<T>(string key, T defaultValue = default)
        {
            if (!IsInitialized)
            {
                Debug.LogWarning($"[RemoteConfigClient] 초기화되지 않았습니다. 기본값 반환");
                return defaultValue;
            }

            try
            {
                var configService = RemoteConfigService.Instance;
                var jsonString = configService.appConfig.GetString(key, "");

                if (string.IsNullOrEmpty(jsonString))
                {
                    Debug.LogWarning($"[RemoteConfigClient] JSON 키 '{key}'가 비어있습니다. 기본값 반환");
                    return defaultValue;
                }

                return JsonUtility.FromJson<T>(jsonString);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RemoteConfigClient] GetJson 실패 ({key}): {ex.Message}");
                return defaultValue;
            }
        }

        // Remote Config에 필요한 속성 클래스들
        private struct UserAttributes { }
        private struct AppAttributes { }
    }
}