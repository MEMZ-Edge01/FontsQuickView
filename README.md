# FontsQuickView（字体速览）

[![Build](https://github.com/MEMZ-Edge01/FontsQuickView/actions/workflows/build.yml/badge.svg)](https://github.com/MEMZ-Edge01/FontsQuickView/actions/workflows/build.yml)
[![GitHub Release](https://img.shields.io/github/v/release/MEMZ-Edge01/FontsQuickView)](https://github.com/MEMZ-Edge01/FontsQuickView/releases/latest)
[![License](https://img.shields.io/github/license/MEMZ-Edge01/FontsQuickView)](LICENSE)

FontsQuickView 是一款轻量的 Windows 字体预览工具，可以用同一段文字快速浏览、搜索和筛选系统中已安装的字体。

## 功能

- 同时读取系统级和当前用户安装的字体。
- 自定义预览文字，并在 12–80 pt 之间调整字号。
- 按字体名称即时搜索。
- 按“全部字体”“支持中文”“支持英文”筛选。
- 标记可能不支持当前中文或英文预览内容的字体。
- 使用 WinUI 3、Mica 背景和自适应网格展示字体。

> [!NOTE]
> 字体语言支持由字体名称和常见字体规则推断，仅用于快速筛选，不等同于对字体字符表的完整检测。

## 下载与运行

1. 前往 [Releases](https://github.com/MEMZ-Edge01/FontsQuickView/releases/latest) 下载 `FontsQuickView-win-x64.zip`。
2. 解压全部文件。
3. 运行 `FontsQuickView.exe`。

系统要求：Windows 10 版本 2004（内部版本 19041）或更高版本，x64 处理器。

Release 中提供的是自包含版本，无需另外安装 .NET 运行时。

## 从源码构建

需要安装 [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)，并使用 Windows x64 环境。

```powershell
dotnet restore .\FontsQuickView.csproj -r win-x64
dotnet build .\FontsQuickView.csproj -c Release -p:Platform=x64 --no-restore
```

生成自包含发布目录：

```powershell
dotnet publish .\FontsQuickView.csproj `
  -c Release `
  -r win-x64 `
  -p:Platform=x64 `
  --self-contained true `
  -p:WindowsAppSDKSelfContained=true `
  -o .\artifacts\FontsQuickView-win-x64
```

## 技术栈

- .NET 8
- C# / XAML
- WinUI 3 / Windows App SDK 1.8
- Windows 注册表字体枚举

## 参与贡献

欢迎通过 Issue 报告问题或提出建议，也欢迎提交 Pull Request。提交前请确保 Release 构建通过且没有新增警告。

## 许可证

本项目使用 [MIT License](LICENSE) 开源。
