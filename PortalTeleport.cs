using UnityEngine;
using System.Collections;

public class PortalTeleport : MonoBehaviour
{
    [Header("Portal Destination")]
    [SerializeField] private Transform destination;

    [Header("Teleport Settings")]
    [SerializeField] private KeyCode teleportKey = KeyCode.E;
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private float exitDistance = 2f;
    [SerializeField] private float teleportCooldown = 0.75f;

    [Header("Teleport Audio")]
    [SerializeField] private AudioSource teleportAudio;
    [SerializeField] private AudioClip teleportSound;
    [SerializeField, Range(0f, 1f)]
    private float teleportVolume = 1f;

    private Transform player;
    private CharacterController playerController;
    private PlayerMovement playerMovement;

    private bool isTeleporting;
    private float nextTeleportTime;

    private void Awake()
    {
        // Use an AudioSource on this portal if one is available.
        if (teleportAudio == null)
            teleportAudio = GetComponent<AudioSource>();

        if (teleportAudio != null)
        {
            teleportAudio.playOnAwake = false;
            teleportAudio.loop = false;
        }
    }

    private void Update()
    {
        if (player == null || isTeleporting)
            return;

        if (Time.time < nextTeleportTime)
            return;

        float distance = Vector3.Distance(
            player.position,
            transform.position
        );

        if (distance <= interactionDistance &&
            Input.GetKeyDown(teleportKey))
        {
            StartCoroutine(TeleportPlayer());
        }
    }

    private IEnumerator TeleportPlayer()
    {
        if (destination == null)
        {
            Debug.LogWarning(
                name + ": Assign the destination portal in the Inspector."
            );
            yield break;
        }

        isTeleporting = true;

        // Play the teleport sound once.
        if (teleportAudio != null && teleportSound != null)
        {
            teleportAudio.PlayOneShot(
                teleportSound,
                teleportVolume
            );
        }

        // Calculate a position in front of the destination portal.
        Vector3 targetPosition =
            destination.position +
            destination.forward * exitDistance;

        // Preserve the player's current height.
        targetPosition.y = player.position.y;

        // Disable the CharacterController during teleportation.
        if (playerController != null)
            playerController.enabled = false;

        // Move the player to the destination.
        player.position = targetPosition;

        // Reset vertical velocity using the existing movement script.
        if (playerMovement != null)
            playerMovement.ResetVelocityAfterTeleport();

        // Wait one frame before restoring collision.
        yield return null;

        if (playerController != null)
            playerController.enabled = true;

        nextTeleportTime = Time.time + teleportCooldown;
        isTeleporting = false;

        Debug.Log("Teleported to " + destination.name);
    }

    private void OnTriggerEnter(Collider other)
    {
        RegisterPlayer(other);
    }

    private void OnTriggerStay(Collider other)
    {
        RegisterPlayer(other);
    }

    private void RegisterPlayer(Collider other)
    {
        Transform candidate = other.transform;

        // Find the player even if the collider belongs to a child object.
        if (!candidate.CompareTag("Player"))
        {
            if (candidate.root.CompareTag("Player"))
                candidate = candidate.root;
            else
                return;
        }

        player = candidate;

        playerController =
            player.GetComponent<CharacterController>();

        if (playerController == null)
        {
            playerController =
                player.GetComponentInParent<CharacterController>();
        }

        if (playerController == null)
        {
            playerController =
                player.GetComponentInChildren<CharacterController>();
        }

        playerMovement =
            player.GetComponent<PlayerMovement>();

        if (playerMovement == null)
        {
            playerMovement =
                player.GetComponentInParent<PlayerMovement>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (player == null)
            return;

        Transform exitedObject = other.transform;

        if (exitedObject == player ||
            exitedObject.IsChildOf(player) ||
            exitedObject.root == player)
        {
            player = null;
            playerController = null;
            playerMovement = null;
        }
    }
}
 
