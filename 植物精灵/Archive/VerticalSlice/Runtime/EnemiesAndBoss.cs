using System;
using System.Collections;
using UnityEngine;

namespace PlantSpirit.VerticalSlice
{
    public sealed class EnemyRuntimeData
    {
        public EnemyKind Kind; public string Name; public float Health; public float Speed; public float Damage; public float Range; public float AttackCooldown; public int Essence; public float DropChance; public TraitDefinition Drop; public float Width; public float Height; public Color Color;
        public static EnemyRuntimeData For(EnemyKind kind)
        {
            switch (kind)
            {
                case EnemyKind.Vine: return Create(kind, "腐化藤蔓怪", 25, 1.8f, 8, 1.2f, 1.5f, 5, .45f, "vine_tendril", .8f, 1f, new Color(.25f, .58f, .22f));
                case EnemyKind.Mushroom: return Create(kind, "毒孢蘑菇", 18, .6f, 6, 6f, 2.5f, 6, .35f, "toxic_cap", .7f, .8f, new Color(.55f, .2f, .66f));
                case EnemyKind.Beetle: return Create(kind, "废铁甲虫", 35, 3.2f, 12, 1f, 1.8f, 8, .3f, "iron_root", 1f, .65f, new Color(.42f, .48f, .52f));
                case EnemyKind.Treant: return Create(kind, "腐化树人", 55, .9f, 16, 2f, 2.8f, 12, .55f, "treant_arm", 1.2f, 1.8f, new Color(.35f, .26f, .12f));
                case EnemyKind.Berry: return Create(kind, "自爆浆果", 10, 3.8f, 25, .75f, 0, 4, .4f, "burst_pod", .55f, .55f, new Color(.9f, .18f, .22f));
                default: return Create(kind, "荆棘种荚", 16, 0, 10, 5f, 2.2f, 5, .2f, "cactus_stem", .7f, .7f, new Color(.72f, .2f, .35f));
            }
        }
        private static EnemyRuntimeData Create(EnemyKind kind, string name, float hp, float speed, float damage, float range, float cooldown, int essence, float chance, string drop, float width, float height, Color color)
        {
            return new EnemyRuntimeData { Kind = kind, Name = name, Health = hp, Speed = speed, Damage = damage, Range = range, AttackCooldown = cooldown, Essence = essence, DropChance = chance, Drop = VerticalSliceCatalog.FindTrait(drop), Width = width, Height = height, Color = color };
        }
    }

    public sealed class PlatformEnemy : MonoBehaviour
    {
        private EnemyRuntimeData data;
        private float health;
        private float attackCooldown;
        private float chargeWindup;
        private float poisonTime;
        private float poisonDps;
        private bool charging;
        private bool dead;
        public EnemyKind Kind => data.Kind;

        public void Setup(EnemyRuntimeData definition) { data = definition; health = definition.Health; }
        private void Update()
        {
            if (dead || VerticalSliceRuntime.Instance.Player == null || VerticalSliceRuntime.Instance.IsPaused || VerticalSliceRuntime.Instance.IsChoosingEvolution) return;
            PlatformPlayer player = VerticalSliceRuntime.Instance.Player;
            float dx = player.transform.position.x - transform.position.x;
            float distance = Mathf.Abs(dx);
            attackCooldown -= Time.deltaTime;
            if (poisonTime > 0) { poisonTime -= Time.deltaTime; Hit(poisonDps * Time.deltaTime, 0); }
            if (data.Kind == EnemyKind.Mushroom || data.Kind == EnemyKind.ThornPod)
            {
                if (distance <= data.Range && attackCooldown <= 0) { VerticalSliceRuntime.Instance.SpawnProjectile(transform.position, new Vector2(Mathf.Sign(dx) * 5f, 1f), data.Damage, true, data.Color); attackCooldown = data.AttackCooldown; }
                return;
            }
            if (data.Kind == EnemyKind.Beetle)
            {
                if (!charging && distance < 5 && attackCooldown <= 0) { charging = true; chargeWindup = .4f; }
                if (charging) { chargeWindup -= Time.deltaTime; if (chargeWindup <= 0) { transform.position += Vector3.right * Mathf.Sign(dx) * 8f * Time.deltaTime; if (distance < .9f) { player.TakeDamage(data.Damage); charging = false; attackCooldown = data.AttackCooldown; } } }
                else Move(dx);
                return;
            }
            if (data.Kind == EnemyKind.Berry)
            {
                Move(dx);
                if (distance <= data.Range) { player.TakeDamage(data.Damage); Die(false); }
                return;
            }
            Move(dx);
            if (distance <= data.Range && Mathf.Abs(player.transform.position.y - transform.position.y) < 1.4f && attackCooldown <= 0) { player.TakeDamage(data.Damage); attackCooldown = data.AttackCooldown; }
        }
        private void Move(float dx) { transform.position += Vector3.right * Mathf.Sign(dx) * data.Speed * Time.deltaTime; }
        public void ApplyPoison(float dps, float duration) { poisonDps = dps; poisonTime = duration; }
        public void Hit(float damage, float knockback)
        {
            if (dead) return;
            health -= damage;
            transform.position += Vector3.right * knockback * Time.deltaTime;
            if (health <= 0) Die(true);
        }
        private void Die(bool reward)
        {
            if (dead) return;
            dead = true;
            VerticalSliceRuntime.Instance.RemoveEnemy(this);
            if (reward)
            {
                VerticalSliceRuntime.Instance.Player.GainEssence(data.Essence);
                GgjGameFlow.Current?.NotifyEnemyDefeated(data.Kind, transform.position, data.Drop);
            }
            Destroy(gameObject);
        }
    }

