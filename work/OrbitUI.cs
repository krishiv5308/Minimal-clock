using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

internal static class OrbitUI
{
    public static readonly Color Page=Rgb(0x14151b), Surface=Rgb(0x202129), Raised=Rgb(0x292a35),
        Line=Rgb(0x383944), Text=Rgb(0xf5f3fb), Muted=Rgb(0xb4b1c2), Accent=Rgb(0xcab6ff), Deep=Rgb(0x282035);
    public static Color Rgb(int c){return Color.FromArgb((c>>16)&255,(c>>8)&255,c&255);}
    public static Color Mix(Color a,Color b,float t){return Color.FromArgb((int)(a.R+(b.R-a.R)*t),(int)(a.G+(b.G-a.G)*t),(int)(a.B+(b.B-a.B)*t));}
    [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr h,int id,ref int value,int len);
    public static void Frame(Form window)
    {
        int dark=1; DwmSetWindowAttribute(window.Handle,20,ref dark,4);
        Icon appIcon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        window.Icon=appIcon;
        window.Disposed+=delegate{if(appIcon!=null)appIcon.Dispose();};
        window.ShowIcon=true;
    }
    public static GraphicsPath Round(RectangleF rect,float radius)
    {
        var path=new GraphicsPath();
        float d=Math.Min(Math.Min(rect.Width,rect.Height),radius*2);
        if(d<1){path.AddRectangle(rect);return path;}
        path.AddArc(rect.Left,rect.Top,d,d,180,90);path.AddArc(rect.Right-d,rect.Top,d,d,270,90);
        path.AddArc(rect.Right-d,rect.Bottom-d,d,d,0,90);path.AddArc(rect.Left,rect.Bottom-d,d,d,90,90);path.CloseFigure();
        return path;
    }
    public static void Box(Graphics g,RectangleF rect,float radius,Color fill,Color edge,float stroke)
    {
        using(var path=Round(rect,radius))
        {
            if(fill.A!=0) using(var brush=new SolidBrush(fill)) g.FillPath(brush,path);
            if(stroke>0) using(var pen=new Pen(edge,stroke)) g.DrawPath(pen,path);
        }
    }
    public static void Type(Graphics g,string value,Font font,Color color,Rectangle area,bool center=false)
    {
        TextRenderer.DrawText(g,value,font,area,color,TextFormatFlags.VerticalCenter|TextFormatFlags.NoPrefix|TextFormatFlags.NoPadding|
            TextFormatFlags.EndEllipsis|TextFormatFlags.SingleLine|(center?TextFormatFlags.HorizontalCenter:TextFormatFlags.Left));
    }
    public static RectangleF Fit(RectangleF space,Face design)
    {
        float ratio=FaceArt.IsTextOnly(design)?TextFaces.Width/(float)TextFaces.Height:FaceArt.Width/(float)FaceArt.Height;
        float w=Math.Min(space.Width,space.Height*ratio),h=w/ratio;
        return new RectangleF(space.X+(space.Width-w)/2,space.Y+(space.Height-h)/2,w,h);
    }
    // Original dusk landscape, drawn at the size of the preview.
    public static void Landscape(Graphics g,Rectangle area,bool light)
    {
        if(area.Width<1||area.Height<1)return;
        var state=g.Save();
        try
        {
            g.TranslateTransform(area.X,area.Y);g.ScaleTransform(area.Width/1000f,area.Height/260f);
            g.SmoothingMode=SmoothingMode.AntiAlias;
            Color[] sky=light?new[]{Rgb(0xb8b4d7),Rgb(0xe6c5ce),Rgb(0xffdbbe)}:new[]{Rgb(0x343252),Rgb(0x98769b),Rgb(0xf1aea4)};
            using(var b=new LinearGradientBrush(new Rectangle(0,0,1000,260),sky[0],sky[2],90f)){
                b.InterpolationColors=new ColorBlend {Colors=sky,Positions=new[]{0f,.65f,1f}}; g.FillRectangle(b,0,0,1000,260);
            }
            // Scale the sun in screen pixels so it stays circular in each layout.
            float rx=25*area.Height/260f*1000f/area.Width;
            using(var sun=new SolidBrush(light?Rgb(0xffedce):Rgb(0xffd9c4)))g.FillEllipse(sun,850-rx,56,rx*2,50);
            Ridge(g,light?0xb49db7:0x9d799a,new PointF[]{new PointF(-10,173),new PointF(85,169),new PointF(180,185),new PointF(272,158),new PointF(333,165),new PointF(420,148),new PointF(482,154),new PointF(568,123),new PointF(628,140),new PointF(745,107),new PointF(817,126),new PointF(890,112),new PointF(1010,145)});
            Ridge(g,light?0x998fab:0x785f86,new PointF[]{new PointF(-10,171),new PointF(96,149),new PointF(171,159),new PointF(227,133),new PointF(334,147),new PointF(422,169),new PointF(502,175),new PointF(622,157),new PointF(706,167),new PointF(817,142),new PointF(934,135),new PointF(1010,143)});
            Ridge(g,light?0x79768f:0x534a6c,new PointF[]{new PointF(-10,195),new PointF(98,182),new PointF(205,178),new PointF(298,154),new PointF(399,170),new PointF(502,192),new PointF(608,205),new PointF(703,188),new PointF(817,186),new PointF(911,168),new PointF(1010,177)});
            Ridge(g,light?0x605f77:0x353449,new PointF[]{new PointF(-10,190),new PointF(86,179),new PointF(172,186),new PointF(260,203),new PointF(371,212),new PointF(475,202),new PointF(554,209),new PointF(656,226),new PointF(740,214),new PointF(859,222),new PointF(950,200),new PointF(1010,190)});
        } finally{g.Restore(state);}
    }
    private static void Ridge(Graphics g,int rgb,PointF[] points)
    {
        using(var p=new GraphicsPath()) using(var b=new SolidBrush(Rgb(rgb))){
            p.AddCurve(points,.28f);p.AddLine(points[points.Length-1],new PointF(1010,263));p.AddLine(1010,263,-10,263);p.CloseFigure();g.FillPath(b,p);
        }
    }
    public static Color TileColor(Face f)
    {
        switch(f){
            case Face.Carbon:return Rgb(0xe6dece);
            case Face.SilkItalic:case Face.Cursive:return Rgb(0x684e66);
            case Face.OrbitStencil:return Rgb(0x7558b0);
            case Face.BareMinute:case Face.Offset:case Face.FieldNotes:case Face.Horizon:return Rgb(0x426969);
            case Face.Outline:case Face.Signal:case Face.Stack:case Face.Hairline:return Rgb(0x46587e);
            case Face.Punch:case Face.Echo:case Face.Duotone:return Rgb(0x785672);
            default:return Rgb(0x75605f);
        }
    }
}

