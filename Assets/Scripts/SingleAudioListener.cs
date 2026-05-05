using UnityEngine;

/// <summary>
/// Ensures exactly one AudioListener is active in the scene at all times.
///
/// Setup: attach this to your Main Camera (or any persistent GameObject).
/// It runs once on Awake and disables every AudioListener except the first
/// one it finds, stopping Unity's "2 audio listeners" spam dead.
/// </summary>
public class SingleAudioListener : MonoBehaviour
{
    private void Awake()
    {
        AudioListener[] all = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);

        if (all.Length <= 1) return;

        // Keep the first one, disable the rest.
        for (int i = 1; i < all.Length; i++)
        {
            all[i].enabled = false;
            Debug.Log($"[SingleAudioListener] Disabled extra AudioListener on '{all[i].gameObject.name}'.");
        }
    }
}
