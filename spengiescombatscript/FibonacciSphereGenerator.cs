using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace IngameScript
{
    public static class FibonacciSphereGenerator
    {
        static float phi = (float)(Math.PI * (3.0 - Math.Sqrt(5.0))); // golden ratio
        public static List<Vector3D> Generate(Vector3D center, float radius, int count)
        {
            List<Vector3D> points = new List<Vector3D>();
            

            for (int i = 0; i < count; i++)
            {
                float y = 1 - (i / (float)(count - 1)) * 2; // range from 1 to -1
                float radiusAtY = (float)Math.Sqrt(1 - y * y) * radius;
                float theta = phi * i; // golden angle increment

                float x = (float)(Math.Cos(theta) * radiusAtY);
                float z = (float)(Math.Sin(theta) * radiusAtY);

                points.Add(new Vector3D(center.X + x, center.Y + y * radius, center.Z + z));
            }

            return points;
        }
    }
}
