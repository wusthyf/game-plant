using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PlantSpirit
{
    public enum PartSlot { Root, Stem, Flower }
    public enum EnemyType { Vine, Mushroom, Beetle, Treant, Berry }

    public sealed class PlantSpiritDemo : MonoBehaviour
    {
        public static PlantSpiritDemo Instance { get; private set; }
        public PlayerController Player { get; private set; }
        public RoomDirector Rooms { get; private set; }
        public readonly List<EnemyController> Enemies = new List<EnemyController>();
        public readonly List<Projectile> Projectiles = new List<Projectile>();
        private Sprite square;

        // Legacy top-down combat prototype. The vertical-slice bootstrap is now the default entry point.
        public static void CreateLegacyDemo()
        {
            if (FindObjectOfType<PlantSpiritDemo>() == null)
            {
                new GameObject("Plant Spirit Demo").AddComponent<PlantSpiritDemo>();
            }
        }

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            square = CreateSquareSprite();
            Application.targetFrameRate = 60;
            SetupCamera();
            CreateArena();
            Player = CreatePlayer();
            Rooms = gameObject.AddComponent<RoomDirector>();
            gameObject.AddComponent<DemoHud>();
        }

        private void SetupCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                camera = new GameObject("Main Camera").AddComponent<Camera>();
                camera.tag = "MainCamera";
            }
            camera.orthographic = true;
            camera.orthographicSize = 5.4f;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.backgroundColor = new Color(0.045f, 0.09f, 0.075f);
        }

        private void CreateArena()
        {
            CreateVisual("Ground", Vector2.zero, new Vector2(18f, 10f), new Color(0.10f, 0.19f, 0.12f), -2);
            CreateVisual("Top Border", new Vector2(0f, 5.1f), new Vector2(18f, .18f), new Color(.25f, .45f, .24f), -1);
            CreateVisual("Bottom Border", new Vector2(0f, -5.1f), new Vector2(18f, .18f), new Color(.25f, .45f, .24f), -1);
            CreateVisual("Left Border", new Vector2(-9f, 0f), new Vector2(.18f, 10f), new Color(.25f, .45f, .24f), -1);
            CreateVisual("Right Border", new Vector2(9f, 0f), new Vector2(.18f, 10f), new Color(.25f, .45f, .24f), -1);
        }

        public GameObject CreateVisual(string label, Vector2 position, Vector2 scale, Color color, int order = 0)
        {
            GameObject obj = new GameObject(label);
            obj.transform.position = position;
            obj.transform.localScale = scale;
            SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = square;
            renderer.color = color;
            renderer.sortingOrder = order;
            return obj;
        }

        private PlayerController CreatePlayer()
        {
            GameObject obj = CreateVisual("植物精灵", Vector2.zero, new Vector2(.6f, .8f), new Color(.55f, 1f, .42f), 2);
            return obj.AddComponent<PlayerController>();
        }

        public EnemyController SpawnEnemy(EnemyType type, Vector2 position)
        {
            EnemyData data = EnemyData.For(type);
            GameObject obj = CreateVisual(data.DisplayName, position, Vector2.one * data.Scale, data.Color, 1);
            EnemyController enemy = obj.AddComponent<EnemyController>();
            enemy.Initialize(data);
            Enemies.Add(enemy);
            return enemy;
        }

        public void SpawnProjectile(Vector2 position, Vector2 direction, float speed, float damage, bool hostile, Color color, float lifetime = 3f)
        {
            GameObject obj = CreateVisual(hostile ? "腐化果实" : "种子", position, Vector2.one * .23f, color, 3);
            Projectile projectile = obj.AddComponent<Projectile>();
            projectile.Initialize(direction.normalized, speed, damage, hostile, lifetime);
            Projectiles.Add(projectile);
        }

        public void RemoveEnemy(EnemyController enemy) => Enemies.Remove(enemy);
        public void RemoveProjectile(Projectile projectile) => Projectiles.Remove(projectile);

        public void CreatePickup(Vector2 position, PlantPart part)
        {
            GameObject obj = CreateVisual(part.Name, position, Vector2.one * .35f, part.Color, 3);
            Pickup pickup = obj.AddComponent<Pickup>();
            pickup.Part = part;
        }

        private Sprite CreateSquareSprite()
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(.5f, .5f), 1f);
        }
    }

    public sealed class PlayerController : MonoBehaviour
    {
        public float MaxHealth = 100f;
        public float Health { get; private set; }
        public int Experience { get; private set; }
        public readonly Dictionary<PartSlot, PlantPart> Parts = new Dictionary<PartSlot, PlantPart>();
        private float attackTimer;
        private float skillTimer;
        private Vector2 facing = Vector2.right;

        private void Start() => ResetPlayer();

        private void Update()
        {
            if (Health <= 0f)
            {
                if (Input.GetKeyDown(KeyCode.R)) ResetPlayer();
                return;
            }
            Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
            transform.position = ClampArena((Vector2)transform.position + input * 4.5f * Time.deltaTime);
            if (input.sqrMagnitude > .01f) facing = input;
            attackTimer -= Time.deltaTime;
            skillTimer -= Time.deltaTime;
            if (Input.GetKey(KeyCode.Space) && attackTimer <= 0f) BasicAttack();
            if (Input.GetKeyDown(KeyCode.F) && skillTimer <= 0f) UseFlowerSkill();
            if (Input.GetKeyDown(KeyCode.R)) ResetPlayer();
        }

        public void ResetPlayer()
        {
            Health = MaxHealth;
            Experience = 0;
            Parts.Clear();
            transform.position = Vector3.zero;
        }

        private void BasicAttack()
        {
            PlantPart stem = GetPart(PartSlot.Stem);
            float damage = stem != null ? stem.Damage : 10f;
            float range = stem != null ? stem.Range : 1f;
            float cooldown = stem != null ? stem.Cooldown : 1f;
            int hits = stem != null && stem.Id == "vine_tendril" ? 2 : 1;
            attackTimer = cooldown;
            List<EnemyController> targets = GetEnemiesInRange(range, hits);
            foreach (EnemyController target in targets)
            {
                Vector2 knockback = stem != null && stem.Id == "treant_arm" ? facing * 4.5f : facing * 1.5f;
                target.TakeDamage(damage, knockback);
            }
            BossController boss = BossController.Current;
            if (boss != null && Vector2.Distance(transform.position, boss.transform.position) <= range)
                boss.TakeDamage(damage);
        }

        private void UseFlowerSkill()
        {
            PlantPart flower = GetPart(PartSlot.Flower);
            if (flower == null)
            {
                PlantSpiritDemo.Instance.SpawnProjectile(transform.position, facing, 7f, 8f, false, new Color(.95f, .92f, .45f));
                skillTimer = 3f;
                return;
            }
            skillTimer = flower.Cooldown;
            if (flower.Id == "toxic_cap")
            {
                foreach (EnemyController enemy in GetEnemiesInRange(3f, 99)) enemy.ApplyPoison(6f, 3f, .3f);
            }
            else if (flower.Id == "burst_pod")
            {
                Vector2 end = ClampArena((Vector2)transform.position + facing * 4f);
                transform.position = end;
                TakeDamage(Health * .15f, false);
                foreach (EnemyController enemy in GetEnemiesInRange(2.5f, 99)) enemy.TakeDamage(35f, ((Vector2)enemy.transform.position - end).normalized * 5f);
            }
        }

        private List<EnemyController> GetEnemiesInRange(float range, int max)
        {
            List<EnemyController> candidates = new List<EnemyController>();
            foreach (EnemyController enemy in PlantSpiritDemo.Instance.Enemies)
                if (enemy != null && !enemy.IsDead && Vector2.Distance(transform.position, enemy.transform.position) <= range) candidates.Add(enemy);
            candidates.Sort((a, b) => Vector2.Distance(transform.position, a.transform.position).CompareTo(Vector2.Distance(transform.position, b.transform.position)));
            if (candidates.Count > max) candidates.RemoveRange(max, candidates.Count - max);
            return candidates;
        }

        public void TakeDamage(float damage, bool knockback)
        {
            PlantPart root = GetPart(PartSlot.Root);
            if (root != null && root.Id == "iron_shell") damage *= .75f;
            Health = Mathf.Max(0f, Health - damage);
            if (knockback) transform.position = ClampArena((Vector2)transform.position - facing * .25f);
        }

        public void Equip(PlantPart part) => Parts[part.Slot] = part;
        public void GainExperience(int amount) => Experience += amount;
        public void HealFull() => Health = MaxHealth;
        public PlantPart GetPart(PartSlot slot) => Parts.TryGetValue(slot, out PlantPart part) ? part : null;
        public static Vector2 ClampArena(Vector2 p) => new Vector2(Mathf.Clamp(p.x, -8.6f, 8.6f), Mathf.Clamp(p.y, -4.7f, 4.7f));
    }

    public sealed class EnemyController : MonoBehaviour
    {
        public EnemyData Data { get; private set; }
        public bool IsDead { get; private set; }
        public float Health { get; private set; }
        private float cooldown;
        private float chargeTimer;
        private float poisonTimer;
        private float poisonDamage;
        private float slowPercent;
        private bool charging;

        public void Initialize(EnemyData data) { Data = data; Health = data.Health; }

        private void Update()
        {
            if (IsDead || PlantSpiritDemo.Instance.Player == null) return;
            PlayerController player = PlantSpiritDemo.Instance.Player;
            Vector2 toPlayer = (Vector2)player.transform.position - (Vector2)transform.position;
            float distance = toPlayer.magnitude;
            cooldown -= Time.deltaTime;
            if (poisonTimer > 0f)
            {
                poisonTimer -= Time.deltaTime;
                TakeDamage(poisonDamage * Time.deltaTime, Vector2.zero);
            }
            if (Data.Type == EnemyType.Mushroom)
            {
                if (distance <= Data.Range && cooldown <= 0f) { PlantSpiritDemo.Instance.SpawnProjectile(transform.position, toPlayer, 4.5f, Data.Damage, true, new Color(.62f, .2f, .75f)); cooldown = Data.Cooldown; }
                return;
            }
            if (Data.Type == EnemyType.Beetle)
            {
                if (!charging && distance <= 5f && cooldown <= 0f) { charging = true; chargeTimer = .45f; }
                if (charging) { chargeTimer -= Time.deltaTime; if (chargeTimer <= 0f) { transform.position += (Vector3)toPlayer.normalized * 8f * Time.deltaTime; if (distance < 1f) { player.TakeDamage(Data.Damage, true); charging = false; cooldown = Data.Cooldown; } } }
                else Move(toPlayer, Data.Speed);
                return;
            }
            if (Data.Type == EnemyType.Berry)
            {
                Move(toPlayer, Data.Speed);
                if (distance <= .75f) { foreach (EnemyController e in PlantSpiritDemo.Instance.Enemies) { } player.TakeDamage(Data.Damage, true); Die(false); }
                return;
            }
            Move(toPlayer, Data.Speed);
            if (distance <= Data.Range && cooldown <= 0f) { player.TakeDamage(Data.Damage, true); cooldown = Data.Cooldown; }
        }

        private void Move(Vector2 direction, float speed) { transform.position = PlayerController.ClampArena((Vector2)transform.position + direction.normalized * speed * (1f - slowPercent) * Time.deltaTime); }
        public void ApplyPoison(float dps, float duration, float slow) { poisonDamage = dps; poisonTimer = duration; slowPercent = slow; }
        public void TakeDamage(float amount, Vector2 knockback)
        {
            if (IsDead) return;
            Health -= amount;
            transform.position += (Vector3)knockback * Time.deltaTime;
            if (Health <= 0f) Die(true);
        }
        private void Die(bool rewards)
        {
            if (IsDead) return;
            IsDead = true;
            PlantSpiritDemo.Instance.RemoveEnemy(this);
            if (rewards)
            {
                PlantSpiritDemo.Instance.Player.GainExperience(Data.Experience);
                if (UnityEngine.Random.value <= Data.DropChance) PlantSpiritDemo.Instance.CreatePickup(transform.position, Data.Drop);
            }
            Destroy(gameObject);
        }
    }

    public sealed class Projectile : MonoBehaviour
    {
        private Vector2 direction; private float speed; private float damage; private bool hostile; private float life;
        public void Initialize(Vector2 dir, float newSpeed, float newDamage, bool isHostile, float lifetime) { direction = dir; speed = newSpeed; damage = newDamage; hostile = isHostile; life = lifetime; }
        private void Update()
        {
            transform.position += (Vector3)(direction * speed * Time.deltaTime); life -= Time.deltaTime;
            if (hostile)
            {
                PlayerController player = PlantSpiritDemo.Instance.Player;
                if (Vector2.Distance(transform.position, player.transform.position) < .38f) { player.TakeDamage(damage, true); DestroyProjectile(); }
            }
            else
            {
                foreach (EnemyController enemy in new List<EnemyController>(PlantSpiritDemo.Instance.Enemies)) if (!enemy.IsDead && Vector2.Distance(transform.position, enemy.transform.position) < .35f) { enemy.TakeDamage(damage, direction); DestroyProjectile(); break; }
                BossController boss = BossController.Current;
                if (boss != null && Vector2.Distance(transform.position, boss.transform.position) < .45f) { boss.TakeDamage(damage); DestroyProjectile(); }
            }
            if (life <= 0f || Mathf.Abs(transform.position.x) > 10f || Mathf.Abs(transform.position.y) > 6f) DestroyProjectile();
        }
        private void DestroyProjectile() { PlantSpiritDemo.Instance.RemoveProjectile(this); Destroy(gameObject); }
    }

    public sealed class Pickup : MonoBehaviour
    {
        public PlantPart Part;
        private void Update()
        {
            PlayerController player = PlantSpiritDemo.Instance.Player;
            if (Vector2.Distance(transform.position, player.transform.position) < .65f) { player.Equip(Part); Destroy(gameObject); }
        }
    }

    public sealed class RoomDirector : MonoBehaviour
    {
        public int CurrentRoom { get; private set; }
        public string Status { get; private set; } = "按 Enter 开始房间 1";
        private bool running;
        private void Update()
        {
            if (!running && Input.GetKeyDown(KeyCode.Return))
            {
                if (CurrentRoom < 3) StartCoroutine(RunRoom(++CurrentRoom)); else StartCoroutine(RunBoss());
            }
        }
        private IEnumerator RunRoom(int room)
        {
            running = true; Status = "房间 " + room + " 战斗开始";
            EnemyType[][] waves = room == 1 ? new[] { new[] { EnemyType.Vine, EnemyType.Vine }, new[] { EnemyType.Vine, EnemyType.Mushroom }, new[] { EnemyType.Vine } } : room == 2 ? new[] { new[] { EnemyType.Vine, EnemyType.Vine, EnemyType.Mushroom }, new[] { EnemyType.Beetle, EnemyType.Beetle }, new[] { EnemyType.Berry, EnemyType.Berry } } : new[] { new[] { EnemyType.Beetle, EnemyType.Mushroom, EnemyType.Mushroom }, new[] { EnemyType.Treant }, new[] { EnemyType.Berry, EnemyType.Berry, EnemyType.Vine } };
            for (int wave = 0; wave < waves.Length; wave++)
            {
                Status = "房间 " + room + " - 第 " + (wave + 1) + " 波";
                foreach (EnemyType type in waves[wave]) PlantSpiritDemo.Instance.SpawnEnemy(type, RandomPosition());
                while (PlantSpiritDemo.Instance.Enemies.Count > 0) yield return null;
                if (wave < waves.Length - 1) yield return new WaitForSeconds(room == 3 && wave == 0 ? 3f : 2f);
            }
            Status = "房间 " + room + " 已净化，按 Enter 继续"; running = false;
        }
        private IEnumerator RunBoss()
        {
            running = true; Status = "腐化古树巢穴";
            PlantSpiritDemo.Instance.Player.HealFull();
            GameObject bossObject = PlantSpiritDemo.Instance.CreateVisual("腐化古树", new Vector2(0f, 3.2f), Vector2.one * 2.2f, new Color(.22f, .42f, .18f), 1);
            BossController boss = bossObject.AddComponent<BossController>();
            boss.InitializeBoss();
            while (!boss.IsFinished) yield return null;
            Status = "腐化古树已净化，Demo 完成"; running = false;
        }
        private static Vector2 RandomPosition()
        {
            Vector2 p; do { p = new Vector2(UnityEngine.Random.Range(-7.5f, 7.5f), UnityEngine.Random.Range(-4f, 4f)); } while (p.magnitude < 3f); return p;
        }
    }

    public sealed class BossController : MonoBehaviour
    {
        public static BossController Current { get; private set; }
        public bool IsFinished { get; private set; }
        private float health = 150f; private float attackTimer; private int phase = 1; private bool summonedOne; private bool summonedTwo;
        public void InitializeBoss()
        {
            Current = this;
        }
        private void Update()
        {
            PlayerController player = PlantSpiritDemo.Instance.Player;
            if (health <= 0f) { IsFinished = true; Current = null; Destroy(gameObject); return; }
            int newPhase = health <= 45f ? 3 : health <= 90f ? 2 : 1;
            if (newPhase != phase) phase = newPhase;
            if (phase == 1 && !summonedOne) { summonedOne = true; PlantSpiritDemo.Instance.SpawnEnemy(EnemyType.Vine, new Vector2(-3f, 2f)); PlantSpiritDemo.Instance.SpawnEnemy(EnemyType.Vine, new Vector2(3f, 2f)); }
            if (phase == 2 && !summonedTwo) { summonedTwo = true; PlantSpiritDemo.Instance.SpawnEnemy(EnemyType.Treant, new Vector2(0f, 2f)); }
            attackTimer -= Time.deltaTime;
            if (attackTimer <= 0f) { StartCoroutine(Attack()); attackTimer = phase == 1 ? 2.5f : phase == 2 ? 1.8f : 1.2f; }
            if (Vector2.Distance(transform.position, player.transform.position) < .9f) player.TakeDamage(14f * Time.deltaTime, false);
        }
        private IEnumerator Attack()
        {
            PlayerController player = PlantSpiritDemo.Instance.Player;
            int fruitCount = phase; for (int i = 0; i < fruitCount; i++) { float angle = (i - (fruitCount - 1) * .5f) * 15f; Vector2 direction = Quaternion.Euler(0f, 0f, angle) * ((Vector2)player.transform.position - (Vector2)transform.position).normalized; PlantSpiritDemo.Instance.SpawnProjectile(transform.position, direction, 4f + phase * .5f, 9f, true, new Color(.7f, .18f, .22f)); }
            yield return new WaitForSeconds(phase == 1 ? 1.2f : phase == 2 ? 1f : .7f);
            int spikes = phase; for (int i = 0; i < spikes; i++) { Vector2 point = player.transform.position; yield return new WaitForSeconds(.2f); if (Vector2.Distance(player.transform.position, point) < 1.2f) player.TakeDamage(10f + phase * 2f, true); }
            if (phase >= 2 && Vector2.Distance(player.transform.position, transform.position) < (phase == 2 ? 2f : 3f)) player.TakeDamage(phase == 2 ? 15f : 18f, true);
            if (phase == 3) for (int i = 0; i < 8; i++) { Vector2 point = new Vector2(UnityEngine.Random.Range(-8f, 8f), UnityEngine.Random.Range(-4f, 4f)); if (Vector2.Distance(player.transform.position, point) < .8f) player.TakeDamage(10f, true); }
        }
        public void TakeDamage(float damage) { health -= damage; }
    }

    public sealed class DemoHud : MonoBehaviour
    {
        private GUIStyle title; private GUIStyle body;
        private void OnGUI()
        {
            if (PlantSpiritDemo.Instance == null || PlantSpiritDemo.Instance.Player == null) return;
            if (title == null) { title = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } }; body = new GUIStyle(GUI.skin.label) { fontSize = 15, normal = { textColor = new Color(.85f, 1f, .85f) } }; }
            PlayerController player = PlantSpiritDemo.Instance.Player;
            GUI.Label(new Rect(18, 14, 600, 32), "植物精灵 - 枯萎森林 Demo", title);
            GUI.Label(new Rect(18, 52, 500, 26), "生命 " + Mathf.CeilToInt(player.Health) + " / 100    生命质 " + player.Experience, body);
            GUI.Label(new Rect(18, 78, 720, 26), "根: " + PartName(player, PartSlot.Root) + " | 茎: " + PartName(player, PartSlot.Stem) + " | 花: " + PartName(player, PartSlot.Flower), body);
            GUI.Label(new Rect(18, Screen.height - 64, 900, 26), "移动 WASD/方向键 | 普攻 Space | 花技能 F | 重置 R | 开始/下一房 Enter", body);
            GUI.Label(new Rect(18, Screen.height - 38, 700, 26), PlantSpiritDemo.Instance.Rooms.Status, title);
            if (player.Health <= 0f) GUI.Label(new Rect(Screen.width / 2 - 120, Screen.height / 2, 300, 40), "生命枯竭，按 R 重生", title);
        }
        private static string PartName(PlayerController player, PartSlot slot) => player.GetPart(slot) == null ? "基础" : player.GetPart(slot).Name;
    }

    public sealed class PlantPart
    {
        public string Id; public string Name; public PartSlot Slot; public float Damage; public float Range; public float Cooldown; public Color Color;
        public PlantPart(string id, string name, PartSlot slot, float damage, float range, float cooldown, Color color) { Id = id; Name = name; Slot = slot; Damage = damage; Range = range; Cooldown = cooldown; Color = color; }
    }

    public sealed class EnemyData
    {
        public EnemyType Type; public string DisplayName; public float Health; public float Speed; public float Damage; public float Cooldown; public float Range; public int Experience; public float DropChance; public PlantPart Drop; public float Scale; public Color Color;
        public static EnemyData For(EnemyType type)
        {
            switch (type)
            {
                case EnemyType.Vine: return New(type, "腐化藤蔓怪", 25, 1.8f, 8, 1.5f, 1.2f, 5, .45f, new PlantPart("vine_tendril", "藤蔓触须", PartSlot.Stem, 8, 2.8f, .7f, new Color(.35f, .85f, .3f)), 1f, new Color(.2f, .58f, .22f));
                case EnemyType.Mushroom: return New(type, "毒孢蘑菇", 18, .6f, 6, 2.5f, 6f, 6, .35f, new PlantPart("toxic_cap", "毒菌伞", PartSlot.Flower, 6, 3f, 5f, new Color(.68f, .28f, .88f)), .8f, new Color(.55f, .2f, .66f));
                case EnemyType.Beetle: return New(type, "废铁甲虫", 35, 3.2f, 12, 1.8f, 1f, 8, .30f, new PlantPart("iron_shell", "铁甲壳", PartSlot.Root, 0, 0, 0, new Color(.57f, .65f, .72f)), 1f, new Color(.42f, .48f, .52f));
                case EnemyType.Treant: return New(type, "腐化树人", 55, .9f, 16, 2.8f, 2f, 12, .55f, new PlantPart("treant_arm", "树人之臂", PartSlot.Stem, 17, 1.4f, 1.8f, new Color(.62f, .36f, .14f)), 1.4f, new Color(.35f, .26f, .12f));
                default: return New(type, "自爆浆果", 10, 3.8f, 25, 0, 2.5f, 4, .40f, new PlantPart("burst_pod", "爆裂果囊", PartSlot.Flower, 35, 2.5f, 8f, new Color(1f, .28f, .18f)), .6f, new Color(.92f, .18f, .22f));
            }
        }
        private static EnemyData New(EnemyType type, string name, float hp, float spd, float atk, float cd, float range, int exp, float drop, PlantPart part, float scale, Color color) => new EnemyData { Type = type, DisplayName = name, Health = hp, Speed = spd, Damage = atk, Cooldown = cd, Range = range, Experience = exp, DropChance = drop, Drop = part, Scale = scale, Color = color };
    }
}
