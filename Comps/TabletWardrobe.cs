using System.Collections.Generic;
using GorillaNetworking;
using UnityEngine;
using UnityEngine.UI;

namespace YizziCamModV2.Comps
{
    /// <summary>
    /// Mirrors Gorilla stump wardrobe lists (<c>CosmeticsController</c> unlock arrays). Applies with
    /// <see cref="CosmeticsController.PressWardrobeItemButton" /> so cosmetics persist like the real wardrobe.
    /// </summary>
    public class TabletWardrobe : MonoBehaviour
    {
        public static TabletWardrobe Instance { get; private set; }

        readonly string[] CatLabels =
        {
            "HAT", "FACE", "BADGE", "PAW", "FUR", "SHIRT", "PANTS", "ARMS", "BACK", "CHEST", "TAG FX"
        };

        const int CategoryCount = 11;

        int _category;
        int _page;
        bool _pawEquipRightHand = true;

        public Text SummaryText { get; private set; }
        public GameObject HandButtonRoot { get; private set; }

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        internal void AttachUi(Text summary, GameObject handRow)
        {
            SummaryText = summary;
            HandButtonRoot = handRow;
            RefreshDisplay();
        }

        public void TogglePawSide()
        {
            if (_category != 3)
                return;
            _pawEquipRightHand = !_pawEquipRightHand;
            RefreshDisplay();
        }

        public void CycleCategory(int delta)
        {
            var cc = CosmeticsController.instance;
            if (cc == null)
                return;

            var start = _category;
            for (var step = 0; step < CategoryCount; step++)
            {
                _category = ((_category + delta) % CategoryCount + CategoryCount) % CategoryCount;
                var list = GetList(cc, _category);
                if (list != null && list.Count > 0)
                {
                    _page = 0;
                    ClampPage(cc);
                    RefreshDisplay();
                    return;
                }
            }

            _category = start;
            ClampPage(cc);
            RefreshDisplay();
        }

        public void CyclePage(int delta)
        {
            var cc = CosmeticsController.instance;
            if (cc == null)
                return;
            var list = GetList(cc, _category);
            if (list == null || list.Count == 0)
            {
                RefreshDisplay();
                return;
            }

            var maxPage = Mathf.Max(0, (list.Count - 1) / 3);
            _page = Mathf.Clamp(_page + delta, 0, maxPage);
            RefreshDisplay();
        }

        public void EquipSlot(int slotIndexInPage)
        {
            if (slotIndexInPage < 0 || slotIndexInPage > 2)
                return;
            var cc = CosmeticsController.instance;
            if (cc == null)
                return;

            var list = GetList(cc, _category);
            if (list == null || list.Count == 0)
                return;

            var idx = _page * 3 + slotIndexInPage;
            if (idx < 0 || idx >= list.Count)
                return;

            var item = list[idx];
            if (item.isNullItem || string.IsNullOrEmpty(item.itemName))
                return;

            var isLeftHand = _category == 3 && !_pawEquipRightHand;
            cc.PressWardrobeItemButton(item, isLeftHand, false);
            RefreshDisplay();
        }

        public void ScrollOutfit(bool forward)
        {
            var cc = CosmeticsController.instance;
            if (cc == null || !CosmeticsController.CanScrollOutfits())
                return;
            cc.PressWardrobeScrollOutfit(forward);
            RefreshDisplay();
        }

        public void RefreshDisplay()
        {
            var cc = CosmeticsController.instance;
            if (SummaryText == null)
                return;

            if (HandButtonRoot != null)
                HandButtonRoot.SetActive(_category == 3);

            if (cc == null)
            {
                SummaryText.text = "COSMETICS\nNOT READY";
                return;
            }

            var list = GetList(cc, _category);
            if (list == null || list.Count == 0)
            {
                _page = 0;
                SummaryText.text = $"{CatLabels[_category]}\n\n(NO ITEMS)";
                return;
            }

            var maxPage = Mathf.Max(0, (list.Count - 1) / 3);
            _page = Mathf.Clamp(_page, 0, maxPage);

            var lines = $"{CatLabels[_category]}    PAGE {_page + 1}/{maxPage + 1}    x{list.Count}\n";
            for (var s = 0; s < 3; s++)
            {
                var i = _page * 3 + s;
                var label = "---";
                var equipped = false;
                if (i < list.Count)
                {
                    label = TrimName(list[i]);
                    equipped = !list[i].isNullItem && cc.IsCosmeticEquipped(list[i]);
                }
                if (equipped)
                    lines += $"\n<color=green>[{s + 1}] {label}</color>";
                else
                    lines += $"\n[{s + 1}] {label}";
            }

            if (_category == 3)
                lines += _pawEquipRightHand ? "\n\nHAND: RIGHT" : "\n\nHAND: LEFT";
            SummaryText.text = lines;
        }

        void ClampPage(CosmeticsController cc)
        {
            var list = GetList(cc, _category);
            if (list == null || list.Count == 0)
            {
                _page = 0;
                return;
            }

            var maxPage = Mathf.Max(0, (list.Count - 1) / 3);
            _page = Mathf.Clamp(_page, 0, maxPage);
        }

        static string TrimName(CosmeticsController.CosmeticItem item)
        {
            if (item.isNullItem)
                return "---";
            var s = item.overrideDisplayName;
            if (string.IsNullOrEmpty(s))
                s = item.displayName;
            if (string.IsNullOrEmpty(s))
                s = item.itemName;
            if (string.IsNullOrEmpty(s))
                return "---";
            return s.Length <= 14 ? s : s.Substring(0, 13) + ">";
        }

        static List<CosmeticsController.CosmeticItem> GetList(CosmeticsController cc, int catIndex)
        {
            return catIndex switch
            {
                0 => cc.unlockedHats,
                1 => cc.unlockedFaces,
                2 => cc.unlockedBadges,
                3 => cc.unlockedPaws,
                4 => cc.unlockedFurs,
                5 => cc.unlockedShirts,
                6 => cc.unlockedPants,
                7 => cc.unlockedArms,
                8 => cc.unlockedBacks,
                9 => cc.unlockedChests,
                10 => cc.unlockedTagFX,
                _ => null
            };
        }
    }
}
