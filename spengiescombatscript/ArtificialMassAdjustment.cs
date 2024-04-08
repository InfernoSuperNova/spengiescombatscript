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
    internal class ArtificialMassAdjustment
    {
        public static void AdjustCenterOfMass(List<IMyArtificialMassBlock> massBlocks, Vector3 target)
        {
            const double epsilon = 0.0001; // Small value for convergence

            // Initial calculation of center of mass
            Vector3 currentCenter = Vector3.Zero;

            foreach (var massBlock in massBlocks)
            {
                currentCenter += massBlock.GetPosition();
            }
            currentCenter /= massBlocks.Count;

            // Loop until center of mass is close to the target point
            while (Math.Abs(currentCenter.X - target.X) > epsilon ||
                   Math.Abs(currentCenter.Y - target.Y) > epsilon ||
                   Math.Abs(currentCenter.Z - target.Z) > epsilon)
            {
                // Adjust masses based on distance from target
                foreach (var massBlock in massBlocks)
                {
                    Vector3 distanceVec = massBlock.GetPosition() - currentCenter;

                    double distanceToTarget = distanceVec.Length(); // Magnitude of the vector

                    // Adjust mass inversely proportional to distance
                    massBlock.VirtualMass *= Math.Max(1.0, distanceToTarget / 1000.0); // You can adjust this factor
                }

                // Recalculate the center of mass
                currentCenter = massBlocks.Select(obj => obj.GetPosition()).Aggregate((sum, next) => sum + next) / massBlocks.Count;
            }
        }
    }
}
