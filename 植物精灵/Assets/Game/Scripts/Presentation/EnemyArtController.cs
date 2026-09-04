using UnityEngine;

namespace PlantSpirit.GGJ
{
    public sealed class EnemyArtController : MonoBehaviour
    {
        private EnemyController controller;
        private Rigidbody2D body;
        private SpriteSequence2D visual;
        private Sprite[] idle;
        private Sprite[] walk;
        private Sprite[] run;
        private Sprite[] attackPrimary;
        private Sprite[] attackSecondary;
        private Sprite[] hit;
        private Sprite[] death;
        private bool oneShot;
        private bool dead;
        private bool telegraph;
        private int attackCount;

        public bool HasArt => visual != null && visual.HasFrames;
        public float DeathDuration => death == null || death.Length == 0 ? .15f : death.Length / 10f + .05f;

        public void Configure(EnemyController enemy)
        {
            if (controller != null) Unsubscribe();
            controller = enemy;
            body = GetComponent<Rigidbody2D>();

            string root;
            float height;
            if (enemy.Kind == EnemyKind.Vine)
            {
                root = "Enemies/Vine/";
                height = 1.55f;
                idle = ArtResources2D.LoadSequence(root + "Idle");
                walk = ArtResources2D.LoadSequence(root + "Walk");
                run = ArtResources2D.LoadSequence(root + "Run");
                attackPrimary = ArtResources2D.LoadSequence(root + "Swing");
                attackSecondary = ArtResources2D.LoadSequence(root + "Bite");
            }
            else if (enemy.Kind == EnemyKind.Mushroom)
            {
                root = "Enemies/Mushroom/";
                height = 1.45f;
                idle = ArtResources2D.LoadSequence(root + "Idle");
                walk = ArtResources2D.LoadSequence(root + "Walk");
                run = walk;
                attackPrimary = ArtResources2D.LoadSequence(root + "Attack");
                attackSecondary = attackPrimary;
            }
            else
            {
                return;
            }

            hit = ArtResources2D.LoadSequence(root + "Hit");
            death = ArtResources2D.LoadSequence(root + "Death");
            if (idle.Length == 0) return;
            visual = SpriteSequence2D.Create(transform, "EnemyArt", height, 3, true);
            visual.PlayLoop(idle, 6f);
            Subscribe();
        }

        public void SetTelegraph(bool enabled)
        {
            telegraph = enabled;
            RefreshTint();
        }

        private void Update()
        {
            if (visual == null || controller == null || dead) return;
            float velocity = body == null ? 0f : body.velocity.x;
            bool facingLeft = velocity < -.05f || (Mathf.Abs(velocity) <= .05f && controller.Target != null && controller.Target.position.x < transform.position.x);
            visual.SetFacingLeft(facingLeft);
            if (oneShot) return;

            if (Mathf.Abs(velocity) <= .08f) visual.PlayLoop(idle, 6f);
            else if (controller.Kind == EnemyKind.Vine && controller.Target != null && Mathf.Abs(controller.Target.position.x - transform.position.x) > 3f) visual.PlayLoop(run, 11f);
            else visual.PlayLoop(walk, 8f);
        }

        private void Subscribe()
        {
            controller.AttackStarted += OnAttack;
            controller.Damaged += OnDamaged;
            controller.Died += OnDied;
        }

        private void Unsubscribe()
        {
            controller.AttackStarted -= OnAttack;
            controller.Damaged -= OnDamaged;
            controller.Died -= OnDied;
        }

        private void OnDestroy()
        {
            if (controller != null) Unsubscribe();
        }

        private void OnAttack(EnemyController enemy)
        {
            if (visual == null || dead) return;
            Sprite[] sequence = attackCount++ % 2 == 0 ? attackPrimary : attackSecondary;
            oneShot = true;
            visual.PlayOnce(sequence, 12f, ResumeMovement);
        }

        private void OnDamaged(EnemyController enemy)
        {
            if (visual == null || dead || hit.Length == 0) return;
            oneShot = true;
            visual.PlayOnce(hit, 15f, ResumeMovement);
        }

        private void OnDied(EnemyController enemy)
        {
            if (visual == null) return;
            dead = true;
            oneShot = true;
            telegraph = false;
            RefreshTint();
            visual.PlayOnce(death, 10f);
        }

        private void ResumeMovement()
        {
            if (dead || visual == null) return;
            oneShot = false;
            visual.PlayLoop(idle, 6f);
        }

        private void RefreshTint()
        {
            if (visual != null) visual.SetTint(telegraph ? new Color(1f, .45f, .28f) : Color.white);
        }
    }
}
