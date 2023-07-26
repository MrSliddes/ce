using SLIDDES.StateMachines.Trinity;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace SLIDDES.Project.FPS
{
    public class Shooting : WeaponState
    {
        /// <summary>
        /// The speed at which the weapon fires per second
        /// </summary>
        public float FireSpeed => fireSpeed;

        /// <summary>
        /// Has the weapon completed the fire mode to exit the shooting state?
        /// </summary>
        private bool completedFireMode;
        /// <summary>
        /// True when recoil recovery resetted position
        /// </summary>
        private bool resettedRecoilPosition;
        /// <summary>
        /// The current index of the recoil[]
        /// </summary>
        private int recoilIndex;
        /// <summary>
        /// The amount of shots fired since entering this state
        /// </summary>
        private int shotsFired;
        /// <summary>
        /// Keeps track of the fire animation time
        /// </summary>
        private float fireAnimationTimer;
        /// <summary>
        /// The speed at which the weapon fires per second
        /// </summary>
        private float fireSpeed;
        /// <summary>
        /// Triggerd when recoil recovery is completed
        /// </summary>
        private UnityAction actionOnRecoilRecoveryComplete;

        public Shooting() { }

        public override void Initialize(StateMachine baseStateMachine)
        {
            base.Initialize(baseStateMachine);
            actionOnRecoilRecoveryComplete = () =>
            {
                recoilIndex = 0;
            };
        }

        public override void OnEnter()
        {
            base.OnEnter();

            // Listeners
            ListenerJumping = true;
            ListenerReloading = true;
            ListenerRunning = true;
            ListenerWalking = true;
            ListenerZooming = true;

            // Values
            if(Values.fireMode == FireMode.burst) allowsExit = false; // await shooting all bullets before exiting fire Mode
            Values.isShooting = true;
            completedFireMode = false;
            shotsFired = 0;
            fireSpeed = Values.roundsPerMinute / 60f;
            if(resettedRecoilPosition) recoilIndex = 0;
        }

        public override void OnUpdate()
        {
            base.OnUpdate();

            // Shoot if magazine still has ammo
            if(Values.magazineCurrentAmmo > 0) Shoot();

            // Get out of fireMode automatic if not holding trigger or ammo is empty
            if(Values.fireMode == FireMode.fullyAutomatic) completedFireMode = !Weapon.input.shoot || Values.magazineCurrentAmmo <= 0;

            // Check fire animation exit
            fireAnimationTimer -= Time.deltaTime;
            if(fireAnimationTimer <= 0 && completedFireMode)
            {
                // Allow state to be exited
                allowsExit = true;

                if(Values.magazineCurrentAmmo <= 0)
                {
                    if(Values.CurrentTotalAmmo > 0)
                    {
                        // Exit to auto reloading
                        stateMachine.NewState("reloading");
                    }
                    else
                    {
                        stateMachine.NewState("idle");
                    }
                    return;
                }

                // Else to idle
                stateMachine.NewState("idle");
                return;
            }
        }

        public override void OnExit()
        {
            base.OnExit();

            // Listeners
            ListenerJumping = false;
            ListenerReloading = false;
            ListenerRunning = false;
            ListenerWalking = false;
            ListenerZooming = false;

            // Values
            Values.isShooting = false;

            // Recover from recoil
            Components.weaponController.components.weaponCameraController.RecoverFromRecoil(Components.recoil.recoveryTime, actionOnRecoilRecoveryComplete);
        }

        /// <summary>
        /// Adds recoil to the weapon
        /// </summary>
        private void Recoil()
        {
            // Check if recoil is assigned
            if(Components.recoil == null) return;

            // Get current recoil value
            Vector3 recoilValue = Components.recoil.pattern[recoilIndex];
            // Get the time the recoil needs to move from prev pos to new pos (Auto calc if set to 0)
            float recoilTime = recoilValue.z == 0 ? 1 / fireSpeed : recoilValue.z;

            // Add recoil force to camera
            Components.weaponController.components.weaponCameraController.AddRecoilForce(new WeaponCameraController.RecoilInfo(recoilValue, recoilTime));

            // Increase index
            recoilIndex++;
            // Check recoil loop mode
            if(recoilIndex >= Components.recoil.pattern.Length)
            {
                recoilIndex = Components.recoil.repeatMode switch
                {
                    WeaponRecoil.RepeatMode.loop => 0,
                    WeaponRecoil.RepeatMode.loopFrom => Components.recoil.loopFromIndex,
                    _ => 0
                };
            }
        }

        /// <summary>
        /// Shoots a projectile
        /// </summary>
        private void Shoot()
        {
            // Await next projectile shot
            if(!Values.CanShoot) return;
            // Dont shoot a projectile if completed fireMode
            if(completedFireMode) return;

            // Get the amount of projectiles to be shot at the same time
            int projectileAmount = Components.projectile.stamp.Length == 0 ? 1 : Components.projectile.stamp.Length;
            for(int i = 0; i < projectileAmount; i++)
            {
                // Get projectile shot position
                Vector3 projectilPosition = Components.projectile.stamp.Length == 0 ? Vector3.zero : Components.projectile.stamp[i];

                // Shoot a raycast or a prefab
                if(Components.projectile.prefabProjectile == null)
                {
                    // Raycast projectile
                    // Weapon spread
                    Vector3 shootDirection = Components.weaponController.components.weaponCameraController.transform.forward;
                    if(!Values.isZoomedIn || Values.projectileSpreadADSFire)
                    {
                        shootDirection += Components.weaponController.transform.TransformDirection(new Vector3(Random.Range(-Values.projectileSpread.x, Values.projectileSpread.x), Random.Range(-Values.projectileSpread.y, Values.projectileSpread.y), 0));
                        shootDirection += Components.weaponController.transform.TransformDirection(projectilPosition);
                    }

                    Vector3 hitPoint = Vector3.zero;
                    RaycastHit[] hits = Physics.RaycastAll(Components.weaponController.transform.position, shootDirection, Values.maxShootingRange, Components.projectile.layerMask, QueryTriggerInteraction.Collide);
                    hits = hits.OrderBy(x => x.distance).ToArray(); // Order hits by distance as raycast doesnt do that

                    // If anything is hit
                    if(hits.Length > 0)
                    {
                        int piercedShots = 0;
                        for(int j = 0; j < hits.Length; j++)
                        {
                            RayCastHitShot(hits[j]);
                            hitPoint = hits[j].point;

                            // Exit piercing
                            if(piercedShots >= Components.projectile.maxPiercingAmount) break;
                            piercedShots++;
                        }
                    }
                    else
                    {
                        // Nothing was hit
                        // Set hitpoint deadcenter
                        Ray ray = Weapon.components.weaponController.components.camera.ScreenPointToRay(Vector3.zero);
                        hitPoint = ray.GetPoint(Values.maxShootingRange);
                    }
                }
                else
                {
                    // Prefab projectile
                    GameObject a = GameObject.Instantiate(Components.projectile.prefabProjectile, Components.projectileInstantiatePoint.position + projectilPosition, Components.projectileInstantiatePoint.rotation);
                }
            }
            // Shot projectiles
            Events.onFiredProjectile.Invoke();

            // Values
            shotsFired++;
            Values.magazineCurrentAmmo--;
            Values.timeSinceLastShot = 0;

            Recoil();

            // VFX
            if(Components.vfxMuzzleFlash != null) Components.vfxMuzzleFlash.SendEvent("OnTrigger");

            // Check fireMode completion
            switch(Values.fireMode)
            {
                case FireMode.burst:
                    if(shotsFired >= Values.burstFireAmount)
                    {
                        completedFireMode = true;
                        Values.needToReleaseTrigger = true;
                    }
                    break;
                case FireMode.fullyAutomatic:
                    completedFireMode = !Weapon.input.shoot; // completed when trigger is released (also set in onUpdate)
                    break;
                case FireMode.semiAutomatic:
                    completedFireMode = true;
                    Values.needToReleaseTrigger = true;
                    break;
                default: throw new System.IndexOutOfRangeException($"FireMode = {(int)Values.fireMode}");
            }

            // Have to wait till fire animation is finished
            allowsExit = false;
            fireAnimationTimer = 1 / fireSpeed;

            WeaponController.events.onWeaponShot.Invoke(Weapon);
            WeaponController.events.onWeaponAmmoChange.Invoke(Weapon);
            WeaponController.events.onWeaponCrosshairBig.Invoke(Weapon);
        }

        private void RayCastHitShot(RaycastHit hit)
        {
            Debug.Log("hit: " + hit.transform.name);

            // Spawn VFX projectile on impact
            if(Components.projectile.prefabImpactPoint != null)
            {
                GameObject a = GameObject.Instantiate(Components.projectile.prefabImpactPoint, hit.point, Quaternion.LookRotation(hit.normal));
                a.transform.SetParent(hit.transform);
            }

            Events.onHit.Invoke(hit.transform.gameObject);
        }
    }
}
