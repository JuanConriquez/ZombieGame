using UnityEngine;
using Unity.Netcode;
using ZombieGame.Combat;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    public float moveSpeed = 6f;
    public Camera playerCamera;
    public float mouseSensitivity = 120f;

    private CharacterController controller;
    private float currentCameraYaw;
    private bool isDead = false;
    public bool IsDead => isDead;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            playerCamera = GetComponentInChildren<Camera>();
        }
        else
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null) cam.gameObject.SetActive(false);
        }

        // listen for death so we can hide the player
        Health health = GetComponent<Health>();
        if (health != null)
            health.OnDied += OnPlayerDied;
    }

    // called by Health when hp hits 0
    void OnPlayerDied(GameObject go)
    {
        isDead = true;

        // hide the player visually
       // foreach (Renderer r in GetComponentsInChildren<Renderer>())
         //   r.enabled = false;

        // stop movement
        controller.enabled = false;
    }

    // called by NetworkGameManager at the start of each countdown so player respawns between waves
    public void Respawn(Vector3 spawnPosition)
    {
        isDead = false;
        controller.enabled = true;
        transform.position = spawnPosition;

        // show the player again
        //foreach (Renderer r in GetComponentsInChildren<Renderer>())
           // r.enabled = true;

        // heal back to full — only server can write to the NetworkVariable
        Health health = GetComponent<Health>();
        if (health != null && IsServer)
            health.ServerHeal(health.MaxHealth);
    }

    void Update()
    {
        if (!IsOwner) return;
        if (isDead) return;

        float mouseX = Input.GetAxis("Mouse X");
        currentCameraYaw += mouseX * mouseSensitivity * Time.deltaTime;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 cameraForward = Quaternion.Euler(0f, currentCameraYaw, 0f) * Vector3.back;
        Vector3 cameraRight = Quaternion.Euler(0f, currentCameraYaw, 0f) * Vector3.left;
        Vector3 moveDir = (cameraRight * h + cameraForward * v);

        if (moveDir.sqrMagnitude > 1f)
            moveDir.Normalize();

        controller.Move(moveDir * moveSpeed * Time.deltaTime);
        controller.Move(Vector3.down * 9.8f * Time.deltaTime);

        if (playerCamera == null) return;

        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (groundPlane.Raycast(ray, out float distance))
        {
            Vector3 lookPoint = ray.GetPoint(distance);
            lookPoint.y = transform.position.y;
            transform.LookAt(lookPoint);
        }
    }

    void LateUpdate()
    {
        if (!IsOwner) return;
        if (playerCamera == null) return;

        Vector3 cameraOffset = Quaternion.Euler(0f, currentCameraYaw, 0f) * new Vector3(0f, 30f, 35f);
        playerCamera.transform.position = transform.position + cameraOffset;
        playerCamera.transform.LookAt(transform.position);
    }
}
