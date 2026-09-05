using UnityEngine;

namespace PlantSpirit.GGJ
{
    [CreateAssetMenu(menuName = "Plant Spirit/Player Config")]
    public sealed class PlayerConfig : ScriptableObject
    {
        public float MaxHealth = 100f;
        public float MoveSpeed = 5.2f;
        public float GroundAcceleration = 45f;
        public float GroundDeceleration = 55f;
        public float AirAcceleration = 42f;
        public float JumpVelocity = 12.5f;
        public float MaxFallSpeed = 18f;
        public float CoyoteSeconds = .12f;
        public float JumpBufferSeconds = .12f;
        public float DashDistance = 3.6f;
        public float DashSeconds = .22f;
        public float DashCooldown = 1.1f;
        public float DashInvincibleSeconds = .18f;
        public float HurtInvincibleSeconds = .75f;
    }
}
