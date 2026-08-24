using Microsoft.Win32;

namespace FontsQuickView;

/// <summary>
/// 从 HKLM + HKCU 注册表枚举系统中真正安装的所有字体族名。
/// HKCU 包含用户安装的额外字体（思源、阿里妈妈、造字工房等）。
/// </summary>
internal static class FontEnumerator
{
    private static readonly string[] _styleWords =
    {
        " Bold", " Italic", " Regular", " Light", " SemiLight", " SemiBold",
        " ExtraBold", " ExtraLight", " Medium", " Thin", " Black", " Heavy",
        " Narrow", " Demi", " Expanded", " Condensed", " Semibold",
        " Light Italic", " Bold Italic", " SemiBold Italic",
        " ExtraBold Italic", " Light Regular"
    };

    private static readonly string[] _compositeDelimiters =
    {
        " & "
    };

    /// <summary>
    /// 返回系统中所有实际字体族名称列表（含用户安装字体）。
    /// </summary>
    public static string[] GetSystemFontFamilies()
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 1. HKLM — 系统字体
        CollectFromKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts",
            RegistryHive.LocalMachine, names);

        // 2. HKCU — 用户安装字体（思源、阿里妈妈、造字工房等）
        CollectFromKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts",
            RegistryHive.CurrentUser, names);

        if (names.Count == 0)
        {
            return new[] { "Arial", "Calibri", "Consolas", "Segoe UI",
                           "Microsoft YaHei UI", "SimSun" };
        }

        return names.OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static void CollectFromKey(string subKey, RegistryHive hive, HashSet<string> result)
    {
        try
        {
            using var root = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
            using var key = root.OpenSubKey(subKey);
            if (key == null) return;

            foreach (var valueName in key.GetValueNames())
            {
                string trimmed = valueName;

                // 去掉括号后缀，如 "(TrueType)" "(OpenType)"
                int parenIdx = trimmed.IndexOf('(');
                if (parenIdx > 0)
                    trimmed = trimmed.Substring(0, parenIdx).Trim();

                if (string.IsNullOrEmpty(trimmed))
                    continue;

                // 处理 " & " 分隔的复合名，拆分成多个字体
                var parts = SplitOnDelimiters(trimmed);

                foreach (var part in parts)
                {
                    string name = part;

                    // 去掉逗号数字后缀
                    int commaIdx = name.IndexOf(',');
                    if (commaIdx > 0)
                        name = name.Substring(0, commaIdx).Trim();

                    if (string.IsNullOrEmpty(name))
                        continue;

                    // 去样式后缀
                    string stripped = StripStyle(name);

                    if (!string.IsNullOrEmpty(stripped))
                        result.Add(stripped);
                }
            }
        }
        catch
        {
            // 如果某注册表路径不可读则静默跳过
        }
    }

    private static List<string> SplitOnDelimiters(string input)
    {
        var parts = new List<string>();
        int splitStart = 0;

        for (int i = 0; i < input.Length; i++)
        {
            //
            foreach (var delim in _compositeDelimiters)
            {
                if (i + delim.Length <= input.Length &&
                    input.Substring(i, delim.Length).Equals(delim, StringComparison.OrdinalIgnoreCase))
                {
                    string part = input.Substring(splitStart, i - splitStart).Trim();
                    if (!string.IsNullOrEmpty(part))
                        parts.Add(part);
                    splitStart = i + delim.Length;
                    i += delim.Length - 1;
                    // found = true;
                    break;
                }
            }
        }

        if (splitStart < input.Length)
        {
            string lastPart = input.Substring(splitStart).Trim();
            if (!string.IsNullOrEmpty(lastPart))
                parts.Add(lastPart);
        }

        if (parts.Count == 0)
            parts.Add(input);

        return parts;
    }

    private static string StripStyle(string name)
    {
        string result = name;
        bool changed;
        do
        {
            changed = false;
            foreach (var sw in _styleWords)
            {
                if (result.EndsWith(sw, StringComparison.OrdinalIgnoreCase))
                {
                    result = result.Substring(0, result.Length - sw.Length).Trim();
                    changed = true;
                    break;
                }
            }
        } while (changed);
        return result;
    }
}