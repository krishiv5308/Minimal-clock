using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.Windows.Forms;

internal sealed class StyleEditor : Form
{
    private readonly Face face;
    private readonly Action<ThemeSettings> apply;
    private ThemeSettings draft;
    private bool syncing;
    private readonly NumericUpDown size=new NumericUpDown(),opacity=new NumericUpDown();
    private readonly ComboBox font=new OrbitChoice(),time=new OrbitChoice(),date=new OrbitChoice();
    private readonly List<InkControl> colors=new List<InkControl>();
    private readonly OrbitButton save=new OrbitButton { Text="Save changes", Primary=true, DialogResult=DialogResult.OK };
    private readonly Panel preview=new SurfacePanel();
    private readonly CheckBox light=new CheckBox { Text="Light preview",AutoSize=true };
    private readonly CheckBox clear=new CheckBox { Text="Text only",AutoSize=true };
    private readonly OrbitFonts fonts=new OrbitFonts();
    private readonly SurfacePanel head=new SurfacePanel(), footer=new SurfacePanel(),stage=new SurfacePanel();
    private readonly Panel body=new Panel();
    private readonly OrbitButton cancel=new OrbitButton { Text="Cancel",DialogResult=DialogResult.Cancel };
    private readonly OrbitButton reset=new OrbitButton { Text="Reset theme" };
    private readonly TableLayoutPanel table=new TableLayoutPanel();
    private float dpi;
    private bool ready;

