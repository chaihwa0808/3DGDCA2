using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public GameObject activatedCheckpoint;
    public GameObject unactivatedCheckpoint;

    private bool checkpointActivated = false;

    private void OnTriggerEnter(Collider other)
    {
        ThirdPersonController player = other.GetComponent<ThirdPersonController>();
        if (player != null)
        {
            // Deactivate the unactivated checkpoint and activate the activated one
            activatedCheckpoint.SetActive(true);
            unactivatedCheckpoint.SetActive(false);

            // Set the checkpoint position with a small offset
            player.SetCheckpoint(transform.position + new Vector3(0, 0, 0.5f));

            checkpointActivated = true;
        }
    }
}
