using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

public class GraphNode
{
    public int Id { get; set; }
    public string Name { get; set; }
    public SkiaSharp.SKPoint Position { get; set; }
}

public static class GraphLayoutEngine
{
    internal static class Native
    {
        private const string GvcDll = "gvc.dll";
        private const string CGraphDll = "cgraph.dll";
        private const string KernelDll = "kernel32.dll";

        [DllImport(KernelDll, SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "LoadLibraryExW")]
        public static extern IntPtr LoadLibraryEx(string lpLibFileName, IntPtr hFile, uint dwFlags);

        [DllImport(KernelDll, CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        public const uint LOAD_WITH_ALTERED_SEARCH_PATH = 0x00000008;

        [DllImport(GvcDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr gvContext();

        [DllImport(GvcDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void gvAddLibrary(IntPtr gvc, IntPtr lib);

        [DllImport(GvcDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int gvLayout(IntPtr gvc, IntPtr g, string engine);

        [DllImport(GvcDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int gvFreeLayout(IntPtr gvc, IntPtr g);

        [DllImport(GvcDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int gvFreeContext(IntPtr gvc);

        [DllImport(CGraphDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr agopen(string name, Agdesc_t desc, IntPtr disc);

        [DllImport(CGraphDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr agnode(IntPtr g, string name, int createifnd);

        [DllImport(CGraphDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr agedge(IntPtr g, IntPtr u, IntPtr v, string name, int createifnd);

        [DllImport(CGraphDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr agattr(IntPtr g, int kind, string name, string value);

        [DllImport(CGraphDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern IntPtr agget(IntPtr obj, string name);

        [DllImport(CGraphDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int agclose(IntPtr g);

        [DllImport(GvcDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int gvParseArgs(IntPtr gvc, int argc, string[] argv);
        [DllImport(GvcDll, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern int gvRender(IntPtr gvc, IntPtr g, string format, IntPtr outFd);

        [DllImport(GvcDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern void gvFreeRender(IntPtr gvc);
        [StructLayout(LayoutKind.Sequential, Size = 28)]
        public struct Agdesc_t
        {
            public uint directed;
            public uint strict;
            public uint no_loop;
            public uint maxtor;
            public uint flat;
            public uint no_write;
            public uint has_attrs;
        }

        public static readonly Agdesc_t Agundirected = new Agdesc_t { directed = 0, strict = 0 };

        public const int AGNODE = 1;
        public const int AGEDGE = 2;
    }

    // Imported C++ functions from MFCDialogs.dll
    [DllImport("MFCDialogs.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern int GetNodeCount();

    [DllImport("MFCDialogs.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern int GetNeighborCount(int nodeId);

    [DllImport("MFCDialogs.dll", CallingConvention = CallingConvention.Cdecl)]
    public static extern int GetNeighborId(int nodeId, int neighborIndex);

    [DllImport("MFCDialogs.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern void GetNodeName(int nodeId, StringBuilder buffer, int bufferSize);

    public static (List<GraphNode> nodes, List<(int Src, int Dst)> edges) ComputeFdpLayout()
    {
        int nodeCount = GetNodeCount();
        var nodes = new List<GraphNode>();
        var edges = new HashSet<(int, int)>();

        for (int i = 0; i < nodeCount; i++)
        {
            StringBuilder sb = new StringBuilder(256);
            GetNodeName(i, sb, sb.Capacity);
            nodes.Add(new GraphNode { Id = i, Name = sb.ToString() });

            int neighborCount = GetNeighborCount(i);
            for (int j = 0; j < neighborCount; j++)
            {
                int neighborId = GetNeighborId(i, j);
                int min = Math.Min(i, neighborId);
                int max = Math.Max(i, neighborId);
                edges.Add((min, max));
            }
        }

        // 1. Initialize Context
        IntPtr gvc = Native.gvContext();

        // 2. Load Core & Neato/FDP Layout Plugins into process memory
        IntPtr hCore = Native.LoadLibraryEx("gvplugin_core.dll", IntPtr.Zero, Native.LOAD_WITH_ALTERED_SEARCH_PATH);
        if (hCore != IntPtr.Zero)
        {
            IntPtr pCore = Native.GetProcAddress(hCore, "gvplugin_core_LTX_library");
            if (pCore != IntPtr.Zero) Native.gvAddLibrary(gvc, pCore);
        }

        IntPtr hNeato = Native.LoadLibraryEx("gvplugin_neato_layout.dll", IntPtr.Zero, Native.LOAD_WITH_ALTERED_SEARCH_PATH);
        if (hNeato != IntPtr.Zero)
        {
            IntPtr pNeato = Native.GetProcAddress(hNeato, "gvplugin_neato_layout_LTX_library");
            if (pNeato != IntPtr.Zero) Native.gvAddLibrary(gvc, pNeato);
        }

        // CRITICAL FIX 1: Bind layout engine via CLI args so gvc maps "fdp" to the neato plugin functions
        string[] args = new string[] { "fdp", "-Kfdp" };
        Native.gvParseArgs(gvc, args.Length, args);

        // 3. Open Graph
        IntPtr g = Native.agopen("G", Native.Agundirected, IntPtr.Zero);

        // CRITICAL FIX 2: Set attributes on the root graph BEFORE creating nodes
        Native.agattr(g, Native.AGNODE, "pos", "");
        Native.agattr(g, Native.AGNODE, "width", "0.75");
        Native.agattr(g, Native.AGNODE, "height", "0.5");
        Native.agattr(g, Native.AGEDGE, "pos", "");

        // 4. Create Nodes & Edges
        IntPtr[] agNodes = new IntPtr[nodeCount];
        for (int i = 0; i < nodeCount; i++)
        {
            agNodes[i] = Native.agnode(g, $"n{i}", 1);
        }

        foreach (var edge in edges)
        {
            Native.agedge(g, agNodes[edge.Item1], agNodes[edge.Item2], null, 1);
        }

        int layoutResult = Native.gvLayout(gvc, g, "fdp");

        // ATTACH / RENDER FIX: Populates the "pos" string attributes
        Native.gvRender(gvc, g, "dot", IntPtr.Zero);
        // 6. Extract Coordinates
        for (int i = 0; i < nodeCount; i++)
        {
            IntPtr posPtr = Native.agget(agNodes[i], "pos");
            if (posPtr != IntPtr.Zero)
            {
                string rawPos = Marshal.PtrToStringAnsi(posPtr);

                if (!string.IsNullOrWhiteSpace(rawPos))
                {
                    string cleanPos = rawPos.TrimEnd('!', '?').Trim();
                    string[] parts = cleanPos.Split(',');
                    if (parts.Length >= 2 &&
                        float.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out float x) &&
                        float.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out float y))
                    {
                        nodes[i].Position = new SkiaSharp.SKPoint(x, -y);
                    }
                }
            }
        }

        // Cleanup
        Native.gvFreeLayout(gvc, g);
        Native.agclose(g);
        Native.gvFreeContext(gvc);

        return (nodes, new List<(int, int)>(edges));
    }
}

namespace MainApplicationWPF.Views
{

    public partial class SettingsView : UserControl
    {
        private (List<GraphNode> Nodes, List<(int Src, int Dst)> Edges)? _cachedGraph;
        private SKPoint _panOffset = new SKPoint(0, 0);
        private Point _lastPointerPosition;
        private float _zoom = 1.0f;
        private const float MinZoom = 0.1f;
        private const float MaxZoom = 10.0f;
        private bool _isPanning;

        // Compute the current transformation matrix
        private SKMatrix TransformMatrix
        {
            get
            {
                // 1. First translate by pan offset
                SKMatrix translation = SKMatrix.CreateTranslation(_panOffset.X, _panOffset.Y);

                // 2. Scale relative to origin
                SKMatrix scale = SKMatrix.CreateScale(_zoom, _zoom);

                // Concat: Scale * Translation
                return SKMatrix.Concat(translation, scale);
            }
        }
        public SettingsView()
        {
            InitializeComponent();

            Loaded += SettingsView_Loaded;
            Unloaded += SettingsView_Unloaded;
        }

        private void SettingsView_Loaded(object sender, RoutedEventArgs e)
        {
            // Subscribe to WPF's rendering loop when view opens
            CompositionTarget.Rendering += OnRendering;
        }

        private void SettingsView_Unloaded(object sender, RoutedEventArgs e)
        {
            // Unsubscribe when navigating away to prevent CPU background usage
            CompositionTarget.Rendering -= OnRendering;
        }

        private void OnRendering(object? sender, EventArgs e)
        {
            SettingsSkiaCanvas.InvalidateVisual(); // Trigger redraw
        }
        private void SettingsSkiaCanvas_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            if (_cachedGraph == null)
            {
                _cachedGraph = GraphLayoutEngine.ComputeFdpLayout();
            }

            var (nodes, edges) = _cachedGraph.Value;
            if (nodes == null || nodes.Count == 0) return;

            SKCanvas canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.Empty); // Transparent background

            // Apply viewport transform (Pan & Zoom)
            canvas.Save();
            canvas.SetMatrix(TransformMatrix);

            // 1. Draw Edges
            using (var edgePaint = new SKPaint())
            {
                edgePaint.Color = SKColors.Gray;
                edgePaint.StrokeWidth = 2.0f;
                edgePaint.IsAntialias = true;
                edgePaint.Style = SKPaintStyle.Stroke;

                foreach (var edge in edges)
                {
                    // Assumes nodes[index].Position maps to SKPoint or System.Numerics.Vector2
                    var src = new SKPoint(nodes[edge.Src].Position.X, nodes[edge.Src].Position.Y);
                    var dst = new SKPoint(nodes[edge.Dst].Position.X, nodes[edge.Dst].Position.Y);

                    canvas.DrawLine(src, dst, edgePaint);
                }
            }

            // 2. Draw Nodes & Text
            float nodeRadius = 18.0f;

            using (var fillPaint = new SKPaint { Color = SKColors.RoyalBlue, Style = SKPaintStyle.Fill, IsAntialias = true })
            using (var strokePaint = new SKPaint { Color = SKColors.White, Style = SKPaintStyle.Stroke, StrokeWidth = 2.0f, IsAntialias = true })
            using (var textPaint = new SKPaint { Color = SKColors.White, IsAntialias = true })
            using (var font = new SKFont(SKTypeface.FromFamilyName("Segoe UI"), 13))
            {
                foreach (var node in nodes)
                {
                    var nodePos = new SKPoint(node.Position.X, node.Position.Y);

                    // Fill & Outline
                    canvas.DrawCircle(nodePos, nodeRadius, fillPaint);
                    canvas.DrawCircle(nodePos, nodeRadius, strokePaint);

                    // Center-align text horizontally
                    //textPaint.TextAlign = SKTextAlign.Center;

                    // Measure height to center text vertically inside circle
                    font.GetFontMetrics(out SKFontMetrics metrics);
                    float verticalOffset = (metrics.Descent + metrics.Ascent) / 2;

                    canvas.DrawText(node.Name, nodePos.X, nodePos.Y - verticalOffset, font, textPaint);
                }
            }

            canvas.Restore();
        }

        private void SettingsSkiaCanvas_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Middle || e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                _isPanning = true;
                _lastPointerPosition = e.GetPosition(SettingsSkiaCanvas);
                SettingsSkiaCanvas.CaptureMouse();
            }
        }

        private void SettingsSkiaCanvas_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            Point mousePos = e.GetPosition(SettingsSkiaCanvas);

            int delta = e.Delta;
            float zoomFactor = delta > 0 ? 1.15f : (1.0f / 1.15f);
            float newZoom = Math.Clamp(_zoom * zoomFactor, MinZoom, MaxZoom);

            // 1. Calculate the world coordinate currently under the mouse cursor
            float worldX = (float)(mousePos.X - _panOffset.X) / _zoom;
            float worldY = (float)(mousePos.Y - _panOffset.Y) / _zoom;

            // 2. Apply new zoom level
            _zoom = newZoom;

            // 3. Update pan offset so the same world point maps back to the mouse position
            _panOffset.X = (float)mousePos.X - (worldX * _zoom);
            _panOffset.Y = (float)mousePos.Y - (worldY * _zoom);

            SettingsSkiaCanvas.InvalidateVisual();
        }

        private void SettingsSkiaCanvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_isPanning) return;

            Point currentPos = e.GetPosition(SettingsSkiaCanvas);
            var delta = new SKPoint(
                (float)(currentPos.X - _lastPointerPosition.X),
                (float)(currentPos.Y - _lastPointerPosition.Y)
            );

            _panOffset.X += delta.X;
            _panOffset.Y += delta.Y;
            _lastPointerPosition = currentPos;

            SettingsSkiaCanvas.InvalidateVisual();
        }

        private void SettingsSkiaCanvas_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (_isPanning && (e.ChangedButton == System.Windows.Input.MouseButton.Middle || e.ChangedButton == System.Windows.Input.MouseButton.Left))
            {
                _isPanning = false;
                SettingsSkiaCanvas.ReleaseMouseCapture();
            }
        }
    }
}