    public StyleEditor(Face design,ThemeSettings initial,Action<ThemeSettings> changed)
    {
        face=design; apply=changed; draft=initial.Copy();
        Text="Orbit — Customize "+FaceArt.Names[(int)face];
        AutoScaleMode=AutoScaleMode.None;
        StartPosition=FormStartPosition.CenterParent;ShowInTaskbar=false;MinimizeBox=false;
        Font=fonts.Body;BackColor=OrbitUI.Page;ForeColor=OrbitUI.Text;
        using(var g=CreateGraphics())dpi=g.DpiX/96f;
        OrbitUI.Frame(this);
        Rectangle screen=Screen.FromPoint(Cursor.Position).WorkingArea;
        MinimumSize=new Size(Math.Min(P(440),screen.Width),Math.Min(P(395),screen.Height));
        ClientSize=new Size(Math.Min(P(900),screen.Width-P(40)),Math.Min(P(696),screen.Height-P(65)));
        head.BackColor=body.BackColor=footer.BackColor=OrbitUI.Page;
        stage.BackColor=preview.BackColor=OrbitUI.Surface;
        head.Paint+=delegate(object sender,PaintEventArgs e){
            OrbitUI.Type(e.Graphics,"Theme editor",fonts.Heading,OrbitUI.Text,new Rectangle(P(24),0,head.Width-P(48),head.Height));
            using(var pen=new Pen(OrbitUI.Line))e.Graphics.DrawLine(pen,0,head.Height-1,head.Width,head.Height-1);
        };
        stage.Paint+=delegate(object sender,PaintEventArgs e){
            OrbitUI.Type(e.Graphics,FaceArt.Names[(int)face],fonts.Brand,OrbitUI.Text,new Rectangle(P(17),P(11),stage.Width-P(30),P(35)));
        };
        preview.Paint+=PaintPreview;preview.Resize+=delegate {preview.Invalidate();};
        light.Text="Light backdrop";light.Font=fonts.Small;light.BackColor=OrbitUI.Surface;light.ForeColor=OrbitUI.Muted;
        light.Checked=face==Face.Carbon; light.CheckedChanged+=delegate { preview.Invalidate(); };
        stage.Controls.Add(preview);stage.Controls.Add(light);
        table.Dock=DockStyle.Top;table.AutoSize=true;table.ColumnCount=2;table.GrowStyle=TableLayoutPanelGrowStyle.AddRows;
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,39));table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,61));
        body.AutoScroll=true;body.Controls.Add(table);
        Section("Appearance");
        SetNumber(size,50,200,10);SetNumber(opacity,25,100,5);
        AddRange("Size (%)",size); AddRange("Opacity (%)",opacity);
        SetChoices(font,ThemeSettings.Fonts); SetChoices(time,ThemeSettings.Times); SetChoices(date,ThemeSettings.Dates);
        AddRow(table,"Typeface",font);
        Section("Color palette");
        AddInk(table,"Headline",delegate(Color c){ draft.Ink=c; });AddInk(table,"Details",delegate(Color c){ draft.Detail=c; });
        if(!FaceArt.IsTextOnly(face)){
            AddInk(table,"Background",delegate(Color c){draft.Background=c;});AddInk(table,"Accent",delegate(Color c){draft.Accent=c;});
            clear.ForeColor=OrbitUI.Text;AddRow(table,"Artwork",clear);
            clear.CheckedChanged+=delegate{if(!syncing){draft.ClearCard=clear.Checked;UpdateCardControls();Publish();}};
        }
        Section("Date & time");
        AddRow(table,"Time format",time);AddRow(table,"Date format",date);
        reset.Font=cancel.Font=save.Font=fonts.Strong;reset.BackColor=cancel.BackColor=OrbitUI.Surface;
        reset.Click+=delegate{draft=new ThemeSettings();LoadValues();Publish();};
        footer.Paint+=delegate(object sender,PaintEventArgs e){using(var pen=new Pen(OrbitUI.Line))e.Graphics.DrawLine(pen,0,0,footer.Width,0);};
        footer.Controls.Add(reset);footer.Controls.Add(cancel);footer.Controls.Add(save);
        Controls.Add(body);Controls.Add(stage);Controls.Add(head);Controls.Add(footer);
        AcceptButton=save;CancelButton=cancel;ready=true;Resize+=delegate{Arrange();};Arrange();
        size.ValueChanged+=delegate { if(!syncing){ draft.Scale=(int)size.Value; Publish(); } };
        opacity.ValueChanged+=delegate { if(!syncing){ draft.Opacity=(int)opacity.Value; Publish(); } };
        font.SelectedIndexChanged+=delegate { if(!syncing){ draft.FontIndex=font.SelectedIndex; Publish(); } };
        time.SelectedIndexChanged+=delegate { if(!syncing){ draft.TimeIndex=time.SelectedIndex; Publish(); } };
        date.SelectedIndexChanged+=delegate { if(!syncing){ draft.DateIndex=date.SelectedIndex; Publish(); } };
        LoadValues();
    }
    private int P(float value){return (int)Math.Round(value*dpi);}
    private void Arrange()
    {
        if(!ready)return;
        int w=ClientSize.Width,h=ClientSize.Height,pad=P(22),gap=P(20);
        head.SetBounds(0,0,w,P(60));footer.SetBounds(0,h-P(64),w,P(64));
        int top=head.Bottom+P(14),bottom=footer.Top-P(12);
        bool narrow=w<P(750);
        if(narrow){
            int ph=Math.Min(P(147),Math.Max(P(70),(bottom-top)/3));
            stage.SetBounds(pad,top,w-2*pad,ph);
            preview.SetBounds(stage.Width/2,P(6),stage.Width/2-P(7),ph-P(12));
            body.SetBounds(pad,stage.Bottom+P(7),w-2*pad,Math.Max(P(50),bottom-stage.Bottom-P(7)));
        }else{
            int sw=(int)((w-2*pad-gap)*.44);
            stage.SetBounds(pad,top,sw,bottom-top);body.SetBounds(stage.Right+gap,top,w-pad-gap-stage.Right,bottom-top);
            preview.SetBounds(P(12),P(52),stage.Width-P(24),Math.Max(P(60),stage.Height-P(114)));
        }
        light.Location=new Point(P(15),stage.Height-light.PreferredSize.Height-P(16));
        save.SetBounds(w-pad-P(118),P(14),P(118),P(36));
        cancel.SetBounds(save.Left-P(92),P(14),P(84),P(36));
        reset.SetBounds(pad,P(14),P(111),P(36));stage.Invalidate();
    }
    private void AddRange(string label,NumericUpDown number)
    {
        var container=new Panel { Height=P(30) };
        number.AccessibleName=label;number.Font=fonts.Small;number.TextAlign=HorizontalAlignment.Center;
        var slider=new OrbitRange(number);
        container.Controls.Add(slider);container.Controls.Add(number);
        container.Resize+=delegate{
            int fieldWidth=P(62),height=number.PreferredHeight;
            number.SetBounds(container.Width-fieldWidth,(container.Height-height)/2,fieldWidth,height);
            slider.SetBounds(0,0,Math.Max(1,number.Left-P(7)),container.Height);
        };
        AddRow(table,label,container);
    }
    private void Section(string title)
    {
        int row=table.RowCount++;table.RowStyles.Add(new RowStyle(SizeType.Absolute,P(38)));
        var label=new Label{Text=title,UseMnemonic=false,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,ForeColor=OrbitUI.Accent,Font=fonts.Strong};
        table.Controls.Add(label,0,row);table.SetColumnSpan(label,2);
    }
    private static void SetNumber(NumericUpDown input,int min,int max,int step)
    {input.Minimum=min;input.Maximum=max;input.Increment=step;input.BackColor=OrbitUI.Raised;input.ForeColor=OrbitUI.Text;input.BorderStyle=BorderStyle.FixedSingle;}
    private static void SetChoices(ComboBox input,string[] items)
    {input.DropDownStyle=ComboBoxStyle.DropDownList;input.FlatStyle=FlatStyle.Flat;input.BackColor=OrbitUI.Raised;input.ForeColor=OrbitUI.Text;input.Items.AddRange(items);}
    private void AddRow(TableLayoutPanel table,string label,Control control)
    {
        int row=table.RowCount++;table.RowStyles.Add(new RowStyle(SizeType.Absolute,P(37)));
        table.Controls.Add(new Label{Text=label,Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,ForeColor=OrbitUI.Muted,Font=fonts.Small},0,row);
        control.AccessibleName=label;control.Font=fonts.Body;control.Anchor=AnchorStyles.Left|AnchorStyles.Right;
        control.Margin=new Padding(P(3),P(2),P(5),P(2));table.Controls.Add(control,1,row);
    }
    protected override void Dispose(bool disposing){base.Dispose(disposing);if(disposing)fonts.Dispose();}    private void AddInk(TableLayoutPanel table,string title,Action<Color> update)
    {
        var ink=new InkControl(title,delegate(Color c,bool valid) {
            if(syncing) return;
            save.Enabled=colors.TrueForAll(p=>p.Valid);
            if(valid) { update(c); Publish(); }
        });
        colors.Add(ink); AddRow(table,title,ink);
    }
    private void LoadValues()
    {
        syncing=true;
        size.Value=draft.Scale; opacity.Value=draft.Opacity; font.SelectedIndex=draft.FontIndex;
        time.SelectedIndex=draft.TimeIndex; date.SelectedIndex=draft.DateIndex;
        colors[0].Value=draft.Ink; colors[1].Value=draft.Detail;
        if(colors.Count>2)
        {
            colors[2].Value=draft.Background; colors[3].Value=draft.Accent;
            clear.Checked=draft.ClearCard; UpdateCardControls();
        }
        save.Enabled=true; syncing=false;
    }
    private void Publish() { apply(draft.Copy()); preview.Invalidate(); }
    public void RefreshPreview() { preview.Invalidate(); }
    private void UpdateCardControls() { colors[2].Enabled=colors[3].Enabled=!draft.ClearCard; }
    private void PaintPreview(object sender,PaintEventArgs e)
    {
        var area=preview.ClientRectangle;
        if(area.Width<1 || area.Height<1) return;
        var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
        using(var clip=OrbitUI.Round(area,P(12)))g.SetClip(clip);
        OrbitUI.Landscape(g,area,light.Checked);
        bool clear=FaceArt.IsTextOnly(face);
        float ratio=clear ? TextFaces.Width/(float)TextFaces.Height : FaceArt.Width/(float)FaceArt.Height;
        float h=(float)(Math.Min(area.Height-12,(area.Width-16)/ratio)*Math.Sqrt(draft.Scale/200.0));
        float w=h*ratio;
        DrawPreview(e.Graphics,new RectangleF((area.Width-w)/2,(area.Height-h)/2,w,h),face,new ClockReading(DateTime.Now,draft));
    }
    public static void DrawPreview(Graphics g,RectangleF box,Face design,ClockReading reading)
    {
        if(reading.Style.Opacity==100){ FaceArt.Draw(g,box,design,reading); return; }
        var dest=Rectangle.Round(box);
        if(dest.Width<1 || dest.Height<1) return;
        using(var bitmap=new Bitmap(dest.Width,dest.Height,PixelFormat.Format32bppPArgb))
        using(var alpha=new ImageAttributes())
        {
            using(var canvas=Graphics.FromImage(bitmap))
                FaceArt.Draw(canvas,new Rectangle(Point.Empty,dest.Size),design,reading);
            var matrix=new ColorMatrix(); matrix.Matrix33=reading.Style.Opacity/100f;
            alpha.SetColorMatrix(matrix);
            g.DrawImage(bitmap,dest,0,0,dest.Width,dest.Height,GraphicsUnit.Pixel,alpha);
        }
    }
}

