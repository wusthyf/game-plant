using UnityEngine;

namespace PlantSpirit.GGJ
{
    [CreateAssetMenu(menuName = "Plant Spirit/Graft Definition")]
    public sealed class GraftDefinition : ScriptableObject
    {
        public string Id;
        public GraftSlot Slot;
        public string DisplayName;
        [TextArea] public string Description;
        public float DamageReduction;
        public float AttackDamage;
        public float AttackRange;
        public float Cooldown;
        public bool BlocksProjectilesDuringDash;
    }
}