    public sealed class DamageProjectile : MonoBehaviour
    {
        private Vector2 velocity; private float damage; private bool hostile; private float life = 4f;
        public void Setup(Vector2 initialVelocity, float value, bool isHostile) { velocity = initialVelocity; damage = value; hostile = isHostile; }
        private void Update()
        {
            transform.position += (Vector3)(velocity * Time.deltaTime); velocity.y -= 3f * Time.deltaTime; life -= Time.deltaTime;
            if (hostile)
            {
                PlatformPlayer player = VerticalSliceRuntime.Instance.Player;
                if (Vector2.Distance(transform.position, player.transform.position) < .45f) { player.TakeDamage(damage); DestroyProjectile(); }
            }
            else
            {
                foreach (PlatformEnemy enemy in VerticalSliceRuntime.Instance.Enemies) if (enemy != null && Vector2.Distance(transform.position, enemy.transform.position) < .42f) { enemy.Hit(damage, Mathf.Sign(velocity.x) * 4); DestroyProjectile(); break; }
                if (CorruptedAncient.Current != null && Vector2.Distance(transform.position, CorruptedAncient.Current.transform.position) < .75f) { CorruptedAncient.Current.Hit(damage); DestroyProjectile(); }
            }
            if (life <= 0 || Mathf.Abs(transform.position.x) > 15 || transform.position.y < -6) DestroyProjectile();
        }
        private void DestroyProjectile() { VerticalSliceRuntime.Instance.RemoveProjectile(this); Destroy(gameObject); }
    }

    public sealed class TraitPickup : MonoBehaviour
    {
        private TraitDefinition trait;
        public void Setup(TraitDefinition definition) { trait = definition; }
        private void Update()
        {
            if (Vector2.Distance(transform.position, VerticalSliceRuntime.Instance.Player.transform.position) < .75f) { VerticalSliceRuntime.Instance.Player.AddToInventory(trait); GgjGameFlow.Current?.NotifyPickup(trait); Destroy(gameObject); }
        }
    }

    public sealed class CorruptedAncient : MonoBehaviour
    {
        public static CorruptedAncient Current { get; private set; }
        public float Health { get; private set; } = 150f;
        public int Phase { get; private set; } = 1;
        public bool Defeated { get; private set; }
        private float attackTimer;
        private bool summonedPhaseOne;
        private bool summonedPhaseTwo;

        public void Initialize() { Current = this; }
        private void Update()
        {
            if (Defeated) return;
            Phase = Health <= 45 ? 3 : Health <= 90 ? 2 : 1;
            if (Phase == 1 && !summonedPhaseOne) { summonedPhaseOne = true; VerticalSliceRuntime.Instance.SpawnEnemy(EnemyKind.Vine, new Vector2(-3, -2.8f)); VerticalSliceRuntime.Instance.SpawnEnemy(EnemyKind.Vine, new Vector2(3, -2.8f)); }
            if (Phase == 2 && !summonedPhaseTwo) { summonedPhaseTwo = true; VerticalSliceRuntime.Instance.SpawnEnemy(EnemyKind.Treant, new Vector2(2, -2.8f)); }
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0) { StartCoroutine(AttackSequence()); attackTimer = Phase == 1 ? 2.5f : Phase == 2 ? 1.8f : 1.2f; }
        }
        public void Hit(float damage)
        {
            if (Defeated) return;
            Health -= damage;
            if (Health <= 0) { Defeated = true; Current = null; GetComponent<SpriteRenderer>().color = new Color(.5f, .9f, .35f); VerticalSliceRuntime.Instance.GetComponent<RoomFlow>().CompleteBoss(); }
        }
        private IEnumerator AttackSequence()
        {
            PlatformPlayer player = VerticalSliceRuntime.Instance.Player;
            for (int i = 0; i < Phase; i++)
            {
                float offset = (i - (Phase - 1) * .5f) * 15f;
                Vector2 dir = Quaternion.Euler(0, 0, offset) * ((Vector2)player.transform.position - (Vector2)transform.position).normalized;
                VerticalSliceRuntime.Instance.SpawnProjectile(transform.position, dir * (4f + Phase * .5f), 9f, true, new Color(.78f, .18f, .25f));
            }
            yield return new WaitForSeconds(Phase == 1 ? 1.2f : Phase == 2 ? 1f : .7f);
            int spikes = Phase;
            for (int i = 0; i < spikes; i++)
            {
                Vector2 telegraph = player.transform.position;
                GameObject warning = VerticalSliceRuntime.Instance.Visual("根刺预警", telegraph, Vector2.one * 1.2f, new Color(1, .2f, .25f, .45f), 2);
                yield return new WaitForSeconds(Phase == 1 ? 1.2f : Phase == 2 ? 1f : .7f);
                if (Vector2.Distance(player.transform.position, telegraph) < 1.2f) player.TakeDamage(10 + Phase * 2);
                Destroy(warning);
            }
            if (Phase >= 2 && Mathf.Abs(player.transform.position.x - transform.position.x) < (Phase == 2 ? 2f : 3f)) player.TakeDamage(Phase == 2 ? 15 : 18);
            if (Phase == 3) for (int i = 0; i < 8; i++) { Vector2 target = new Vector2(VerticalSliceRuntime.Instance.Random.Range(-80, 81) / 10f, VerticalSliceRuntime.Instance.Random.Range(-28, 26) / 10f); if (Vector2.Distance(target, player.transform.position) < .8f) player.TakeDamage(10); }
        }
    }
}
