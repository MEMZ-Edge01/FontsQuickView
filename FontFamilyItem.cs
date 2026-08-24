using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace FontsQuickView;

public sealed class FontFamilyItem : INotifyPropertyChanged
{
    // 已知的符号/图标字体（不渲染可读英文文本）
    private static readonly HashSet<string> _symbolFonts = new(StringComparer.OrdinalIgnoreCase)
    {
        "Bookshelf Symbol 7", "MT Extra", "MS Reference Specialty", "Kingsoft Symbol",
        "Segoe MDL2 Assets", "Segoe Fluent Icons", "Sans Serif Collection",
        "Symbol", "Wingdings", "Wingdings 2", "Wingdings 3", "Webdings"
    };

    public string Name { get; }
    public FontFamily FontFamily { get; }
    public bool IsCJK { get; }
    public bool IsLatin { get; }

    private string _unsupportedText = "";
    public string UnsupportedText
    {
        get => _unsupportedText;
        set { if (_unsupportedText != value) { _unsupportedText = value; OnPropertyChanged(); } }
    }

    private Visibility _unsupportedVis = Visibility.Collapsed;
    public Visibility UnsupportedVis
    {
        get => _unsupportedVis;
        set { if (_unsupportedVis != value) { _unsupportedVis = value; OnPropertyChanged(); } }
    }

    public FontFamilyItem(string name)
    {
        Name = name;
        FontFamily = new FontFamily(name);
        IsCJK = IsCJKFont(name);
        IsLatin = !_symbolFonts.Contains(name);
    }

    public static bool IsCJKFont(string name)
    {
        foreach (char c in name)
            if ((c >= 0x4E00 && c <= 0x9FFF) || (c >= 0x3400 && c <= 0x4DBF) || (c >= 0xF900 && c <= 0xFAFF) || (c >= 0x3000 && c <= 0x303F))
                return true;
        string l = name.ToLowerInvariant();
        string[] kw = { "yahei", "jhenghei", "simsun", "simhei", "kaiti", "fangsong", "mingliu", "pmingliu", "malgun", "gulim", "batang", "dotum", "gungsuh", "mincho", "meiryo", "yu gothic", "noto sans sc", "noto serif sc", "noto sans tc", "noto sans jp", "noto sans kr", "noto sans cjk", "source han", "dengxian", "microsoft yahei", "microsoft jhenghei", "cjk", "sisans", "misans" };
        foreach (var k in kw) if (l.Contains(k)) return true;
        return false;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
}