using Cysharp.Threading.Tasks;

namespace SahurRaising.Core
{
    /// <summary>
    /// 데이터 저장/로드 서비스 인터페이스
    /// 에디터에서는 로컬 파일, 빌드에서는 CloudSave 사용
    /// </summary>
    public interface IDataService
    {
        /// <summary>
        /// 데이터를 저장합니다
        /// </summary>
        /// <param name="key">저장 키 (파일명 또는 CloudSave 키)</param>
        /// <param name="jsonData">JSON 문자열 데이터</param>
        UniTask SaveAsync(string key, string jsonData);

        /// <summary>
        /// 데이터를 로드합니다
        /// </summary>
        /// <param name="key">로드 키</param>
        /// <returns>JSON 문자열 데이터, 없으면 null</returns>
        UniTask<string> LoadAsync(string key);

        /// <summary>
        /// 데이터가 존재하는지 확인합니다
        /// </summary>
        /// <param name="key">확인할 키</param>
        /// <returns>존재 여부</returns>
        UniTask<bool> ExistsAsync(string key);
    }
}