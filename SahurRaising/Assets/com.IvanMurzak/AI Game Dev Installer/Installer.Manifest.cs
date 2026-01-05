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
using System.IO;
using System.Linq;
using UnityEngine;
using com.IvanMurzak.Unity.MCP.Installer.SimpleJSON;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("com.IvanMurzak.Unity.MCP.Installer.Tests")]
namespace com.IvanMurzak.Unity.MCP.Installer
{
    public static partial class Installer
    {
        static string ManifestPath => Path.Combine(Application.dataPath, "../Packages/manifest.json");

        // Property names
        public const string Dependencies = "dependencies";
        public const string ScopedRegistries = "scopedRegistries";
        public const string Name = "name";
        public const string Url = "url";
        public const string Scopes = "scopes";

        // Property values
        public const string RegistryName = "package.openupm.com";
        public const string RegistryUrl = "https://package.openupm.com";
        public static readonly string[] PackageIds = new string[] {
            "com.ivanmurzak",            // Ivan Murzak's OpenUPM packages
            "extensions.unity",          // Ivan Murzak's OpenUPM packages (older)
            "org.nuget.com.ivanmurzak",  // Ivan Murzak's NuGet packages
            "org.nuget.microsoft",       // Microsoft NuGet packages
            "org.nuget.system",          // Microsoft NuGet packages
            "org.nuget.r3"               // R3 package NuGet package
        };

        /// <summary>
        /// Determines if the version should be updated. Only update if installer version is higher than current version.
        /// </summary>
        /// <param name="currentVersion">Current package version string</param>
        /// <param name="installerVersion">Installer version string</param>
        /// <returns>True if version should be updated (installer version is higher), false otherwise</returns>

        internal static bool ShouldUpdateVersion(string currentVersion, string installerVersion)
        {
            if (string.IsNullOrEmpty(currentVersion))
                return true; // No current version, should install

            if (string.IsNullOrEmpty(installerVersion))
                return false; // No installer version, don't change

            // UPM은 "file:", "git:", "https:" 같은 소스 참조를 버전 칸에 넣을 수 있습니다.
            // 이런 값은 숫자 버전과 비교 자체가 무의미하고, 오히려 로컬/임베디드 패키지를
            // 실수로 레지스트리 버전으로 덮어쓰는 위험이 있어 업데이트를 금지합니다.
            if (IsUpmSourceReference(currentVersion) || IsUpmSourceReference(installerVersion))
                return false;

            try
            {
                // Try to parse as System.Version (semantic versioning)
                if (!TryParseSystemVersion(currentVersion, out var current) ||
                    !TryParseSystemVersion(installerVersion, out var installer))
                {
                    // If version parsing fails, fall back to string comparison
                    // This ensures we don't break if version format is unexpected
                    return string.Compare(installerVersion, currentVersion, System.StringComparison.OrdinalIgnoreCase) > 0;
                }

                // Only update if installer version is higher than current version
                return installer > current;
            }
            catch (System.Exception)
            {
                Debug.LogWarning($"Failed to parse versions '{currentVersion}' or '{installerVersion}' as System.Version.");
                // If version parsing fails, fall back to string comparison
                // This ensures we don't break if version format is unexpected
                return string.Compare(installerVersion, currentVersion, System.StringComparison.OrdinalIgnoreCase) > 0;
            }
        }

        static bool IsUpmSourceReference(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            return value.StartsWith("file:", System.StringComparison.OrdinalIgnoreCase) ||
                   value.StartsWith("git:", System.StringComparison.OrdinalIgnoreCase) ||
                   value.StartsWith("http://", System.StringComparison.OrdinalIgnoreCase) ||
                   value.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase);
        }

        static bool TryParseSystemVersion(string value, out System.Version version)
        {
            version = default!;
            if (string.IsNullOrWhiteSpace(value))
                return false;

            // 흔한 패턴: "v0.29.1", "0.29.1-preview.1" 같은 문자열에서 숫자 버전 prefix만 추출합니다.
            var trimmed = value.Trim();
            if (trimmed.Length > 1 && (trimmed[0] == 'v' || trimmed[0] == 'V'))
                trimmed = trimmed.Substring(1);

            var length = 0;
            while (length < trimmed.Length)
            {
                var c = trimmed[length];
                if ((c >= '0' && c <= '9') || c == '.')
                {
                    length++;
                    continue;
                }
                break;
            }

            if (length <= 0)
                return false;

            var prefix = trimmed.Substring(0, length);
            return System.Version.TryParse(prefix, out version);
        }

        public static void AddScopedRegistryIfNeeded(string manifestPath, int indent = 2)
        {
            if (!File.Exists(manifestPath))
            {
                Debug.LogError($"{manifestPath} not found!");
                return;
            }
            string jsonText;
            try
            {
                jsonText = File.ReadAllText(manifestPath)
                    .Replace("{ }", "{\n}")
                    .Replace("{}", "{\n}")
                    .Replace("[ ]", "[\n]")
                    .Replace("[]", "[\n]");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to read {manifestPath}. The installer can't update scoped registries automatically. {ex.Message}");
                return;
            }

            var manifestJson = JSONObject.Parse(jsonText);
            if (manifestJson == null)
            {
                Debug.LogError($"Failed to parse {manifestPath} as JSON.");
                return;
            }

            var modified = false;

            // --- Add scoped registries if needed
            var scopedRegistries = manifestJson[ScopedRegistries];
            if (scopedRegistries == null)
            {
                manifestJson[ScopedRegistries] = scopedRegistries = new JSONArray();
                modified = true;
            }

            // --- Add OpenUPM registry if needed
            var openUpmRegistry = scopedRegistries!.Linq
                .Select(kvp => kvp.Value)
                .Where(r => r.Linq
                    .Any(p => p.Key == Name && p.Value == RegistryName))
                .FirstOrDefault();

            if (openUpmRegistry == null)
            {
                scopedRegistries.Add(openUpmRegistry = new JSONObject
                {
                    [Name] = RegistryName,
                    [Url] = RegistryUrl,
                    [Scopes] = new JSONArray()
                });
                modified = true;
            }

            // --- Add missing scopes
            var scopes = openUpmRegistry[Scopes];
            if (scopes == null)
            {
                openUpmRegistry[Scopes] = scopes = new JSONArray();
                modified = true;
            }
            foreach (var packageId in PackageIds)
            {
                var existingScope = scopes!.Linq
                    .Select(kvp => kvp.Value)
                    .Where(value => value == packageId)
                    .FirstOrDefault();
                if (existingScope == null)
                {
                    scopes.Add(packageId);
                    modified = true;
                }
            }

            // --- Package Dependency (Version-aware installation)
            // Only update version if installer version is higher than current version
            // This prevents downgrades when users manually update to newer versions
            var dependencies = manifestJson[Dependencies];
            if (dependencies == null)
            {
                manifestJson[Dependencies] = dependencies = new JSONObject();
                modified = true;
            }

            // Only update version if installer version is higher than current version
            var currentVersion = dependencies[PackageId];
            if (currentVersion == null || ShouldUpdateVersion(currentVersion, Version))
            {
                dependencies[PackageId] = Version;
                modified = true;
            }

            // --- Write changes back to manifest
            if (modified)
            {
                try
                {
                    File.WriteAllText(manifestPath, manifestJson.ToString(indent).Replace("\" : ", "\": "));
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Failed to write {manifestPath}. Please ensure the file isn't read-only/locked. {ex.Message}");
                }
            }
        }
    }
}