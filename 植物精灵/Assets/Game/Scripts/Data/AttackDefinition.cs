using UnityEngine;

namespace PlantSpirit.GGJ
{
    public enum AttackExecutorType { MeleeBox, VineLine, Projectile, PoisonZone }

    [CreateAssetMenu(menuName = "Plant Spirit/Attack Definition")]
    public sealed class AttackDefinition : ScriptableObject
    {
        public string Id;
        public AttackExecutorType Executor;
        public float Damage;
        public float Startup;
        public float Active;
        public float Recovery;
        public float Cooldown;
        public Vector2 Offset;
        public Vector2 Size = Vector2.one;
        public float Range;
        public int MaxTargets = 1;
    }
}
