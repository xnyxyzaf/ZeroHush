using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PixelStudio;

public class CanvasEngine
{
    public int Width { get; private set; }
    public int Height { get; private set; }
    public LayerNode Root { get; private set; }
    public LayerNode? ActiveNode { get; set; }
    public WriteableBitmap Bitmap { get; private set; }

    private byte[] _compositeBuffer;

    public CanvasEngine(int width, int height)
    {
        Width = width;
        Height = height;
        _compositeBuffer = new byte[width * height * 4];

        Root = new LayerNode(NodeType.Folder, "根目录");

        var layer = new Layer(width, height, "图层 1");
        var node = new LayerNode(NodeType.Layer, layer.Name) { Layer = layer };
        node.AttachTo(Root);
        ActiveNode = node;

        Bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
        Refresh();
    }

    public Layer? ActiveLayer => ActiveNode?.Layer;

    public void Refresh()
    {
        Array.Clear(_compositeBuffer, 0, _compositeBuffer.Length);
        CompositeNode(Root);
        Bitmap.WritePixels(new Int32Rect(0, 0, Width, Height), _compositeBuffer, Width * 4, 0);
    }

    private void CompositeNode(LayerNode node)
    {
        if (!node.Visible) return;

        foreach (var child in node.Children)
        {
            if (!child.Visible) continue;

            if (child.IsLayer && child.Layer != null)
                BlendLayerPixels(child.Layer);
            else if (child.IsFolder)
                CompositeNode(child);
        }
    }

    private void BlendLayerPixels(Layer layer)
    {
        var src = layer.PixelsOrNull;
        if (src == null) return;

        for (int i = 0; i < _compositeBuffer.Length; i += 4)
        {
            byte srcB = src[i];
            byte srcG = src[i + 1];
            byte srcR = src[i + 2];
            byte srcA = src[i + 3];
            if (srcA == 0) continue;

            if (srcA == 255)
            {
                _compositeBuffer[i] = srcB;
                _compositeBuffer[i + 1] = srcG;
                _compositeBuffer[i + 2] = srcR;
                _compositeBuffer[i + 3] = 255;
            }
            else
            {
                int dstA = _compositeBuffer[i + 3];
                int outA = srcA + dstA * (255 - srcA) / 255;
                if (outA == 0) continue;

                _compositeBuffer[i] = (byte)((srcB * srcA + _compositeBuffer[i] * dstA * (255 - srcA) / 255) / outA);
                _compositeBuffer[i + 1] = (byte)((srcG * srcA + _compositeBuffer[i + 1] * dstA * (255 - srcA) / 255) / outA);
                _compositeBuffer[i + 2] = (byte)((srcR * srcA + _compositeBuffer[i + 2] * dstA * (255 - srcA) / 255) / outA);
                _compositeBuffer[i + 3] = (byte)outA;
            }
        }
    }

