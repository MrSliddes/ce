using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SLIDDES.Project.FPS
{
    public class Empty : WeaponState
    {
        public Empty() { }

        public override void OnEnter()
        {
            base.OnEnter();

            ListenerIdle = true;

            Events.onEmpty.Invoke();
        }

        public override void OnExit()
        {
            base.OnExit();

            ListenerIdle = false;
        }
    }
}
