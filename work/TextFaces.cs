using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;

internal sealed partial class TextFaces
{
    private readonly ThemeSettings options;
    private TextFaces(ThemeSettings style) { options=style; }
    public const int Width = 468, Height = 156;
    private static bool Latin(string word)
    {
        foreach (char c in word) if (c < 'A' || c > 'Z') return false;
        return word.Length > 0;
    }

    private static string Spaced(string word)
    {
        return Latin(word) ? string.Join("\u2009", Array.ConvertAll(word.ToCharArray(), c => c.ToString())) : word;
    }

    // Fit the ink bounds so italic overhangs and long weekdays stay inside the widget.
    private void Lettering(Graphics g, string text, string font, FontStyle style, Color color,
        float x, float y, float width, float height, bool left, bool outline=false, bool detail=false)
    {
        if(string.IsNullOrEmpty(text)) return;
        font=options.FontName(font);
        color=options.TextColor(height>=32 && !detail,color);
        using (var family = new FontFamily(font))
        using (var path = new GraphicsPath())
        using (var matrix = new Matrix())
        using (var ink = new SolidBrush(color))
        {
            path.AddString(text, family, (int)style, 64, PointF.Empty, StringFormat.GenericTypographic);
            RectangleF bounds = path.GetBounds();
            if (bounds.Width <= 0 || bounds.Height <= 0) return;
            float scale = Math.Min(height / bounds.Height, width / bounds.Width);
            float start = left ? x : x + (width - bounds.Width * scale) / 2;
            matrix.Translate(start - bounds.X * scale, y + (height - bounds.Height * scale) / 2 - bounds.Y * scale);
            matrix.Scale(scale, scale);
            path.Transform(matrix);
            if(outline) { using(var pen=new Pen(color,1.8f)) g.DrawPath(pen,path); }
            else g.FillPath(ink, path);
        }
    }

    public static void Draw(Graphics g, RectangleF box, Face face, ClockReading r)
    {
        new TextFaces(r.Style).DrawCore(g,box,face,r);
    }
    private void DrawCore(Graphics g, RectangleF box, Face face, ClockReading r)
    {
        GraphicsState state = g.Save();
        try
        {
            float scale = Math.Min(box.Width/Width, box.Height/Height);
            g.TranslateTransform(box.X + (box.Width-Width*scale)/2, box.Y + (box.Height-Height*scale)/2);
            g.ScaleTransform(scale,scale);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var culture = CultureInfo.CurrentCulture;
            string day = r.Now.ToString("dddd",culture), caps = day.ToUpper(culture);
            string date = r.Caption;
            Color white = Color.White;
            if(face>=Face.BareMinute) { DrawNewFace(g,face,r); return; }
            if(options.DateIndex==4)
            {
                string[] families={"Segoe UI Semibold","Georgia","Segoe UI Light","Arial Black","Arial","Consolas"};
                FontStyle[] styles={FontStyle.Regular,FontStyle.Italic,FontStyle.Regular,FontStyle.Regular,FontStyle.Bold|FontStyle.Italic,FontStyle.Bold};
                Color[] inks={white,Color.FromArgb(255,244,221),white,Color.FromArgb(33,32,37),white,Color.FromArgb(198,239,201)};
                int index=(int)face-(int)Face.OrbitStencil;
                Lettering(g,r.Time,families[index],styles[index],inks[index],14,40,440,72,false);
                return;
            }
            switch (face)
            {
                case Face.SilkItalic:
                    Color ivory = Color.FromArgb(255,244,221);
                    Lettering(g,day,"Georgia",FontStyle.Italic,ivory,12,10,444,64,false);
                    Lettering(g,date,"Georgia",FontStyle.Regular,ivory,16,88,436,15,false);
                    Lettering(g,r.Time,"Georgia",FontStyle.Italic,ivory,16,119,436,17,false);
                    break;
                case Face.Hairline:
                    Lettering(g,Spaced(caps),"Segoe UI Light",FontStyle.Regular,white,12,25,444,38,false);
                    Lettering(g,date.ToUpper(culture),"Segoe UI",FontStyle.Regular,white,18,87,432,12,false);
                    Lettering(g,r.Time,"Segoe UI Light",FontStyle.Regular,white,18,115,432,19,false);
                    break;
                case Face.Carbon:
                    Color charcoal = Color.FromArgb(33,32,37);
                    Lettering(g,caps,"Arial Black",FontStyle.Regular,charcoal,12,17,444,57,true);
                    Lettering(g,date.ToUpper(culture),"Arial",FontStyle.Bold,charcoal,14,89,440,15,true);
                    Lettering(g,r.Time,"Consolas",FontStyle.Bold,charcoal,14,116,440,18,true);
                    break;
                case Face.Echo:
                    Lettering(g,caps,"Arial",FontStyle.Bold|FontStyle.Italic,Color.FromArgb(222,91,177),16,22,440,50,false);
                    Lettering(g,caps,"Arial",FontStyle.Bold|FontStyle.Italic,Color.FromArgb(99,222,221),10,17,440,50,false);
                    Lettering(g,caps,"Arial",FontStyle.Bold|FontStyle.Italic,white,12,19,440,50,false);
                    Lettering(g,date.ToUpper(culture),"Consolas",FontStyle.Bold,white,12,88,444,15,false);
                    Lettering(g,r.Time,"Consolas",FontStyle.Bold,Color.FromArgb(166,249,240),12,119,444,15,false);
                    break;
                case Face.FieldNotes:
                    Color mint = Color.FromArgb(198,239,201);
                    Lettering(g,Spaced(caps),"Consolas",FontStyle.Bold,mint,14,21,440,47,true);
                    Lettering(g,date.ToUpper(culture)+"   /   "+r.Time,"Consolas",FontStyle.Regular,mint,16,91,436,17,true);
                    break;
                default:
                    if (options.FontIndex!=0 || !DrawStencil(g,caps))
                        Lettering(g,Spaced(caps),"Segoe UI Semibold",FontStyle.Regular,white,12,22,444,41,false);
                    Lettering(g,date.ToUpper(culture)+(options.DateIndex==0 ? "." : ""),"Segoe UI",FontStyle.Bold,white,18,86,432,15,false);
                    Lettering(g,"-  "+r.Time+"  -","Segoe UI",FontStyle.Bold,white,18,119,432,14,false);
                    break;
            }
        }
        finally { g.Restore(state); }
    }

