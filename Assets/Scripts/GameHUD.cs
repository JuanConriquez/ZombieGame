using UnityEngine;
using Unity.Netcode;
using ZombieGame.Combat;

// Shows wave info and health bar on screen for the local player
public class GameHUD : NetworkBehaviour
{
    private WaveManager waveManager;
    private Health health;

    // textures for the health bar
    private Texture2D darkBg;
    private Texture2D greenBar;
    private Texture2D redBar;

    public override void OnNetworkSpawn()
    {
        // grab the health on this same player object
        health = GetComponent<Health>();

        // find the wave manager in the scene
        waveManager = FindFirstObjectByType<WaveManager>();
    }

    void OnGUI()
    {
        // only draw for the player we control
        if (!IsOwner) return;

        MakeTextures();
        DrawWaveInfo();
        DrawHealthBar();
    }

    void DrawWaveInfo()
    {
        if (waveManager == null) return;

        // wave number top left
        GUI.Box(new Rect(18, 18, 200, 35), $"WAVE {waveManager.CurrentWave}");
        
        // countdown between waves or zombies remaining
        if (waveManager.State == WaveManager.WaveState.Countdown)
        {
            GUI.Box(new Rect(18, 58, 200, 35), $"Next wave in {waveManager.CountdownSeconds}s");
        }
        else
        {
            GUI.Box(new Rect(18, 58, 200, 35), $"Zombies: {waveManager.AliveZombies} / {waveManager.TotalThisWave}");
        }
    }

    void DrawHealthBar()
    {
        if (health == null) return;

        float percent = health.CurrentHealth / health.MaxHealth;
        float barWidth = 220f;
        float x = 18;
        float y = Screen.height - 50;

        // background
        GUI.DrawTexture(new Rect(x, y, barWidth, 25), darkBg);

        // green or red fill depending on health
        Texture2D fill = percent < 0.3f ? redBar : greenBar;
        GUI.DrawTexture(new Rect(x, y, barWidth * percent, 25), fill);

        // hp number on top
        GUI.Label(new Rect(x, y, barWidth, 25), $"  HP: {Mathf.CeilToInt(health.CurrentHealth)}");
    }

    // only make textures once
    void MakeTextures()
    {
        if (darkBg != null) return;

        darkBg = new Texture2D(1, 1);
        darkBg.SetPixel(0, 0, new Color(0, 0, 0, 0.7f));
        darkBg.Apply();

        greenBar = new Texture2D(1, 1);
        greenBar.SetPixel(0, 0, new Color(0.2f, 0.8f, 0.2f, 1f));
        greenBar.Apply();

        redBar = new Texture2D(1, 1);
        redBar.SetPixel(0, 0, new Color(0.85f, 0.15f, 0.15f, 1f));
        redBar.Apply();
    }
}
