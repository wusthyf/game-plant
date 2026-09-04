using System;
using UnityEngine;

namespace PlantSpirit.GGJ
{
    public sealed class GraftInventory : MonoBehaviour
    {
        public event Action<GraftDefinition> Added;

        public bool Add(GraftDefinition item)
        {
            bool added = GameBootstrap.Instance != null && GameBootstrap.Instance.Session.Add(item);
            if (added) Added?.Invoke(item);
            return added;
        }

        public bool Contains(GraftDefinition item)
        {
            return item != null && GameBootstrap.Instance != null && GameBootstrap.Instance.Session.Inventory.Exists(x => x.Id == item.Id);
        }
    }
}
