using System;
using UnityEngine;

namespace PlantSpirit.GGJ
{
    public sealed class GraftApplier : MonoBehaviour
    {
        [SerializeField] private PlayerCombat combat;
        public event Action<GraftDefinition> Applied;

        public bool TryApply(GraftDefinition item)
        {
            if (item == null || GameBootstrap.Instance == null || !GameBootstrap.Instance.Session.Inventory.Exists(x => x.Id == item.Id)) return false;
            GameBootstrap.Instance.Session.Equip(item);
            combat?.RefreshLoadout();
            GameAudio.Play(AudioCue.GraftConfirm);
            Applied?.Invoke(item);
            return true;
        }
    }
}
