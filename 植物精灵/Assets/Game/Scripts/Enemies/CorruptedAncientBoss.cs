using System;
using System.Collections;
using UnityEngine;

namespace PlantSpirit.GGJ
{
    public sealed class CorruptedAncientBoss : MonoBehaviour, IDamageReceiver
    {
        private Transform player;
        private float health = 150f;
        private float nextAttack;
        private int attackIndex;
        public event Action Defeated;

        public void Configure(Transform target) => player = target;

        private void Update()
        {
            if (player == null || GameBootstrap.Instance == null || GameBootstrap.Instance.State.Current != GameState.Playing) return;
            if (Time.time < nextAttack) return;
            nextAttack = Time.time + 2.5f;
            int phase = attackIndex++ % 3;
            if (phase == 0) FireFruit();
            else if (phase == 1) StartCoroutine(Spike());
            else SummonTreant();
        }

        public bool TryReceive(DamageInfo info)
        {
            health -= info.Amount;
            if (health > 0f) return true;
            Defeated?.Invoke();
            Destroy(gameObject);
            return true;
        }

        private void FireFruit()
        {
            GameObject fruit = new GameObject("CorruptedFruit");
            fruit.layer = 11;
            fruit.transform.position = transform.position + Vector3.up * .7f;
            fruit.AddComponent<PlaceholderVisual>().Configure(new Color(.45f, .15f, .48f), Vector2.one * .36f, 5);
            BossFruit projectile = fruit.AddComponent<BossFruit>();
            projectile.Launch(((Vector2)(player.position - fruit.transform.position)).normalized * 4.5f);
        }

        private IEnumerator Spike()
        {
            Vector3 point = player.position;
            GameObject warning = new GameObject("RootSpikeWarning");
            warning.transform.position = point;
            warning.AddComponent<PlaceholderVisual>().Configure(new Color(1f, .3f, .15f, .35f), Vector2.one * 2.4f, 2);
            yield return new WaitForSeconds(1.2f);
            Collider2D hit = Physics2D.OverlapCircle(point, 1.2f, 1 << 9);
            hit?.GetComponent<Hurtbox2D>()?.Receive(new DamageInfo { AttackInstanceId = GetInstanceID() + attackIndex, Amount = 12f, Source = gameObject });
            WorldArtPresentation2D.SpawnBurst(point, 1.2f);
            Destroy(warning);
        }

        private void SummonTreant()
        {
            GameObject minion = new GameObject("CorruptedTreant");
            minion.layer = 12;
            minion.transform.position = transform.position + Vector3.right * (player.position.x < transform.position.x ? -1.6f : 1.6f);
            minion.AddComponent<PlaceholderVisual>().Configure(new Color(.26f, .44f, .2f), new Vector2(1.2f, 1.7f), 3);
            Rigidbody2D body = minion.AddComponent<Rigidbody2D>(); body.gravityScale = 3.2f; body.freezeRotation = true;
            minion.AddComponent<BoxCollider2D>(); minion.AddComponent<Hurtbox2D>(); minion.AddComponent<StatusController>();
            EnemyController enemy = minion.AddComponent<EnemyController>();
            enemy.Configure(EnemyKind.Vine, player, 55f, .9f, 16f);
        }
    }

    public sealed class BossFruit : MonoBehaviour
    {
        private Vector2 velocity;
        public void Launch(Vector2 initialVelocity) { velocity = initialVelocity; Destroy(gameObject, 3f); }
        private void Update()
        {
            transform.position += (Vector3)(velocity * Time.deltaTime);
            Collider2D hit = Physics2D.OverlapCircle(transform.position, .22f, 1 << 9);
            if (hit == null) return;
            hit.GetComponent<Hurtbox2D>()?.Receive(new DamageInfo { AttackInstanceId = GetInstanceID(), Amount = 9f, Source = gameObject });
            GameObject floor = new GameObject("CorruptedFloor"); floor.transform.position = transform.position;
            floor.AddComponent<PlaceholderVisual>().Configure(new Color(.35f, .08f, .38f, .38f), Vector2.one * 2f, 1);
            floor.AddComponent<PoisonZone>().Activate(2f, 3f, 0f, 1 << 9);
            Destroy(gameObject);
        }
    }

    public sealed class HealingShrine : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            PlayerHealth health = other.GetComponent<PlayerHealth>();
            if (health != null) health.RestoreFull();
        }
    }
}
