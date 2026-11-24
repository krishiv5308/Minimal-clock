using System;
using System.Drawing;
using System.Globalization;

internal sealed partial class TextFaces
{
    private void DrawNewFace(Graphics g,Face face,ClockReading r)
    {
        var culture=CultureInfo.CurrentCulture;
        bool date=options.DateIndex!=4;
        string weekday=date ? r.Now.ToString("dddd",culture) : "";
        string caps=weekday.ToUpper(culture);
        Color white=Color.White, linen=ThemeSettings.Rgb(0xffe8cc), pink=ThemeSettings.Rgb(0xffa3cc);
        Color mint=ThemeSettings.Rgb(0xbce5d6), sky=ThemeSettings.Rgb(0xb9e0ff);
        const FontStyle regular=FontStyle.Regular, bold=FontStyle.Bold;
        string hour=r.Digits, minute="";
        string sep=culture.DateTimeFormat.TimeSeparator;
        int split=sep.Length==0 ? -1 : hour.LastIndexOf(sep,StringComparison.Ordinal);
        if(split>0) { minute=hour.Substring(split+sep.Length); hour=hour.Substring(0,split); }
        switch(face)
        {
            case Face.BareMinute:
                Lettering(g,r.Time,"Segoe UI Light",regular,white,10,date?12:33,448,81,false);
                Lettering(g,r.Caption,"Segoe UI",regular,white,15,114,438,13,false);
                break;
            case Face.Offset:
                Lettering(g,r.Time,"Arial",bold,white,10,41,date?253:448,67,true);
                if(date)
                {
                    Lettering(g,caps,"Segoe UI Semibold",regular,mint,281,49,176,22,true);
                    Lettering(g,r.Caption,"Segoe UI",regular,white,281,88,176,14,true);
                }
                break;
            case Face.Almanac:
                if(date)
                {
                    Lettering(g,r.Day,"Georgia",regular,linen,12,13,152,124,false);
                    Lettering(g,caps,"Georgia",regular,linen,181,21,272,23,true);
                    Lettering(g,r.Caption,"Georgia",FontStyle.Italic,white,182,62,270,17,true);
                }
                Lettering(g,r.Time,"Georgia",regular,white,date?181:14,date?99:33,date?272:440,date?31:78,!date);
                break;
            case Face.Stack:
                Lettering(g,hour,"Arial Black",regular,white,16,5,180,62,false);
                Lettering(g,minute,"Arial Black",regular,white,16,82,180,62,false);
                Lettering(g,Spaced(caps),"Consolas",bold,sky,210,30,244,23,true);
                Lettering(g,r.Caption,"Consolas",regular,white,210,75,244,15,true);
                Lettering(g,r.Marker,"Consolas",bold,sky,210,112,244,15,true);
                break;
            case Face.Meridian:
                DrawDial(g,r,white,linen);
                Lettering(g,r.Time,"Georgia",regular,white,164,date?28:50,284,55,true);
                Lettering(g,r.Caption,"Segoe UI",regular,linen,166,104,282,16,true);
                break;
            case Face.Byline:
                Lettering(g,weekday,"Georgia",FontStyle.Italic,linen,12,9,436,49,true);
                Lettering(g,r.Time,"Georgia",regular,white,12,date?76:39,259,63,true);
                Lettering(g,r.Caption,"Georgia",FontStyle.Italic,linen,280,102,176,16,false);
                break;
            case Face.Outline:
                Lettering(g,r.Time,"Arial",bold,white,10,date?10:28,448,91,false,true);
                Lettering(g,r.Caption.ToUpper(culture),"Consolas",regular,sky,14,121,440,13,false);
                break;
            case Face.Punch:
                Lettering(g,r.Time,"Arial Black",FontStyle.Italic,linen,8,date?10:28,452,88,true);
                Lettering(g,caps,"Arial",bold,pink,12,118,158,17,true);
                Lettering(g,r.Caption,"Arial",bold,white,190,119,263,15,false);
                break;
            case Face.Signal:
                if(options.FontIndex!=0 || !DotTime(g,r,sky))
                    Lettering(g,r.Time,"Consolas",bold,sky,15,20,438,74,false);
                Lettering(g,r.Caption.ToUpper(culture),"Consolas",regular,white,16,120,436,14,false);
                break;
            case Face.Cursive:
                Lettering(g,date?weekday:r.Time,"Segoe Script",regular,linen,12,8,444,78,false);
                if(date) Lettering(g,r.Caption+"   ·   "+r.Time,"Georgia",regular,white,14,111,440,17,false);
                break;
            case Face.Horizon:
                Lettering(g,r.Time,"Segoe UI Light",regular,white,109,33,250,53,false);
                using(var p=new Pen(options.TextColor(false,mint),1.5f))
                { g.DrawLine(p,18,60,95,60); g.DrawLine(p,373,60,450,60); }
                Lettering(g,r.Caption.ToUpper(culture),"Segoe UI",regular,mint,12,112,444,13,false);
                break;
            case Face.Duotone:
                Lettering(g,Spaced(caps),"Consolas",bold,linen,10,8,448,15,false);
                Lettering(g,hour,"Georgia",regular,white,12,37,208,69,false);
                Lettering(g,":","Georgia",regular,white,223,49,22,40,false);
                // The minutes use the secondary ink independently from the hours.
                DrawAccentText(g,minute,"Georgia",linen,new RectangleF(248,37,208,69));
                Lettering(g,(r.Caption+"   "+r.Marker).Trim(),"Segoe UI",regular,linen,16,126,436,13,false);
                break;
        }
    }

