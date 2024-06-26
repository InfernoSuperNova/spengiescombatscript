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
        public double derivativeNonLinearity;
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
        ClampedIntegralPID roll;

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
        public int framesWithTargetDriftingAwayFromShip = 0;
        public ShipAim(ShipAimConfig config, List<IMyGyro> gyros)
        {

            this.config = config;
            this.gyros = gyros;
            pitch = new ClampedIntegralPID(config.kP, config.kI, config.kD, config.TimeStep, -config.integralClamp, config.integralClamp);
            yaw = new ClampedIntegralPID(config.kP, config.kI, config.kD, config.TimeStep, -config.integralClamp, config.integralClamp);
            roll = new ClampedIntegralPID(config.kP, config.kI, config.kD, config.TimeStep, -config.integralClamp, config.integralClamp);



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

            UpdateDerivative();
            if (hasTarget && aim)
            {
                Vector3D referencePosition = currentController.GetPosition();
                if (config.AutonomousMode)
                {
                    Vector3D targetAcceleration = (target.Velocity - previousTargetVelocity) / config.TimeStep;
                    Vector3D meToTarget = aimPos - referencePosition;
                    float dot = Vector3.Dot(meToTarget, target.Velocity);
                    if (targetAcceleration.LengthSquared() < 0.1 && dot > 0)
                    {
                        framesWithTargetDriftingAwayFromShip++;
                    }
                    else
                    {
                        framesWithTargetDriftingAwayFromShip = 0;
                    }
                    LCDManager.AddText("Frames with target drifting away: " + framesWithTargetDriftingAwayFromShip.ToString());
                }
                active = true;
                LCDManager.AddText("Fuck shit up, captain!");
                //get reference pos
                MatrixD refOrientation = currentController.WorldMatrix;
                MatrixD ShipMatrix = currentController.CubeGrid.WorldMatrix;
                
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
                double rollValue = currentController.RollIndicator;
                if (config.AutonomousMode)
                {

                    double altitude = 0;
                    bool isInGravity = GetGravity(ref altitude);
                    if (isInGravity) LCDManager.AddText("Altitude: " + altitude.ToString());
                    if (isInGravity && altitude < config.flightDeck)
                    {
                        rollValue = AngleRollToGravity();
                    }
                    else
                    {
                        roll.Control(0); // hacky way to reset the integral and derivative term
                        if (timeSincelastRollChange > config.autonomousRollChangeFrames)
                        {
                            if (config.maxRollPowerToFlipRollSign > currentRollPower && config.rng.NextDouble() < config.probabilityOfFlippingRollSign)
                            {
                                currentRollSign *= -1;
                            }
                            currentRollPower = config.rng.NextDouble() * (config.maxAutonomousRollPower - config.minAutonomousRollPower) + config.minAutonomousRollPower;
                            timeSincelastRollChange = 0;
                        }
                        rollValue = currentRollPower * currentRollSign;
                        LCDManager.AddText("Roll speed: " + rollValue.ToString());
                        timeSincelastRollChange++;
                    }

                }

                Vector3D worldDirection = Vector3D.Normalize(leadPos - referencePosition);
                
                Rotate(worldDirection, rollValue);
            }
            else // not aiming
            {
                if (config.AutonomousMode)
                {
                    double altitude = 0;
                    bool isInGravity = GetGravity(ref altitude);
                    if (isInGravity) LCDManager.AddText("Altitude: " + altitude.ToString());
                    if (isInGravity && altitude < config.flightDeck)
                    {
                        double roll = AngleRollToGravity();
                        // get a vector perpendicular to gravity to level out the pitch
                        Vector3D desiredGlobalFwdNormalized = -Vector3D.Normalize(Vector3D.Cross(currentController.GetNaturalGravity(), currentController.WorldMatrix.Right));
                        LCDManager.AddText(Vector3D.Dot(currentController.WorldMatrix.Forward, desiredGlobalFwdNormalized).ToString());
                        if (Vector3D.Dot(currentController.WorldMatrix.Forward, desiredGlobalFwdNormalized) < 0.9999)
                        {
                            LCDManager.AddText("Rolling to gravity");
                            if (active == false)
                            {
                                gyros.ForEach(g => g.GyroOverride = true);
                                active = true;
                            }
                            Rotate(desiredGlobalFwdNormalized, roll);
                        }
                        else
                        {
                            NeutralizeRoll();
                        }
                    }
                    else
                    {
                        NeutralizeRoll();
                    }
                        
                }
                else
                {
                    NeutralizeRoll();
                }
            }
        }

        private void UpdateDerivative()
        {
            Vector3D angularVelocity = currentController.GetShipVelocities().AngularVelocity;
            angularVelocity = Vector3D.TransformNormal(angularVelocity, MatrixD.Transpose(currentController.WorldMatrix));
            angularVelocity.Z = 0;

            angularVelocity *= 60 / (2 * Math.PI);
            
            double newKD = Math.Pow(angularVelocity.Length(), config.derivativeNonLinearity);
            LCDManager.AddText("New KD: " + (newKD * config.kD));
            pitch.Kd = newKD * config.kD;
            yaw.Kd = newKD * config.kD;

        }
        private bool GetGravity(ref double altitude)
        {
            // Get altitude
            return currentController.TryGetPlanetElevation(MyPlanetElevation.Surface, out altitude);
        }

        private void NeutralizeRoll()
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

        private double AngleRollToGravity()
        {
            // handles angling the ship up when below the flight deck
            Vector3D gravityDirection = currentController.GetNaturalGravity().Normalized();

            Vector3D upDirection = currentController.WorldMatrix.Up;
            double rollAngleChange = Math.Asin(Vector3D.Dot(currentController.WorldMatrix.Left, gravityDirection)) * 10;
            return roll.Control(rollAngleChange);
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
                double x = pitch.Control(-axis.X);
                double y = yaw.Control(-axis.Y);

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
                double adjust = config.maxAngular / (Math.Abs(gy) + Math.Abs(gp));
                gy *= adjust;
                gp *= adjust;
                // No you're right, this sucks
                gr = Vector3D.TransformNormal(currentController.GetShipVelocities().AngularVelocity, MatrixD.Transpose(currentController.WorldMatrix)).Z * 10;
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
