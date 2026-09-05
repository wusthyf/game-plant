using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlantSpirit.GGJ
{
    public sealed class EncounterZone : MonoBehaviour
    {
        [SerializeField] private int order;
        [SerializeField] private GameObject leftGate;
        [SerializeField] private GameObject rightGate;
        [SerializeField] private GraftDefinition reward;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private EnemyKind[] enemies;
        [SerializeField] private GraftInventory inventory;
        [SerializeField] private Transform player;
        private readonly HashSet<EnemyController> living = new HashSet<EnemyController>();
        private static readonly EnemyKind[][] RoomOneWaves =
        {
            new[] { EnemyKind.Vine, EnemyKind.Vine, EnemyKind.Mushroom },
            new[] { EnemyKind.Beetle, EnemyKind.Beetle },
            new[] { EnemyKind.Mushroom, EnemyKind.Beetle },
            new[] { EnemyKind.Vine, EnemyKind.Vine }
        };
        private bool started;
        private bool cleared;
        private int waveIndex;

        public int Order => order;
        public int Remaining => living.Count;
        public bool Started => started;
        public bool ClearedState => cleared;
        public event Action<EncounterZone> Cleared;

        public void Configure(int sequence, Transform playerTransform, GraftInventory graftInventory, GraftDefinition graft, EnemyKind[] roster, Transform[] points)
        {
            order = sequence;
            player = playerTransform;
            inventory = graftInventory;
            reward = graft;
            enemies = roster;
            spawnPoints = points;
        }

        public void Begin()
        {
            if (started || cleared) return;
            started = true;
            if (leftGate != null) leftGate.SetActive(true);
            if (rightGate != null) rightGate.SetActive(true);
            SpawnWave();
        }

        public void UnlockEntry()
        {
            if (leftGate != null) leftGate.SetActive(false);
        }

        private void SpawnEnemy(EnemyKind enemyKind, Transform spawnPoint)
        {
            GameObject obj = new GameObject(enemyKind + " Enemy");
            obj.layer = 12;
            obj.transform.position = spawnPoint.position;
            Color color = enemyKind == EnemyKind.Vine ? new Color(.38f, .75f, .28f) : enemyKind == EnemyKind.Mushroom ? new Color(.72f, .32f, .72f) : new Color(.66f, .45f, .18f);
            obj.AddComponent<PlaceholderVisual>().Configure(color, new Vector2(.7f, .9f), 3);
            Rigidbody2D body = obj.AddComponent<Rigidbody2D>();
            body.gravityScale = 3.2f;
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            obj.AddComponent<BoxCollider2D>().size = Vector2.one;
            obj.AddComponent<Hurtbox2D>();
            obj.AddComponent<StatusController>();
            EnemyController enemy = enemyKind == EnemyKind.Vine ? obj.AddComponent<VineEnemy>() : enemyKind == EnemyKind.Mushroom ? obj.AddComponent<MushroomEnemy>() : obj.AddComponent<BeetleEnemy>();
            float health = enemyKind == EnemyKind.Vine ? 25f : enemyKind == EnemyKind.Mushroom ? 18f : 35f;
            float moveSpeed = enemyKind == EnemyKind.Vine ? 1.8f : enemyKind == EnemyKind.Mushroom ? .6f : 3.2f;
            float attackDamage = enemyKind == EnemyKind.Vine ? 8f : enemyKind == EnemyKind.Mushroom ? 6f : 12f;
            enemy.Configure(enemyKind, player, health, moveSpeed, attackDamage);
            Bounds bounds = GetComponent<Collider2D>().bounds;
            enemy.SetAllowedBounds(bounds.min.x + .45f, bounds.max.x - .45f);
            enemy.Died += OnEnemyDied;
            enemy.Died += TrySpawnMonsterDrop;
            living.Add(enemy);
        }

        private void TrySpawnMonsterDrop(EnemyController enemy)
        {
            float chance = enemy.Kind == EnemyKind.Vine ? .45f : enemy.Kind == EnemyKind.Mushroom ? .35f : 0f;
            if (chance <= 0f || UnityEngine.Random.value > chance || inventory == null) return;
            GraftDefinition item = ScriptableObject.CreateInstance<GraftDefinition>();
            item.Id = enemy.Kind == EnemyKind.Vine ? "vine_tendril" : "toxic_cap";
            item.Slot = enemy.Kind == EnemyKind.Vine ? GraftSlot.Stem : GraftSlot.Flower;
            item.DisplayName = enemy.Kind == EnemyKind.Vine ? "藤蔓触须" : "毒菌伞";
            GameObject drop = new GameObject(item.DisplayName + " Drop");
            drop.layer = 14;
            drop.transform.position = enemy.transform.position;
            drop.AddComponent<PlaceholderVisual>().Configure(new Color(.95f, .84f, .28f), Vector2.one * .42f, 5);
            BoxCollider2D collider = drop.AddComponent<BoxCollider2D>(); collider.isTrigger = true;
            drop.AddComponent<GraftPickup>().Configure(item, inventory);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<PlayerMotor2D>() != null) LevelFlow.Current?.RequestEncounter(this);
        }

        private void OnEnemyDied(EnemyController enemy)
        {
            enemy.Died -= OnEnemyDied;
            if (!living.Remove(enemy)) return;
            if (GameBootstrap.Instance != null) GameBootstrap.Instance.Session.RegisterEnemyDefeated();
            if (living.Count != 0) return;
            if (order == 0 && waveIndex < RoomOneWaves.Length - 1)
            {
                waveIndex++;
                StartCoroutine(BeginNextWave());
                return;
            }
            CompleteOnce();
        }

        private void SpawnWave()
        {
            EnemyKind[] roster = order == 0 ? RoomOneWaves[waveIndex] : enemies;
            for (int i = 0; i < roster.Length; i++) SpawnEnemy(roster[i], spawnPoints[i % spawnPoints.Length]);
        }

        private IEnumerator BeginNextWave()
        {
            yield return new WaitForSeconds(2f);
            if (!cleared && GameBootstrap.Instance != null && GameBootstrap.Instance.State.Current == GameState.Playing) SpawnWave();
        }

        private void CompleteOnce()
        {
            if (cleared) return;
            cleared = true;
            if (leftGate != null) leftGate.SetActive(false);
            if (rightGate != null) rightGate.SetActive(false);
            SpawnReward();
            Cleared?.Invoke(this);
        }

        private void SpawnReward()
        {
            if (reward == null || inventory == null) return;
            GameObject drop = new GameObject(reward.DisplayName + " Drop");
            drop.layer = 14;
            drop.transform.position = transform.position + Vector3.up * .5f;
            drop.AddComponent<PlaceholderVisual>().Configure(new Color(.95f, .84f, .28f), Vector2.one * .42f, 5);
            BoxCollider2D collider = drop.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = Vector2.one;
            drop.AddComponent<GraftPickup>().Configure(reward, inventory);
        }
    }
}
