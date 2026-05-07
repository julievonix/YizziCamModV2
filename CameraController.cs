using System;
using System.Collections;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Photon.Pun;
using GorillaNetworking;
using BepInEx;
using Unity.Cinemachine;
using GorillaLocomotion;
using Player = GorillaLocomotion.GTPlayer;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using YizziCamModV2.Comps;
#pragma warning disable CS0618
namespace YizziCamModV2
{
    public class CameraController : MonoBehaviour
    {
        public static CameraController Instance;
        public GameObject CameraTablet;
        public GameObject FirstPersonCameraGO;
        public GameObject ThirdPersonCameraGO;
        public GameObject CMVirtualCameraGO;
        public GameObject LeftHandGO;
        public GameObject RightHandGO;
        public GameObject TabletCameraGO;
        public GameObject MainPage;
        public GameObject MiscPage;
        public GameObject ExtraPage;
        public GameObject MainPinnedShortcutButton;
        /// <summary>Main-page slot that shows PIN (empty) or the pinned Extra feature label; opens that feature or Extra when empty.</summary>
        public GameObject MainPinSlotButton;
        public bool MiscReturnToExtraInsteadOfMain;
        public GameObject WardrobePage;
        public GameObject WeatherTimePage;
        public Text WTRainStatusText;
        public Text WTTimeStatusText;
        public GameObject CameraClipPage;
        public Text ClipLagValueText;
        public Text ClipLagStatusText;
        public GameObject GeneralPage;
        public GameObject ReportPage;
        public GameObject MusicPage;
        public GameObject PinSelectorPage;
        public GameObject ExtraPageUnpinButton;
        Text MusicSongNameText;
        Text MusicTimeText;
        Text MusicClockText;
        string _mediaSongLine    = "♪  —";
        string _mediaArtistLine  = "";
        double _mediaElapsed     = 0;
        double _mediaEndTime     = 0;
        bool   _mediaPaused      = true;
        DateTime _mediaFetchTime = DateTime.MinValue;
        float  _lastMediaAutoRefresh = -999f;
        volatile bool _mediaBusy;
        volatile bool _mediaRefreshed;
        public Text GenWatermarkText;
        public Text GenRawRotText;
        public Text GenSummonText;
        public Text GenCamDisText;
        public GameObject LeftGrabCol;
        public GameObject RightGrabCol;
        public GameObject CameraFollower;
        public GameObject TPVBodyFollower;
        public GameObject ColorScreenGO;
        public GameObject FakeCameraGO;
        public List<GameObject> Buttons = new List<GameObject>();
        public List<GameObject> ColorButtons = new List<GameObject>();
        public List<Material> ScreenMats = new List<Material>();
        public List<MeshRenderer> meshRenderers = new List<MeshRenderer>();

        const string ExtraPinPrefKey = "YizziExtraPin";

        /// <summary>Gorilla-style UI tint (requested <c>rgb(251,254,13)</c> ≈ <c>hsl(61,83%,47%)</c>).</summary>
        static readonly Color TabletLabelYellow = new Color(251f / 255f, 254f / 255f, 13f / 255f);

        /// <summary>Extra / main tablet mesh button labels.</summary>
        const int TabletWorldButtonFontSize = 27;

        const int TabletPageTitleFontSize = 29;

        public Camera TabletCamera;
        public Camera FirstPersonCamera;
        public Camera ThirdPersonCamera;
        public CinemachineVirtualCamera CMVirtualCamera;

        public Text FovText;
        public Text NearClipText;
        public Text ColorScreenText;
        public Text MinDistText;
        public Text SpeedText;
        public Text SmoothText;
        public Text TPText;
        public Text TPRotText;

        public bool followheadrot = true;
        public bool canbeused;
        public bool flipped;
        public bool tpv;
        public bool fpv;
        public bool fp;
        public bool camDisconnect;
        public bool fpvRawRotation = false;
        public bool fpvClipping = false;
        public float fpvClipLag = 0.5f;
        public bool openedurl;
        public float minDist = 2f;
        float dist;
        public float fpspeed = 0.01f;
        Vector3 tabletCamDefaultLocalPos;
        Quaternion tabletCamDefaultLocalRot;
        Vector3 tpCamDefaultLocalPos;
        Quaternion tpCamDefaultLocalRot;
        public float smoothing = 0.05f;
        Vector3 targetPosition;
        Vector3 velocity = Vector3.zero;
        public TPVModes TPVMode = TPVModes.BACK;
        bool init;
        bool lobbyHopBusy;
        void Awake()
        {
            Instance = this;
        }

        public void YizziStart()
        {
            this.gameObject.AddComponent<InputManager>().gameObject.AddComponent<UI>();
            ColorScreenGO = LoadBundle("ColorScreen", "YizziCamModV2.Assets.colorscreen");
            CameraTablet = LoadBundle("CameraTablet", "YizziCamModV2.Assets.yizzicam");
            FirstPersonCameraGO = GorillaTagger.Instance.mainCamera;
            ThirdPersonCameraGO = GameObject.Find("Player Objects/Third Person Camera/Shoulder Camera");
            CMVirtualCameraGO = GameObject.Find("Player Objects/Third Person Camera/Shoulder Camera/CM vcam1");
            TPVBodyFollower = GorillaTagger.Instance.bodyCollider.gameObject;
            CMVirtualCamera = CMVirtualCameraGO.GetComponent<CinemachineVirtualCamera>();
            FirstPersonCamera = FirstPersonCameraGO.GetComponent<Camera>();
            ThirdPersonCamera = ThirdPersonCameraGO.GetComponent<Camera>();
            LeftHandGO = GorillaTagger.Instance.leftHandTransform.gameObject;
            RightHandGO = GorillaTagger.Instance.rightHandTransform.gameObject;
            CameraTablet.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            CameraFollower = GameObject.Find("Player Objects/Player VR Controller/GorillaPlayer/TurnParent/Main Camera/Camera Follower");
            TabletCameraGO = GameObject.Find("CameraTablet(Clone)/Camera");
            TabletCamera = TabletCameraGO.GetComponent<Camera>();
            tabletCamDefaultLocalPos = TabletCameraGO.transform.localPosition;
            tabletCamDefaultLocalRot = TabletCameraGO.transform.localRotation;
            FakeCameraGO = GameObject.Find("CameraTablet(Clone)/FakeCamera");
            FakeCameraGO.transform.localPosition = new Vector3(0f, 0.55f, 0.1f);
            LeftGrabCol = GameObject.Find("CameraTablet(Clone)/LeftGrabCol");
            RightGrabCol = GameObject.Find("CameraTablet(Clone)/RightGrabCol");
            LeftGrabCol.AddComponent<LeftGrabTrigger>();
            RightGrabCol.AddComponent<RightGrabTrigger>();
            MainPage = GameObject.Find("CameraTablet(Clone)/MainPage");
            MiscPage = GameObject.Find("CameraTablet(Clone)/MiscPage");
            FovText = GameObject.Find("CameraTablet(Clone)/MainPage/Canvas/FovValueText").GetComponent<Text>();
            SmoothText = GameObject.Find("CameraTablet(Clone)/MainPage/Canvas/SmoothingValueText").GetComponent<Text>();
            NearClipText = GameObject.Find("CameraTablet(Clone)/MainPage/Canvas/NearClipValueText").GetComponent<Text>();
            MinDistText = GameObject.Find("CameraTablet(Clone)/MiscPage/Canvas/MinDistValueText").GetComponent<Text>();
            SpeedText = GameObject.Find("CameraTablet(Clone)/MiscPage/Canvas/SpeedValueText").GetComponent<Text>();
            TPText = GameObject.Find("CameraTablet(Clone)/MiscPage/Canvas/TPText").GetComponent<Text>();
            TPRotText = GameObject.Find("CameraTablet(Clone)/MiscPage/Canvas/TPRotText").GetComponent<Text>();
            Buttons.Add(GameObject.Find("CameraTablet(Clone)/MainPage/MiscButton"));
            Buttons.Add(GameObject.Find("CameraTablet(Clone)/MainPage/FPVButton"));
            Buttons.Add(GameObject.Find("CameraTablet(Clone)/MainPage/FovUP"));
            Buttons.Add(GameObject.Find("CameraTablet(Clone)/MainPage/FovDown"));
            Buttons.Add(GameObject.Find("CameraTablet(Clone)/MainPage/FlipCamButton"));
            Buttons.Add(GameObject.Find("CameraTablet(Clone)/MainPage/NearClipUp"));
            Buttons.Add(GameObject.Find("CameraTablet(Clone)/MainPage/NearClipDown"));
            Buttons.Add(GameObject.Find("CameraTablet(Clone)/MainPage/FPButton"));
            Buttons.Add(GameObject.Find("CameraTablet(Clone)/MainPage/ControlsButton"));
            Buttons.Add(GameObject.Find("CameraTablet(Clone)/MainPage/TPVButton"));
            Buttons.Add(GameObject.Find("CameraTablet(Clone)/MainPage/SmoothingDownButton"));
            Buttons.Add(GameObject.Find("CameraTablet(Clone)/MainPage/SmoothingUpButton"));
            Buttons.Add(GameObject.Find("CameraTablet(Clone)/MiscPage/BackButton"));
            Buttons.Add(GameObject.Find("CameraTablet(Clone)/MiscPage/GreenScreenButton"));
            Buttons.Add(GameObject.Find("CameraTablet(Clone)/MiscPage/MinDistDownButton"));
            Buttons.Add(GameObject.Find("CameraTablet(Clone)/MiscPage/MinDistUpButton"));
            Buttons.Add(GameObject.Find("CameraTablet(Clone)/MiscPage/SpeedDownButton"));
            Buttons.Add(GameObject.Find("CameraTablet(Clone)/MiscPage/SpeedUpButton"));
            Buttons.Add(GameObject.Find("CameraTablet(Clone)/MiscPage/TPModeDownButton"));
            Buttons.Add(GameObject.Find("CameraTablet(Clone)/MiscPage/TPModeUpButton"));
            Buttons.Add(GameObject.Find("CameraTablet(Clone)/MiscPage/TPRotButton"));
            Buttons.Add(GameObject.Find("CameraTablet(Clone)/MiscPage/TPRotButton1"));

            var miscSlotGo = GameObject.Find("CameraTablet(Clone)/MainPage/MiscButton");
            var fpSlotGo = GameObject.Find("CameraTablet(Clone)/MainPage/FPButton");
            Vector3 miscSlotPrefabLocal =
                miscSlotGo != null ? miscSlotGo.transform.localPosition : Vector3.zero;

            ExtraPage = CreateExtraPage();
            ExtraPage.SetActive(false);

            if (miscSlotGo != null)
            {
                miscSlotGo.name = "PinButton";
                MainPinSlotButton = miscSlotGo;
            }

            ApplyHomePagePinExtraFollowLayout(miscSlotPrefabLocal);

            if (fpSlotGo != null)
                SetOrCreateButtonLabel(fpSlotGo, "FOLLOW\nPLAYER", sizeOverride: 20);

            var fpvBtn = GameObject.Find("CameraTablet(Clone)/MainPage/FPVButton");
            if (fpvBtn != null)
                SetOrCreateButtonLabel(fpvBtn, "FIRST\nPERSON", sizeOverride: 20);
            var greenCoverTpl = GameObject.Find("CameraTablet(Clone)/MiscPage/GreenScreenButton");
            AddMeshLabelCoverFromMiscGreen(fpSlotGo, greenCoverTpl, MeshLabelCoverFollowPlayerSlot);
            AddMeshLabelCoverFromMiscGreen(MainPinSlotButton, greenCoverTpl, MeshLabelCoverPinMiscSlot);
            AddMeshLabelCoverFromMiscGreen(MainPinnedShortcutButton, greenCoverTpl, MeshLabelCoverExtraVsFollowBleedSlot);
            StripMiscLettersFromPrefabTextOnHosts(fpSlotGo, MainPinSlotButton, MainPinnedShortcutButton);
            StripMainPageBakedLabels();
            // Labels must be applied AFTER stripping so the new text isn't disabled by the strip pass.
            RefreshPinnedShortcutLabel();

            // Bundled tablet prefab may still contain a banana mesh; code-only removal does not edit the asset bundle.
            RemoveBundledBananaVisualFromTablet();

            foreach (GameObject btns in Buttons)
            {
                btns.AddComponent<YzGButton>();
            }
            CMVirtualCamera.enabled = false;
            ThirdPersonCameraGO.transform.SetParent(CameraTablet.transform, true);
            CameraTablet.transform.position = new Vector3(-65, 12, -82);
            ThirdPersonCameraGO.transform.position = TabletCamera.transform.position;
            ThirdPersonCameraGO.transform.rotation = TabletCamera.transform.rotation;
            tpCamDefaultLocalPos = ThirdPersonCameraGO.transform.localPosition;
            tpCamDefaultLocalRot = ThirdPersonCameraGO.transform.localRotation;
            CameraTablet.transform.Rotate(0, 180, 0);
            ColorScreenText = GameObject.Find("CameraTablet(Clone)/MiscPage/Canvas/ColorScreenText").GetComponent<Text>();
            ColorButtons.Add(GameObject.Find("ColorScreen(Clone)/Stuff/RedButton"));
            ColorButtons.Add(GameObject.Find("ColorScreen(Clone)/Stuff/GreenButton"));
            ColorButtons.Add(GameObject.Find("ColorScreen(Clone)/Stuff/BlueButton"));
            foreach (GameObject btns in ColorButtons)
            {
                btns.AddComponent<YzGButton>();
            }
            ScreenMats.Add(GameObject.Find("ColorScreen(Clone)/Screen1").GetComponent<MeshRenderer>().material);
            ScreenMats.Add(GameObject.Find("ColorScreen(Clone)/Screen2").GetComponent<MeshRenderer>().material);
            ScreenMats.Add(GameObject.Find("ColorScreen(Clone)/Screen3").GetComponent<MeshRenderer>().material);
            meshRenderers.Add(GameObject.Find("CameraTablet(Clone)/Tablet").GetComponent<MeshRenderer>());
            meshRenderers.Add(GameObject.Find("CameraTablet(Clone)/Handle").GetComponent<MeshRenderer>());
            meshRenderers.Add(GameObject.Find("CameraTablet(Clone)/Handle2").GetComponent<MeshRenderer>());
            ColorScreenGO.transform.position = new Vector3(-54.3f, 16.21f, -122.96f);
            ColorScreenGO.transform.Rotate(0, 30, 0);
            ColorScreenGO.SetActive(false);
            MiscPage.SetActive(false);
            ExtraPage.SetActive(false);
            ThirdPersonCamera.nearClipPlane = 0.1f;
            TabletCamera.nearClipPlane = 0.1f;
            ReplaceAtlasTexture();
            camDisconnect = PlayerPrefs.GetInt("YizziCamDis", 0) == 1;
            fpv = true;
            foreach (MeshRenderer mr in meshRenderers)
            {
                mr.enabled = false;
            }
            MainPage.SetActive(false);
            init = true;
        }

