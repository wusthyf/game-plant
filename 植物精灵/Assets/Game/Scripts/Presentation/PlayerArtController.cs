using UnityEngine;

namespace PlantSpirit.GGJ
{
    public sealed class PlayerArtController : MonoBehaviour
    {
        private PlayerMotor2D motor;
        private PlayerCombat combat;
        private PlayerHealth health;
        private Rigidbody2D body;
        private SpriteSequence2D visual;
        private Sprite[] idle;
        private Sprite[] attack;
        private Sprite[] skill;
        private bool oneShot;
        private float previousHealth;
        private float flashUntil;
        private float hurtAnimationUntil;

        public bool HasArt => visual != null && visual.HasFrames;

        private void Awake()
        {
            motor = GetComponent<PlayerMotor2D>();
            combat = GetComponent<PlayerCombat>();
            health = GetComponent<PlayerHealth>();
            body = GetComponent<Rigidbody2D>();
            attack = ArtResources2D.LoadSequence("Player/AttackA");
            skill = ArtResources2D.LoadSequence("Player/AttackB");
            if (attack.Length == 0) return;
            idle = attack.Length > 1 ? new[] { attack[0], attack[attack.Length - 1] } : attack;
            visual = SpriteSequence2D.Create(transform, "PlayerArt", 1.35f, 4, true);
            visual.PlayLoop(idle, 2f);
        }

        private void OnEnable()
        {
            if (combat != null)
            {
                combat.AttackStarted += OnAttack;
                combat.SkillStarted += OnSkill;
            }
            if (health != null)
            {
                health.Changed += OnHealthChanged;
                health.Hurt += OnHurt;
            }
        }

        private void Start() => previousHealth = health == null ? 0f : health.Current;

        private void OnDisable()
        {
            if (combat != null)
            {
                combat.AttackStarted -= OnAttack;
                combat.SkillStarted -= OnSkill;
            }
            if (health != null)
            {
                health.Changed -= OnHealthChanged;
                health.Hurt -= OnHurt;
            }
        }

        private void Update()
        {
            if (visual == null || motor == null) return;
            visual.SetFacingLeft(motor.Facing < 0);
            visual.SetTint(Time.unscaledTime < flashUntil ? new Color(1f, .45f, .35f) : Color.white);

            if (Time.unscaledTime < hurtAnimationUntil)
            {
                float recoil = Mathf.Clamp01((hurtAnimationUntil - Time.unscaledTime) / .22f);
                visual.SetWorldOffset(new Vector2(-motor.Facing * .16f * recoil, Mathf.Sin(Time.unscaledTime * 42f) * .035f));
                visual.SetLocalAngle(motor.Facing > 0 ? 13f * recoil : -13f * recoil);
                return;
            }

            if (oneShot && hurtAnimationUntil > 0f)
            {
                hurtAnimationUntil = 0f;
                ResumeIdle();
            }

            float speed = body == null ? 0f : Mathf.Abs(body.velocity.x);
            float bob = !oneShot && motor.Grounded && speed > .15f ? Mathf.Sin(Time.time * 13f) * .045f : 0f;
            visual.SetWorldOffset(Vector2.up * bob);
            if (motor.IsDashing) visual.SetLocalAngle(motor.Facing > 0 ? -9f : 9f);
            else if (!motor.Grounded) visual.SetLocalAngle(motor.Facing > 0 ? -4f : 4f);
            else visual.SetLocalAngle(0f);
        }

        private void OnAttack()
        {
            if (visual == null || attack.Length == 0) return;
            oneShot = true;
            visual.PlayOnce(attack, 16f, ResumeIdle);
        }

        private void OnSkill()
        {
            if (visual == null || skill.Length == 0) return;
            oneShot = true;
            visual.PlayOnce(skill, 15f, ResumeIdle);
        }

        private void ResumeIdle()
        {
            oneShot = false;
            visual.PlayLoop(idle, 2f);
        }

        private void OnHealthChanged(float current)
        {
            if (previousHealth > 0f && current < previousHealth) flashUntil = Time.unscaledTime + .12f;
            previousHealth = current;
        }

        private void OnHurt()
        {
            if (visual == null || health == null || health.Dead) return;
            oneShot = true;
            hurtAnimationUntil = Time.unscaledTime + .22f;
            visual.Show(idle.Length > 0 ? idle[0] : null);
        }
    }
}
