using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlantSpirit.VerticalSlice
{
    public sealed class VerticalSliceRuntime : MonoBehaviour
    {
        public const int GroundLayer = 8;
        public const int PlayerLayer = 9;
        public const int EnemyLayer = 10;
        public const int ProjectileLayer = 11;
        public static VerticalSliceRuntime Instance { get; private set; }
        public PlatformPlayer Player { get; private set; }
        public RunRandom Random { get; private set; }
        public readonly List<PlatformEnemy> Enemies = new List<PlatformEnemy>();
        public readonly List<DamageProjectile> Projectiles = new List<DamageProjectile>();
        // Retained for compatibility with the archived vertical-slice prototype; GGJ48H uses GgjGameFlow.
        public RoomFlow Rooms { get; private set; }
        public bool IsPaused { get; private set; }
        public bool IsChoosingEvolution { get; set; }
        public bool IsGameplayActive { get; set; }
        private Sprite square;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (FindObjectOfType<VerticalSliceRuntime>() == null) new GameObject("Vertical Slice Runtime").AddComponent<VerticalSliceRuntime>();
        }

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            int seed = LoadSeedOrCreate();
            Random = new RunRandom(seed);
            VerticalSliceCatalog.Validate();
            Physics2D.IgnoreLayerCollision(PlayerLayer, EnemyLayer, true);
            Physics2D.IgnoreLayerCollision(PlayerLayer, ProjectileLayer, true);
            Physics2D.IgnoreLayerCollision(EnemyLayer, ProjectileLayer, true);
            square = CreateSquare();
            BuildCamera();
            BuildGreybox();
            Player = CreatePlayer();
            gameObject.AddComponent<GgjGameFlow>();
            gameObject.AddComponent<GgjUi>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) { IsPaused = !IsPaused; Time.timeScale = IsPaused ? 0f : 1f; }
        }

        public GameObject Visual(string name, Vector2 position, Vector2 size, Color color, int order)
        {
            GameObject obj = new GameObject(name);
            obj.transform.position = position;
            obj.transform.localScale = size;
            SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = square;
            renderer.color = color;
            renderer.sortingOrder = order;
            return obj;
        }

        public PlatformEnemy SpawnEnemy(EnemyKind kind, Vector2 point)
        {
            EnemyRuntimeData data = EnemyRuntimeData.For(kind);
            GameObject obj = Visual(data.Name, point, new Vector2(data.Width, data.Height), data.Color, 3);
            obj.layer = EnemyLayer;
            BoxCollider2D collider = obj.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            PlatformEnemy enemy = obj.AddComponent<PlatformEnemy>();
            enemy.Setup(data);
            Enemies.Add(enemy);
            return enemy;
        }

        public void SpawnProjectile(Vector2 position, Vector2 velocity, float damage, bool hostile, Color color)
        {
            GameObject obj = Visual(hostile ? "敌方弹幕" : "种子", position, Vector2.one * .22f, color, 5);
            obj.layer = ProjectileLayer;
            DamageProjectile projectile = obj.AddComponent<DamageProjectile>();
            projectile.Setup(velocity, damage, hostile);
            Projectiles.Add(projectile);
        }

        public void SpawnPickup(Vector2 position, TraitDefinition trait)
        {
            GameObject obj = Visual(trait.DisplayName, position, Vector2.one * .32f, trait.Color, 5);
            obj.AddComponent<TraitPickup>().Setup(trait);
        }

        public void RemoveEnemy(PlatformEnemy enemy) => Enemies.Remove(enemy);
        public void RemoveProjectile(DamageProjectile projectile) => Projectiles.Remove(projectile);

        private void BuildCamera()
        {
            Camera camera = Camera.main;
            if (camera == null) { camera = new GameObject("Main Camera").AddComponent<Camera>(); camera.tag = "MainCamera"; }
            camera.orthographic = true;
            camera.orthographicSize = 5.2f;
            camera.backgroundColor = new Color(.07f, .12f, .1f);
            camera.transform.position = new Vector3(0, 0, -10);
            camera.gameObject.AddComponent<FollowCamera>();
        }

        private void BuildGreybox()
        {
            Visual("枯萎森林背景", Vector2.zero, new Vector2(24, 12), new Color(.10f, .16f, .12f), -5);
            CreatePlatform("地面", new Vector2(0, -4), new Vector2(26, .6f));
            CreatePlatform("左训练台", new Vector2(-5.2f, -2.25f), new Vector2(3.4f, .35f));
            CreatePlatform("中训练台", new Vector2(0, -.55f), new Vector2(3f, .35f));
            CreatePlatform("右训练台", new Vector2(5.2f, -1.35f), new Vector2(3.4f, .35f));
            CreatePlatform("树冠平台", new Vector2(0, 1.25f), new Vector2(4.5f, .35f));
        }

        private void CreatePlatform(string name, Vector2 position, Vector2 size)
        {
            GameObject obj = Visual(name, position, size, new Color(.27f, .42f, .25f), 1);
            obj.layer = GroundLayer;
            obj.AddComponent<BoxCollider2D>();
            obj.AddComponent<PlatformSurface>();
        }

        private PlatformPlayer CreatePlayer()
        {
            GameObject obj = Visual("植物精灵", new Vector2(-7, -2.8f), new Vector2(.55f, .9f), new Color(.65f, 1f, .38f), 4);
            obj.layer = PlayerLayer;
            Rigidbody2D body = obj.AddComponent<Rigidbody2D>();
            body.gravityScale = 3.2f;
            body.freezeRotation = true;
            obj.AddComponent<BoxCollider2D>();
            return obj.AddComponent<PlatformPlayer>();
        }

        private Sprite CreateSquare()
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1);
        }

        private static int LoadSeedOrCreate()
        {
            string path = Path.Combine(Application.persistentDataPath, "plant_spirit_run.json");
            if (File.Exists(path))
            {
                try
                {
                    RunSaveData save = JsonUtility.FromJson<RunSaveData>(File.ReadAllText(path));
                    if (save != null && save.seed != 0) return save.seed;
                }
                catch (Exception exception) { Debug.LogWarning("PlantSpirit save could not be read: " + exception.Message); }
            }
            return Environment.TickCount;
        }
    }

    public sealed class PlatformSurface : MonoBehaviour { }

    public sealed class FollowCamera : MonoBehaviour
    {
        private void LateUpdate()
        {
            if (VerticalSliceRuntime.Instance == null || VerticalSliceRuntime.Instance.Player == null) return;
            float x = Mathf.Clamp(VerticalSliceRuntime.Instance.Player.transform.position.x, -2f, 2f);
            transform.position = Vector3.Lerp(transform.position, new Vector3(x, .1f, -10), 5f * Time.unscaledDeltaTime);
        }
    }

    public sealed class PlatformPlayer : MonoBehaviour
    {
        public float Health { get; private set; } = 100f;
        public float Corruption { get; private set; }
        public int Essence { get; private set; }
        public int EvolutionCount { get; private set; }
        public readonly Dictionary<TraitSlot, TraitDefinition> Traits = new Dictionary<TraitSlot, TraitDefinition>();
        public readonly List<TraitDefinition> Inventory = new List<TraitDefinition>();
        public readonly List<EvolutionDefinition> Evolutions = new List<EvolutionDefinition>();
        public readonly List<FusionDefinition> Fusions = new List<FusionDefinition>();
        private Rigidbody2D body;
        private SpriteRenderer spriteRenderer;
        private float horizontal;
        private float coyote;
        private float jumpBuffer;
        private float dashTimer;
        private float invulnerability;
        private float attackCooldown;
        private float skillCooldown;
        private int combo;
        private int direction = 1;
        private bool grounded;
        private readonly Collider2D[] groundHits = new Collider2D[4];
        private const float GroundProbeOffset = .48f;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            body.bodyType = RigidbodyType2D.Kinematic;
        }
        private void Update()
        {
            if (!VerticalSliceRuntime.Instance.IsGameplayActive || VerticalSliceRuntime.Instance.IsPaused || VerticalSliceRuntime.Instance.IsChoosingEvolution) return;
            horizontal = Input.GetAxisRaw("Horizontal");
            if (horizontal != 0) direction = horizontal > 0 ? 1 : -1;
            if (Input.GetButtonDown("Jump")) jumpBuffer = .12f;
            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.JoystickButton1)) BeginDash();
            if (Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.JoystickButton0)) LightAttack();
            if (Input.GetKeyDown(KeyCode.K) || Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.JoystickButton2)) UseSkill();
            if (Input.GetKeyDown(KeyCode.C)) AbsorbCorruption();
            coyote -= Time.deltaTime; jumpBuffer -= Time.deltaTime; dashTimer -= Time.deltaTime; invulnerability -= Time.deltaTime; attackCooldown -= Time.deltaTime; skillCooldown -= Time.deltaTime;
            spriteRenderer.color = invulnerability > 0f ? Color.white : new Color(.65f, 1f, .38f);
            if (jumpBuffer > 0 && coyote > 0) { body.velocity = new Vector2(body.velocity.x, 12.5f); jumpBuffer = 0; coyote = 0; grounded = false; }
        }

        private void FixedUpdate()
        {
            if (!VerticalSliceRuntime.Instance.IsGameplayActive || VerticalSliceRuntime.Instance.IsPaused || VerticalSliceRuntime.Instance.IsChoosingEvolution) return;
            UpdateGroundedState();
            float targetSpeed = dashTimer > 0 ? direction * 13f : horizontal * 6f;
            float acceleration = grounded ? 45f : 25f;
            body.velocity = new Vector2(Mathf.MoveTowards(body.velocity.x, targetSpeed, acceleration * Time.fixedDeltaTime), body.velocity.y);
            if (transform.position.y < -6.5f) Respawn();
        }

        private void UpdateGroundedState()
        {
            int hitCount = Physics2D.OverlapBoxNonAlloc((Vector2)transform.position + Vector2.down * GroundProbeOffset, new Vector2(.42f, .12f), 0f, groundHits, 1 << VerticalSliceRuntime.GroundLayer);
            bool wasGrounded = grounded;
            grounded = hitCount > 0 && body.velocity.y <= .5f;
            if (grounded) coyote = .12f;
            if (!grounded && wasGrounded) coyote = Mathf.Max(coyote, .12f);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube((Vector2)transform.position + Vector2.down * GroundProbeOffset, new Vector2(.42f, .12f));
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            // Physical collisions are intentionally limited to the terrain layer.
            if (collision.gameObject.layer != VerticalSliceRuntime.GroundLayer) return;
            foreach (ContactPoint2D point in collision.contacts)
            {
                if (point.normal.y > .45f) { grounded = true; coyote = .12f; return; }
            }
        }

        private void BeginDash()
        {
            if (dashTimer > 0 || skillCooldown > 0) return;
            dashTimer = .35f;
            invulnerability = .35f;
            skillCooldown = .75f;
        }

        private void LightAttack()
        {
            if (attackCooldown > 0) return;
            TraitDefinition stem = GetTrait(TraitSlot.Stem);
            float cooldown = stem == null ? .42f : stem.Cooldown;
            float damage = (stem == null ? 10f : stem.Damage) + EvolutionDamage();
            float range = stem == null ? 1.2f : stem.Range;
            combo = combo % 3 + 1;
            attackCooldown = cooldown;
            foreach (PlatformEnemy enemy in new List<PlatformEnemy>(VerticalSliceRuntime.Instance.Enemies))
            {
                float delta = enemy.transform.position.x - transform.position.x;
                if (Mathf.Sign(delta) == direction && Mathf.Abs(delta) <= range && Mathf.Abs(enemy.transform.position.y - transform.position.y) < 1.2f)
                    enemy.Hit(damage, direction * (stem != null && stem.Id == "treant_arm" ? 8f : 4f));
            }
            CorruptedAncient boss = CorruptedAncient.Current;
            if (boss != null && Mathf.Abs(boss.transform.position.x - transform.position.x) <= range) boss.Hit(damage);
        }

        private void UseSkill()
        {
            if (skillCooldown > 0) return;
            TraitDefinition flower = GetTrait(TraitSlot.Flower);
            if (flower == null) { VerticalSliceRuntime.Instance.SpawnProjectile(transform.position, new Vector2(direction * 11f, 0), 8f, false, new Color(.95f, .85f, .3f)); skillCooldown = 2.5f; return; }
            skillCooldown = flower.Cooldown;
            if (flower.Id == "toxic_cap")
            {
                foreach (PlatformEnemy enemy in VerticalSliceRuntime.Instance.Enemies) if (Vector2.Distance(transform.position, enemy.transform.position) <= 3f) enemy.ApplyPoison(6f, 3f);
            }
            else if (flower.Id == "burst_pod")
            {
                body.velocity = new Vector2(direction * 15f, body.velocity.y);
                TakeDamage(Health * .15f);
                foreach (PlatformEnemy enemy in VerticalSliceRuntime.Instance.Enemies) if (Vector2.Distance(transform.position, enemy.transform.position) <= 2.5f) enemy.Hit(35f + EvolutionDamage(), direction * 9f);
            }
            else VerticalSliceRuntime.Instance.SpawnProjectile(transform.position, new Vector2(direction * 10f, 0), flower.Damage + EvolutionDamage(), false, flower.Color);
        }

        private void AbsorbCorruption()
        {
            if (Corruption >= 100f) return;
            Corruption = Mathf.Min(100f, Corruption + 25f);
            skillCooldown = Mathf.Max(0f, skillCooldown - 1.5f);
            if (Corruption >= 100f) { Corruption = 0f; if (Evolutions.Count > 0) Evolutions.RemoveAt(Evolutions.Count - 1); }
        }

        public void TakeDamage(float amount)
        {
            if (invulnerability > 0) return;
            TraitDefinition root = GetTrait(TraitSlot.Root);
            if (root != null) amount *= 1f - root.DamageReduction;
            Health -= amount;
            invulnerability = .6f;
            if (Health <= 0) Respawn();
        }

        public void Equip(TraitDefinition trait) { Traits[trait.Slot] = trait; CheckFusions(); }
        public void AddToInventory(TraitDefinition trait) { if (!Inventory.Exists(item => item.Id == trait.Id)) Inventory.Add(trait); }
        public TraitDefinition GetTrait(TraitSlot slot) => Traits.TryGetValue(slot, out TraitDefinition trait) ? trait : null;
        public void GainEssence(int amount)
        {
            Essence += amount;
            // GGJ48H uses no evolution selection in its playable loop.
        }
        public void SelectEvolution(EvolutionDefinition evolution) { Evolutions.Add(evolution); EvolutionCount++; }
        public float EvolutionDamage() { float total = 0; foreach (EvolutionDefinition evolution in Evolutions) total += evolution.DamageBonus; return total; }
        public void Restore() { Health = 100f; }
        public void SetPlayable(bool playable)
        {
            body.bodyType = playable ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
            body.velocity = Vector2.zero;
            if (!playable) grounded = false;
        }
        public void ResetRun()
        {
            Health = 100f;
            Corruption = 0f;
            Essence = 0;
            EvolutionCount = 0;
            Traits.Clear();
            Inventory.Clear();
            Evolutions.Clear();
            Fusions.Clear();
            transform.position = new Vector2(-8f, -2.6f);
            body.velocity = Vector2.zero;
        }
        private void CheckFusions()
        {
            foreach (FusionDefinition fusion in VerticalSliceCatalog.Fusions)
            {
                if (Fusions.Exists(item => item.Id == fusion.Id)) continue;
                bool first = false;
                bool second = false;
                foreach (TraitDefinition trait in Traits.Values) { if (trait.Id == fusion.FirstTrait) first = true; if (trait.Id == fusion.SecondTrait) second = true; }
                if (first && second) Fusions.Add(fusion);
            }
        }
        private void Respawn() { Health = 100f; Corruption = 0f; transform.position = new Vector2(-7, -2.6f); body.velocity = Vector2.zero; }
    }
}
