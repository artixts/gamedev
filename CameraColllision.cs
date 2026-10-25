 
using UnityEngine;

public class CameraCollision : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;

    [Header("Collision Settings")]
    [SerializeField] private float cameraRadius = 0.2f;
    [SerializeField] private float collisionOffset = 0.1f;
    [SerializeField] private LayerMask wallLayers = ~0;

    private Vector3 initialLocalPosition;

    private void Start()
    {
        initialLocalPosition = transform.localPosition;

        if (player == null && transform.parent != null)
        {
            player = transform.parent;
        }
    }

    private void LateUpdate()
    {
        if (player == null)
            return;

        Vector3 desiredPosition =
            player.TransformPoint(initialLocalPosition);

        Vector3 direction = desiredPosition - player.position;
        float distance = direction.magnitude;

        if (distance < 0.01f)
            return;

        direction.Normalize();

        if (Physics.SphereCast(
            player.position,
            cameraRadius,
            direction,
            out RaycastHit hit,
            distance,
            wallLayers,
            QueryTriggerInteraction.Ignore))
        {
            if (hit.transform == player ||
                hit.transform.IsChildOf(player))
            {
                return;
            }

            float safeDistance = Mathf.Max(
                0.05f,
                hit.distance - collisionOffset
            );

            transform.position =
                player.position + direction * safeDistance;
        }
        else
        {
            transform.position = desiredPosition;
        }
    }
}
 