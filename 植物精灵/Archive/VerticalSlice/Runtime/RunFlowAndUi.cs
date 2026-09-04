using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PlantSpirit.VerticalSlice
{
    public sealed class RoomFlow : MonoBehaviour
    {
        public string Status { get; private set; } = "按 Enter 开始枯萎森林远征";
        public int RoomIndex { get; private set; } = -1;
        public IReadOnlyList<RoomKind> Route => route;
        private readonly List<RoomKind> route = new List<RoomKind>();
        private readonly List<EvolutionDefinition> offered = new List<EvolutionDefinition>();
        private bool resolving;

        private void Start()
        {
            route.AddRange(new[] { RoomKind.Combat, RoomKind.Platform, RoomKind.Combat, RoomKind.Event, RoomKind.Elite, RoomKind.Rest, RoomKind.Combat, RoomKind.Boss });
        }
        private void Update()
        {
            if (!resolving && !VerticalSliceRuntime.Instance.IsChoosingEvolution && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.JoystickButton7))) StartNextRoom();
            if (VerticalSliceRuntime.Instance.IsChoosingEvolution)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.JoystickButton0)) SelectEvolution(0);
                if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.JoystickButton1)) SelectEvolution(1);
                if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.JoystickButton2)) SelectEvolution(2);
            }
        }
        private void StartNextRoom()
        {
            if (RoomIndex + 1 >= route.Count) return;
            RoomIndex++;
            StartCoroutine(RunRoom(route[RoomIndex]));
        }
        private IEnumerator RunRoom(RoomKind room)
        {
            resolving = true;
            Status = "进入 " + RoomTitle(room) + " " + (RoomIndex + 1) + "/" + route.Count;
            if (room == RoomKind.Rest)
            {
                VerticalSliceRuntime.Instance.Player.Restore();
                Status = "休憩嫁接台：生命已回复，按 Enter 继续";
                resolving = false;
                yield break;
            }
            if (room == RoomKind.Event)
            {
                Status = "生态事件：按 C 吸收污染以缩短技能冷却，或按 Enter 保持自然路线";
                yield return new WaitForSeconds(3f);
                Status = "生态事件结束，按 Enter 继续";
                resolving = false;
                yield break;
            }
            if (room == RoomKind.Platform)
            {
                Status = "平台挑战：登上树冠平台后按 Enter";
                while (VerticalSliceRuntime.Instance.Player.transform.position.y < 2f) yield return null;
                VerticalSliceRuntime.Instance.Player.GainEssence(8);
                Status = "发现隐藏种子，按 Enter 继续";
                resolving = false;
                yield break;
            }
            if (room == RoomKind.Boss)
            {
                VerticalSliceRuntime.Instance.Player.Restore();
                Status = "腐化古树出现";
                GameObject bossObject = VerticalSliceRuntime.Instance.Visual("腐化古树", new Vector2(4.8f, -2.3f), new Vector2(2.4f, 3.3f), new Color(.22f, .42f, .18f), 3);
                bossObject.AddComponent<BoxCollider2D>();
                CorruptedAncient boss = bossObject.AddComponent<CorruptedAncient>();
                boss.Initialize();
                while (!boss.Defeated) yield return null;
                resolving = false;
                yield break;
            }

            EnemyKind[][] waves = room == RoomKind.Elite
                ? new[] { new[] { EnemyKind.Beetle, EnemyKind.Mushroom }, new[] { EnemyKind.Treant, EnemyKind.Berry, EnemyKind.Berry } }
                : new[] { new[] { EnemyKind.Vine, EnemyKind.Mushroom }, new[] { EnemyKind.Beetle, EnemyKind.Vine }, new[] { EnemyKind.Berry, EnemyKind.ThornPod } };
            for (int wave = 0; wave < waves.Length; wave++)
            {
                Status = RoomTitle(room) + " - 第 " + (wave + 1) + " 波";
                foreach (EnemyKind enemy in waves[wave]) VerticalSliceRuntime.Instance.SpawnEnemy(enemy, SpawnPoint());
                while (VerticalSliceRuntime.Instance.Enemies.Count > 0) yield return null;
                if (wave < waves.Length - 1) yield return new WaitForSeconds(1.5f);
            }
            VerticalSliceRuntime.Instance.Player.GainEssence(room == RoomKind.Elite ? 20 : 10);
            SaveRun();
            Status = RoomTitle(room) + " 已净化，按 Enter 继续";
            resolving = false;
        }

        public void OfferEvolution()
        {
            if (VerticalSliceRuntime.Instance.IsChoosingEvolution) return;
            offered.Clear();
            List<EvolutionDefinition> candidates = new List<EvolutionDefinition>(VerticalSliceCatalog.Evolutions);
            candidates.Sort((a, b) => Score(b).CompareTo(Score(a)));
            RunRandom choiceRandom = VerticalSliceRuntime.Instance.Random.Fork("evolution-" + VerticalSliceRuntime.Instance.Player.EvolutionCount);
            while (offered.Count < 3 && candidates.Count > 0)
            {
                int index = choiceRandom.Range(0, candidates.Count);
                offered.Add(candidates[index]);
                candidates.RemoveAt(index);
            }
            VerticalSliceRuntime.Instance.IsChoosingEvolution = true;
            Status = "进化选择：按 1、2、3 确认一项";
        }
        public IReadOnlyList<EvolutionDefinition> OfferedEvolutions => offered;
        private int Score(EvolutionDefinition evolution)
        {
            foreach (TraitDefinition trait in VerticalSliceRuntime.Instance.Player.Traits.Values)
                foreach (TraitTag tag in trait.Tags) if (tag == evolution.RequiredTag) return 10;
            return 0;
        }
        private void SelectEvolution(int index)
        {
            if (index < 0 || index >= offered.Count) return;
            VerticalSliceRuntime.Instance.Player.SelectEvolution(offered[index]);
            VerticalSliceRuntime.Instance.IsChoosingEvolution = false;
            Status = "进化完成：" + offered[index].DisplayName;
        }
        public void CompleteBoss()
        {
            Status = "枯萎森林已净化。世界恢复生机，Demo 完成";
            SaveRun(true);
        }
        private Vector2 SpawnPoint()
        {
            float x = VerticalSliceRuntime.Instance.Random.Range(-70, 81) / 10f;
            return new Vector2(x, -2.8f);
        }
        private static string RoomTitle(RoomKind kind)
        {
            switch (kind)
            {
                case RoomKind.Combat: return "普通战斗房";
                case RoomKind.Platform: return "平台挑战房";
                case RoomKind.Elite: return "精英巢穴";
                case RoomKind.Rest: return "休憩嫁接台";
                case RoomKind.Event: return "生态事件房";
                default: return "腐化古树巢穴";
            }
        }
        private void SaveRun(bool cleared = false)
        {
            RunSaveData data = new RunSaveData { seed = VerticalSliceRuntime.Instance.Random.Seed, roomIndex = RoomIndex, cleared = cleared, essence = VerticalSliceRuntime.Instance.Player.Essence };
            string path = Path.Combine(Application.persistentDataPath, "plant_spirit_run.json");
            File.WriteAllText(path, JsonUtility.ToJson(data, true));
        }
    }

    [Serializable]
    public sealed class RunSaveData { public int seed; public int roomIndex; public int essence; public bool cleared; }

    public sealed class VerticalSliceHud : MonoBehaviour
    {
        private GUIStyle heading; private GUIStyle body;
        private void OnGUI()
        {
            if (VerticalSliceRuntime.Instance == null || VerticalSliceRuntime.Instance.Player == null) return;
            if (heading == null)
            {
                heading = new GUIStyle(GUI.skin.label) { fontSize = 23, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
                body = new GUIStyle(GUI.skin.label) { fontSize = 15, normal = { textColor = new Color(.85f, 1f, .86f) } };
            }
            PlatformPlayer player = VerticalSliceRuntime.Instance.Player;
            RoomFlow rooms = VerticalSliceRuntime.Instance.Rooms;
            GUI.Label(new Rect(18, 14, 640, 30), "植物精灵 | 枯萎森林垂直切片", heading);
            GUI.Label(new Rect(18, 47, 600, 24), "生命 " + Mathf.CeilToInt(player.Health) + "/100  生命质 " + player.Essence + "  污染 " + Mathf.CeilToInt(player.Corruption) + "/100", body);
            GUI.Label(new Rect(18, 70, 900, 24), "根 " + TraitName(player, TraitSlot.Root) + " | 茎 " + TraitName(player, TraitSlot.Stem) + " | 花 " + TraitName(player, TraitSlot.Flower), body);
            if (CorruptedAncient.Current != null) GUI.Label(new Rect(Screen.width - 300, 18, 280, 28), "腐化古树 P" + CorruptedAncient.Current.Phase + "  HP " + Mathf.CeilToInt(CorruptedAncient.Current.Health) + "/150", heading);
            GUI.Label(new Rect(18, Screen.height - 65, Screen.width - 36, 24), "移动 WASD/方向键 | 跳跃 Space | 冲刺 Shift | 普攻 J/左键 | 技能 K/右键 | 污染 C | 暂停 Esc", body);
            GUI.Label(new Rect(18, Screen.height - 38, Screen.width - 36, 28), rooms.Status, heading);
            if (VerticalSliceRuntime.Instance.IsChoosingEvolution)
            {
                GUI.Box(new Rect(Screen.width / 2 - 310, Screen.height / 2 - 100, 620, 200), "进化选择");
                IReadOnlyList<EvolutionDefinition> options = rooms.OfferedEvolutions;
                for (int i = 0; i < options.Count; i++) GUI.Label(new Rect(Screen.width / 2 - 280, Screen.height / 2 - 55 + i * 42, 560, 32), (i + 1) + ". " + options[i].DisplayName + " - " + options[i].Description, body);
            }
            if (VerticalSliceRuntime.Instance.IsPaused) GUI.Label(new Rect(Screen.width / 2 - 60, Screen.height / 2, 160, 32), "已暂停", heading);
        }
        private static string TraitName(PlatformPlayer player, TraitSlot slot) { TraitDefinition trait = player.GetTrait(slot); return trait == null ? "基础" : trait.DisplayName; }
    }
}
