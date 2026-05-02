using UnityEngine;
#pragma warning disable CS0618
namespace YizziCamModV2.Comps
{
    class YzGButton : MonoBehaviour
    {
        void Start()
        {
            this.gameObject.layer = 18;
        }
        void OnEnable() { Invoke("ButtonTimer", 1f); }
        void OnDisable() { CameraController.Instance.canbeused = false; }
        void ButtonTimer()
        {
            if (!this.enabled)
            {
                CameraController.Instance.canbeused = false;
            }
            CameraController.Instance.canbeused = true;
        }
        void OnTriggerEnter(Collider col)
        {
            if (CameraController.Instance.canbeused && (col.name == "RightHandTriggerCollider" || col.name == "LeftHandTriggerCollider"))
            {
                CameraController.Instance.canbeused = false;
                Invoke("ButtonTimer", 1f);
                switch (this.name)
                {
                    case "BackButton":
                        CameraController.Instance.MainPage.SetActive(true);
                        CameraController.Instance.MiscPage.SetActive(false);
                        break;
                    case "ExtraOptButton":
                        CameraController.Instance.MainPage.SetActive(false);
                        CameraController.Instance.MiscPage.SetActive(false);
                        CameraController.Instance.ExtraPage.SetActive(true);
                        break;
                    case "ExtraBackButton":
                        CameraController.Instance.ExtraPage.SetActive(false);
                        CameraController.Instance.MainPage.SetActive(true);
                        break;
                    case "WeatherTimeBtn":
                        CameraController.Instance.ExtraPage.SetActive(false);
                        CameraController.Instance.WeatherTimePage.SetActive(true);
                        {
                            var ui = CameraController.Instance.GetComponent<UI>();
                            if (CameraController.Instance.WTRainStatusText != null)
                                CameraController.Instance.WTRainStatusText.text = (ui != null && ui.raining) ? "RAIN:ON" : "RAIN:CLEAR";
                            if (CameraController.Instance.WTTimeStatusText != null)
                            {
                                string[] tNames = { "DAWN", "DAY", "NIGHT FALL", "NIGHT", "MIDNIGHT" };
                                int tp = (ui != null) ? ui.timePreset : 1;
                                if (tp < 0 || tp >= tNames.Length) tp = 1;
                                CameraController.Instance.WTTimeStatusText.text = "TIME:" + tNames[tp];
                            }
                        }
                        break;
                    case "WTBackButton":
                        CameraController.Instance.WeatherTimePage.SetActive(false);
                        CameraController.Instance.ExtraPage.SetActive(true);
                        break;
                    case "CameraClipBtn":
                        CameraController.Instance.ExtraPage.SetActive(false);
                        CameraController.Instance.CameraClipPage.SetActive(true);
                        {
                            if (CameraController.Instance.ClipLagStatusText != null)
                                CameraController.Instance.ClipLagStatusText.text = CameraController.Instance.fpvClipping ? "CLIP LAGGING:ON" : "CLIP LAGGING:OFF";
                            if (CameraController.Instance.ClipLagValueText != null)
                                CameraController.Instance.ClipLagValueText.text = CameraController.Instance.fpvClipLag.ToString("F2");
                        }
                        break;
                    case "CCBackButton":
                        CameraController.Instance.CameraClipPage.SetActive(false);
                        CameraController.Instance.ExtraPage.SetActive(true);
                        break;
                    case "GeneralBtn":
                        CameraController.Instance.ExtraPage.SetActive(false);
                        CameraController.Instance.GeneralPage.SetActive(true);
                        {
                            var ui = CameraController.Instance.GetComponent<UI>();
                            if (CameraController.Instance.GenWatermarkText != null)
                                CameraController.Instance.GenWatermarkText.text = (ui != null && ui.showWatermark) ? "WATERMARK:ON" : "WATERMARK:OFF";
                            if (CameraController.Instance.GenRawRotText != null)
                                CameraController.Instance.GenRawRotText.text = CameraController.Instance.fpvRawRotation ? "RAW ROTATION:ON" : "RAW ROTATION:OFF";
                            if (CameraController.Instance.GenSummonText != null)
                            {
                                int sm = InputManager.instance != null ? InputManager.instance.summonInputMode : 0;
                                if (sm < 0 || sm > 2) sm = 0;
                                string[] sLabels = { "KEY:F6", "KEY:X/Y", "" };
                                CameraController.Instance.GenSummonText.text = sm == 2
                                    ? InputManager.instance.GetCustomBindLabel()
                                    : sLabels[sm];
                            }
                            if (CameraController.Instance.GenCamDisText != null)
                                CameraController.Instance.GenCamDisText.text = CameraController.Instance.camDisconnect ? "CAM DIS:ON" : "CAM DIS:OFF";
                        }
                        break;
                    case "GenBackButton":
                        CameraController.Instance.GeneralPage.SetActive(false);
                        CameraController.Instance.ExtraPage.SetActive(true);
                        break;
                    case "SaveSettsBtn":
                        {
                            var ui = CameraController.Instance.GetComponent<UI>();
                            Settings.Save(
                                CameraController.Instance.fpv ? 0 : CameraController.Instance.fp ? 1 : CameraController.Instance.tpv ? 2 : 3,
                                CameraController.Instance.TabletCamera.fieldOfView,
                                ui.showWatermark,
                                CameraController.Instance.smoothing,
                                ui.timePreset,
                                ui.raining,
                                CameraController.Instance.ThirdPersonCamera.nearClipPlane,
                                InputManager.instance.summonInputMode,
                                CameraController.Instance.fpvRawRotation,
                                CameraController.Instance.fpvClipping,
                                CameraController.Instance.fpvClipLag
                            );
                        }
                        break;
                    case "LobbyHopBtn":
                        CameraController.Instance.LobbyHop();
                        break;
                    case "GridBtn_1_1":
                        CameraController.Instance.ExtraPage.SetActive(false);
                        CameraController.Instance.WardrobePage.SetActive(true);
                        TabletWardrobe.Instance?.RefreshDisplay();
                        break;
                    case "WBBackButton":
                        CameraController.Instance.WardrobePage.SetActive(false);
                        CameraController.Instance.ExtraPage.SetActive(true);
                        break;
                    case "GridBtn_1_2":
                        CameraController.Instance.ExtraPage.SetActive(false);
                        CameraController.Instance.ReportPage.SetActive(true);
                        TabletReport.Instance?.Refresh();
                        break;
                    case "RPBackButton":
                        if (TabletReport.Instance != null && TabletReport.Instance.IsInDetail)
                        {
                            TabletReport.Instance.HideDetail();
                        }
                        else
                        {
                            CameraController.Instance.ReportPage.SetActive(false);
                            CameraController.Instance.ExtraPage.SetActive(true);
                        }
                        break;
                    case "RPDetailBack":
                        TabletReport.Instance?.HideDetail();
                        break;
                    case "RPPreviewBtn":
                        TabletReport.Instance?.CycleDetailView();
                        break;
                    case "RPHateSpeech":
                        if (TabletReport.Instance != null && TabletReport.Instance.IsInDetail)
                        {
                            var hsLine = TabletReport.Instance.FindScoreboardLine(TabletReport.Instance.DetailActorNumber);
                            if (hsLine != null) hsLine.PressButton(true, GorillaPlayerLineButton.ButtonType.HateSpeech);
                        }
                        break;
                    case "RPToxicity":
                        if (TabletReport.Instance != null && TabletReport.Instance.IsInDetail)
                        {
                            var txLine = TabletReport.Instance.FindScoreboardLine(TabletReport.Instance.DetailActorNumber);
                            if (txLine != null) txLine.PressButton(true, GorillaPlayerLineButton.ButtonType.Toxicity);
                        }
                        break;
                    case "RPCheating":
                        if (TabletReport.Instance != null && TabletReport.Instance.IsInDetail)
                        {
                            var chLine = TabletReport.Instance.FindScoreboardLine(TabletReport.Instance.DetailActorNumber);
                            if (chLine != null) chLine.PressButton(true, GorillaPlayerLineButton.ButtonType.Cheating);
                        }
                        break;
                    case "RPVoiceFocus":
                        if (TabletReport.Instance != null && TabletReport.Instance.IsInDetail)
                            TabletReport.Instance.ToggleVoiceFocus();
                        break;
                    case "RPMute":
                        if (TabletReport.Instance != null && TabletReport.Instance.IsInDetail)
                            TabletReport.Instance.ToggleMute();
                        break;
                    case "WBCategoryPrevBtn":
                        TabletWardrobe.Instance?.CycleCategory(-1);
                        break;
                    case "WBCategoryNextBtn":
                        TabletWardrobe.Instance?.CycleCategory(1);
                        break;
                    case "WBPagePrevBtn":
                        TabletWardrobe.Instance?.CyclePage(-1);
                        break;
                    case "WBPageNextBtn":
                        TabletWardrobe.Instance?.CyclePage(1);
                        break;
                    case "WBWear1Btn":
                        TabletWardrobe.Instance?.EquipSlot(0);
                        break;
                    case "WBWear2Btn":
                        TabletWardrobe.Instance?.EquipSlot(1);
                        break;
                    case "WBWear3Btn":
                        TabletWardrobe.Instance?.EquipSlot(2);
                        break;
                    case "WBPreviewBtn":
                        WardrobeModelPreview.Instance?.CycleView();
                        break;
                    case "WBHandBtn":
                        TabletWardrobe.Instance?.TogglePawSide();
                        break;
                    case "WBOutPrevBtn":
                        TabletWardrobe.Instance?.ScrollOutfit(false);
                        break;
                    case "WBOutNextBtn":
                        TabletWardrobe.Instance?.ScrollOutfit(true);
                        break;
                    case "GenCamDisBtn":
                        CameraController.Instance.camDisconnect = !CameraController.Instance.camDisconnect;
                        UnityEngine.PlayerPrefs.SetInt("YizziCamDis", CameraController.Instance.camDisconnect ? 1 : 0);
                        UnityEngine.PlayerPrefs.Save();
                        if (!CameraController.Instance.camDisconnect && CameraController.Instance.fpv)
                        {
                            CameraController.Instance.ResetTabletCamera();
                            CameraController.Instance.HideRigForFPV();
                        }
                        if (CameraController.Instance.GenCamDisText != null)
                            CameraController.Instance.GenCamDisText.text = CameraController.Instance.camDisconnect ? "CAM DIS:ON" : "CAM DIS:OFF";
                        if (CameraController.Instance.GenRawRotText != null)
                            CameraController.Instance.GenRawRotText.text = CameraController.Instance.fpvRawRotation ? "RAW ROTATION:ON" : "RAW ROTATION:OFF";
                        break;
                    case "GenWatermarkBtn":
                        {
                            var ui = CameraController.Instance.GetComponent<UI>();
                            ui.showWatermark = !ui.showWatermark;
                            if (CameraController.Instance.GenWatermarkText != null)
                                CameraController.Instance.GenWatermarkText.text = ui.showWatermark ? "WATERMARK:ON" : "WATERMARK:OFF";
                        }
                        break;
                    case "GenRawRotBtn":
                        CameraController.Instance.fpvRawRotation = !CameraController.Instance.fpvRawRotation;
                        if (CameraController.Instance.GenRawRotText != null)
                            CameraController.Instance.GenRawRotText.text = CameraController.Instance.fpvRawRotation ? "RAW ROTATION:ON" : "RAW ROTATION:OFF";
                        break;
                    case "GenSummonBtn":
                        {
                            int sm = InputManager.instance.summonInputMode;
                            sm = (sm + 1) % 3;
                            InputManager.instance.summonInputMode = sm;
                            if (sm == 2)
                            {
                                InputManager.instance.waitingForCustomBind = true;
                                if (CameraController.Instance.GenSummonText != null)
                                    CameraController.Instance.GenSummonText.text = "KEY:PRESS ANY...";
                            }
                            else
                            {
                                InputManager.instance.waitingForCustomBind = false;
                                string[] sLabels = { "KEY:F6", "KEY:X/Y" };
                                if (CameraController.Instance.GenSummonText != null)
                                    CameraController.Instance.GenSummonText.text = sLabels[sm];
                            }
                        }
                        break;
                    case "CCToggleBtn":
                        CameraController.Instance.fpvClipping = !CameraController.Instance.fpvClipping;
                        if (CameraController.Instance.ClipLagStatusText != null)
                            CameraController.Instance.ClipLagStatusText.text = CameraController.Instance.fpvClipping ? "CLIP LAGGING:ON" : "CLIP LAGGING:OFF";
                        var toggleLabel = this.GetComponentInChildren<UnityEngine.UI.Text>(true);
                        if (toggleLabel != null) toggleLabel.text = CameraController.Instance.fpvClipping ? "ON" : "OFF";
                        break;
                    case "CCMinusBtn":
                        CameraController.Instance.fpvClipLag = Mathf.Clamp(CameraController.Instance.fpvClipLag - 0.025f, 0.05f, 0.95f);
                        if (CameraController.Instance.ClipLagValueText != null)
                            CameraController.Instance.ClipLagValueText.text = CameraController.Instance.fpvClipLag.ToString("F2");
                        CameraController.Instance.canbeused = true;
                        break;
                    case "CCPlusBtn":
                        CameraController.Instance.fpvClipLag = Mathf.Clamp(CameraController.Instance.fpvClipLag + 0.025f, 0.05f, 0.95f);
                        if (CameraController.Instance.ClipLagValueText != null)
                            CameraController.Instance.ClipLagValueText.text = CameraController.Instance.fpvClipLag.ToString("F2");
                        CameraController.Instance.canbeused = true;
                        break;
                    case "WTDawnBtn":
                        { var ui = CameraController.Instance.GetComponent<UI>(); ui.timePreset = 0; }
                        BetterDayNightManager.instance.SetTimeOfDay(1);
                        if (CameraController.Instance.WTTimeStatusText != null)
                            CameraController.Instance.WTTimeStatusText.text = "TIME:DAWN";
                        break;
                    case "WTDayBtn":
                        { var ui = CameraController.Instance.GetComponent<UI>(); ui.timePreset = 1; }
                        BetterDayNightManager.instance.SetTimeOfDay(3);
                        if (CameraController.Instance.WTTimeStatusText != null)
                            CameraController.Instance.WTTimeStatusText.text = "TIME:DAY";
                        break;
                    case "WTNightFallBtn":
                        { var ui = CameraController.Instance.GetComponent<UI>(); ui.timePreset = 2; }
                        BetterDayNightManager.instance.SetTimeOfDay(6);
                        if (CameraController.Instance.WTTimeStatusText != null)
                            CameraController.Instance.WTTimeStatusText.text = "TIME:NIGHT FALL";
                        break;
                    case "WTMidnightBtn":
                        { var ui = CameraController.Instance.GetComponent<UI>(); ui.timePreset = 4; }
                        BetterDayNightManager.instance.SetTimeOfDay(8);
                        if (CameraController.Instance.WTTimeStatusText != null)
                            CameraController.Instance.WTTimeStatusText.text = "TIME:MIDNIGHT";
                        break;
                    case "WTClearBtn":
                        { var ui = CameraController.Instance.GetComponent<UI>(); ui.raining = false; }
                        BetterDayNightManager.instance.ClearFixedWeather();
                        if (CameraController.Instance.WTRainStatusText != null)
                            CameraController.Instance.WTRainStatusText.text = "RAIN:CLEAR";
                        break;
                    case "WTRainBtn":
                        { var ui = CameraController.Instance.GetComponent<UI>(); ui.raining = true; }
                        BetterDayNightManager.instance.SetFixedWeather(BetterDayNightManager.WeatherType.Raining);
                        if (CameraController.Instance.WTRainStatusText != null)
                            CameraController.Instance.WTRainStatusText.text = "RAIN:ON";
                        break;
                    case "ControlsButton":
                        if (!CameraController.Instance.openedurl)
                        {
                            Application.OpenURL("https://github.com/julievonix/YizziCamModReimagined#controls");
                            CameraController.Instance.openedurl = true;
                        }
                        break;
                    case "SmoothingDownButton":
                        CameraController.Instance.smoothing -= 0.01f;
                        if (CameraController.Instance.smoothing < 0.05f)
                        {
                            CameraController.Instance.smoothing = 0.11f;
                        }
                        CameraController.Instance.SmoothText.text = CameraController.Instance.smoothing.ToString();
                        CameraController.Instance.canbeused = true;
                        break;
                    case "SmoothingUpButton":
                        CameraController.Instance.smoothing += 0.01f;
                        if (CameraController.Instance.smoothing > 0.11f)
                        {
                            CameraController.Instance.smoothing = 0.05f;
                        }
                        CameraController.Instance.SmoothText.text = CameraController.Instance.smoothing.ToString();
                        CameraController.Instance.canbeused = true;
                        break;
                    case "TPVButton":
                        if (CameraController.Instance.TPVMode == CameraController.TPVModes.BACK)
                        {
                            if (CameraController.Instance.flipped)
                            {
                                CameraController.Instance.flipped = false;
                                CameraController.Instance.ThirdPersonCameraGO.transform.Rotate(0.0f, 180f, 0.0f);
                                CameraController.Instance.TabletCameraGO.transform.Rotate(0.0f, 180f, 0.0f);
                            }
                        }
                        else if (CameraController.Instance.TPVMode == CameraController.TPVModes.FRONT)
                        {
                            if (!CameraController.Instance.flipped)
                            {
                                CameraController.Instance.flipped = true;
                                CameraController.Instance.ThirdPersonCameraGO.transform.Rotate(0.0f, 180f, 0.0f);
                                CameraController.Instance.TabletCameraGO.transform.Rotate(0.0f, 180f, 0.0f);
                            }
                        }
                        CameraController.Instance.fp = false;
                        CameraController.Instance.fpv = false;
                        CameraController.Instance.tpv = true;
                        break;
                    case "FPVButton":
                        if (CameraController.Instance.flipped)
                        {
                            CameraController.Instance.flipped = false;
                            CameraController.Instance.ThirdPersonCameraGO.transform.Rotate(0.0f, 180f, 0.0f);
                            CameraController.Instance.TabletCameraGO.transform.Rotate(0.0f, 180f, 0.0f);
                        }
                        CameraController.Instance.fp = false;
                        CameraController.Instance.fpv = true;
                        break;
                    case "FlipCamButton":
                        CameraController.Instance.flipped = !CameraController.Instance.flipped;
                        CameraController.Instance.ThirdPersonCameraGO.transform.Rotate(0.0f, 180f, 0.0f);
                        CameraController.Instance.TabletCameraGO.transform.Rotate(0.0f, 180f, 0.0f);
                        break;
                    case "FovDown":
                        CameraController.Instance.TabletCamera.fieldOfView -= 5f;
                        if (CameraController.Instance.TabletCamera.fieldOfView < 20)
                        {
                            CameraController.Instance.TabletCamera.fieldOfView = 130f;
                            CameraController.Instance.ThirdPersonCamera.fieldOfView = 130f;
                        }
                        CameraController.Instance.ThirdPersonCamera.fieldOfView = CameraController.Instance.TabletCamera.fieldOfView;
                        CameraController.Instance.FovText.text = CameraController.Instance.TabletCamera.fieldOfView.ToString();
                        CameraController.Instance.canbeused = true;
                        break;
                    case "FovUP":
                        CameraController.Instance.TabletCamera.fieldOfView += 5f;
                        if (CameraController.Instance.TabletCamera.fieldOfView > 130)
                        {
                            CameraController.Instance.TabletCamera.fieldOfView = 20f;
                            CameraController.Instance.ThirdPersonCamera.fieldOfView = 20f;
                        }
                        CameraController.Instance.ThirdPersonCamera.fieldOfView = CameraController.Instance.TabletCamera.fieldOfView;
                        CameraController.Instance.FovText.text = CameraController.Instance.TabletCamera.fieldOfView.ToString();
                        CameraController.Instance.canbeused = true;
                        break;
                    case "MiscButton":
                        CameraController.Instance.MainPage.SetActive(false);
                        CameraController.Instance.MiscPage.SetActive(true);
                        break;
                    case "NearClipDown":
                        CameraController.Instance.TabletCamera.nearClipPlane -= 0.01f;
                        if (CameraController.Instance.TabletCamera.nearClipPlane < 0.01)
                        {
                            CameraController.Instance.TabletCamera.nearClipPlane = 1f;
                            CameraController.Instance.ThirdPersonCamera.nearClipPlane = 1f;
                        }
                        CameraController.Instance.ThirdPersonCamera.nearClipPlane = CameraController.Instance.TabletCamera.nearClipPlane;
                        CameraController.Instance.NearClipText.text = CameraController.Instance.TabletCamera.nearClipPlane.ToString();
                        CameraController.Instance.canbeused = true;
                        break;
                    case "NearClipUp":
                        CameraController.Instance.TabletCamera.nearClipPlane += 0.01f;
                        if (CameraController.Instance.TabletCamera.nearClipPlane > 1.0)
                        {
                            CameraController.Instance.TabletCamera.nearClipPlane = 0.01f;
                            CameraController.Instance.ThirdPersonCamera.nearClipPlane = 0.01f;
                        }
                        CameraController.Instance.ThirdPersonCamera.nearClipPlane = CameraController.Instance.TabletCamera.nearClipPlane;
                        CameraController.Instance.NearClipText.text = CameraController.Instance.TabletCamera.nearClipPlane.ToString();
                        CameraController.Instance.canbeused = true;
                        break;
                    case "FPButton":
                        CameraController.Instance.fp = !CameraController.Instance.fp;
                        break;
                    case "MinDistDownButton":
                        CameraController.Instance.minDist -= 0.1f;
                        if (CameraController.Instance.minDist < 1)
                        {
                            CameraController.Instance.minDist = 1;
                        }
                        CameraController.Instance.MinDistText.text = CameraController.Instance.minDist.ToString();
                        CameraController.Instance.canbeused = true;
                        break;
                    case "MinDistUpButton":
                        CameraController.Instance.minDist += 0.1f;
                        if (CameraController.Instance.minDist > 10)
                        {
                            CameraController.Instance.minDist = 10;
                        }
                        CameraController.Instance.MinDistText.text = CameraController.Instance.minDist.ToString();
                        CameraController.Instance.canbeused = true;
                        break;
                    case "SpeedUpButton":
                        CameraController.Instance.fpspeed += 0.01f;
                        if (CameraController.Instance.fpspeed > 0.1)
                        {
                            CameraController.Instance.fpspeed = 0.1f;
                        }
                        CameraController.Instance.SpeedText.text = CameraController.Instance.fpspeed.ToString();
                        CameraController.Instance.canbeused = true;
                        break;
                    case "SpeedDownButton":
                        CameraController.Instance.fpspeed -= 0.01f;
                        if (CameraController.Instance.fpspeed < 0.01)
                        {
                            CameraController.Instance.fpspeed = 0.01f;
                        }
                        CameraController.Instance.SpeedText.text = CameraController.Instance.fpspeed.ToString();
                        CameraController.Instance.canbeused = true;
                        break;
                    case "TPModeDownButton":
                        if (CameraController.Instance.TPVMode == CameraController.TPVModes.BACK)
                        {
                            CameraController.Instance.TPVMode = CameraController.TPVModes.FRONT;
                        }
                        else
                        {
                            CameraController.Instance.TPVMode = CameraController.TPVModes.BACK;
                        }
                        CameraController.Instance.TPText.text = CameraController.Instance.TPVMode.ToString();
                        break;
                    case "TPModeUpButton":
                        if (CameraController.Instance.TPVMode == CameraController.TPVModes.BACK)
                        {
                            CameraController.Instance.TPVMode = CameraController.TPVModes.FRONT;
                        }
                        else
                        {
                            CameraController.Instance.TPVMode = CameraController.TPVModes.BACK;
                        }
                        CameraController.Instance.TPText.text = CameraController.Instance.TPVMode.ToString();
                        break;
                    case "TPRotButton":
                        CameraController.Instance.followheadrot = !CameraController.Instance.followheadrot;
                        CameraController.Instance.TPRotText.text = CameraController.Instance.followheadrot.ToString().ToUpper();
                        break;
                    case "TPRotButton1":
                        CameraController.Instance.followheadrot = !CameraController.Instance.followheadrot;
                        CameraController.Instance.TPRotText.text = CameraController.Instance.followheadrot.ToString().ToUpper();
                        break;
                    case "GreenScreenButton":
                        CameraController.Instance.ColorScreenGO.active = !CameraController.Instance.ColorScreenGO.active;
                        if (CameraController.Instance.ColorScreenGO.active)
                        {
                            CameraController.Instance.ColorScreenText.text = "(ENABLED)";
                        }
                        else
                        {
                            CameraController.Instance.ColorScreenText.text = "(DISABLED)";
                        }
                        break;
                    case "RedButton":
                        foreach (Material mat in CameraController.Instance.ScreenMats)
                        {
                            mat.color = Color.red;
                        }
                        break;
                    case "GreenButton":
                        foreach (Material mat in CameraController.Instance.ScreenMats)
                        {
                            mat.color = Color.green;
                        }
                        break;
                    case "BlueButton":
                        foreach (Material mat in CameraController.Instance.ScreenMats)
                        {
                            mat.color = Color.blue;
                        }
                        break;
                    default:
                        if (this.name.StartsWith("RPPlayerBtn_"))
                        {
                            var idxStr = this.name.Substring("RPPlayerBtn_".Length);
                            if (int.TryParse(idxStr, out int idx) && TabletReport.Instance != null)
                            {
                                int actor = TabletReport.Instance.GetActorNumberForIndex(idx);
                                if (actor > 0) TabletReport.Instance.ShowDetail(actor);
                            }
                        }
                        break;
                }
            }
        }
    }
}
