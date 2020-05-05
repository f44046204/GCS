using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GMap.NET;
using System.Windows.Media.Media3D;

namespace GCSv2
{
    class CollisionAvoidance
    {
        public const double r = 1; //km
        public const double N = 10;

        public static double[] NDM (double speed, double[] Oxyz, double[] Ixyz)
        {
            double[] direction = new double[3];
            double[] acceleration = new double[3];
            direction[0] = Oxyz[0] - Ixyz[0];
            direction[1] = Oxyz[1] - Ixyz[1];
            direction[2] = Oxyz[2] - Ixyz[2];

            acceleration[0] = - speed * direction[0];
            acceleration[1] = - speed * direction[1];
            acceleration[2] = - speed * direction[2];
            return (acceleration);
        }

        public static double[] MNDM (int count, double[] V, double[] Oxyz, List<double[]> Ixyz)
        {
            double[] direction = new double[3];
            double[] acceleration = new double[3];
            double distance;
            double speed = Math.Sqrt(V[0] * V[0] + V[1] * V[1] + V[2] * V[2]);

            for (int i=0;i<count;i++)
            {
                if(Ixyz[i] != Oxyz)
                {
                    distance = Math.Sqrt((Ixyz[i][0] - Oxyz[0])* (Ixyz[i][0] - Oxyz[0]) + (Ixyz[i][1] - Oxyz[1])* (Ixyz[i][1] - Oxyz[1]) + (Ixyz[i][2] - Oxyz[2])* (Ixyz[i][2] - Oxyz[2]));
                    direction[0] = (Oxyz[0] - Ixyz[i][0]) / distance;
                    direction[1] = (Oxyz[1] - Ixyz[i][1]) / distance;
                    direction[2] = (Oxyz[2] - Ixyz[i][2]) / distance;

                    if (V[0] / speed == direction[0] && V[1] / speed == direction[1] && V[2] / speed == direction[2])
                    {
                        direction[0] = direction[0] * Math.Cos(30 * Math.PI / 180) - direction[1] * Math.Sin(30 * Math.PI / 180);
                        direction[1] = direction[0] * Math.Sin(30 * Math.PI / 180) + direction[1] * Math.Cos(30 * Math.PI / 180);
                    }

                    if(distance<=600)
                    {
                        acceleration[0] += speed * direction[0];
                        acceleration[1] += speed * direction[1];
                        acceleration[2] += speed * direction[2];
                    }
                }
            }
            return (acceleration);
        }

        public static Vector3D PF (int count, Point3D Oxyz, List<Point3D> Ixyz, Point3D Txyz)
        {
            double Frep;
            Vector3D repulsive = new Vector3D();
            Vector3D attractive = new Vector3D();
            Vector3D resultant = new Vector3D();

            for (int i = 0; i < count; i++)
            {
                if(Ixyz[i]!=Oxyz)
                {
                    double distance = Point3D.Subtract(Oxyz, Ixyz[i]).Length;
                    
                    if (distance/1000 <= r)
                    {
                        Frep = -(distance / 1000) * Math.Pow(Math.E, 0.5 * (distance / 1000) * (distance / 1000)) + r * Math.Pow(Math.E, 0.5 *r * r);
                        Frep *= 1000;

                        repulsive += Frep * Point3D.Subtract(Oxyz, Ixyz[i]) / distance;
                    }
                }
            }
            attractive = Point3D.Subtract(Txyz, Oxyz);

            if (Vector3D.AngleBetween(attractive, repulsive) == 0)
            {
                repulsive.X *= Math.Cos(5 * Math.PI / 180);
                repulsive.Y *= Math.Cos(5 * Math.PI / 180);
            }
               
            resultant = Vector3D.Add(attractive, repulsive);
            
            return (resultant);
        }

        public static void threatenDetection(Point3D Po, Point3D Pi, Vector3D Vo, Vector3D Vi)
        {
            double normLength = new double();
            double projectLength = new double();
            double approachRate = new double();
            Point3D nextPo = Point3D.Add(Po, Vo);
            Point3D nextPi = Point3D.Add(Pi, Vi);

            if (Point3D.Subtract(nextPo, nextPi).Length < Point3D.Subtract(Po, Pi).Length)
            {
                Vector3D Norm = Vector3D.CrossProduct(Vo, Vi);
                normLength = Norm.Length;
                projectLength = Vector3D.DotProduct(Point3D.Subtract(Po, Pi), Norm);
                approachRate = Vector3D.Subtract(Vi, Vo).Length;
            }

            double time = Point3D.Subtract(Po, Pi).Length / approachRate;
            double minDistance = projectLength / normLength;

            if(time <= 40)
                Console.WriteLine(time);
        }
    }
}
