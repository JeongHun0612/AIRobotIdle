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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace com.IvanMurzak.Unity.MCP
{
    /// <summary>
    /// 로그를 파일(라인 단위)로 저장/조회하는 간단한 저장소입니다.
    /// 포맷: {ticks}\t{logTypeInt}\t{base64(message)}\t{base64(stackTrace)}
    /// </summary>
    public class FileLogStorage : ILogStorage, IDisposable
    {
        private readonly object _gate = new();
        private readonly List<LogEntry> _entries = new(1024);

        private readonly string _filePath;
        private readonly long _maxFileSizeBytes;

        public FileLogStorage(string requestedFileName = "unity-mcp-editor-logs.txt", int maxFileSizeMB = 64)
        {
            if (string.IsNullOrWhiteSpace(requestedFileName))
                requestedFileName = "unity-mcp-editor-logs.txt";

            // Editor/Tests 환경에서 경로 충돌을 최소화하기 위해 temp cache 를 기본으로 사용
            var root = Application.temporaryCachePath;
            _filePath = Path.GetFullPath(Path.Combine(root, requestedFileName));

            if (maxFileSizeMB < 1) maxFileSizeMB = 1;
            _maxFileSizeBytes = (long)maxFileSizeMB * 1024L * 1024L;

            LoadFromFileIfExists();
        }

        public void AddLog(string message, string stackTrace, LogType type)
        {
            Append(new LogEntry(message, stackTrace, type));
        }

        public void Append(LogEntry entry)
        {
            lock (_gate)
            {
                EnsureFileNotTooLarge_NoLock();

                _entries.Add(entry);
                AppendLineToFile_NoLock(entry);
            }
        }

        public LogEntry[] Query(int maxEntries = 100, LogType? logTypeFilter = null, bool includeStackTrace = false, int lastMinutes = 0)
        {
            if (maxEntries < 1) maxEntries = 1;

            lock (_gate)
            {
                var nowTicks = DateTime.UtcNow.Ticks;
                var minTicks = lastMinutes > 0
                    ? nowTicks - TimeSpan.FromMinutes(lastMinutes).Ticks
                    : long.MinValue;

                var result = new List<LogEntry>(Math.Min(maxEntries, _entries.Count));

                // 최신 로그부터 반환
                for (int i = _entries.Count - 1; i >= 0 && result.Count < maxEntries; i--)
                {
                    var e = _entries[i];

                    if (e.TimestampUtcTicks < minTicks)
                        continue;

                    if (logTypeFilter.HasValue && e.LogType != logTypeFilter.Value)
                        continue;

                    if (!includeStackTrace && !string.IsNullOrEmpty(e.StackTrace))
                        e = new LogEntry(e.LogType, e.Message, stackTrace: string.Empty, timestampUtcTicks: e.TimestampUtcTicks);

                    result.Add(e);
                }

                result.Reverse(); // 오래된 -> 최신 순으로
                return result.ToArray();
            }
        }

        public void Clear()
        {
            lock (_gate)
            {
                _entries.Clear();
                TryDeleteFile_NoLock();
            }
        }

        public void Save()
        {
            // Append 시점에 바로 파일 반영을 하기 때문에 기본 구현은 no-op.
            // (원래 구현에서 버퍼링을 할 수도 있어서 API는 유지)
        }

        public void Dispose()
        {
            // 리소스 보유 없음
        }

        private void LoadFromFileIfExists()
        {
            lock (_gate)
            {
                _entries.Clear();

                if (!File.Exists(_filePath))
                    return;

                try
                {
                    foreach (var line in File.ReadLines(_filePath))
                    {
                        if (TryParseLine(line, out var entry))
                            _entries.Add(entry);
                    }
                }
                catch
                {
                    // 파일이 손상됐으면 안전하게 초기화
                    _entries.Clear();
                    TryDeleteFile_NoLock();
                }
            }
        }

        private void EnsureFileNotTooLarge_NoLock()
        {
            try
            {
                if (!File.Exists(_filePath))
                    return;

                var info = new FileInfo(_filePath);
                if (info.Length <= _maxFileSizeBytes)
                    return;

                // 너무 커지면 리셋 (테스트 의도: 크래시 없이 계속 동작)
                _entries.Clear();
                TryDeleteFile_NoLock();
            }
            catch
            {
                // 무시 (로그 저장 실패가 에디터 실행을 막으면 안됨)
            }
        }

        private void AppendLineToFile_NoLock(LogEntry entry)
        {
            try
            {
                var dir = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var line = Serialize(entry);
                File.AppendAllText(_filePath, line + Environment.NewLine, Encoding.UTF8);
            }
            catch
            {
                // 무시
            }
        }

        private void TryDeleteFile_NoLock()
        {
            try
            {
                if (File.Exists(_filePath))
                    File.Delete(_filePath);
            }
            catch
            {
                // 무시
            }
        }

        private static string Serialize(LogEntry e)
        {
            // ticks \t logTypeInt \t base64(message) \t base64(stackTrace)
            var msgB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(e.Message ?? string.Empty));
            var stB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(e.StackTrace ?? string.Empty));
            // Unity/.NET 프로파일에 따라 string.Create 오버로드가 제한될 수 있어,
            // 가장 호환성 높은 방식(단순 문자열 결합)으로 직렬화합니다.
            return $"{e.TimestampUtcTicks}\t{(int)e.LogType}\t{msgB64}\t{stB64}";
        }

        private static bool TryParseLine(string line, out LogEntry entry)
        {
            entry = default;
            if (string.IsNullOrEmpty(line)) return false;

            var parts = line.Split('\t');
            if (parts.Length < 4) return false;

            if (!long.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var ticks))
                return false;

            if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var logTypeInt))
                return false;

            string message;
            string stack;
            try
            {
                message = Encoding.UTF8.GetString(Convert.FromBase64String(parts[2]));
                stack = Encoding.UTF8.GetString(Convert.FromBase64String(parts[3]));
            }
            catch
            {
                return false;
            }

            entry = new LogEntry((LogType)logTypeInt, message, stack, ticks);
            return true;
        }
    }
}


