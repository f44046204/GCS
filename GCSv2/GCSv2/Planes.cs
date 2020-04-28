using System;
using System.Data;
using GMap.NET;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using System.Drawing;

namespace GCSv2
{
    class Planes
    {
        /*飛機資料*/
        public DataTable dt = new DataTable();

        public static int TA = 30;
        public static int RA = 20;

        /* 不變常數宣告 */
        private static readonly double d2r = 0.01745329251994329576923690768489;

        public static void AddPosition(double lat, double lng, string name, double heading, double speed, GMapOverlay overlay)
        {
            //飛機的圖層
            Image srcPlane = Image.FromFile("drone2.png");
            Bitmap picPlane = GraphicRotateAtAny((Bitmap)srcPlane, srcPlane.Height / 2, srcPlane.Width / 2, heading);
            picPlane.MakeTransparent(Color.Yellow);

            GMapMarker plane = new GMarkerGoogle(new PointLatLng(lat, lng), picPlane);
            plane.Offset = new Point(-picPlane.Width / 2, -picPlane.Height / 2);
            GMapToolTip tooltip = new GMapToolTip(plane);

            Font f = new Font("Arial", 7, FontStyle.Bold);
            MarkerTooltipMode mode = MarkerTooltipMode.Always;

            tooltip.Fill = new SolidBrush(Color.White);
            tooltip.Foreground = new SolidBrush(Color.DarkBlue);
            tooltip.Offset = new Point(28, -10);
            tooltip.TextPadding = new Size(10, 10);

            plane.Tag = name;
            plane.ToolTipText = "ID: " + name + "\n";
            //plane.ToolTipText += "Speed: " + speed;
            //plane.ToolTipText += $"Latitude: {plane.Position.Lat}, \nLongitude: {plane.Position.Lng}";
            plane.ToolTip = tooltip;
            plane.ToolTipMode = mode;
            plane.ToolTip.Font = f;

            overlay.Markers.Add(plane);
        }

        private static Bitmap GraphicRotateAtAny(Bitmap SrcBmp, int m, int n, double angle)
        {
            double radians = -d2r * angle;

            float cosine = (float)Math.Cos(radians);
            float sine = (float)Math.Sin(radians);

            Bitmap DestBmp = new Bitmap(SrcBmp.Width, SrcBmp.Height);
            Graphics tmpBox = Graphics.FromImage(DestBmp);
            for (int x = 0; x < DestBmp.Width; x++)
            {
                for (int y = 0; y < DestBmp.Height; y++)
                {
                    int SrcBitmapx = (int)Math.Ceiling((x - m) * cosine - (y - n) * sine + m);
                    int SrcBitmapy = (int)Math.Ceiling((y - n) * cosine + (x - m) * sine + n);

                    if (SrcBitmapx >= 0 && SrcBitmapx < SrcBmp.Width && SrcBitmapy >= 0 && SrcBitmapy < SrcBmp.Height)
                    {
                        DestBmp.SetPixel(x, y, SrcBmp.GetPixel(SrcBitmapx, SrcBitmapy));
                    }
                    else
                    {
                        DestBmp.SetPixel(x, y, Color.Yellow);
                    }
                }
            }
            DestBmp.Save("DestBmp");
            return DestBmp;
        }

        public static Bitmap Resize(Bitmap originImage, Double times)
        {
            int width = Convert.ToInt32(originImage.Width * times);
            int height = Convert.ToInt32(originImage.Height * times);

            return Process(originImage, originImage.Width, originImage.Height, width, height);
        }

        private static Bitmap Process(Bitmap originImage, int oriwidth, int oriheight, int width, int height)
        {
            Bitmap resizedbitmap = new Bitmap(width, height);
            Graphics g = Graphics.FromImage(resizedbitmap);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.Clear(Color.Transparent);
            g.DrawImage(originImage, new Rectangle(0, 0, width, height), new Rectangle(0, 0, oriwidth, oriheight), GraphicsUnit.Pixel);
            return resizedbitmap;
        }

        
    }
}
