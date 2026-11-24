using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;

internal enum Face { Graphite, SundayPress, MossDial, SplitFlip, PocketLCD, SideA, NightDrive, StickerClub, Blueprint, Gilded, FlowerPower, DayPass, OrbitStencil, SilkItalic, Hairline, Carbon, Echo, FieldNotes,
    BareMinute, Offset, Almanac, Stack, Meridian, Byline, Outline, Punch, Signal, Cursive, Horizon, Duotone }

internal sealed class ClockReading
{
    public readonly DateTime Now;
    public readonly string Time, Digits, Marker, Date, Weekday, Day, Month;
    public readonly bool MarkerFirst;
    public readonly ThemeSettings Style;
    public readonly string Caption;

    public ClockReading(DateTime now) : this(now,new ThemeSettings()) { }
    public ClockReading(DateTime now, ThemeSettings settings)
    {
        Now = now;
        CultureInfo culture = CultureInfo.CurrentCulture;
        Style=settings;
        Time = now.ToString(settings.TimeIndex==1 ? "h:mm tt" : settings.TimeIndex==2 ? "HH:mm" : "t", culture).Trim();
        string marker = now.ToString("tt", culture);
        MarkerFirst = marker.Length > 0 && Time.StartsWith(marker, StringComparison.Ordinal);
        bool marked = MarkerFirst || (marker.Length > 0 && Time.EndsWith(marker, StringComparison.Ordinal));
        Marker = marked ? marker : "";
        Digits = !marked ? Time : (MarkerFirst ? Time.Substring(marker.Length) : Time.Substring(0, Time.Length - marker.Length)).Trim();
        Date = now.ToString("D", culture);
        Weekday = now.ToString("ddd", culture);
        if (!culture.DateTimeFormat.LongDatePattern.Contains("ddd")) Date = Weekday + "  ·  " + Date;
        Day = now.ToString("dd", culture);
        Month = now.ToString("MMM", culture);
        Caption=now.ToString("d MMMM, yyyy",culture);
        if(settings.DateIndex==1) Caption=Date=now.ToString("d",culture);
        if(settings.DateIndex==2) Caption=Date=now.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture);
        if(settings.DateIndex==3) Caption=Date=now.ToString("D",culture);
        if(settings.DateIndex==4) Caption=Date=Day=Month=Weekday="";
    }
}

// All faces use the same logical drawing area on the widget and in the gallery.
internal sealed class FaceArt
{
    public const int Width = 272, Height = 132;
    public static readonly string[] Names = { "Graphite", "Sunday Press", "Moss Dial", "Split Flip", "Pocket LCD", "Side A", "Night Drive", "Sticker Club", "Blueprint", "Gilded", "Flower Power", "Day Pass", "Orbit Stencil", "Silk Italic", "Hairline", "Carbon", "Echo", "Field Notes",
        "Bare Minute", "Offset", "Almanac", "Stack", "Meridian", "Byline", "Outline", "Punch", "Signal", "Cursive", "Horizon", "Duotone" };
    public static bool IsTextOnly(Face face) { return face >= Face.OrbitStencil; }
    private readonly Graphics g;
    private readonly ClockReading r;
    private readonly Face design;
    private static readonly int[] CardAccents={0x47504b,0xa52e25,0xe4784a,0x353332,0xb2c58c,0xed825e,0xff996a,0xe6fa5a,0x87aecd,0xba9c62,0xc17ae3,0x252c32};
    private const string Sans = "Segoe UI", Serif = "Georgia", Mono = "Consolas", Heavy = "Arial Black";

    private FaceArt(Graphics graphics, ClockReading reading,Face face) { g = graphics; r = reading; design=face; }

    public static void Draw(Graphics g, RectangleF box, Face face, ClockReading reading)
    {
        if (IsTextOnly(face)) { TextFaces.Draw(g, box, face, reading); return; }
        GraphicsState original = g.Save();
        try
        {
            g.TranslateTransform(box.X, box.Y);
            g.ScaleTransform(box.Width / Width, box.Height / Height);
            g.SetClip(new RectangleF(0, 0, Width, Height), CombineMode.Intersect);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            var art = new FaceArt(g, reading,face);
            switch (face)
            {
                case Face.SundayPress: art.Press(); break;
                case Face.MossDial: art.Dial(); break;
                case Face.SplitFlip: art.Flip(); break;
                case Face.PocketLCD: art.Lcd(); break;
                case Face.SideA: art.Cassette(); break;
                case Face.NightDrive: art.Neon(); break;
                case Face.StickerClub: art.Stickers(); break;
                case Face.Blueprint: art.Plan(); break;
                case Face.Gilded: art.Gold(); break;
                case Face.FlowerPower: art.Flowers(); break;
                case Face.DayPass: art.Ticket(); break;
                default: art.Minimal(); break;
            }
        }
        finally { g.Restore(original); }
    }

