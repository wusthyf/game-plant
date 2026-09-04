using System;
using System.Collections;
using UnityEngine;

namespace PlantSpirit.GGJ
{
    public sealed class PlayerCombat : MonoBehaviour
    {
        [SerializeField] private PlayerMotor2D motor;
        [SerializeField] private Hitbox2D hitbox;
        [SerializeField] private AttackDefinition defaultAttack;
        [SerializeField] private AttackDefinition defaultSkill;
        [SerializeField] private AttackDefinition vineAttack;
        [SerializeField] private AttackDefinition poisonSkill;
        private int nextAttackId;
        private bool locked;
        private bool actionsLocked;
        private float attackReadyAt;
        private float skillReadyAt;
        private InputReader input;

        public bool InputBound => input != null;
        public event Action AttackStarted;
        public event Action SkillStarted;

        private void OnEnable() => BindInput();

        private void OnDisable()
        {
            if (input == null) return;
            input.Attack -= OnAttack;
            input.Skill -= OnSkill;
            input = null;
        }

        public void Configure(PlayerMotor2D playerMotor, Hitbox2D playerHitbox, AttackDefinition attack, AttackDefinition skill, AttackDefinition vine, AttackDefinition poison)
        {
            motor = playerMotor;
            hitbox = playerHitbox;
            defaultAttack = attack;
            defaultSkill = skill;
            vineAttack = vine;
            poisonSkill = poison;
        }

        public bool RequestAttack()
        {
            if (!CanAct() || Time.time < attackReadyAt) return false;
            AttackDefinition definition = CurrentStem() == "vine_tendril" ? vineAttack : defaultAttack;
            if (definition == null || hitbox == null || motor == null) return false;
            AttackStarted?.Invoke();
            GameAudio.Play(definition == vineAttack ? AudioCue.VineSwing : AudioCue.PlayerAttackSwing);
            StartCoroutine(ExecuteMelee(definition));
            return true;
        }

        public bool RequestSkill()
        {
            if (!CanAct() || Time.time < skillReadyAt) return false;
            AttackDefinition definition = CurrentFlower() == "toxic_cap" ? poisonSkill : defaultSkill;
            if (definition == null || motor == null) return false;
            SkillStarted?.Invoke();
            StartCoroutine(ExecuteSkill(definition));
            return true;
        }

        public void RefreshLoadout() { }

        public void LockActions()
        {
            actionsLocked = true;
            StopAllCoroutines();
            hitbox?.Close();
            locked = false;
        }

        private IEnumerator ExecuteMelee(AttackDefinition definition)
        {
            locked = true;
            yield return new WaitForSeconds(definition.Startup);
            if (!CanContinueAction()) yield break;
            int instance = ++nextAttackId;
            hitbox.Open(instance);
            float direction = motor.Facing;
            Vector2 center = (Vector2)transform.position + new Vector2(direction * definition.Offset.x, definition.Offset.y);
            int hitCount = hitbox.StrikeBox(center, definition.Size, new DamageInfo { Amount = definition.Damage, Knockback = new Vector2(direction * 4f, 1f), Source = gameObject }, definition.MaxTargets);
            if (hitCount > 0)
            {
                GameAudio.Play(AudioCue.PlayerAttackHit);
                WorldArtPresentation2D.SpawnBurst(center, .72f);
            }
            yield return new WaitForSeconds(definition.Active);
            hitbox.Close();
            yield return new WaitForSeconds(definition.Recovery);
            attackReadyAt = Time.time + definition.Cooldown;
            locked = false;
        }

        private IEnumerator ExecuteSkill(AttackDefinition definition)
        {
            locked = true;
            yield return new WaitForSeconds(definition.Startup);
            if (!CanContinueAction()) yield break;
            if (definition.Executor == AttackExecutorType.PoisonZone)
            {
                GameAudio.Play(AudioCue.PoisonCast);
                GameObject zone = new GameObject("PoisonZone");
                zone.transform.position = transform.position + Vector3.right * motor.Facing * definition.Range;
                zone.AddComponent<PlaceholderVisual>().Configure(new Color(.58f, .2f, .68f, .42f), new Vector2(6f, 1.7f), 1);
                WorldArtPresentation2D.AttachPoisonZone(zone);
                PoisonZone poison = zone.AddComponent<PoisonZone>();
                poison.Activate(definition.Damage, 3f, .3f, 1 << 12);
            }
            else
            {
                GameObject projectile = new GameObject("SeedProjectile");
                projectile.layer = 10;
                projectile.transform.position = transform.position + Vector3.right * motor.Facing * .5f;
                projectile.AddComponent<PlaceholderVisual>().Configure(new Color(.85f, .95f, .3f), Vector2.one * .24f, 5);
                Projectile2D seed = projectile.AddComponent<Projectile2D>();
                seed.Launch(new Vector2(motor.Facing * 8.5f, 0f), definition.Damage, 1.2f, 1 << 12, ++nextAttackId);
            }
            yield return new WaitForSeconds(definition.Recovery);
            skillReadyAt = Time.time + definition.Cooldown;
            locked = false;
        }

        private string CurrentStem() { GraftDefinition graft = GameBootstrap.Instance?.Session.Get(GraftSlot.Stem); return graft == null ? string.Empty : graft.Id; }
        private string CurrentFlower() { GraftDefinition graft = GameBootstrap.Instance?.Session.Get(GraftSlot.Flower); return graft == null ? string.Empty : graft.Id; }

        private bool CanAct()
        {
            return !actionsLocked && !locked && motor != null && !motor.IsDashing && GetComponent<PlayerHealth>()?.Dead != true &&
                   (GameBootstrap.Instance == null || GameBootstrap.Instance.State.Current == GameState.Playing);
        }

        private bool CanContinueAction()
        {
            if (!actionsLocked && motor != null && GetComponent<PlayerHealth>()?.Dead != true &&
                (GameBootstrap.Instance == null || GameBootstrap.Instance.State.Current == GameState.Playing)) return true;
            hitbox?.Close();
            locked = false;
            return false;
        }

        private void BindInput()
        {
            if (input != null) return;
            input = FindObjectOfType<InputReader>();
            if (input == null) return;
            input.Attack += OnAttack;
            input.Skill += OnSkill;
        }

        private void OnAttack() => RequestAttack();
        private void OnSkill() => RequestSkill();
    }
}
