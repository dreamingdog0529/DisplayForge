using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DisplayForge.Resources;
using DisplayForge.ViewModels;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using Cursors = System.Windows.Input.Cursors;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;
using UserControl = System.Windows.Controls.UserControl;

namespace DisplayForge.Views;

/// <summary>
/// Visual monitor arrangement editor. Drag rectangles to update <see cref="MonitorRowViewModel"/> positions.
/// </summary>
public partial class MonitorLayoutEditor : UserControl
{
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(
            nameof(ItemsSource),
            typeof(IEnumerable),
            typeof(MonitorLayoutEditor),
            new PropertyMetadata(null, OnItemsSourceChanged));

    public static readonly DependencyProperty IsReadOnlyProperty =
        DependencyProperty.Register(
            nameof(IsReadOnly),
            typeof(bool),
            typeof(MonitorLayoutEditor),
            new PropertyMetadata(false, (_, _) => { /* rebuild via items */ }));

    private readonly List<MonitorVisual> _visuals = [];
    private INotifyCollectionChanged? _subscribedCollection;
    private readonly List<INotifyPropertyChanged> _subscribedItems = [];

    private MonitorVisual? _dragVisual;
    private Point _dragStartMouse;
    private int _dragStartPosX;
    private int _dragStartPosY;
    private bool _isDragging;
    private bool _rebuildQueued;

    private const double PaddingDip = 16;
    private const int SnapThresholdPx = 24;

    private static readonly SolidColorBrush PrimaryFill = Brush("#2B6CB0");
    private static readonly SolidColorBrush EnabledFill = Brush("#4A5568");
    private static readonly SolidColorBrush DisabledFill = Brush("#A0AEC0");
    private static readonly SolidColorBrush DragFill = Brush("#3182CE");
    private static readonly SolidColorBrush BorderBrushColor = Brush("#1A202C");
    private static readonly SolidColorBrush TextBrushColor = Brush("#FFFFFF");

