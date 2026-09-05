using System;
using System.Collections;
using UnityEngine;

namespace PlantSpirit.GGJ
{
    public enum EnemyKind { Vine, Mushroom, Beetle }

    [RequireComponent(typeof(Hurtbox2D), typeof(Collider2D))]
    public class EnemyController : MonoBehaviour, IDamageReceiver
    {
        [SerializeField] protected EnemyKind kind;
        [SerializeField] protected float maxHealth = 30f;
        [SerializeField] protected float speed = 2f;
        [SerializeField] protected float damage = 12f;
        [SerializeField] protected Transform target;
        public EnemyKind Kind => kind;
        public Transform Target => target;
        public bool Dead { get; private set; }
        public float CurrentHealth { get; private set; }
        public event Action<EnemyController> Died;
        public event Action<EnemyController> Damaged;
        public event Action<EnemyController> AttackStarted;
        protected Rigidbody2D body;
        protected StatusController status;
        protected float cooldown;
        protected float minX = float.NegativeInfinity;
        protected float maxX = float.PositiveInfinity;
        private const int TerrainLayerMask = 1 << 8;
        private const float ContactDamageInterval = .9f;
        private float nextContactDamageTime;
        private Collider2D collisionShape;
        private SpriteRenderer visual;
        private Color baseColor;

        protected virtual void Awake()
        {
            CurrentHealth = maxHealth;
            body = GetComponent<Rigidbody2D>();
            collisionShape = GetComponent<Collider2D>();
            status = GetComponent<StatusController>();
            visual = GetComponent<SpriteRenderer>();
            if (visual != null) baseColor = visual.color;
            GetComponent<Hurtbox2D>().Receiver = this;
        }
        public void SetAllowedBounds(float minimumX, float maximumX) { minX = minimumX; maxX = maximumX; }
        public void Configure(EnemyKind type, Transform player, float health, float moveSpeed, float attackDamage)
        {
            kind = type; target = player; maxHealth = health; CurrentHealth = health; speed = moveSpeed; damage = attackDamage;
            EnemyArtController art = GetComponent<EnemyArtController>();
            if (art == null) art = gameObject.AddComponent<EnemyArtController>();
            art.Configure(this);
        }
        protected virtual void Update()
        {
            if (Dead || GameBootstrap.Instance == null || GameBootstrap.Instance.State.Current != GameState.Playing || target == null) return;
            cooldown -= Time.deltaTime;
            TickBehaviour();
        }
        protected virtual void TickBehaviour()
        {
            float distance = Vector2.Distance(transform.position, target.position);
            if (distance > 1.05f) MoveTowardTarget();
            else
            {
                StopHorizontalMovement();
                if (cooldown <= 0f) StartCoroutine(AttackSequence(.45f, .35f));
            }
        }
        protected void MoveTowardTarget()
        {
            float modifier = 1f - (status == null ? 0f : status.SlowPercent);
            float velocity = Mathf.Sign(target.position.x - transform.position.x) * speed * modifier;
            if ((transform.position.x <= minX && velocity < 0f) || (transform.position.x >= maxX && velocity > 0f)) velocity = 0f;
            if (body != null)
            {
                float horizontalDistance = Mathf.Abs(target.position.x - transform.position.x);
                bool targetIsAbove = target.position.y > transform.position.y + .65f;
                bool grounded = collisionShape != null && collisionShape.IsTouchingLayers(TerrainLayerMask);
                if (grounded && targetIsAbove && horizontalDistance < 2.5f)
                    body.velocity = new Vector2(velocity, 8.5f);
                else body.velocity = new Vector2(velocity, body.velocity.y);
            }
            else transform.position += Vector3.right * velocity * Time.deltaTime;
        }
        protected IEnumerator AttackSequence(float startup, float recovery)
        {
            cooldown = startup + recovery + .45f;
            NotifyAttackStarted();
            SetTelegraph(true);
            yield return new WaitForSeconds(startup);
            SetTelegraph(false);
            if (!Dead && target != null && Vector2.Distance(transform.position, target.position) < 1.25f)
                target.GetComponent<Hurtbox2D>()?.Receive(new DamageInfo { AttackInstanceId = GetInstanceID() + Time.frameCount, Amount = damage, Knockback = (target.position - transform.position).normalized * 4f, Source = gameObject });
            yield return new WaitForSeconds(recovery);
        }
        protected void SetTelegraph(bool enabled)
        {
            if (visual != null) visual.color = enabled ? new Color(1f, .3f, .18f) : baseColor;
            GetComponent<EnemyArtController>()?.SetTelegraph(enabled);
        }
        public bool TryReceive(DamageInfo info)
        {
            if (Dead) return false;
            CurrentHealth -= info.Amount;
            GameAudio.Play(AudioCue.EnemyHurt);
            if (body != null) body.AddForce(info.Knockback, ForceMode2D.Impulse);
            if (CurrentHealth <= 0f) Die();
            else Damaged?.Invoke(this);
            return true;
        }
        protected void Die()
        {
            if (Dead) return;
            Dead = true;
            SetTelegraph(false);
            Collider2D hitCollider = GetComponent<Collider2D>();
            if (hitCollider != null) hitCollider.enabled = false;
            if (body != null) { body.velocity = Vector2.zero; body.simulated = false; }
            Died?.Invoke(this);
            EnemyArtController art = GetComponent<EnemyArtController>();
            Destroy(gameObject, art == null ? .15f : art.DeathDuration);
        }