internal sealed class InkControl : UserControl
{
    private readonly TextBox hex=new TextBox();
    private readonly Button swatch=new Button();
    private readonly Action<Color,bool> changed;
    private Color current;
    public bool Valid { get; private set; }
    public Color Value { set { hex.Text=value.IsEmpty?"Default":"#"+(value.ToArgb()&0xffffff).ToString("X6"); } }
    public InkControl(string label,Action<Color,bool> update)
    {
        changed=update; Height=26;
        swatch.Dock=DockStyle.Left; swatch.Width=40; swatch.AccessibleName="Choose "+label;
        hex.Dock=DockStyle.Fill; hex.AccessibleName=label+" hex or Default";
        var revert=new OrbitButton {Text="Auto",Dock=DockStyle.Right,Width=47,AccessibleName="Use design default "+label};
        BackColor=OrbitUI.Raised;hex.BorderStyle=BorderStyle.FixedSingle;hex.ForeColor=OrbitUI.Text;
        swatch.FlatStyle=FlatStyle.Flat;swatch.FlatAppearance.BorderSize=0;swatch.ForeColor=OrbitUI.Text;
        Controls.Add(hex); Controls.Add(revert); Controls.Add(swatch);
        revert.Click+=delegate { Value=Color.Empty; };
        swatch.Click+=delegate {
            using(var pick=new ColorDialog { FullOpen=true,Color=current.IsEmpty?Color.White:current })
                if(pick.ShowDialog(this)==DialogResult.OK) Value=pick.Color;
        };
        hex.TextChanged+=delegate {
            string s=hex.Text.Trim().TrimStart('#'); int rgb=0;
            bool automatic=s.Length==0 || s.Equals("Default",StringComparison.OrdinalIgnoreCase);
            Valid=automatic || (s.Length==6 && int.TryParse(s,NumberStyles.AllowHexSpecifier,CultureInfo.InvariantCulture,out rgb));
            hex.BackColor=Valid?OrbitUI.Raised:Color.FromArgb(104,49,58);
            if(Valid) current=automatic?Color.Empty:ThemeSettings.Rgb(rgb);
            swatch.BackColor=current.IsEmpty?OrbitUI.Line:current;
            swatch.Text=current.IsEmpty?"…":"";
            changed(current,Valid);
        };
    }
}