    public MonitorLayoutEditor()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            ApplyEmptyHint();
            Rebuild();
        };
        IsVisibleChanged += (_, _) =>
        {
            if (IsVisible)
                Rebuild();
        };
    }

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public bool IsReadOnly
    {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var editor = (MonitorLayoutEditor)d;
        editor.UnsubscribeSource();
        editor.SubscribeSource(e.NewValue as IEnumerable);
        editor.Rebuild();
    }

    private void SubscribeSource(IEnumerable? source)
    {
        if (source is INotifyCollectionChanged ncc)
        {
            _subscribedCollection = ncc;
            ncc.CollectionChanged += OnCollectionChanged;
        }

        ResubscribeItems();
    }

    private void UnsubscribeSource()
    {
        if (_subscribedCollection is not null)
        {
            _subscribedCollection.CollectionChanged -= OnCollectionChanged;
            _subscribedCollection = null;
        }

        UnsubscribeItems();
    }

    private void ResubscribeItems()
    {
        UnsubscribeItems();
        foreach (var row in EnumerateRows())
        {
            _subscribedItems.Add(row);
            row.PropertyChanged += OnItemPropertyChanged;
        }
    }

    private void UnsubscribeItems()
    {
        foreach (var item in _subscribedItems)
            item.PropertyChanged -= OnItemPropertyChanged;
        _subscribedItems.Clear();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ResubscribeItems();
        QueueRebuild();
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isDragging)
            return;

        // Geometry / flags that affect the layout visualization.
        if (e.PropertyName is nameof(MonitorRowViewModel.PositionX)
            or nameof(MonitorRowViewModel.PositionY)
            or nameof(MonitorRowViewModel.Width)
            or nameof(MonitorRowViewModel.Height)
            or nameof(MonitorRowViewModel.IsEnabled)
            or nameof(MonitorRowViewModel.IsPrimary)
            or nameof(MonitorRowViewModel.Name)
            or nameof(MonitorRowViewModel.OrientationDegrees))
        {
            QueueRebuild();
        }
    }

    private void QueueRebuild()
    {
        if (_rebuildQueued)
            return;

        _rebuildQueued = true;
        Dispatcher.BeginInvoke(() =>
        {
            _rebuildQueued = false;
            Rebuild();
        });
    }

    private void LayoutCanvas_OnSizeChanged(object sender, SizeChangedEventArgs e) => Rebuild();

    private IEnumerable<MonitorRowViewModel> EnumerateRows()
    {
        if (ItemsSource is null)
            yield break;

        foreach (var item in ItemsSource)
        {
            if (item is MonitorRowViewModel row)
                yield return row;
        }
    }

    private void ApplyEmptyHint()
    {
        EmptyHint.Text = Strings.LayoutEditorEmpty;
    }

    public void RefreshLabels()
    {
        ApplyEmptyHint();
        Rebuild();
    }

    private void Rebuild()
    {
        if (_isDragging)
            return;

        LayoutCanvas.Children.Clear();
        _visuals.Clear();

        var rows = EnumerateRows().ToList();
        if (rows.Count == 0 || ActualWidth <= 1 || ActualHeight <= 1)
        {
            EmptyHint.Visibility = rows.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            return;
        }

        EmptyHint.Visibility = Visibility.Collapsed;

        // Prefer enabled monitors for layout bounds; fall back to all if none enabled.
        var layoutRows = rows.Where(r => r.IsEnabled && r.Width > 0 && r.Height > 0).ToList();
        if (layoutRows.Count == 0)
            layoutRows = rows.Where(r => r.Width > 0 && r.Height > 0).ToList();
        if (layoutRows.Count == 0)
        {
            EmptyHint.Visibility = Visibility.Visible;
            return;
        }

        var bounds = ComputeBounds(layoutRows);
        var scale = ComputeScale(bounds, LayoutCanvas.ActualWidth, LayoutCanvas.ActualHeight);
        if (scale <= 0)
            return;

        var virtW = bounds.Width;
        var virtH = bounds.Height;
        var offsetX = (LayoutCanvas.ActualWidth - virtW * scale) / 2;
        var offsetY = (LayoutCanvas.ActualHeight - virtH * scale) / 2;

        // Draw disabled first so enabled sit on top.
        var ordered = rows
            .Select((r, i) => (Row: r, Index: i + 1))
            .OrderBy(x => x.Row.IsEnabled)
            .ThenBy(x => x.Index)
            .ToList();

        foreach (var (row, index) in ordered)
        {
            if (row.Width <= 0 || row.Height <= 0)
                continue;

            var (displayW, displayH) = GetDisplaySize(row);
            var left = offsetX + (row.PositionX - bounds.MinX) * scale;
            var top = offsetY + (row.PositionY - bounds.MinY) * scale;
            var w = Math.Max(28, displayW * scale);
            var h = Math.Max(20, displayH * scale);

            var visual = CreateMonitorVisual(row, index, left, top, w, h, scale, bounds, offsetX, offsetY);
            _visuals.Add(visual);
            LayoutCanvas.Children.Add(visual.Root);
        }
    }

    private MonitorVisual CreateMonitorVisual(
        MonitorRowViewModel row,
        int index,
        double left,
        double top,
        double w,
        double h,
        double scale,
        LayoutBounds bounds,
        double offsetX,
        double offsetY)
    {
        var fill = !row.IsEnabled
            ? DisabledFill
            : row.IsPrimary
                ? PrimaryFill
                : EnabledFill;

        var border = new Border
        {
            Width = w,
            Height = h,
            Background = fill,
            BorderBrush = BorderBrushColor,
            BorderThickness = new Thickness(row.IsPrimary ? 2.5 : 1.5),
            CornerRadius = new CornerRadius(3),
            Cursor = IsReadOnly || !row.IsEditable || !row.IsEnabled ? Cursors.Arrow : Cursors.SizeAll,
            Opacity = row.IsEnabled ? 1.0 : 0.55,
            Tag = row
        };

        var stack = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            Margin = new Thickness(4)
        };

        stack.Children.Add(new TextBlock
        {
            Text = index.ToString(),
            FontSize = Math.Clamp(h * 0.35, 12, 28),
            FontWeight = FontWeights.Bold,
            Foreground = TextBrushColor,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center
        });

        var label = string.IsNullOrWhiteSpace(row.Name) ? $"#{index}" : row.Name;
        if (label.Length > 18)
            label = label[..16] + "…";

        stack.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = Math.Clamp(h * 0.12, 9, 12),
            Foreground = TextBrushColor,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxWidth = Math.Max(20, w - 8)
        });

        stack.Children.Add(new TextBlock
        {
            Text = $"{row.Width}×{row.Height}",
            FontSize = Math.Clamp(h * 0.1, 8, 11),
            Foreground = TextBrushColor,
            Opacity = 0.85,
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center
        });

        border.Child = stack;
        Canvas.SetLeft(border, left);
        Canvas.SetTop(border, top);

        var visual = new MonitorVisual(row, index, border, scale, bounds, offsetX, offsetY);

        if (!IsReadOnly && row.IsEditable && row.IsEnabled)
        {
            border.MouseLeftButtonDown += OnMonitorMouseDown;
            border.MouseMove += OnMonitorMouseMove;
            border.MouseLeftButtonUp += OnMonitorMouseUp;
            border.LostMouseCapture += OnMonitorLostCapture;
        }

        return visual;
    }

    private void OnMonitorMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (IsReadOnly || sender is not Border border || border.Tag is not MonitorRowViewModel row)
            return;

        var visual = _visuals.FirstOrDefault(v => v.Root == border);
        if (visual is null)
            return;

        _dragVisual = visual;
        _isDragging = true;
        _dragStartMouse = e.GetPosition(LayoutCanvas);
        _dragStartPosX = row.PositionX;
        _dragStartPosY = row.PositionY;
        border.Background = DragFill;
        border.CaptureMouse();
        Panel.SetZIndex(border, 100);
        e.Handled = true;
    }

    private void OnMonitorMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging || _dragVisual is null || e.LeftButton != MouseButtonState.Pressed)
            return;

        var mouse = e.GetPosition(LayoutCanvas);
        var dx = mouse.X - _dragStartMouse.X;
        var dy = mouse.Y - _dragStartMouse.Y;
        var scale = _dragVisual.Scale;
        if (scale <= 0)
            return;

        var newX = _dragStartPosX + (int)Math.Round(dx / scale);
        var newY = _dragStartPosY + (int)Math.Round(dy / scale);

        var others = EnumerateRows()
            .Where(r => r != _dragVisual.Row && r.IsEnabled && r.Width > 0 && r.Height > 0)
            .ToList();

        (newX, newY) = SnapPosition(_dragVisual.Row, newX, newY, others);

        _dragVisual.Row.SetPositionSilently(newX, newY);

        var left = _dragVisual.OffsetX + (newX - _dragVisual.Bounds.MinX) * scale;
        var top = _dragVisual.OffsetY + (newY - _dragVisual.Bounds.MinY) * scale;
        Canvas.SetLeft(_dragVisual.Root, left);
        Canvas.SetTop(_dragVisual.Root, top);
        e.Handled = true;
    }

    private void OnMonitorMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging)
            return;

        EndDrag(commit: true);
        e.Handled = true;
    }

    private void OnMonitorLostCapture(object sender, MouseEventArgs e)
    {
        if (_isDragging)
            EndDrag(commit: true);
    }

    private void EndDrag(bool commit)
    {
        if (_dragVisual is null)
        {
            _isDragging = false;
            return;
        }

        var visual = _dragVisual;
        visual.Root.ReleaseMouseCapture();

        if (commit)
            visual.Row.CommitPositionChange();

        visual.Root.Background = !visual.Row.IsEnabled
            ? DisabledFill
            : visual.Row.IsPrimary
                ? PrimaryFill
                : EnabledFill;

        Panel.SetZIndex(visual.Root, 0);
        _dragVisual = null;
        _isDragging = false;

        // Re-fit canvas after move (bounds may have changed).
        Rebuild();
    }

    private static (int X, int Y) SnapPosition(
        MonitorRowViewModel moving,
        int x,
        int y,
        IReadOnlyList<MonitorRowViewModel> others)
    {
        var (mw, mh) = GetDisplaySize(moving);
        var right = x + mw;
        var bottom = y + mh;

        // Snap to origin (primary-style alignment).
        if (Math.Abs(x) <= SnapThresholdPx) x = 0;
        if (Math.Abs(y) <= SnapThresholdPx) y = 0;

        foreach (var o in others)
        {
            var (ow, oh) = GetDisplaySize(o);
            var ol = o.PositionX;
            var ot = o.PositionY;
            var oright = ol + ow;
            var obottom = ot + oh;

            // Horizontal edges
            if (Math.Abs(x - oright) <= SnapThresholdPx) x = oright;
            if (Math.Abs(right - ol) <= SnapThresholdPx) x = ol - mw;
            if (Math.Abs(x - ol) <= SnapThresholdPx) x = ol;
            if (Math.Abs(right - oright) <= SnapThresholdPx) x = oright - mw;

            // Vertical edges
            if (Math.Abs(y - obottom) <= SnapThresholdPx) y = obottom;
            if (Math.Abs(bottom - ot) <= SnapThresholdPx) y = ot - mh;
            if (Math.Abs(y - ot) <= SnapThresholdPx) y = ot;
            if (Math.Abs(bottom - obottom) <= SnapThresholdPx) y = obottom - mh;

            right = x + mw;
            bottom = y + mh;
        }

        return (x, y);
    }

    private static (int Width, int Height) GetDisplaySize(MonitorRowViewModel row)
    {
        // Stored width/height are source mode size; orientation may be separate.
        // Use as-is (matches CCD layout coords used by Apply).
        return (Math.Max(1, row.Width), Math.Max(1, row.Height));
    }

    private static LayoutBounds ComputeBounds(IReadOnlyList<MonitorRowViewModel> rows)
    {
        var minX = int.MaxValue;
        var minY = int.MaxValue;
        var maxX = int.MinValue;
        var maxY = int.MinValue;

        foreach (var r in rows)
        {
            var (w, h) = GetDisplaySize(r);
            minX = Math.Min(minX, r.PositionX);
            minY = Math.Min(minY, r.PositionY);
            maxX = Math.Max(maxX, r.PositionX + w);
            maxY = Math.Max(maxY, r.PositionY + h);
        }

        return new LayoutBounds(minX, minY, maxX - minX, maxY - minY);
    }

    private static double ComputeScale(LayoutBounds bounds, double canvasW, double canvasH)
    {
        var availW = Math.Max(1, canvasW - PaddingDip * 2);
        var availH = Math.Max(1, canvasH - PaddingDip * 2);
        var bw = Math.Max(1, bounds.Width);
        var bh = Math.Max(1, bounds.Height);
        return Math.Min(availW / bw, availH / bh);
    }

    private static SolidColorBrush Brush(string hex)
    {
        var c = (Color)ColorConverter.ConvertFromString(hex)!;
        var b = new SolidColorBrush(c);
        b.Freeze();
        return b;
    }

    private sealed class MonitorVisual(
        MonitorRowViewModel row,
        int index,
        Border root,
        double scale,
        LayoutBounds bounds,
        double offsetX,
        double offsetY)
    {
        public MonitorRowViewModel Row { get; } = row;
        public int Index { get; } = index;
        public Border Root { get; } = root;
        public double Scale { get; } = scale;
        public LayoutBounds Bounds { get; } = bounds;
        public double OffsetX { get; } = offsetX;
        public double OffsetY { get; } = offsetY;
    }

    private readonly record struct LayoutBounds(int MinX, int MinY, int Width, int Height);
}