        protected void StopHorizontalMovement()
        {
            if (body != null) body.velocity = new Vector2(0f, body.velocity.y);
        }

        protected void NotifyAttackStarted()
        {
            if (kind == EnemyKind.Vine) GameAudio.Play(AudioCue.EnemyVineTelegraph);
            AttackStarted?.Invoke(this);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (Dead || GameBootstrap.Instance == null || GameBootstrap.Instance.State.Current != GameState.Playing || Time.time < nextContactDamageTime) return;
            Hurtbox2D hurtbox = collision.collider.GetComponentInParent<Hurtbox2D>();
            if (hurtbox == null || hurtbox.GetComponentInParent<PlayerHealth>() == null) return;
            nextContactDamageTime = Time.time + ContactDamageInterval;
            Vector2 knockback = ((Vector2)hurtbox.transform.position - (Vector2)transform.position).normalized * 3.5f;
            hurtbox.Receive(new DamageInfo
            {
                AttackInstanceId = GetInstanceID() ^ Time.frameCount,
                Amount = damage,
                Knockback = knockback,
                Source = gameObject,
                Type = DamageType.Physical
            });
        }
    }

    public sealed class VineEnemy : EnemyController
    {
        protected override void TickBehaviour() { base.TickBehaviour(); }
    }

    public sealed class MushroomEnemy : EnemyController
    {
        protected override void TickBehaviour()
        {
            float distance = Vector2.Distance(transform.position, target.position);
            if (distance < 2.5f)
            {
                float velocity = -Mathf.Sign(target.position.x - transform.position.x) * speed * .5f;
                if ((transform.position.x <= minX && velocity < 0f) || (transform.position.x >= maxX && velocity > 0f)) velocity = 0f;
                if (body != null) body.velocity = new Vector2(velocity, body.velocity.y);
                else transform.position += Vector3.right * velocity * Time.deltaTime;
            }
            if (distance <= 3f && cooldown <= 0f) StartCoroutine(Shot());
        }
        private IEnumerator Shot()
        {
            cooldown = 4f;
            NotifyAttackStarted();
            SetTelegraph(true);
            yield return new WaitForSeconds(.65f);
            SetTelegraph(false);
            if (Dead || target == null) yield break;
            GameAudio.Play(AudioCue.EnemyMushroomShoot);
            Vector2 direction = ((Vector2)(target.position - transform.position)).normalized;
            GameObject cloud = new GameObject("MushroomPoisonCloud");
            cloud.transform.position = (Vector2)transform.position + direction * 1.1f;
            cloud.AddComponent<PlaceholderVisual>().Configure(new Color(.65f, .18f, .7f, .42f), new Vector2(2.4f, 1.25f), 2);
            PoisonZone poison = cloud.AddComponent<PoisonZone>();
            poison.Activate(damage, 2f, 0f, 1 << 9);
        }
    }

    public sealed class BeetleEnemy : EnemyController
    {
        private bool charging;
        private float chargeDirection;
        protected override void TickBehaviour()
        {
            if (charging)
            {
                if (body != null) body.velocity = new Vector2(chargeDirection * 8f, body.velocity.y);
                else transform.position += Vector3.right * chargeDirection * 8f * Time.deltaTime;
                return;
            }
            bool targetIsAbove = target.position.y > transform.position.y + .65f;
            if (targetIsAbove && Mathf.Abs(target.position.x - transform.position.x) < 2.5f) MoveTowardTarget();
            else if (cooldown <= 0f && Mathf.Abs(target.position.x - transform.position.x) < 6f) StartCoroutine(Charge());
            else MoveTowardTarget();
        }
        private IEnumerator Charge()
        {
            cooldown = 3f;
            NotifyAttackStarted();
            GameAudio.Play(AudioCue.EnemyBeetleCharge);
            SetTelegraph(true);
            yield return new WaitForSeconds(.8f);
            SetTelegraph(false);
            if (Dead || target == null) yield break;
            charging = true; chargeDirection = Mathf.Sign(target.position.x - transform.position.x);
            float until = Time.time + .55f;
            while (Time.time < until && !Dead && charging)
            {
                if (transform.position.x <= minX || transform.position.x >= maxX) { StopCharge(1.1f); break; }
                if (target != null && Vector2.Distance(transform.position, target.position) < .8f)
                    target.GetComponent<Hurtbox2D>()?.Receive(new DamageInfo { AttackInstanceId = GetInstanceID() + Time.frameCount, Amount = damage, Source = gameObject });
                yield return null;
            }
            charging = false;
        }
        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (charging && collision.gameObject.layer == 8) StopCharge(1.1f);
        }
        private void StopCharge(float stunSeconds)
        {
            charging = false;
            cooldown = Mathf.Max(cooldown, stunSeconds);
            if (body != null) body.velocity = new Vector2(0f, body.velocity.y);
        }
    }
}