    private void DrawAccentText(Graphics g,string text,string font,Color fallback,RectangleF rect)
    {
        Lettering(g,text,font,FontStyle.Regular,fallback,rect.X,rect.Y,rect.Width,rect.Height,false,false,true);
    }
    private void DrawDial(Graphics g,ClockReading r,Color primary,Color secondary)
    {
        using(var marks=new Pen(options.TextColor(false,secondary),2))
        using(var hands=new Pen(options.TextColor(true,primary),4))
        {
            for(int i=0;i<12;i++) g.DrawLine(marks,DialPoint(i/12.0,i%3==0?48:53),DialPoint(i/12.0,58));
            g.DrawLine(hands,new PointF(77,78),DialPoint((r.Now.Hour+r.Now.Minute/60.0)/12,28));
            hands.Width=2.5f;
            g.DrawLine(hands,new PointF(77,78),DialPoint(r.Now.Minute/60.0,43));
        }
    }
    private static PointF DialPoint(double turns,float radius)
    {
        double a=2*Math.PI*turns;
        return new PointF(77+radius*(float)Math.Sin(a),78-radius*(float)Math.Cos(a));
    }

    // Five-column numeral patterns for the dot-matrix face.
    private static readonly int[][] Numerals={
        new[]{14,17,19,21,25,17,14},new[]{4,12,4,4,4,4,14},new[]{14,17,1,2,4,8,31},
        new[]{30,1,1,14,1,1,30},new[]{2,6,10,18,31,2,2},new[]{31,16,16,30,1,1,30},
        new[]{6,8,16,30,17,17,14},new[]{31,1,2,4,8,8,8},new[]{14,17,17,14,17,17,14},new[]{14,17,17,15,1,2,12}};
    private bool DotTime(Graphics g,ClockReading r,Color color)
    {
        string value=r.Digits;
        int columns=0;
        foreach(char c in value)
        {
            if(c>='0' && c<='9') columns+=6;
            else if(c==':' || c=='.') columns+=2;
            else return false;
        }
        if(columns==0) return false;
        float unit=Math.Min(10,369f/columns);
        float x=(Width-(columns-1)*unit-(r.Marker.Length>0?46:0))/2;
        using(var brush=new SolidBrush(options.TextColor(true,color)))
        {
            foreach(char c in value)
            {
                if(c==':' || c=='.')
                {
                    if(c==':') g.FillEllipse(brush,x,25+unit*2,unit*.74f,unit*.74f);
                    g.FillEllipse(brush,x,25+unit*5,unit*.74f,unit*.74f); x+=2*unit; continue;
                }
                int[] rows=Numerals[c-'0'];
                for(int row=0;row<7;row++) for(int col=0;col<5;col++)
                    if((rows[row] & (16>>col))!=0) g.FillEllipse(brush,x+col*unit,25+row*unit,unit*.74f,unit*.74f);
                x+=6*unit;
            }
        }
        Lettering(g,r.Marker,"Consolas",FontStyle.Bold,color,x+3,70,44,15,true);
        return true;
    }
}
