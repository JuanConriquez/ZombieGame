using UnityEngine;

namespace ZombieGame.Combat
{
    /// <summary>
    /// Runtime placeholder models for quick playtesting: simple player accents and
    /// distinct gun silhouettes for pistol, shotgun, and assault rifle.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(WeaponController))]
    public class SimplePlayerGunVisuals : MonoBehaviour
    {
        [Tooltip("Hide the original capsule/sphere mesh so the placeholder model is visible.")]
        public bool hideOriginalBodyRenderer = true;

        WeaponController _weaponController;
        Transform _root;
        Transform _gunRoot;
        WeaponKind? _lastKind;

        Material _armorMat;
        Material _darkMat;
        Material _accentMat;
        Material _shotgunMat;
        Material _rifleMat;

        void Awake()
        {
            _weaponController = GetComponent<WeaponController>();
            EnsureMaterials();
            HideOriginalBodyIfNeeded();
            BuildPlayerAccents();
            RefreshNow();
        }

        void LateUpdate()
        {
            var active = _weaponController.ActiveWeaponData;
            if (active != null && _lastKind != active.kind)
                RefreshNow();
        }

        public void RefreshNow()
        {
            var active = _weaponController.ActiveWeaponData;
            if (active == null) return;

            EnsureGunRoot();
            ClearChildren(_gunRoot);

            switch (active.kind)
            {
                case WeaponKind.Shotgun:
                    BuildShotgun();
                    break;
                case WeaponKind.AssaultRifle:
                    BuildAssaultRifle();
                    break;
                default:
                    BuildPistol();
                    break;
            }

            _lastKind = active.kind;
        }

        void BuildPlayerAccents()
        {
            _root = transform.Find("CombatVisuals");
            if (_root != null) return;

            _root = new GameObject("CombatVisuals").transform;
            _root.SetParent(transform, false);

            // Compact tactical body that replaces the old white capsule visually.
            var torso = CreatePrimitive("Torso", PrimitiveType.Cube, _root, _armorMat);
            torso.transform.localPosition = new Vector3(0f, 0.58f, 0f);
            torso.transform.localScale = new Vector3(0.44f, 0.62f, 0.34f);

            var visor = CreatePrimitive("ForwardVisor", PrimitiveType.Cube, _root, _darkMat);
            visor.transform.localPosition = new Vector3(0f, 1.17f, 0.34f);
            visor.transform.localScale = new Vector3(0.22f, 0.08f, 0.12f);

            // Helmet / facing marker.
            var helmet = CreatePrimitive("Helmet", PrimitiveType.Sphere, _root, _armorMat);
            helmet.transform.localPosition = new Vector3(0f, 1.15f, 0.12f);
            helmet.transform.localScale = new Vector3(0.32f, 0.22f, 0.32f);

            // Shoulder pads make the player easier to read from top-down.
            var leftShoulder = CreatePrimitive("LeftShoulder", PrimitiveType.Cube, _root, _armorMat);
            leftShoulder.transform.localPosition = new Vector3(-0.32f, 0.78f, 0.05f);
            leftShoulder.transform.localScale = new Vector3(0.18f, 0.12f, 0.28f);

            var rightShoulder = CreatePrimitive("RightShoulder", PrimitiveType.Cube, _root, _armorMat);
            rightShoulder.transform.localPosition = new Vector3(0.32f, 0.78f, 0.05f);
            rightShoulder.transform.localScale = new Vector3(0.18f, 0.12f, 0.28f);
        }

        void HideOriginalBodyIfNeeded()
        {
            if (!hideOriginalBodyRenderer) return;

            var renderer = GetComponent<Renderer>();
            if (renderer != null)
                renderer.enabled = false;
        }

        void EnsureGunRoot()
        {
            if (_gunRoot != null) return;

            if (_root == null) BuildPlayerAccents();
            _gunRoot = new GameObject("GunModel").transform;
            _gunRoot.SetParent(_root, false);
            _gunRoot.localPosition = new Vector3(0.18f, 0.88f, 0.55f);
            _gunRoot.localRotation = Quaternion.identity;
        }

