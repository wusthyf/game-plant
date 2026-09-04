using UnityEngine;

namespace PlantSpirit.GGJ
{
    public sealed class KillPlane : MonoBehaviour
    {
        private Vector3 respawn;
        private PlayerMotor2D player;

        public void Configure(Vector3 spawnPoint, PlayerMotor2D target)
        {
            respawn = spawnPoint;
            player = target;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            PlayerMotor2D motor = other.GetComponent<PlayerMotor2D>();
            if (motor == null || motor != player) return;
            PlayerHealth health = motor.GetComponent<PlayerHealth>();
            health?.ApplyFallDamage(20f);
            if (health != null && health.Dead) return;
            Rigidbody2D body = motor.GetComponent<Rigidbody2D>();
            motor.transform.position = respawn;
            if (body != null) body.velocity = Vector2.zero;
        }
    }
}
