using UnityEngine;

namespace PlantSpirit.GGJ
{
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class PlayerMotor2D : MonoBehaviour
    {
        [SerializeField] private PlayerConfig config;
        [SerializeField] private LayerMask groundMask;
        [SerializeField] private Transform groundProbe;
        public bool Grounded { get; private set; }
        public bool IsDashing => dashRemaining > 0f;
        public int Facing { get; private set; } = 1;
        public float DashIFramesRemaining => dashIFrames;
        public bool IsProjectileShieldActive => IsDashing && dashIFrames > 0f && GameBootstrap.Instance != null && GameBootstrap.Instance.Session.Get(GraftSlot.Root)?.BlocksProjectilesDuringDash == true;
        private Rigidbody2D body;
        private float moveInput;
        private float coyote;
        private float jumpBuffer;
        private float dashRemaining;
        private float dashCooldown;
        private float dashIFrames;
        private bool controlLocked;
        private PlayerHealth health;
        private InputReader input;

        public bool InputBound => input != null;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.freezeRotation = true;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        private void Start()
        {
            if (GetComponent<PlayerArtController>() == null) gameObject.AddComponent<PlayerArtController>();
        }

        private void OnEnable() => BindInput();

        private void OnDisable()
        {
            if (input == null) return;
            input.Move -= OnMove;
            input.Jump -= BufferJump;
            input.Dash -= OnDash;
            input = null;
        }

        public void Configure(PlayerConfig playerConfig, LayerMask terrainMask, Transform probe)
        {
            config = playerConfig;
            groundMask = terrainMask;
            groundProbe = probe;
        }

        public void SetMove(float value)
        {
            if (!CanControl()) { moveInput = 0f; return; }
            moveInput = Mathf.Clamp(value, -1f, 1f);
            if (moveInput != 0f) Facing = moveInput > 0f ? 1 : -1;
        }
        public void BufferJump() { if (CanControl() && !IsDashing) jumpBuffer = config.JumpBufferSeconds; }
        public bool BeginDash()
        {
            if (!CanControl() || dashCooldown > 0f || dashRemaining > 0f) return false;
            dashRemaining = config.DashSeconds;
            dashIFrames = config.DashInvincibleSeconds;
            dashCooldown = config.DashCooldown;
            return true;
        }

        public void LockControl()
        {
            controlLocked = true;
            moveInput = 0f;
            jumpBuffer = 0f;
            dashRemaining = 0f;
            dashIFrames = 0f;
            if (body != null) body.velocity = Vector2.zero;
        }

        private void Update()
        {
            if (input == null) BindInput();
            jumpBuffer -= Time.deltaTime;
            dashRemaining -= Time.deltaTime;
            dashCooldown -= Time.deltaTime;
            dashIFrames -= Time.deltaTime;
            if (jumpBuffer > 0f && coyote > 0f)
            {
                body.velocity = new Vector2(body.velocity.x, config.JumpVelocity);
                GameAudio.Play(AudioCue.PlayerJump);
                jumpBuffer = 0f;
                coyote = 0f;
            }
        }

        private void FixedUpdate()
        {
            if (!CanControl()) moveInput = 0f;
            Vector2 probe = groundProbe == null ? (Vector2)transform.position + Vector2.down * .52f : groundProbe.position;
            Grounded = Physics2D.OverlapBox(probe, new Vector2(.44f, .12f), 0f, groundMask) != null && body.velocity.y <= .5f;
            if (Grounded) coyote = config.CoyoteSeconds; else coyote -= Time.fixedDeltaTime;
            float target = IsDashing ? Facing * (config.DashDistance / config.DashSeconds) : moveInput * config.MoveSpeed;
            float acceleration = Grounded
                ? (Mathf.Abs(moveInput) > .01f || IsDashing ? config.GroundAcceleration : config.GroundDeceleration)
                : config.AirAcceleration;
            float verticalVelocity = Mathf.Max(body.velocity.y, -config.MaxFallSpeed);
            body.velocity = new Vector2(Mathf.MoveTowards(body.velocity.x, target, acceleration * Time.fixedDeltaTime), verticalVelocity);
        }

        private bool CanControl()
        {
            if (health == null) health = GetComponent<PlayerHealth>();
            if (controlLocked || (health != null && health.Dead)) return false;
            return GameBootstrap.Instance == null || GameBootstrap.Instance.State.Current == GameState.Playing;
        }

        private void BindInput()
        {
            if (input != null) return;
            input = FindObjectOfType<InputReader>();
            if (input == null) return;
            input.Move += OnMove;
            input.Jump += BufferJump;
            input.Dash += OnDash;
        }

        private void OnMove(Vector2 value) => SetMove(value.x);
        private void OnDash() => BeginDash();
    }
}