    private Color C(int rgb) { return rgb==CardAccents[(int)design] && !r.Style.Accent.IsEmpty ? r.Style.Accent : ThemeSettings.Rgb(rgb); }
    private void Rect(int c, float x, float y, float w, float h)
    { if(r.Style.ClearCard) return; using (var b = new SolidBrush(C(c))) g.FillRectangle(b, x, y, w, h); }
    private void Dot(int c, float x, float y, float radius)
    { if(r.Style.ClearCard) return; using (var b = new SolidBrush(C(c))) g.FillEllipse(b, x-radius, y-radius, radius*2, radius*2); }
    private void Ring(int c, float x, float y, float radius, float thick)
    { if(r.Style.ClearCard) return; using (var p = new Pen(C(c), thick)) g.DrawEllipse(p, x-radius, y-radius, radius*2, radius*2); }
    private void Line(int c, float x, float y, float xx, float yy, float thick)
    { if(r.Style.ClearCard) return; using (var p = new Pen(C(c), thick)) g.DrawLine(p, x, y, xx, yy); }
    private void Frame(int c, int x, int y, int w, int h)
    { if(r.Style.ClearCard) return; using (var p = new Pen(C(c))) g.DrawRectangle(p, x, y, w, h); }
    private void Fill(int c) { Rect(r.Style.Background.IsEmpty ? c : r.Style.Background.ToArgb() & 0xffffff, 0, 0, Width, Height); }
    private void Label(string text, string family, float size, int color, float x, float y, float w, float h, bool center, FontStyle style)
    {
        if (text.Length == 0) return;
        family=r.Style.FontName(family);
        using (var format = (StringFormat)StringFormat.GenericTypographic.Clone())
        using (var initial = new Font(family, size, style, GraphicsUnit.Pixel))
        using (var ink = new SolidBrush(r.Style.TextColor(size>=32,C(color))))
        {
            format.FormatFlags |= StringFormatFlags.NoWrap;
            format.Alignment = center ? StringAlignment.Center : StringAlignment.Near;
            format.LineAlignment = StringAlignment.Center;
            SizeF measured = g.MeasureString(text, initial, PointF.Empty, format);
            float fit = Math.Min(1f, Math.Min((w-2) / Math.Max(1, measured.Width), h / Math.Max(1, measured.Height)));
            using (var font = new Font(family, size * fit, style, GraphicsUnit.Pixel))
                g.DrawString(text, font, ink, new RectangleF(x, y, w, h), format);
        }
    }
    private void Time(string family, int size, int c, int x, int y, int w, int h, bool center, FontStyle style)
    { Label(r.Time, family, size, c, x, y, w, h, center, style); }
    private void DateLine(int c, int x, int y, int w, int h, bool center)
    { Label(r.Date, Sans, 14, c, x, y, w, h, center, FontStyle.Regular); }

