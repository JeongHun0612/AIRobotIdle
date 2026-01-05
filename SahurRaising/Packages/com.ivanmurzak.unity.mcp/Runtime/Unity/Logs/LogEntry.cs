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
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP
{
    /// <summary>
    /// Unity 콘솔 로그 1건을 직렬화/전송하기 위한 DTO 입니다.
    /// </summary>
    [Serializable]
    public readonly struct LogEntry
    {
        public LogType LogType { get; }
        public string Message { get; }
        public string StackTrace { get; }

        /// <summary>
        /// 로그 발생 시각(로컬 시간)입니다. 테스트/표시용 API로 노출됩니다.
        /// </summary>
        public DateTime Timestamp => new DateTime(TimestampUtcTicks, DateTimeKind.Utc).ToLocalTime();

        /// <summary>
        /// 내부 저장/직렬화 용 UTC ticks 입니다.
        /// </summary>
        public long TimestampUtcTicks { get; }

        public LogEntry(LogType logType, string message)
            : this(logType, message, stackTrace: string.Empty, timestampUtcTicks: DateTime.UtcNow.Ticks)
        {
        }

        public LogEntry(LogType type, string message, string stackTrace)
            : this(type, message, stackTrace, DateTime.UtcNow.Ticks)
        {
        }

        public LogEntry(string message, string stackTrace, LogType logType)
            : this(logType, message, stackTrace, DateTime.UtcNow.Ticks)
        {
        }

        public LogEntry(LogType type, string message, string stackTrace, long timestampUtcTicks)
        {
            LogType = type;
            Message = message ?? string.Empty;
            StackTrace = stackTrace ?? string.Empty;
            TimestampUtcTicks = timestampUtcTicks;
        }

        public override string ToString()
        {
            // 테스트에서 요구하는 포맷: [Warning] + Timestamp("yyyy-MM-dd HH:mm:ss.fff") + message 포함
            return $"[{LogType}] {Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Message}";
        }
    }
}


