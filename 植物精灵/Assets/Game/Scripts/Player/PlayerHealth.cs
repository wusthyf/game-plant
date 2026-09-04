using System;
using UnityEngine;

namespace PlantSpirit.GGJ
{
    public sealed class PlayerHealth : MonoBehaviour, IDamageReceiver
    {
        [SerializeField] private PlayerConfig config;
        [SerializeField] private PlayerMotor2D motor;
        public float Current { get; private set; }
        public bool Dead { get; private set; }
        public event Action<float> Changed;
        public event Action Died;
        private float hurtIFrames;
        private GameSession session;

        private void Awake() { if (config != null) ResetHealth(); }
        public void Configure(PlayerConfig playerConfig, PlayerMotor2D playerMotor, GameSession gameSession = null)
        {
            config = playerConfig;
            motor = playerMotor;
            session = gameSession;
            ResetHealth();
        }
        private void Update() => hurtIFrames -= Time.deltaTime;
        public void ResetHealth() { Current = config.MaxHealth; Dead = false; hurtIFrames = 0; Changed?.Invoke(Current); }
        public bool TryReceive(DamageInfo info)
        {
            if (GameBootstrap.Instance != null && GameBootstrap.Instance.State.Current != GameState.Playing) return false;
            if (Dead || hurtIFrames > 0f || (motor != null && motor.DashIFramesRemaining > 0f)) return false;
            GameSession activeSession = session ?? GameBootstrap.Instance?.Session;
            GraftDefinition root = activeSession?.Get(GraftSlot.Root);
            float reduction = root == null ? 0f : root.DamageReduction;
            Current = Mathf.Max(0f, Current - info.Amount * (1f - reduction));
            Rigidbody2D body = motor == null ? null : motor.GetComponent<Rigidbody2D>();
            if (body != null && info.Knockback.sqrMagnitude > 0f) body.AddForce(info.Knockback, ForceMode2D.Impulse);
            hurtIFrames = config.HurtInvincibleSeconds;
            Changed?.Invoke(Current);
            if (Current <= 0f) { Dead = true; Died?.Invoke(); }
            return true;
        }

        public void ApplyFallDamage(float amount)
        {
            if (GameBootstrap.Instance != null && GameBootstrap.Instance.State.Current != GameState.Playing) return;
            if (Dead) return;
            Current = Mathf.Max(0f, Current - amount);
            hurtIFrames = config.HurtInvincibleSeconds;
            Changed?.Invoke(Current);
            if (Current <= 0f) { Dead = true; Died?.Invoke(); }
        }
    }
}