    // Original open-stroke capitals for the seven English day names.
    private bool DrawStencil(Graphics g, string word)
    {
        foreach(char c in word) if ("ADEFIHMNORSTUWY".IndexOf(c) < 0) return false;
        if(word.Length == 0) return false;
        const float advance=34, glyph=23;
        float width=(word.Length-1)*advance+glyph+4;
        float scale=Math.Min(1.75f,440/width);
        GraphicsState state=g.Save();
        try
        {
            g.TranslateTransform((Width-width*scale)/2+2*scale,22+(43-24*scale)/2);
            g.ScaleTransform(scale,scale);
            using(var pen=new Pen(options.TextColor(true,Color.White),3.2f))
            {
                pen.LineJoin=LineJoin.Bevel;
                foreach(char c in word)
                {
                    switch(c)
                    {
                        case 'A': Stroke(g,pen,0,23,11,1,22,23); g.DrawLine(pen,11,17,17,17); break;
                        case 'D': g.DrawBezier(pen,2,1,29,-1,29,25,2,23); g.DrawLine(pen,2,9,2,18); break;
                        case 'E': Stroke(g,pen,21,1,2,1,2,23,21,23); g.DrawLine(pen,8,12,17,12); break;
                        case 'F': Stroke(g,pen,21,1,2,1,2,23); g.DrawLine(pen,8,12,18,12); break;
                        case 'H': g.DrawLine(pen,1,1,1,23); g.DrawLine(pen,21,1,21,23); g.DrawLine(pen,7,12,21,12); break;
                        case 'I': g.DrawLine(pen,11,1,11,23); break;
                        case 'M': Stroke(g,pen,0,23,1,1,11,15,21,1,22,23); break;
                        case 'N': Stroke(g,pen,1,23,1,1,21,23); g.DrawLine(pen,21,1,21,16); break;
                        case 'O': g.DrawArc(pen,0,1,22,22,310,330); break;
                        case 'R': g.DrawBezier(pen,1,1,25,-1,26,15,8,14); g.DrawLine(pen,9,14,20,23); g.DrawLine(pen,2,9,2,23); break;
                        case 'S': g.DrawBezier(pen,9,1,-2,1,-2,12,10,12); g.DrawBezier(pen,10,12,24,11,25,23,13,23); break;
                        case 'T': g.DrawLine(pen,0,1,22,1); g.DrawLine(pen,11,6,11,23); break;
                        case 'U': Stroke(g,pen,1,1,1,13); g.DrawBezier(pen,1,13,1,27,21,27,21,13); g.DrawLine(pen,21,1,21,8); break;
                        case 'W': Stroke(g,pen,0,1,5,23,11,11,17,23,23,1); break;
                        case 'Y': Stroke(g,pen,1,1,11,13,11,23); g.DrawLine(pen,14,10,22,1); break;
                    }
                    g.TranslateTransform(advance,0);
                }
            }
        }
        finally { g.Restore(state); }
        return true;
    }
    private static void Stroke(Graphics g, Pen pen, params float[] coords)
    {
        var points=new PointF[coords.Length/2];
        for(int i=0;i<points.Length;i++) points[i]=new PointF(coords[2*i],coords[2*i+1]);
        g.DrawLines(pen,points);
    }
}
