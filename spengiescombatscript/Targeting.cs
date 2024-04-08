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
            public static Vector3D GetTargetLeadPosition(Vector3D targetPos, Vector3D targetVel, Vector3D shooterPos, Vector3D shooterVel, float projectileSpeed, double timeStep, ref Vector3D previousTargetVelocity, bool doEcho, bool leadAcceleration)
            {
                Vector3D deltaV = ((targetVel - previousTargetVelocity) / timeStep) / 2;

                Vector3D relativePos = targetPos - shooterPos;
                Vector3D relativeVel = targetVel - shooterVel;
                Vector3D gravity = currentController.GetNaturalGravity() / 2;

                double timeToIntercept;
                Vector3D targetLeadPos = Vector3D.Zero;
                if (leadAcceleration)
                {
                    timeToIntercept = CalculateTimeToIntercept(relativePos, relativeVel, deltaV, -gravity, projectileSpeed);
                    targetLeadPos = targetPos + ((deltaV + relativeVel) * timeToIntercept) + (-gravity * timeToIntercept);
                }
                else
                {
                    timeToIntercept = CalculateTimeToIntercept(relativePos, relativeVel, Vector3D.Zero, -gravity, projectileSpeed);
                    targetLeadPos = targetPos + ((relativeVel) * timeToIntercept) + (-gravity * timeToIntercept);
                }
            
                previousTargetVelocity = targetVel;

                if (doEcho)
                {
                    LCDManager.AddText("Target velocity: " + targetVel.Length().RoundToDecimal(2).ToString() + " m/s\nRelative: " + relativeVel.Length().RoundToDecimal(2).ToString() + " m/s");
                    LCDManager.AddText("Target acceleration: " + deltaV.Length().RoundToDecimal(2).ToString() + " m/s^2");
                    LCDManager.AddText("Target distance: " + relativePos.Length().RoundToDecimal(2).ToString() + " meters");
                    LCDManager.AddText($"Time to intercept (@{projectileSpeed}m/s): " + timeToIntercept.RoundToDecimal(2).ToString() + " seconds");

                }
                Data.aimPos = targetLeadPos; // This is a hack, fix this shit
                return targetLeadPos;
            }
        private static Vector3D RotatePosition(Vector3D position, Vector3D center, MatrixD rotation)
        {
            Vector3D relativePosition = position - center;
            Vector3D rotatedPosition = Vector3D.Transform(relativePosition, rotation);
            return center + rotatedPosition;
        }

        private static double CalculateTimeToIntercept(Vector3D relativePos, Vector3D relativeVel, Vector3D targetAcc, Vector3D gravity, float projectileSpeed)
        {
            double a = targetAcc.LengthSquared() - projectileSpeed * projectileSpeed;
            double b = 2 * (Vector3D.Dot(relativePos, targetAcc) + Vector3D.Dot(relativeVel, targetAcc));
            double c = relativePos.LengthSquared();

            // Factor in gravity
            a += gravity.LengthSquared();
            b += 2 * Vector3D.Dot(relativePos, gravity);

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
