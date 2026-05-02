using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

namespace YizziCamModV2.Comps
{
    public class TabletReport : MonoBehaviour
    {
        public static TabletReport Instance { get; private set; }

        // --- Grid view state ---
        readonly List<GameObject> _items = new List<GameObject>();
        readonly List<int> _playerActorNumbers = new List<int>();
        readonly List<SpeakerEntry> _speakerEntries = new List<SpeakerEntry>();
        GorillaPlayerScoreboardLine[] _cachedLines;
        float _nextSpeakerCheck;
        float _nextRefreshCheck;
        float _nextForceRefresh;
        const float SpeakerCheckInterval = 0.3f;
        const float RefreshCheckInterval = 1.5f;
        const float ForceRefreshInterval = 3f;
        int _lastPlayerHash;
        Transform _container;
        GameObject _btnTemplate;

        // --- Detail view state ---
        bool _inDetail;
        int _detailActorNumber;
        readonly List<GameObject> _detailItems = new List<GameObject>();
        Camera _detailCam;
        RenderTexture _detailRT;
        VRRig _detailRig;
        int _detailViewIndex;
        Text _detailFpsText;
        Text _detailPlatformText;
        Text _detailInfoText;
        float _nextDetailFpsUpdate;
        bool _detailMuted;
        bool _voiceFocusActive;

        struct SpeakerEntry
        {
            public int actorNumber;
            public GameObject iconGO;
        }

        void Awake() { Instance = this; }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            CleanupDetailCamera();
        }

        internal void Init(Transform container, GameObject btnTemplate)
        {
            _container = container;
            _btnTemplate = btnTemplate;
        }

        void LateUpdate()
        {
            if (!gameObject.activeInHierarchy) return;

            EnforceMutes();

            if (_inDetail)
            {
                RenderDetailPreview();
                UpdateDetailInfo();
                return;
            }

            if (Time.time >= _nextForceRefresh)
            {
                _nextForceRefresh = Time.time + ForceRefreshInterval;
                Refresh();
                return;
            }

            if (Time.time >= _nextRefreshCheck)
            {
                _nextRefreshCheck = Time.time + RefreshCheckInterval;
                int hash = ComputePlayerHash();
                if (hash != _lastPlayerHash)
                {
                    _lastPlayerHash = hash;
                    Refresh();
                    return;
                }
            }

            if (_speakerEntries.Count == 0) return;
            if (Time.time < _nextSpeakerCheck) return;
            _nextSpeakerCheck = Time.time + SpeakerCheckInterval;

            if (_cachedLines == null)
                _cachedLines = FindObjectsOfType<GorillaPlayerScoreboardLine>(true);

            foreach (var entry in _speakerEntries)
            {
                if (entry.iconGO == null) continue;
                bool talking = false;
                foreach (var line in _cachedLines)
                {
                    if (line == null) { _cachedLines = null; break; }
                    if (line.playerActorNumber == entry.actorNumber &&
                        line.speakerIcon != null && line.speakerIcon.enabled &&
                        line.speakerIcon.gameObject.activeSelf)
                    {
                        talking = true;
                        break;
                    }
                }
                entry.iconGO.SetActive(talking);
            }
        }

        // ========== GRID VIEW ==========

        GameObject _notConnectedLabel;

        public void Refresh()
        {
            if (_inDetail) return;

            foreach (var go in _items)
                if (go != null) Destroy(go);
            _items.Clear();
            _playerActorNumbers.Clear();
            _speakerEntries.Clear();
            _cachedLines = null;
            _lastPlayerHash = ComputePlayerHash();
            _nextForceRefresh = Time.time + ForceRefreshInterval;

            if (_container == null || _btnTemplate == null) return;

            // Show message when not in a lobby
            if (_notConnectedLabel != null) Destroy(_notConnectedLabel);
            if (!PhotonNetwork.InRoom)
            {
                var baseP = _btnTemplate.transform.localPosition;
                var canvasGO = new GameObject("NotConnectedCanvas");
                canvasGO.transform.SetParent(_container, false);
                canvasGO.transform.localPosition = baseP + new Vector3(-0.02f, 0.40f, -0.65f);
                canvasGO.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                var canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvasGO.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 80);
                canvasGO.transform.localScale = new Vector3(0.004f, 0.004f, 0.004f);
                var txt = canvasGO.AddComponent<Text>();
                txt.text = "YOU ARE NOT CONNECTED\nTO A ROOM";
                txt.fontSize = 22;
                txt.color = Color.yellow;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.fontStyle = FontStyle.Bold;
                if (CameraController.Instance != null && CameraController.Instance.FovText != null)
                    txt.font = CameraController.Instance.FovText.font;
                else
                    txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                _notConnectedLabel = canvasGO;
                return;
            }

