using System.Collections.Generic;
using UnityEngine;

namespace PlantSpirit.GGJ
{
    public enum GraftSlot { Root, Stem, Flower }

    public sealed class GameSession
    {
        public readonly List<GraftDefinition> Inventory = new List<GraftDefinition>();
        public readonly Dictionary<GraftSlot, GraftDefinition> Loadout = new Dictionary<GraftSlot, GraftDefinition>();
        public float StartedAt { get; private set; }
        public float CompletedSeconds { get; private set; }
        public int EnemiesDefeated { get; private set; }
        public bool Completed { get; private set; }

        public void BeginRun()
        {
            Inventory.Clear();
            Loadout.Clear();
            StartedAt = Time.unscaledTime;
            CompletedSeconds = 0f;
            EnemiesDefeated = 0;
            Completed = false;
        }

        public void Tick(float deltaTime) { if (GameBootstrap.Instance != null && GameBootstrap.Instance.State.Current == GameState.Playing) CompletedSeconds += deltaTime; }
        public void RegisterEnemyDefeated() => EnemiesDefeated++;
        public void CompleteRun() => Completed = true;
        public bool Add(GraftDefinition definition)
        {
            if (definition == null || Inventory.Exists(item => item.Id == definition.Id)) return false;
            Inventory.Add(definition);
            return true;
        }
        public void Equip(GraftDefinition definition) => Loadout[definition.Slot] = definition;
        public GraftDefinition Get(GraftSlot slot) => Loadout.TryGetValue(slot, out GraftDefinition value) ? value : null;
    }
}
