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
        
        //Inconspcuous name :D
        string GroupName = "Flight Control";


        //offset the forward reference
        float OffsetVert = -5; //offset in meters, positive is up
        float OffsetCoax = 0; //offset in meters, positive is forward
        float OffsetHoriz = 0; //offset in meters, positive is right


        float maxAngular = 30.0f; //Max angular velocity in RPM, set to 60 for small grid and 30 for large grid (or something less if you wish for slower seek)
        float ProjectileVelocity = 2000.0f; //Initialize this if you only have one primary weapon, otherwise run with argument to set velocity
        float rollSensitivityMultiplier = 1; //Increase if spinning too slowly, decrease if spi   nning too quickly

        AimType aimType = AimType.TurretAverage; //Valid options are CenterOfMass, TurretAverage, and RandomTurretTarget. Can also be set with argument
    
        //PID CONFIG
        //Ask Gxaps for help if you don't know how to tune

        //Level one PID, for getting to the target
        float kP = 40.0f;
        float kI = 0.0f;
        float kD = 15.0f;

        const double TimeStep = 1.0 / 60.0;




        //No touchy below >:(

        int maximumLogLength = 20;

        string echoMessage = "";
        bool aim = true;


        List<IMyShipController> controllers;
        IMyShipController currentController;
        List<IMyLargeTurretBase> turrets;
        Dictionary<IMyLargeTurretBase, MyDetectedEntityInfo> turretTargets;
        List<IMyGyro> gyros;



        MyDetectedEntityInfo target;
        Vector3D primaryShipAimPos = Vector3D.Zero;
        bool hasTarget = false;

        ShipAim ShipAim;

        string[] Args =
        {
            "toggle ship aim",
            "set velocity",
            "set aim type",
            "cycle aim type",
        };
        public enum AimType
        {
            CenterOfMass, //Useful for maneuverable and small targets
            TurretAverage, //Useful for large targets
            RandomTurretTarget //Useful for strike runs on large targets, or sniping reactors and other critical components
        }

        public Program()
        {
            
            Targeting.program = this;
            InitializeShipAim();
        }



        private void InitializeShipAim()
        {
            ShipAimConfig aimDetails = new ShipAimConfig();
            aimDetails.program = this;
            aimDetails.OffsetVert = OffsetVert;
            aimDetails.OffsetCoax = OffsetCoax;
            aimDetails.OffsetHoriz = OffsetHoriz;
            aimDetails.rollSensitivityMultiplier = rollSensitivityMultiplier;
            aimDetails.maxAngular = maxAngular;
            aimDetails.TimeStep = TimeStep;
            aimDetails.kP = kP;
            aimDetails.kI = kI;
            aimDetails.kD = kD;

            Runtime.UpdateFrequency = UpdateFrequency.Update1 | UpdateFrequency.Update100;


            List<IMyBlockGroup> groups = new List<IMyBlockGroup>();

            GridTerminalSystem.GetBlockGroups(groups);

            gyros = new List<IMyGyro>();
            turrets = new List<IMyLargeTurretBase>();
            controllers = new List<IMyShipController>();

            bool groupFound = false;
            foreach (IMyBlockGroup group in groups)
            {
                if (group.Name == GroupName)
                {
                    groupFound = true;
                    group.GetBlocksOfType(turrets);
                    group.GetBlocksOfType(gyros);
                    group.GetBlocksOfType(controllers);
                }
            }
            ShipAim = new ShipAim(aimDetails, gyros);
            if (!groupFound)
            {
                Runtime.UpdateFrequency = UpdateFrequency.None;
                Echo("No group found, please create a group named \"" + GroupName + "\" and add the required blocks to it, then recompile!");
            }
            if (controllers.Count > 0)
            {
                currentController = controllers[0];
            }

            foreach (IMyGyro gyro in gyros)
            {
                gyro.GyroOverride = false;
            }
        }

        public void Main(string argument, UpdateType updateType)
        {
            
            Echo("Aim type: " + aimType.ToString());
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
                
            }
            UpdateLog();
        }

        private void UpdateLog()
        {
            //Clear old lines from the log by counting \n characters
            int lineCount = 0;
            for (int i = 0; i < echoMessage.Length; i++)
            {
                if (echoMessage[i] == '\n')
                {
                    lineCount++;
                }
            }

            if (lineCount > maximumLogLength)
            {
                int index = echoMessage.LastIndexOf('\n');
                echoMessage = echoMessage.Remove(index);
            }
            
            Echo("\n" + echoMessage);
        }
        private void RunCommand(string arg)
        {
            arg = arg.ToLower();
            if (arg == Args[0])
            {
                
                aim = !aim;
                echoMessage = "aim set to " + aim.ToString() + "\n" + echoMessage;
            }
            else if (arg.StartsWith(Args[1]))
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
                            echoMessage = "Set velocity to " + ProjectileVelocity.ToString() + "\n" + echoMessage;
                        }
                        catch
                        {
                            echoMessage = "Error parsing velocity, please remove any bad characters\n" + echoMessage;
                              //Echo("Error parsing velocity, please remove any bad characters");
                        }
                        break;
                    }
                }
            }
            //set aim type
            else if (arg.StartsWith(Args[2]))
            {
                for (int i = Args[2].Length - 1; i < arg.Length; i++)
                {
                    foreach (AimType aimType in Enum.GetValues(typeof(AimType)))
                    {
                        if (arg.Substring(i) == aimType.ToString().ToLower())
                        {
                            this.aimType = aimType;
                            echoMessage = "Aim type set to " + aimType.ToString() + "\n" + echoMessage;
                            return;
                        }
                    }
                }
                echoMessage = "Couldn't find aimtype!\n" + echoMessage;
            }
            //cycle aim type
            else if (arg.StartsWith(Args[3]))
            {
                  int index = (int)aimType;
                index++;
                if (index >= Enum.GetValues(typeof(AimType)).Length)
                {
                    index = 0;
                }
                aimType = (AimType)index;
                echoMessage = "Aim type set to " + aimType.ToString() + "\n" + echoMessage;
            }
            else
            {
                string echoMessage = "Invalid argument! Valid arguments are:\n";
                foreach (string argString in Args)
                {
                    echoMessage += argString + "\n";
                }
                this.echoMessage = echoMessage + this.echoMessage;
            }
        }
        private void RunContinuousLogic()
        {
            
            SetCurrentController();
            Targeting.currentController = currentController;
            GetTurretTargets(turrets, out turretTargets);
            primaryShipAimPos = GetShipTarget(out hasTarget, ref target, turretTargets);
            UpdateShipAim();
            
        }
        private void UpdateShipAim()
        {
            
            //check if the forward reference is valid
            if (controllers.Count == 0)
            {
                Echo("No controller found, please include a controller in the group, and recompile!");
                return;
            }
            if (gyros.Count == 0)
            {
                Echo("No gyros found, please include gyros in the group, and recompile!");
                return;
            }
            if (turrets.Count == 0)
            {
                Echo("No turrets found, please include turrets in the group, and recompile!");
                return;
            }
            Echo("Using current controller: " + currentController.CustomName);
            ShipAimUpdate newDetails = new ShipAimUpdate();
            newDetails.target = target;
            newDetails.aimPos = primaryShipAimPos;
            newDetails.hasTarget = hasTarget;
            newDetails.aim = aim;
            newDetails.currentController = currentController;
            newDetails.ProjectileVelocity = ProjectileVelocity;
            Echo("Currently targeted grid: " + newDetails.target.Name);

            ShipAim.CheckForTargets(newDetails);
        }
        void SetCurrentController()
        {
            foreach(IMyShipController controller in controllers)
            {
                //do a validity check
                if (controller == null)
                {
                    controllers.Remove(controller);

                }
            }
            foreach(IMyShipController controller in controllers)
            {
                if (controller.IsUnderControl && controller.CanControlShip)
                {
                    currentController = controller;
                    return;
                }
            }
            try
            {
                currentController = controllers[0];
            }
            catch
            {
                //no controllers
            }
        }

        
        void GetTurretTargets(List<IMyLargeTurretBase> turrets, out Dictionary<IMyLargeTurretBase, MyDetectedEntityInfo> targets)
        {
            //put in separate for loop for efficiency or something
            foreach(IMyLargeTurretBase turret in turrets)
            {
                if (turret.Closed)
                {
                    echoMessage = "removing bad turret " + turret.CustomName + "\n" + echoMessage ;
                    turrets.Remove(turret);
                    continue;
                }
            }

            targets = new Dictionary<IMyLargeTurretBase, MyDetectedEntityInfo>();
            foreach(IMyLargeTurretBase turret in turrets)
            {
                targets.Add(turret, turret.GetTargetedEntity());
            }
        }

        //Rework to:
        //Check the target of every turret, pick the most targeted target
        //Get the block target of each turret on that target
        //Evaluate the block target positions, and return a position that roughly represents a cluster of blocks
        //Or perhaps just average all the target positions?
        Vector3D GetShipTarget(out bool result, ref MyDetectedEntityInfo currentTarget, Dictionary<IMyLargeTurretBase, MyDetectedEntityInfo> targets)
        {
            result = false;
            MyDetectedEntityInfo finalTarget = new MyDetectedEntityInfo();

            Dictionary<MyDetectedEntityInfo, int> detectionCount = new Dictionary<MyDetectedEntityInfo, int>();
            foreach (KeyValuePair<IMyLargeTurretBase, MyDetectedEntityInfo> pair in targets)
            {

                if (aimType == AimType.CenterOfMass)
                {

                }
                IMyLargeTurretBase turret = pair.Key;
                MyDetectedEntityInfo target = pair.Value;
                if (turret.HasTarget)
                {
                    
                    if (target.EntityId == currentTarget.EntityId)
                    {
                        switch (aimType)
                        {
                            case AimType.CenterOfMass:
                                result = true;
                                return target.Position;
                            case AimType.TurretAverage:
                                break;
                            case AimType.RandomTurretTarget:
                                if (target.HitPosition != null)
                                {
                                    result = true;
                                    return (Vector3D)target.HitPosition;
                                }
                                break;
                        }
                        result = true;
                        finalTarget = target;
                    }
                    if (detectionCount.ContainsKey(target))
                    {
                        detectionCount[target]++;
                    }
                    else
                    {
                        detectionCount.Add(target, 1);
                    }
                    
                }
            }
            //If the current target couldn't be found, then find the most detected target
            
            if (!result)
            {
                int max = 0;
                foreach (KeyValuePair<MyDetectedEntityInfo, int> pair in detectionCount)
                {
                    if (pair.Value > max)
                    {
                        result = true;
                        max = pair.Value;
                        finalTarget = pair.Key;
                    }
                }
            }

            
            if (result)
            {

                currentTarget = finalTarget;
                //Get the average position of the turret targets
                return AverageTurretTarget(finalTarget, targets);
            }
            else
            {
                return Vector3D.Zero;
            }    
        }
        
        Vector3D AverageTurretTarget(MyDetectedEntityInfo target, Dictionary<IMyLargeTurretBase, MyDetectedEntityInfo> turrets)
        {
            List<Vector3D> aimpoints = new List<Vector3D>();
            foreach (KeyValuePair<IMyLargeTurretBase, MyDetectedEntityInfo> pair in turrets)
            {
                IMyLargeTurretBase turret = pair.Key;
                MyDetectedEntityInfo turretTarget = pair.Value;
                if (turretTarget.EntityId == target.EntityId)
                {
                    if (turretTarget.HitPosition != null)
                    {
                        aimpoints.Add((Vector3D)turretTarget.HitPosition);
                    }
                }
            }

            return AverageVectorList(aimpoints);

        }


        private Vector3D AverageVectorList(List<Vector3D> vectors)
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