        public enum TPVModes
        {
            BACK,
            FRONT
        }

        void LateUpdate()
        {
            if (init)
            {
                // ── Music page: auto-refresh + live countdown + system clock ──────────
                if (MusicPage != null && MusicPage.activeSelf)
                {
                    if (!_mediaBusy && Time.time - _lastMediaAutoRefresh > 3f)
                    {
                        _lastMediaAutoRefresh = Time.time;
                        RefreshMediaInfo();
                    }
                    if (_mediaRefreshed)
                    {
                        _mediaRefreshed = false;
                        if (MusicSongNameText != null) MusicSongNameText.text = _mediaSongLine;
                    }
                    // Live countdown: subtract time elapsed since last fetch (only if playing)
                    if (MusicTimeText != null && _mediaFetchTime != DateTime.MinValue)
                    {
                        double addSec   = _mediaPaused ? 0 : (DateTime.UtcNow - _mediaFetchTime).TotalSeconds;
                        double remaining = Math.Max(0, _mediaEndTime - (_mediaElapsed + addSec));
                        int rm = (int)remaining / 60; int rs = (int)remaining % 60;
                        string status = _mediaPaused ? "▐▐" : "▶";
                        MusicTimeText.text = $"{_mediaArtistLine}  {status}  -{rm}:{rs:D2}";
                    }
                    // System clock top-right
                    if (MusicClockText != null)
                        MusicClockText.text = DateTime.Now.ToString("h:mm tt");
                }

                if (fpv)
                {
                    if (camDisconnect)
                    {
                        if (fpvClipping)
                        {
                            TabletCameraGO.transform.position = Vector3.Lerp(TabletCameraGO.transform.position, CameraFollower.transform.position, fpvClipLag);
                            ThirdPersonCameraGO.transform.position = Vector3.Lerp(ThirdPersonCameraGO.transform.position, CameraFollower.transform.position, fpvClipLag);
                        }
                        else
                        {
                            TabletCameraGO.transform.position = CameraFollower.transform.position;
                            ThirdPersonCameraGO.transform.position = CameraFollower.transform.position;
                        }
                        if (fpvRawRotation)
                        {
                            TabletCameraGO.transform.rotation = CameraFollower.transform.rotation;
                            ThirdPersonCameraGO.transform.rotation = CameraFollower.transform.rotation;
                        }
                        else
                        {
                            TabletCameraGO.transform.rotation = Quaternion.Lerp(TabletCameraGO.transform.rotation, CameraFollower.transform.rotation, smoothing);
                            ThirdPersonCameraGO.transform.rotation = Quaternion.Lerp(ThirdPersonCameraGO.transform.rotation, CameraFollower.transform.rotation, smoothing);
                        }
                    }
                    else
                    {
                        if (MainPage.activeSelf)
                        {
                            foreach (MeshRenderer mr in meshRenderers)
                            {
                                mr.enabled = false;
                            }
                            MainPage.SetActive(false);
                        }
                        if (FakeCameraGO.activeSelf) FakeCameraGO.SetActive(false);
                        if (fpvClipping)
                            CameraTablet.transform.position = Vector3.Lerp(CameraTablet.transform.position, CameraFollower.transform.position, fpvClipLag);
                        else
                            CameraTablet.transform.position = CameraFollower.transform.position;
                        if (fpvRawRotation)
                            CameraTablet.transform.rotation = CameraFollower.transform.rotation;
                        else
                            CameraTablet.transform.rotation = Quaternion.Lerp(CameraTablet.transform.rotation, CameraFollower.transform.rotation, smoothing);
                    }
                }
                if (InputManager.instance.TeleportCamera && CameraTablet.transform.parent == null)
                {
                    fp = false;
                    tpv = false;

                    if (camDisconnect)
                    {
                        fpv = true;
                        ResetTabletCamera();
                        if (!FakeCameraGO.activeSelf) FakeCameraGO.SetActive(true);
                        SwitchToMainPage();
                    }
                    else
                    {
                        fpv = false;
                        ResetTabletCamera();
                        if (!FakeCameraGO.activeSelf) FakeCameraGO.SetActive(true);
                        SwitchToMainPage();
                    }

                    // Place in front of the player, facing them
                    var head = Player.Instance.headCollider.transform;
                    CameraTablet.transform.position = head.position + head.forward;
                    CameraTablet.transform.LookAt(head.position);
                    CameraTablet.transform.Rotate(0f, 180f, 0f);
                }
                if (fp)
                {
                    CameraTablet.transform.LookAt(2f * CameraTablet.transform.position - CameraFollower.transform.position);
                    if (!flipped)
                    {
                        flipped = true;
                        ThirdPersonCameraGO.transform.Rotate(0.0f, 180f, 0.0f);
                        TabletCameraGO.transform.Rotate(0.0f, 180f, 0.0f);
                    }
                    dist = Vector3.Distance(CameraFollower.transform.position, CameraTablet.transform.position);
                    if (dist > minDist)
                    {
                        CameraTablet.transform.position = Vector3.Lerp(CameraTablet.transform.position, CameraFollower.transform.position, fpspeed);
                    }
                }
                if (tpv)
                {
                    if (MainPage.activeSelf)
                    {
                        foreach (MeshRenderer mr in meshRenderers)
                        {
                            mr.enabled = false;
                        }
                        MainPage.SetActive(false);
                    }
                    if (TPVMode == TPVModes.BACK)
                    {
                        if (followheadrot)
                        {
                            targetPosition = CameraFollower.transform.TransformPoint(new Vector3(0.3f, 0.1f, -1.5f));
                        }
                        else
                        {
                            targetPosition = TPVBodyFollower.transform.TransformPoint(new Vector3(0.3f, 0.1f, -1.5f));
                        }
                        CameraTablet.transform.position = Vector3.SmoothDamp(CameraTablet.transform.position, targetPosition, ref velocity, 0.1f);
                        CameraTablet.transform.LookAt(CameraFollower.transform.position);
                    }
                    else if (TPVMode == TPVModes.FRONT)
                    {
                        if (followheadrot)
                        {
                            targetPosition = CameraFollower.transform.TransformPoint(new Vector3(0.1f, 0.3f, 2.5f));
                        }
                        else
                        {
                            targetPosition = TPVBodyFollower.transform.TransformPoint(new Vector3(0.1f, 0.3f, 2.5f));
                        }
                        CameraTablet.transform.position = Vector3.SmoothDamp(CameraTablet.transform.position, targetPosition, ref velocity, 0.1f);
                        CameraTablet.transform.LookAt(2f * CameraTablet.transform.position - CameraFollower.transform.position);
                    }
                    if (InputManager.instance.TeleportCamera)
                    {
                        CameraTablet.transform.position = Player.Instance.headCollider.transform.position + Player.Instance.headCollider.transform.forward;
                        foreach (MeshRenderer mr in meshRenderers)
                        {
                            mr.enabled = true;
                        }
                        CameraTablet.transform.parent = null;
                        tpv = false;
                    }
                }
            }
        }
        public void LobbyHop()
        {
            if (lobbyHopBusy || PhotonNetworkController.Instance == null) return;
            StartCoroutine(LobbyHopRoutine());
        }

