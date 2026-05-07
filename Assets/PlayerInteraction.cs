using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    public KeyCode interactKey = KeyCode.F;
    public float interactRange = 2f;

    private Inventory inventory;

    void Awake()
    {
        inventory = GetComponent<Inventory>();
    }

    void Update()
    {
        if (Input.GetKeyDown(interactKey))
        {
            TryInteractWithDoor();
        }
    }

    void TryInteractWithDoor()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, interactRange);

        foreach (Collider hit in hits)
        {
            Doors door = hit.GetComponent<Doors>();

            if (door != null)
            {
                door.TryOpenOrClose(inventory);
                return;
            }
        }

        Debug.Log("[Interact] No door nearby.");
    }
}