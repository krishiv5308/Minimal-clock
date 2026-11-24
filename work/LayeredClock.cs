using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

internal static class LayeredClock
{
    private const int LayeredStyle = 0x80000;
    [StructLayout(LayoutKind.Sequential)]
    private struct DibHeader
    {
        public uint Size;
        public int Width, Height;
        public ushort Planes, Bits;
        public uint Compression, ImageSize;
        public int XPixels, YPixels;
        public uint Colors, ImportantColors;
    }
    [StructLayout(LayoutKind.Sequential, Pack=1)]
    private struct Blend { public byte Operation, Flags, Opacity, AlphaFormat; }
    [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr h, int index);
    [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr h, int index, int style);
    [DllImport("gdi32.dll",SetLastError=true)] private static extern IntPtr CreateCompatibleDC(IntPtr dc);
    [DllImport("gdi32.dll",SetLastError=true)] private static extern IntPtr CreateDIBSection(IntPtr dc, ref DibHeader info,
        uint usage, out IntPtr bits, IntPtr section, uint offset);
    [DllImport("gdi32.dll")] private static extern IntPtr SelectObject(IntPtr dc, IntPtr obj);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr obj);
    [DllImport("gdi32.dll")] private static extern bool DeleteDC(IntPtr dc);
    [DllImport("user32.dll",SetLastError=true)] private static extern bool UpdateLayeredWindow(IntPtr window, IntPtr dst,
        IntPtr destPoint, ref Size size, IntPtr src, ref Point origin, uint colorKey, ref Blend blend, uint flags);

    public static void SetEnabled(IntPtr hwnd, bool enabled)
    {
        int style=GetWindowLong(hwnd,-20);
        int next=enabled ? style|LayeredStyle : style & ~LayeredStyle;
        if(next!=style) SetWindowLong(hwnd,-20,next);
    }

    public static void Draw(IntPtr hwnd, Size size, Face face, ClockReading reading)
    {
        var header=new DibHeader { Size=(uint)Marshal.SizeOf(typeof(DibHeader)), Width=size.Width,
            Height=-size.Height, Planes=1, Bits=32 };
        IntPtr dc=CreateCompatibleDC(IntPtr.Zero);
        if(dc==IntPtr.Zero) throw new Win32Exception();
        IntPtr dib=IntPtr.Zero, previous=IntPtr.Zero;
        try
        {
            IntPtr bits;
            dib=CreateDIBSection(dc,ref header,0,out bits,IntPtr.Zero,0);
            if(dib==IntPtr.Zero) throw new Win32Exception();
            using (var pixels=new Bitmap(size.Width,size.Height,size.Width*4,PixelFormat.Format32bppPArgb,bits))
            using (var g=Graphics.FromImage(pixels))
            {
                g.Clear(Color.Transparent);
                FaceArt.Draw(g,new Rectangle(Point.Empty,size),face,reading);
            }
            previous=SelectObject(dc,dib);
            var blend=new Blend { Opacity=(byte)Math.Round(reading.Style.Opacity*2.55), AlphaFormat=1 };
            var origin=Point.Empty;
            if(!UpdateLayeredWindow(hwnd,IntPtr.Zero,IntPtr.Zero,ref size,dc,ref origin,0,ref blend,2))
                throw new Win32Exception();
        }
        finally
        {
            if(previous!=IntPtr.Zero) SelectObject(dc,previous);
            if(dib!=IntPtr.Zero) DeleteObject(dib);
            DeleteDC(dc);
        }
    }
}
