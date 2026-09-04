using UnityEngine;

namespace PlantSpirit.GGJ
{
    public sealed class StatusController : MonoBehaviour
    {
        public float SlowPercent { get; private set; }
        private float poisonUntil;
        private float poisonDamage;
        private float nextPoisonTick;
        private IDamageReceiver receiver;

        private void Awake() { receiver = GetComponent<Hurtbox2D>()?.Receiver; }
        public void ApplyPoison(float damagePerSecond, float duration, float slow)
        {
            ApplyPoisonAt(damagePerSecond, duration, slow, Time.time);
        }
        public void ApplyPoisonAt(float damagePerSecond, float duration, float slow, float now)
        {
            receiver = GetComponent<Hurtbox2D>()?.Receiver ?? receiver;
            poisonUntil = Mathf.Max(poisonUntil, now + duration);
            poisonDamage = damagePerSecond;
            SlowPercent = Mathf.Max(SlowPercent, slow);
            if (nextPoisonTick <= now) nextPoisonTick = now + 1f;
        }
        private void Update() => TickAt(Time.time);
        public void TickAt(float now)
        {
            while (nextPoisonTick > 0f && nextPoisonTick <= poisonUntil + .0001f && now >= nextPoisonTick)
            {
                float tickTime = nextPoisonTick;
                nextPoisonTick += 1f;
                receiver?.TryReceive(new DamageInfo { AttackInstanceId = GetInstanceID() + Mathf.FloorToInt(tickTime * 100f), Amount = poisonDamage, Source = gameObject, Type = DamageType.Poison });
            }
            if (now < poisonUntil) return;
            poisonUntil = 0f;
            poisonDamage = 0f;
            nextPoisonTick = 0f;
            SlowPercent = 0f;
        }
    }
}
