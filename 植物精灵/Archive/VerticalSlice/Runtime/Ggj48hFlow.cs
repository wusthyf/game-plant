using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PlantSpirit.VerticalSlice
{
    public enum GgjState { Menu, Playing, Grafting, PortalReady, Result, Dead }

    public sealed class GgjGameFlow : MonoBehaviour
    {
        public static GgjGameFlow Current { get; private set; }
        public GgjState State { get; private set; } = GgjState.Menu;
        public string Message { get; private set; } = "击败敌怪，取得器官，随时嫁接";
        public float RunStartTime { get; private set; }
        public float Elapsed => State == GgjState.Playing || State == GgjState.PortalReady ? Time.time - RunStartTime : finishedTime;
        private int zone = -1;
        private int activeEnemies;
        private bool[] guaranteedDrops = new bool[3];
        private GameObject portal;
        private float finishedTime;
        private float graftInputLock;

        private void Awake() { Current = this; }
        private void Update()
        {
            if (State == GgjState.Playing)
            {
                if (graftInputLock > 0) graftInputLock -= Time.deltaTime;
                if (graftInputLock <= 0 && (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.G) || Input.GetKeyDown(KeyCode.JoystickButton3))) OpenGraft();
                TryStartEncounter();
            }
            if (State == GgjState.PortalReady && portal != null && Vector2.Distance(VerticalSliceRuntime.Instance.Player.transform.position, portal.transform.position) < 1.1f && (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.JoystickButton1))) StartCoroutine(EnterPortal());
        }

        public void StartGame()
        {
            ClearRuntimeObjects();
            guaranteedDrops = new bool[3];
            zone = -1;
            activeEnemies = 0;
            finishedTime = 0;
            VerticalSliceRuntime.Instance.Player.ResetRun();
            VerticalSliceRuntime.Instance.Player.SetPlayable(true);
            VerticalSliceRuntime.Instance.IsGameplayActive = true;
            Time.timeScale = 1f;
            State = GgjState.Playing;
            RunStartTime = Time.time;
            Message = "教学起点：移动、跳跃、冲刺、攻击与技能";
        }

        public void ReturnToMenu()
        {
            ClearRuntimeObjects();
            VerticalSliceRuntime.Instance.IsGameplayActive = false;
            VerticalSliceRuntime.Instance.Player.SetPlayable(false);
            State = GgjState.Menu;
            Message = "击败敌怪，取得器官，随时嫁接";
            Time.timeScale = 1f;
        }

        public void Restart() => StartGame();
        public void OpenGraft()
        {
            if (State != GgjState.Playing) return;
            State = GgjState.Grafting;
            Time.timeScale = 0f;
            Message = "嫁接界面：选择已收集的器官";
        }
        public void CloseGraft()
        {
            if (State != GgjState.Grafting) return;
            State = GgjState.Playing;
            Time.timeScale = 1f;
            graftInputLock = .25f;
            Message = "嫁接完成，能力已立即刷新";
        }
        public void ApplyGraft(TraitDefinition trait)
        {
            VerticalSliceRuntime.Instance.Player.Equip(trait);
            CloseGraft();
        }
        public void NotifyPickup(TraitDefinition trait) { Message = "拾取 " + trait.DisplayName + "，按 Tab 或 G 随时嫁接"; }
        public void NotifyEnemyDefeated(EnemyKind kind, Vector2 position, TraitDefinition drop)
        {
            activeEnemies = Mathf.Max(0, activeEnemies - 1);
            if (zone == 0 && kind == EnemyKind.Vine && !guaranteedDrops[0]) { guaranteedDrops[0] = true; VerticalSliceRuntime.Instance.SpawnPickup(position, VerticalSliceCatalog.FindTrait("vine_tendril")); }
            if (zone == 1 && kind == EnemyKind.Mushroom && !guaranteedDrops[1]) { guaranteedDrops[1] = true; VerticalSliceRuntime.Instance.SpawnPickup(position, VerticalSliceCatalog.FindTrait("toxic_cap")); }
            if (zone == 2 && kind == EnemyKind.Beetle && !guaranteedDrops[2]) { guaranteedDrops[2] = true; VerticalSliceRuntime.Instance.SpawnPickup(position, VerticalSliceCatalog.FindTrait("iron_root")); }
            if (activeEnemies == 0) CompleteEncounter();
        }

        private void TryStartEncounter()
        {
            float x = VerticalSliceRuntime.Instance.Player.transform.position.x;
            if (zone < 0 && x > -4.8f) StartEncounter(0);
            else if (zone == 0 && x > -.5f) StartEncounter(1);
            else if (zone == 1 && x > 4.2f) StartEncounter(2);
        }
        private void StartEncounter(int index)
        {
            zone = index;
            EnemyKind[] roster = index == 0 ? new[] { EnemyKind.Vine, EnemyKind.Vine } : index == 1 ? new[] { EnemyKind.Vine, EnemyKind.Vine, EnemyKind.Mushroom } : new[] { EnemyKind.Vine, EnemyKind.Vine, EnemyKind.Mushroom, EnemyKind.Beetle };
            activeEnemies = roster.Length;
            Message = "战斗区 " + (index + 1) + "：击败全部敌人";
            for (int i = 0; i < roster.Length; i++) VerticalSliceRuntime.Instance.SpawnEnemy(roster[i], new Vector2(Mathf.Min(7.5f, -2.5f + index * 4.5f + i * 1.3f), -2.8f));
        }
        private void CompleteEncounter()
        {
            if (zone < 2) { Message = "战斗区 " + (zone + 1) + " 已清除，继续向右推进"; return; }
            StartCoroutine(OpenPortal());
        }
        private IEnumerator OpenPortal()
        {
            Message = "枯死根冠正在生长";
            yield return new WaitForSeconds(.8f);
            portal = VerticalSliceRuntime.Instance.Visual("净化传送门", new Vector2(8f, -2.7f), new Vector2(1f, 2.1f), new Color(.35f, 1f, .55f), 4);
            State = GgjState.PortalReady;
            Message = "全部敌人已清除，前往传送门并按 E 进入";
        }
        private IEnumerator EnterPortal()
        {
            State = GgjState.Result;
            VerticalSliceRuntime.Instance.IsGameplayActive = false;
            VerticalSliceRuntime.Instance.Player.SetPlayable(false);
            finishedTime = Time.time - RunStartTime;
            Message = "枯萎林地已净化";
            yield return null;
        }
        private void ClearRuntimeObjects()
        {
            foreach (PlatformEnemy enemy in new List<PlatformEnemy>(VerticalSliceRuntime.Instance.Enemies)) if (enemy != null) Destroy(enemy.gameObject);
            foreach (DamageProjectile projectile in new List<DamageProjectile>(VerticalSliceRuntime.Instance.Projectiles)) if (projectile != null) Destroy(projectile.gameObject);
            VerticalSliceRuntime.Instance.Enemies.Clear();
            VerticalSliceRuntime.Instance.Projectiles.Clear();
            if (portal != null) Destroy(portal);
        }
    }

    public sealed class GgjUi : MonoBehaviour
    {
        private GUIStyle title;
        private GUIStyle body;
        private void OnGUI()
        {
            if (GgjGameFlow.Current == null) return;
            if (title == null) { title = new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } }; body = new GUIStyle(GUI.skin.label) { fontSize = 16, normal = { textColor = Color.white } }; }
            GgjGameFlow flow = GgjGameFlow.Current;
            if (flow.State == GgjState.Menu) DrawMenu(flow);
            else if (flow.State == GgjState.Grafting) DrawGraft(flow);
            else if (flow.State == GgjState.Result) DrawResult(flow);
            else DrawHud(flow);
        }
        private void DrawMenu(GgjGameFlow flow)
        {
            GUI.Label(new Rect(Screen.width / 2 - 210, 120, 440, 50), "植物精灵", title);
            GUI.Label(new Rect(Screen.width / 2 - 210, 165, 440, 32), "枯萎森林 GGJ48H 可玩 Demo", body);
            if (GUI.Button(new Rect(Screen.width / 2 - 130, 255, 260, 45), "开始游戏")) flow.StartGame();
            GUI.Label(new Rect(Screen.width / 2 - 210, 325, 440, 80), "击败敌怪，拾取器官。按 Tab 或 G 随时嫁接，能力立即改变。", body);
        }
        private void DrawHud(GgjGameFlow flow)
        {
            PlatformPlayer player = VerticalSliceRuntime.Instance.Player;
            GUI.Label(new Rect(18, 16, 600, 30), "生命 " + Mathf.CeilToInt(player.Health) + "/100    " + flow.Message, body);
            GUI.Label(new Rect(18, 42, 800, 26), "根：" + Name(player, TraitSlot.Root) + "  茎：" + Name(player, TraitSlot.Stem) + "  花：" + Name(player, TraitSlot.Flower), body);
            GUI.Label(new Rect(18, Screen.height - 35, 900, 25), "A/D 移动 | Space 跳跃 | Shift 冲刺 | 左键/J 普攻 | 右键/K 技能 | Tab/G 嫁接", body);
        }
        private void DrawGraft(GgjGameFlow flow)
        {
            PlatformPlayer player = VerticalSliceRuntime.Instance.Player;
            GUI.Box(new Rect(Screen.width / 2 - 300, 100, 600, 430), "随时嫁接 - 战斗已暂停");
            GUI.Label(new Rect(Screen.width / 2 - 260, 140, 520, 30), "已收集器官。选择后立即替换对应能力与外观。", body);
            int row = 0;
            foreach (TraitDefinition trait in player.Inventory)
            {
                if (GUI.Button(new Rect(Screen.width / 2 - 250, 185 + row * 55, 500, 42), trait.DisplayName + "  [" + SlotName(trait.Slot) + "]  " + Description(trait))) flow.ApplyGraft(trait);
                row++;
            }
            if (row == 0) GUI.Label(new Rect(Screen.width / 2 - 250, 200, 500, 30), "尚未拾取部件，击败首只关键敌人后必定掉落。", body);
            if (GUI.Button(new Rect(Screen.width / 2 - 100, 460, 200, 36), "返回战斗")) flow.CloseGraft();
        }
        private void DrawResult(GgjGameFlow flow)
        {
            PlatformPlayer player = VerticalSliceRuntime.Instance.Player;
            GUI.Label(new Rect(Screen.width / 2 - 180, 140, 360, 42), "枯萎林地已净化", title);
            GUI.Label(new Rect(Screen.width / 2 - 180, 200, 360, 30), "本局用时 " + flow.Elapsed.ToString("F1") + " 秒", body);
            GUI.Label(new Rect(Screen.width / 2 - 180, 230, 360, 50), "根：" + Name(player, TraitSlot.Root) + "\n茎：" + Name(player, TraitSlot.Stem) + "  花：" + Name(player, TraitSlot.Flower), body);
            if (GUI.Button(new Rect(Screen.width / 2 - 130, 320, 260, 42), "再次挑战")) flow.Restart();
            if (GUI.Button(new Rect(Screen.width / 2 - 130, 372, 260, 42), "返回主页面")) flow.ReturnToMenu();
        }
        private static string Name(PlatformPlayer player, TraitSlot slot) { TraitDefinition trait = player.GetTrait(slot); return trait == null ? "基础" : trait.DisplayName; }
        private static string SlotName(TraitSlot slot) { return slot == TraitSlot.Root ? "根" : slot == TraitSlot.Stem ? "茎" : "花"; }
        private static string Description(TraitDefinition trait) { return trait.Id == "vine_tendril" ? "长距离穿透藤鞭" : trait.Id == "toxic_cap" ? "毒雾持续伤害并减速" : "减伤 25%，冲刺护盾"; }
    }
}
