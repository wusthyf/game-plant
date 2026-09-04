using System.Collections.Generic;
using UnityEngine;

namespace PlantSpirit.GGJ
{
    public sealed class Hitbox2D : MonoBehaviour
    {
        [SerializeField] private LayerMask hurtboxMask;
        private readonly Collider2D[] results = new Collider2D[16];
        private readonly HashSet<Hurtbox2D> hitTargets = new HashSet<Hurtbox2D>();
        private int attackInstanceId;

        public void Configure(LayerMask mask) => hurtboxMask = mask;

        public int Open(int instanceId)
        {
            attackInstanceId = instanceId;
            hitTargets.Clear();
            return attackInstanceId;
        }

        public int StrikeBox(Vector2 center, Vector2 size, DamageInfo info, int maxTargets)
        {
            int count = Physics2D.OverlapBoxNonAlloc(center, size, 0f, results, hurtboxMask);
            int accepted = 0;
            for (int i = 0; i < count && accepted < maxTargets; i++)
            {
                Hurtbox2D hurtbox = results[i].GetComponent<Hurtbox2D>();
                if (hurtbox == null || !hitTargets.Add(hurtbox)) continue;
                info.AttackInstanceId = attackInstanceId;
                if (hurtbox.Receive(info)) accepted++;
            }
            return accepted;
        }

        public void Close()
        {
            attackInstanceId = 0;
            hitTargets.Clear();
        }
    }
}
