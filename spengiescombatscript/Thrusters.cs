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
    internal class Thrusters
    {
        public List<IMyThrust> upThrust;
        public List<IMyThrust> downThrust;
        public List<IMyThrust> leftThrust;
        public List<IMyThrust> rightThrust;
        public List<IMyThrust> forwardThrust;
        public List<IMyThrust> backwardThrust;

        public List<IMyGravityGenerator> upGravityGen;
        public List<IMyGravityGenerator> downGravityGen;
        public List<IMyGravityGenerator> leftGravityGen;    
        public List<IMyGravityGenerator> rightGravityGen;
        public List<IMyGravityGenerator> forwardGravityGen;
        public List<IMyGravityGenerator> backwardGravityGen;

        MyGridProgram program;
        
        public Thrusters(List<IMyThrust> allThrust, IMyShipController currentController, List<IMyGravityGenerator> allGravity, MyGridProgram program)
        {
            upThrust = new List<IMyThrust>();
            downThrust = new List<IMyThrust>();
            leftThrust = new List<IMyThrust>();
            rightThrust = new List<IMyThrust>();
            forwardThrust = new List<IMyThrust>();
            backwardThrust = new List<IMyThrust>();

            upGravityGen = new List<IMyGravityGenerator>();
            downGravityGen = new List<IMyGravityGenerator>();
            leftGravityGen = new List<IMyGravityGenerator>();
            rightGravityGen = new List<IMyGravityGenerator>();
            forwardGravityGen = new List<IMyGravityGenerator>();
            backwardGravityGen = new List<IMyGravityGenerator>();

            this.program = program;

            foreach (var thruster in allThrust)
            {
                //compare the thruster direction to the controller direction

                if (thruster.WorldMatrix.Forward == -currentController.WorldMatrix.Forward)
                {
                    backwardThrust.Add(thruster);
                }
                else if (thruster.WorldMatrix.Forward == currentController.WorldMatrix.Forward)
                {
                    forwardThrust.Add(thruster);
                }
                else if (thruster.WorldMatrix.Forward == -currentController.WorldMatrix.Left)
                {
                    rightThrust.Add(thruster);
                }
                else if (thruster.WorldMatrix.Forward == currentController.WorldMatrix.Left)
                {
                    leftThrust.Add(thruster);
                }
                else if (thruster.WorldMatrix.Forward == -currentController.WorldMatrix.Up)
                {
                    upThrust.Add(thruster);
                }
                else if (thruster.WorldMatrix.Forward == currentController.WorldMatrix.Up)
                {
                    downThrust.Add(thruster);
                }
                //there's probably a better solution lol
            }

            foreach (var grav in allGravity)
            {
                if (grav.WorldMatrix.Up == -currentController.WorldMatrix.Forward)
                {
                    backwardGravityGen.Add(grav);
                }
                else if (grav.WorldMatrix.Up == currentController.WorldMatrix.Forward)
                {
                    forwardGravityGen.Add(grav);
                }
                else if (grav.WorldMatrix.Up == -currentController.WorldMatrix.Left)
                {
                    rightGravityGen.Add(grav);
                }
                else if (grav.WorldMatrix.Up == currentController.WorldMatrix.Left)
                {
                    leftGravityGen.Add(grav);
                }
                else if (grav.WorldMatrix.Up == -currentController.WorldMatrix.Up)
                {
                    upGravityGen.Add(grav);
                }
                else if (grav.WorldMatrix.Up == currentController.WorldMatrix.Up)
                {
                    downGravityGen.Add(grav);
                }
            }
        }
        private List<IMyThrust> GetThrusterDir(thrusterDir dir)
        {
            if (dir == thrusterDir.Up)
            {
                return upThrust;
            }
            else if (dir == thrusterDir.Down)
            {
                return downThrust;
            }
            else if (dir == thrusterDir.Left)
            {
                return leftThrust;
            }
            else if (dir == thrusterDir.Right)
            {
                return rightThrust;
            }
            else if (dir == thrusterDir.Forward)
            {
                return forwardThrust;
            }
            else if (dir == thrusterDir.Backward)
            {
                return backwardThrust;
            }
            return null;
        }

        private List<IMyGravityGenerator> GetGravityDir(thrusterDir dir)
        {
            if (dir == thrusterDir.Up)
            {
                return upGravityGen;
            }
            else if (dir == thrusterDir.Down)
            {
                return downGravityGen;
            }
            else if (dir == thrusterDir.Left)
            {
                return leftGravityGen;
            }
            else if (dir == thrusterDir.Right)
            {
                return rightGravityGen;
            }
            else if (dir == thrusterDir.Forward)
            {
                return forwardGravityGen;
            }
            else if (dir == thrusterDir.Backward)
            {
                return backwardGravityGen;
            }
            return null;
        }

        public void SetThrustInDirection(float thrust, thrusterDir dir)
        {
            
            var thrusters = GetThrusterDir(dir);
            for (int i = thrusters.Count - 1; i >= 0; i--)
            {
                IMyThrust thruster = thrusters[i];
                if (thruster == null)
                {
                    thrusters.Remove(thruster);
                    continue;
                }
                thruster.ThrustOverridePercentage = thrust;
            }
        }

        public void SetGravityThrustInDirection(float thrust, thrusterDir dir)
        {
            var thrusters = GetGravityDir(dir);
            for (int i = thrusters.Count - 1; i >= 0; i--)
            {
                IMyGravityGenerator thruster = thrusters[i];
                if (thruster == null)
                {
                    thrusters.Remove(thruster);
                    continue;
                }
                thruster.GravityAcceleration = (float)(thrust * 9.81);
            }
        }

        private thrusterDir GetOppositeDir(thrusterDir dir)
        {
            if (dir == thrusterDir.Up)
            {
                return thrusterDir.Down;
            }
            else if (dir == thrusterDir.Down)
            {
                return thrusterDir.Up;
            }
            else if (dir == thrusterDir.Left)
            {
                return thrusterDir.Right;
            }
            else if (dir == thrusterDir.Right)
            {
                return thrusterDir.Left;
            }
            else if (dir == thrusterDir.Forward)
            {
                return thrusterDir.Backward;
            }
            else if (dir == thrusterDir.Backward)
            {
                return thrusterDir.Forward;
            }
            return thrusterDir.Up;
        }
        private thrusterDir GetThrusterDirFromAxis(thrusterAxis axis, float sign)
        {
            switch (axis)
            {
                case thrusterAxis.UpDown:
                    if (sign > 0)
                    {
                        return thrusterDir.Up;
                    }
                    else
                    {
                        return thrusterDir.Down;
                    }
                case thrusterAxis.LeftRight:
                    if (sign > 0)
                    {
                        return thrusterDir.Left;
                    }
                    else
                    {
                        return thrusterDir.Right;
                    }
                case thrusterAxis.ForwardBackward:
                    if (sign > 0)
                    {
                        return thrusterDir.Forward;
                    }
                    else
                    {
                        return thrusterDir.Backward;
                    }
            }
            return thrusterDir.Up;
        }


        public void SetThrustInAxis(float thrust, thrusterAxis axis)
        {
            if (thrust == 0)
            {
                thrusterDir mainThrustEscape = GetThrusterDirFromAxis(axis, 1);
                thrusterDir oppositeThrustEscape = GetThrusterDirFromAxis(axis, -1);

                SetThrustInDirection(0, mainThrustEscape);
                SetThrustInDirection(0, oppositeThrustEscape);

                SetGravityThrustInDirection(0, mainThrustEscape);
                SetGravityThrustInDirection(0, oppositeThrustEscape);

                return;
            }
            int thrustSign = Math.Sign(thrust);
            thrusterDir mainThrust = GetThrusterDirFromAxis(axis, thrust);
            thrusterDir oppositeThrust = GetThrusterDirFromAxis(axis, -thrust);
            float mainThrustValue = MathHelper.Clamp(Math.Abs(thrust), 0.01f, 1);
            float oppositeThrustValue = MathHelper.Clamp(-Math.Abs(thrust), 0.01f, 1);
            SetThrustInDirection(mainThrustValue, mainThrust);
            SetThrustInDirection(oppositeThrustValue, oppositeThrust);
            SetGravityThrustInAxis(thrust, axis);

        }

        private void SetGravityThrustInAxis(float thrust, thrusterAxis axis)
        {
            SetGravityThrustInDirection(thrust, GetThrusterDirFromAxis(axis, 1));
            SetGravityThrustInDirection(-thrust, GetThrusterDirFromAxis(axis, -1));
        }


        public void SetNeutralGravity()
        {
            if (upGravityGen.Count > 0)
            {
                upGravityGen[0].GravityAcceleration = -100;
                upGravityGen[0].Enabled = true;
                return;
            }
            if (downGravityGen.Count > 0)
            {
                downGravityGen[0].GravityAcceleration = 100;
                downGravityGen[0].Enabled = true;
                return;
            }
        }
        public void ResetGravityThrust()
        {
            foreach (var grav in upGravityGen)
            {
                grav.GravityAcceleration = 0;
            }
            foreach (var grav in downGravityGen)
            {
                grav.GravityAcceleration = 0;
            }
            foreach (var grav in leftGravityGen)
            {
                grav.GravityAcceleration = 0;
            }
            foreach (var grav in rightGravityGen)
            {
                grav.GravityAcceleration = 0;
            }
            foreach (var grav in forwardGravityGen)
            {
                grav.GravityAcceleration = 0;
            }
            foreach (var grav in backwardGravityGen)
            {
                grav.GravityAcceleration = 0;
            }
        }
    }
    public enum thrusterDir
    {
        Up,
        Down,
        Left,
        Right,
        Forward,
        Backward

    }

    public enum thrusterAxis
    {
        UpDown,
        LeftRight,
        ForwardBackward
    }
}
