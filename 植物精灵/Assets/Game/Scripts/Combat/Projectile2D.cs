using UnityEngine;

namespace PlantSpirit.GGJ
{
    public sealed class Projectile2D : MonoBehaviour
    {
        private Vector2 velocity;
        private float damage;
        private float expiry;
        private LayerMask hurtboxMask;
        private int instanceId;
        public void Launch(Vector2 initialVelocity, float value, float lifetime, LayerMask mask, int attackId)
        {
            velocity = initialVelocity; damage = value; expiry = Time.time + lifetime; hurtboxMask = mask; instanceId = attackId;
            WorldArtPresentation2D.AttachProjectile(gameObject, velocity);
        }
        private void Update()
        {
            Vector2 start = transform.position;
            Vector2 displacement = velocity * Time.deltaTime;
            float distance = displacement.magnitude;
            RaycastHit2D hit = distance > 0f
                ? Physics2D.CircleCast(start, .14f, displacement / distance, distance, hurtboxMask)
                : default;
            if (hit.collider != null)
            {
                transform.position = hit.centroid;
                PlayerMotor2D shieldedPlayer = hit.collider.GetComponent<PlayerMotor2D>();
                if (shieldedPlayer != null && shieldedPlayer.IsProjectileShieldActive) { Finish(true); return; }
                Hurtbox2D hurtbox = hit.collider.GetComponent<Hurtbox2D>();
                if (hurtbox != null) hurtbox.Receive(new DamageInfo { AttackInstanceId = instanceId, Amount = damage, Knockback = velocity.normalized * 3f, Source = gameObject });
                Finish(true);
                return;
            }
            transform.position = start + displacement;
            if (Time.time >= expiry) Finish(false);
        }

        private void Finish(bool showImpact)
        {
            if (showImpact) WorldArtPresentation2D.SpawnBurst(transform.position, .62f);
            Destroy(gameObject);
        }
    }
}
