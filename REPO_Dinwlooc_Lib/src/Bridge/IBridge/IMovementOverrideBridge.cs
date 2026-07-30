using UnityEngine;

namespace Dinwlooc.Common.IBridge
{
    /// <summary>
    /// 玩家速度/移动相关的覆盖操作接口（来自 PlayerController）。
    /// 包含临时覆盖（带过渡）和永久字段设置。
    /// </summary>
    public interface IMovementOverrideBridge
    {
        // ---- 临时覆盖（带过渡） ----
        void OverrideSpeed(float speedMultiplier, float timeIn, float timeOut, float time = 0.1f);
        void OverrideTimeScale(float timeScaleMultiplier, float time = 0.1f);
        void OverrideLookSpeed(float lookSpeedTarget, float timeIn, float timeOut, float time = 0.1f);
        void OverrideVoicePitch(float voicePitchMultiplier, float timeIn, float timeOut, float time = 0.1f);
        void OverrideJumpCooldown(float cooldown);
        void OverrideDisableTurn(float time = 0.1f, bool reset = false);
        void OverrideTurnRotation(Quaternion rotation, float time = 0.1f);
        void OverrideAnimationSpeed(float animSpeedMulti, float timeIn, float timeOut, float time = 0.1f);

        // ---- 直接字段设置（永久修改） ----
        void SetMoveSpeed(float speed);
        void ResetMoveSpeed();
        void SetSprintSpeed(float speed);
        void ResetSprintSpeed();
        void SetCrouchSpeed(float speed);
        void ResetCrouchSpeed();
        void SetCustomGravity(float gravity);
        void ResetCustomGravity();

        // ---- 其他常用效果 ----
        void MoveForce(Vector3 direction, float amount, float time);
        void ForceImpulse(Vector3 force);
        void AntiGravity(float timer);
        void Feather(float timer);
        void Kinematic(float timer);
        void InputDisable(float time);
        void CrouchOverride(float time);
    }
}