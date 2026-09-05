using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace PlantSpirit.GGJ.Tests
{
    public sealed class GameFeelTests
    {
        [Test]
        public void CombatFeelTuningUsesWiderMeleeRangesAndAirControl()
        {
            PlayerConfig player = AssetDatabase.LoadAssetAtPath<PlayerConfig>("Assets/Game/Data/PlayerConfig.asset");
            AttackDefinition basic = AssetDatabase.LoadAssetAtPath<AttackDefinition>("Assets/Game/Data/Attack_default_attack.asset");
            AttackDefinition vine = AssetDatabase.LoadAssetAtPath<AttackDefinition>("Assets/Game/Data/Attack_vine_attack.asset");

            Assert.That(player.AirAcceleration, Is.EqualTo(42f).Within(.001f));
            Assert.That(basic.Size.x, Is.EqualTo(1.7f).Within(.001f));
            Assert.That(basic.Range, Is.EqualTo(1.7f).Within(.001f));
            Assert.That(vine.Size.x, Is.EqualTo(3.4f).Within(.001f));
            Assert.That(vine.Range, Is.EqualTo(3.4f).Within(.001f));
            Assert.That(PlayerCombat.MeleeHitStopSeconds, Is.EqualTo(.2f).Within(.001f));
        }

        [Test]
        public void NonLethalDamageRaisesHurtAnimationEvent()
        {
            GameObject player = new GameObject("HurtEventPlayer");
            player.AddComponent<Rigidbody2D>();
            player.AddComponent<BoxCollider2D>();
            PlayerMotor2D motor = player.AddComponent<PlayerMotor2D>();
            PlayerConfig config = ScriptableObject.CreateInstance<PlayerConfig>();
            motor.Configure(config, 0, null);
            PlayerHealth health = player.AddComponent<PlayerHealth>();
            health.Configure(config, motor);
            bool receivedHurtEvent = false;
            health.Hurt += () => receivedHurtEvent = true;

            Assert.That(health.TryReceive(new DamageInfo { Amount = 1f }), Is.True);
            Assert.That(receivedHurtEvent, Is.True);

            Object.DestroyImmediate(config);
            Object.DestroyImmediate(player);
        }
    }
}