            var players = GetPlayers();
            var basePos = _btnTemplate.transform.localPosition;

            const int cols = 3;
            const float spacingZ = 0.46f;
            const float spacingY = 0.26f;
            const float startY = 0.66f;
            const float startZ = -0.20f;
            const float stretchZ = 2.2f;
            const float squishY = 0.65f;

            Sprite speakerSprite = null;
            var sbLines = FindObjectsOfType<GorillaPlayerScoreboardLine>(true);
            foreach (var line in sbLines)
            {
                if (line.speakerIcon != null && line.speakerIcon.sprite != null)
                {
                    speakerSprite = line.speakerIcon.sprite;
                    break;
                }
            }

            for (int i = 0; i < players.Count && i < 9; i++)
            {
                var info = players[i];
                _playerActorNumbers.Add(info.ActorNumber);
                int col = i % cols;
                int row = i / cols;

                float posY = startY - row * spacingY;
                float posZ = startZ - col * spacingZ;

                var btn = Object.Instantiate(_btnTemplate, _container);
                btn.name = "RPPlayerBtn_" + i;
                btn.transform.localPosition = basePos + new Vector3(0f, posY, posZ);

                var origScale = btn.transform.localScale;
                btn.transform.localScale = new Vector3(origScale.x, origScale.y * squishY, origScale.z * stretchZ);

                var existingLabel = btn.GetComponentInChildren<Text>(true);
                if (existingLabel != null) Destroy(existingLabel.transform.parent.gameObject);

                if (!btn.GetComponent<YzGButton>())
                    btn.AddComponent<YzGButton>();

                float invZ = 1f / stretchZ;
                float invY = 1f / squishY;

                var swatchGO = GameObject.CreatePrimitive(PrimitiveType.Quad);
                swatchGO.name = "Swatch";
                swatchGO.layer = btn.layer;
                swatchGO.transform.SetParent(btn.transform, false);
                swatchGO.transform.localPosition = new Vector3(-0.52f, 0f, 0.80f * invZ);
                swatchGO.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                swatchGO.transform.localScale = new Vector3(0.40f * invZ, 0.45f * invY, 1f);
                Object.Destroy(swatchGO.GetComponent<Collider>());

                if (info.SwatchMaterial != null)
                {
                    var mat = new Material(info.SwatchMaterial);
                    mat.color = info.SwatchTint;
                    swatchGO.GetComponent<MeshRenderer>().material = mat;
                }
                else
                {
                    var unlitShader = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
                    var mat = new Material(unlitShader);
                    mat.color = info.BodyColor;
                    swatchGO.GetComponent<MeshRenderer>().material = mat;
                }

                if (speakerSprite != null)
                {
                    var iconGO = new GameObject("SpeakerIcon");
                    iconGO.layer = btn.layer;
                    iconGO.transform.SetParent(btn.transform, false);
                    iconGO.transform.localPosition = new Vector3(-0.53f, 0f, 0.80f * invZ);
                    iconGO.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                    iconGO.transform.localScale = new Vector3(0.06f * invZ, 0.07f * invY, 1f);
                    var sr = iconGO.AddComponent<SpriteRenderer>();
                    sr.sprite = speakerSprite;
                    sr.color = Color.white;
                    sr.sortingOrder = 2;
                    iconGO.SetActive(false);

                    _speakerEntries.Add(new SpeakerEntry
                    {
                        actorNumber = info.ActorNumber,
                        iconGO = iconGO
                    });
                }

                var canvasGO = new GameObject("LabelCanvas");
                canvasGO.transform.SetParent(btn.transform, false);
                canvasGO.transform.localPosition = new Vector3(-0.52f, 0f, -0.15f * invZ);
                canvasGO.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                canvasGO.transform.localScale = new Vector3(0.01f * invZ, 0.01f * invY, 0.01f);
                var canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                canvas.sortingOrder = 0;
                canvasGO.GetComponent<RectTransform>().sizeDelta = new Vector2(120f, 60f);

                var textGO = new GameObject("Label");
                textGO.transform.SetParent(canvasGO.transform, false);
                var txt = textGO.AddComponent<Text>();
                txt.text = info.DisplayName;
                txt.fontSize = 26;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.color = Color.white;
                txt.horizontalOverflow = HorizontalWrapMode.Overflow;
                txt.verticalOverflow = VerticalWrapMode.Overflow;
                if (CameraController.Instance?.FovText != null)
                    txt.font = CameraController.Instance.FovText.font;
                var textRT = textGO.GetComponent<RectTransform>();
                textRT.sizeDelta = new Vector2(120f, 60f);
                textRT.anchoredPosition = Vector2.zero;

                _items.Add(btn);
            }
        }

        // ========== DETAIL VIEW ==========

        public int GetActorNumberForIndex(int index)
        {
            if (index < 0 || index >= _playerActorNumbers.Count) return -1;
            return _playerActorNumbers[index];
        }

        public void ShowDetail(int actorNumber)
        {
            _inDetail = true;
            _detailActorNumber = actorNumber;
            _detailViewIndex = 0;
            _detailMuted = false;

            foreach (var go in _items)
                if (go != null) go.SetActive(false);

            if (_container == null || _btnTemplate == null) return;
            var basePos = _btnTemplate.transform.localPosition;

            Player targetPlayer = null;
            foreach (var p in PhotonNetwork.PlayerList)
            {
                if (p.ActorNumber == actorNumber) { targetPlayer = p; break; }
            }
            if (targetPlayer == null) { HideDetail(); return; }

            string playerName = string.IsNullOrEmpty(targetPlayer.NickName)
                ? "P" + actorNumber : targetPlayer.NickName;

            _detailRig = FindRigForActor(actorNumber);

            // Color from scoreboard swatch (proven to work)
            Color playerColor = Color.gray;
            var sbLine = FindScoreboardLine(actorNumber);
            if (sbLine != null && sbLine.playerSwatch != null)
                playerColor = sbLine.playerSwatch.color;
            else if (_detailRig != null)
                playerColor = _detailRig.playerColor;
            int cr = Mathf.Clamp(Mathf.RoundToInt(playerColor.r * 9f), 0, 9);
            int cg = Mathf.Clamp(Mathf.RoundToInt(playerColor.g * 9f), 0, 9);
            int cb = Mathf.Clamp(Mathf.RoundToInt(playerColor.b * 9f), 0, 9);

            // FPS via reflection
            int fps = GetRigFps(_detailRig);

            // Platform detection (mod count heuristic — BepInEx mods = Steam)
            string platform = DetectPlatform(actorNumber);

            // Check if we previously muted this player
            _detailMuted = _mutedActors.Contains(actorNumber);

            SetPageTitle(playerName);

            // --- Info rows (left side) ---
            string fpsStr = fps >= 0 ? "FPS: " + fps : "FPS: ?";
            MakeInfoButton("DetailFps", basePos + new Vector3(0f, 0.60f, -0.35f), fpsStr);
            _detailFpsText = _detailItems[_detailItems.Count - 1].GetComponentInChildren<Text>(true);

            MakeInfoButton("DetailPlatform", basePos + new Vector3(0f, 0.46f, -0.35f), "PLATFORM: " + platform);
            _detailPlatformText = _detailItems[_detailItems.Count - 1].GetComponentInChildren<Text>(true);

            string colorStr = "COLOR: " + cr + " " + cg + " " + cb;
            MakeInfoButton("DetailColor", basePos + new Vector3(0f, 0.32f, -0.35f), colorStr);
            _detailInfoText = _detailItems[_detailItems.Count - 1].GetComponentInChildren<Text>(true);

            // --- 3D Preview (right side) ---
            BuildDetailPreview(basePos + new Vector3(0f, 0.42f, -0.90f));

            _voiceFocusActive = _focusedActors.Contains(actorNumber);

            // --- Voice Focus + Mute + Report buttons (bottom) ---
            string[] btnNames = { "RPVoiceFocus", "RPMute", "RPHateSpeech", "RPToxicity", "RPCheating" };
            string[] btnLabels = {
                _voiceFocusActive ? "VOICE\nFOCUS:ON" : "VOICE\nFOCUS",
                _detailMuted ? "UNMUTE" : "MUTE",
                "HATE\nSPEECH", "TOXIC\nITY", "CHEAT\nING"
            };
            for (int i = 0; i < 5; i++)
            {
                var btn = Object.Instantiate(_btnTemplate, _container);
                btn.name = btnNames[i];
                btn.transform.localPosition = basePos + new Vector3(0f, 0.06f, -0.20f - i * 0.22f);
                btn.transform.localScale = _btnTemplate.transform.localScale * 0.65f;
                var existingLabel = btn.GetComponentInChildren<Text>(true);
                if (existingLabel != null) Destroy(existingLabel.transform.parent.gameObject);
                if (!btn.GetComponent<YzGButton>()) btn.AddComponent<YzGButton>();
                AddBtnLabel(btn, btnLabels[i]);
                _detailItems.Add(btn);
            }
        }

        static readonly FieldInfo _mutedField = typeof(VRRig).GetField("muted",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        static readonly HashSet<int> _mutedActors = new HashSet<int>();
        static readonly HashSet<int> _focusedActors = new HashSet<int>();
        const float FocusBoost = 1.5f;
        const float FocusDim = 0.15f;

        static readonly Dictionary<int, AudioSource> _voiceSourceCache = new Dictionary<int, AudioSource>();
        static float _nextVoiceCacheBuild;

        static void BuildVoiceCache()
        {
            if (Time.time < _nextVoiceCacheBuild) return;
            _nextVoiceCacheBuild = Time.time + 3f;
            _voiceSourceCache.Clear();

            // Voice audio lives on "HeadSpeaker" GameObjects with GTSpeaker component,
            // parented under the VRRig hierarchy (NOT under PhotonView).
            // Map HeadSpeaker → VRRig → playerNameVisible → actor number.
            var rigs = FindObjectsOfType<VRRig>(true);
            foreach (var rig in rigs)
            {
                if (rig.isOfflineVRRig || !rig.gameObject.activeInHierarchy) continue;
                string rigName = (rig.playerNameVisible ?? "").ToUpper().Trim();
                if (string.IsNullOrEmpty(rigName)) continue;

                int actor = -1;
                foreach (var p in PhotonNetwork.PlayerList)
                {
                    if ((p.NickName ?? "").ToUpper().Trim() == rigName) { actor = p.ActorNumber; break; }
                }
                if (actor < 0) continue;

                // Find the HeadSpeaker AudioSource in this rig's children
                var sources = rig.GetComponentsInChildren<AudioSource>(true);
                foreach (var src in sources)
                {
                    if (src.gameObject.name == "HeadSpeaker")
                    {
                        _voiceSourceCache[actor] = src;
                        break;
                    }
                }
            }
        }

        float _nextEnforceTime;

        void EnforceMutes()
        {
            bool hasMutes = _mutedActors.Count > 0;
            bool hasFocus = _focusedActors.Count > 0;
            if (!hasMutes && !hasFocus) return;

            if (Time.time < _nextEnforceTime) return;
            _nextEnforceTime = Time.time + 0.1f;

            BuildVoiceCache();

            foreach (var kvp in _voiceSourceCache)
            {
                int actor = kvp.Key;
                var src = kvp.Value;
                if (src == null) continue;

                if (_mutedActors.Contains(actor))
                    src.volume = 0f;
                else if (hasFocus && _focusedActors.Contains(actor))
                    src.volume = FocusBoost;
                else if (hasFocus)
                    src.volume = FocusDim;
                else
                    src.volume = 1f;
            }
        }

        public void ToggleMute()
        {
            _detailMuted = !_detailMuted;
            if (_detailMuted)
                _mutedActors.Add(_detailActorNumber);
            else
                _mutedActors.Remove(_detailActorNumber);
            UpdateMuteLabel();
        }

        public void ToggleVoiceFocus()
        {
            _voiceFocusActive = !_voiceFocusActive;
            if (_voiceFocusActive)
            {
                _focusedActors.Add(_detailActorNumber);
                _nextVoiceCacheBuild = 0f;
            }
            else
            {
                _focusedActors.Remove(_detailActorNumber);
                // Restore all non-muted players to normal volume
                foreach (var kvp in _voiceSourceCache)
                {
                    if (kvp.Value != null && !_mutedActors.Contains(kvp.Key))
                        kvp.Value.volume = 1f;
                }
            }
            foreach (var go in _detailItems)
            {
                if (go == null || go.name != "RPVoiceFocus") continue;
                var txt = go.GetComponentInChildren<Text>(true);
                if (txt != null) txt.text = _voiceFocusActive ? "VOICE\nFOCUS:ON" : "VOICE\nFOCUS";
            }
        }

        static bool _audioDumpDone;
        static void DumpAudioSources()
        {
            if (_audioDumpDone) return;
            _audioDumpDone = true;
            try
            {
                var path = Path.Combine(Application.dataPath, "..", "BepInEx", "plugins", "YizziAudioDump.txt");
                using (var sw = new StreamWriter(path, false))
                {
                    var allSrc = FindObjectsOfType<AudioSource>(true);
                    sw.WriteLine($"=== ALL AudioSources: {allSrc.Length} ===\n");
                    foreach (var src in allSrc)
                    {
                        var go = src.gameObject;
                        sw.Write($"  [{go.name}] active={go.activeInHierarchy} vol={src.volume} mute={src.mute}");
                        var pv = src.GetComponentInParent<PhotonView>();
                        if (pv != null)
                            sw.Write($" pv_owner={pv.OwnerActorNr} pv_id={pv.ViewID}");
                        sw.WriteLine();
                        var comps = go.GetComponents<MonoBehaviour>();
                        foreach (var c in comps)
                        {
                            if (c != null)
                                sw.WriteLine($"    comp: {c.GetType().FullName}");
                        }
                        // Also check parent for voice-related components
                        if (go.transform.parent != null)
                        {
                            sw.WriteLine($"    parent: {go.transform.parent.name}");
                            var parentComps = go.transform.parent.GetComponents<MonoBehaviour>();
                            foreach (var c in parentComps)
                            {
                                if (c != null && c.GetType().Name.ToLower().Contains("voice") || 
                                    c != null && c.GetType().Name.ToLower().Contains("speak"))
                                    sw.WriteLine($"    parent_comp: {c.GetType().FullName}");
                            }
                        }
                    }
                }
            }
            catch { }
        }

        void UpdateMuteLabel()
        {
            foreach (var go in _detailItems)
            {
                if (go == null || go.name != "RPMute") continue;
                var txt = go.GetComponentInChildren<Text>(true);
                if (txt != null) txt.text = _detailMuted ? "UNMUTE" : "MUTE";
            }
        }

        void SetPageTitle(string text)
        {
            var titleCanvas = _container.Find("PageTitleCanvas");
            if (titleCanvas == null) return;
            var txt = titleCanvas.GetComponentInChildren<Text>(true);
            if (txt != null) txt.text = text;
        }

        void MakeInfoButton(string name, Vector3 localPos, string text)
        {
            const float scaleY = 0.55f;
            const float scaleZ = 2.0f;
            float invY = 1f / scaleY;
            float invZ = 1f / scaleZ;

            var btn = Object.Instantiate(_btnTemplate, _container);
            btn.name = name;
            btn.transform.localPosition = localPos;
            var origScale = btn.transform.localScale;
            btn.transform.localScale = new Vector3(origScale.x, origScale.y * scaleY, origScale.z * scaleZ);
            var existingLabel = btn.GetComponentInChildren<Text>(true);
            if (existingLabel != null) Destroy(existingLabel.transform.parent.gameObject);
            var col = btn.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);

            var canvasGO = new GameObject("LabelCanvas");
            canvasGO.transform.SetParent(btn.transform, false);
            canvasGO.transform.localPosition = new Vector3(-0.52f, 0f, 0f);
            canvasGO.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            canvasGO.transform.localScale = new Vector3(0.01f * invZ, 0.01f * invY, 0.01f);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 0;
            canvasGO.GetComponent<RectTransform>().sizeDelta = new Vector2(200f, 60f);

            var textGO = new GameObject("Label");
            textGO.transform.SetParent(canvasGO.transform, false);
            var txt = textGO.AddComponent<Text>();
            txt.text = text;
            txt.fontSize = 22;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.color = Color.white;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            if (CameraController.Instance?.FovText != null)
                txt.font = CameraController.Instance.FovText.font;
            var rt = textGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200f, 60f);
            rt.anchoredPosition = Vector2.zero;

            _detailItems.Add(btn);
        }

        public void HideDetail()
        {
            _inDetail = false;
            _detailActorNumber = -1;
            _detailRig = null;
            _detailFpsText = null;
            _detailPlatformText = null;
            _detailInfoText = null;
            _detailMuted = false;

            CleanupDetailCamera();

            foreach (var go in _detailItems)
                if (go != null) Destroy(go);
            _detailItems.Clear();

            foreach (var go in _items)
                if (go != null) go.SetActive(true);

            SetPageTitle("REPORT");

            _nextForceRefresh = Time.time + 0.1f;
        }

        public void CycleDetailView()
        {
            _detailViewIndex = (_detailViewIndex + 1) % 4;
        }

        public bool IsInDetail => _inDetail;
        public int DetailActorNumber => _detailActorNumber;

        public GorillaPlayerScoreboardLine FindScoreboardLine(int actorNumber)
        {
            var lines = FindObjectsOfType<GorillaPlayerScoreboardLine>(true);
            foreach (var line in lines)
            {
                if (line.playerActorNumber == actorNumber)
                    return line;
            }
            return null;
        }

        void AddBtnLabel(GameObject btn, string label)
        {
            var canvasGO = new GameObject("LabelCanvas");
            canvasGO.transform.SetParent(btn.transform, false);
            canvasGO.transform.localPosition = new Vector3(-0.60f, -0.02f, 0f);
            canvasGO.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            canvasGO.transform.localScale = Vector3.one * 0.01f;
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 0;
            canvasGO.GetComponent<RectTransform>().sizeDelta = new Vector2(120f, 60f);

            var textGO = new GameObject("Label");
            textGO.transform.SetParent(canvasGO.transform, false);
            var txt = textGO.AddComponent<Text>();
            txt.text = label;
            txt.fontSize = 20;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            if (CameraController.Instance?.FovText != null)
                txt.font = CameraController.Instance.FovText.font;
            var rt = textGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(120f, 60f);
            rt.anchoredPosition = Vector2.zero;
        }

        void BuildDetailPreview(Vector3 localPos)
        {
            CleanupDetailCamera();

            const float w = 0.28f;
            const float h = 0.42f;

            var previewRoot = new GameObject("DetailPreviewRoot");
            previewRoot.transform.SetParent(_container, false);
            previewRoot.transform.localPosition = localPos;
            previewRoot.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "RPPreviewBtn";
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

            _detailRT = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 1,
                name = "YizziDetailPreviewRT"
            };
            _detailRT.Create();

            var sh = Shader.Find("Unlit/Texture")
                     ?? Shader.Find("Universal Render Pipeline/Unlit")
                     ?? Shader.Find("Sprites/Default");
            var mat = new Material(sh) { mainTexture = _detailRT };
            quad.GetComponent<MeshRenderer>().material = mat;

            var camGo = new GameObject("YizziDetailPreviewCam");
            camGo.transform.SetParent(null, false);
            _detailCam = camGo.AddComponent<Camera>();
            _detailCam.targetTexture = _detailRT;
            _detailCam.clearFlags = CameraClearFlags.SolidColor;
            _detailCam.backgroundColor = Color.black;
            _detailCam.depth = -80;
            _detailCam.nearClipPlane = 0.05f;
            _detailCam.farClipPlane = 50f;
            _detailCam.fieldOfView = 35f;
            _detailCam.cullingMask = 0;
            _detailCam.enabled = false;

            _detailItems.Add(previewRoot);
        }

        void CleanupDetailCamera()
        {
            if (_detailCam != null && _detailCam.gameObject != null)
                Destroy(_detailCam.gameObject);
            _detailCam = null;
            if (_detailRT != null)
            {
                _detailRT.Release();
                Destroy(_detailRT);
            }
            _detailRT = null;
        }

        const int PreviewLayer = 31;

        void RenderDetailPreview()
        {
            if (_detailCam == null || _detailRig == null) return;

            if (_detailCam.cullingMask == 0)
            {
                _detailCam.cullingMask = 1 << PreviewLayer;
                Camera srcCam = null;
                if (GorillaTagger.Instance != null && GorillaTagger.Instance.mainCamera != null)
                    srcCam = GorillaTagger.Instance.mainCamera.GetComponent<Camera>();
                if (srcCam == null) srcCam = Camera.main;
                if (srcCam != null)
                {
                    var urpData = srcCam.GetComponent("UniversalAdditionalCameraData");
                    if (urpData != null)
                    {
                        var myUrp = _detailCam.GetComponent("UniversalAdditionalCameraData");
                        if (myUrp == null)
                            _detailCam.gameObject.AddComponent(urpData.GetType());
                    }
                }
            }

            UpdateDetailCameraPose();
            _detailCam.enabled = false;

            // Swap rig renderers to an isolated layer so only the rig renders
            var renderers = _detailRig.GetComponentsInChildren<Renderer>(true);
            var origLayers = new int[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                origLayers[i] = renderers[i].gameObject.layer;
                renderers[i].gameObject.layer = PreviewLayer;
            }

            _detailCam.Render();

            for (int i = 0; i < renderers.Length; i++)
                renderers[i].gameObject.layer = origLayers[i];
        }

        void UpdateDetailCameraPose()
        {
            if (_detailRig == null || _detailCam == null) return;
            var body = _detailRig.transform;
            var f = body.forward;
            f.y = 0f;
            if (f.sqrMagnitude < 1e-4f) f = Vector3.forward;
            f.Normalize();
            var right = Vector3.Cross(Vector3.up, f).normalized;
            var origin = body.position + Vector3.up * -0.05f;
            const float dist = 1.6f;
            Vector3 camPos;
            switch (_detailViewIndex)
            {
                case 0: camPos = origin + f * dist; break;
                case 1: camPos = origin + right * dist; break;
                case 2: camPos = origin - f * dist; break;
                default: camPos = origin - right * dist; break;
            }
            _detailCam.transform.position = camPos;
            _detailCam.transform.LookAt(origin, Vector3.up);
        }

        void UpdateDetailInfo()
        {
            if (Time.time < _nextDetailFpsUpdate) return;
            _nextDetailFpsUpdate = Time.time + 0.5f;

            // Update FPS and platform
            if (_detailRig != null)
            {
                int fps = GetRigFps(_detailRig);
                if (_detailFpsText != null)
                    _detailFpsText.text = fps >= 0 ? "FPS: " + fps : "FPS: ?";
                if (_detailPlatformText != null)
                    _detailPlatformText.text = "PLATFORM: " + DetectPlatform(_detailActorNumber);
            }

            // Update color from scoreboard (changes when tagged)
            if (_detailInfoText != null)
            {
                Color playerColor = Color.gray;
                var line = FindScoreboardLine(_detailActorNumber);
                if (line != null && line.playerSwatch != null)
                    playerColor = line.playerSwatch.color;
                else if (_detailRig != null)
                    playerColor = _detailRig.playerColor;

                int cr = Mathf.Clamp(Mathf.RoundToInt(playerColor.r * 9f), 0, 9);
                int cg = Mathf.Clamp(Mathf.RoundToInt(playerColor.g * 9f), 0, 9);
                int cb = Mathf.Clamp(Mathf.RoundToInt(playerColor.b * 9f), 0, 9);
                _detailInfoText.text = "COLOR: " + cr + " " + cg + " " + cb;
            }
        }

        static readonly FieldInfo _fpsField = typeof(VRRig).GetField("fps",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        static int GetRigFps(VRRig rig)
        {
            if (rig == null || _fpsField == null) return -1;
            try { return (int)_fpsField.GetValue(rig); }
            catch { return -1; }
        }

        static string DetectPlatform(int actorNumber)
        {
            // GT doesn't expose platform data and mod-based detection is unreliable
            return "?";
        }

        static VRRig FindRigForActor(int actorNumber)
        {
            // VRRigs in GT don't have PhotonView — match via playerNameVisible
            Player target = null;
            foreach (var p in PhotonNetwork.PlayerList)
            {
                if (p.ActorNumber == actorNumber) { target = p; break; }
            }
            if (target == null) return null;
            string targetName = (target.NickName ?? "").ToUpper().Trim();
            if (string.IsNullOrEmpty(targetName)) return null;

            VRRig inactiveFallback = null;
            var rigs = FindObjectsOfType<VRRig>(true);
            foreach (var rig in rigs)
            {
                if (rig.isOfflineVRRig) continue;
                string rigName = (rig.playerNameVisible ?? "").ToUpper().Trim();
                if (rigName != targetName) continue;
                if (rig.gameObject.activeInHierarchy)
                    return rig;
                if (inactiveFallback == null)
                    inactiveFallback = rig;
            }
            return inactiveFallback;
        }

        static bool _dumpDone;
        static void DumpDiagnostics(VRRig rig, Player player)
        {
            if (_dumpDone) return;
            _dumpDone = true;
            try
            {
                var path = Path.Combine(Application.dataPath, "..", "BepInEx", "plugins", "YizziRigDump.txt");
                using (var sw = new StreamWriter(path, false))
                {
                    sw.WriteLine($"=== Target actor = {player?.ActorNumber}, rig = {(rig != null ? rig.name : "NULL")} ===\n");

                    // Enumerate ALL VRRigs in scene
                    var allRigs = FindObjectsOfType<VRRig>(true);
                    sw.WriteLine($"=== ALL VRRigs in scene: {allRigs.Length} ===");
                    foreach (var r in allRigs)
                    {
                        var pv = r.GetComponent<PhotonView>();
                        var pvParent = r.GetComponentInParent<PhotonView>();
                        var pvChild = r.GetComponentInChildren<PhotonView>(true);
                        sw.WriteLine($"  name={r.name} active={r.gameObject.activeInHierarchy} layer={r.gameObject.layer}");
                        sw.WriteLine($"    PhotonView(self)={FormatPV(pv)}");
                        sw.WriteLine($"    PhotonView(parent)={FormatPV(pvParent)}");
                        sw.WriteLine($"    PhotonView(child)={FormatPV(pvChild)}");
                        sw.WriteLine($"    position={r.transform.position}");
                        sw.WriteLine($"    renderers={r.GetComponentsInChildren<Renderer>(true).Length}");

                        // Dump key fields
                        var fields = typeof(VRRig).GetFields(
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        int count = 0;
                        foreach (var f in fields)
                        {
                            string n = f.Name.ToLower();
                            bool interesting = n.Contains("player") || n.Contains("owner") ||
                                n.Contains("actor") || n.Contains("creator") || n.Contains("net") ||
                                n.Contains("fps") || n.Contains("frame") || n.Contains("platform") ||
                                n.Contains("quest") || n.Contains("steam") || n.Contains("local") ||
                                n.Contains("offline") || n.Contains("color") || n.Contains("mute") ||
                                f.FieldType == typeof(Player) || f.FieldType == typeof(PhotonView);
                            if (interesting)
                            {
                                string val = "?";
                                try { val = (f.GetValue(r) ?? "null").ToString(); }
                                catch { val = "ERROR"; }
                                sw.WriteLine($"    [{f.FieldType.Name}] {f.Name} = {val}");
                                count++;
                            }
                        }
                        if (count == 0)
                        {
                            // Just dump ALL field names + types
                            sw.WriteLine("    --- ALL FIELDS ---");
                            foreach (var f in fields)
                            {
                                string val = "?";
                                try { val = (f.GetValue(r) ?? "null").ToString(); }
                                catch { val = "ERR"; }
                                sw.WriteLine($"    [{f.FieldType.Name}] {f.Name} = {val}");
                            }
                        }
                        sw.WriteLine();
                    }

                    // Enumerate ALL PhotonViews in scene
                    var allPVs = FindObjectsOfType<PhotonView>(true);
                    sw.WriteLine($"\n=== ALL PhotonViews: {allPVs.Length} ===");
                    foreach (var pv in allPVs)
                    {
                        int ownerActor = -1;
                        try { ownerActor = pv.OwnerActorNr; } catch { }
                        sw.WriteLine($"  {pv.gameObject.name} owner={ownerActor} viewID={pv.ViewID}");
                    }

                    sw.WriteLine($"\n=== Player CustomProperties (actor {player?.ActorNumber}) ===");
                    if (player?.CustomProperties != null)
                    {
                        foreach (var key in player.CustomProperties.Keys)
                        {
                            var val = player.CustomProperties[key];
                            sw.WriteLine($"  [{key}] ({val?.GetType().Name}) = {val}");
                        }
                    }

                    sw.WriteLine("\n=== Camera Info ===");
                    var mainCam = Camera.main;
                    sw.WriteLine($"  Camera.main = {(mainCam != null ? mainCam.name : "null")}");
                    if (mainCam != null)
                        sw.WriteLine($"  cullingMask = {mainCam.cullingMask}");
                }
            }
            catch { }
        }

        static string FormatPV(PhotonView pv)
        {
            if (pv == null) return "null";
            try { return $"owner={pv.OwnerActorNr} viewID={pv.ViewID}"; }
            catch { return "ERROR"; }
        }

        // ========== SHARED HELPERS ==========

        struct PlayerInfo
        {
            public string DisplayName;
            public Color BodyColor;
            public Material SwatchMaterial;
            public Color SwatchTint;
            public int ActorNumber;
        }

        static int ComputePlayerHash()
        {
            if (!PhotonNetwork.InRoom) return 0;
            int hash = 17;
            foreach (var p in PhotonNetwork.PlayerList)
            {
                if (p.IsLocal) continue;
                hash = hash * 31 + p.ActorNumber;
            }
            var lines = FindObjectsOfType<GorillaPlayerScoreboardLine>(true);
            foreach (var line in lines)
            {
                if (line.playerActorNumber <= 0) continue;
                if (line.playerSwatch != null)
                {
                    var c = line.playerSwatch.color;
                    hash = hash * 31 + ((int)(c.r * 255) << 16 | (int)(c.g * 255) << 8 | (int)(c.b * 255));
                }
            }
            return hash;
        }

        static List<PlayerInfo> GetPlayers()
        {
            var result = new List<PlayerInfo>();
            if (!PhotonNetwork.InRoom) return result;

            var swatchMap = BuildSwatchMap();
            var colorMap = BuildRigColorMap();

            foreach (var p in PhotonNetwork.PlayerList)
            {
                if (p.IsLocal) continue;

                var name = string.IsNullOrEmpty(p.NickName) ? "P" + p.ActorNumber : p.NickName;
                if (name.Length > 9) name = name.Substring(0, 8) + ">";

                Material swatch = null;
                Color swatchTint = Color.white;
                if (swatchMap.TryGetValue(p.ActorNumber, out var entry))
                {
                    swatch = entry.mat;
                    swatchTint = entry.tint;
                }

                Color color = Color.gray;
                if (colorMap.TryGetValue(p.ActorNumber, out var c))
                    color = c;

                result.Add(new PlayerInfo
                {
                    DisplayName = name,
                    BodyColor = color,
                    SwatchMaterial = swatch,
                    SwatchTint = swatchTint,
                    ActorNumber = p.ActorNumber
                });
            }
            return result;
        }

        struct SwatchEntry
        {
            public Material mat;
            public Color tint;
        }

        static Dictionary<int, SwatchEntry> BuildSwatchMap()
        {
            var map = new Dictionary<int, SwatchEntry>();
            var lines = FindObjectsOfType<GorillaPlayerScoreboardLine>(true);

            foreach (var line in lines)
            {
                if (line.playerActorNumber <= 0) continue;
                if (map.ContainsKey(line.playerActorNumber)) continue;

                if (line.playerSwatch != null)
                {
                    var entry = new SwatchEntry
                    {
                        mat = line.playerSwatch.material != null
                            ? new Material(line.playerSwatch.material)
                            : null,
                        tint = line.playerSwatch.color
                    };
                    if (entry.mat != null)
                        map[line.playerActorNumber] = entry;
                }
            }
            return map;
        }

        static Dictionary<int, Color> BuildRigColorMap()
        {
            var map = new Dictionary<int, Color>();
            var rigs = FindObjectsOfType<VRRig>(true);

            foreach (var rig in rigs)
            {
                var pv = rig.GetComponent<PhotonView>();
                if (pv == null) continue;
                int owner = pv.OwnerActorNr;
                if (owner <= 0 || map.ContainsKey(owner)) continue;

                var c = rig.playerColor;
                if (c.r > 0.02f || c.g > 0.02f || c.b > 0.02f)
                    map[owner] = c;
            }
            return map;
        }
    }
}
