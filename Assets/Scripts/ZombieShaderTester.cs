using UnityEngine;

public class ZombieShaderTester : MonoBehaviour
{
    [SerializeField] private WholezombieshaderFXController zombieFX;

    private void Awake()
    {
        if (zombieFX == null)
            zombieFX = GetComponent<WholezombieshaderFXController>();
    }

    private void Update()
    {
        if (zombieFX == null)
            return;

        if (Input.GetKeyDown(KeyCode.F))
            zombieFX.PlayHitFlash();
    }
}