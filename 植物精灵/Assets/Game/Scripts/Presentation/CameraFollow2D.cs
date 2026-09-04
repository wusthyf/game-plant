using UnityEngine;

namespace PlantSpirit.GGJ
{
    public sealed class CameraFollow2D : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float minX = -10f;
        [SerializeField] private float maxX = 25f;
        [SerializeField] private float followSpeed = 8f;

        public Transform Target => target;

        public void Configure(Transform followTarget, float leftBound, float rightBound)
        {
            target = followTarget;
            minX = leftBound;
            maxX = rightBound;
        }

        private void Awake()
        {
            ResolveTarget();
            SnapToTarget();
        }

        private void LateUpdate()
        {
            if (!ResolveTarget()) return;
            float x = Mathf.Clamp(target.position.x, minX, maxX);
            transform.position = Vector3.Lerp(transform.position, new Vector3(x, 0f, -10f), followSpeed * Time.unscaledDeltaTime);
        }

        private bool ResolveTarget()
        {
            if (target != null) return true;
            PlayerMotor2D player = FindObjectOfType<PlayerMotor2D>();
            if (player != null) target = player.transform;
            return target != null;
        }

        private void SnapToTarget()
        {
            if (target == null) return;
            float x = Mathf.Clamp(target.position.x, minX, maxX);
            transform.position = new Vector3(x, 0f, -10f);
        }
    }
}
