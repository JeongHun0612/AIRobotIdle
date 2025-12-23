/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable

using System;
using System.Threading.Tasks;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP
{
    /// <summary>
    /// Collects Unity logs and forwards them to an <see cref="ILogStorage"/> implementation.
    /// </summary>
    public sealed class UnityLogCollector : IDisposable
    {
        private readonly ILogStorage _logStorage;
        private bool _disposed;

        public UnityLogCollector(ILogStorage logStorage)
        {
            _logStorage = logStorage ?? throw new ArgumentNullException(nameof(logStorage));
            Application.logMessageReceivedThreaded += OnLogMessageReceived;
        }

        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            // IMPORTANT: This callback can be invoked from non-main threads.
            // ILogStorage implementations should be thread-safe and avoid Unity API calls.
            _logStorage.Append(new LogEntry(condition, stackTrace, type));
        }

        public global::com.IvanMurzak.Unity.MCP.LogEntry[] Query(int maxEntries = 100, LogType? logTypeFilter = null, bool includeStackTrace = false, int lastMinutes = 0)
        {
            return _logStorage.Query(
                maxEntries: maxEntries,
                logTypeFilter: logTypeFilter,
                includeStackTrace: includeStackTrace,
                lastMinutes: lastMinutes
            );
        }

        public void Clear()
        {
            _logStorage.Clear();
        }

        public void Save()
        {
            _logStorage.Flush();
        }

        public Task SaveAsync()
        {
            // 테스트/에디터 호환용: 실제 구현이 동기 Save()만 제공하더라도 Task 형태로 감싸 제공합니다.
            try
            {
                return _logStorage.FlushAsync();
            }
            catch
            {
                // 로그 저장 실패가 에디터 실행을 막으면 안됨
                return Task.CompletedTask;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Application.logMessageReceivedThreaded -= OnLogMessageReceived;
        }
    }
}


