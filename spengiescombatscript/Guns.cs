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
        Dictionary<MyDefinitionId, float> knownFireDelays;

        const float IdlePowerDraw = 0.002f;

        private List<Gun> guns;
        private Gun activeGun;
        private Dictionary<Gun, bool> availableGuns;
        private MyGridProgram program;
        private float secondDifferenceToGroupGunFiring = 0;

        public bool volley;
        public int volleyDelayFrames;
        private int currentVolleyFrame = 0;
        private bool currentGunIsVolleyFiring = false;

        public Guns(List<IMyUserControllableGun> guns, MyGridProgram program, Dictionary<MyDefinitionId, float> knownFireDelays, float framesToGroupGuns, bool doVolley, int volleyDelayFrames)
        {

            secondDifferenceToGroupGunFiring = framesToGroupGuns / 60;
            availableGuns = new Dictionary<Gun, bool>();
            this.guns = new List<Gun>();
            this.knownFireDelays = knownFireDelays;
            foreach (var gun in guns)
            {
                IMyLargeTurretBase isTurret = gun as IMyLargeTurretBase;
                if (isTurret == null)
                {
                    this.guns.Add(new Gun(gun, knownFireDelays));
                }
            }
            foreach (var gun in this.guns)
            {
                availableGuns[gun] = gun.Available;
            }
            volley = doVolley;
            this.volleyDelayFrames = (int)volleyDelayFrames;

            this.program = program;
            if (volley)
            {
                activeGun = this.guns[0];
            }
        }


        public int AreAvailable()
        {
            int availableGuns = 0;
            for (int i = guns.Count - 1; i >= 0; i--)
            {
                Gun gun = guns[i];
                if (gun == null || gun.Closed)
                {
                    guns.RemoveAt(i); continue;
                }
                gun.Tick();
                bool isGunAvailable = gun.Available;
                availableGuns += isGunAvailable ? 1 : 0;
                this.availableGuns[gun] = isGunAvailable;
            }
            if (volley && currentGunIsVolleyFiring) TickActiveGun();
            return availableGuns;
        }

        private void TickActiveGun()
        {
            currentVolleyFrame++;
            if (currentVolleyFrame >= volleyDelayFrames)
            {
                currentVolleyFrame = 0;
                currentGunIsVolleyFiring = false;
                IncrementActiveGun();
            }
        }

        private void IncrementActiveGun()
        {
            int currentIndex = guns.IndexOf(activeGun);
            activeGun = guns[(currentIndex + 1) % guns.Count];
        }
        public Vector3D GetAimingReferencePos(Vector3D fallback)
        {
            float lowestTimeToFire = GetLowestTimeToFire();
            Vector3D averagePos = Vector3D.Zero;
            int activeGunCount = 0;
            int chargingGunCount = 0;
            foreach (var gun in guns)
            {
                if (availableGuns[gun])
                {
                    if (gun.GetTimeToFire() - secondDifferenceToGroupGunFiring > lowestTimeToFire) { chargingGunCount++; continue; } ;
                    Vector3D GunPos = gun.GetPosition();
                    if (double.IsNaN(GunPos.X)) continue;
                    averagePos += gun.GetPosition();
                    activeGunCount++;
                }
            }
            LCDManager.AddText("    Pre charging: " + chargingGunCount);
            LCDManager.AddText("    Used for aiming: " + activeGunCount);

            if (activeGunCount == 0) return fallback;
            return averagePos / activeGunCount;
        }

        public float GetLowestTimeToFire()
        {
            float lowestTimeToFire = float.MaxValue;
            foreach (var gun in guns)
            {
                if (gun.Available)
                {
                    float timeToFire = gun.GetTimeToFire();
                    lowestTimeToFire = Math.Min(lowestTimeToFire, timeToFire);
                }
            }
            return lowestTimeToFire;
        }

        public void Fire()
        {
            if (volley)
            {
                if (availableGuns[activeGun])
                {
                    activeGun.Enabled = true;
                    activeGun.Shoot = true;
                    currentGunIsVolleyFiring = true;
                }
            }
            else
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
