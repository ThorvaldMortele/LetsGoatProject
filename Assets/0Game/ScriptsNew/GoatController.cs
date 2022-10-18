using System.Collections;
using System.Collections.Generic;
using Fusion;
using Sirenix.OdinInspector;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[OrderBefore(typeof(NetworkTransform))]
[DisallowMultipleComponent]
// ReSharper disable once CheckNamespace
public class GoatController : NetworkTransform
{
    [Header("Character Controller Settings")]
    public float gravity = -20.0f;
    public float jumpImpulse = 8.0f;
    public float acceleration = 10.0f;
    public float braking = 10.0f;
    public float maxSpeed = 2.0f;
    public float rotationSpeed = 10f;
    public float sprintMultiplier = 1.5f;

    [Networked]
    [HideInInspector]
    public bool IsGrounded { get; set; }

    [Networked]
    [HideInInspector]
    public Vector3 Velocity { get; set; }

    [Networked]
    public bool ApplyGravity { get; set; }

    [Networked]
    public bool Sprinting { get; set; }

    [Networked]
    public float MaxVelocityY { get; set; }

    /// <summary>
    /// Sets the default teleport interpolation velocity to be the CC's current velocity.
    /// For more details on how this field is used, see <see cref="NetworkTransform.TeleportToPosition"/>.
    /// </summary>
    protected override Vector3 DefaultTeleportInterpolationVelocity => Velocity;

    /// <summary>
    /// Sets the default teleport interpolation angular velocity to be the CC's rotation speed on the Z axis.
    /// For more details on how this field is used, see <see cref="NetworkTransform.TeleportToRotation"/>.
    /// </summary>
    protected override Vector3 DefaultTeleportInterpolationAngularVelocity => new Vector3(0f, 0f, rotationSpeed);

    public CharacterController Controller { get; private set; }

    private List<PushTimer> _pushTimers = new List<PushTimer>();

    public struct PushTimer
    {
        public Vector3 Force;
        public TickTimer Timer;
        public float Time;
    }

    protected override void Awake()
    {
        base.Awake();
        CacheController();
    }

    public override void Spawned()
    {
        base.Spawned();
        CacheController();

        // Caveat: this is needed to initialize the Controller's state and avoid unwanted spikes in its perceived velocity
        Controller.Move(transform.position);
        ApplyGravity = true;
        Sprinting = false;
        MaxVelocityY = 0;
    }

    private void CacheController()
    {
        if (Controller == null)
        {
            Controller = GetComponent<CharacterController>();

            Assert.Check(Controller != null, $"An object with {nameof(GoatController)} must also have a {nameof(CharacterController)} component.");
        }
    }

    protected override void CopyFromBufferToEngine()
    {
        // Trick: CC must be disabled before resetting the transform state
        Controller.enabled = false;

        // Pull base (NetworkTransform) state from networked data buffer
        base.CopyFromBufferToEngine();

        // Re-enable CC
        Controller.enabled = true;
    }

    /// <summary>
    /// Basic implementation of a jump impulse (immediately integrates a vertical component to Velocity).
    /// <param name="ignoreGrounded">Jump even if not in a grounded state.</param>
    /// <param name="overrideImpulse">Optional field to override the jump impulse. If null, <see cref="jumpImpulse"/> is used.</param>
    /// </summary>
    public virtual void Jump(bool ignoreGrounded = false, float? overrideImpulse = null)
    {
        if (IsGrounded || ignoreGrounded)
        {
            var newVel = Velocity;
            newVel.y = MaxVelocityY = overrideImpulse ?? jumpImpulse;
            Velocity = newVel;
        }
    }

    /// <summary>
    /// Basic implementation of a character controller's movement function based on an intended direction.
    /// <param name="direction">Intended movement direction, subject to movement query, acceleration and max speed values.</param>
    /// </summary>
    public virtual void Move(Vector3 direction)
    {
        var deltaTime = Runner.DeltaTime;
        var previousPos = transform.position;
        var moveVelocity = Velocity;

        direction = direction.normalized;

        if (IsGrounded && moveVelocity.y < 0)
        {
            moveVelocity.y = 0f;
        }

        if (ApplyGravity)
        {
            float value = gravity * Runner.DeltaTime;
            moveVelocity.y += value;
        }

        var horizontalVel = default(Vector3);
        horizontalVel.x = moveVelocity.x;
        horizontalVel.z = moveVelocity.z;

        if (direction == default)
        {
            horizontalVel = Vector3.Lerp(horizontalVel, default, braking * deltaTime);
        }
        else
        {
            if (Sprinting)
            {
                horizontalVel = Vector3.ClampMagnitude(horizontalVel + direction * acceleration * sprintMultiplier * deltaTime, maxSpeed * sprintMultiplier);
            }
            else
            {
                horizontalVel = Vector3.ClampMagnitude(horizontalVel + direction * acceleration * deltaTime, maxSpeed);
            }
            
            //do this in update in goat script to avoid jitter
            //transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), rotationSpeed * Runner.DeltaTime);
        }

        moveVelocity.x = horizontalVel.x;
        moveVelocity.z = horizontalVel.z;

        if (Velocity.y > MaxVelocityY)
        {
            moveVelocity.y -= Controller.velocity.y - MaxVelocityY;
        }

        Controller.Move(moveVelocity * deltaTime);

        Velocity = (transform.position - previousPos) * Runner.Simulation.Config.TickRate;
        if (ApplyGravity && MaxVelocityY > 0)
        {
            MaxVelocityY += gravity * Runner.DeltaTime;
            if (MaxVelocityY < 0)
            {
                MaxVelocityY = 0;
            }
        }
        IsGrounded = Controller.isGrounded;
    }

    [Button]
    public virtual void Push(Vector3 force, float time)
    {
        if (Object.HasStateAuthority)
        {
            _pushTimers.Add(new PushTimer(){Force = force, Time = time, Timer = TickTimer.CreateFromSeconds(Runner, time)});
        }
    }

    public override void FixedUpdateNetwork()
    {
        List<int> indexesToRemove = new List<int>(_pushTimers.Count);
        float deltaTime = Runner.DeltaTime;
        for (int i = 0; i < _pushTimers.Count; i++)
        {
            PushTimer timer = _pushTimers[i];
            if (timer.Timer.Expired(Runner))
            {
                indexesToRemove.Add(i);
            }
            else
            {
                Controller.Move(EasePushForce(timer) * deltaTime);
            }
        }

        for (int i = indexesToRemove.Count - 1; i >= 0; i--)
        {
            _pushTimers.RemoveAt(i);
        }
    }

    private Vector3 EasePushForce(PushTimer pushTimer)
    {
        float t = (pushTimer.Time - pushTimer.Timer.RemainingTime(Runner).Value) / pushTimer.Time;
        float x = LeanTween.easeInQuad(pushTimer.Force.x, 0, t);
        float y = LeanTween.easeInQuad(pushTimer.Force.y, 0, t);
        float z = LeanTween.easeInQuad(pushTimer.Force.z, 0, t);
        return new Vector3(x, y, z);
    }
}
