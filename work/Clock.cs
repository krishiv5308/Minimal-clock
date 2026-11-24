using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

internal sealed class Clock : Form
{
    private const string Title = "Minimal Date Clock";
    private static readonly string SettingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Title);
    private static readonly int ShowThemesMessage = RegisterWindowMessage("MinimalDateClock.ShowThemes.F26536AB");
    private readonly System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
    private readonly ContextMenuStrip menu = new ContextMenuStrip();
    private Face face;
    private ClockReading reading;
    private ThemeGallery gallery;
    private float scale;
    private readonly ThemeSettings[] styles=new ThemeSettings[FaceArt.Names.Length];

    [DllImport("user32.dll")] private static extern bool ReleaseCapture();
    [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hwnd, int msg, IntPtr w, IntPtr l);
    [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr hwnd, int msg, IntPtr w, IntPtr l);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] private static extern IntPtr FindWindow(string cls, string title);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] private static extern int RegisterWindowMessage(string name);

    [STAThread]
    private static void Main(string[] args)
    {
        bool first;
        using (var instance = new Mutex(true, @"Local\MinimalDateClock-F26536AB", out first))
        {
            if (!first)
            {
                IntPtr existing = FindWindow(null, Title);
                if (existing != IntPtr.Zero) PostMessage(existing, ShowThemesMessage, IntPtr.Zero, IntPtr.Zero);
                return;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using (var clock = new Clock())
            {
                if (Array.IndexOf(args,"--text-themes") >= 0 && !FaceArt.IsTextOnly(clock.face))
                    clock.ApplyTheme(Face.OrbitStencil);
                if (Array.IndexOf(args,"--themes") >= 0 || Array.IndexOf(args,"--text-themes") >= 0)
                    clock.Shown += delegate { clock.BeginInvoke((Action)clock.OpenGallery); };
                Application.Run(clock);
            }
        }
    }

    private Clock()
    {
        Text = Title;
        AccessibleRole = AccessibleRole.Clock;
        AccessibleDescription = "Right-click for clock themes. Drag to move.";
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.None;
        DoubleBuffered = true;
        using (var g = CreateGraphics()) scale = g.DpiX / 96f;
        if (!Enum.TryParse<Face>(ReadSetting("theme.txt").Trim(), out face) || !Enum.IsDefined(typeof(Face), face))
            face = Face.Graphite;
        for(int i=0;i<styles.Length;i++) styles[i]=ThemeSettings.Decode(ReadSetting("style-"+((Face)i)+".txt"));
        menu.Items.Add("Choose theme…", null, delegate { OpenGallery(); });
        menu.Items.Add("Customize this theme…", null, delegate { EditTheme(this); });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Close clock", null, delegate { Close(); });
        ContextMenuStrip = menu;
        timer.Tick += delegate { RefreshClock(); };
        SetDimensions();
        RefreshClock();
        Rectangle area = Screen.PrimaryScreen.WorkingArea;
        Point location = new Point(area.Right - Width - Px(24), area.Top + Px(24));
        string[] saved = ReadSetting("position.txt").Split(',');
        int x,y;
        if (saved.Length == 2 && int.TryParse(saved[0],out x) && int.TryParse(saved[1],out y)) location = new Point(x,y);
        Location = location;
        KeepOnScreen();
    }

    private static string ReadSetting(string name)
    {
        try { return File.ReadAllText(Path.Combine(SettingsDir,name)); }
        catch (IOException) { return ""; }
        catch (UnauthorizedAccessException) { return ""; }
    }
    private static void SaveSetting(string name, string value)
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            string path=Path.Combine(SettingsDir,name);
            string tmp=path+".tmp";
            File.WriteAllText(tmp,value);
            if(File.Exists(path)) File.Replace(tmp,path,null);
            else File.Move(tmp,path);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private void OpenGallery()
    {
        if (gallery == null || gallery.IsDisposed)
        {
            Rectangle area = Screen.FromControl(this).WorkingArea;
            gallery = new ThemeGallery(face, reading, ApplyTheme, area,styles,delegate { EditTheme(gallery); });
            gallery.StartPosition = FormStartPosition.Manual;
            gallery.Location = new Point(area.Left+(area.Width-gallery.Width)/2, area.Top+(area.Height-gallery.Height)/2);
            gallery.FormClosed += delegate { gallery = null; };
            gallery.Show(this);
        }
        gallery.Activate();
    }

    private void ApplyTheme(Face next)
    {
        if (next == face) return;
        face = next;
        SetDimensions();
        KeepOnScreen();
        SaveSetting("theme.txt",face.ToString());
        RefreshClock();
    }

    private void EditTheme(Form owner)
    {
        Face editing=face;
        ThemeSettings original=styles[(int)editing];
        Point position=Location;
        using(var editor=new StyleEditor(editing,original,delegate(ThemeSettings draft) { UpdateActiveStyle(editing,draft); }))
        {
            EventHandler onMinute=delegate{editor.RefreshPreview();};
            timer.Tick+=onMinute;
            try
            {
                if(editor.ShowDialog(owner)==DialogResult.OK)
                    SaveSetting("style-"+editing+".txt",styles[(int)editing].Encode());
                else { UpdateActiveStyle(editing,original); Location=position; KeepOnScreen(); }
            }
            finally { timer.Tick-=onMinute; }
        }
    }
    private void UpdateActiveStyle(Face editing,ThemeSettings settings)
    {
        styles[(int)editing]=settings;
        SetDimensions();
        KeepOnScreen();
        RefreshClock();
    }
    private bool UsesLayeredWindow { get { return FaceArt.IsTextOnly(face) || (styles!=null && styles[(int)face]!=null && (styles[(int)face].Opacity<100 || styles[(int)face].ClearCard)); } }

    protected override bool ShowWithoutActivation { get { return true; } }
    protected override CreateParams CreateParams
    {
        get { CreateParams p = base.CreateParams; p.ExStyle |= 0x80; if(UsesLayeredWindow) p.ExStyle |= 0x80000; return p; }
    }
    private int Px(float n) { return (int)Math.Round(n*scale); }

    private void SetDimensions()
    {
        bool textOnly=FaceArt.IsTextOnly(face);
        LayeredClock.SetEnabled(Handle,UsesLayeredWindow);
        float zoom=styles[(int)face].Scale/100f;
        ClientSize = textOnly ? new Size(Px(TextFaces.Width*zoom),Px(TextFaces.Height*zoom)) : new Size(Px(FaceArt.Width*zoom),Px(FaceArt.Height*zoom));
        if(textOnly)
        {
            Region old=Region;
            Region=null;
            if(old!=null) old.Dispose();
            return;
        }
        float d = Px(20*zoom);
        using (var p = new GraphicsPath())
        {
            p.AddArc(0,0,d,d,180,90); p.AddArc(Width-d,0,d,d,270,90);
            p.AddArc(Width-d,Height-d,d,d,0,90); p.AddArc(0,Height-d,d,d,90,90); p.CloseFigure();
            Region old=Region;
            Region=new Region(p);
            if(old!=null) old.Dispose();
        }
    }
    private void RefreshClock()
    {
        DateTime now = DateTime.Now;
        reading = new ClockReading(now,styles[(int)face]);
        AccessibleName = Text+", "+reading.Time+", "+reading.Date;
        RenderFace();
        if (gallery != null && !gallery.IsDisposed) gallery.UpdateReading(reading);
        timer.Stop();
        timer.Interval = Math.Max(1,60000-now.Second*1000-now.Millisecond);
        timer.Start();
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        if (reading != null && !UsesLayeredWindow) FaceArt.Draw(e.Graphics,ClientRectangle,face,reading);
    }
    private void RenderFace()
    {
        if (reading!=null && UsesLayeredWindow)
            LayeredClock.Draw(Handle,ClientSize,face,reading);
        else Invalidate();
    }
    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        RenderFace();
    }
    private void KeepOnScreen()
    {
        Rectangle area = Screen.FromRectangle(Bounds).WorkingArea;
        Location = new Point(Math.Max(area.Left, Math.Min(Left, area.Right - Width)),
            Math.Max(area.Top, Math.Min(Top, area.Bottom - Height)));
    }
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left) return;
        ReleaseCapture();
        SendMessage(Handle,0xA1,(IntPtr)2,IntPtr.Zero);
        KeepOnScreen();
        SaveSetting("position.txt", Left+","+Top);
    }
    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        if (reading == null || IsDisposed || Disposing) return;
        if (m.Msg == ShowThemesMessage) { OpenGallery(); return; }
        if (m.Msg == 0x02E0)
        {
            scale=(m.WParam.ToInt64() & 0xffff)/96f;
            SetDimensions(); KeepOnScreen(); RenderFace();
        }
        if (m.Msg == 0x007E) KeepOnScreen();
        if (m.Msg == 0x001A) CultureInfo.CurrentCulture.ClearCachedData();
        if (m.Msg == 0x001E || m.Msg == 0x001A ||
            (m.Msg == 0x0218 && (m.WParam.ToInt32()==18 || m.WParam.ToInt32()==7))) RefreshClock();
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { timer.Dispose(); menu.Dispose(); }
        base.Dispose(disposing);
    }
}