internal sealed class OrbitFonts : IDisposable
{
    public readonly Font Brand=new Font("Segoe UI Semibold",16), Heading=new Font("Segoe UI Semibold",19),
        Display=new Font("Segoe UI Semibold",27), Body=new Font("Segoe UI",10), Strong=new Font("Segoe UI Semibold",10), Small=new Font("Segoe UI",9);
    public void Dispose(){Brand.Dispose();Heading.Dispose();Display.Dispose();Body.Dispose();Strong.Dispose();Small.Dispose();}
}

internal sealed class SurfacePanel : Panel
{
    public SurfacePanel(){DoubleBuffered=true;ResizeRedraw=true;}
}

internal class OrbitButton : Button
{
    public bool Primary,Selected;
    private bool over;
    public OrbitButton()
    {
        FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;Cursor=Cursors.Hand;
        DoubleBuffered=true;UseVisualStyleBackColor=false;BackColor=OrbitUI.Surface;ForeColor=OrbitUI.Text;
    }
    protected override void OnMouseEnter(EventArgs e){base.OnMouseEnter(e);over=true;Invalidate();}
    protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);over=false;Invalidate();}
    protected override void OnPaint(PaintEventArgs e)
    {
        var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
        Color fill=Primary?OrbitUI.Accent:Selected?OrbitUI.Raised:BackColor;
        if(over&&Enabled)fill=OrbitUI.Mix(fill,Color.White,.07f);
        float dpi=g.DpiX/96f;
        OrbitUI.Box(g,new RectangleF(dpi,dpi,Width-2*dpi,Height-2*dpi),8*dpi,fill,Selected?OrbitUI.Accent:OrbitUI.Line,Primary?0:dpi);
        Color ink=Enabled?(Primary?OrbitUI.Deep:OrbitUI.Text):OrbitUI.Muted;
        OrbitUI.Type(g,Text,Font,ink,ClientRectangle,true);
        if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(g,Rectangle.Inflate(ClientRectangle,-4,-4),ink,fill);
    }
}

