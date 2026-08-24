using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;

namespace FontsQuickView;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly List<FontFamilyItem> _allFonts = new();

    private string _sampleText = "字体速览 Font Preview 0123456789";
    private double _previewSize = 32;
    private string _searchQuery = string.Empty;
    private int _batchIndex;
    private volatile bool _loading;
    private bool _showAllFonts = true;
    private bool _showChineseOnly;
    private bool _showEnglishOnly;

    public string SampleText
    {
        get => _sampleText;
        set
        {
            if (_sampleText != value)
            {
                bool oldCJK = HasChineseText;
                bool oldEN = HasEnglishText;
                _sampleText = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ItemWidth));
                if (oldCJK != HasChineseText || oldEN != HasEnglishText)
                {
                    OnPropertyChanged(nameof(HasChineseText));
                    OnPropertyChanged(nameof(HasEnglishText));
                    UpdateUnsupportedLabels();
                    ApplyFilter();
                }
            }
        }
    }

    public double PreviewSize
    {
        get => _previewSize;
        set { if (Math.Abs(_previewSize - value) > 0.01) { _previewSize = value; OnPropertyChanged(); OnPropertyChanged(nameof(PreviewSizeDisplay)); OnPropertyChanged(nameof(ItemWidth)); OnPropertyChanged(nameof(ItemHeight)); } }
    }

    public string PreviewSizeDisplay => _previewSize.ToString("0");

    public string SearchQuery
    {
        get => _searchQuery;
        set { if (_searchQuery != value) { _searchQuery = value; OnPropertyChanged(); ApplyFilter(); } }
    }

    public bool ShowAllFonts
    {
        get => _showAllFonts;
        set { if (_showAllFonts != value) { if (value) { _showChineseOnly = false; _showEnglishOnly = false; } _showAllFonts = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShowChineseOnly)); OnPropertyChanged(nameof(ShowEnglishOnly)); ApplyFilter(); } }
    }

    public bool ShowChineseOnly
    {
        get => _showChineseOnly;
        set { if (_showChineseOnly != value) { if (value) _showAllFonts = false; _showChineseOnly = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShowAllFonts)); ApplyFilter(); } }
    }

    public bool ShowEnglishOnly
    {
        get => _showEnglishOnly;
        set { if (_showEnglishOnly != value) { if (value) _showAllFonts = false; _showEnglishOnly = value; OnPropertyChanged(); OnPropertyChanged(nameof(ShowAllFonts)); ApplyFilter(); } }
    }

    // 输入是否包含中文字符
    public bool HasChineseText
    {
        get
        {
            foreach (char c in _sampleText ?? "")
                if ((c >= 0x4E00 && c <= 0x9FFF) || (c >= 0x3400 && c <= 0x4DBF) || (c >= 0xF900 && c <= 0xFAFF) || (c >= 0x3000 && c <= 0x303F))
                    return true;
            return false;
        }
    }

    // 输入是否包含英文字母
    public bool HasEnglishText
    {
        get
        {
            foreach (char c in _sampleText ?? "")
                if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
                    return true;
            return false;
        }
    }

    public double ItemWidth
    {
        get
        {
            string text = _sampleText ?? "";
            double w = 0;
            foreach (char c in text)
            {
                if ((c >= 0x4E00 && c <= 0x9FFF) || (c >= 0x3000 && c <= 0x303F) || (c >= 0xFF00 && c <= 0xFFEF) || (c >= 0xAC00 && c <= 0xD7AF) || (c >= 0x3040 && c <= 0x30FF))
                    w += _previewSize * 0.95;
                else if (c >= 0x20 && c <= 0x7E)
                    w += _previewSize * 0.55;
                else
                    w += _previewSize * 0.7;
            }
            if (w < 1) w = _previewSize * 8;
            return Math.Clamp(w + 48, 220, 2000);
        }
    }

    public double ItemHeight { get { double h = _previewSize * 3.0 + 28; return Math.Clamp(h, 56, 300); } }

    public ObservableCollection<FontFamilyItem> Fonts { get; } = new();

    private int _totalCount;
    public int TotalCount { get => _totalCount; private set { if (_totalCount != value) { _totalCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(CountDisplay)); } } }

    private int _shownCount;
    public int ShownCount { get => _shownCount; private set { if (_shownCount != value) { _shownCount = value; OnPropertyChanged(); OnPropertyChanged(nameof(CountDisplay)); } } }

    public string CountDisplay => "显示 " + ShownCount + " / " + TotalCount + " 种字体";

    public void LoadFonts()
    {
        _allFonts.Clear(); Fonts.Clear(); _batchIndex = 0; _loading = true;
        string[] families = FontEnumerator.GetSystemFontFamilies();
        _allFonts.AddRange(families.Select(f => new FontFamilyItem(f)));
        TotalCount = _allFonts.Count;
        UpdateUnsupportedLabels();

        var dispatcher = DispatcherQueue.GetForCurrentThread();
        if (dispatcher != null)
        {
            var timer = dispatcher.CreateTimer();
            timer.Interval = TimeSpan.FromMilliseconds(15);
            timer.IsRepeating = true;
            int batchSize = 300;
            timer.Tick += (_, _) =>
            {
                if (!_loading) { timer.Stop(); return; }
                int end = Math.Min(_batchIndex + batchSize, _allFonts.Count);
                for (int i = _batchIndex; i < end; i++) Fonts.Add(_allFonts[i]);
                _batchIndex = end; ShownCount = Fonts.Count;
                if (_batchIndex >= _allFonts.Count) { _loading = false; timer.Stop(); ApplyFilter(); }
            };
            timer.Start();
        }
        else { foreach (var item in _allFonts) Fonts.Add(item); ShownCount = Fonts.Count; _loading = false; }
    }

    private void ApplyFilter()
    {
        if (_loading) return;
        Fonts.Clear();
        string query = _searchQuery?.Trim() ?? string.Empty;
        var filtered = _allFonts.AsEnumerable();
        if (!string.IsNullOrEmpty(query))
            filtered = filtered.Where(f => f.Name.Contains(query, StringComparison.OrdinalIgnoreCase));
        if (_showChineseOnly)
            filtered = filtered.Where(f => f.IsCJK);
        if (_showEnglishOnly)
            filtered = filtered.Where(f => f.IsLatin);

        // 排序：中英混合时 CJK+Latin 字体最优先，纯中文时 CJK 优先，纯英文时 Latin 优先
        if (HasChineseText && HasEnglishText)
            filtered = filtered.OrderByDescending(f => f.IsCJK).ThenByDescending(f => f.IsLatin).ThenBy(f => f.Name, StringComparer.OrdinalIgnoreCase);
        else if (HasChineseText)
            filtered = filtered.OrderByDescending(f => f.IsCJK).ThenBy(f => f.Name, StringComparer.OrdinalIgnoreCase);
        else if (HasEnglishText)
            filtered = filtered.OrderByDescending(f => f.IsLatin).ThenBy(f => f.Name, StringComparer.OrdinalIgnoreCase);

        foreach (var item in filtered) Fonts.Add(item);
        ShownCount = Fonts.Count;
    }

    private void UpdateUnsupportedLabels()
    {
        bool hasCJK = HasChineseText;
        bool hasEN = HasEnglishText;
        foreach (var item in _allFonts)
        {
            var parts = new List<string>();
            if (hasCJK && !item.IsCJK) parts.Add("汉字");
            if (hasEN && !item.IsLatin) parts.Add("英文");
            if (parts.Count > 0)
            {
                item.UnsupportedText = " -不支持" + string.Join("和", parts);
                item.UnsupportedVis = Visibility.Visible;
            }
            else
            {
                item.UnsupportedText = "";
                item.UnsupportedVis = Visibility.Collapsed;
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}