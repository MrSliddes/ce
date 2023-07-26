using SLIDDES.StateMachines.Trinity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace SLIDDES.Project.FPS
{
    public class WeaponState : State
    {
        // Shortcuts
        protected Weapon Weapon => weapon;
        protected Weapon.Components Components => Weapon.components;
        protected Weapon.Events Events => Weapon.events;
        protected Weapon.Values Values => Weapon.values;
        protected Weapon.Input Input => Weapon.input;
        protected WeaponController WeaponController => Components.weaponController;

        protected StateMachine stateMachine;
        protected Weapon weapon;

        // Actions
        protected UnityAction<InputAction.CallbackContext> actionIdle;
        protected UnityAction<bool> actionJumping;
        protected UnityAction<InputAction.CallbackContext> actionReloading;
        protected UnityAction<bool> actionRunning;
        protected UnityAction<InputAction.CallbackContext> actionShooting;
        protected UnityAction<bool> actionWalking;
        protected UnityAction<InputAction.CallbackContext> actionZooming;

        // Listeners
        protected bool ListenerIdle
        {
            set
            {
                if(value) Weapon.events.onInputShoot.AddListener(actionIdle); else Weapon.events.onInputShoot.RemoveListener(actionIdle);
            }
        }
        protected bool ListenerJumping // used as onJump and onLand
        {
            set
            {
                if(value) Weapon.events.onJumping.AddListener(actionJumping); else Weapon.events.onJumping.RemoveListener(actionJumping);
            }
        }
        protected bool ListenerReloading
        {
            set
            {
                if(value) Weapon.events.onInputReload.AddListener(actionReloading); else Weapon.events.onInputReload.RemoveListener(actionReloading);
            }
        }
        protected bool ListenerRunning
        {
            set
            {
                if(value) Weapon.events.onRunning.AddListener(actionRunning); else Weapon.events.onRunning.RemoveListener(actionRunning);
            }
        }
        protected bool ListenerShooting
        {
            set
            {
                if(value) Weapon.events.onInputShoot.AddListener(actionShooting); else Weapon.events.onInputShoot.RemoveListener(actionShooting);
            }
        }
        protected bool ListenerWalking
        {
            set
            {
                if(value) Weapon.events.onWalking.AddListener(actionWalking); else Weapon.events.onWalking.RemoveListener(actionWalking);
            }
        }
        protected bool ListenerZooming
        {
            set
            {
                if(value) Weapon.events.onInputZoom.AddListener(actionZooming); else Weapon.events.onInputZoom.RemoveListener(actionZooming);
            }
        }

        // Animation Layers
        protected readonly int animLayerReloading = 0;

        public override void Initialize(StateMachine baseStateMachine)
        {
            base.Initialize(baseStateMachine);
            this.stateMachine = baseStateMachine;
            weapon = stateMachine.components.stateMachineUser.GetComponent<Weapon>();

            actionIdle = x =>
            {
                if(x.ReadValue<float>() <= 0) stateMachine.NewState("idle");
            };    
            actionJumping = x =>
            {
                if(x)
                {
                    stateMachine.states["jumping"].OnEnter(); // substate
                }
                else
                {
                    stateMachine.states["jumping"].OnExit(); // substate
                }
            };
            actionReloading = x =>
            {
                if(x.ReadValue<float>() > 0 && Weapon.values.CanReload) stateMachine.NewState("reloading");
            };
            actionRunning = x =>
            {
                if(x && !Values.isRunning)
                {
                    stateMachine.CurrentState.allowsExit = true; // force exit
                    stateMachine.NewState("running");
                }
                else if(!x && Values.isRunning)
                {
                    if(Input.shoot && Values.CanShoot)
                    {
                        stateMachine.NewState("shooting");
                    }
                    else
                    {
                        stateMachine.NewState("idle");
                    }

                    // Continue zoom if holding zoom
                    if(Input.zoom && Values.CanZoom) stateMachine.states["zoom"].OnEnter(); // substate
                }
            };
            actionShooting = x =>
            {
                if(x.ReadValue<float>() > 0)
                {
                    if(Values.CanShoot)
                    {
                        stateMachine.NewState("shooting");
                    }
                    else if(stateMachine.CurrentState == stateMachine.states["running"])
                    {
                        // Interrupt running
                        WeaponController.events.onWeaponInterruptRunning.Invoke(Weapon);
                    }
                    else if(Values.IsMagazineEmpty)
                    {
                        stateMachine.NewState("empty");
                    }
                }
            };
            actionWalking = x =>
            {
                // Substate walking
                if(x && !Values.isWalking && !Values.isJumping)
                {
                    // Enter
                    stateMachine.states["walking"].OnEnter(); // substate
                }
                else if(!x && Values.isWalking && !Values.isJumping)
                {
                    // Exit
                    stateMachine.states["walking"].OnExit(); // substate
                }

                // Exiting running to walking
                // Continue shooting if holding shoot
                if(Input.shoot && Values.CanShoot) stateMachine.NewState("shooting");
                // Continue zoom if holding zoom
                if(Input.zoom && Values.CanZoom) stateMachine.states["zoom"].OnEnter(); // substate
            };
            actionZooming = x =>
            {
                if(x.ReadValue<float>() > 0 && Values.CanZoom)
                {
                    stateMachine.states["zoom"].OnEnter(); // substate
                }
                else
                {
                    stateMachine.states["zoom"].OnExit(); // substate
                }

                if(stateMachine.CurrentState == stateMachine.states["running"])
                {
                    // Interrupt running
                    WeaponController.events.onWeaponInterruptRunning.Invoke(Weapon);
                }
            };
        }
    }
}
