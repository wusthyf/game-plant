using UnityEngine;

namespace PlantSpirit.GGJ
{
    public sealed class GraftPickup : MonoBehaviour
    {
        [SerializeField] private GraftDefinition definition;
        [SerializeField] private GraftInventory inventory;

        public void Configure(GraftDefinition item, GraftInventory target)
        {
            definition = item;
            inventory = target;
            WorldArtPresentation2D.AttachPickup(gameObject, definition);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<PlayerMotor2D>() != null && inventory != null && inventory.Add(definition)) Destroy(gameObject);
        }
    }
}