    public void SetPixel(int x, int y, Color c)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height) return;
        ActiveLayer?.SetPixel(Width, x, y, c);
    }

    public Color GetPixel(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height) return Colors.Transparent;
        int idx = (y * Width + x) * 4;
        if (idx + 3 >= _compositeBuffer.Length) return Colors.Transparent;
        return Color.FromArgb(
            _compositeBuffer[idx + 3],
            _compositeBuffer[idx + 2],
            _compositeBuffer[idx + 1],
            _compositeBuffer[idx]);
    }

    public void FloodFill(int x, int y, Color newColor)
    {
        if (ActiveLayer == null) return;
        if (x < 0 || y < 0 || x >= Width || y >= Height) return;

        var oldColor = ActiveLayer.GetPixel(Width, x, y);
        if (oldColor == newColor) return;

        var stack = new Stack<(int x, int y)>();
        stack.Push((x, y));
        int safety = Width * Height + 10;

        while (stack.Count > 0 && safety-- > 0)
        {
            var (cx, cy) = stack.Pop();
            if (cx < 0 || cy < 0 || cx >= Width || cy >= Height) continue;
            if (ActiveLayer.GetPixel(Width, cx, cy) != oldColor) continue;
            ActiveLayer.SetPixel(Width, cx, cy, newColor);
            stack.Push((cx + 1, cy));
            stack.Push((cx - 1, cy));
            stack.Push((cx, cy + 1));
            stack.Push((cx, cy - 1));
        }
    }

    public void DrawLine(int x0, int y0, int x1, int y1, Color c)
    {
        int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;
        while (true)
        {
            SetPixel(x0, y0, c);
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }

    public void DrawRect(int x0, int y0, int x1, int y1, Color c, bool filled)
    {
        int left = Math.Min(x0, x1), right = Math.Max(x0, x1);
        int top = Math.Min(y0, y1), bottom = Math.Max(y0, y1);
        if (filled)
        {
            for (int y = top; y <= bottom; y++)
                for (int x = left; x <= right; x++)
                    SetPixel(x, y, c);
        }
        else
        {
            for (int x = left; x <= right; x++) { SetPixel(x, top, c); SetPixel(x, bottom, c); }
            for (int y = top; y <= bottom; y++) { SetPixel(left, y, c); SetPixel(right, y, c); }
        }
    }

    public void DrawEllipse(int x0, int y0, int x1, int y1, Color c)
    {
        int cx = (x0 + x1) / 2, cy = (y0 + y1) / 2;
        int rx = Math.Abs(x1 - x0) / 2, ry = Math.Abs(y1 - y0) / 2;
        if (rx == 0 || ry == 0) { DrawLine(x0, y0, x1, y1, c); return; }

        int steps = Math.Max(rx, ry) * 8 + 16;
        for (int i = 0; i < steps; i++)
        {
            double t = 2 * Math.PI * i / steps;
            int x = cx + (int)Math.Round(rx * Math.Cos(t));
            int y = cy + (int)Math.Round(ry * Math.Sin(t));
            SetPixel(x, y, c);
        }
    }

    public byte[] SnapshotActiveLayer()
    {
        return ActiveLayer?.ClonePixels() ?? Array.Empty<byte>();
    }

    public void RestoreActiveLayer(byte[] data)
    {
        if (data.Length == 0) return;
        ActiveLayer?.RestorePixels(data);
    }

    public LayerNode AddLayer(LayerNode? parent = null)
    {
        var p = parent;
        if (p == null)
        {
            if (ActiveNode == null) p = Root;
            else if (ActiveNode.IsFolder) p = ActiveNode;
            else p = ActiveNode.Parent ?? Root;
        }

        var layer = new Layer(Width, Height, $"图层 {CountLayers() + 1}");
        var node = new LayerNode(NodeType.Layer, layer.Name) { Layer = layer };
        node.AttachTo(p);
        ActiveNode = node;
        return node;
    }

    public LayerNode AddFolder(LayerNode? parent = null)
    {
        var p = parent;
        if (p == null)
        {
            if (ActiveNode == null) p = Root;
            else if (ActiveNode.IsFolder) p = ActiveNode;
            else p = ActiveNode.Parent ?? Root;
        }

        var node = new LayerNode(NodeType.Folder, $"文件夹 {CountFolders() + 1}");
        node.AttachTo(p);
        return node;
    }

    public void RemoveNode(LayerNode node)
    {
        if (node == Root) return;
        if (node.IsLayer && CountLayers() <= 1) return;

        node.Detach();

        if (ActiveNode == null || !IsInTree(ActiveNode))
            ActiveNode = FindFirstLayer(Root);
    }

    private bool IsInTree(LayerNode node)
    {
        var cur = node;
        while (cur != null)
        {
            if (cur == Root) return true;
            cur = cur.Parent;
        }
        return false;
    }

    private LayerNode? FindFirstLayer(LayerNode node)
    {
        foreach (var child in node.Children)
        {
            if (child.IsLayer) return child;
            var f = FindFirstLayer(child);
            if (f != null) return f;
        }
        return null;
    }

    public int CountLayers()
    {
        int count = 0;
        CountLayersRec(Root, ref count);
        return count;
    }

    private void CountLayersRec(LayerNode node, ref int count)
    {
        foreach (var child in node.Children)
        {
            if (child.IsLayer) count++;
            else CountLayersRec(child, ref count);
        }
    }

    public int CountFolders()
    {
        int count = 0;
        CountFoldersRec(Root, ref count);
        return count;
    }

    private void CountFoldersRec(LayerNode node, ref int count)
    {
        foreach (var child in node.Children)
        {
            if (child.IsFolder)
            {
                count++;
                CountFoldersRec(child, ref count);
            }
        }
    }
}