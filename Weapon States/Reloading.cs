using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SLIDDES.Project.FPS
{
    public class Reloading : WeaponState
    {
        /// <summary>
        /// Coroutine used for reloading
        /// </summary>
        private Coroutine coroutineReloading;

        public Reloading() { }

        public override void OnEnter()
        {
            base.OnEnter();

            // If zoomed in zoom out
            if(Values.IsZooming) stateMachine.states["zoom"].OnExit();

            // Start reload coroutine
            coroutineReloading = Weapon.StartCoroutine(ReloadAsync());

            // Listeners
            ListenerWalking = true;
            ListenerRunning = true;
            ListenerJumping = true;

            // Values
            WeaponController.events.onWeaponCrosshairSmall.Invoke(Weapon);
            Events.onReloadStart.Invoke();

            // Animation
            Components.weaponController.components.animatorFirstPerson.SetFloat("animSpeedReloading", 1 / Values.reloadTime);
            Components.weaponController.components.animatorFirstPerson.CrossFade("Reloading", 0.1f);
        }

        public override void OnExit()
        {
            base.OnExit();

            // Listeners
            ListenerWalking = false;
            ListenerRunning = false;
            ListenerJumping = false;

            // If state needs to be exited but still reloading
            if(Values.isReloading)
            {
                // Interrupt reloading
                Values.isReloading = false;
                if(coroutineReloading != null) Weapon.StopCoroutine(coroutineReloading);
                // Speed up animation to finish with 2x speed of reload time
                Components.weaponController.components.animatorFirstPerson.SetFloat("animSpeedReloading", 1 / (Values.reloadTime * 0.5f));
            }
            else
            {
                Events.onReloadFinished.Invoke();
            }
        }

        /// <summary>
        /// Reload the weapon
        /// </summary>
        public void Reload()
        {
            // Reload ammo
            Values.currentAmmo += Values.magazineCurrentAmmo; // dump ammo back
            Values.magazineCurrentAmmo = 0;
            // If there is enough ammo left to fill magazine take max magazine amount, else get whats left of current ammo
            int receivingAmmo = Values.currentAmmo >= Values.magazineMaxAmmo ? Values.magazineMaxAmmo : Values.currentAmmo;
            Values.magazineCurrentAmmo = receivingAmmo;
            Values.currentAmmo -= receivingAmmo;

            Values.isReloading = false;
            WeaponController.events.onWeaponReload.Invoke(Weapon);
            // Continue zoom if holding zoom
            if(Input.zoom && Values.CanZoom) stateMachine.states["zoom"].OnEnter(); // substate
            stateMachine.NewState("idle");
        }

        /// <summary>
        /// Await seconds before reloading
        /// </summary>
        /// <returns></returns>
        private IEnumerator ReloadAsync()
        {
            Values.isReloading = true;
            yield return new WaitForSeconds(Values.reloadTime);

            Reload();
        }
    }
}
