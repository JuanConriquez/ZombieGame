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
        if (Input.GetKeyDown(KeyCode.F))
            zombieFX.PlayHitFlash();

        if (Input.GetKeyDown(KeyCode.X))
            zombieFX.PlayDissolve();
    }
}