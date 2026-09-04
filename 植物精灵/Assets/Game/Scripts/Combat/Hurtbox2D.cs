using System.Collections.Generic;
using UnityEngine;

namespace PlantSpirit.GGJ
{
    public interface IDamageReceiver { bool TryReceive(DamageInfo info); }

    public sealed class Hurtbox2D : MonoBehaviour
    {
        private readonly HashSet<int> receivedInstances = new HashSet<int>();
        public IDamageReceiver Receiver { get; set; }

        private void Awake()
        {
            if (Receiver != null) return;
            MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (!(behaviours[i] is IDamageReceiver receiver)) continue;
                Receiver = receiver;
                break;
            }
        }

        public bool Receive(DamageInfo info)
        {
            if (info.AttackInstanceId != 0 && !receivedInstances.Add(info.AttackInstanceId)) return false;
            bool accepted = Receiver != null && Receiver.TryReceive(info);
            if (!accepted && info.AttackInstanceId != 0) receivedInstances.Remove(info.AttackInstanceId);
            return accepted;
        }

        public void ClearHistory() => receivedInstances.Clear();
    }
}
