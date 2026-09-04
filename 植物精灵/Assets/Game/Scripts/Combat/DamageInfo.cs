using UnityEngine;

namespace PlantSpirit.GGJ
{
    public enum DamageType { Physical, Poison }

    public struct DamageInfo
    {
        public int AttackInstanceId;
        public float Amount;
        public Vector2 Knockback;
        public GameObject Source;
        public DamageType Type;
    }
}
