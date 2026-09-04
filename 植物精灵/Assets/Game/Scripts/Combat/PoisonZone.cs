using System.Collections.Generic;
using UnityEngine;

namespace PlantSpirit.GGJ
{
    public sealed class PoisonZone : MonoBehaviour
    {
        private float damage; private float expiresAt; private float slow; private LayerMask hurtboxMask;
        public void Activate(float damagePerSecond, float duration, float slowPercent, LayerMask mask)
        {
            damage = damagePerSecond; expiresAt = Time.time + duration; slow = slowPercent; hurtboxMask = mask;
        }
        private void Update()
        {
            if (Time.time >= expiresAt) { Destroy(gameObject); return; }
            foreach (Collider2D hit in Physics2D.OverlapCircleAll(transform.position, 3f, hurtboxMask))
            {
                StatusController status = hit.GetComponent<StatusController>();
                if (status != null) status.ApplyPoison(damage, 3f, slow);
            }
        }
    }
}