    private void Minimal()
    {
        Fill(0x1f2223);
        Time("Segoe UI Light", 59, 0xf6f5ef, 21, 3, 230, 76, false, FontStyle.Regular);
        Line(0x47504b, 22, 84, 250, 84, 1);
        DateLine(0xbecac4, 22, 87, 230, 34, false);
    }
    private void Press()
    {
        Fill(0xf5eedc);
        Line(0xa52e25, 18, 14, 254, 14, 3);
        Label(r.Date, Serif, 14, 0x453b30, 18, 18, 236, 24, true, FontStyle.Italic);
        Line(0x453b30, 18, 45, 254, 45, 1);
        Time(Serif, 62, 0x322b25, 15, 44, 242, 76, true, FontStyle.Regular);
        Line(0x453b30, 18, 118, 254, 118, 1);
    }
    private void Hand(double turns, float length, float thick, int c)
    {
        double a = turns * Math.PI * 2;
        Line(c, 61, 66, 61 + (float)Math.Sin(a)*length, 66 - (float)Math.Cos(a)*length, thick);
    }
    private void Dial()
    {
        Fill(0xdce7d4); Dot(0xf5f6e8,61,66,46); Ring(0x788f72,61,66,46,1.5f);
        for (int i=0; i<12; i++)
        {
            double a = i*Math.PI/6;
            Dot(0x55735d, 61 + (float)Math.Sin(a)*38, 66 - (float)Math.Cos(a)*38, i%3==0 ? 2 : 1);
        }
        int handColor=r.Style.TextColor(true,C(0x273c30)).ToArgb() & 0xffffff;
        Hand((r.Now.Hour + r.Now.Minute/60.0)/12, 22, 4, handColor);
        Hand(r.Now.Minute/60.0, 32, 2, handColor); Dot(0xe4784a,61,66,4);
        Time(Sans, 40, 0x253e30, 120, 28, 138, 52, false, FontStyle.Regular);
        DateLine(0x415948,120,78,139,26,false);
    }
    private void Flip()
    {
        Fill(0x191818);
        string separator = CultureInfo.CurrentCulture.DateTimeFormat.TimeSeparator;
        string[] parts = separator.Length == 0 ? new string[0] : r.Digits.Split(new string[]{separator}, StringSplitOptions.None);
        Rect(0x353332,14,13,117,75); Rect(0x353332,141,13,117,75);
        if (parts.Length == 2)
        {
            Label(parts[0], "Arial", 65, 0xf5e7cb, 18, 12, 109, 75, true, FontStyle.Bold);
            Label(parts[1], "Arial", 65, 0xf5e7cb, 145, 12, 109, 75, true, FontStyle.Bold);
        }
        else Label(r.Digits, "Arial", 62, 0xf5e7cb, 18, 12, 236, 75, true, FontStyle.Bold);
        Line(0x191818,14,51,258,51,1.6f);
        DateLine(0xe9dac4, 14, 94, r.Marker.Length == 0 ? 244 : 195, 27, false);
        Label(r.Marker, Mono, 13, 0xeaae72, 214, 94, 44, 27, true, FontStyle.Bold);
    }
    private void Lcd()
    {
        Fill(0x595c53); Rect(0x333b30,10,10,252,112); Rect(0xb2c58c,14,14,244,104);
        Frame(0x91a373,18,18,235,95);
        const int active=0x293923, ghost=0xa4b681;
        string numerals = "";
        foreach (char ch in r.Digits) if (char.IsDigit(ch)) numerals += (int)char.GetNumericValue(ch);
        if (!r.Style.ClearCard && r.Style.FontIndex==0 && (numerals.Length == 3 || numerals.Length == 4))
        {
            int x = numerals.Length == 3 ? 55 : 36;
            int[] masks = {63,6,91,79,102,109,125,7,127,111};
            for (int i=0; i<numerals.Length; i++)
            {
                if (i==numerals.Length-2) { Rect(active,x+2,41,5,5); Rect(active,x+2,61,5,5); x+=15; }
                int mask=masks[numerals[i]-'0'];
                int[,] bars={{4,0,23,5},{27,4,5,20},{27,29,5,20},{4,48,23,5},{0,29,5,20},{0,4,5,20},{4,24,23,5}};
                int lit=r.Style.TextColor(true,C(active)).ToArgb() & 0xffffff;
                for (int j=0; j<7; j++) Rect((mask & (1<<j)) != 0 ? lit : ghost,x+bars[j,0],29+bars[j,1],bars[j,2],bars[j,3]);
                x+=41;
            }
            Label(r.Marker,Mono,12,active,215,27,32,21,true,FontStyle.Bold);
        }
        else Time(Mono,54,active,24,22,224,64,true,FontStyle.Bold);
        Label(r.Date,Mono,13,active,22,87,228,23,true,FontStyle.Bold);
    }
    private void Cassette()
    {
        Fill(0x2b2836); Rect(0xf4ddbc,13,11,246,75);
        Rect(0xed825e,13,65,246,6); Rect(0xd3586c,13,71,246,6); Rect(0x835e91,13,77,246,6);
        Time("Arial",50,0x302735,23,10,226,54,true,FontStyle.Bold);
        Rect(0x14151b,39,87,194,23);
        foreach(int x in new int[]{37,235}) { Dot(0xd4bbb2,x,98,12); Dot(0x524452,x,98,7); Dot(0x17151b,x,98,3); }
        Label(r.Date,Mono,13,0xf7e5cf,54,87,164,23,true,FontStyle.Regular);
        foreach(int x in new int[]{10,262}) { Dot(0x9a8d91,x,120,2); }
    }
    private void Neon()
    {
        if(!r.Style.ClearCard && r.Style.Background.IsEmpty)
        {
            using(var gradient=new LinearGradientBrush(new Rectangle(0,0,272,132),C(0x361745),C(0x131631),90f)) g.FillRectangle(gradient,0,0,272,132);
        }
        else Fill(0x361745);
        Dot(0xff996a,225,40,26);
        for(int i=0;i<4;i++) Rect(0x361745,196,47+i*5,58,1+i);
        for(int i=-4;i<=4;i++) Line(0x65467e,139+i*12,88,139+i*73,132,1);
        foreach(int y in new int[]{88,93,102,117}) Line(0x6c4c8b,0,y,272,y,1);
        Time("Arial",54,0xf25cbe,17,12,178,66,false,FontStyle.Bold | FontStyle.Italic);
        Time("Arial",54,0x9ff8f0,15,10,178,66,false,FontStyle.Bold | FontStyle.Italic);
        Rect(0x211b37,10,98,252,23);
        Label(r.Date,Mono,13,0xf4cbe7,13,97,246,24,true,FontStyle.Regular);
    }
    private void Burst(float cx,float cy,float outer,float inner,int points,int c)
    {
        if(r.Style.ClearCard) return;
        var p=new PointF[points*2];
        for(int i=0;i<p.Length;i++)
        {
            double a=Math.PI*i/points-Math.PI/2; float radius=i%2==0?outer:inner;
            p[i]=new PointF(cx+radius*(float)Math.Cos(a),cy+radius*(float)Math.Sin(a));
        }
        using(var b=new SolidBrush(C(c))) g.FillPolygon(b,p);
    }
    private void Stickers()
    {
        Fill(0xff885e); Burst(241,17,40,22,12,0x29243a); Burst(241,17,34,19,12,0xe6fa5a);
        GraphicsState s=g.Save(); g.TranslateTransform(14,26); g.RotateTransform(-4);
        Rect(0x282635,4,5,205,61); Rect(0xfff6df,0,0,205,61);
        g.Restore(s);
        Time(Heavy,47,0x292537,24,19,183,58,true,FontStyle.Regular);
        Label(r.Date,Sans,14,0x282335,14,89,244,23,false,FontStyle.Bold);
        for(int x=0;x<272;x+=10) for(int y=0;y<2;y++) Rect((x/10+y)%2==0?0x2c2737:0xfff3d4,x,116+y*9,10,9);
    }
    private void Plan()
    {
        Fill(0x184778);
        for(int x=0;x<272;x+=12) Line(0x2b5885,x,0,x,132,0.6f);
        for(int y=0;y<132;y+=12) Line(0x2b5885,0,y,272,y,0.6f);
        Frame(0x87aecd,15,14,242,104);
        foreach(int x in new int[]{15,257}) foreach(int y in new int[]{14,118})
        { Line(0xe9cf81,x-5,y,x+5,y,1); Line(0xe9cf81,x,y-5,x,y+5,1); }
        Time(Mono,55,0xe9f2f3,25,13,222,69,false,FontStyle.Regular);
        Line(0x87aecd,15,83,257,83,1);
        Label(r.Date,Mono,13,0xf3d99b,25,85,222,28,false,FontStyle.Regular);
    }
    private void Gold()
    {
        Fill(0x122a2b); Frame(0xa58c54,10,10,252,112); Frame(0x76683f,15,15,242,102);
        foreach(int x in new int[]{15,257}) foreach(int y in new int[]{15,117})
        { int dx=x==15?1:-1, dy=y==15?1:-1; Line(0xe6c88b,x+dx*16,y,x,y+dy*16,1.5f); }
        Time(Serif,57,0xf0d89e,24,17,224,66,true,FontStyle.Regular);
        Line(0xba9c62,85,84,127,84,1); Burst(136,84,5,2.5f,4,0xe5c884); Line(0xba9c62,145,84,187,84,1);
        DateLine(0xe2d9b8,22,87,228,25,true);
    }
    private void Flower(float x,float y,float radius,int petals,int core)
    {
        for(int i=0;i<6;i++)
        {
            double a=i*Math.PI/3; Dot(petals,x+(float)Math.Cos(a)*radius,y+(float)Math.Sin(a)*radius,radius*.76f);
        }
        Dot(core,x,y,radius*.66f);
    }
    private void Flowers()
    {
        Fill(0xf7c2dd); Flower(252,43,23,0xc17ae3,0xffe071); Flower(11,107,15,0xf194b8,0xffedac);
        Time("Georgia",57,0x612d70,18,10,196,71,false,FontStyle.Bold | FontStyle.Italic);
        Rect(0xffe5f1,29,86,214,28);
        Label(r.Date,Sans,14,0x67346b,33,86,206,28,true,FontStyle.Regular);
    }
    private void Ticket()
    {
        Fill(0xf5d761); Rect(0x252c32,202,0,70,132);
        for(int y=7;y<132;y+=9) Line(0x775f29,195,y,195,y+4,1);
        Time("Arial",50,0x262b30,12,14,177,64,false,FontStyle.Bold);
        Line(0x796b3e,13,79,183,79,1);
        DateLine(0x32342b,13,84,175,32,false);
        Label(r.Month.ToUpper(CultureInfo.CurrentCulture),Mono,16,0xf4d763,209,17,57,26,true,FontStyle.Bold);
        Label(r.Day,"Arial",40,0xfffae8,206,39,62,51,true,FontStyle.Bold);
        int x=215;
        foreach(int w in new int[]{2,1,3,2,1,4,1,2,2,3}) { Rect(0xf5d761,x,97,w,14); x+=w+2; }
    }
}
