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
using System.Security.Cryptography;
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
        public float flightDeck;
        public double TimeStep;
        public bool AutonomousMode;
        public double maxRollPowerToFlipRollSign;
        public double minAutonomousRollPower;
        public double maxAutonomousRollPower;
        public double autonomousRollChangeFrames;
        public double probabilityOfFlippingRollSign;
        public double kP;
        public double kI;
        public double kD;
        public double cPscaling;
        public double cIscaling;
        public double cDscaling;
        public int cascadeCount;
        public double integralClamp;
        public bool leadAcceleration;
        public Random rng;
    }
    public struct ShipAimUpdate
    {
        public MyDetectedEntityInfo target;
        public Vector3D aimPos;
        public Vector3D averageGunPos;
        public bool hasTarget;
        public bool aim;
        public IMyShipController currentController;
        public float ProjectileVelocity;
        public bool leadAcceleration;
    }
    public class ShipAim
    {
        //variables to assign once/from config
        ShipAimConfig config;
        public List<IMyGyro> gyros;
        public IMyGyro masterGyro;
        //variables to determine here
        ClampedIntegralPID pitch;
        ClampedIntegralPID yaw;

        List<ClampedIntegralPID> pitches;
        List<ClampedIntegralPID> yaws;
        List<ClampedIntegralPID> rolls;
        public bool active = true;
        public Vector3D previousTargetVelocity;
        //variables to assign continuously
        public MyDetectedEntityInfo target;
        public Vector3D aimPos;
        public bool hasTarget = false;
        public bool aim = true;
        public bool leadAcceleration = false;
        public IMyShipController currentController;
        public float ProjectileVelocity = 0.0f;
        public Vector3D averageGunPos = Vector3D.Zero;
        public MatrixD angularVelocity = MatrixD.Zero;
        public MatrixD previousRotation = MatrixD.Zero;
        private double currentRollPower = 0.5;
        private double currentRollSign = 1;
        private int timeSincelastRollChange = 0;
        public ShipAim(ShipAimConfig config, List<IMyGyro> gyros)
        {

            pitches = new List<ClampedIntegralPID>();
            yaws = new List<ClampedIntegralPID>();
            rolls = new List<ClampedIntegralPID>();
            this.config = config;
            this.gyros = gyros;
            pitch = new ClampedIntegralPID(config.kP, config.kI, config.kD, config.TimeStep, -config.integralClamp, config.integralClamp);
            yaw = new ClampedIntegralPID(config.kP, config.kI, config.kD, config.TimeStep, -config.integralClamp, config.integralClamp);

            for (int i = 0; i < config.cascadeCount; i++)
            {

                double proportionalGain = config.kP / Math.Pow(config.cPscaling, i);
                double integralGain = config.kI / Math.Pow(config.cIscaling, i);
                double derivativeGain = config.kD / Math.Pow(config.cDscaling, i);

                pitches.Add(new ClampedIntegralPID(proportionalGain, integralGain, derivativeGain, config.TimeStep, -config.integralClamp, config.integralClamp));
                yaws.Add(new ClampedIntegralPID(proportionalGain, integralGain, derivativeGain, config.TimeStep, -config.integralClamp, config.integralClamp));
                rolls.Add(new ClampedIntegralPID(proportionalGain, integralGain, derivativeGain, config.TimeStep, -config.integralClamp, config.integralClamp));
            }


            active = false;
            previousTargetVelocity = Vector3D.Zero;
            this.leadAcceleration = config.leadAcceleration;
        }
        public void CheckForTargets(ShipAimUpdate newDetails)
        {
            averageGunPos = newDetails.averageGunPos;
            target = newDetails.target;
            aimPos = newDetails.aimPos;
            hasTarget = newDetails.hasTarget;
            aim = newDetails.aim;
            currentController = newDetails.currentController;
            ProjectileVelocity = newDetails.ProjectileVelocity;
            leadAcceleration = newDetails.leadAcceleration;

            if (hasTarget && aim)
            {
                active = true;
                LCDManager.AddText("Fuck shit up, captain!");
                //get reference pos
                MatrixD refOrientation = currentController.WorldMatrix;
                MatrixD ShipMatrix = currentController.CubeGrid.WorldMatrix;
                Vector3D referencePosition = currentController.GetPosition();
                //Offset reference
                referencePosition += refOrientation.Up * config.OffsetVert;
                referencePosition += refOrientation.Forward * config.OffsetCoax;
                referencePosition += refOrientation.Right * config.OffsetHoriz;
                if (averageGunPos != Vector3D.Zero)
                {
                    referencePosition = averageGunPos;
                }
                Data.prevTargetVelocity = new Vector3D(previousTargetVelocity.X, previousTargetVelocity.Y, previousTargetVelocity.Z);
                angularVelocity = previousRotation - target.Orientation;

                Vector3D leadPos = Targeting.GetTargetLeadPosition(aimPos, target.Velocity, referencePosition, currentController.CubeGrid.LinearVelocity, ProjectileVelocity, config.TimeStep, ref previousTargetVelocity, true, leadAcceleration);
                previousRotation = target.Orientation;
                double roll = currentController.RollIndicator;
                if (config.AutonomousMode)
                {
                    // Get altitude
                    double altitude;
                    bool isInGravity = currentController.TryGetPlanetElevation(MyPlanetElevation.Surface, out altitude);
                    
                    if (isInGravity) LCDManager.AddText("Altitude: " + altitude.ToString());
                    if (isInGravity && altitude < config.flightDeck)
                    {
                        // handles angling the ship up when below the flight deck
                        Vector3D gravityDirection = currentController.GetNaturalGravity().Normalized();
                        
                        Vector3D upDirection = currentController.WorldMatrix.Up;
                        double rollAngleChange = Math.Asin(Vector3D.Dot(currentController.WorldMatrix.Left, gravityDirection)) * 10;
                        roll = ControlPIDList(rolls, rollAngleChange);
                    }
                    else
                    {
                        ControlPIDList(rolls, 0); // hacky way to reset the derivative term
                        if (timeSincelastRollChange > config.autonomousRollChangeFrames)
                        {
                            if (config.maxRollPowerToFlipRollSign > currentRollPower && config.rng.NextDouble() < config.probabilityOfFlippingRollSign)
                            {
                                currentRollSign *= -1;
                            }
                            currentRollPower = config.rng.NextDouble() * (config.maxAutonomousRollPower - config.minAutonomousRollPower) + config.minAutonomousRollPower;
                            timeSincelastRollChange = 0;
                        }
                        roll = currentRollPower * currentRollSign;
                        config.program.Echo("Roll speed:");
                        config.program.Echo(roll.ToString());
                        timeSincelastRollChange++;
                    }

                }

                Vector3D worldDirection = Vector3D.Normalize(leadPos - referencePosition);
                Rotate(worldDirection, roll);
            }
            else
            {

                LCDManager.AddText("All systems nominal");
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

        private double ControlPIDList(List<ClampedIntegralPID> pidList, double error)
        {
            double output = 0;
            for (int i = 0; i < pidList.Count; i++)
            {
                output += pidList[i].Control(error);
                LCDManager.AddText(pidList[i].GetErrorSum().ToString());
            }
            return output;
        }
        private void Rotate(Vector3D desiredGlobalFwdNormalized, double roll)
        {
            double gp;
            double gy;
            double gr = roll;
            
            //Rotate Toward forward
            if (currentController.WorldMatrix.Forward.Dot(desiredGlobalFwdNormalized) < 1)
            {
                var waxis = Vector3D.Cross(currentController.WorldMatrix.Forward, desiredGlobalFwdNormalized);
                Vector3D axis = Vector3D.TransformNormal(waxis, MatrixD.Transpose(currentController.WorldMatrix));
                double x = ControlPIDList(pitches, -axis.X);
                double y = ControlPIDList(yaws, -axis.Y);


                gp = (float)MathHelper.Clamp(x, -config.maxAngular, config.maxAngular);
                gy = (float)MathHelper.Clamp(y, -config.maxAngular, config.maxAngular);
            }
            else
            {
                gp = 0.0;
                gy = 0.0;
            }
            if (Math.Abs(gy) + Math.Abs(gp) > config.maxAngular)
            {
                double adjust = config.maxAngular / (Math.Abs(gy) + Math.Abs(gp) + Math.Abs(gr));
                gy *= adjust;
                gp *= adjust;
                gr *= adjust;
            }
            const double sigma = 0.000009;
            if (Math.Abs(gp) < sigma) gp = 0;
            if (Math.Abs(gy) < sigma) gy = 0;
            //if (Math.Abs(gr) < sigma * 1000) gr = 0;
            ApplyGyroOverride(gp, gy, gr, gyros, currentController.WorldMatrix);
        }

        private void ApplyGyroOverride(double pitchSpeed, double yawSpeed, double rollSpeed, List<IMyGyro> gyroList, MatrixD worldMatrix)
        {
            var rotationVec = new Vector3D(pitchSpeed, yawSpeed, rollSpeed);
            var relativeRotationVec = Vector3D.TransformNormal(rotationVec, worldMatrix);
            //if (masterGyro == null || masterGyro.Closed)
            //{
            //    masterGyro = GetNewGyro(gyroList);
            //    if (masterGyro == null)
            //    {
            //        return;
            //    }
            //}
            //if (!masterGyro.IsFunctional || !masterGyro.IsWorking || !masterGyro.Enabled)
            //{
            //    masterGyro.Pitch = 0;
            //    masterGyro.Yaw = 0;
            //    masterGyro.Roll = 0;
            //    masterGyro.GyroOverride = false;
            //    masterGyro = GetNewGyro(gyroList);
            //}
            //if (masterGyro == null)
            //{
            //    LCDManager.AddText("No functional gyroscopes found!");
            //    return;
            //}
            //var transformedRotationVec = Vector3D.TransformNormal(relativeRotationVec, Matrix.Transpose(masterGyro.WorldMatrix));
            //LCDManager.AddText("Using gyroscope : " + masterGyro.CustomName);
            //masterGyro.Pitch = (float)transformedRotationVec.X;
            //masterGyro.Yaw = (float)transformedRotationVec.Y;
            //masterGyro.Roll = (float)transformedRotationVec.Z;
            //masterGyro.GyroOverride = true;
            foreach (IMyGyro gyro in gyroList)
            {
                if (gyro.IsFunctional && gyro.IsWorking && gyro.Enabled && !gyro.Closed)
                {
                    var transformedRotationVec = Vector3D.TransformNormal(relativeRotationVec, MatrixD.Transpose(gyro.WorldMatrix));
                    gyro.Pitch = (float)transformedRotationVec.X;
                    gyro.Yaw = (float)transformedRotationVec.Y;
                    gyro.Roll = (float)transformedRotationVec.Z;
                    gyro.GyroOverride = true;
                }
            }
        }
        private IMyGyro GetNewGyro(List<IMyGyro> gyroList)
        {
            for (int i = 0; i < gyroList.Count; i++)
            {
                if (gyroList[i].IsFunctional && gyroList[i].IsWorking && gyroList[i].Enabled && !gyroList[i].Closed)
                {
                    return gyroList[i];
                }
            }
            return null;
        }
    }
}
