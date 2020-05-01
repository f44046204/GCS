using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GMap.NET;

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

        public static double[] PF (int count, double[] Oxyz, List<double[]> Ixyz, double[] Txyz)
        {
            double Frep;
            double[] repulsive = new double[3];
            double[] attractive = new double[3];
            double[] resultant = new double[3];

            for (int i = 0; i < count; i++)
            {
                if(Ixyz[i]!=Oxyz)
                {
                    double distance = Math.Sqrt((Oxyz[0] - Ixyz[i][0]) * (Oxyz[0] - Ixyz[i][0]) + 
                                                (Oxyz[1] - Ixyz[i][1]) * (Oxyz[1] - Ixyz[i][1]) + 
                                                (Oxyz[2] - Ixyz[i][2]) * (Oxyz[2] - Ixyz[i][2]));

                    if (distance/1000 <= r)
                    {
                        Frep = -(distance / 1000) + r;
                        Frep *= 1000; 

                        repulsive[0] += Frep * (Oxyz[0] - Ixyz[i][0]) / distance;
                        repulsive[1] += Frep * (Oxyz[1] - Ixyz[i][1]) / distance;
                        repulsive[2] += Frep * (Oxyz[2] - Ixyz[i][2]) / distance;
                    }
                }
            }
            attractive[0] = Txyz[0] - Oxyz[0];
            attractive[1] = Txyz[1] - Oxyz[1];
            attractive[2] = Txyz[2] - Oxyz[2];
            
            resultant[0] = attractive[0] + repulsive[0];
            resultant[1] = attractive[1] + repulsive[1];
            resultant[2] = attractive[2] + repulsive[2];
            
            return (resultant);
        }
    }
}
