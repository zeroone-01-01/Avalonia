﻿#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.Immutable;
using CrossUI;

#if AVALONIA_SKIA
namespace Avalonia.Skia.RenderTests.CrossUI;
#else
namespace Avalonia.Direct2D1.RenderTests.CrossUI;
#endif

class AvaloniaCrossControl : Control
{
    private readonly CrossControl _src;
    private readonly Dictionary<CrossControl, AvaloniaCrossControl> _children;

    public AvaloniaCrossControl(CrossControl src)
    {
        _src = src;
        _children = src.Children.ToDictionary(x => x, x => new AvaloniaCrossControl(x));
        Width = src.Bounds.Width;
        Height = src.Bounds.Height;
        foreach (var ch in src.Children)
        {
            var c = _children[ch];
            VisualChildren.Add(c);
            LogicalChildren.Add(c);
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (var ch in _children)
            ch.Value.Measure(ch.Key.Bounds.Size);
        return _src.Bounds.Size;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach (var ch in _children)
            ch.Value.Arrange(ch.Key.Bounds);
        return base.ArrangeOverride(finalSize);
    }

    public override void Render(DrawingContext context)
    {
        _src.Render(new AvaloniaCrossDrawingContext(context));
    }
}

class AvaloniaCrossDrawingContext : ICrossDrawingContext
{
    private readonly DrawingContext _ctx;

    public AvaloniaCrossDrawingContext(DrawingContext ctx)
    {
        _ctx = ctx;
    }

    static Transform? ConvertTransform(Matrix? m) => m == null ? null : new MatrixTransform(m.Value);

    static RelativeRect ConvertRect(Rect rc, BrushMappingMode mode)
        => new RelativeRect(rc,
            mode == BrushMappingMode.RelativeToBoundingBox ? RelativeUnit.Relative : RelativeUnit.Absolute);


    static Geometry ConvertGeometry(CrossGeometry g)
    {
        if (g is CrossRectangleGeometry rg)
            return new RectangleGeometry(rg.Rect);
        else if (g is CrossSvgGeometry svg)
            return PathGeometry.Parse(svg.Path);
        throw new NotSupportedException();
    }
    static Drawing ConvertDrawing(CrossDrawing src)
    {
        if (src is CrossDrawingGroup g)
            return new DrawingGroup() { Children = new DrawingCollection(g.Children.Select(ConvertDrawing)) };
        if (src is CrossGeometryDrawing geo)
            return new GeometryDrawing()
            {
                Geometry = ConvertGeometry(geo.Geometry), Brush = ConvertBrush(geo.Brush), Pen = ConvertPen(geo.Pen)
            };
        throw new NotSupportedException();
    }
    
    static IBrush? ConvertBrush(CrossBrush? brush)
    {
        if (brush == null)
            return null;
        static Brush Sync(Brush dst, CrossBrush src)
        {
            dst.Opacity = src.Opacity;
            dst.Transform = ConvertTransform(src.Transform);
            if (src.RelativeTransform != null)
                throw new PlatformNotSupportedException();
            return dst;
        }

        static Brush SyncTile(TileBrush dst, CrossTileBrush src)
        {
            dst.Stretch = src.Stretch;
            dst.AlignmentX = src.AlignmentX;
            dst.AlignmentY = src.AlignmentY;
            dst.TileMode = src.TileMode;
            dst.SourceRect = ConvertRect(src.Viewbox, src.ViewboxUnits);
            dst.DestinationRect = ConvertRect(src.Viewport, src.ViewportUnits);
            return Sync(dst, src);
        }

        if (brush is CrossSolidColorBrush br)
            return Sync(new SolidColorBrush(br.Color), brush);
        if (brush is CrossDrawingBrush db)
            return SyncTile(new DrawingBrush(ConvertDrawing(db.Drawing)), db);
        throw new NotSupportedException();
    }

    static IPen? ConvertPen(CrossPen? pen)
    {
        if (pen == null)
            return null;
        return new Pen(ConvertBrush(pen.Brush), pen.Thickness);
    }

    static IImage ConvertImage(CrossImage image)
    {
        if (image is CrossBitmapImage bi)
            return new Bitmap(bi.Path);
        if (image is CrossDrawingImage di)
            return new DrawingImage(ConvertDrawing(di.Drawing));
        throw new NotSupportedException();
    }
    
    public void DrawRectangle(CrossBrush? brush, CrossPen? pen, Rect rc) => _ctx.DrawRectangle(ConvertBrush(brush), ConvertPen(pen), rc);
    public void DrawImage(CrossImage image, Rect rc) => _ctx.DrawImage(ConvertImage(image), rc);
}
