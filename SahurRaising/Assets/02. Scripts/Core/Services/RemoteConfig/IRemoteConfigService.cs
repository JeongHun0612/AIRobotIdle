using Cysharp.Threading.Tasks;

namespace SahurRaising.Core
{
    public interface IRemoteConfigService
    {
        bool IsInitialized { get; }
        UniTask InitializeAsync();
        string GetString(string key, string defaultValue = "");
        int GetInt(string key, int defaultValue = 0);
        float GetFloat(string key, float defaultValue = 0f);
        bool GetBool(string key, bool defaultValue = false);
        T GetJson<T>(string key, T defaultValue = default);
    }
}