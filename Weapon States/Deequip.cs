using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SLIDDES.Tweening;

namespace SLIDDES.Project.FPS
{
    public class Deequip : WeaponState
    {
        public float Time => Values.equippingTime * Values.equippingTimerNormalized;

        private TweenInfo tweenInfoDeequip;

        public Deequip() { }

        public override void OnEnter()
        {
            base.OnEnter();
            // If zoomed in zoom out
            if(Values.IsZooming) stateMachine.states["zoom"].OnExit();

            // Animation
            WeaponController.components.animatorFirstPerson.SetFloat("animSpeedDeequip", 1 / Values.equippingTime);
            WeaponController.components.animatorFirstPerson.CrossFade("Deequip", 0.1f, 0, Mathf.Abs(Values.equippingTimerNormalized - 1)); // reversed normalize (1-0 to 0-1)

            // Values
            Values.isEquipped = false;
            tweenInfoDeequip = Tween.Value(Values.equippingTimerNormalized, 0, Time).OnChange(x =>
            {
                Values.equippingTimerNormalized = x.Float;
            }).OnComplete(x => 
            {
                Weapon.gameObject.SetActive(false);
            });
            WeaponController.events.onWeaponCrosshairSmall.Invoke(Weapon);
        }

        public override void OnExit()
        {
            base.OnExit();

            // Free
            if(tweenInfoDeequip != null)
            {
                tweenInfoDeequip.Stop();
                tweenInfoDeequip.Free();
            }
        }
    }
}
