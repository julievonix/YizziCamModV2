using System.Collections.Generic;
using UnityEngine;

namespace YizziCamModV2.Comps
{
    /// <summary>
    /// Live mini-render of the local monkey above SIDE; tap cycles front / right / back / left.
    /// </summary>
    public class WardrobeModelPreview : MonoBehaviour
    {
        public static WardrobeModelPreview Instance { get; private set; }

        Camera _cam;
        RenderTexture _rt;
        int _viewIndex;

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            if (_cam != null && _cam.gameObject != null)
                Destroy(_cam.gameObject);
            if (_rt != null)
            {
                _rt.Release();
                Destroy(_rt);
            }
        }

        void OnEnable()
        {
            _viewIndex = 0;
        }

        void OnDisable() { }

        public void CycleView()
        {
            _viewIndex = (_viewIndex + 1) % 4;
        }

        public void Setup(Camera previewCamera, RenderTexture rt)
        {
            _cam = previewCamera;
            _rt = rt;
        }

        void LateUpdate()
        {
            if (_cam == null || !_cam.gameObject.activeInHierarchy)
                return;
            var rig = GorillaTagger.Instance?.offlineVRRig;
            if (rig == null)
                return;
            if (_cam.cullingMask == 0)
                _cam.cullingMask = BuildRigCullingMask(rig);
            UpdateCameraPose(rig);

            _cam.enabled = false;

            var tablet = CameraController.Instance?.CameraTablet;
            List<Renderer> hiddenRenderers = null;
            List<Canvas> hiddenCanvases = null;
            if (tablet != null)
            {
                hiddenRenderers = new List<Renderer>();
                foreach (var r in tablet.GetComponentsInChildren<Renderer>(false))
                {
                    if (r.enabled)
                    {
                        r.enabled = false;
                        hiddenRenderers.Add(r);
                    }
                }
                hiddenCanvases = new List<Canvas>();
                foreach (var c in tablet.GetComponentsInChildren<Canvas>(false))
                {
                    if (c.enabled)
                    {
                        c.enabled = false;
                        hiddenCanvases.Add(c);
                    }
                }
            }

            _cam.Render();

            if (hiddenRenderers != null)
                foreach (var r in hiddenRenderers)
                    r.enabled = true;
            if (hiddenCanvases != null)
                foreach (var c in hiddenCanvases)
                    c.enabled = true;
        }

        static int BuildRigCullingMask(VRRig rig)
        {
            var mask = 0;
            GatherLayersRecursive(rig.transform, ref mask);
            if (mask == 0)
                mask = ~0;
            mask &= ~(1 << 18);
            mask &= ~(1 << 5);
            return mask;
        }

        static void GatherLayersRecursive(Transform t, ref int mask)
        {
            mask |= 1 << t.gameObject.layer;
            for (var i = 0; i < t.childCount; i++)
                GatherLayersRecursive(t.GetChild(i), ref mask);
        }

        void UpdateCameraPose(VRRig rig)
        {
            var body = rig.transform;
            var f = body.forward;
            f.y = 0f;
            if (f.sqrMagnitude < 1e-4f)
                f = Vector3.forward;
            f.Normalize();
            var right = Vector3.Cross(Vector3.up, f).normalized;
            var origin = body.position + Vector3.up * 0.15f;
            const float dist = 1.6f;
            Vector3 camPos;
            switch (_viewIndex)
            {
                case 0:
                    camPos = origin + f * dist;
                    break;
                case 1:
                    camPos = origin + right * dist;
                    break;
                case 2:
                    camPos = origin - f * dist;
                    break;
                default:
                    camPos = origin - right * dist;
                    break;
            }

            _cam.transform.position = camPos;
            _cam.transform.LookAt(origin, Vector3.up);
        }

        internal static void Build(Transform wardrobePage, Vector3 basePos, float yPage, float rzMid, Transform btnTemplate)
        {
            var comp = wardrobePage.GetComponent<WardrobeModelPreview>();
            if (comp != null && comp._cam != null)
                return;
            if (comp == null)
                comp = wardrobePage.gameObject.AddComponent<WardrobeModelPreview>();

            const float w = 0.28f;
            const float h = 0.42f;

            var previewRoot = new GameObject("WardrobePreviewRoot");
            previewRoot.transform.SetParent(wardrobePage, false);
            previewRoot.transform.localPosition = basePos + new Vector3(0f, yPage + h * 0.5f + 0.12f, rzMid);
            previewRoot.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "WBPreviewBtn";
            quad.layer = 18;
            quad.transform.SetParent(previewRoot.transform, false);
            quad.transform.localPosition = new Vector3(0f, 0f, -0.045f);
            quad.transform.localRotation = Quaternion.identity;
            quad.transform.localScale = new Vector3(w, h, 1f);
            Object.Destroy(quad.GetComponent<MeshCollider>());
            var box = quad.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = Vector3.zero;
            box.size = new Vector3(1f, 1f, 0.05f);
            quad.AddComponent<YzGButton>();

            var rt = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 1,
                name = "YizziWardrobePreviewRT"
            };
            rt.Create();

            var sh = Shader.Find("Unlit/Texture")
                     ?? Shader.Find("Universal Render Pipeline/Unlit")
                     ?? Shader.Find("Sprites/Default");
            var mat = new Material(sh)
            {
                mainTexture = rt
            };
            quad.GetComponent<MeshRenderer>().material = mat;

            var camGo = new GameObject("YizziWardrobePreviewCam");
            camGo.transform.SetParent(null, false);
            var cam = camGo.AddComponent<Camera>();
            cam.targetTexture = rt;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.06f, 0.06f, 0.08f, 1f);
            cam.depth = -80;
            cam.nearClipPlane = 0.06f;
            cam.farClipPlane = 10f;
            cam.fieldOfView = 40f;
            cam.cullingMask = 0;
            cam.enabled = false;

            comp.Setup(cam, rt);
        }
    }
}