internal sealed class OrbitChoice : ComboBox
{
    public OrbitChoice()
    {
        DropDownStyle=ComboBoxStyle.DropDownList;DrawMode=DrawMode.OwnerDrawFixed;
        SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer,true);
        BackColor=OrbitUI.Raised;ForeColor=OrbitUI.Text;
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g=e.Graphics;float d=g.DpiX/96f;g.SmoothingMode=SmoothingMode.AntiAlias;
        g.Clear(Parent==null?OrbitUI.Page:Parent.BackColor);
        OrbitUI.Box(g,new RectangleF(1,1,Width-2,Height-2),5*d,OrbitUI.Raised,Focused?OrbitUI.Accent:OrbitUI.Line,d);
        OrbitUI.Type(g,Text,Font,OrbitUI.Text,new Rectangle((int)(9*d),0,Width-(int)(34*d),Height));
        float x=Width-14*d,y=Height/2f;
        using(var p=new Pen(OrbitUI.Muted,1.5f*d))g.DrawLines(p,new[]{new PointF(x-4*d,y-2*d),new PointF(x,y+2*d),new PointF(x+4*d,y-2*d)});
    }
    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        if(e.Index<0)return;
        bool active=(e.State&DrawItemState.Selected)!=0;
        using(var b=new SolidBrush(active?OrbitUI.Deep:OrbitUI.Surface))e.Graphics.FillRectangle(b,e.Bounds);
        var r=e.Bounds;r.X+=6;r.Width-=8;
        OrbitUI.Type(e.Graphics,GetItemText(Items[e.Index]),Font,active?OrbitUI.Accent:OrbitUI.Text,r);
    }
    protected override void OnSelectedIndexChanged(EventArgs e){base.OnSelectedIndexChanged(e);Invalidate();}
    protected override void OnGotFocus(EventArgs e){base.OnGotFocus(e);Invalidate();}
    protected override void OnLostFocus(EventArgs e){base.OnLostFocus(e);Invalidate();}
}

// A mouse slider paired with the native numeric field for typing and keyboard input.
internal sealed class OrbitRange : Control
{
    private readonly NumericUpDown field;
    public OrbitRange(NumericUpDown number)
    {
        field=number;DoubleBuffered=true;Cursor=Cursors.Hand;TabStop=false;BackColor=OrbitUI.Page;
        AccessibleName=number.AccessibleName;AccessibleRole=AccessibleRole.Slider;
        field.ValueChanged+=delegate{Invalidate();};
    }
    private void Pick(int x)
    {
        var n=field.Minimum+(field.Maximum-field.Minimum)*(decimal)Math.Max(0,Math.Min(1,(x-9)/(double)Math.Max(1,Width-18)));
        field.Value=Math.Max(field.Minimum,Math.Min(field.Maximum,Math.Round(n)));
    }
    protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button==MouseButtons.Left){Capture=true;Pick(e.X);}}
    protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(Capture&&e.Button==MouseButtons.Left)Pick(e.X);}
    protected override void OnMouseUp(MouseEventArgs e){base.OnMouseUp(e);Capture=false;}
    protected override void OnPaint(PaintEventArgs e)
    {
        float d=e.Graphics.DpiX/96f, cy=Height/2f, x=9+(Width-18)*(float)((field.Value-field.Minimum)/(field.Maximum-field.Minimum));
        var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
        using(var p=new Pen(OrbitUI.Line,3*d)){p.StartCap=p.EndCap=LineCap.Round;g.DrawLine(p,9,cy,Width-9,cy);p.Color=OrbitUI.Accent;g.DrawLine(p,9,cy,x,cy);}
        using(var b=new SolidBrush(OrbitUI.Accent))g.FillEllipse(b,x-5*d,cy-5*d,10*d,10*d);
    }
}
