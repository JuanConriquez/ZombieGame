using UnityEngine;

namespace ZombieGame.Combat
{
    /// <summary>
    /// Simple immediate-mode HUD. Kept intentionally small so it does not cover the map.
    /// </summary>
    [DisallowMultipleComponent]
    public class WeaponHUD : MonoBehaviour
    {
        string _currentWeaponName = "PISTOL";
        string _currentAmmoText = "0 / 0";
        bool _reloadVisible;
        float _reloadProgress;
        GUIStyle _currentStyle;
        GUIStyle _smallStyle;
        Texture2D _darkTex;
        Texture2D _activeTex;
        Texture2D _crosshairTex;

        void OnEnable()
        {
            Cursor.visible = false;
        }

        void OnDisable() => Cursor.visible = true;
        void OnDestroy() => Cursor.visible = true;

        public void SetWeaponName(string n)
        {
            _currentWeaponName = n.ToUpperInvariant();
        }

        public void SetAmmo(int magazine, int reserve)
        {
            _currentAmmoText = $"{magazine} / {reserve}";
        }

        public void SetWeaponSlot(int index, string weaponName, int magazine, int reserve, bool active)
        {
            // Kept for WeaponController compatibility. The compact HUD only shows
            // the current weapon to avoid covering the map.
        }

        public void ClearWeaponSlot(int index)
        {
            // Kept for WeaponController compatibility.
        }

        public void SetReloadProgress(bool active, float t01)
        {
            _reloadVisible = active;
            _reloadProgress = Mathf.Clamp01(t01);
        }

        public void PulseCrosshair(float spread01)
        {
            // Intentionally no visual crosshair square.
        }

        void OnGUI()
        {
            EnsureGuiStyles();

            const float width = 260f;
            const float height = 86f;
            float x = Screen.width - width - 18f;
            float y = Screen.height - height - 18f;

            GUI.Label(new Rect(x, y, width, height), $"{_currentWeaponName}\n{_currentAmmoText}", _currentStyle);

            if (_reloadVisible)
            {
                GUI.Box(new Rect(x, y - 18f, width, 12f), GUIContent.none, _smallStyle);
                GUI.Box(new Rect(x, y - 18f, width * _reloadProgress, 12f), GUIContent.none, _currentStyle);
            }

            DrawMouseCrosshair();
        }

        void EnsureGuiStyles()
        {
            if (_currentStyle != null) return;

            _darkTex = MakeTexture(new Color(0f, 0f, 0f, 0.72f));
            _activeTex = MakeTexture(new Color(1f, 0.82f, 0.28f, 0.95f));
            _crosshairTex = MakeTexture(new Color(1f, 0.9f, 0.25f, 0.95f));

            _currentStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 24,
                alignment = TextAnchor.MiddleRight,
                padding = new RectOffset(8, 14, 6, 6),
                normal = { textColor = Color.white, background = _darkTex }
            };

            _smallStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = _darkTex }
            };
        }

        void DrawMouseCrosshair()
        {
            Vector2 mouse = Event.current.mousePosition;
            const float line = 12f;
            const float gap = 4f;
            const float thickness = 2f;

            GUI.DrawTexture(new Rect(mouse.x - line - gap, mouse.y - thickness * 0.5f, line, thickness), _crosshairTex);
            GUI.DrawTexture(new Rect(mouse.x + gap, mouse.y - thickness * 0.5f, line, thickness), _crosshairTex);
            GUI.DrawTexture(new Rect(mouse.x - thickness * 0.5f, mouse.y - line - gap, thickness, line), _crosshairTex);
            GUI.DrawTexture(new Rect(mouse.x - thickness * 0.5f, mouse.y + gap, thickness, line), _crosshairTex);
        }

        static Texture2D MakeTexture(Color color)
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }

    }
}
