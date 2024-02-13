using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        
        //Inconspcuous name :D
        string GroupName = "Flight Control";

        

        float ProjectileVelocity = 2000;                //Initialize this if you only have one primary weapon, otherwise run with argument to set velocity
        float ProjectileMaxDist = 2000;                 //Maximum distance to target, used for determining if the ship should fire or not, and if it should approach the target or not
        float TurretVelocity = 500.0f;                  //velocity that the turrets use for turret aim overriding
        float rollSensitivityMultiplier = 1;            //Increase if spinning too slowly, decrease if spi   nning too quickly
        float maxAngular = 30.0f;                       //Max angular velocity in RPM, set to 60 for small gridId and 30 for large gridId (or something less if you wish for slower seek)
        bool predictAcceleration = false;
   
        AimType aimType = AimType.CenterOfMass;         //Valid options are CenterOfMass, TurretAverage, and RandomTurretTarget. Can also be set with argument


        /// COMPLETELY AUTONOMOUS MODE ///
        bool AutonomousMode = true;                     // complete control over the ship
        float autonomousDesiredDistance = 1500;         //Distance to idle at
        double autonomousRollPower = 0.5;               //speed to roll at
        float autonomousFireSigma = 0.9997f;            //how close to on target the ship needs to be to fire
        static int autonomousMinFramesOnTarget = 3;     //minimum frames o be on target before firing

        bool SendEnemyLocation = true;                  //Send enemy location to other ships
        bool ReceiveEnemyLocation = true;               //Receive enemy location from other ships (autonomous required)
        string TaskForceTag = "TaskForceOne";           //Tag use for co ordination between ships, use a different tag if you want co ordination with a different group

        float FriendlyAvoidanceThreshold = 10;          //Distance to stop moving away from friendlies

        //Used for maintaining distance
        float autonomouskP = 0.5f;
        float autonomouskI = 0.0f;
        float autonomouskD = 1f;


        /// PID CONFIG ///
        double kP = 2400;
        double kI = 0;
        double kD = 2400;
        int cascadeCount = 10;
        double cPscaling = 1.0;
        double cIscaling = 2.0;
        double cDscaling = 2.8;

        const double TimeStep = 1.0 / 60.0;

        //offset the forward reference
        float OffsetVert = -5;                          //offset in meters, positive is up
        float OffsetCoax = 0;                           //offset in meters, positive is forward
        float OffsetHoriz = 0;                          //offset in meters, positive is right

        float PassiveRadius = 300;                      //For passive antenna range
        float TransmitRadius = 50000;                   //For transmitting enemy location
        string TransmitMessage = "";
        




        //jump drive config, requires the jump drive API mod (and autonomous mode)
        bool useJumping = true;
        float minDistanceToJump = 10000;


        bool UseRandomTransmitMessage = true;
        const int framesPerTransmitMessage = 1200;
        List<string> splitText = new List<string>();
        string[] TransmitMessages =
        {
            "Do you know who ate all the doughnuts?",
            "Sometimes I dream about cheese.",
            "Why do we all have to wear these ridiculous ties?",

            "America will never fall to Communist invasion.",
            "Commencing tactical assessment. Red Chinese threat detected.",
            "Democracy is non-negotiable.",
            "Engaging Red Chinese aggressors.",
            "Freedom is the sovereign right of every American.",
            "Death is a preferable alternative to Communism.",
            "Chairman Cheng will fail. China will fall.",
            "Communist engaged.",
            "Communist detected on American soil. Lethal force engaged.",
            "Democracy will never be defeated.",
            "Alaska's liberation is imminent.",
            "Engaging Chinese invader.",
            "Communism is a lie.",
            "Initiating directive 7395 -- destroy all Communists.",
            "Tactical assessment: Red Chinese victory... impossible.",
            "Communist target acquired.",
            "Anchorage will be liberated.",
            "Communism is the very definition of failure.",
            "The last domino falls here.",
            "We will not fear the Red Menace.",
            "Communism is a temporary setback on the road to freedom.",
            "Embrace democracy, or you will be eradicated.",
            "Democracy is truth. Communism is death.",
            "Voice module online. Audio functionality test... initialized. Designation: Liberty Prime. Mission: the Liberation of Anchorage, Alaska.",
            "Bzzzt.",
            "Established strategem: Inadequate.",
            "Revised strategy: Initiate photonic resonance overcharge.",
            "Significant obstruction detected. Composition: Titanium alloy supplemented by enhanced photonic resonance barrier.",
            "Obstruction detected. Composition: Titanium alloy supplemented by photonic resonance barrier. Probability of mission hindrance: zero percent.",
            "Obstruction detected. Composition: Titanium alloy supplemented by photonic resonance barrier. Chinese blockade attempt: futile.",
            "Warning: Forcible impact alert. Scanning for Chinese artillery.",
            "Liberty Prime is online. All systems nominal. Weapons hot. Mission: the destruction of any and all Chinese communists.",
            "Catastrophic system failure. Initiating core shutdown as per emergency initiative 2682209. I die so that democracy may live.",
            "Repeat: Red Chinese orbital strike inbound! All U.S Army personnel must vacate the area immediately! Protection protocals engaged!",
            "Warning! Warning! Red Chinese orbital strike imminent! All personnel should reach minimum safe distance immediately!",
            "Satellite Uplink detected. Analysis of Communist transmission pending.",
            "Structural weakness detected. Exploiting.",
            "Communist threat assessment: Minimal. Scanning defenses...",
            "Liberty Prime... back online.",
            "Diagnostic command: accepted.",
            "Desigation: Liberty Prime Mark II. Mission: the liberation of Anchorage, Alaska.",
            "Primary Targets: any and all Red Chinese invaders.",
            "All systems: nominal. Weapons: hot.",
            "Warning: Nuclear weapon payload depleted. Reload required.",
            "Warning: Power Core offline. Running on external power only. Core restart recommended.",
            "Ability to repel Red Chinese invaders: compromised.",
            "Updated tactical assessment: Red Chinese presence detected.",
            "Aerial incursion by Communist forces cannon succeed.",
            "Global positioning initialized. Location: the Commonwealth of Massachusetts. Birthplace of American freedom.",
            "Designation: Liberty Prime. Operational assessment: All systems nominal. Primary directive: War.",
            "Area classified as active warzone. Engaging sentry protocols. Weapons hot.",
            "System diagnostic commencing. Mobility - Complete. Optic beam - fully charged. Nuclear warheads - armed.",
            "Defending Life, Liberty and the pursuit of happiness.",
            "Only together can we stop the spread of communism.",
            "Cultural database accessed. Quoting New England poet Robert Frost: 'Freedom lies in being bold.'",
            "Accessing dictionary database. Entry: democracy. A form of government in which the leader is chosen by vote, and everyone has equal rights.",
            "Accessing dictionary database. Entry: communism. A form of government in which the state controls everything, and people are denied... freedom",
            "I am Liberty Prime. I am America.",
            "Scanners operating at 100% efficiency. Enemy presence detected. Attack imminent.",
            "Mission proceeding as planned.",
            "Defense protocols active. All friendly forces - remain in close proximity.",
            "Democracy is the essence of good. Communism, the very definition of evil.",
            "Freedom is always worth fighting for.",
            "Democracy is freedom. Communism is tyranny.",
            "I hold these truths to be self-evident that all Americans are created... equal. And are endowed with certain unalienable rights",
            "Victory is assured.",
            "American casualties unacceptable. Overkill protocols authorized.",
            "Glory is the reward of valor.",
            "Defeat is not an option.",
            "Commence tactical assessment: Red Chinese threat detected.",
            "Proceeding to target coordinates.",
            "Fusion Core: reinitialized.",
            "Liberty Prime full system analysis.",
            "Hostile software detected. Communist subversion likely.",
            "Targeting... parameters...offline. Re-calibrating...",
            "Red Chinese Infiltration Unit: eliminated. Let freedom ring.",
            "Obstruction: eliminated.",
            "Ground units initiate Directive 7395. Destroy all Communists!",
            "Memorial site: recognized.",
            "Patriotism subroutines: engaged.",
            "Honoring the fallen is the duty of every red-blooded American.",
            "Obstruction detected. Overland travel to target: compromised.",
            "Probability of mission hindrance: thirty-two percent.",
            "Revised stratagem: initiated. Aquatic transit protocol: activated.",
            "Probability of mission hindrance: zero percent.",
            "Democracy is truth. Communism is death. Anchorage will be liberated.",
            "Objective reached.",
            "Scanning defenses.",
            "Scanning results, negative.",
            "Warning: subterranean Red Chinese compound detected.",
            "Obstruction depth: five meters. Composition: sand, gravel and communism.",
            "Tactical assessment: Breach compound to restore democracy.",
            "Warning: all personnel should move to minimum safe distance.",

        };

        //No touchy below >:(
        Vector3D FriendlyAvoidanceVector = Vector3D.Zero;
        string EnemyLocationTag = "EnemyLocation";
        string JumpRequestTag = "JumpRequest";
        string JumpPositionTag = "JumpPosition";
        string CurrentlyAttackingEnemyTag = "CurrentlyAttackingEnemy";
        string CoordinationPositionalDataTag = "CoordinationPositionalData";
        int framesSinceLastTransmitMessage = 0;
        IMyBroadcastListener EnemyLocator;
        IMyBroadcastListener CurrentlyAttackingEnemy;
        IMyBroadcastListener CoordinationPositionalData;
        ClampedIntegralPID forwardBackwardPID;
        bool[] autonomousOnTarget = new bool[autonomousMinFramesOnTarget];

        int maximumLogLength = 20;

        string echoMessage = "";
        bool aim = true;
        bool jumping = false;
        Vector3D jumpPos = Vector3D.Zero;


        Random rng = new Random();
        List<IMyShipController> controllers;
        IMyShipController currentController;
        List<IMyLargeTurretBase> turrets;
        List<IMyTurretControlBlock> turretControllers;
        Dictionary<IMyFunctionalBlock, MyDetectedEntityInfo> turretTargets = new Dictionary<IMyFunctionalBlock, MyDetectedEntityInfo>();
        List<IMyGyro> gyros;
        List<IMyTextPanel> panels;
        List<IMyThrust> allThrusters;
        Thrusters thrusters;
        List<IMyRadioAntenna> antennas;
        //make a list of railguns
        List<IMySmallMissileLauncherReload> railguns;
        Vector3D averageGunPos = Vector3D.Zero;
        List<IMyJumpDrive> jumpDrives;

        static readonly MyDefinitionId ElectricityId = new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), "Electricity");
        const float IdlePowerDraw = 0.0002f;
        const float Epsilon = 1e-6f;


        MyDetectedEntityInfo target;

        Vector3D primaryShipAimPos = Vector3D.Zero;
        bool hasTarget = false;

        ShipAim ShipAim;
        ShipAimUpdate newDetails = new ShipAimUpdate();

        string[] Args =
        {
            "toggle ship aim",
            "set velocity",
            "set aim type",
            "cycle aim type",
            "toggle turret aim",
            "set turret velocity",
            "unfuck turrets",
            "retarget turrets",
            "toggle acceleration lead",
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
            Turrets.program = this;
            GetGroupBlocks();
            InitializeShipAim();
            InitializeTurrets();
            InitializeThrusters();
            InitializeIGC();
            

            Runtime.UpdateFrequency = UpdateFrequency.Update1 | UpdateFrequency.Update100;
            LCDManager.InitializePanels(panels);
            LCDManager.program = this;
            LCDManager.WriteText();
        }

        private void GetGroupBlocks()
        {
            gyros = new List<IMyGyro>();
            turrets = new List<IMyLargeTurretBase>();
            turretControllers = new List<IMyTurretControlBlock>();
            controllers = new List<IMyShipController>();
            panels = new List<IMyTextPanel>();
            allThrusters = new List<IMyThrust>();
            railguns = new List<IMySmallMissileLauncherReload>();
            jumpDrives = new List<IMyJumpDrive>();
            antennas = new List<IMyRadioAntenna>();
            bool groupFound = false;

            var groups = new List<IMyBlockGroup>();
            GridTerminalSystem.GetBlockGroups(groups);

            foreach (IMyBlockGroup group in groups)
            {
                if (group.Name == GroupName)
                {
                    groupFound = true;
                    group.GetBlocksOfType(turrets);
                    group.GetBlocksOfType(turretControllers);
                    group.GetBlocksOfType(gyros);
                    group.GetBlocksOfType(controllers);
                    group.GetBlocksOfType(panels);
                    group.GetBlocksOfType(allThrusters);
                    group.GetBlocksOfType(railguns);
                    group.GetBlocksOfType(antennas);
                    group.GetBlocksOfType(jumpDrives);
                }
            }
            if (!groupFound)
            {
                Runtime.UpdateFrequency = UpdateFrequency.None;

                LCDManager.AddText("No group found, please create a group named \"" + GroupName + "\" and add the required blocks to it, then recompile!");
            }
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
            aimDetails.cPscaling = cPscaling;
            aimDetails.cIscaling = cIscaling;
            aimDetails.cDscaling = cDscaling;
            aimDetails.cascadeCount = cascadeCount;
            aimDetails.autonomousRollPower = autonomousRollPower;
            aimDetails.AutonomousMode = AutonomousMode;
            aimDetails.leadAcceleration = predictAcceleration;
            ShipAim = new ShipAim(aimDetails, gyros);
            forwardBackwardPID = new ClampedIntegralPID(autonomouskP, autonomouskI, autonomouskD, TimeStep, -maxAngular, maxAngular);
            if (controllers.Count > 0)
            {
                currentController = controllers[0];
            }
            foreach (IMyGyro gyro in gyros)
            {
                gyro.GyroOverride = false;
            }
            int spinDirection = rng.Next(0, 2) * 2 - 1;
            autonomousRollPower *= spinDirection;
        }

        private void InitializeTurrets()
        {
            Turrets.TimeStep = TimeStep;
            Turrets.projectileVelocity = TurretVelocity;
        }

        public void InitializeThrusters()
        {
            if (AutonomousMode)
            {

                thrusters = new Thrusters(allThrusters, currentController);
            }
        }

        private void InitializeIGC()
        {
            EnemyLocator = IGC.RegisterBroadcastListener(TaskForceTag + EnemyLocationTag);
            CurrentlyAttackingEnemy = IGC.RegisterBroadcastListener(CurrentlyAttackingEnemyTag);
            CoordinationPositionalData = IGC.RegisterBroadcastListener(CoordinationPositionalDataTag);
        }

        //main loop entrypoint
        public void Main(string argument, UpdateType updateType)
        {

            LCDManager.AddText("Aim Type: " + aimType.ToString());
            if ((updateType & (UpdateType.Trigger | UpdateType.Terminal)) != 0)
            {
                RunCommand(argument);
            }

            if ((updateType & UpdateType.Update1) != 0)
            {
                RunContinuousLogic();

            }
            if ((updateType & UpdateType.Update100) != 0)
            {

            }
            LCDManager.WriteText();
        }
        private void RunCommand(string arg)
        {
            arg = arg.ToLower();
            switch (arg)
            {
                case "toggle ship aim":
                    ToggleShipAim();
                    break;
                case "set velocity":
                    SetProjectileVelocity(arg);
                    break;
                case "set aim type":
                    SetAimType(arg);
                    break;
                case "cycle aim type":
                    CycleAimType();
                    break;
                case "toggle turret aim":
                    ToggleTurretAim();
                    break;
                case "set turret velocity":
                    SetTurretVelocity(arg);
                    break;
                case "unfuck turrets": case "retarget turrets":
                    UnfuckTurrets();
                    break;
                case "toggle acceleration lead":
                    predictAcceleration = !predictAcceleration;
                    Log("Acceleration prediction set to " +  predictAcceleration);
                    break;
                default:
                    string echoMessage = "Invalid argument! Valid arguments are:\n";
                    foreach (string argString in Args)
                    {
                        echoMessage += argString + "\n";
                    }
                    Log(echoMessage);
                    break;
            }
        }

        private void ToggleShipAim()
        {
            aim = !aim;
            Log("Aim set to " + aim.ToString());
        }
        private void SetProjectileVelocity(string arg)
        {
            for (int i = 0; i < arg.Length; i++)
            {
                if (char.IsDigit(arg[i]))
                {
                    try
                    {
                        ProjectileVelocity = float.Parse(arg.Substring(i));
                        Log("Set velocity to " + ProjectileVelocity.ToString());
                    }
                    catch
                    {
                        Log("Error parsing velocity, please remove any bad characters\n");
                        //Echo("Error parsing velocity, please remove any bad characters");
                    }
                    break;
                }
            }
        }
        private void SetAimType(string arg)
        {
            string arge = "set aim type";
            for (int i = arge.Length - 1; i < arg.Length; i++)
            {
                foreach (AimType aimType in Enum.GetValues(typeof(AimType)))
                {
                    if (arg.Substring(i) == aimType.ToString().ToLower())
                    {
                        this.aimType = aimType;
                        Log("Aim type set to " + aimType.ToString());
                        return;
                    }
                }
            }
            Log("Couldn't find aimtype!");
        }

        private void CycleAimType()
        {
            int index = (int)aimType;
            index++;
            if (index >= Enum.GetValues(typeof(AimType)).Length)
            {
                index = 0;
            }
            aimType = (AimType)index;
            Log("Aim type set to " + aimType.ToString());
        }

        private void ToggleTurretAim()
        {
            Turrets.overrideTurretAim = !Turrets.overrideTurretAim;
            Log("Turret override set to " + Turrets.overrideTurretAim.ToString());
        }

        private void SetTurretVelocity(string arg)
        {
            //If setvel is parsed with any series of numbers, set the projectile velocity to that number
            //loop through string to find the first number
            for (int i = 0; i < arg.Length; i++)
            {
                if (char.IsDigit(arg[i]))
                {
                    try
                    {
                        Turrets.projectileVelocity = float.Parse(arg.Substring(i));
                        Log("Set turret velocity to " + Turrets.projectileVelocity.ToString());
                    }
                    catch
                    {
                        Log("Error parsing turret velocity, please remove any bad characters");
                        //Echo("Error parsing velocity, please remove any bad characters");
                    }
                    break;
                }
            }
        }

        private void UnfuckTurrets()
        {
            Helpers.UnfuckTurrets(turrets);
            Log("Attempting to unfuck turrets!");
        }

        private void RunContinuousLogic()
        {
            SetCurrentController();
            Targeting.currentController = currentController;
            turretTargets.Clear();
            GetTurretTargets(turrets, turretControllers, ref turretTargets);
            primaryShipAimPos = GetShipTarget(out hasTarget, ref target, turretTargets);
            
            if (hasTarget)
            {
                if (SendEnemyLocation)
                {
                    //Split transmit message text up into 64 charcter chunks, feed to antennas
                    splitText.Clear();
                    int arrayIndex = -1;
                    for (int i = 0; i < TransmitMessage.Length; i++)
                    {
                        if (i % 50  == 0)
                        {
                            arrayIndex++;
                            splitText.Add("");
                        }
                        splitText[arrayIndex] += TransmitMessage[i];
                    }
                    for (int i = 0; i < antennas.Count; i++)
                    {
                        string text = " ";
                        try { text = splitText[i];}

                        catch { }
                        
                        IMyRadioAntenna antenna = antennas[i];
                        antenna.Radius = TransmitRadius;
                        antenna.HudText = text;
                    }
                    
                    IGC.SendBroadcastMessage<Vector3D>(TaskForceTag + EnemyLocationTag, primaryShipAimPos, TransmissionDistance.TransmissionDistanceMax);


                    var shipIds = new Dictionary<int, long>();
                    int shipsRequestingJumpPositions = 0;
                    for (int i = 0; i < jumpPositionRequests.Count; i++)
                    {
                        MyIGCMessage message = jumpPositionRequests[i];
                        shipsRequestingJumpPositions++;
                        shipIds.Add(i, message.Source);
                    }
                    if (shipsRequestingJumpPositions > 0)
                    {
                        Log(shipsRequestingJumpPositions + " ships requested jump positions, generating battle sphere!");
                    }
                    List<Vector3D> points = FibonacciSphereGenerator.Generate(primaryShipAimPos, autonomousDesiredDistance, shipIds.Count + 1);

                    LCDManager.AddText(points.Count.ToString());
                    
                    for (int i = 0; i < shipIds.Count; i++)
                    {
                        long sendee = shipIds[i];
                        Vector3D position = points[i];
                        IGC.SendUnicastMessage(sendee, TaskForceTag + JumpPositionTag, position);
                    }
                }
            }
            else
            {
                if (!EnemyLocator.HasPendingMessage || jumping == true)
                {
                    foreach (var antenna in antennas)
                    {
                        antenna.Radius = PassiveRadius;
                    }
                }
                
                while (EnemyLocator.HasPendingMessage)
                {
                    MyIGCMessage sender = EnemyLocator.AcceptMessage();
                    if (!hasTarget && ReceiveEnemyLocation && AutonomousMode && sender.Tag == TaskForceTag + EnemyLocationTag)
                    {
                        primaryShipAimPos = (Vector3D)sender.Data;
                        hasTarget = true;
                    }
                    if (jumpDrives.Count > 0 && !jumping && hasTarget && Vector3D.Distance(primaryShipAimPos, currentController.GetPosition()) > minDistanceToJump)
                    {
                        foreach (var antenna in antennas)
                        {
                            antenna.Radius = TransmitRadius;
                        }
                        LCDManager.AddText("Out of range! Requesting jump position...");
                        IGC.SendUnicastMessage(sender.Source, TaskForceTag + JumpRequestTag, "");
                    }
                    else
                    {
                        if (!jumping)
                        {
                            jumpPos = Vector3D.Zero;
                        }
                    }
                }
                foreach (var position in recievedJumpPositions)
                {
                    jumpPos = (Vector3D)position.Data;
                }
            }
            GetIGCMessages();
            UpdateGuns();
            UpdateJumpDrives();
            UpdateShipAim();
            Turrets.UpdateTurretAim(currentController, turretTargets);
            UpdateShipThrust();
            CoordinateAttack();
            UpdateAntennas();
            UpdateLog();
        }
        List<MyIGCMessage> jumpPositionRequests = new List<MyIGCMessage>();
        List<MyIGCMessage> recievedJumpPositions = new List<MyIGCMessage>();
        void GetIGCMessages()
        {
            jumpPositionRequests.Clear();
            recievedJumpPositions.Clear();
            while (IGC.UnicastListener.HasPendingMessage)
            {
                MyIGCMessage newMessage = IGC.UnicastListener.AcceptMessage();
                if (newMessage.Tag == TaskForceTag + JumpPositionTag)
                {
                    recievedJumpPositions.Add(newMessage);
                }
                if (newMessage.Tag == TaskForceTag + JumpRequestTag)
                {
                    jumpPositionRequests.Add(newMessage);
                }
            }
        }
        void UpdateAntennas()
        {
            if (UseRandomTransmitMessage && hasTarget)
            {
                framesSinceLastTransmitMessage++;
                if (framesSinceLastTransmitMessage > framesPerTransmitMessage)
                {
                    framesSinceLastTransmitMessage = 0;

                    int random = rng.Next(0, TransmitMessages.Length);
                    TransmitMessage = TransmitMessages[random];
                }
            }
        }
        IMyJumpDrive primaryJumpDrive;
        void UpdateJumpDrives()
        {
            jumping = false;
            if (!AutonomousMode || !useJumping || !hasTarget || jumpPos == Vector3D.Zero) { return; }
            if (primaryJumpDrive == null)
            {
                SetNewPrimaryJumpDrive();
            }
            if (primaryJumpDrive == null)
            {
                LCDManager.AddText("No jump drives found!");
                return;
            }

            switch (primaryJumpDrive.Status)
            {
                case MyJumpDriveStatus.Jumping:
                    jumping = true;
                    break;
                case MyJumpDriveStatus.Ready:
                    primaryJumpDrive.SetValue<Vector3D?>("ScriptJumpTarget", jumpPos);
                    primaryJumpDrive.ApplyAction("ScriptJump");
                    break;
                case MyJumpDriveStatus.Charging:
                    SetNewPrimaryJumpDrive();
                    if (primaryJumpDrive.Status == MyJumpDriveStatus.Charging)
                    {
                        LCDManager.AddText("No available jump drives found!");
                        return;
                    }
                    break;
            }


            LCDManager.AddText("Preparing to jump to " + jumpPos.ToString() + " with jump drive " + primaryJumpDrive.Name);
            jumping = true; 
            //we want to set the primary aim position to where the enemy will be relative to ourselves after we jump

            Vector3D jumpPosToTargetPos = primaryShipAimPos - jumpPos;
            Vector3D preJumpTargetPos = currentController.GetPosition() + jumpPosToTargetPos;
            primaryShipAimPos = preJumpTargetPos;
            
        }
        List<long> shipsToCoordinateWith = new List<long>();
        List<MyIGCMessage> shipPositions = new List<MyIGCMessage>();

        void SetNewPrimaryJumpDrive()
        {
            foreach (IMyJumpDrive candidate in jumpDrives)
            {
                if (candidate.Status == MyJumpDriveStatus.Charging) { continue; }
                primaryJumpDrive = candidate;
            }
        }
        private void CoordinateAttack()
        {
            Vector3D position = Me.CubeGrid.GetPosition();
            if (hasTarget)
            {
                //We want to transmit on the "public" channel our current target so that we can find co ordinating ships and avoid crashing into them
                IGC.SendBroadcastMessage(CurrentlyAttackingEnemyTag, target.EntityId, TransmissionDistance.TransmissionDistanceMax);
                IGC.SendBroadcastMessage(CoordinationPositionalDataTag, position);
            }
            shipsToCoordinateWith.Clear();
            shipPositions.Clear();
            while (CurrentlyAttackingEnemy.HasPendingMessage)
            {
                MyIGCMessage message = CurrentlyAttackingEnemy.AcceptMessage();
                if ((long)message.Data == target.EntityId)
                {
                    shipsToCoordinateWith.Add(message.Source);
                }
                
            }
            while (CoordinationPositionalData.HasPendingMessage)
            {
                MyIGCMessage message = CoordinationPositionalData.AcceptMessage();
                if (shipsToCoordinateWith.Contains(message.Source))
                {
                    shipPositions.Add(message);
                }
            }
            LCDManager.AddText("Coordinating with " + shipsToCoordinateWith.Count + " other ships");

            //Now that we have a list of ships that could potentially collide
            FriendlyAvoidanceVector = Vector3D.Zero;
            
            foreach (var message in shipPositions)
            {
                if ((Vector3D)message.Data == position)
                {
                    continue;
                }
                Vector3D friendlyPosition = (Vector3D)message.Data;
                Vector3D selfToFriendly = friendlyPosition - position;
                FriendlyAvoidanceVector += Vector3D.Normalize(selfToFriendly) * (float)(1 / selfToFriendly.Length());
            }




        }
        private void UpdateShipAim()
        {
            
            //check if the forward reference is valid
            if (controllers.Count == 0)
            {
                
                LCDManager.AddText("No controller found, please include a controller in the group, and recompile!");
                return;
            }
            if (gyros.Count == 0)
            {
                LCDManager.AddText("No gyros found, please include gyros in the group, and recompile!");
                return;
            }
            if (turrets.Count == 0)
            {
                LCDManager.AddText("No turrets found, please include turrets in the group, and recompile!");
                return;
            }
            LCDManager.AddText("Using current controller: " + currentController.CustomName);
            newDetails.averageGunPos = averageGunPos;
            newDetails.target = target;
            newDetails.aimPos = primaryShipAimPos;
            newDetails.hasTarget = hasTarget;
            newDetails.aim = aim;
            newDetails.currentController = currentController;
            newDetails.ProjectileVelocity = ProjectileVelocity;
            newDetails.leadAcceleration = predictAcceleration;
            LCDManager.AddText("Currently targeted grid: " + newDetails.target.Name);

            ShipAim.CheckForTargets(newDetails);
        }
        void UpdateShipThrust()
        {
            if (AutonomousMode)
            {
                if (hasTarget && !jumping)
                {
                    currentController.DampenersOverride = false;
                    //get the distance to the target, if less than max projectile range, don't thrust up
                    if (Vector3D.Distance(currentController.GetPosition(), primaryShipAimPos) < ProjectileMaxDist)
                    {
                        thrusters.SetThrustInDirection(1, thrusterDir.Up);
                    }
                    else
                    {
                        thrusters.SetThrustInDirection(0, thrusterDir.Up);
                    }
                    //get distance from target
                    float distance = (float)Vector3D.Distance(currentController.GetPosition(), primaryShipAimPos);

                    float error = distance - autonomousDesiredDistance;
                    forwardBackwardPID.Control(error);
                    thrusters.SetThrustInAxis((float)forwardBackwardPID.Value, thrusterAxis.ForwardBackward);

                    float sideThrustMul = Vector3.Dot(currentController.WorldMatrix.Left, FriendlyAvoidanceVector) * 1000;
                    thrusters.SetThrustInAxis(-sideThrustMul, thrusterAxis.LeftRight);
                    float upThrustMul = Vector3.Dot(currentController.WorldMatrix.Up, FriendlyAvoidanceVector) * 1000;
                    thrusters.SetThrustInDirection(upThrustMul, thrusterDir.Down);

                }
                else
                {
                    currentController.DampenersOverride = true;
                    thrusters.SetThrustInDirection(0, thrusterDir.Up);
                    thrusters.SetThrustInDirection(0, thrusterDir.Down);
                    thrusters.SetThrustInDirection(0, thrusterDir.Left);
                    thrusters.SetThrustInDirection(0, thrusterDir.Right);
                    thrusters.SetThrustInDirection(0, thrusterDir.Forward);
                    thrusters.SetThrustInDirection(0, thrusterDir.Backward);
                }

                
            }
            
        }
        List<IMySmallMissileLauncherReload> tempRailguns = new List<IMySmallMissileLauncherReload>();
        void UpdateGuns()
        {
            int activeRailguns = 0;
            averageGunPos = Vector3D.Zero;
            tempRailguns.Clear();
            foreach (IMySmallMissileLauncherReload railgun in railguns)
            {
                tempRailguns.Add(railgun);
            }
            foreach (IMySmallMissileLauncherReload railgun in tempRailguns)
            {
                if (railgun == null || railgun.Closed)
                {
                    railguns.Remove(railgun);
                    continue;
                }
                if (railgun.Components.Get<MyResourceSinkComponent>().MaxRequiredInputByType(ElectricityId) < (IdlePowerDraw + Epsilon) && railgun.Enabled && railgun.IsFunctional)
                {
                    averageGunPos += railgun.GetPosition();
                    activeRailguns++;
                }


            }
            if (activeRailguns != 0)
            {
                averageGunPos /= activeRailguns;
            }
            LCDManager.AddText("Active railguns: " + activeRailguns.ToString());


            if (AutonomousMode && !jumping)
            {
                //get a vector from the ship to the target
                Vector3D shipToTarget = (primaryShipAimPos - currentController.GetPosition());
                
                Vector3D shipToTargetNormal = Vector3D.Normalize(shipToTarget);
                Vector3D forward = currentController.WorldMatrix.Forward;
                bool onTarget = Vector3D.Dot(shipToTargetNormal, forward) > autonomousFireSigma;
                
                //remove the oldest value
                for (int i = 0; i < autonomousOnTarget.Length - 1; i++)
                {
                    autonomousOnTarget[i] = autonomousOnTarget[i + 1];
                }
                //add the new value
                autonomousOnTarget[autonomousOnTarget.Length - 1] = onTarget;

                //check if all values are true
                onTarget = true;
                foreach (bool value in autonomousOnTarget)
                {
                    if (!value)
                    {
                        onTarget = false;
                    }
                }
                if (onTarget && shipToTarget.Length() < ProjectileMaxDist)
                {
                    LCDManager.AddText("Firing!");
                    tempRailguns.Clear();
                    foreach (IMySmallMissileLauncherReload railgun in railguns)
                    {
                        tempRailguns.Add(railgun);
                    }
                    foreach (IMySmallMissileLauncherReload railgun in tempRailguns)
                    {
                        if (railgun == null)
                        {
                            railguns.Remove(railgun);
                            continue;
                        }
                        railgun.Shoot = true;
                    }
                }
                else
                {
                    tempRailguns.Clear();
                    foreach (IMySmallMissileLauncherReload railgun in railguns)
                    {
                        tempRailguns.Add(railgun);
                    }
                    foreach (IMySmallMissileLauncherReload railgun in tempRailguns)
                    {
                        if (railgun == null)
                        {
                            railguns.Remove(railgun);
                            continue;
                        }
                        railgun.Shoot = false;
                    }
                }
            }
            
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

        
        void GetTurretTargets(List<IMyLargeTurretBase> turrets, List<IMyTurretControlBlock> turretControllers, ref Dictionary<IMyFunctionalBlock, MyDetectedEntityInfo> targets)
        {
            //put in separate for loop for efficiency or something
            foreach(IMyLargeTurretBase turret in turrets)
            {
                if (turret == null)
                {
                    Log("removing bad turret " + turret.CustomName);
                    turrets.Remove(turret);
                    continue;
                }
            }

            foreach (IMyTurretControlBlock turret in turretControllers)
            {
                if (turret == null)
                {
                    Log("removing bad turret " + turret.CustomName);
                    turretControllers.Remove(turret);
                    continue;
                }
            }

            targets.Clear();
            
            foreach(IMyLargeTurretBase turret in turrets)
            {
                targets.Add(turret, turret.GetTargetedEntity());
            }

            foreach (IMyTurretControlBlock turret in turretControllers)
            {
                targets.Add(turret, turret.GetTargetedEntity());
            }
        }

        //Rework to:
        //Check the target of every turret, pick the most targeted target
        //Get the block target of each turret on that target
        //Evaluate the block target positions, and return a position that roughly represents a cluster of blocks
        //Or perhaps just average all the target positions?
        Dictionary<MyDetectedEntityInfo, int> detectionCount = new Dictionary<MyDetectedEntityInfo, int>();
        Vector3D GetShipTarget(out bool result, ref MyDetectedEntityInfo currentTarget, Dictionary<IMyFunctionalBlock, MyDetectedEntityInfo> targets)
        {
            result = false;
            //must declare since readonly
            MyDetectedEntityInfo finalTarget = new MyDetectedEntityInfo();


            detectionCount.Clear();
            foreach (KeyValuePair<IMyFunctionalBlock, MyDetectedEntityInfo> pair in targets)
            {

                if (aimType == AimType.CenterOfMass)
                {
                    if (pair.Value.EntityId == currentTarget.EntityId)
                    {
                        currentTarget = pair.Value;
                    }
                }
                IMyFunctionalBlock turret = pair.Key;
                MyDetectedEntityInfo target = pair.Value;

                if (FunctionalTurretHasTarget(turret))
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
        List<Vector3D> aimpoints = new List<Vector3D>();
        Vector3D AverageTurretTarget(MyDetectedEntityInfo target, Dictionary<IMyFunctionalBlock, MyDetectedEntityInfo> turrets)
        {
            aimpoints.Clear();
            foreach (KeyValuePair<IMyFunctionalBlock, MyDetectedEntityInfo> pair in turrets)
            {
                IMyFunctionalBlock turret = pair.Key;
                MyDetectedEntityInfo turretTarget = pair.Value;
                if (turretTarget.EntityId == target.EntityId)
                {
                    if (turretTarget.HitPosition != null)
                    {
                        aimpoints.Add((Vector3D)turretTarget.HitPosition);
                    }
                }
            }

            return Helpers.AverageVectorList(aimpoints);

        }

        bool FunctionalTurretHasTarget(IMyFunctionalBlock turret)
        {
            if (turret is IMyLargeTurretBase)
            {
                IMyLargeTurretBase turret2 = (IMyLargeTurretBase)turret;
                return turret2.HasTarget;
            }

            if (turret is IMyTurretControlBlock)
            {
                IMyTurretControlBlock turret2 = (IMyTurretControlBlock)turret;
                return turret2.HasTarget;
            }
            return false;
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

            LCDManager.AddText("\n" + echoMessage);
        }

        private void Log(string toAdd)
        {
            echoMessage = toAdd + "\n" + echoMessage;
        }



    }
}

