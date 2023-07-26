using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SLIDDES.Tweening;

namespace SLIDDES.Project.FPS
{
    public class Equip : WeaponState
    {
        public float Time => Values.equippingTime - (Values.equippingTime * Values.equippingTimerNormalized);
        private TweenInfo tweenInfoEquip;

        public Equip() { }

        public override void OnEnter()
        {
            base.OnEnter();
            
            // Animation
            WeaponController.components.animatorFirstPerson.SetFloat("animSpeedEquip", 1 / Values.equippingTime);
            WeaponController.components.animatorFirstPerson.CrossFade("Equip", 0.1f, 0, Values.equippingTimerNormalized);
            
            // Values
            Weapon.gameObject.SetActive(true);            
            Values.isEquipped = true;
            Values.isEquipping = true;
            tweenInfoEquip = Tween.Value(Values.equippingTimerNormalized, 1, Time).OnChange(x =>
            {
                Values.equippingTimerNormalized = x.Float;
            }).OnComplete(x =>
            {
                stateMachine.NewState("idle");
            });
            WeaponController.events.onWeaponCrosshairSmall.Invoke(Weapon);
        }

        public override void OnExit()
        {
            base.OnExit();

            // Values
            Values.isEquipping = false;

            // Free
            if(tweenInfoEquip != null)
            {
                tweenInfoEquip.Stop();
                tweenInfoEquip.Free();
            }
        }
    }
}
