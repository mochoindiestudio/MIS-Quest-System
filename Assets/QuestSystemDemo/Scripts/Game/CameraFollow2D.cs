using UnityEngine;

namespace MochoIndieStudio.QuestSystemDemo
{
    /// <summary>Smoothly keeps the camera centred on a target in the XY plane, preserving its Z.</summary>
    public sealed class CameraFollow2D : MonoBehaviour
    {
        [SerializeField]
        private Transform target;

        [SerializeField]
        private float smoothTime = 0.15f;

        private Vector3 velocity;

        /// <summary>Reassigns the follow target (used by the scene builder).</summary>
        public void SetTarget(Transform newTarget) => target = newTarget;

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 goal = new Vector3(target.position.x, target.position.y, transform.position.z);
            transform.position = Vector3.SmoothDamp(transform.position, goal, ref velocity, smoothTime);
        }
    }
}
