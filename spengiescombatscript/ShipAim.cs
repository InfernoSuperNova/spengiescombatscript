using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.Entities.Blocks;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    public struct ShipAimConfig
    {
        public MyGridProgram program;
        public float OffsetVert;
        public float OffsetCoax;
        public float OffsetHoriz;
        public float rollSensitivityMultiplier;
        public float maxAngular;
        public double TimeStep;
        public float kP;
        public float kI;
        public float kD;
    }
    public struct ShipAimUpdate
    {
        public MyDetectedEntityInfo target;
        public Vector3D aimPos;
        public bool hasTarget;
        public bool aim;
        public IMyShipController currentController;
        public float ProjectileVelocity;
    }
    public class ShipAim
    {
        //variables to assign once/from config
        ShipAimConfig config;
        public List<IMyGyro> gyros;

        //variables to determine here
        ClampedIntegralPID pitch;
        ClampedIntegralPID yaw;
        ClampedIntegralPID roll;
        public bool active = true;
        public Vector3D previousTargetVelocity;

        //variables to assign continuously
        public MyDetectedEntityInfo target;
        public Vector3D aimPos;
        public bool hasTarget = false;
        public bool aim = true;
        public IMyShipController currentController;
        public float ProjectileVelocity = 0.0f;
        
        public ShipAim(ShipAimConfig config, List<IMyGyro> gyros)
        {
            this.config = config;
            this.gyros = gyros;
            pitch = new ClampedIntegralPID(config.kP, config.kI, config.kD, config.TimeStep, -config.maxAngular, config.maxAngular);
            yaw = new ClampedIntegralPID(config.kP, config.kI, config.kD, config.TimeStep, -config.maxAngular, config.maxAngular);
            roll = new ClampedIntegralPID(config.kP, config.kI, config.kD, config.TimeStep, -config.maxAngular, config.maxAngular);
            active = false;
            previousTargetVelocity = Vector3D.Zero;
            
        }
        public void CheckForTargets(ShipAimUpdate newDetails)
        {
            
            target = newDetails.target;
            aimPos = newDetails.aimPos;
            hasTarget = newDetails.hasTarget;
            aim = newDetails.aim;
            currentController = newDetails.currentController;
            ProjectileVelocity = newDetails.ProjectileVelocity;


            if (hasTarget && aim)
            {
                active = true;
                config.program.Echo("Fuck shit up, captain!");
                //get reference pos
                MatrixD refOrientation = currentController.WorldMatrix;
                MatrixD ShipMatrix = currentController.CubeGrid.WorldMatrix;
                Vector3D referencePosition = currentController.GetPosition();
                //Offset reference
                referencePosition += refOrientation.Up * config.OffsetVert;
                referencePosition += refOrientation.Forward * config.OffsetCoax;
                referencePosition += refOrientation.Right * config.OffsetHoriz;
                Vector3D leadPos = Targeting.GetTargetLeadPosition(aimPos, target.Velocity, referencePosition, currentController.CubeGrid.LinearVelocity, ProjectileVelocity, config.TimeStep, ref previousTargetVelocity);
                float roll = currentController.RollIndicator;

                Vector3D worldDirection = Vector3D.Normalize(leadPos - referencePosition);
                Rotate(worldDirection, roll);
            }
            else
            {

                config.program.Echo("All systems nominal");
                if (active)
                {
                    active = false;
                    foreach (IMyGyro gyro in gyros)
                    {
                        gyro.GyroOverride = false;
                    }
                }

            }

        }
        private void Rotate(Vector3D desiredGlobalFwdNormalized, float roll)
        {
            double gp;
            double gy;
            double gr = roll;
            //Rotate Toward forward
            if (currentController.WorldMatrix.Forward.Dot(desiredGlobalFwdNormalized) < 1)
            {
                var waxis = Vector3D.Cross(currentController.WorldMatrix.Forward, desiredGlobalFwdNormalized);
                Vector3D axis = Vector3D.TransformNormal(waxis, MatrixD.Transpose(currentController.WorldMatrix));
                gp = (float)MathHelper.Clamp(pitch.Control(-axis.X), -config.maxAngular, config.maxAngular);
                gy = (float)MathHelper.Clamp(yaw.Control(-axis.Y), -config.maxAngular, config.maxAngular);
            }
            else
            {
                gp = 0.0;
                gy = 0.0;
            }
            if (Math.Abs(gy) + Math.Abs(gp) > config.maxAngular)
            {
                double adjust = config.maxAngular / (Math.Abs(gy) + Math.Abs(gp));
                gy *= adjust;
                gp *= adjust;
            }
            const double sigma = 0.0009;
            if (Math.Abs(gp) < sigma) gp = 0;
            if (Math.Abs(gy) < sigma) gy = 0;
            if (Math.Abs(gr) < sigma * 1000) gr = 0;
            ApplyGyroOverride(gp, gy, gr, gyros, currentController.WorldMatrix);
        }

        private void ApplyGyroOverride(double pitchSpeed, double yawSpeed, double rollSpeed, List<IMyGyro> gyroList, MatrixD worldMatrix)
        {
            var rotationVec = new Vector3D(pitchSpeed, yawSpeed, rollSpeed);
            var relativeRotationVec = Vector3D.TransformNormal(rotationVec, worldMatrix);

            foreach (var thisGyro in gyroList)
            {
                if (thisGyro == null)
                {
                    config.program.Echo("Removing bad gyro!");
                    gyroList.Remove(thisGyro);
                    continue;
                }
                var transformedRotationVec = Vector3D.TransformNormal(relativeRotationVec, Matrix.Transpose(thisGyro.WorldMatrix));

                thisGyro.Pitch = (float)transformedRotationVec.X;
                thisGyro.Yaw = (float)transformedRotationVec.Y;
                thisGyro.Roll = (float)transformedRotationVec.Z;
                thisGyro.GyroOverride = true;
            }
        }
    }
}
