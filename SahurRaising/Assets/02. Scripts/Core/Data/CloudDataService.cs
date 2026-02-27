using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.Services.CloudSave;
using UnityEngine;

namespace SahurRaising.Core
{
    /// <summary>
    /// Unity Cloud Save 서비스 구현
    /// 빌드된 게임에서 플레이어 데이터를 클라우드에 저장
    /// </summary>
    public class CloudDataService : IDataService
    {
        public async UniTask SaveAsync(string key, string jsonData)
        {
            try
            {
                var data = new Dictionary<string, object>
                {
                    { key, jsonData }
                };

                await CloudSaveService.Instance.Data.Player.SaveAsync(data);
                Debug.Log($"[CloudSaveService] 저장 완료: {key}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CloudSaveService] 저장 실패 ({key}): {ex.Message}");
                throw;
            }
        }

        public async UniTask<string> LoadAsync(string key)
        {
            try
            {
                var keys = new HashSet<string> { key };
                var data = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

                if (data.TryGetValue(key, out var item))
                {
                    var json = item.Value.GetAs<string>();
                    Debug.Log($"[CloudSaveService] 로드 완료: {key}");
                    return json;
                }

                Debug.Log($"[CloudSaveService] 데이터가 없습니다: {key}");
                return null;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CloudSaveService] 로드 실패 ({key}): {ex.Message}");
                return null;
            }
        }

        public async UniTask<bool> ExistsAsync(string key)
        {
            try
            {
                var keys = new HashSet<string> { key };
                var data = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);
                return data.ContainsKey(key);
            }
            catch
            {
                return false;
            }
        }
    }
}
