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
    public static class Targeting
    {
        public static IMyShipController currentController;
        public static MyGridProgram program;
        public static Vector3D GetTargetLeadPosition(Vector3D targetPos, Vector3D targetVel, Vector3D shooterPos, Vector3D shooterVel, float projectileSpeed, double timeStep, ref Vector3D previousTargetVelocity)
        {
            Vector3D deltaV = (targetVel - previousTargetVelocity) / timeStep;
            //Echo target acceleration to 2dp
            Vector3D relativePos = targetPos - shooterPos;
            Vector3D relativeVel = targetVel - shooterVel;
            Vector3D gravity = currentController.GetNaturalGravity();
            double timeToIntercept = CalculateTimeToIntercept(relativePos, relativeVel, deltaV, projectileSpeed);
            Vector3D targetLeadPos = targetPos + ((deltaV + relativeVel) * timeToIntercept) + (gravity * timeToIntercept);
            previousTargetVelocity = targetVel;

            
            program.Echo("Target velocity: " + targetVel.Length().RoundToDecimal(2).ToString() + " m/s\nRelative: " + relativeVel.Length().RoundToDecimal(2).ToString() + " m/s");
            program.Echo("Target acceleration: " + deltaV.Length().RoundToDecimal(2).ToString() + " m/s^2");
            program.Echo("Target distance: " + relativePos.Length().RoundToDecimal(2).ToString() + " meters");
            program.Echo($"Time to intercept (@{projectileSpeed}m/s): " + timeToIntercept.RoundToDecimal(2).ToString() + " seconds");


            return targetLeadPos;
            

        }

        private static double CalculateTimeToIntercept(Vector3D relativePos, Vector3D relativeVel, Vector3D targetAcc, float projectileSpeed)
        {
            double a = targetAcc.X * targetAcc.X + targetAcc.Y * targetAcc.Y + targetAcc.Z * targetAcc.Z - projectileSpeed * projectileSpeed;
            double b = 2 * (relativePos.X * targetAcc.X + relativePos.Y * targetAcc.Y + relativePos.Z * targetAcc.Z + relativeVel.X * targetAcc.X + relativeVel.Y * targetAcc.Y + relativeVel.Z * targetAcc.Z);
            double c = relativePos.X * relativePos.X + relativePos.Y * relativePos.Y + relativePos.Z * relativePos.Z;

            double discriminant = b * b - 4 * a * c;

            if (discriminant < 0)
            {
                // No real solution, return a default position (e.g., current target position)
                return 0;
            }

            double t1 = (-b + Math.Sqrt(discriminant)) / (2 * a);
            double t2 = (-b - Math.Sqrt(discriminant)) / (2 * a);

            if (t1 < 0 && t2 < 0)
            {
                // Both solutions are negative, return a default position (e.g., current target position)
                return 0;
            }
            else if (t1 < 0)
            {
                // t1 is negative, return t2
                return t2;
            }
            else if (t2 < 0)
            {
                // t2 is negative, return t1
                return t1;
            }
            else
            {
                // Both solutions are valid, return the minimum positive time
                return Math.Min(t1, t2);
            }
        }
    }
}
