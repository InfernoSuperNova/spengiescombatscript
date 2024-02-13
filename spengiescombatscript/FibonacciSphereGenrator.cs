using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IngameScript
{
    using System;
    using System.Collections.Generic;
    using VRageMath; // This is the namespace for Vector3D in Space Engineers.

    public static class FibonacciSphereGenerator
    {
        public static List<Vector3D> GenerateFibonacciSphere(Vector3D center, double radius, int numPoints)
        {
            List<Vector3D> points = new List<Vector3D>();

            double goldenRatio = (1 + Math.Sqrt(5)) / 2 - 1; // Golden ratio constant

            double angleIncrement = Math.PI * 2 * goldenRatio;

            for (int i = 0; i < numPoints; i++)
            {
                double y = 1 - (i / (double)(numPoints - 1)) * 2; // Distribute points evenly on the y-axis

                double radiusAtY = Math.Sqrt(1 - y * y) * radius;

                double theta = i * angleIncrement;

                double x = Math.Cos(theta) * radiusAtY;
                double z = Math.Sin(theta) * radiusAtY;

                points.Add(new Vector3D(center.X + x, center.Y + y * radius, center.Z + z));
            }

            return points;
        }
    }
}
