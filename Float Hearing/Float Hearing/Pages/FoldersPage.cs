using Float_Hearing.Services;
using Microsoft.UI.Xaml.Controls;

namespace Float_Hearing.Pages;

/// <summary>
/// 文件夹页：BreadcrumbBar 显示当前路径，TreeView 懒加载浏览目录树。
/// </summary>
public sealed partial class FoldersPage : Page
{
    private readonly List<string> _pathSegments = [];

    public FoldersPage()
    {
        InitializeComponent();
        Title = Loc.Get("PageFoldersTitle");
        Loaded += FoldersPage_Loaded;
    }

    public string Title { get; }

    private void FoldersPage_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (FolderTree.RootNodes.Count > 0)
        {
            return;
        }

        var roots = new[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
        }
        .Where(Directory.Exists)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

        if (roots.Count == 0)
        {
            roots.Add(Path.GetPathRoot(Environment.SystemDirectory) ?? @"C:\");
        }

        foreach (var root in roots)
        {
            FolderTree.RootNodes.Add(CreateNode(root));
        }

        if (FolderTree.RootNodes.Count > 0)
        {
            var first = FolderTree.RootNodes[0];
            first.IsExpanded = true;
            if (AsInfo(first.Content) is { } info)
            {
                UpdateBreadcrumb(info.FullPath);
                FolderTree.SelectedItem = info;
            }
        }
    }

    private static FolderNodeInfo? AsInfo(object? content) => content as FolderNodeInfo;

    private static TreeViewNode CreateNode(string fullPath)
    {
        var trimmed = fullPath.TrimEnd('\\', '/');
        var fileName = Path.GetFileName(trimmed);
        var name = string.IsNullOrEmpty(fileName) ? trimmed : fileName;

        var node = new TreeViewNode
        {
            Content = new FolderNodeInfo(fullPath, name),
        };

        // 预挂占位子节点以显示展开箭头；真正加载在 Expanding
        node.Children.Add(new TreeViewNode { Content = Placeholder.Instance });
        return node;
    }

    private void FolderTree_Expanding(TreeView sender, TreeViewExpandingEventArgs args)
    {
        if (AsInfo(args.Node.Content) is not { } info || info.ChildrenLoaded)
        {
            return;
        }

        args.Node.Children.Clear();
        try
        {
            foreach (var dir in Directory.EnumerateDirectories(info.FullPath))
            {
                args.Node.Children.Add(CreateNode(dir));
            }
        }
        catch (UnauthorizedAccessException)
        {
        }
        catch (IOException)
        {
        }
        catch (System.Security.SecurityException)
        {
        }

        info.ChildrenLoaded = true;
    }

    private void FolderTree_ItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
    {
        if (AsInfo(args.InvokedItem) is { } info)
        {
            UpdateBreadcrumb(info.FullPath);
        }
    }

    private void PathBreadcrumb_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
    {
        if (args.Index < 0 || args.Index >= _pathSegments.Count)
        {
            return;
        }

        var parts = _pathSegments.Take(args.Index + 1).ToList();
        var target = parts[0].EndsWith(':')
            ? parts[0] + Path.DirectorySeparatorChar + string.Join(Path.DirectorySeparatorChar, parts.Skip(1))
            : string.Join(Path.DirectorySeparatorChar, parts);

        UpdateBreadcrumb(target);
        SelectByPath(target);
    }

    private void UpdateBreadcrumb(string fullPath)
    {
        _pathSegments.Clear();
        var trimmed = fullPath.TrimEnd(Path.DirectorySeparatorChar, '/');

        if (trimmed.Length >= 2 && trimmed[1] == ':')
        {
            _pathSegments.Add(trimmed[..2]);
            var rest = trimmed[2..].TrimStart(Path.DirectorySeparatorChar, '/');
            if (rest.Length > 0)
            {
                _pathSegments.AddRange(rest.Split(
                    new[] { Path.DirectorySeparatorChar, '/' },
                    StringSplitOptions.RemoveEmptyEntries));
            }
        }
        else
        {
            _pathSegments.AddRange(trimmed.Split(
                new[] { Path.DirectorySeparatorChar, '/' },
                StringSplitOptions.RemoveEmptyEntries));
        }

        // BreadcrumbBar 使用 ItemsSource
        PathBreadcrumb.ItemsSource = _pathSegments.ToList();
    }

    private void SelectByPath(string fullPath)
    {
        var normalized = fullPath.TrimEnd('\\', '/');
        foreach (var root in FolderTree.RootNodes)
        {
            if (TrySelect(root, normalized))
            {
                return;
            }
        }
    }

    private bool TrySelect(TreeViewNode node, string normalized)
    {
        if (AsInfo(node.Content) is { } info
            && string.Equals(info.FullPath.TrimEnd('\\', '/'), normalized, StringComparison.OrdinalIgnoreCase))
        {
            // 确保父链展开
            for (var p = node.Parent; p is not null; p = p.Parent)
            {
                p.IsExpanded = true;
            }

            FolderTree.SelectedItem = info;
            return true;
        }

        foreach (var child in node.Children)
        {
            if (TrySelect(child, normalized))
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>树节点：完整路径 + 显示名。</summary>
public sealed class FolderNodeInfo
{
    public FolderNodeInfo(string fullPath, string displayName)
    {
        FullPath = fullPath;
        DisplayName = displayName;
    }

    public string FullPath { get; }

    public string DisplayName { get; }

    public bool ChildrenLoaded { get; set; }

    public override string ToString() => DisplayName;
}

/// <summary>占位子节点内容（触发展开箭头）。</summary>
internal sealed class Placeholder
{
    public static readonly Placeholder Instance = new();

    private Placeholder()
    {
    }

    public override string ToString() => string.Empty;
}
