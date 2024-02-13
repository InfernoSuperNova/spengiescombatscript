using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace IngameScript
{
    public static class Helpers
    {

        public static float RoundToDecimal(this double value, int decimalPlaces)
        {
            float multiplier = (float)Math.Pow(10, decimalPlaces);
            return (float)Math.Round(value * multiplier) / multiplier;
        }



        public static void UnfuckTurrets(List<IMyLargeTurretBase> turrets)
        {

            foreach (var turret in turrets)
            {
                UnfuckTurret(turret);

            }
        }
        public static void UnfuckTurret(IMyLargeTurretBase turret)
        {
            //store turret values
            bool enabled = turret.Enabled;
            bool targetMeteors = turret.TargetMeteors;
            bool targetMissiles = turret.TargetMissiles;
            bool targetCharacters = turret.TargetCharacters;
            bool targetSmallGrids = turret.TargetSmallGrids;
            bool targetLargeGrids = turret.TargetLargeGrids;
            bool targetStations = turret.TargetStations;
            float range = turret.Range;
            bool enableIdleRotation = turret.EnableIdleRotation;

            turret.ResetTargetingToDefault();

            //restore turret values
            turret.Enabled = enabled;
            turret.TargetMeteors = targetMeteors;
            turret.TargetMissiles = targetMissiles;
            turret.TargetCharacters = targetCharacters;
            turret.TargetSmallGrids = targetSmallGrids;
            turret.TargetLargeGrids = targetLargeGrids;
            turret.TargetStations = targetStations;
            turret.Range = range;
            turret.EnableIdleRotation = enableIdleRotation;
        }
        public static Vector3D AverageVectorList(List<Vector3D> vectors)
        {
            double x = 0;
            double y = 0;
            double z = 0;
            foreach (Vector3D vector in vectors)
            {
                x += vector.X;
                y += vector.Y;
                z += vector.Z;
            }
            x /= vectors.Count;
            y /= vectors.Count;
            z /= vectors.Count;
            return new Vector3D(x, y, z);
        }

    }
}
