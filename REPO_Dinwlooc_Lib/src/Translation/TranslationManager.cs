// 文件：Dinwlooc.Common/Core/TranslationManager.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BepInEx;

namespace Dinwlooc.Common.Core
{
    /// <summary>
    /// 翻译注册管理器，为模组提供统一的“官译”文件生成与更新服务。
    /// 每个模组、每种语言独立维护一个翻译文件，存放于 BepInEx/Config/Translation/{语言}/Dinwlooc_Translation/{模组ID}.txt
    /// 文件编码为 UTF-8 无 BOM，兼容 XUnity.AutoTranslator 等工具。
    /// </summary>
    public static class TranslationManager
    {
        private const string TRANSLATION_FOLDER_NAME = "Dinwlooc_Translation";
        private const string HEADER_VERSION_PREFIX = "# Version=";
        private const string HEADER_HASH_PREFIX = "# Hash=";
        private const int HASH_BUFFER_SIZE = 4096;

        /// <summary>
        /// 注册或更新翻译条目。
        /// </summary>
        /// <param name="modId">模组唯一标识（建议使用插件 GUID）</param>
        /// <param name="languageCode">语言代码（如 "zh", "en"）</param>
        /// <param name="version">翻译版本号（整数，建议递增）</param>
        /// <param name="translations">键值对翻译字典（键为原始字符串，值为翻译文本）</param>
        public static void RegisterTranslations(
            string modId,
            string languageCode,
            int version,
            IReadOnlyDictionary<string, string> translations)
        {
            if (string.IsNullOrEmpty(modId))
                throw new ArgumentException("模组标识不能为空", nameof(modId));
            if (string.IsNullOrEmpty(languageCode))
                throw new ArgumentException("语言代码不能为空", nameof(languageCode));
            if (translations == null || translations.Count == 0)
            {
                CommonPlugin.Logger.LogWarning($"[TranslationManager] 模组 {modId} 未提供任何翻译条目，跳过注册。");
                return;
            }

            string filePath = GetTranslationFilePath(modId, languageCode);
            string directory = Path.GetDirectoryName(filePath)!;
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            // 计算内容哈希（基于排序后的键值对，确保一致性）
            string contentHash = ComputeContentHash(translations);

            // 如果文件存在，检查版本和哈希
            if (File.Exists(filePath))
            {
                if (TryReadFileHeader(filePath, out int existingVersion, out string existingHash))
                {
                    if (existingVersion == version && existingHash == contentHash)
                    {
                        CommonPlugin.Logger.LogInfo($"[TranslationManager] 模组 {modId} 翻译文件已是最新 (版本 {version})，无需更新。");
                        return;
                    }
                    CommonPlugin.Logger.LogInfo($"[TranslationManager] 模组 {modId} 翻译文件版本或哈希不匹配 (旧: {existingVersion}/{existingHash}, 新: {version}/{contentHash})，覆盖更新。");
                }
                else
                {
                    CommonPlugin.Logger.LogWarning($"[TranslationManager] 无法解析翻译文件头 {filePath}，将覆盖写入。");
                }
            }

            // 写入新文件
            WriteTranslationFile(filePath, version, contentHash, translations);
            CommonPlugin.Logger.LogInfo($"[TranslationManager] 模组 {modId} 翻译文件已更新: {filePath}");
        }

        private static string GetTranslationFilePath(string modId, string languageCode)
        {
            string baseDir = Paths.ConfigPath;
            string langDir = Path.Combine(baseDir, "Translation", languageCode);
            string modDir = Path.Combine(langDir, TRANSLATION_FOLDER_NAME);
            return Path.Combine(modDir, $"{modId}.txt");
        }

        private static string ComputeContentHash(IReadOnlyDictionary<string, string> translations)
        {
            var sortedKeys = translations.Keys.OrderBy(k => k);
            var sb = new StringBuilder();
            foreach (string key in sortedKeys)
            {
                sb.Append(key);
                sb.Append('=');
                sb.Append(translations[key]);
                sb.Append('\n');
            }
            byte[] inputBytes = Encoding.UTF8.GetBytes(sb.ToString());
            using (MD5 md5 = MD5.Create())
            {
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }

        private static bool TryReadFileHeader(string filePath, out int version, out string hash)
        {
            version = 0;
            hash = string.Empty;
            try
            {
                // 使用无 BOM 的 UTF-8 读取
                using (StreamReader reader = new StreamReader(filePath, new UTF8Encoding(false)))
                {
                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.StartsWith(HEADER_VERSION_PREFIX))
                        {
                            string verStr = line.Substring(HEADER_VERSION_PREFIX.Length);
                            int.TryParse(verStr, out version);
                        }
                        else if (line.StartsWith(HEADER_HASH_PREFIX))
                        {
                            hash = line.Substring(HEADER_HASH_PREFIX.Length).Trim();
                        }
                        // 遇到第一个非空且不以#开头的行，停止读取头部
                        if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith("#"))
                            break;
                    }
                }
                return !string.IsNullOrEmpty(hash);
            }
            catch (Exception ex)
            {
                CommonPlugin.Logger.LogError($"[TranslationManager] 读取文件头失败: {ex.Message}");
                return false;
            }
        }

        private static void WriteTranslationFile(string filePath, int version, string hash, IReadOnlyDictionary<string, string> translations)
        {
            try
            {
                // 使用无 BOM 的 UTF-8 写入
                using (StreamWriter writer = new StreamWriter(filePath, false, new UTF8Encoding(false)))
                {
                    writer.WriteLine($"# Version={version}");
                    writer.WriteLine($"# Hash={hash}");
                    writer.WriteLine("# 以下为翻译条目 (键=值)");
                    writer.WriteLine();

                    foreach (var kv in translations.OrderBy(kv => kv.Key))
                    {
                        string escapedValue = kv.Value.Replace("\n", "\\n");
                        writer.WriteLine($"{kv.Key}={escapedValue}");
                    }
                }
            }
            catch (Exception ex)
            {
                CommonPlugin.Logger.LogError($"[TranslationManager] 写入翻译文件失败: {ex.Message}");
                throw;
            }
        }
    }
}