        /// <summary>
        /// Gorilla-native hop: <c>PhotonNetwork.LeaveRoom(false)</c> keeps you on the Photon master and does
        /// <b>not</b> run <c>NetworkSystem.SinglePlayerStarted</c> → <c>VRRigCache.OnLeftRoom</c>, so scoreboard
        /// lines and rig bindings can stack (20 rows / 10 players, duplicate names, cosmetic mix-ups). The game’s
        /// own leave path is <see cref="NetworkSystem.ReturnToSinglePlayer" />, which tears down voice, disconnects,
        /// and clears rig cache. Rejoin uses <c>AttemptToJoinPublicRoom</c> as usual (same as tunnels).
        /// </summary>
        IEnumerator LobbyHopRoutine()
        {
            lobbyHopBusy = true;
            try
            {
                var pnc = PhotonNetworkController.Instance;
                var ns = NetworkSystem.Instance;
                if (pnc == null || ns == null) yield break;

                pnc.ClearDeferredJoin();

                var trigger = ResolveLobbyHopTrigger(pnc);
                if (trigger == null)
                    yield break;

                if (ns.netState == NetSystemState.InGame || ns.netState == NetSystemState.Connecting)
                {
                    Task leaveTask = ns.ReturnToSinglePlayer();
                    while (!leaveTask.IsCompleted)
                        yield return null;
                    if (leaveTask.IsFaulted)
                        yield break;
                }
                else if (PhotonNetwork.InRoom)
                {
                    PhotonNetwork.LeaveRoom(false);
                    float deadline = Time.realtimeSinceStartup + 15f;
                    while ((PhotonNetwork.InRoom || ns.InRoom) && Time.realtimeSinceStartup < deadline)
                        yield return null;
                    if (PhotonNetwork.InRoom || ns.InRoom)
                        yield break;
                    if (ns.netState == NetSystemState.InGame || ns.netState == NetSystemState.Connecting)
                    {
                        Task leaveTask = ns.ReturnToSinglePlayer();
                        while (!leaveTask.IsCompleted)
                            yield return null;
                        if (leaveTask.IsFaulted)
                            yield break;
                    }
                }

                float idleDeadline = Time.realtimeSinceStartup + 10f;
                while (ns.netState != NetSystemState.Idle && Time.realtimeSinceStartup < idleDeadline)
                    yield return null;
                if (ns.netState != NetSystemState.Idle)
                    yield break;

                // One frame + short realtime so UI/rig cache finishes clearing before matchmaking runs.
                yield return null;
                yield return new WaitForSecondsRealtime(0.1f);
                yield return null;

                if (PhotonNetwork.InRoom || ns.InRoom ||
                    ns.netState == NetSystemState.Connecting ||
                    ns.netState == NetSystemState.Disconnecting ||
                    ns.netState == NetSystemState.Initialization ||
                    ns.netState == NetSystemState.PingRecon)
                    yield break;

                pnc.AttemptToJoinPublicRoom(trigger, JoinType.Solo, null, false);
            }
            finally
            {
                lobbyHopBusy = false;
            }
        }

