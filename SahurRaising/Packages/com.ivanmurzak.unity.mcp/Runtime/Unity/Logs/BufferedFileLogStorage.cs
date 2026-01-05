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

namespace com.IvanMurzak.Unity.MCP
{
    /// <summary>
    /// 원본 패키지의 버퍼링 저장소가 누락된 경우를 대비한 최소 구현입니다.
    /// 현재는 <see cref="FileLogStorage"/>와 동일하게 동작합니다.
    /// </summary>
    public sealed class BufferedFileLogStorage : FileLogStorage
    {
        public BufferedFileLogStorage(string cacheFileName = "unity-mcp-editor-logs.txt", int maxFileSizeMB = 64)
            : base(requestedFileName: cacheFileName, maxFileSizeMB: maxFileSizeMB)
        {
        }
    }
}


