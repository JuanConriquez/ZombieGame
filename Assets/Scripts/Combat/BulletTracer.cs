using System.Collections.Generic;
using UnityEngine;

namespace ZombieGame.Combat
{
    /// <summary>
    /// Tiny pooled tracer renderer. Spawns a brief LineRenderer between two points.
    /// </summary>
    public class BulletTracer : MonoBehaviour
    {
        static BulletTracer _instance;
        public static BulletTracer Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var go = new GameObject("~BulletTracer");
                _instance = go.AddComponent<BulletTracer>();
                DontDestroyOnLoad(go);
                return _instance;
            }
        }

        readonly Stack<TracerInstance> _pool = new Stack<TracerInstance>();
        readonly List<TracerInstance> _live = new List<TracerInstance>();

        static Material _sharedMat;

        public void Spawn(Vector3 from, Vector3 to, Color color, float seconds, float width)
        {
            var t = _pool.Count > 0 ? _pool.Pop() : Create();
            t.gameObject.SetActive(true);
            t.line.startWidth = width;
            t.line.endWidth = width * 0.4f;
            t.line.startColor = color;
            t.line.endColor = new Color(color.r, color.g, color.b, 0f);
            t.line.SetPosition(0, from);
            t.line.SetPosition(1, to);
            t.remaining = Mathf.Max(0.01f, seconds);
            t.duration = t.remaining;
            _live.Add(t);
        }

        TracerInstance Create()
        {
            var go = new GameObject("Tracer");
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            lr.numCapVertices = 2;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lr.receiveShadows = false;
            if (_sharedMat == null)
            {
                var shader = Shader.Find("Sprites/Default");
                if (shader == null) shader = Shader.Find("Unlit/Color");
                _sharedMat = new Material(shader);
            }
            lr.material = _sharedMat;
            return new TracerInstance { gameObject = go, line = lr };
        }

        void Update()
        {
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                var t = _live[i];
                t.remaining -= Time.deltaTime;
                if (t.remaining <= 0f)
                {
                    t.gameObject.SetActive(false);
                    _pool.Push(t);
                    _live.RemoveAt(i);
                    continue;
                }
                float a = t.remaining / t.duration;
                var c = t.line.startColor; c.a = a;
                t.line.startColor = c;
            }
        }

        class TracerInstance
        {
            public GameObject gameObject;
            public LineRenderer line;
            public float remaining;
            public float duration;
        }
    }
}
