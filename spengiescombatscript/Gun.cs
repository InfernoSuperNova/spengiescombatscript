using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components.Interfaces;
using VRage.Game.ObjectBuilders.Components;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    internal class Gun
    {
        static readonly MyDefinitionId ElectricityId = new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), "Electricity");
        IMyUserControllableGun actualGun;
        private float FireDelay;
        private float TimeSpentFiring = 0;
        private bool Firing = true;
        public Gun(IMyUserControllableGun gun, Dictionary<MyDefinitionId, float> knownFireDelays)
        {
            actualGun = gun;
            if (!knownFireDelays.ContainsKey(gun.BlockDefinition))
            {
                FireDelay = 0f;
            }
            else
            {
                FireDelay = knownFireDelays[gun.BlockDefinition];
            }
            
        }

        public void Tick()
        {
            if (Available && Firing)
            {
                TimeSpentFiring = Math.Min(TimeSpentFiring + (1f/60f), FireDelay);
            }
            else
            {
                TimeSpentFiring = 0;
            }
        }

        public bool Enabled
        {
            get { return actualGun.Enabled; }

            set { actualGun.Enabled = value; }
        }
        public bool Shoot
        {
            get { return actualGun.Shoot; }
            set { actualGun.Shoot = value; }
        }
        public float PowerDraw
        {
            get { return actualGun.Components.Get<MyResourceSinkComponent>().MaxRequiredInputByType(ElectricityId); }
        }
        public bool Closed
        {
            get { return actualGun.Closed; }
        }
        public bool IsFunctional
        {
            get { return actualGun.IsFunctional; }
        }


        public Vector3D GetPosition()
        {
            return actualGun.GetPosition();
        }
        public float GetTimeToFire()
        {
            return FireDelay - TimeSpentFiring;
        }
        public bool Available
        {
            get { return actualGun.IsFunctional && PowerDraw < 0.002f; }
        }

    }
}
