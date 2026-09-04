using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PlantSpirit.GGJ
{
    public static class ArtResources2D
    {
        private const string Root = "PlantSpirit/";
        private static readonly Dictionary<string, Sprite[]> Sequences = new Dictionary<string, Sprite[]>();

        public static Sprite[] LoadSequence(string path)
        {
            if (Sequences.TryGetValue(path, out Sprite[] cached)) return cached;
            Sprite[] loaded = Resources.LoadAll<Sprite>(Root + path)
                .OrderBy(sprite => sprite.name, StringComparer.Ordinal)
                .ToArray();
            Sequences[path] = loaded;
            return loaded;
        }

        public static Sprite LoadSprite(string path) => Resources.Load<Sprite>(Root + path);

        public static void HidePlaceholder(GameObject owner)
        {
            if (owner.GetComponent<PlaceholderVisual>() == null) return;
            SpriteRenderer renderer = owner.GetComponent<SpriteRenderer>();
            if (renderer != null) renderer.enabled = false;
        }

        public static SpriteRenderer CreateWorldSprite(Transform parent, string name, Sprite sprite, Vector2 bottomCenter, float worldHeight, int order, Color tint)
        {
            if (sprite == null || worldHeight <= 0f) return null;
            GameObject visual = new GameObject(name);
            visual.transform.SetParent(parent, true);
            float scale = worldHeight / Mathf.Max(.001f, sprite.bounds.size.y);
            visual.transform.localScale = Vector3.one * scale;
            visual.transform.position = new Vector3(
                bottomCenter.x - sprite.bounds.center.x * scale,
                bottomCenter.y - sprite.bounds.min.y * scale,
                0f);
            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = tint;
            renderer.sortingOrder = order;
            return renderer;
        }
    }

    public sealed class SpriteSequence2D : MonoBehaviour
    {
        private Transform owner;
        private Collider2D ownerCollider;
        private SpriteRenderer spriteRenderer;
        private Sprite[] frames = Array.Empty<Sprite>();
        private float targetWorldHeight;
        private float framesPerSecond;
        private float elapsed;
        private int frameIndex;
        private bool loop;
        private bool playing;
        private bool feetAnchored;
        private Vector2 worldOffset;
        private Action completed;

        public SpriteRenderer Renderer => spriteRenderer;
        public bool IsPlaying => playing;
        public bool HasFrames => frames.Length > 0;

        public static SpriteSequence2D Create(Transform visualOwner, string childName, float worldHeight, int sortingOrder, bool anchorAtFeet, bool hidePlaceholder = true)
        {
            GameObject child = new GameObject(childName);
            child.transform.SetParent(visualOwner, false);
            SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = sortingOrder;
            SpriteSequence2D sequence = child.AddComponent<SpriteSequence2D>();
            sequence.owner = visualOwner;
            sequence.ownerCollider = visualOwner.GetComponent<Collider2D>();
            sequence.spriteRenderer = renderer;
            sequence.targetWorldHeight = worldHeight;
            sequence.feetAnchored = anchorAtFeet;
            if (hidePlaceholder) ArtResources2D.HidePlaceholder(visualOwner.gameObject);
            return sequence;
        }

        public void PlayLoop(Sprite[] sequence, float fps)
        {
            if (ReferenceEquals(frames, sequence) && loop && playing) return;
            Play(sequence, fps, true, null);
        }

        public void PlayOnce(Sprite[] sequence, float fps, Action onComplete = null) => Play(sequence, fps, false, onComplete);

        public void Show(Sprite sprite)
        {
            if (sprite == null) return;
            Play(new[] { sprite }, 1f, true, null);
        }

        public void SetFacingLeft(bool facingLeft)
        {
            if (spriteRenderer != null) spriteRenderer.flipX = facingLeft;
        }

        public void SetTint(Color color)
        {
            if (spriteRenderer != null) spriteRenderer.color = color;
        }

        public void SetWorldOffset(Vector2 offset)
        {
            worldOffset = offset;
            ApplyFrame();
        }

        public void SetLocalAngle(float degrees) => transform.localRotation = Quaternion.Euler(0f, 0f, degrees);

        private void Play(Sprite[] sequence, float fps, bool shouldLoop, Action onComplete)
        {
            frames = sequence ?? Array.Empty<Sprite>();
            framesPerSecond = Mathf.Max(.01f, fps);
            loop = shouldLoop;
            completed = onComplete;
            elapsed = 0f;
            frameIndex = 0;
            playing = frames.Length > 0;
            ApplyFrame();
        }

        private void Update()
        {
            if (!playing || frames.Length <= 1) return;
            elapsed += Time.deltaTime;
            float frameDuration = 1f / framesPerSecond;
            while (elapsed >= frameDuration)
            {
                elapsed -= frameDuration;
                frameIndex++;
                if (frameIndex >= frames.Length)
                {
                    if (loop) frameIndex = 0;
                    else
                    {
                        frameIndex = frames.Length - 1;
                        playing = false;
                        ApplyFrame();
                        Action callback = completed;
                        completed = null;
                        callback?.Invoke();
                        return;
                    }
                }
                ApplyFrame();
            }
        }

        private void ApplyFrame()
        {
            if (spriteRenderer == null || frames.Length == 0 || frameIndex >= frames.Length) return;
            Sprite sprite = frames[frameIndex];
            if (sprite == null) return;
            spriteRenderer.sprite = sprite;

            float worldScale = targetWorldHeight / Mathf.Max(.001f, sprite.bounds.size.y);
            Vector3 parentScale = owner == null ? Vector3.one : owner.lossyScale;
            transform.localScale = new Vector3(
                worldScale / Mathf.Max(.001f, Mathf.Abs(parentScale.x)),
                worldScale / Mathf.Max(.001f, Mathf.Abs(parentScale.y)),
                1f);

            if (owner == null) return;
            Vector3 anchor = owner.position;
            if (feetAnchored && ownerCollider != null) anchor.y = ownerCollider.bounds.min.y;
            if (feetAnchored)
            {
                anchor.x -= sprite.bounds.center.x * worldScale;
                anchor.y -= sprite.bounds.min.y * worldScale;
            }
            else
            {
                anchor.x -= sprite.bounds.center.x * worldScale;
                anchor.y -= sprite.bounds.center.y * worldScale;
            }
            transform.position = anchor + (Vector3)worldOffset;
        }
    }
}
