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
    partial class Program : MyGridProgram
    {


        string TurretGroupName = "Turrets";
        string GyroGroupName = "Gyros";

        string ForwardReferenceName = "Cockpit";

        //offset the forward reference
        float OffsetVert = -5; //offset in meters, positive is up
        float OffsetCoax = 0; //offset in meters, positive is forward
        float OffsetHoriz = 0; //offset in meters, positive is right

        

        float ProjectileVelocity = 2000.0f; //Initialize this if you only have one primary weapon, otherwise run with argument to set velocity
        float rollSensitivityMultiplier = 1000.0f; //Increase if spinning too slowly, decrease if spinning too quickly



        //PID CONFIG
        //Ask Gxaps for help if you don't know how to tune

        //Level one PID, for large oscellations
        float l1P = 300f;
        float l1I = 0f;
        float l1D = 10f;
        //Level two PID, for vibrations
        float l2P = 1f;
        float l2I = 0f;
        float l2D = 0.5f;
        const double TimeStep = 1.0 / 60.0;




        //No touchy below >:(





        bool aim = true;
        bool active = true;

        Vector3D previousTargetVelocity;

        IMyShipController forwardReference;
        List<IMyLargeTurretBase> turrets;
        List<IMyGyro> gyros;

        PID l1pitch;
        PID l1yaw;
        PID l1roll;
        PID l2pitch;
        PID l2yaw;
        PID l2roll;

        public Program()
        {
            l1pitch = new PID(l1P, l1I, l1D, TimeStep);
            l1yaw = new PID(l1P, l1I, l1D, TimeStep);
            l1roll = new PID(l1P, l1I, l1D, TimeStep);
            l2pitch = new PID(l2P, l2I, l2D, TimeStep);
            l2yaw = new PID(l2P, l2I, l2D, TimeStep);
            l2roll = new PID(l2P, l2I, l2D, TimeStep);

            Runtime.UpdateFrequency = UpdateFrequency.Update1 | UpdateFrequency.Update100;
            forwardReference = GridTerminalSystem.GetBlockWithName(ForwardReferenceName) as IMyShipController;
            turrets = new List<IMyLargeTurretBase>();
            gyros = new List<IMyGyro>();
            List<IMyBlockGroup> groups = new List<IMyBlockGroup>();

            GridTerminalSystem.GetBlockGroups(groups);
            foreach (IMyBlockGroup group in groups)
            {
                if (group.Name == TurretGroupName)
                {
                    group.GetBlocksOfType(turrets);
                }
                if (group.Name == GyroGroupName)
                {
                    group.GetBlocksOfType(gyros);
                }
            }
            foreach (IMyGyro gyro in gyros)
            {
                gyro.GyroOverride = false;
            }
        }

        public void Save()
        {

        }

        public void Main(string argument, UpdateType updateType)
        {
            if ((updateType & (UpdateType.Trigger | UpdateType.Terminal)) != 0)
            {
                RunCommand(argument);
            }

            // If the update source has this update flag, it means
            // that it's run from the frequency system, and we should
            // update our continuous logic.
            if ((updateType & UpdateType.Update1) != 0)
            {
                RunContinuousLogic();

            }
            if ((updateType & UpdateType.Update100) != 0)
            {
                //Run new target logic here
            }

        }
        private void RunCommand(string arg)
        {
            if (arg == "toggle")
            {
                aim = !aim;
            }
            else if (arg.StartsWith("setvelocity"))
            {
                //If setvel is parsed with any series of numbers, set the projectile velocity to that number
                //loop through string to find the first number
                for (int i = 0; i < arg.Length; i++)
                {
                    if (char.IsDigit(arg[i]))
                    {
                        try
                        {
                            ProjectileVelocity = float.Parse(arg.Substring(i));
                        }
                        catch
                        {
                              Echo("Error parsing velocity, please remove any bad characters");
                        }
                        break;
                    }
                }
            }
        }
        private void RunContinuousLogic()
        {
            //check if the forward reference is valid
            if (forwardReference == null)
            {
                Echo("No controller found, please name a cockpit " + '"' + ForwardReferenceName + '"' + ", or rename the ForwardReferenceName field to something else, and recompile!");
                return;
            }
            if (gyros.Count == 0)
            {
                Echo("No gyros found, please group gyros in the group " + '"' + GyroGroupName + '"' + ", or rename the GyroGroupName field to something else, and recompile!");
                return;
            }
            if (turrets.Count == 0)
            {
                Echo("No turrets found, please group turrets in the group " + '"' + TurretGroupName + '"' + ", or rename the TurretGroupName field to something else, and recompile!");
                return;
            }
            CheckForTargets();
        }


        void CheckForTargets()
        {
            
            bool result;
            MyDetectedEntityInfo target = GetTarget(out result);
            if (result && aim)
            {
                active = true;
                Echo("Fuck shit up, captain!");
                //get reference pos
                MatrixD refOrientation = forwardReference.WorldMatrix;
                Vector3D referencePosition = forwardReference.GetPosition();
                //Offset reference position
                referencePosition += refOrientation.Up * OffsetVert;
                referencePosition += refOrientation.Forward * OffsetCoax;
                referencePosition += refOrientation.Right * OffsetHoriz;
                Vector3D leadPos = GetTargetLeadPosition(target.Position, target.Velocity, referencePosition, forwardReference.CubeGrid.LinearVelocity, ProjectileVelocity, TimeStep);
                Vector3D Error = CalculateGyroValues(leadPos, refOrientation, referencePosition);
                //Get roll commands from the cockpit
                float roll = forwardReference.RollIndicator;

                ApplyGyroValues(Error, refOrientation, roll);
            }
            else
            {

                Echo("All systems nominal");
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

        MyDetectedEntityInfo GetTarget(out bool result)
        {
            foreach (IMyLargeTurretBase turret in turrets)
            {
                if (turret == null)
                {
                    Echo("removing bad turret");
                    turrets.Remove(turret);
                    continue;
                }

                if (turret.HasTarget)
                {
                    result = true;
                    return turret.GetTargetedEntity();
                }
            }
            result = false;
            return new MyDetectedEntityInfo();
        }
        Vector3D GetTargetLeadPosition(Vector3D targetPos, Vector3D targetVel, Vector3D shooterPos, Vector3D shooterVel, float projectileSpeed, double timeStep)
        {
            if (!previousTargetVelocity.IsValid())
            {
                previousTargetVelocity = targetVel;
            }
            Vector3D deltaV = targetVel - previousTargetVelocity;
            Vector3D targetToShooter = shooterPos - targetPos;

            double distanceToTarget = targetToShooter.Length();
            double timeToReachTarget = distanceToTarget / projectileSpeed;

            Vector3D relativeVel = targetVel + deltaV - shooterVel;
            Vector3D aimpointPos = targetPos + relativeVel * timeToReachTarget;



            previousTargetVelocity = targetVel;
            return aimpointPos;
        }

        Vector3D CalculateGyroValues(Vector3D targetPos, MatrixD refOrientation, Vector3D referencePosition)
        {


            Vector3D targetDirection = targetPos - referencePosition;
            Vector3D transformedDirection = VectorMath.SafeNormalize(Vector3D.TransformNormal(targetDirection, MatrixD.Transpose(refOrientation)));

            //Calculate the errors
            double pitchError = Math.Atan2(-transformedDirection.Y, Math.Sqrt(transformedDirection.X * transformedDirection.X + transformedDirection.Z * transformedDirection.Z));
            double yawError = Math.Atan2(transformedDirection.X, transformedDirection.Z);
            yawError = (yawError - Math.Sign(yawError) * Math.PI) * -1;
            double rollError = 0.0;

            //Echo("Pitch Error: " + pitchError.ToString() + "\nYaw Error: " + yawError.ToString());

            //Compensate
            return CompensateError(pitchError, yawError, rollError);

        }

        Vector3D CompensateError(double pitchError, double yawError, double rollError)
        {
            double pitchCompensated1 = l1pitch.Control(pitchError);
            double yawCompensated1 = l1yaw.Control(yawError);
            double rollCompensated1 = l1roll.Control(rollError);
            //Echo("level 1 compensation:");
            //Echo(new Vector3D(pitchCompensated1, yawCompensated1, rollCompensated1).ToString());

            double pitchError2 = pitchCompensated1 - pitchError;
            double yawError2 = yawCompensated1 - yawError;
            double rollError2 = rollCompensated1 - rollError;

            double pitchCompensated2 =l2pitch.Control(pitchError2);
            double yawCompensated2 = l2yaw.Control(yawError2);
            double rollCompensated2 = l2roll.Control(rollError2);
            //Echo("level 2 compensation:");
            //Echo(new Vector3D(pitchCompensated2, yawCompensated2, rollCompensated2).ToString());
            return new Vector3D(pitchCompensated2, yawCompensated2, rollCompensated2);
        }

        //Apply the results
        public void ApplyGyroValues(Vector3D pitchYawRoll, MatrixD refOrientation, float roll)
        {
            pitchYawRoll.Z = roll * rollSensitivityMultiplier;
            Vector3D transformedAngles = Vector3D.TransformNormal(pitchYawRoll, refOrientation);

            foreach (IMyGyro gyro in gyros)
            {
                if (gyro == null)
                {
                    Echo("Removing bad gyro!");
                    gyros.Remove(gyro);
                    continue;
                }
                gyro.GyroOverride = true;
                MatrixD gyroOrientation = MatrixD.Transpose(gyro.WorldMatrix);
                Vector3D gyroRelativeAngles = Vector3D.TransformNormal(transformedAngles, gyroOrientation);

                gyro.Pitch = (float)gyroRelativeAngles.X;
                gyro.Roll = (float)gyroRelativeAngles.Z;
                gyro.Yaw = (float)gyroRelativeAngles.Y;
            }
        }
    }
}

