using System.Collections.Generic;
using UnityEngine;

namespace PlantSpirit.GGJ
{
    public sealed class PoisonZone : MonoBehaviour
    {
        private readonly HashSet<StatusController> affected = new HashSet<StatusController>();
        private float damage; private float expiresAt; private float slow; private LayerMask hurtboxMask;
        public void Activate(float damagePerSecond, float duration, float slowPercent, LayerMask mask)
        {
            damage = damagePerSecond; expiresAt = Time.time + duration; slow = slowPercent; hurtboxMask = mask;
            affected.Clear();
            Physics2D.SyncTransforms();
            ApplyToTargets(duration);
        }
        private void Update()
        {
            float remaining = expiresAt - Time.time;
            if (remaining <= 0f) { Destroy(gameObject); return; }
            ApplyToTargets(remaining);
        }

        private void ApplyToTargets(float duration)
        {
            foreach (Collider2D hit in Physics2D.OverlapCircleAll(transform.position, 3f, hurtboxMask))
            {
                StatusController status = hit.GetComponent<StatusController>();
                if (status != null && affected.Add(status)) status.ApplyPoison(damage, duration, slow);
            }
        }
    }
}
