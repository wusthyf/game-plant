using System;
using UnityEngine;

namespace PlantSpirit.GGJ
{
    public sealed class BossFogTrigger : MonoBehaviour
    {
        private bool triggered;
        private BossApproachPrompt prompt;
        public event Action PlayerEntered;

        public void Configure(BossApproachPrompt approachPrompt)
        {
            prompt = approachPrompt;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (triggered || other.GetComponent<PlayerMotor2D>() == null) return;
            triggered = true;
            prompt?.Show();
            PlayerEntered?.Invoke();
        }
    }

    public sealed class BossFogVisual : MonoBehaviour
    {
        private SpriteRenderer[] layers;
        private float[] offsets;

        public void Configure()
        {
            layers = new SpriteRenderer[5];
            offsets = new float[layers.Length];
            for (int i = 0; i < layers.Length; i++)
            {
                GameObject layer = new GameObject("MistLayer" + i);
                layer.transform.SetParent(transform, false);
                layer.transform.localPosition = new Vector3(0f, -1.35f + i * .67f, 0f);
                layer.AddComponent<PlaceholderVisual>().Configure(new Color(.94f, .97f, 1f, .15f + i * .025f), new Vector2(1.55f, .92f), 4);
                layers[i] = layer.GetComponent<SpriteRenderer>();
                offsets[i] = i * .82f;
            }
        }

        private void Update()
        {
            if (layers == null) return;
            for (int i = 0; i < layers.Length; i++)
            {
                float wave = Mathf.Sin(Time.time * (1.1f + i * .12f) + offsets[i]);
                layers[i].transform.localPosition = new Vector3(wave * .16f, -1.35f + i * .67f, 0f);
                Color color = layers[i].color;
                color.a = .14f + i * .025f + (wave + 1f) * .025f;
                layers[i].color = color;
            }
        }
    }
}