        static GorillaNetworkJoinTrigger ResolveLobbyHopTrigger(PhotonNetworkController pnc)
        {
            try
            {
                typeof(PhotonNetworkController).GetMethod("UpdateCurrentJoinTrigger",
                    BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(pnc, null);
            }
            catch
            {
                // ignored
            }

            if (pnc.currentJoinTrigger != null)
                return pnc.currentJoinTrigger;

            string zoneStr = "";
            try
            {
                zoneStr = pnc.CurrentRoomZone.ToString();
            }
            catch
            {
                zoneStr = "";
            }

            var gc = GorillaComputer.instance;
            if (gc != null)
            {
                if (!string.IsNullOrEmpty(zoneStr))
                {
                    var byZone = gc.GetJoinTriggerForZone(zoneStr);
                    if (byZone != null) return byZone;

                    if (gc.primaryTriggersByZone != null && gc.primaryTriggersByZone.TryGetValue(zoneStr, out var pb) &&
                        pb != null)
                        return pb;
                }

                if (!string.IsNullOrEmpty(gc.currentQueue))
                {
                    var byQueue = gc.GetJoinTriggerFromFullGameModeString(gc.currentQueue);
                    if (byQueue != null) return byQueue;
                }

                var gmStr = gc.currentGameMode?.Value;
                if (!string.IsNullOrEmpty(gmStr))
                {
                    var byGm = gc.GetJoinTriggerFromFullGameModeString(gmStr);
                    if (byGm != null) return byGm;
                }
            }

            try
            {
                if (string.IsNullOrEmpty(zoneStr))
                    return null;

                var listField = typeof(PhotonNetworkController).GetField("allJoinTriggers",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var list = listField?.GetValue(pnc) as System.Collections.IList;
                if (list == null)
                    return null;

                for (var i = 0; i < list.Count; i++)
                {
                    if (list[i] is GorillaNetworkJoinTrigger jt && jt != null &&
                        !string.IsNullOrEmpty(jt.networkZone) &&
                        string.Equals(jt.networkZone, zoneStr, StringComparison.OrdinalIgnoreCase))
                        return jt;
                }
            }
            catch
            {
                // ignored
            }

            return null;
        }

        public void ResetTabletCamera()
        {
            TabletCameraGO.transform.localPosition = tabletCamDefaultLocalPos;
            TabletCameraGO.transform.localRotation = tabletCamDefaultLocalRot;
            ThirdPersonCameraGO.transform.localPosition = tpCamDefaultLocalPos;
            ThirdPersonCameraGO.transform.localRotation = tpCamDefaultLocalRot;
        }

        public void SwitchToMainPage()
        {
            MiscReturnToExtraInsteadOfMain = false;
            if (MiscPage.activeSelf) MiscPage.SetActive(false);
            if (ExtraPage.activeSelf) ExtraPage.SetActive(false);
            if (WardrobePage.activeSelf) WardrobePage.SetActive(false);
            if (WeatherTimePage.activeSelf) WeatherTimePage.SetActive(false);
            if (CameraClipPage.activeSelf) CameraClipPage.SetActive(false);
            if (GeneralPage.activeSelf) GeneralPage.SetActive(false);
            if (MusicPage != null && MusicPage.activeSelf) MusicPage.SetActive(false);
            if (PinSelectorPage != null && PinSelectorPage.activeSelf) PinSelectorPage.SetActive(false);
            if (ReportPage != null && ReportPage.activeSelf)
            {
                if (TabletReport.Instance != null && TabletReport.Instance.IsInDetail)
                    TabletReport.Instance.HideDetail();
                ReportPage.SetActive(false);
            }

            foreach (GameObject btns in Buttons)
                btns.SetActive(true);
            foreach (MeshRenderer mr in meshRenderers)
                mr.enabled = true;
            MainPage.SetActive(true);
        }

        public void HideRigForFPV()
        {
            foreach (MeshRenderer mr in meshRenderers)
                mr.enabled = false;
            MainPage.SetActive(false);
            if (ExtraPage.activeSelf) ExtraPage.SetActive(false);
            if (WardrobePage.activeSelf) WardrobePage.SetActive(false);
            if (WeatherTimePage.activeSelf) WeatherTimePage.SetActive(false);
            if (CameraClipPage.activeSelf) CameraClipPage.SetActive(false);
            if (GeneralPage.activeSelf) GeneralPage.SetActive(false);
            if (MusicPage != null && MusicPage.activeSelf) MusicPage.SetActive(false);
            if (ReportPage != null && ReportPage.activeSelf) ReportPage.SetActive(false);
            if (FakeCameraGO.activeSelf) FakeCameraGO.SetActive(false);
        }

        public bool HasPinnedPage => !string.IsNullOrEmpty(PlayerPrefs.GetString(ExtraPinPrefKey, ""));

        /// <summary>Show or hide the UnpinButton on a sub-page based on whether it is currently pinned.</summary>
        public void SyncSubPageUnpin(string btnId)
        {
            GameObject page = btnId switch {
                "WeatherTimeBtn" => WeatherTimePage,
                "CameraClipBtn"  => CameraClipPage,
                "GeneralBtn"     => GeneralPage,
                "GridBtn_1_1"    => WardrobePage,
                "GridBtn_1_2"    => ReportPage,
                "MusicBtn"       => MusicPage,
                _                => null
            };
            if (page == null) return;
            var tf = page.transform.Find("UnpinButton");
            if (tf == null) return;
            string pinned = PlayerPrefs.GetString(ExtraPinPrefKey, "");
            tf.gameObject.SetActive(!string.IsNullOrEmpty(pinned) && pinned == btnId);
        }

        /// <summary>Show the ExtraPageUnpinButton only when an action-only page (Save Settings / Lobby Hop) is pinned.</summary>
        public void SyncExtraPageUnpin()
        {
            if (ExtraPageUnpinButton == null) return;
            string id = PlayerPrefs.GetString(ExtraPinPrefKey, "");
            ExtraPageUnpinButton.SetActive(id == "SaveSettsBtn" || id == "LobbyHopBtn" || id == "ExtraMiscBtn");
        }

        public void PinExtraChoice(string yzGButtonName)
        {
            if (string.IsNullOrEmpty(yzGButtonName)) return;
            PlayerPrefs.SetString(ExtraPinPrefKey, yzGButtonName);
            PlayerPrefs.Save();
            RefreshPinnedShortcutLabel();
        }

        public void UnpinExtraChoice()
        {
            PlayerPrefs.DeleteKey(ExtraPinPrefKey);
            PlayerPrefs.Save();
            RefreshPinnedShortcutLabel();
        }

        public void RefreshPinnedShortcutLabel()
        {
            string id = PlayerPrefs.GetString(ExtraPinPrefKey, "");
            if (MainPinSlotButton != null)
            {
                SetOrCreateButtonLabel(MainPinSlotButton,
                    string.IsNullOrEmpty(id) ? "PIN" : ExtraPinLabelForId(id));
            }

            if (MainPinnedShortcutButton != null)
                SetOrCreateButtonLabel(MainPinnedShortcutButton, "EXTRA\nOPTS");
        }

        static string ExtraPinLabelForId(string id)
        {
            switch (id)
            {
                case "WeatherTimeBtn": return "WEATHER\n& TIME";
                case "CameraClipBtn": return "CAMERA\nCLIP";
                case "GeneralBtn": return "GENER\nAL";
                case "SaveSettsBtn": return "SAVE\nSETTS";
                case "LobbyHopBtn": return "LOBBY\nHOP";
                case "GridBtn_1_1": return "WARD\nROBE";
                case "GridBtn_1_2": return "REPO\nRT";
                case "MusicBtn":       return "MUSIC\nCTRL";
                case "ExtraMiscBtn":  return "MISC";
                default: return "EXTRA\nOPTS";
            }
        }

        public void SyncWeatherPageStatusTexts()
        {
            var ui = GetComponent<UI>();
            if (WTRainStatusText != null)
                WTRainStatusText.text = (ui != null && ui.raining) ? "RAIN:ON" : "RAIN:CLEAR";
            if (WTTimeStatusText != null)
            {
                string[] tNames = { "DAWN", "DAY", "NIGHT FALL", "NIGHT", "MIDNIGHT" };
                int tp = (ui != null) ? ui.timePreset : 1;
                if (tp < 0 || tp >= tNames.Length) tp = 1;
                WTTimeStatusText.text = "TIME:" + tNames[tp];
            }
        }

        public void SyncGeneralPageStatusTexts()
        {
            var ui = GetComponent<UI>();
            if (GenWatermarkText != null)
                GenWatermarkText.text = (ui != null && ui.showWatermark) ? "WATERMARK:ON" : "WATERMARK:OFF";
            if (GenRawRotText != null)
                GenRawRotText.text = fpvRawRotation ? "RAW ROTATION:ON" : "RAW ROTATION:OFF";
            if (GenSummonText != null)
            {
                int sm = InputManager.instance != null ? InputManager.instance.summonInputMode : 0;
                if (sm < 0 || sm > 2) sm = 0;
                string[] sLabels = { "KEY:F6", "KEY:X/Y", "" };
                GenSummonText.text = sm == 2
                    ? InputManager.instance.GetCustomBindLabel()
                    : sLabels[sm];
            }
            if (GenCamDisText != null)
                GenCamDisText.text = camDisconnect ? "CAM DIS:ON" : "CAM DIS:OFF";
        }

        /// <summary>Opens the pinned Extra feature from the main pin slot (or Extra page if nothing pinned).</summary>
        public void OpenPinnedShortcutFromMain()
        {
            string id = PlayerPrefs.GetString(ExtraPinPrefKey, "");
            MiscPage.SetActive(false);
            ExtraPage.SetActive(false);
            MainPage.SetActive(false);

            if (string.IsNullOrEmpty(id))
            {
                ExtraPage.SetActive(true);
                return;
            }

            switch (id)
            {
                case "WeatherTimeBtn":
                    WeatherTimePage.SetActive(true);
                    SyncWeatherPageStatusTexts();
                    WeatherTimePage.transform.Find("UnpinButton")?.gameObject.SetActive(true);
                    break;
                case "CameraClipBtn":
                    CameraClipPage.SetActive(true);
                    if (ClipLagStatusText != null)
                        ClipLagStatusText.text = fpvClipping ? "CLIP LAGGING:ON" : "CLIP LAGGING:OFF";
                    if (ClipLagValueText != null)
                        ClipLagValueText.text = fpvClipLag.ToString("F2");
                    CameraClipPage.transform.Find("UnpinButton")?.gameObject.SetActive(true);
                    break;
                case "GeneralBtn":
                    GeneralPage.SetActive(true);
                    SyncGeneralPageStatusTexts();
                    GeneralPage.transform.Find("UnpinButton")?.gameObject.SetActive(true);
                    break;
                case "SaveSettsBtn":
                    // Action-only — execute the save then show ExtraPage with UNPIN visible
                    {
                        var ui = GetComponent<UI>();
                        Settings.Save(
                            fpv ? 0 : fp ? 1 : tpv ? 2 : 3,
                            TabletCamera.fieldOfView,
                            ui.showWatermark,
                            smoothing,
                            ui.timePreset,
                            ui.raining,
                            ThirdPersonCamera.nearClipPlane,
                            InputManager.instance.summonInputMode,
                            fpvRawRotation,
                            fpvClipping,
                            fpvClipLag
                        );
                    }
                    ExtraPage.SetActive(true);
                    SyncExtraPageUnpin();
                    break;
                case "LobbyHopBtn":
                    // Action-only — execute lobby hop then show ExtraPage with UNPIN visible
                    LobbyHop();
                    ExtraPage.SetActive(true);
                    SyncExtraPageUnpin();
                    break;
                case "ExtraMiscBtn":
                    // MISC has no sub-page of its own — open MiscPage directly
                    MiscReturnToExtraInsteadOfMain = false;
                    MiscPage.SetActive(true);
                    break;
                case "GridBtn_1_1":
                    WardrobePage.SetActive(true);
                    TabletWardrobe.Instance?.RefreshDisplay();
                    WardrobePage.transform.Find("UnpinButton")?.gameObject.SetActive(true);
                    break;
                case "GridBtn_1_2":
                    ReportPage.SetActive(true);
                    TabletReport.Instance?.Refresh();
                    ReportPage.transform.Find("UnpinButton")?.gameObject.SetActive(true);
                    break;
                case "MusicBtn":
                    if (MusicPage != null)
                    {
                        MusicPage.SetActive(true);
                        RefreshMediaInfo();
                        MusicPage.transform.Find("UnpinButton")?.gameObject.SetActive(true);
                    }
                    break;
                default:
                    MainPage.SetActive(true);
                    break;
            }
        }

        public void OpenMiscFromExtraPage()
        {
            MiscReturnToExtraInsteadOfMain = true;
            ExtraPage.SetActive(false);
            MiscPage.SetActive(true);
        }

        /// <summary>Into/out of tablet face on MainPage local <c>X</c> (FOLLOW; PIN inherits same <c>X</c>).</summary>
        const float MainPagePlaqueDepthX = -0.007f;

        /// <summary>FOLLOW plaque: MainPage-local depth on X.</summary>
        /// <remarks>MainPage-local <c>Z</c> tracks along the row; use X for in/out, not Z.</remarks>
        static readonly Vector3 FollowPlayerForwardNudge = new Vector3(MainPagePlaqueDepthX, 0f, 0f);

        /// <summary>EXTRA: original bundle offset from the misc plaque (below / along the slab from Follow anchor).</summary>
        static readonly Vector3 MainPinnedShortcutOffsetFromMiscSlot = new Vector3(0f, -0.52f, -0.25f);

        /// <summary>Follow + depth; EXTRA below anchor; PIN same <c>X</c>/<c>Z</c> as FOLLOW (aligned with Follow column), EXTRA row height on <c>Y</c>.</summary>
        void ApplyHomePagePinExtraFollowLayout(Vector3 miscSlotPrefabLocal)
        {
            var fp = GameObject.Find("CameraTablet(Clone)/MainPage/FPButton");
            Transform pinTf = MainPage != null ? MainPage.transform.Find("PinButton") : null;
            if (fp == null || pinTf == null || MainPinnedShortcutButton == null) return;

            Vector3 fpLocal = miscSlotPrefabLocal + FollowPlayerForwardNudge;
            fp.transform.localPosition = fpLocal;

            Vector3 extraPos = miscSlotPrefabLocal + MainPinnedShortcutOffsetFromMiscSlot;
            MainPinnedShortcutButton.transform.localPosition = extraPos;
            pinTf.localPosition = new Vector3(fpLocal.x, extraPos.y, fpLocal.z);
        }

        /// <summary>Disable only the MISC and FOLLOW baked labels on the MainPage Canvas and any 3D TextMesh on MainPage buttons.</summary>
        void StripMainPageBakedLabels()
        {
            var mainCanvas = GameObject.Find("CameraTablet(Clone)/MainPage/Canvas");
            if (mainCanvas != null)
            {
                foreach (var tmp in mainCanvas.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (tmp == null) continue;
                    if (IsMiscOrFollow(tmp.text)) tmp.enabled = false;
                }
                foreach (var tx in mainCanvas.GetComponentsInChildren<Text>(true))
                {
                    if (tx == null) continue;
                    if (IsMiscOrFollow(tx.text)) tx.enabled = false;
                }
            }

            // Also blank any 3D TextMesh labels on MainPage button objects that say MISC or FOLLOW.
            var mainPage = GameObject.Find("CameraTablet(Clone)/MainPage");
            if (mainPage != null)
            {
                foreach (var tm in mainPage.GetComponentsInChildren<TextMesh>(true))
                {
                    if (tm == null || !IsMiscOrFollow(tm.text)) continue;
                    tm.text = "";
                    var mr = tm.GetComponent<MeshRenderer>();
                    if (mr != null) mr.enabled = false;
                }
            }

            bool IsMiscOrFollow(string s)
            {
                if (string.IsNullOrEmpty(s)) return false;
                return s.IndexOf("misc", StringComparison.OrdinalIgnoreCase) >= 0
                    || s.IndexOf("follow", StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }

        /// <summary>Blank TextMesh plaques and disable TMP/Text containing "misc" on these specific button hosts.</summary>
        void StripMiscLettersFromPrefabTextOnHosts(params GameObject[] hosts)
        {
            if (hosts == null) return;
            foreach (GameObject host in hosts)
            {
                if (host == null) continue;

                // Legacy 3D TextMesh plaques — blank only if the text looks like a baked MISC label
                foreach (var tm in host.GetComponentsInChildren<TextMesh>(true))
                {
                    if (tm == null) continue;
                    if (IsMiscLikeText(tm.text)) tm.text = "";
                }

                // TMP_Text — only disable if the text contains "misc"
                foreach (var tmp in host.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (tmp == null) continue;
                    if (IsMiscLikeText(tmp.text)) tmp.enabled = false;
                }

                // Legacy Unity UI Text — only disable if the text contains "misc"
                foreach (var tx in host.GetComponentsInChildren<Text>(true))
                {
                    if (tx == null) continue;
                    if (IsMiscLikeText(tx.text)) tx.enabled = false;
                }
            }

            bool IsMiscLikeText(string s) =>
                !string.IsNullOrEmpty(s) &&
                s.IndexOf("misc", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>Follow sits on misc plaque — crank Z forward hard so green slab buries baked MISC; FOLLOW label sits on canvas above.</summary>
        static readonly Vector3 MeshLabelCoverFollowPlayerSlot = new Vector3(0f, 0.07f, 0.148f);

        /// <summary>PIN slab (cloned misc mesh): forward cover for bleed.</summary>
        static readonly Vector3 MeshLabelCoverPinMiscSlot = new Vector3(0f, 0.058f, 0.098f);

        /// <summary>EXTRA clone under column — bury ghost FOLLOW / misc bleed.</summary>
        static readonly Vector3 MeshLabelCoverExtraVsFollowBleedSlot = new Vector3(0f, 0.058f, 0.118f);

        void AddMeshLabelCoverFromMiscGreen(GameObject host, GameObject miscGreenTemplate, Vector3 localOffset)
        {
            if (host == null || miscGreenTemplate == null) return;
            if (host.transform.Find("MeshLabelCover") != null) return;

            GameObject cover = Instantiate(miscGreenTemplate, host.transform);
            cover.name = "MeshLabelCover";
            cover.transform.localRotation = Quaternion.identity;
            cover.transform.localScale = miscGreenTemplate.transform.localScale;
            cover.transform.localPosition = localOffset;
            foreach (var col in cover.GetComponentsInChildren<Collider>(true))
                Destroy(col);
            cover.transform.SetAsFirstSibling();
        }

        GameObject CreateExtraPage()
        {
            GameObject page = Instantiate(MiscPage, MiscPage.transform.parent);
            page.name = "ExtraPage";
            foreach (Transform child in page.transform)
            {
                if (child.name == "Canvas")
                {
                    foreach (Transform canvasChild in child)
                        Destroy(canvasChild.gameObject);
                }
                else
                {
                    Destroy(child.gameObject);
                }
            }

            var miscBtn = GameObject.Find("CameraTablet(Clone)/MainPage/MiscButton");
            var extraOptBtn = Instantiate(miscBtn, miscBtn.transform.parent);
            extraOptBtn.name = "MainPinnedShortcutBtn";
            extraOptBtn.transform.localPosition = miscBtn.transform.localPosition + MainPinnedShortcutOffsetFromMiscSlot;
            SetOrCreateButtonLabel(extraOptBtn, "EXTRA\nOPTS");
            MainPinnedShortcutButton = extraOptBtn;
            Buttons.Add(extraOptBtn);


            var backTemplate = GameObject.Find("CameraTablet(Clone)/MiscPage/BackButton");
            var extraBackBtn = Instantiate(backTemplate, page.transform);
            extraBackBtn.name = "ExtraBackButton";
            extraBackBtn.transform.localPosition = backTemplate.transform.localPosition + new Vector3(0f, 0.03f, 0f);
            AddButtonLabel(extraBackBtn, "BACK");
            Buttons.Add(extraBackBtn);

            // UNPIN button for action-only pins (Save Settings / Lobby Hop) that have no sub-page
            var extraUnpin = Instantiate(backTemplate, page.transform);
            extraUnpin.name = "ExtraPageUnpinButton";
            extraUnpin.transform.localPosition = backTemplate.transform.localPosition + new Vector3(0f, 0.03f, -1.38f);
            AddButtonLabel(extraUnpin, "UNPIN");
            extraUnpin.SetActive(false);
            Buttons.Add(extraUnpin);
            extraUnpin.AddComponent<YzGButton>();
            ExtraPageUnpinButton = extraUnpin;

            AddPageTitle(page, backTemplate, "EXTRA OPTIONS");

            WeatherTimePage = CreateSubPage(backTemplate, "WeatherTimePage", "WTBackButton", "WEATHER & TIME");
            PopulateWeatherTimePage(WeatherTimePage, backTemplate);

            CameraClipPage = CreateSubPage(backTemplate, "CameraClipPage", "CCBackButton", "CAMERA CLIP");
            PopulateCameraClipPage(CameraClipPage, backTemplate);

            GeneralPage = CreateSubPage(backTemplate, "GeneralPage", "GenBackButton", "GENERAL");
            PopulateGeneralPage(GeneralPage, backTemplate);

            WardrobePage = CreateSubPage(backTemplate, "WardrobePage", "WBBackButton", "WARDROBE");
            foreach (Transform ch in WardrobePage.transform)
            {
                if (ch.name != "PageTitleCanvas")
                    continue;
                Destroy(ch.gameObject);
                break;
            }

            var wbUnpin = WardrobePage.transform.Find("UnpinButton");
            if (wbUnpin != null)
                wbUnpin.localPosition = backTemplate.transform.localPosition + new Vector3(0f, 0.70f, 0f);

            PopulateWardrobePage(WardrobePage, backTemplate);

            MusicPage = CreateSubPage(backTemplate, "MusicPage", "MusicBackButton", "MUSIC CONTROLS");
            PopulateMusicPage(MusicPage, backTemplate);

            ReportPage = CreateSubPage(backTemplate, "ReportPage", "RPBackButton", "REPORT");
            var rpBack = ReportPage.transform.Find("RPBackButton");
            if (rpBack != null)
            {
                rpBack.localScale = rpBack.localScale * 0.7f;
                rpBack.localPosition += new Vector3(0f, -0.04f, 0.05f);
            }
            var rpUnpin = ReportPage.transform.Find("UnpinButton");
            if (rpUnpin != null)
            {
                rpUnpin.localScale    = rpUnpin.localScale * 0.7f;
                // Same Y as rpBack final position (backTemplate.y + 0.03 - 0.04 = backTemplate.y - 0.01)
                rpUnpin.localPosition = backTemplate.transform.localPosition
                    + new Vector3(0f, -0.01f, -1.18f);
            }
            var rpComp = ReportPage.GetComponent<TabletReport>();
            if (rpComp == null) rpComp = ReportPage.AddComponent<TabletReport>();
            rpComp.Init(ReportPage.transform, backTemplate);

            // ── Pin Selector Page ──────────────────────────────────────────────────
            // Same grid layout as Extra Options but pressing a button pins it and
            // returns to the home page instead of navigating to the page.
            PinSelectorPage = Instantiate(MiscPage, MiscPage.transform.parent);
            PinSelectorPage.name = "PinSelectorPage";
            foreach (Transform ch in PinSelectorPage.transform)
            {
                if (ch.name == "Canvas")
                    foreach (Transform cc in ch) Destroy(cc.gameObject);
                else
                    Destroy(ch.gameObject);
            }
            AddPageTitle(PinSelectorPage, backTemplate, "SELECT PAGE TO PIN");
            {
                string[] psLabels = {
                    "WEATHER\n& TIME", "CAMERA\nCLIP", "GENER\nAL", "SAVE\nSETTS", "LOBBY\nHOP",
                    "WARD\nROBE",      "REPO\nRT",      "MISC",      "MUSIC\nCTRL"
                };
                string[] psNames = {
                    "PS_WeatherTimeBtn", "PS_CameraClipBtn", "PS_GeneralBtn", "PS_SaveSettsBtn", "PS_LobbyHopBtn",
                    "PS_WardrobeBtn",    "PS_ReportBtn",     "PS_MiscBtn",    "PS_MusicBtn"
                };
                float[] psRow0Z = { -0.10f, -0.38f, -0.66f, -0.94f, -1.22f };
                float[] psRow1Z = { -0.24f, -0.52f, -0.80f, -1.08f };
                float[] psRowY  = { 0.57f, 0.30f };
                for (int col = 0; col < 5; col++)
                {
                    var b = Instantiate(backTemplate, PinSelectorPage.transform);
                    b.name = psNames[col];
                    b.transform.localPosition = backTemplate.transform.localPosition
                        + new Vector3(0f, psRowY[0], psRow0Z[col]);
                    AddButtonLabel(b, psLabels[col]);
                    Buttons.Add(b); b.AddComponent<YzGButton>();
                }
                for (int col = 0; col < 4; col++)
                {
                    var b = Instantiate(backTemplate, PinSelectorPage.transform);
                    b.name = psNames[5 + col];
                    b.transform.localPosition = backTemplate.transform.localPosition
                        + new Vector3(0f, psRowY[1], psRow1Z[col]);
                    AddButtonLabel(b, psLabels[5 + col]);
                    Buttons.Add(b); b.AddComponent<YzGButton>();
                }
                var psCancel = Instantiate(backTemplate, PinSelectorPage.transform);
                psCancel.name = "PSCancelButton";
                psCancel.transform.localPosition = backTemplate.transform.localPosition + new Vector3(0f, 0.03f, 0f);
                AddButtonLabel(psCancel, "BACK");
                Buttons.Add(psCancel); psCancel.AddComponent<YzGButton>();
            }
            PinSelectorPage.SetActive(false);

            // Row 0: 5 buttons  Row 1: 4 buttons  —  spacing 0.28, rows closer together
            string[] btnLabels = {
                "WEATHER\n& TIME", "CAMERA\nCLIP", "GENER\nAL", "SAVE\nSETTS", "LOBBY\nHOP",
                "WARD\nROBE",      "REPO\nRT",      "MISC",      "MUSIC\nCTRL"
            };
            string[] btnNames = {
                "WeatherTimeBtn", "CameraClipBtn", "GeneralBtn", "SaveSettsBtn", "LobbyHopBtn",
                "GridBtn_1_1",    "GridBtn_1_2",   "ExtraMiscBtn", "MusicBtn"
            };
            // Row 0 (5): -0.10, -0.38, -0.66, -0.94, -1.22
            // Row 1 (4): -0.24, -0.52, -0.80, -1.08   (staggered half-step, naturally centered)
            float[] row0Z = { -0.10f, -0.38f, -0.66f, -0.94f, -1.22f };
            float[] row1Z = { -0.24f, -0.52f, -0.80f, -1.08f };
            float[] rowY  = { 0.57f, 0.30f };
            for (int col = 0; col < 5; col++)
            {
                var gridBtn = Instantiate(backTemplate, page.transform);
                gridBtn.name = btnNames[col];
                gridBtn.transform.localPosition = backTemplate.transform.localPosition
                    + new Vector3(0f, rowY[0], row0Z[col]);
                AddButtonLabel(gridBtn, btnLabels[col]);
                Buttons.Add(gridBtn);
            }
            for (int col = 0; col < 4; col++)
            {
                var gridBtn = Instantiate(backTemplate, page.transform);
                gridBtn.name = btnNames[5 + col];
                gridBtn.transform.localPosition = backTemplate.transform.localPosition
                    + new Vector3(0f, rowY[1], row1Z[col]);
                AddButtonLabel(gridBtn, btnLabels[5 + col]);
                Buttons.Add(gridBtn);
            }

            var hLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hLine.transform.SetParent(page.transform, false);
            hLine.transform.localPosition = backTemplate.transform.localPosition
                + new Vector3(0f, 0.42f, -0.55f);
            hLine.transform.localScale = new Vector3(0.01f, 0.01f, 1.2f);
            hLine.GetComponent<MeshRenderer>().material.color = TabletLabelYellow;
            Destroy(hLine.GetComponent<Collider>());

            var vLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
            vLine.transform.SetParent(page.transform, false);
            vLine.transform.localPosition = backTemplate.transform.localPosition
                + new Vector3(0f, 0.15f, -0.55f);
            vLine.transform.localScale = new Vector3(0.01f, 0.55f, 0.01f);
            vLine.GetComponent<MeshRenderer>().material.color = TabletLabelYellow;
            Destroy(vLine.GetComponent<Collider>());

            return page;
        }

        void PopulateWardrobePage(GameObject page, GameObject btnTemplate)
        {
            var basePos = btnTemplate.transform.localPosition;
            Text summaryText;
            const float zNudgeRight = -0.10f;
            const float yNudgeDown = -0.04f;
            const float yNanoCatPageSide = -0.02f;

            void MakeSummaryCanvas()
            {
                var summaryCanvasGO = new GameObject("WardrobeSummaryCanvas");
                summaryCanvasGO.transform.SetParent(page.transform, false);
                summaryCanvasGO.transform.localPosition =
                    basePos + new Vector3(-0.025f, 0.38f + yNudgeDown, -0.28f);
                summaryCanvasGO.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                summaryCanvasGO.transform.localScale = Vector3.one * 0.003f;
                summaryCanvasGO.AddComponent<Canvas>();
                summaryCanvasGO.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
                var rt = summaryCanvasGO.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(210f, 185f);
                var textGO = new GameObject("SummaryText");
                textGO.transform.SetParent(summaryCanvasGO.transform, false);
                var textRT = textGO.AddComponent<RectTransform>();
                textRT.anchorMin = Vector2.zero;
                textRT.anchorMax = Vector2.one;
                textRT.offsetMin = new Vector2(6f, 6f);
                textRT.offsetMax = new Vector2(-6f, -6f);
                summaryText = textGO.AddComponent<Text>();
                summaryText.fontSize = 17;
                summaryText.alignment = TextAnchor.UpperLeft;
                summaryText.color = TabletLabelYellow;
                summaryText.horizontalOverflow = HorizontalWrapMode.Wrap;
                summaryText.verticalOverflow = VerticalWrapMode.Truncate;
                summaryText.supportRichText = true;
                if (FovText != null)
                    summaryText.font = FovText.font;
            }

            MakeSummaryCanvas();

            void MkBtn(string name, string label, Vector3 offset)
            {
                var btn = Instantiate(btnTemplate, page.transform);
                btn.name = name;
                btn.transform.localPosition = basePos + offset;
                AddButtonLabel(btn, label);
                Buttons.Add(btn);
                btn.AddComponent<YzGButton>();
            }

            // Outfit row (Gorilla saved outfit slots), then category / page / wear columns; SIDE same Y as PAGE row, mid Z.
            const float rzWear1 = -0.70f + zNudgeRight;
            const float rzWear3 = -1.18f + zNudgeRight;
            var rzWear2 = (rzWear1 + rzWear3) * 0.5f;

            const float yOut = 0.70f;
            const float yCat = 0.54f + yNudgeDown + yNanoCatPageSide;
            const float yPage = 0.32f + yNudgeDown + yNanoCatPageSide;
            const float yWear = 0.08f + yNudgeDown;

            MkBtn("WBOutPrevBtn", "OUT\n<", new Vector3(0f, yOut, rzWear1));
            MkBtn("WBOutNextBtn", "OUT\n>", new Vector3(0f, yOut, rzWear3));

            MkBtn("WBCategoryPrevBtn", "< CAT", new Vector3(0f, yCat, rzWear1));
            MkBtn("WBCategoryNextBtn", "CAT >", new Vector3(0f, yCat, rzWear3));

            MkBtn("WBPagePrevBtn", "< PAGE", new Vector3(0f, yPage, rzWear1));
            MkBtn("WBPageNextBtn", "PAGE >", new Vector3(0f, yPage, rzWear3));

            MkBtn("WBWear1Btn", "WEAR 1", new Vector3(0f, yWear, rzWear1));
            MkBtn("WBWear2Btn", "WEAR 2", new Vector3(0f, yWear, rzWear2));
            MkBtn("WBWear3Btn", "WEAR 3", new Vector3(0f, yWear, rzWear3));

            var handBtn = Instantiate(btnTemplate, page.transform);
            handBtn.name = "WBHandBtn";
            handBtn.transform.localPosition = basePos + new Vector3(0f, yPage, rzWear2);
            AddButtonLabel(handBtn, "SIDE");
            Buttons.Add(handBtn);
            handBtn.AddComponent<YzGButton>();

            var tw = page.GetComponent<TabletWardrobe>();
            if (tw == null)
                tw = page.AddComponent<TabletWardrobe>();
            tw.AttachUi(summaryText, handBtn);

            WardrobeModelPreview.Build(page.transform, basePos, yPage, rzWear2, btnTemplate.transform);
        }

        void PopulateGeneralPage(GameObject page, GameObject btnTemplate)
        {
            // horizontal line splitting top half from bottom half
            var hLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hLine.transform.SetParent(page.transform, false);
            hLine.transform.localPosition = btnTemplate.transform.localPosition
                + new Vector3(0f, 0.15f, -0.60f);
            hLine.transform.localScale = new Vector3(0.01f, 0.01f, 1.2f);
            hLine.GetComponent<MeshRenderer>().material.color = TabletLabelYellow;
            Destroy(hLine.GetComponent<Collider>());

            // vertical line splitting into left and right (full height)
            var vLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
            vLine.transform.SetParent(page.transform, false);
            vLine.transform.localPosition = btnTemplate.transform.localPosition
                + new Vector3(0f, 0.25f, -0.60f);
            vLine.transform.localScale = new Vector3(0.01f, 1.2f, 0.01f);
            vLine.GetComponent<MeshRenderer>().material.color = TabletLabelYellow;
            Destroy(vLine.GetComponent<Collider>());

            // top-left: WATERMARK
            var wmBtn = Instantiate(btnTemplate, page.transform);
            wmBtn.name = "GenWatermarkBtn";
            wmBtn.transform.localPosition = btnTemplate.transform.localPosition
                + new Vector3(0f, 0.60f, -0.35f);
            AddButtonLabel(wmBtn, "WATER\nMARK");
            Buttons.Add(wmBtn);
            wmBtn.AddComponent<YzGButton>();

            var wmCanvas = CreateStatusCanvas(page, btnTemplate, new Vector3(-0.02f, 0.40f, -0.35f));
            GenWatermarkText = wmCanvas;
            var uiComp = GetComponent<UI>();
            GenWatermarkText.text = (uiComp != null && uiComp.showWatermark) ? "WATERMARK:ON" : "WATERMARK:OFF";

            // top-right: RAW ROTATION
            var rrBtn = Instantiate(btnTemplate, page.transform);
            rrBtn.name = "GenRawRotBtn";
            rrBtn.transform.localPosition = btnTemplate.transform.localPosition
                + new Vector3(0f, 0.60f, -0.95f);
            AddButtonLabel(rrBtn, "RAW\nROTATION");
            Buttons.Add(rrBtn);
            rrBtn.AddComponent<YzGButton>();

            var rrCanvas = CreateStatusCanvas(page, btnTemplate, new Vector3(-0.02f, 0.40f, -0.95f));
            GenRawRotText = rrCanvas;
            GenRawRotText.text = fpvRawRotation ? "RAW ROTATION:ON" : "RAW ROTATION:OFF";

            // bottom-left: SUMMON KEY
            var skBtn = Instantiate(btnTemplate, page.transform);
            skBtn.name = "GenSummonBtn";
            skBtn.transform.localPosition = btnTemplate.transform.localPosition
                + new Vector3(0f, 0.15f, -0.35f);
            AddButtonLabel(skBtn, "SUMMON\nKEY");
            Buttons.Add(skBtn);
            skBtn.AddComponent<YzGButton>();

            int sMode = InputManager.instance != null ? InputManager.instance.summonInputMode : 0;
            if (sMode < 0 || sMode > 2) sMode = 0;
            var skCanvas = CreateStatusCanvas(page, btnTemplate, new Vector3(-0.02f, -0.05f, -0.35f));
            GenSummonText = skCanvas;
            string[] summonLabels = { "KEY:F6", "KEY:X/Y", "" };
            GenSummonText.text = sMode == 2 && InputManager.instance != null
                ? InputManager.instance.GetCustomBindLabel()
                : summonLabels[sMode];

            // bottom-right: CAMERA DISCONNECT
            var cdBtn = Instantiate(btnTemplate, page.transform);
            cdBtn.name = "GenCamDisBtn";
            cdBtn.transform.localPosition = btnTemplate.transform.localPosition
                + new Vector3(0f, 0.15f, -0.95f);
            AddButtonLabel(cdBtn, "CAMER\nDIS.");
            Buttons.Add(cdBtn);
            cdBtn.AddComponent<YzGButton>();

            var cdCanvas = CreateStatusCanvas(page, btnTemplate, new Vector3(-0.02f, -0.05f, -0.95f));
            GenCamDisText = cdCanvas;
            GenCamDisText.text = camDisconnect ? "CAM DIS:ON" : "CAM DIS:OFF";
        }

        void AddPageTitle(GameObject page, GameObject btnTemplate, string title)
        {
            var canvasGO = new GameObject("PageTitleCanvas");
            canvasGO.transform.SetParent(page.transform, false);
            canvasGO.transform.localPosition = btnTemplate.transform.localPosition
                + new Vector3(-0.02f, 0.78f, -0.70f);
            canvasGO.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            canvasGO.transform.localScale = Vector3.one * 0.004f;
            var c = canvasGO.AddComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;
            var rt = canvasGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(300f, 40f);
            var textGO = new GameObject("TitleText");
            textGO.transform.SetParent(canvasGO.transform, false);
            var textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
            var t = textGO.AddComponent<Text>();
            t.text = title;
            t.fontSize = TabletPageTitleFontSize;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = TabletLabelYellow;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.fontStyle = FontStyle.Bold;
            if (FovText != null) t.font = FovText.font;
        }

        Text CreateStatusCanvas(GameObject parent, GameObject btnTemplate, Vector3 offset)
        {
            var canvasGO = new GameObject("StatusCanvas");
            canvasGO.transform.SetParent(parent.transform, false);
            canvasGO.transform.localPosition = btnTemplate.transform.localPosition + offset;
            canvasGO.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            canvasGO.transform.localScale = Vector3.one * 0.003f;
            var c = canvasGO.AddComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;
            var rt = canvasGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(250f, 40f);
            var textGO = new GameObject("StatusText");
            textGO.transform.SetParent(canvasGO.transform, false);
            var textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
            var t = textGO.AddComponent<Text>();
            t.fontSize = 22;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = TabletLabelYellow;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            if (FovText != null) t.font = FovText.font;
            return t;
        }

        void PopulateWeatherTimePage(GameObject page, GameObject btnTemplate)
        {
            string[] timeLabels = { "DAWN", "DAY", "NIGHT\nFALL", "MID\nNIGHT" };
            string[] timeNames = { "WTDawnBtn", "WTDayBtn", "WTNightFallBtn", "WTMidnightBtn" };
            for (int i = 0; i < 4; i++)
            {
                var btn = Instantiate(btnTemplate, page.transform);
                btn.name = timeNames[i];
                btn.transform.localPosition = btnTemplate.transform.localPosition
                    + new Vector3(0f, 0.57f, -0.25f - i * 0.3f);
                AddButtonLabel(btn, timeLabels[i]);
                Buttons.Add(btn);
                btn.AddComponent<YzGButton>();
            }

            string[] weatherLabels = { "CLEAR", "RAIN" };
            string[] weatherNames = { "WTClearBtn", "WTRainBtn" };
            for (int i = 0; i < 2; i++)
            {
                var btn = Instantiate(btnTemplate, page.transform);
                btn.name = weatherNames[i];
                btn.transform.localPosition = btnTemplate.transform.localPosition
                    + new Vector3(0f, 0.27f, -0.55f - i * 0.3f);
                AddButtonLabel(btn, weatherLabels[i]);
                Buttons.Add(btn);
                btn.AddComponent<YzGButton>();
            }

            var rainCanvas = new GameObject("RainStatusCanvas");
            rainCanvas.transform.SetParent(page.transform, false);
            rainCanvas.transform.localPosition = btnTemplate.transform.localPosition
                + new Vector3(-0.02f, -0.02f, -0.35f);
            rainCanvas.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            rainCanvas.transform.localScale = Vector3.one * 0.003f;
            var rc = rainCanvas.AddComponent<Canvas>();
            rc.renderMode = RenderMode.WorldSpace;
            var rrt = rainCanvas.GetComponent<RectTransform>();
            rrt.sizeDelta = new Vector2(150f, 40f);
            var rainTextGO = new GameObject("RainText");
            rainTextGO.transform.SetParent(rainCanvas.transform, false);
            var rainRT = rainTextGO.AddComponent<RectTransform>();
            rainRT.anchorMin = Vector2.zero;
            rainRT.anchorMax = Vector2.one;
            rainRT.offsetMin = Vector2.zero;
            rainRT.offsetMax = Vector2.zero;
            WTRainStatusText = rainTextGO.AddComponent<Text>();
            WTRainStatusText.text = "RAIN:CLEAR";
            WTRainStatusText.fontSize = 22;
            WTRainStatusText.alignment = TextAnchor.MiddleCenter;
            WTRainStatusText.color = TabletLabelYellow;
            WTRainStatusText.horizontalOverflow = HorizontalWrapMode.Overflow;
            WTRainStatusText.verticalOverflow = VerticalWrapMode.Overflow;
            if (FovText != null) WTRainStatusText.font = FovText.font;

            var timeCanvas = new GameObject("TimeStatusCanvas");
            timeCanvas.transform.SetParent(page.transform, false);
            timeCanvas.transform.localPosition = btnTemplate.transform.localPosition
                + new Vector3(-0.02f, -0.02f, -0.95f);
            timeCanvas.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            timeCanvas.transform.localScale = Vector3.one * 0.003f;
            var tc = timeCanvas.AddComponent<Canvas>();
            tc.renderMode = RenderMode.WorldSpace;
            var trt = timeCanvas.GetComponent<RectTransform>();
            trt.sizeDelta = new Vector2(150f, 40f);
            var timeTextGO = new GameObject("TimeText");
            timeTextGO.transform.SetParent(timeCanvas.transform, false);
            var timeRT = timeTextGO.AddComponent<RectTransform>();
            timeRT.anchorMin = Vector2.zero;
            timeRT.anchorMax = Vector2.one;
            timeRT.offsetMin = Vector2.zero;
            timeRT.offsetMax = Vector2.zero;
            WTTimeStatusText = timeTextGO.AddComponent<Text>();
            WTTimeStatusText.text = "TIME:DAY";
            WTTimeStatusText.fontSize = 22;
            WTTimeStatusText.alignment = TextAnchor.MiddleCenter;
            WTTimeStatusText.color = TabletLabelYellow;
            WTTimeStatusText.horizontalOverflow = HorizontalWrapMode.Overflow;
            WTTimeStatusText.verticalOverflow = VerticalWrapMode.Overflow;
            if (FovText != null) WTTimeStatusText.font = FovText.font;
        }

        void PopulateCameraClipPage(GameObject page, GameObject btnTemplate)
        {
            var toggleBtn = Instantiate(btnTemplate, page.transform);
            toggleBtn.name = "CCToggleBtn";
            toggleBtn.transform.localPosition = btnTemplate.transform.localPosition
                + new Vector3(0f, 0.57f, -0.40f);
            AddButtonLabel(toggleBtn, fpvClipping ? "ON" : "OFF");
            Buttons.Add(toggleBtn);
            toggleBtn.AddComponent<YzGButton>();

            var statusCanvas = new GameObject("ClipStatusCanvas");
            statusCanvas.transform.SetParent(page.transform, false);
            statusCanvas.transform.localPosition = btnTemplate.transform.localPosition
                + new Vector3(-0.02f, 0.57f, -0.82f);
            statusCanvas.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            statusCanvas.transform.localScale = Vector3.one * 0.003f;
            var sc = statusCanvas.AddComponent<Canvas>();
            sc.renderMode = RenderMode.WorldSpace;
            var srt = statusCanvas.GetComponent<RectTransform>();
            srt.sizeDelta = new Vector2(200f, 40f);
            var statusGO = new GameObject("ClipStatusText");
            statusGO.transform.SetParent(statusCanvas.transform, false);
            var statusRT = statusGO.AddComponent<RectTransform>();
            statusRT.anchorMin = Vector2.zero;
            statusRT.anchorMax = Vector2.one;
            statusRT.offsetMin = Vector2.zero;
            statusRT.offsetMax = Vector2.zero;
            ClipLagStatusText = statusGO.AddComponent<Text>();
            ClipLagStatusText.text = fpvClipping ? "CLIP LAGGING:ON" : "CLIP LAGGING:OFF";
            ClipLagStatusText.fontSize = 22;
            ClipLagStatusText.alignment = TextAnchor.MiddleCenter;
            ClipLagStatusText.color = TabletLabelYellow;
            ClipLagStatusText.horizontalOverflow = HorizontalWrapMode.Overflow;
            ClipLagStatusText.verticalOverflow = VerticalWrapMode.Overflow;
            if (FovText != null) ClipLagStatusText.font = FovText.font;

            var minusBtn = Instantiate(btnTemplate, page.transform);
            minusBtn.name = "CCMinusBtn";
            minusBtn.transform.localPosition = btnTemplate.transform.localPosition
                + new Vector3(0f, 0.17f, -0.40f);
            AddButtonLabel(minusBtn, "-");
            Buttons.Add(minusBtn);
            minusBtn.AddComponent<YzGButton>();

            var plusBtn = Instantiate(btnTemplate, page.transform);
            plusBtn.name = "CCPlusBtn";
            plusBtn.transform.localPosition = btnTemplate.transform.localPosition
                + new Vector3(0f, 0.17f, -1.0f);
            AddButtonLabel(plusBtn, "+");
            Buttons.Add(plusBtn);
            plusBtn.AddComponent<YzGButton>();

            var sliderBg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sliderBg.transform.SetParent(page.transform, false);
            sliderBg.transform.localPosition = btnTemplate.transform.localPosition
                + new Vector3(0f, 0.17f, -0.70f);
            sliderBg.transform.localScale = new Vector3(0.05f, 0.05f, 0.25f);
            sliderBg.GetComponent<MeshRenderer>().material.color = new Color(0.3f, 0.3f, 0.3f);
            Destroy(sliderBg.GetComponent<Collider>());

            var valueCanvas = new GameObject("ClipValueCanvas");
            valueCanvas.transform.SetParent(page.transform, false);
            valueCanvas.transform.localPosition = btnTemplate.transform.localPosition
                + new Vector3(-0.02f, 0.17f, -0.70f);
            valueCanvas.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            valueCanvas.transform.localScale = Vector3.one * 0.005f;
            var vc = valueCanvas.AddComponent<Canvas>();
            vc.renderMode = RenderMode.WorldSpace;
            var vrt = valueCanvas.GetComponent<RectTransform>();
            vrt.sizeDelta = new Vector2(150f, 40f);
            var textGO = new GameObject("ClipValueText");
            textGO.transform.SetParent(valueCanvas.transform, false);
            var textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
            ClipLagValueText = textGO.AddComponent<Text>();
            ClipLagValueText.text = fpvClipLag.ToString("F2");
            ClipLagValueText.fontSize = 22;
            ClipLagValueText.alignment = TextAnchor.MiddleCenter;
            ClipLagValueText.color = TabletLabelYellow;
            ClipLagValueText.horizontalOverflow = HorizontalWrapMode.Overflow;
            ClipLagValueText.verticalOverflow = VerticalWrapMode.Overflow;
            if (FovText != null) ClipLagValueText.font = FovText.font;
        }

        // ─── External Media Control (Spotify / YouTube / any Windows media player) ──
        // Uses GorillaToolkit's QuickSong.exe (placed in %TEMP% by GorillaToolkit on startup)
        // which queries Windows SMTC and returns JSON with Title, Artist, ElapsedTime, EndTime, Status.

        [System.Runtime.InteropServices.DllImport("user32.dll",
            CallingConvention = System.Runtime.InteropServices.CallingConvention.StdCall,
            CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        static extern void keybd_event(uint bVk, uint bScan, uint dwFlags, uint dwExtraInfo);

        public const uint MK_PLAY_PAUSE = 179u;
        public const uint MK_NEXT       = 176u;
        public const uint MK_PREV       = 177u;
        public const uint MK_VOL_UP     = 0xAFu;
        public const uint MK_VOL_DOWN   = 0xAEu;
        public const uint MK_MUTE       = 0xADu;

        public void SendMediaKeyPublic(uint vk)
        {
            keybd_event(vk, 0u, 0u, 0u);
            keybd_event(vk, 0u, 2u, 0u);   // KEYEVENTF_KEYUP
        }

        static string QuickSongExePath =>
            Path.Combine(Path.GetTempPath(), "QuickSong.exe");

        public void RefreshMediaInfo()
        {
            if (_mediaBusy) return;
            _mediaBusy = true;

            Task.Run(() =>
            {
                string songLine = "♪  —", artistLine = "";
                double elapsed = 0, endTime = 0;
                bool paused = true;
                DateTime fetchTime = DateTime.UtcNow;
                try
                {
                    string exePath = QuickSongExePath;
                    if (!File.Exists(exePath))
                    {
                        songLine = "♪  GorillaToolkit not found";
                    }
                    else
                    {
                        var psi = new System.Diagnostics.ProcessStartInfo(exePath, "-all")
                        {
                            UseShellExecute        = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError  = true,
                            CreateNoWindow         = true
                        };
                        using (var proc = System.Diagnostics.Process.Start(psi))
                        {
                            string json = proc.StandardOutput.ReadToEnd();
                            proc.WaitForExit(4000);
                            var title   = Regex.Match(json, "\"Title\":\"([^\"]+)\"");
                            var artist  = Regex.Match(json, "\"Artist\":\"([^\"]+)\"");
                            var endM    = Regex.Match(json, "\"EndTime\":([0-9.]+)");
                            var elM     = Regex.Match(json, "\"ElapsedTime\":([0-9.]+)");
                            var statusM = Regex.Match(json, "\"Status\":\"([^\"]+)\"");
                            if (title.Success)
                            {
                                songLine   = "♪  " + title.Groups[1].Value;
                                artistLine = artist.Success ? artist.Groups[1].Value : "";
                                endTime    = endM.Success ? double.Parse(endM.Groups[1].Value,
                                    System.Globalization.CultureInfo.InvariantCulture) : 0;
                                elapsed    = elM.Success ? double.Parse(elM.Groups[1].Value,
                                    System.Globalization.CultureInfo.InvariantCulture) : 0;
                                paused     = !statusM.Success ||
                                    statusM.Groups[1].Value != "Playing";
                            }
                            else songLine = "♪  No media playing";
                        }
                    }
                }
                catch { songLine = "♪  —"; }

                _mediaSongLine    = songLine;
                _mediaArtistLine  = artistLine;
                _mediaElapsed     = elapsed;
                _mediaEndTime     = endTime;
                _mediaPaused      = paused;
                _mediaFetchTime   = fetchTime;
                _mediaBusy        = false;
                _mediaRefreshed   = true;
            });
        }

        public void SyncMusicPageState()
        {
            if (MusicSongNameText != null) MusicSongNameText.text = _mediaSongLine;
        }

        void PopulateMusicPage(GameObject page, GameObject btnTemplate)
        {
            var bp = btnTemplate.transform.localPosition;

            Text MakeLabel(string goName, Vector3 offset, Vector2 size, int fontSize)
            {
                var cGO = new GameObject(goName + "Canvas");
                cGO.transform.SetParent(page.transform, false);
                cGO.transform.localPosition = bp + offset;
                cGO.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                cGO.transform.localScale    = Vector3.one * 0.003f;
                var c = cGO.AddComponent<Canvas>(); c.renderMode = RenderMode.WorldSpace;
                cGO.GetComponent<RectTransform>().sizeDelta = size;
                var tGO = new GameObject(goName); tGO.transform.SetParent(cGO.transform, false);
                var tRT = tGO.AddComponent<RectTransform>();
                tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
                tRT.offsetMin = Vector2.zero; tRT.offsetMax = Vector2.zero;
                var tx = tGO.AddComponent<Text>();
                tx.fontSize = fontSize; tx.alignment = TextAnchor.MiddleCenter;
                tx.color = TabletLabelYellow;
                tx.horizontalOverflow = HorizontalWrapMode.Wrap;
                tx.verticalOverflow   = VerticalWrapMode.Overflow;
                if (FovText != null) tx.font = FovText.font;
                return tx;
            }

            GameObject MakeBtn(string name, string label, Vector3 offset)
            {
                var btn = Instantiate(btnTemplate, page.transform);
                btn.name = name; btn.transform.localPosition = bp + offset;
                AddButtonLabel(btn, label); Buttons.Add(btn);
                btn.AddComponent<YzGButton>(); return btn;
            }

            void MakeLine(Vector3 offset, Vector3 scale)
            {
                var ln = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ln.transform.SetParent(page.transform, false);
                ln.transform.localPosition = bp + offset;
                ln.transform.localScale    = scale;
                ln.GetComponent<MeshRenderer>().material.color = TabletLabelYellow;
                Destroy(ln.GetComponent<Collider>());
            }

            // Button columns: Back≈Z=0, Unpin≈Z=-1.38.
            // Playback: PREV pushed far left, NEXT far right.
            // Volume: centered like before but shifted down.
            const float cM = -0.69f;

            // ── System clock (above Back button, top-left) ────────────────────────
            MusicClockText = MakeLabel("MusicClock",
                new Vector3(-0.02f, 0.77f, -0.05f), new Vector2(200f, 36f), 20);
            MusicClockText.text = DateTime.Now.ToString("h:mm tt");

            // ── Song name ──────────────────────────────────────────────────────────
            MusicSongNameText = MakeLabel("MusicSong",
                new Vector3(-0.02f, 0.63f, cM), new Vector2(430f, 52f), 23);
            MusicSongNameText.text = _mediaSongLine;

            // ── Artist | status | remaining time ──────────────────────────────────
            MusicTimeText = MakeLabel("MusicTime",
                new Vector3(-0.02f, 0.49f, cM), new Vector2(430f, 36f), 18);
            MusicTimeText.text = "";

            MakeLine(new Vector3(0f, 0.40f, cM), new Vector3(0.01f, 0.01f, 1.1f));

            // ── Playback row (PREV far left, NEXT far right) ───────────────────────
            MakeBtn("MusicPrevBtn",      "|<\nPREV",    new Vector3(0f, 0.30f, -0.10f));
            MakeBtn("MusicPlayPauseBtn", "PLAY\nPAUSE", new Vector3(0f, 0.30f, cM));
            MakeBtn("MusicNextBtn",      "NEXT\n>|",    new Vector3(0f, 0.30f, -1.28f));

            MakeLine(new Vector3(0f, 0.20f, cM), new Vector3(0.01f, 0.01f, 1.1f));

            // ── Volume row (slightly lower than playback) ──────────────────────────
            MakeBtn("MusicVolDownBtn", "VOL-", new Vector3(0f, 0.05f, -0.35f));
            MakeBtn("MusicMuteBtn",    "MUTE", new Vector3(0f, 0.05f, cM));
            MakeBtn("MusicVolUpBtn",   "VOL+", new Vector3(0f, 0.05f, -1.04f));
        }

        // ─────────────────────────────────────────────────────────────────────────

        GameObject CreateSubPage(GameObject btnTemplate, string pageName, string backBtnName, string pageTitle)
        {
            var subPage = Instantiate(MiscPage, MiscPage.transform.parent);
            subPage.name = pageName;
            foreach (Transform child in subPage.transform)
            {
                if (child.name == "Canvas")
                {
                    foreach (Transform canvasChild in child)
                        Destroy(canvasChild.gameObject);
                }
                else
                {
                    Destroy(child.gameObject);
                }
            }
            var backBtn = Instantiate(btnTemplate, subPage.transform);
            backBtn.name = backBtnName;
            backBtn.transform.localPosition = btnTemplate.transform.localPosition + new Vector3(0f, 0.03f, 0f);
            AddButtonLabel(backBtn, "BACK");
            Buttons.Add(backBtn);
            backBtn.AddComponent<YzGButton>();

            var unpinBtn = Instantiate(btnTemplate, subPage.transform);
            unpinBtn.name = "UnpinButton";
            unpinBtn.transform.localPosition = btnTemplate.transform.localPosition + new Vector3(0f, 0.03f, -1.38f);
            AddButtonLabel(unpinBtn, "UNPIN");
            unpinBtn.SetActive(false);
            Buttons.Add(unpinBtn);
            unpinBtn.AddComponent<YzGButton>();

            AddPageTitle(subPage, btnTemplate, pageTitle);
            subPage.SetActive(false);
            return subPage;
        }

        void AddButtonLabel(GameObject btn, string labelText)
        {
            var canvasGO = new GameObject("LabelCanvas");
            canvasGO.transform.SetParent(btn.transform, false);
            canvasGO.transform.localPosition = new Vector3(-0.60f, -0.02f, 0f);
            canvasGO.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            canvasGO.transform.localScale = Vector3.one * 0.01f;
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 0;
            var rt = canvasGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(120f, 60f);

            var textGO = new GameObject("Label");
            textGO.transform.SetParent(canvasGO.transform, false);
            var textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
            var uiText = textGO.AddComponent<Text>();
            uiText.text = labelText;
            uiText.fontSize = TabletWorldButtonFontSize;
            uiText.alignment = TextAnchor.MiddleCenter;
            uiText.color = TabletLabelYellow;
            uiText.horizontalOverflow = HorizontalWrapMode.Overflow;
            uiText.verticalOverflow = VerticalWrapMode.Overflow;
            if (FovText != null) uiText.font = FovText.font;
        }

        /// <summary>Apply <see cref="TabletWorldButtonFontSize"/> and yellow to programmatic <see cref="Text"/> overlays.</summary>
        void RestoreOriginalOverlayLabelSizing(GameObject btn)
        {
            if (btn == null) return;
            Transform lcTf = btn.transform.Find("LabelCanvas");
            if (lcTf == null) return;
            RectTransform crt = lcTf.GetComponent<RectTransform>();
            if (crt != null)
                crt.sizeDelta = new Vector2(120f, 60f);

            Transform labelTf = lcTf.Find("Label");
            var uit = labelTf != null ? labelTf.GetComponent<Text>() : null;
            if (uit == null) return;

            uit.fontSize = TabletWorldButtonFontSize;
            uit.lineSpacing = 1f;
            uit.resizeTextForBestFit = false;
            uit.horizontalOverflow = HorizontalWrapMode.Overflow;
            uit.verticalOverflow = VerticalWrapMode.Overflow;
            uit.color = TabletLabelYellow;
        }

        void SetOrCreateButtonLabel(GameObject btn, string text, bool preserveSize = false, int sizeOverride = 0)
        {
            int resolvedSize = sizeOverride > 0 ? sizeOverride : TabletWorldButtonFontSize;

            Transform labelPath = btn.transform.Find("LabelCanvas/Label");
            if (labelPath != null && labelPath.GetComponent<Text>() is Text pathText)
            {
                pathText.text = text;
                if (!preserveSize) pathText.fontSize = resolvedSize;
                return;
            }

            var tmp = btn.GetComponentInChildren<TextMeshPro>(true);
            if (tmp != null)
            {
                tmp.text = text;
                if (!preserveSize) tmp.fontSize = resolvedSize;
                tmp.color = TabletLabelYellow;
                tmp.enabled = true;
                return;
            }
            var tmpUI = btn.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmpUI != null)
            {
                tmpUI.text = text;
                if (!preserveSize) tmpUI.fontSize = resolvedSize;
                tmpUI.color = TabletLabelYellow;
                tmpUI.enabled = true;
                return;
            }
            var existingText = btn.GetComponentInChildren<Text>(true);
            if (existingText != null)
            {
                existingText.text = text;
                if (!preserveSize) existingText.fontSize = resolvedSize;
                existingText.color = TabletLabelYellow;
                existingText.enabled = true;
                return;
            }

            var canvasGO = new GameObject("LabelCanvas");
            canvasGO.transform.SetParent(btn.transform, false);
            canvasGO.transform.localPosition = new Vector3(-0.60f, -0.02f, 0f);
            canvasGO.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            canvasGO.transform.localScale = Vector3.one * 0.01f;
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 0;
            var rt = canvasGO.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(120f, 60f);

            var textGO = new GameObject("Label");
            textGO.transform.SetParent(canvasGO.transform, false);
            var textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
            var labelText = textGO.AddComponent<Text>();
            labelText.text = text;
            labelText.fontSize = TabletWorldButtonFontSize;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = TabletLabelYellow;
            labelText.horizontalOverflow = HorizontalWrapMode.Overflow;
            labelText.verticalOverflow = VerticalWrapMode.Overflow;
            if (FovText != null) labelText.font = FovText.font;
            RestoreOriginalOverlayLabelSizing(btn);
        }

        GameObject LoadBundle(string goname, string resourcename)
        {
            Stream str = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourcename);
            AssetBundle asb = AssetBundle.LoadFromStream(str);
            GameObject go = Instantiate<GameObject>(asb.LoadAsset<GameObject>(goname));
            asb.Unload(false);
            str.Close();
            return go;
        }

        void RemoveBundledBananaVisualFromTablet()
        {
            if (CameraTablet == null) return;

            static bool MatchesBanana(string s) =>
                !string.IsNullOrEmpty(s) && s.IndexOf("banana", StringComparison.OrdinalIgnoreCase) >= 0;

            var toDestroy = new HashSet<GameObject>();
            foreach (var t in CameraTablet.GetComponentsInChildren<Transform>(true))
            {
                if (t != null && MatchesBanana(t.name))
                    toDestroy.Add(t.gameObject);
            }

            foreach (var mf in CameraTablet.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh sm = mf != null ? mf.sharedMesh : null;
                if (sm != null && MatchesBanana(sm.name))
                    toDestroy.Add(mf.gameObject);
            }

            foreach (var smr in CameraTablet.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                Mesh sm = smr != null ? smr.sharedMesh : null;
                if (sm != null && MatchesBanana(sm.name))
                    toDestroy.Add(smr.gameObject);
            }

            foreach (var go in toDestroy)
            {
                if (go != null) Destroy(go);
            }
        }

        void ReplaceAtlasTexture()
        {
            Stream str = Assembly.GetExecutingAssembly().GetManifestResourceStream("YizziCamModV2.Assets.Atlas");
            byte[] data;
            using (MemoryStream ms = new MemoryStream())
            {
                str.CopyTo(ms);
                data = ms.ToArray();
            }
            str.Close();

            Texture2D customAtlas = new Texture2D(2, 2);
            ImageConversion.LoadImage(customAtlas, data);

            Renderer[] allRenderers = FindObjectsOfType<Renderer>(true);
            foreach (Renderer renderer in allRenderers)
            {
                foreach (Material mat in renderer.sharedMaterials)
                {
                    if (mat != null && mat.HasProperty("_MainTex"))
                    {
                        Texture tex = mat.mainTexture;
                        if (tex != null && tex.name.ToLower().Contains("atlas"))
                        {
                            mat.mainTexture = customAtlas;
                        }
                    }
                }
            }
        }
    }
}
