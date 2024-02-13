using Sandbox.Game.Entities;
using Sandbox.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;


namespace IngameScript
{
    public static class Turrets
    {
        public static float projectileVelocity = 0;
        public static bool overrideTurretAim = false;
        public static MyGridProgram program;
        public static double TimeStep;
        public static void UpdateTurretAim (IMyShipController currentController, Dictionary<IMyFunctionalBlock, MyDetectedEntityInfo> turretTargets)
        {
            if (!overrideTurretAim)
            {
                return;
            }
            Vector3D shipVelocity = currentController.CubeGrid.LinearVelocity;

            foreach (var pair in turretTargets)
            {
                if (pair.Key is IMyLargeTurretBase)
                {
                    IMyLargeTurretBase turret = (IMyLargeTurretBase)pair.Key;
                    if (turret.IsShooting && turret.IsAimed)
                    {
                        Vector3D aimPoint = GetAimPoint(pair, shipVelocity);

                        SetTurretAim(turret, aimPoint);
                        //Helpers.UnfuckTurret(turret);
                    }
                }
            }
        }

        private static Vector3D GetAimPoint(KeyValuePair<IMyFunctionalBlock, MyDetectedEntityInfo> pair, Vector3D shipVelocity)
        {
            IMyFunctionalBlock turret = pair.Key;
            MyDetectedEntityInfo target = pair.Value;
            if (target.HitPosition != null)
            {
                Vector3D tempVelocity = new Vector3D(Data.prevTargetVelocity.X, Data.prevTargetVelocity.Y, Data.prevTargetVelocity.Z);
                return Targeting.GetTargetLeadPosition((Vector3D)target.HitPosition, target.Velocity, MatrixD.Zero, turret.GetPosition(), shipVelocity, projectileVelocity, TimeStep, ref tempVelocity, false, false);
            }
            return Vector3D.Zero;
        }

        private static void SetTurretAim(IMyLargeTurretBase turret, Vector3D aimPoint)
        {
            // Get the turret's local coordinate system vectors
            Vector3D turretForward = turret.WorldMatrix.Forward;
            Vector3D turretUp = turret.WorldMatrix.Up;
            Vector3D turretRight = turret.WorldMatrix.Right;

            // Calculate the direction vector from the turret's position to the aim point
            Vector3D targetDir = Vector3D.Normalize(aimPoint - turret.WorldMatrix.Translation);

            // Calculate the azimuth angle (yaw) in radians
            double azimuth = Math.Atan2(Vector3D.Dot(targetDir, turretRight), Vector3D.Dot(targetDir, turretForward));

            // Calculate the elevation angle (pitch) in radians
            double elevation = Math.Asin(Vector3D.Dot(targetDir, turretUp));

            if (double.IsNaN(azimuth) || double.IsNaN(elevation))
            {
                return;
            }
            turret.SetManualAzimuthAndElevation(-(float)azimuth, (float)elevation);
            turret.SyncAzimuth();
            turret.SyncElevation();
        }
    }
}
