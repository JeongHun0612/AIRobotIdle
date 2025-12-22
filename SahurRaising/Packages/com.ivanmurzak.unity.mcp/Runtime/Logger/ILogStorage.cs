using System;

namespace com.IvanMurzak.Unity.MCP
{
    public interface ILogStorage
    {
        void AddLog(string message, string stackTrace, UnityEngine.LogType type);

        /// <summary>
        /// 이미 구성된 로그 엔트리를 추가합니다.
        /// </summary>
        void Append(LogEntry entry);

        /// <summary>
        /// 저장된 로그를 조회합니다.
        /// </summary>
        LogEntry[] Query(int maxEntries = 100, UnityEngine.LogType? logTypeFilter = null, bool includeStackTrace = false, int lastMinutes = 0);

        /// <summary>
        /// 저장소를 비웁니다(메모리/파일 포함).
        /// </summary>
        void Clear();

        /// <summary>
        /// 저장소 내용을 디스크에 반영합니다(저장소 구현에 따라 no-op일 수 있음).
        /// </summary>
        void Save();
    }
}
