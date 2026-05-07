using UnityEngine;

public class Doors : MonoBehaviour
{
    // Locked Door settings
    // requiresKey: Variable for when door requires a key (defualt to false to allow all doors to be open)
    // requiredKeyID: To allow specific keys to open specific doors
    [Header("Lock Settings")]
    public bool requiresKey = false; 
    public string requiredKeyID = "RoomA";

    [Tooltip("For doors that need all 4 keys")]
    public bool requiresAllKeys = false;
    public string[] requiredKeyIDs; 

    // Door health settings for enemy to break down door
    public float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    public bool isBroken = false;

    // Door transform animation to simulate door opening
    [Header("Animation")]
    [Tooltip("Transform that rotates when the door opens. Default to this transform.")]
    public Vector3 slideDirection = Vector3.right;
    public float slideDistance = 1f;
    public float slideSpeed = 3f;
    
    public bool IsOpen => isOpen;
    private bool isOpen = false; //tracks open doors

    private Vector3 closedLocalPos;
    private Vector3 openLocalPos;

    /*
    private Quaternion closedRotation; 
    private Quaternion openRotation;
    private Collider doorCollider;
    */
    void Awake()
    {
        /*
        if (doorTransform == null) //Use doors own transform if no transform is provided
        {
            doorTransform = transform;
        }

        doorCollider = GetComponent<Collider>();
        */

        currentHealth = maxHealth; //Initialize health

        closedLocalPos = transform.localPosition;
        Vector3 dir = slideDirection.sqrMagnitude > 0.001f ? slideDirection.normalized : Vector3.right;
        openLocalPos = closedLocalPos + (dir * slideDistance);
    }

    // TryOpenOrClose(Inventory inventory)
    // Function for opening/closing doors
    // Later to be integrated in Interactor (where player can interact with objects)
    public void TryOpenOrClose(Inventory inventory)
    {
        if (isBroken) // When enemy passes through door
        {
            return;
        }

        //for the victory door that need 4 keys
        if (requiresAllKeys)
        {
            if (inventory == null || !inventory.HasAllKeys(requiredKeyIDs))
            { 
            Debug.Log("[Door] Locked. Requires ALL keys to open");
                return;
        }
    }

      else  if (requiresKey) //Check if player has correct key
        {
            if (inventory == null || !inventory.HasKey(requiredKeyID)) //If player has no inventory or wrong key...
            {
                Debug.Log($"[Door] Locked. Requires key: {requiredKeyID}"); //...Door remains locked and closed
                return;
            }
        }

        //Toggle open/closed state for doors
        isOpen = !isOpen;
        Debug.Log($"[Door] {(isOpen ? "Opening" : "Closing")} door.");
    }

    // TakeDamage()
    // Enemy calls this to damage door
    // If health hits 0, door breaks
    public void TakeDamage(float amount)
    {
        if (isBroken) return;

        currentHealth -= amount;
        Debug.Log($"[Door] Took {amount} damage. HP now: {currentHealth}");

        if (currentHealth <= 0f)
        {
            BreakDoor();
        }
    }

    // BreakDoor()
    // Door is no longer blockable
    private void BreakDoor()
    {
        isBroken = true;
        currentHealth = 0f;

        Debug.Log("[Door] Door BROKE.");

        Destroy(gameObject);
        /*
        if (doorCollider != null) //Door no longer blockable
            doorCollider.enabled = false;

        isOpen = true;
        */
    }

    void Update()
    {
        /*
        // Juan - added this so i could open and test it opens when i click E
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryOpenOrClose(null); // no inventory needed
        }
        */

        if (isBroken) return;
        Vector3 targetPos = isOpen ? openLocalPos : closedLocalPos; //Figure out where the door should be depending on the state
        // If door is opening, openRotation
        // If door is closing, closedRotation

        // Create "sliding" animation i.e. slide open or closed
        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            targetPos,
            Time.deltaTime * slideSpeed);
    }

    // Used by the enemy (or other systems) to open the door without checking keys.
    public void ForceOpen()
    {
        isOpen = true;
    }
}
