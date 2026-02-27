using Cysharp.Threading.Tasks;
using Unity.Services.CloudCode;
using UnityEngine;
using Newtonsoft.Json;
using System.Collections.Generic;
using Unity.Services.Core;
using Unity.Services.Authentication;

namespace SahurRaising.Core
{
    public interface ICloudCodeService
    {
        public bool IsInitialized { get; }
        void Initialize();
        UniTask<TResponse> CallFunctionAsync<TRequest, TResponse>(
            string functionName,
            TRequest request) where TResponse : class;
    }

    public class CloudCodeClient : ICloudCodeService
    {
        public bool IsInitialized { get; private set; }

        public void Initialize()
        {
            if (IsInitialized)
            {
                Debug.LogWarning("[CloudCodeClient] 이미 초기화되었습니다.");
                return;
            }

            // Unity Services가 이미 초기화되어 있는지 확인
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                Debug.LogError("[CloudCodeClient] Unity Services가 초기화되지 않았습니다. GameManager에서 먼저 초기화하세요.");
                return;
            }

            // CloudCodeService 인스턴스 확인
            if (CloudCodeService.Instance == null)
            {
                Debug.LogError("[CloudCodeClient] CloudCodeService.Instance가 null입니다.");
                return;
            }

            IsInitialized = true;
            Debug.Log("[CloudCodeClient] 초기화 완료");
        }

        public async UniTask<TResponse> CallFunctionAsync<TRequest, TResponse>(
            string functionName,
            TRequest request) where TResponse : class
        {
            if (!IsInitialized)
            {
                Debug.LogError("[CloudCodeClient] 초기화되지 않았습니다. InitializeAsync()를 먼저 호출하세요.");
                return null;
            }

            try
            {
                // CloudCodeService 인스턴스 확인
                if (CloudCodeService.Instance == null)
                {
                    Debug.LogError("[CloudCodeClient] CloudCodeService.Instance가 null입니다.");
                    return null;
                }

                // TRequest를 Dictionary<string, object>로 변환
                var requestJson = JsonConvert.SerializeObject(request);
                var parameters = JsonConvert.DeserializeObject<Dictionary<string, object>>(requestJson);

                if (parameters == null)
                {
                    Debug.LogError("[CloudCodeClient] 파라미터 변환 실패");
                    return null;
                }

                // Unity의 CloudCodeService 호출
                var response = await CloudCodeService.Instance.CallEndpointAsync(functionName, parameters);

                if (response == null)
                {
                    Debug.LogError($"[CloudCodeClient] {functionName} 응답이 null입니다.");
                    return null;
                }

                // Response를 JSON 문자열로 변환 후 역직렬화
                string responseJson = response is string str ? str : JsonConvert.SerializeObject(response);
                return JsonConvert.DeserializeObject<TResponse>(responseJson);
            }
            catch (CloudCodeException ex)
            {
                Debug.LogError($"[CloudCodeClient] {functionName} 호출 실패: {ex.Message}");
                return null;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CloudCodeClient] {functionName} 예외 발생: {ex.Message}");
                return null;
            }
        }
    }
}