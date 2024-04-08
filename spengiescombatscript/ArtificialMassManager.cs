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
    internal class ArtificialMassManager
    {
        private List<IMyArtificialMassBlock> massBlocks;

        private Dictionary<IMyArtificialMassBlock, bool> enabled;

        private List<IMyGravityGenerator> gravityGenerators;

        private IMyGridTerminalSystem GridTerminalSystem;

        private MyGridProgram program;

        private int updateDelay = 10; // ticks
        public ArtificialMassManager(List<IMyArtificialMassBlock> massBlocks, IMyGridTerminalSystem GridTerminalSystem, MyGridProgram program, List<IMyGravityGenerator> gravityGenerators)
        {
            this.massBlocks = massBlocks;
            this.GridTerminalSystem = GridTerminalSystem;
            this.program = program;
            this.gravityGenerators = gravityGenerators;
            enabled = new Dictionary<IMyArtificialMassBlock, bool>();
            foreach (var block in massBlocks)
            {
                enabled[block] = true;
            }
        }
        int frame = 0;

        bool state = true;
        public void Update(Vector3 target, bool hasTarget, bool isAutonomous, Vector3 moveIndicator)
        {
            if (massBlocks.Count == 0)
            {
                return;
            }

            if (!(isAutonomous && hasTarget) && !(moveIndicator.Length() > 0))
            {
                if (state)
                {
                    DisableGravityDrive();
                    state = false;
                }
            }
            else
            {
                if (!state)
                {
                    EnableGravityDrive();
                    state = true;
                }
            }



            
            frame++;
            LCDManager.AddText(frame.ToString());
            if (frame > updateDelay)
            {

                frame = 0;
                for (int i = massBlocks.Count - 1; i >= 0; i--)
                {
                    IMyArtificialMassBlock massBlock = massBlocks[i];
                    if (massBlock.Closed || !GridTerminalSystem.CanAccess(massBlock))
                    {
                        massBlocks.Remove(massBlock);
                        enabled.Remove(massBlock);
                    }
                }

                AdjustCenterOfMass(massBlocks, target);

            }
        }
        private void AdjustCenterOfMass(List<IMyArtificialMassBlock> massBlocks, Vector3 centerOfMass)
        {
            double totalMass = 0;
            Vector3 artificialCenterOfMass = Vector3.Zero;

            // Calculate total mass and current center of mass
            foreach (var block in massBlocks)
            {
                if (block.IsWorking)
                {
                    totalMass += block.VirtualMass;
                    artificialCenterOfMass += block.WorldMatrix.Translation * (float)block.VirtualMass;
                }
            }

            artificialCenterOfMass /= (float)totalMass;

            // Calculate adjustment needed
            Vector3 adjustment = centerOfMass - artificialCenterOfMass;
            //LCDManager.AddText("length:" + adjustment.Length().ToString());
            //if (adjustment.Length() < 2.5)
            //{
            //    return;
            //}
            // Adjust the artificial mass blocks
            bool applicableClosestBlock = false;
            IMyArtificialMassBlock closestBlock = massBlocks[0];
            float closestDistance = float.MaxValue;

            bool applicableFurthestBlock = false;
            IMyArtificialMassBlock furthestBlock = massBlocks[0];
            float furthestDistance = float.MinValue;
            foreach (var block in massBlocks)
            {
                Vector3 blockPosition = block.WorldMatrix.Translation;
                Vector3 blockAdjustment = blockPosition - artificialCenterOfMass;

                float dot = Vector3.Dot(blockAdjustment, adjustment);
                if (dot < closestDistance && dot < 0 && block.Enabled)
                {
                    closestDistance = dot;
                    closestBlock = block;
                    applicableClosestBlock = true;
                }
                if (dot > furthestDistance && dot > 0 && !block.Enabled)
                {
                    furthestDistance = dot;
                    furthestBlock = block;
                    applicableFurthestBlock = true;
                }
            }
            if (applicableClosestBlock && !applicableFurthestBlock)
            {
                enabled[closestBlock] = false;
                closestBlock.Enabled = false;
            }
            if (applicableFurthestBlock)
            {
                enabled[furthestBlock] = true;
                if (state == true)
                {
                    furthestBlock.Enabled = true;
                }
            }
           
        }
        private void DisableGravityDrive()
        {
            for (int i = massBlocks.Count - 1; i >= 0; i--)
            {
                IMyArtificialMassBlock massBlock = massBlocks[i];
                if (massBlock.Closed || !GridTerminalSystem.CanAccess(massBlock))
                {
                    massBlocks.Remove(massBlock);
                    continue;
                }
                massBlock.Enabled = false;
            }
            for (int i = gravityGenerators.Count - 1; i >= 0; i--)
            {
                IMyGravityGenerator gravityGenerator = gravityGenerators[i];
                if (gravityGenerator.Closed || !GridTerminalSystem.CanAccess(gravityGenerator))
                {
                    gravityGenerators.Remove(gravityGenerator);
                    continue;
                }
                gravityGenerator.Enabled = false;
            }
        }
        private void EnableGravityDrive()
        {
            for (int i = massBlocks.Count - 1; i >= 0; i--)
            {
                IMyArtificialMassBlock massBlock = massBlocks[i];
                if (massBlock.Closed || !GridTerminalSystem.CanAccess(massBlock))
                {
                    massBlocks.Remove(massBlock);
                    continue;
                }
                massBlock.Enabled = enabled[massBlock];
            }
            for (int i = gravityGenerators.Count - 1; i >= 0; i--)
            {
                IMyGravityGenerator gravityGenerator = gravityGenerators[i];
                if (gravityGenerator.Closed || !GridTerminalSystem.CanAccess(gravityGenerator))
                {
                    gravityGenerators.Remove(gravityGenerator);
                    continue;
                }
                gravityGenerator.Enabled = true;
            }
        }
    }
}
