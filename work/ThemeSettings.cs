using System;
using System.Drawing;
using System.Globalization;

internal sealed class ThemeSettings
{
    public static readonly string[] Fonts = { "Design default", "Segoe UI", "Segoe UI Light", "Georgia", "Consolas", "Arial", "Arial Black", "Trebuchet MS", "Times New Roman", "Segoe Script" };
    public static readonly string[] Times = { "Windows setting", "12-hour", "24-hour" };
    public static readonly string[] Dates = { "Design default", "Short date", "Year-month-day", "Long date", "Hide date" };
    public int Scale = 100, Opacity = 100, FontIndex, TimeIndex, DateIndex;
    public Color Ink = Color.Empty, Detail = Color.Empty, Background = Color.Empty, Accent=Color.Empty;
    public bool ClearCard;

    public ThemeSettings Copy() { return (ThemeSettings)MemberwiseClone(); }
    public string FontName(string original) { return FontIndex == 0 ? original : Fonts[FontIndex]; }
    public Color TextColor(bool headline, Color original)
    {
        Color chosen = headline ? Ink : Detail;
        return chosen.IsEmpty ? original : chosen;
    }
    public static Color Rgb(int rgb) { return Color.FromArgb(255,(rgb>>16)&255,(rgb>>8)&255,rgb&255); }
    private static string Hex(Color c) { return c.IsEmpty ? "-" : (c.ToArgb() & 0xffffff).ToString("X6"); }
    private static Color ParseColor(string value)
    {
        int rgb;
        return value.Length == 6 && int.TryParse(value,NumberStyles.AllowHexSpecifier,CultureInfo.InvariantCulture,out rgb) ? Rgb(rgb) : Color.Empty;
    }
    private static int Number(string value,int min,int max,int fallback)
    {
        int n;
        return int.TryParse(value,NumberStyles.Integer,CultureInfo.InvariantCulture,out n) && n>=min && n<=max ? n : fallback;
    }
    public string Encode()
    {
        return string.Join("|",new string[]{ "1", Scale.ToString(CultureInfo.InvariantCulture), Opacity.ToString(CultureInfo.InvariantCulture),
            FontIndex.ToString(CultureInfo.InvariantCulture),TimeIndex.ToString(CultureInfo.InvariantCulture),DateIndex.ToString(CultureInfo.InvariantCulture),
            Hex(Ink),Hex(Detail),Hex(Background),Hex(Accent),ClearCard?"1":"0" });
    }
    public static ThemeSettings Decode(string value)
    {
        var s=new ThemeSettings();
        string[] p=value.Trim().Split('|');
        if((p.Length!=9 && p.Length!=11) || p[0]!="1") return s;
        s.Scale=Number(p[1],50,200,100); s.Opacity=Number(p[2],25,100,100);
        s.FontIndex=Number(p[3],0,Fonts.Length-1,0); s.TimeIndex=Number(p[4],0,Times.Length-1,0); s.DateIndex=Number(p[5],0,Dates.Length-1,0);
        s.Ink=ParseColor(p[6]); s.Detail=ParseColor(p[7]); s.Background=ParseColor(p[8]);
        if(p.Length==11) { s.Accent=ParseColor(p[9]); s.ClearCard=p[10]=="1"; }
        return s;
    }
}
