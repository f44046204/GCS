using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using GMap.NET;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;

namespace GCSv2
{
    class TCAS
    {
        //TTC
        public static int tauTA = 30;
        public static int tauRA = 20;

        public static double angleLimit (double angle)
        {
            if (angle > 360)
                angle -= 360;
            if (angle < 0)
                angle += 360;
            return angle;
        }

        public static bool verticalSelection (double Height1, double vSpeed1, double Height2, double vSpeed2)
        {
            double relHeight = Height2 - Height1;
            double relSpeed = vSpeed2 - vSpeed1;
            if (relSpeed * relHeight < 0)
                return true;
            else
                return false;
        }

        public static bool horizontalSelection (double yaw, PointLatLng llh1, PointLatLng llh2, double[] speed1, double[] speed2)
        {
            double theta = Math.Atan((llh2.Lng - llh1.Lng) / (llh2.Lat - llh1.Lat));
            if (llh2.Lat < llh1.Lat)
                theta += 180;

            theta = angleLimit(theta);

            double phase = theta - yaw;
            phase = angleLimit(phase);

            double alpha = Math.Atan((speed2[0] - speed1[0]) / (speed2[1] - speed1[1]));
            if (speed2[1] < speed1[1])
                alpha += 180;
            alpha = angleLimit(alpha);

            if (phase > 0 && phase < 90)
            {
                if (alpha < 180)
                    return false;
                else
                    return true;
            }
            else if(phase > 90 && phase < 180)
            {
                if (alpha < 270)
                    return false;
                else
                    return true;
            }
            else if(phase > 180 && phase < 270)
            {
                if (alpha > 90)
                    return false;
                else
                    return true;
            }
            else
            {
                if (alpha > 180)
                    return false;
                else
                    return true;
            }
        }

        public static void TAbubble (double lat, double lng, double speed, int zoom, GMapOverlay overlay)
        {
            //separation bubble for TA
            Image bubbleTA = Image.FromFile("TA.png");
            Bitmap originTA = (Bitmap)bubbleTA;
            originTA.MakeTransparent(Color.White);
            Bitmap resizedTA = new Bitmap(originTA.Width, originTA.Height);
            
            double scale = 1;
            if (zoom > 20)
                scale *= Math.Pow(2,(zoom - 20));
            else if (zoom < 20)
                scale *= Math.Pow(0.5, (20 - zoom));
            else
                scale = 1;

            if(scale > 0.05 && scale < 4)
            {
                if (speed != 0)
                    resizedTA = ScaleImage(originTA, (int)(originTA.Width * speed * scale), (int)(originTA.Height * speed * scale));

                GMapMarker TA = new GMarkerGoogle(new PointLatLng(lat, lng), resizedTA);
                TA.Offset = new Point(-resizedTA.Width / 2, -resizedTA.Height / 2);

                if (speed != 0)
                    overlay.Markers.Add(TA);
            }
        }

        public static Bitmap ScaleImage(Bitmap pBmp, int pWidth, int pHeight)
        {
            try
            {
                Bitmap tmpBmp = new Bitmap(pWidth, pHeight);
                Graphics tmpG = Graphics.FromImage(tmpBmp);

                //tmpG.InterpolationMode = InterpolationMode.HighQualityBicubic;

                tmpG.DrawImage(pBmp,
                                           new Rectangle(0, 0, pWidth, pHeight),
                                           new Rectangle(0, 0, pBmp.Width, pBmp.Height),
                                           GraphicsUnit.Pixel);
                tmpG.Dispose();
                return tmpBmp;
            }
            catch
            {
                return null;
            }
        }
    }
}
