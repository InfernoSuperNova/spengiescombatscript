using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.ObjectBuilders.Components;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript.Classes
{

    public class Guns
    {
        static readonly MyDefinitionId ElectricityId = new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), "Electricity");
        Dictionary<MyDefinitionId, float> knownFireDelays;

        const float IdlePowerDraw = 0.002f;

        private List<IMyUserControllableGun> guns;
        private Dictionary<IMyUserControllableGun, bool> availableGuns;
        private MyGridProgram program;
        public Guns(List<IMyUserControllableGun> guns, MyGridProgram program, Dictionary<MyDefinitionId, float> knownFireDelays)
        {
            availableGuns = new Dictionary<IMyUserControllableGun, bool>();
            this.guns = new List<IMyUserControllableGun>();
            this.knownFireDelays = knownFireDelays;
            foreach (var gun in guns)
            {
                IMyLargeTurretBase isTurret = gun as IMyLargeTurretBase;
                if (isTurret == null)
                {
                    this.guns.Add(gun);
                }
            }
            foreach (var gun in this.guns)
            {
                availableGuns[gun] = false;
            }
            this.program = program;
        }


        public int AreAvailable()
        {
            int availableGuns = 0;
            for (int i = guns.Count - 1; i >= 0; i--)
            {
                IMyUserControllableGun gun = guns[i];
                if (gun == null || gun.Closed)
                {
                    guns.RemoveAt(i); continue;
                }
                bool IsFunctional = gun.IsFunctional;
                bool IsReadyToFire = gun.Components.Get<MyResourceSinkComponent>().MaxRequiredInputByType(ElectricityId) < (IdlePowerDraw + float.Epsilon);

                bool isGunAvailable = IsFunctional && IsReadyToFire;
                availableGuns += isGunAvailable ? 1 : 0;
                this.availableGuns[gun] = isGunAvailable;
            }
            return availableGuns;
        }

        public Vector3D GetAimingReferencePos(Vector3D fallback)
        {
            Vector3D averagePos = Vector3D.Zero;
            int activeGunCount = 0;
            foreach (var gun in guns)
            {
                if (availableGuns[gun])
                {
                    Vector3D GunPos = gun.GetPosition();
                    if (double.IsNaN(GunPos.X)) continue;
                    averagePos += gun.GetPosition();
                    activeGunCount++;
                }
            }

            if (activeGunCount == 0) return fallback;

            return averagePos / activeGunCount;
        }

        public void Fire()
        {
            foreach (var gun in guns)
            {
                if (availableGuns[gun])
                {
                    gun.Enabled = true;
                    gun.Shoot = true;
                }
            }
        }
        public void Cancel()
        {
            foreach (var gun in guns)
            {
                if (availableGuns[gun])
                {
                    gun.Shoot = false;
                    gun.Enabled = false;
                }
                else
                {
                    gun.Shoot = false;
                    gun.Enabled = true; //ensure that uncharged guns will still accumulate power
                }
            }
        }
        public void Standby()
        {
            foreach (var gun in guns)
            {
                gun.Shoot = false;
                gun.Enabled = true;
            }
        }
    }
}
