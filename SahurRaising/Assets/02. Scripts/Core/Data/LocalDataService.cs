using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SahurRaising.Core
{
    /// <summary>
    /// 로컬 파일 저장 서비스 (에디터/테스트용)
    /// Application.persistentDataPath에 JSON 파일로 저장
    public class LocalDataService : IDataService
    {
        public async UniTask SaveAsync(string key, string jsonData)
        {
            try
            {
                var path = GetSavePath(key);
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await File.WriteAllTextAsync(path, jsonData);
                Debug.Log($"[LocalFileSaveService] 저장 완료: {key} -> {path}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[LocalFileSaveService] 저장 실패 ({key}): {ex.Message}");
                throw;
            }
        }

        public async UniTask<string> LoadAsync(string key)
        {
            try
            {
                var path = GetSavePath(key);
                if (!File.Exists(path))
                {
                    Debug.Log($"[LocalFileSaveService] 파일이 없습니다: {key}");
                    return null;
                }

                var json = await File.ReadAllTextAsync(path);
                Debug.Log($"[LocalFileSaveService] 로드 완료: {key}");
                return json;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[LocalFileSaveService] 로드 실패 ({key}): {ex.Message}");
                return null;
            }
        }

        public async UniTask<bool> ExistsAsync(string key)
        {
            try
            {
                var path = GetSavePath(key);
                return File.Exists(path);
            }
            catch
            {
                return false;
            }
        }

        private string GetSavePath(string key)
        {
            // key가 이미 .json 확장자를 포함하는 경우와 아닌 경우 모두 처리
            var fileName = key.EndsWith(".json") ? key : $"{key}.json";
            return Path.Combine(Application.persistentDataPath, fileName);
        }
    }
}