        void BuildPistol()
        {
            var body = CreatePrimitive("PistolBody", PrimitiveType.Cube, _gunRoot, _darkMat);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(0.14f, 0.12f, 0.34f);

            var barrel = CreatePrimitive("PistolBarrel", PrimitiveType.Cube, _gunRoot, _darkMat);
            barrel.transform.localPosition = new Vector3(0f, 0.03f, 0.27f);
            barrel.transform.localScale = new Vector3(0.08f, 0.08f, 0.28f);

            var grip = CreatePrimitive("PistolGrip", PrimitiveType.Cube, _gunRoot, _accentMat);
            grip.transform.localPosition = new Vector3(0f, -0.12f, -0.08f);
            grip.transform.localRotation = Quaternion.Euler(-25f, 0f, 0f);
            grip.transform.localScale = new Vector3(0.11f, 0.22f, 0.1f);
        }

        void BuildShotgun()
        {
            var stock = CreatePrimitive("ShotgunStock", PrimitiveType.Cube, _gunRoot, _shotgunMat);
            stock.transform.localPosition = new Vector3(0f, -0.02f, -0.22f);
            stock.transform.localScale = new Vector3(0.18f, 0.14f, 0.3f);

            var receiver = CreatePrimitive("ShotgunReceiver", PrimitiveType.Cube, _gunRoot, _darkMat);
            receiver.transform.localPosition = new Vector3(0f, 0f, 0.02f);
            receiver.transform.localScale = new Vector3(0.16f, 0.12f, 0.28f);

            var barrel = CreatePrimitive("ShotgunDoubleBarrel", PrimitiveType.Cube, _gunRoot, _darkMat);
            barrel.transform.localPosition = new Vector3(0f, 0.04f, 0.42f);
            barrel.transform.localScale = new Vector3(0.16f, 0.08f, 0.58f);
        }

        void BuildAssaultRifle()
        {
            var body = CreatePrimitive("RifleBody", PrimitiveType.Cube, _gunRoot, _rifleMat);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(0.14f, 0.12f, 0.42f);

            var barrel = CreatePrimitive("RifleBarrel", PrimitiveType.Cube, _gunRoot, _darkMat);
            barrel.transform.localPosition = new Vector3(0f, 0.04f, 0.43f);
            barrel.transform.localScale = new Vector3(0.07f, 0.07f, 0.5f);

            var magazine = CreatePrimitive("RifleMagazine", PrimitiveType.Cube, _gunRoot, _darkMat);
            magazine.transform.localPosition = new Vector3(0f, -0.16f, -0.02f);
            magazine.transform.localRotation = Quaternion.Euler(-10f, 0f, 0f);
            magazine.transform.localScale = new Vector3(0.12f, 0.28f, 0.12f);
        }

        GameObject CreatePrimitive(string name, PrimitiveType type, Transform parent, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);

            var collider = go.GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = mat;

            return go;
        }

        void EnsureMaterials()
        {
            _armorMat = CreateMaterial("PlayerArmorBlue", new Color(0.15f, 0.45f, 0.95f, 1f));
            _darkMat = CreateMaterial("GunDarkMetal", new Color(0.03f, 0.035f, 0.04f, 1f));
            _accentMat = CreateMaterial("GunGripAccent", new Color(0.25f, 0.16f, 0.08f, 1f));
            _shotgunMat = CreateMaterial("ShotgunWood", new Color(0.35f, 0.18f, 0.08f, 1f));
            _rifleMat = CreateMaterial("RifleGreen", new Color(0.12f, 0.22f, 0.16f, 1f));
        }

        Material CreateMaterial(string name, Color color)
        {
            var shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Diffuse");
            var mat = new Material(shader) { name = name, color = color };
            return mat;
        }

        static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                Destroy(parent.GetChild(i).gameObject);
        }
    }
}
