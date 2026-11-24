using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

internal sealed class ThemeGallery : Form
{
    private readonly FlowLayoutPanel grid=new FlowLayoutPanel();
    private readonly ThemeTile[] tiles=new ThemeTile[FaceArt.Names.Length];
    private readonly ThemeSettings[] styles;
    private readonly OrbitFonts fonts=new OrbitFonts();
    private readonly SurfacePanel mast=new SurfacePanel(),hero=new SurfacePanel(),bar=new SurfacePanel(),searchBox=new SurfacePanel();
    private readonly TextBox search=new TextBox();
    private readonly Label searchHint=new Label();
    private readonly OrbitButton customize=new OrbitButton(),all=new OrbitButton(),transparent=new OrbitButton(),cards=new OrbitButton();
    private readonly Label empty=new Label();
    private readonly Action<Face> choose;
    private readonly float scale;
    private Face selected;
    private DateTime now;
    private int category,visibleCount;
    private bool ready,layingOut;

    public ThemeGallery(Face current,ClockReading reading,Action<Face> onChoose,Rectangle workingArea,ThemeSettings[] saved,Action edit)
    {
        styles=saved;selected=current;now=reading.Now;choose=onChoose;
        Text="Orbit — Theme library";BackColor=OrbitUI.Page;ForeColor=OrbitUI.Text;Font=fonts.Body;
        AutoScaleMode=AutoScaleMode.None;StartPosition=FormStartPosition.CenterScreen;DoubleBuffered=true;
        using(var g=CreateGraphics())scale=g.DpiX/96f;
        OrbitUI.Frame(this);
        mast.BackColor=bar.BackColor=hero.BackColor=grid.BackColor=OrbitUI.Page;
        mast.Paint+=PaintMast;bar.Paint+=PaintBar;hero.Paint+=PaintHero;
        searchBox.BackColor=OrbitUI.Page;searchBox.Paint+=PaintSearch;
        search.BorderStyle=BorderStyle.None;search.Font=fonts.Body;search.BackColor=OrbitUI.Surface;search.ForeColor=OrbitUI.Text;
        search.AccessibleName="Search themes";search.TabIndex=0;
        searchBox.Controls.Add(search);mast.Controls.Add(searchBox);
        searchHint.Text="Search themes";searchHint.ForeColor=OrbitUI.Muted;searchHint.BackColor=OrbitUI.Surface;
        searchHint.Font=fonts.Small;searchHint.TextAlign=ContentAlignment.MiddleLeft;searchHint.Click+=delegate{search.Focus();};
        searchBox.Controls.Add(searchHint);searchHint.BringToFront();
        search.TextChanged+=delegate { searchHint.Visible=!search.Focused && search.TextLength==0; Filter(); };
        search.Enter+=delegate {searchHint.Visible=false; searchBox.Invalidate(); };search.Leave+=delegate {searchHint.Visible=search.TextLength==0; searchBox.Invalidate(); };
        searchBox.Click+=delegate { search.Focus(); };
        customize.Text="Customize";customize.Font=fonts.Strong;customize.Primary=true;
        customize.AccessibleName="Customize active clock";customize.Click+=delegate { edit(); };hero.Controls.Add(customize);
        SetupFilter(all,"All",0);SetupFilter(transparent,"Transparent",1);SetupFilter(cards,"Cards",2);
        grid.WrapContents=true;grid.AutoScroll=true;grid.TabStop=false;grid.Margin=Padding.Empty;
        Face[] sequence={Face.BareMinute,Face.PocketLCD,Face.SilkItalic,Face.OrbitStencil,Face.SundayPress,Face.Meridian,
            Face.StickerClub,Face.Outline,Face.SideA,Face.Cursive,Face.NightDrive,Face.Almanac,Face.Graphite,Face.Echo,
            Face.FlowerPower,Face.Offset,Face.SplitFlip,Face.Carbon,Face.MossDial,Face.Signal,Face.Gilded,Face.Stack,
            Face.Blueprint,Face.Horizon,Face.DayPass,Face.Byline,Face.Hairline,Face.Punch,Face.FieldNotes,Face.Duotone};
        foreach(Face design in sequence){
            var tile=new ThemeTile(design,new ClockReading(now,styles[(int)design]),design==current,fonts,scale);
            tiles[(int)design]=tile;tile.Click+=SelectTile;grid.Controls.Add(tile);
        }
        empty.Text="No matching themes";empty.ForeColor=OrbitUI.Muted;empty.Font=fonts.Body;
        empty.BackColor=OrbitUI.Page;empty.TextAlign=ContentAlignment.TopCenter;
        Controls.Add(mast);Controls.Add(hero);Controls.Add(bar);Controls.Add(grid);Controls.Add(empty);
        MinimumSize=new Size(Math.Min(Px(485),workingArea.Width),Math.Min(Px(420),workingArea.Height));
        ClientSize=new Size(Math.Min(Px(1030),workingArea.Width-Px(35)),Math.Min(Px(780),workingArea.Height-Px(65)));
        ready=true;Resize+=delegate{Arrange();};Arrange();Filter();
        KeyPreview=true;
        KeyDown+=delegate(object sender,KeyEventArgs e) {
            if(e.Control&&e.KeyCode==Keys.F){search.Focus();search.SelectAll();e.SuppressKeyPress=true;}
            if(e.KeyCode==Keys.Escape){if(search.TextLength>0)search.Clear();else Close();e.SuppressKeyPress=true;}
        };
    }
    private int Px(float n){return (int)Math.Round(n*scale);}
    private void SetupFilter(OrbitButton b,string text,int which)
    {
        b.Text=text;b.Font=fonts.Small;b.AccessibleName="Show "+text.ToLowerInvariant()+" themes";
        b.Click+=delegate { category=which;Filter(); };bar.Controls.Add(b);
    }
    private void Filter()
    {
        if(!ready)return;
        grid.SuspendLayout();grid.AutoScrollPosition=Point.Empty;visibleCount=0;
        string query=search.Text.Trim();
        foreach(ThemeTile tile in tiles){
            bool show=(category==0 || (category==1)==FaceArt.IsTextOnly(tile.Design)) &&
                tile.Text.IndexOf(query,StringComparison.CurrentCultureIgnoreCase)>=0;
            tile.Visible=show;if(show)visibleCount++;
        }
        all.Selected=category==0;transparent.Selected=category==1;cards.Selected=category==2;
        all.Invalidate();transparent.Invalidate();cards.Invalidate();
        grid.ResumeLayout();empty.Visible=visibleCount==0;grid.Visible=visibleCount>0;bar.Invalidate();Arrange();
    }
    private void Arrange()
    {
        if(!ready||layingOut)return;layingOut=true;
        try{
            int w=ClientSize.Width,h=ClientSize.Height;
            int pad=Px(w<Px(640)?16:27),inner=w-2*pad,top=Px(h<Px(580)?55:68);
            mast.SetBounds(pad,0,inner,top);
            int searchW=Math.Min(Px(243),inner/2);searchBox.SetBounds(inner-searchW,(top-Px(36))/2,searchW,Px(36));
            search.SetBounds(Px(34),(searchBox.Height-search.PreferredHeight)/2,searchW-Px(46),search.PreferredHeight);
            searchHint.Bounds=search.Bounds;
            int heroH=Px(h<Px(590)?125:h<Px(690)?160:203);
            hero.SetBounds(pad,top,inner,heroH);
            customize.SetBounds(Px(25),heroH-Px(heroH<Px(145)?39:52),Px(116),Px(32));
            int barH=Px(inner<Px(475)?85:66);
            bar.SetBounds(pad,hero.Bottom,inner,barH);
            int gap=Px(5),total=Px(53+101+65)+gap*2;
            int fx=inner<Px(475)?0:inner-total,fy=inner<Px(475)?Px(44):(barH-Px(32))/2;
            all.SetBounds(fx,fy,Px(53),Px(32));transparent.SetBounds(all.Right+gap,fy,Px(101),Px(32));cards.SetBounds(transparent.Right+gap,fy,Px(65),Px(32));
            grid.SetBounds(pad,bar.Bottom,inner,Math.Max(1,h-bar.Bottom-Px(9)));
            empty.SetBounds(pad,bar.Bottom+Px(15),inner,Px(60));
            int usable=inner-SystemInformation.VerticalScrollBarWidth-Px(2);
            int columns=Math.Max(1,Math.Min(3,usable/Px(237))),spacing=Px(13);
            int cardW=Math.Max(Px(120),(usable-(columns-1)*spacing)/columns);
            int cardH=(int)Math.Round(cardW*.48)+Px(55);
            int i=0;grid.SuspendLayout();
            foreach(Control c in grid.Controls){
                ThemeTile t=(ThemeTile)c;t.Size=new Size(cardW,cardH);
                // Order is fixed, while filtering changes which column a tile occupies.
                bool shown=(category==0 || (category==1)==FaceArt.IsTextOnly(t.Design)) && t.Text.IndexOf(search.Text.Trim(),StringComparison.CurrentCultureIgnoreCase)>=0;
                t.Margin=new Padding(0,0,shown && (i+1)%columns!=0?spacing:0,spacing);if(shown)i++;
            }
            grid.ResumeLayout();hero.Invalidate();bar.Invalidate();
        }finally{layingOut=false;}
    }
    private void PaintMast(object sender,PaintEventArgs e)
    {
        int n=Px(33),y=(mast.Height-n)/2;
        if(Icon!=null)e.Graphics.DrawIcon(Icon,new Rectangle(0,y,n,n));
        OrbitUI.Type(e.Graphics,"Orbit",fonts.Brand,OrbitUI.Text,new Rectangle(Px(42),0,Px(74),mast.Height));
        if(mast.Width>Px(500)){
            using(var p=new Pen(OrbitUI.Line))e.Graphics.DrawLine(p,Px(111),y+Px(6),Px(111),y+n-Px(6));
            OrbitUI.Type(e.Graphics,"Desktop clock",fonts.Small,OrbitUI.Muted,new Rectangle(Px(126),0,Px(120),mast.Height));
        }
    }
    private void PaintSearch(object sender,PaintEventArgs e)
    {
        var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
        OrbitUI.Box(g,new RectangleF(1,1,searchBox.Width-2,searchBox.Height-2),Px(9),OrbitUI.Surface,search.Focused?OrbitUI.Accent:OrbitUI.Line,scale);
        float cy=searchBox.Height/2f;
        using(var p=new Pen(OrbitUI.Muted,1.5f*scale)){g.DrawEllipse(p,Px(12),cy-Px(6),Px(10),Px(10));g.DrawLine(p,Px(20),cy+Px(2),Px(25),cy+Px(7));}
    }
    private void PaintHero(object sender,PaintEventArgs e)
    {
        Graphics g=e.Graphics;int w=hero.Width,h=hero.Height;if(w<2||h<2)return;
        var outer=new RectangleF(1,1,w-2,h-2);g.SmoothingMode=SmoothingMode.AntiAlias;
        var state=g.Save();
        using(var clip=OrbitUI.Round(outer,Px(15))){
            g.SetClip(clip);OrbitUI.Landscape(g,hero.ClientRectangle,selected==Face.Carbon);
            using(var shade=new LinearGradientBrush(hero.ClientRectangle,Color.FromArgb(246,29,25,45),Color.FromArgb(0,29,25,45),0f)){
                shade.InterpolationColors=new ColorBlend{Colors=new[]{Color.FromArgb(245,29,25,45),Color.FromArgb(224,31,27,49),Color.FromArgb(28,32,26,54),Color.FromArgb(0,32,26,54)},Positions=new[]{0f,.29f,.62f,1f}};
                g.FillRectangle(shade,hero.ClientRectangle);
            }
            float split=w*(w<Px(580)?.47f:.39f);
            var space=new RectangleF(split,Px(13),w-split-Px(22),h-2*Px(13));
            var rect=OrbitUI.Fit(space,selected);
            if(!FaceArt.IsTextOnly(selected))rect.Inflate(-rect.Width*.07f,-rect.Height*.07f);
            StyleEditor.DrawPreview(g,rect,selected,new ClockReading(now,styles[(int)selected]));
            int top=Px(h<Px(145)?12:24);
            using(var b=new SolidBrush(OrbitUI.Rgb(0x9de0ba)))g.FillEllipse(b,Px(26),top+Px(7),Px(7),Px(7));
            OrbitUI.Type(g,"On your desktop",fonts.Small,OrbitUI.Rgb(0xe3dcea),new Rectangle(Px(41),top,Px(160),Px(22)));
            OrbitUI.Type(g,FaceArt.Names[(int)selected],h<Px(145)?fonts.Heading:fonts.Display,Color.White,new Rectangle(Px(24),top+Px(21),(int)split-Px(26),Px(h<Px(145)?40:64)));
        }
        g.Restore(state);OrbitUI.Box(g,outer,Px(15),Color.Transparent,OrbitUI.Line,scale);
    }
    private void PaintBar(object sender,PaintEventArgs e)
    {
        int h=bar.Width<Px(475)?Px(43):bar.Height;
        string title=search.TextLength==0?"Theme library":"Search results";
        var g=e.Graphics;int titleW=TextRenderer.MeasureText(g,title,fonts.Heading,Size.Empty,TextFormatFlags.NoPadding).Width;
        OrbitUI.Type(g,title,fonts.Heading,OrbitUI.Text,new Rectangle(0,0,titleW+Px(4),h));
        OrbitUI.Type(g,visibleCount.ToString(),fonts.Small,OrbitUI.Muted,new Rectangle(titleW+Px(12),Px(2),Px(42),h));
    }
    private void SelectTile(object sender,EventArgs args){
        var t=(ThemeTile)sender;choose(t.Design);selected=t.Design;
        foreach(var item in tiles)item.SetSelection(item.Design==selected);
        hero.Invalidate();
    }
    public void UpdateReading(ClockReading reading){
        now=reading.Now;foreach(var t in tiles){t.Reading=new ClockReading(now,styles[(int)t.Design]);t.Invalidate();}
        hero.Invalidate();
    }
    protected override void Dispose(bool disposing){base.Dispose(disposing);if(disposing)fonts.Dispose();}
}

