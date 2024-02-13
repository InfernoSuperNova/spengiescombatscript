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

        public Thrusters(List<IMyThrust> allThrust, IMyShipController currentController)
        {
            upThrust = new List<IMyThrust>();
            downThrust = new List<IMyThrust>();
            leftThrust = new List<IMyThrust>();
            rightThrust = new List<IMyThrust>();
            forwardThrust = new List<IMyThrust>();
            backwardThrust = new List<IMyThrust>();

            foreach (var thruster in allThrust)
            {
                //compare the thruster direction to the controller direction

                if (thruster.WorldMatrix.Forward == -currentController.WorldMatrix.Forward)
                {
                    forwardThrust.Add(thruster);
                }
                else if (thruster.WorldMatrix.Forward == currentController.WorldMatrix.Forward)
                {
                    backwardThrust.Add(thruster);
                }
                else if (thruster.WorldMatrix.Forward == -currentController.WorldMatrix.Left)
                {
                    leftThrust.Add(thruster);
                }
                else if (thruster.WorldMatrix.Forward == currentController.WorldMatrix.Left)
                {
                    rightThrust.Add(thruster);
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

        public void SetThrustInDirection(float thrust, thrusterDir dir)
        {
            var actualList = GetThrusterDir(dir);
            IMyThrust[] thrusters = new IMyThrust[actualList.Count];
            actualList.CopyTo(thrusters);
            foreach (var thruster in thrusters)
            {
                if (thruster == null)
                {
                    actualList.Remove(thruster);
                    continue;
                }
                thruster.ThrustOverridePercentage = thrust;
            }
        }
        private thrusterDir GetThrusterDirFromAxis(thrusterAxis axis, int sign)
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
            var sign = Math.Sign(thrust);
            thrusterDir mainThrust = GetThrusterDirFromAxis(axis, sign);
            thrusterDir oppositeThrust = GetThrusterDirFromAxis(axis, -sign);
            SetThrustInDirection(Math.Abs(thrust), mainThrust);
            SetThrustInDirection(0, oppositeThrust);
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
