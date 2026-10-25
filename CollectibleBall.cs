using UnityEngine;

public class CollectibleBall : MonoBehaviour
{
    [Header("Ball Settings")]
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private float floatHeight = 0.2f;
    [SerializeField] private float floatSpeed = 2f;

    private Vector3 startPosition;
    private bool collected;

    private void Start()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        transform.Rotate(
            Vector3.up,
            rotationSpeed * Time.deltaTime,
            Space.World
        );

        Vector3 position = startPosition;

        position.y +=
            Mathf.Sin(Time.time * floatSpeed) * floatHeight;

        transform.position = position;
    }

   private void OnTriggerEnter(Collider other)
{
    if (!other.CompareTag("Player"))
        return;

    GameManager gameManager =
        FindFirstObjectByType<GameManager>();

    if (gameManager == null)
    {
        Debug.LogWarning("GameManager was not found!");
        return;
    }

    gameManager.CollectBall();
    Destroy(gameObject);
}
}

