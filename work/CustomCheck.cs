using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Windows.Forms;
using System.Globalization;
using System.Runtime.InteropServices;
class CustomCheck
{
    const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Assert(bool ok,string label){if(!ok)throw new Exception(label);}
    static object Field(object o,string name){return o.GetType().GetField(name,Hidden).GetValue(o);}
    static void Call(object o,string name,params object[] args){o.GetType().GetMethod(name,Hidden).Invoke(o,args);}
    static void Capture(Form form,string name)
    {
        using(var b=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(b,new Rectangle(Point.Empty,b.Size));b.Save("work/"+name+".png");}
    }
    [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr hwnd,int index);
    [STAThread] static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        var now=new DateTime(2026,11,24,21,8,0);
        var primary=Color.FromArgb(8,146,229);var detail=Color.FromArgb(235,120,33);
        string folder=Path.GetFullPath("work/custom-check-"+Guid.NewGuid().ToString("N"));
        typeof(Clock).GetField("SettingsDir",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,folder);
        Assert(ThemeSettings.Decode("bad").Scale==100,"invalid input");
        using(var sheet=new Bitmap(940,810))
        using(var sheetArt=Graphics.FromImage(sheet))
        using(var labelFont=new Font("Segoe UI",12,GraphicsUnit.Point))
        {
            sheetArt.Clear(Color.FromArgb(50,47,58));
            foreach(Face face in Enum.GetValues(typeof(Face)))
            {
                bool transparent=FaceArt.IsTextOnly(face);
                int width=transparent?468:272, height=transparent?156:132;
                // A baseline and a range of overlays exercise all the GDI+ paths.
                for(int k=0;k<ThemeSettings.Fonts.Length;k++)
                {
                    var s=new ThemeSettings { Ink=primary, Detail=detail, Background=Color.Bisque,Accent=Color.LimeGreen,FontIndex=k,TimeIndex=k%3,DateIndex=k%5 };
                    var r=new ClockReading(now,s);
                    using(var b=new Bitmap(width,height,PixelFormat.Format32bppPArgb))
                    using(var g=Graphics.FromImage(b))
                    {
                        FaceArt.Draw(g,new Rectangle(0,0,width,height),face,r);
                        if(k==0) {
                            int count=0; for(int y=0;y<height;y++) for(int x=0;x<width;x++) if(b.GetPixel(x,y).ToArgb()==primary.ToArgb()) count++;
                            Assert(count>0,"Headline color missing "+face);
                        }
                        if(k==1 && !transparent) {
                            int match=0; for(int y=0;y<height;y++) for(int x=0;x<width;x++) if(b.GetPixel(x,y).ToArgb()==Color.Bisque.ToArgb()) match++; Assert(match>4,"Background color "+face+" corner "+b.GetPixel(0,0)+" match "+match);
                            s.ClearCard=true; g.Clear(Color.Transparent); FaceArt.Draw(g,new Rectangle(0,0,width,height),face,r);
                            Assert(b.GetPixel(0,0).A==0,"Clear card "+face);
                        }
                    }
                }
                if(face>=Face.BareMinute)
                {
                    int n=(int)face-(int)Face.BareMinute; int x=12+n%2*468,y=10+n/2*132;
                    FaceArt.Draw(sheetArt,new Rectangle(x,y,437,103),face,new ClockReading(now));
                    sheetArt.DrawString(FaceArt.Names[(int)face],labelFont,Brushes.LightGray,x+6,y+102);
                }
            }
            sheet.Save("work/new-theme-sheet.png",ImageFormat.Png);
        }
        using(var clock=(Form)Activator.CreateInstance(typeof(Clock),true))
        {
            clock.Show(); Application.DoEvents();
            var styles=(ThemeSettings[])Field(clock,"styles");
            Assert(styles.Length==30,"theme count");
            foreach(Face f in Enum.GetValues(typeof(Face)))
            {
                Call(clock,"ApplyTheme",f);
                var s=new ThemeSettings {Scale=150,Opacity=55,Ink=primary,Detail=detail,TimeIndex=2,DateIndex=2};
                Call(clock,"UpdateActiveStyle",f,s);
                var r=(ClockReading)Field(clock,"reading");
                Assert(r.Date==r.Now.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture),"date");
                Assert((GetWindowLong(clock.Handle,-20)&0x80000)!=0,"opacity "+f);
                float dpi=(float)Field(clock,"scale");
                int width=(int)Math.Round((FaceArt.IsTextOnly(f)?468:272)*dpi*1.5f);
                Assert(clock.Width==width,"size "+f);
                Call(clock,"UpdateActiveStyle",f,new ThemeSettings());
            }
            Call(clock,"ApplyTheme",Face.BareMinute);
            Call(clock,"OpenGallery"); Application.DoEvents();
            var gallery=(Form)Field(clock,"gallery");
            var grid=gallery.Controls.OfType<FlowLayoutPanel>().Single(p=>p.Controls.OfType<ThemeTile>().Any());
            Assert(grid.Controls.OfType<ThemeTile>().Count(t=>t.Visible)==30,"library count");
            Capture(gallery,"orbit-library");
            ((Button)Field(gallery,"transparent")).PerformClick();
            Assert(grid.Controls.OfType<ThemeTile>().Count(t=>t.Visible)==18,"transparent count");
            ((Button)Field(gallery,"cards")).PerformClick();
            Assert(grid.Controls.OfType<ThemeTile>().Count(t=>t.Visible)==12,"card count");
            var search=(TextBox)Field(gallery,"search");search.Text="LCD";
            Assert(grid.Controls.OfType<ThemeTile>().Count(t=>t.Visible)==1,"search result");
            search.Text="no matching design";Assert(((Label)Field(gallery,"empty")).Visible,"empty result");
            search.Clear();((Button)Field(gallery,"all")).PerformClick();
            var normal=gallery.ClientSize;gallery.ClientSize=new Size(505,490);Application.DoEvents();
            Capture(gallery,"orbit-compact");gallery.ClientSize=normal;
            Action<Face> saveEditor=delegate(Face f) {
                clock.BeginInvoke((Action)delegate {
                    var editor=Application.OpenForms.OfType<StyleEditor>().Single();
                    ((NumericUpDown)Field(editor,"size")).Value=125;
                    ((NumericUpDown)Field(editor,"opacity")).Value=65;
                    ((ComboBox)Field(editor,"font")).SelectedIndex=4;
                    ((ComboBox)Field(editor,"time")).SelectedIndex=1;
                    ((ComboBox)Field(editor,"date")).SelectedIndex=2;
                    var inks=(System.Collections.Generic.List<InkControl>)Field(editor,"colors");
                    inks[0].Value=primary; inks[1].Value=detail;
                    if(f==Face.Graphite){ inks[2].Value=Color.Beige; inks[3].Value=Color.Orange; }
                    Capture(editor,f==Face.Graphite?"custom-card-editor":"custom-transparent-editor");
                    ((Button)Field(editor,"save")).PerformClick();
                });
            };
            saveEditor(Face.BareMinute);
            ((Button)Field(gallery,"customize")).PerformClick();
            var persisted=File.ReadAllText(Path.Combine(folder,"style-BareMinute.txt"));
            Assert(ThemeSettings.Decode(persisted).Ink.ToArgb()==primary.ToArgb(),"persist color");
            gallery.Close();
            Call(clock,"ApplyTheme",Face.Graphite);
            saveEditor(Face.Graphite);
            ((ToolStripMenuItem)clock.ContextMenuStrip.Items[1]).PerformClick();
            Assert(ThemeSettings.Decode(File.ReadAllText(Path.Combine(folder,"style-Graphite.txt"))).Accent.ToArgb()==Color.Orange.ToArgb(),"card accent persisted");
            clock.BeginInvoke((Action)delegate {
                var editor=Application.OpenForms.OfType<StyleEditor>().Single();
                ((NumericUpDown)Field(editor,"size")).Value=195;
                editor.DialogResult=DialogResult.Cancel;
            });
            ((ToolStripMenuItem)clock.ContextMenuStrip.Items[1]).PerformClick();
            Assert(styles[(int)Face.Graphite].Scale==125,"cancel");
            Assert(styles[(int)Face.Stack].Scale==100,"per-theme isolation");
            clock.Close();
        }
        using(var clock=(Form)Activator.CreateInstance(typeof(Clock),true)) {
            var styles=(ThemeSettings[])Field(clock,"styles");
            Assert(styles[(int)Face.BareMinute].Opacity==65 && styles[(int)Face.Graphite].Scale==125,"restart");
        }
        Console.WriteLine("30 themes across 9 font modes, rendered colors, opacity, size, card transparency, editor Save/Cancel, isolation and restart passed.");
    }
}