internal sealed class ThemeTile : Button
{
    public readonly Face Design;
    public ClockReading Reading;
    private readonly OrbitFonts fonts;
    private readonly float scale;
    private bool selected,hover;
    public ThemeTile(Face face,ClockReading reading,bool active,OrbitFonts labelFonts,float dpi){
        Design=face;Reading=reading;fonts=labelFonts;scale=dpi;Text=FaceArt.Names[(int)face];
        BackColor=OrbitUI.Page;FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;Cursor=Cursors.Hand;DoubleBuffered=true;SetSelection(active);
    }
    private int P(float n){return (int)Math.Round(n*scale);}
    public void SetSelection(bool value){selected=value;AccessibleName=Text+(value?", active theme":"");Invalidate();}
    protected override void OnMouseEnter(EventArgs e){base.OnMouseEnter(e);hover=true;Invalidate();}
    protected override void OnMouseLeave(EventArgs e){base.OnMouseLeave(e);hover=false;Invalidate();}
    protected override void OnPaint(PaintEventArgs e){
        Graphics g=e.Graphics;g.Clear(OrbitUI.Page);g.SmoothingMode=SmoothingMode.AntiAlias;
        var box=new RectangleF(1,1,Width-2,Height-2);Color surface=hover?OrbitUI.Raised:OrbitUI.Surface;
        OrbitUI.Box(g,box,P(12),surface,Color.Empty,0);
        int pad=P(6);var art=new Rectangle(pad,pad,Width-2*pad,Height-P(55)-pad);
        if(art.Width<5||art.Height<5)return;
        var state=g.Save();using(var path=OrbitUI.Round(art,P(8))){
            g.SetClip(path);Color baseColor=OrbitUI.TileColor(Design);
            using(var bg=new LinearGradientBrush(art,OrbitUI.Mix(baseColor,Color.White,.11f),OrbitUI.Mix(baseColor,Color.Black,.28f),26f))g.FillRectangle(bg,art);
            if(!FaceArt.IsTextOnly(Design) && !Reading.Style.ClearCard && Reading.Style.Opacity==100){
                // Extend the artwork to the inset edge without distorting its aspect ratio.
                var fit=OrbitUI.Fit(art,Design);float cover=Math.Max(art.Width/fit.Width,art.Height/fit.Height);
                fit=new RectangleF(art.X+(art.Width-fit.Width*cover)/2,art.Y+(art.Height-fit.Height*cover)/2,fit.Width*cover,fit.Height*cover);
                StyleEditor.DrawPreview(g,fit,Design,Reading);
            } else {
                var inset=Rectangle.Inflate(art,-P(12),-P(8));
                StyleEditor.DrawPreview(g,OrbitUI.Fit(inset,Design),Design,Reading);
            }
        }g.Restore(state);
        int y=art.Bottom+P(4);
        OrbitUI.Type(g,Text,fonts.Strong,OrbitUI.Text,new Rectangle(P(13),y,Width-P(45),P(24)));
        OrbitUI.Type(g,FaceArt.IsTextOnly(Design)?"Transparent":"Card",fonts.Small,OrbitUI.Muted,new Rectangle(P(13),y+P(22),Width-P(34),P(20)));
        if(selected){
            int x=Width-P(25),cy=y+P(20);
            using(var b=new SolidBrush(OrbitUI.Accent))g.FillEllipse(b,x-P(8),cy-P(8),P(16),P(16));
            using(var p=new Pen(OrbitUI.Deep,1.6f*scale))g.DrawLines(p,new[]{new Point(x-P(4),cy),new Point(x-P(1),cy+P(3)),new Point(x+P(4),cy-P(3))});
        }
        OrbitUI.Box(g,box,P(12),Color.Empty,selected||Focused&&ShowFocusCues?OrbitUI.Accent:OrbitUI.Line,selected?2*scale:scale);
    }
}

