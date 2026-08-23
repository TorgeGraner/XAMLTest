using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System;
using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text;
using System.Text;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.ViewManagement.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

public class GraphNode
{
    public int Id { get; set; }
    public string Name { get; set; }
    public Vector2 Position { get; set; }
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
                        nodes[i].Position = new Vector2(x, -y);
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
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MainApplication
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Direct2DPage : Page
    {
        private (List<GraphNode> Nodes, List<(int Src, int Dst)> Edges)? _cachedGraph;
        private Vector2 _panOffset = Vector2.Zero;
        private float _zoom = 1.0f;
        private const float MinZoom = 0.1f;
        private const float MaxZoom = 10.0f;

        private Point _lastPointerPosition;
        private bool _isPanning = false;

        // Compute the current transformation matrix
        private Matrix3x2 TransformMatrix =>
            Matrix3x2.CreateScale(_zoom) * Matrix3x2.CreateTranslation(_panOffset);
        public Direct2DPage()
        {
            InitializeComponent();
        }
        private void canvasControl_Draw(Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
        {
            if (_cachedGraph == null)
            {
                _cachedGraph = GraphLayoutEngine.ComputeFdpLayout();
            }

            var (nodes, edges) = _cachedGraph.Value;
            if (nodes == null || nodes.Count == 0) return;

            var session = args.DrawingSession;

            session.Transform = TransformMatrix;
            // 1. Draw Edges
            foreach (var edge in edges)
            {
                session.DrawLine(nodes[edge.Src].Position, nodes[edge.Dst].Position, Colors.Gray, 2.0f);
            }

            // 2. Draw Nodes & Text
            float nodeRadius = 18.0f;
            using (var format = new CanvasTextFormat
            {
                FontSize = 13,
                HorizontalAlignment = CanvasHorizontalAlignment.Center,
                VerticalAlignment = CanvasVerticalAlignment.Center
            })
            {
                foreach (var node in nodes)
                {

                    // Fill & Outline
                    session.FillCircle(node.Position, nodeRadius, Colors.RoyalBlue);
                    session.DrawCircle(node.Position, nodeRadius, Colors.White, 2.0f);

                    // Label
                    session.DrawText(node.Name, node.Position, Colors.White, format);
                }
            }
        }
        public void InvalidateGraphLayout()
        {
            _cachedGraph = null;
            canvasControl.Invalidate();
        }

        private void canvasControl_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var props = e.GetCurrentPoint(canvasControl).Properties;

            // Pan with Middle Mouse Button (or change to IsLeftButtonPressed)
            if (props.IsMiddleButtonPressed || props.IsLeftButtonPressed)
            {
                _isPanning = true;
                _lastPointerPosition = e.GetCurrentPoint(canvasControl).Position;
                canvasControl.CapturePointer(e.Pointer);
            }
        }

        private void canvasControl_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            var point = e.GetCurrentPoint(canvasControl);
            Vector2 pointerPos = point.Position.ToVector2();

            int delta = point.Properties.MouseWheelDelta;
            float zoomFactor = delta > 0 ? 1.15f : (1.0f / 1.15f);
            float newZoom = Math.Clamp(_zoom * zoomFactor, MinZoom, MaxZoom);

            // Zoom toward the pointer position
            _panOffset = pointerPos - (pointerPos - _panOffset) * (newZoom / _zoom);
            _zoom = newZoom;

            canvasControl.Invalidate();
        }

        private void canvasControl_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!_isPanning) return;

            Point currentPos = e.GetCurrentPoint(canvasControl).Position;
            Vector2 delta = new Vector2(
                (float)(currentPos.X - _lastPointerPosition.X),
                (float)(currentPos.Y - _lastPointerPosition.Y)
            );

            _panOffset += delta;
            _lastPointerPosition = currentPos;

            canvasControl.Invalidate();
        }

        private void canvasControl_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (_isPanning)
            {
                _isPanning = false;
                canvasControl.ReleasePointerCapture(e.Pointer);
            }
        }
    }
}
