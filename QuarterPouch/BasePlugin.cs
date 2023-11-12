using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.OptionsAPI;
using MTM101BaldAPI.SaveSystem;
using UnityEngine;
using TMPro;

namespace QuarterPouch
{
    [BepInPlugin("mtm101.rulerp.baldiplus.quarterpouch", "Quarter Pouch", "1.0.0.0")]
    public class QuarterPouchPlugin : BaseUnityPlugin
    {

        public static QuarterPouchPlugin Instance;

        public static int QuarterSizeLimit = 4;

        public static AdjustmentBars QSLBar;

        public static TextLocalizer QSLText;

        public static event Action<PouchManager> InitializePouches;

        public static void CallPouchInit(PouchManager pm)
        {
            InitializePouches.Invoke(pm);
        }

        void Awake()
        {
            Instance = this;
            InitializePouches += InitQuarterPouch;
            Harmony harmony = new Harmony("mtm101.rulerp.baldiplus.quarterpouch");
            harmony.PatchAll();
            CustomOptionsCore.OnMenuInitialize += OnMen;
            ModdedSaveSystem.AddSaveLoadAction(this, (bool isSave, string myPath) =>
            {
                if (isSave)
                {
                    File.WriteAllText(Path.Combine(myPath, "pouchSize.txt"), QuarterSizeLimit.ToString());
                }
                else
                {
                    if (File.Exists(Path.Combine(myPath, "pouchSize.txt")))
                    {
                        QuarterSizeLimit = int.Parse(File.ReadAllText(Path.Combine(myPath, "pouchSize.txt")));
                    }
                }
            });
        }

        void InitQuarterPouch(PouchManager pouchM)
        {
            pouchM.Add(new QuarterPouch());
        }
        void OnMen(OptionsMenu __instance)
        {
            if (Singleton<CoreGameManager>.Instance != null) return; // these settings can only be changed when an active game is NOT going
            GameObject ob = CustomOptionsCore.CreateNewCategory(__instance, "Opt_QuarterPouch");
            // all this bloat code is to change the size of the text and position it properly, as the standard text size CreateText makes is too small
            TextLocalizer TL = CustomOptionsCore.CreateText(__instance, new Vector2(-80f, 60f), "Opt_QPSize");
            RectTransform rt = TL.gameObject.GetComponent<RectTransform>();
            rt.offsetMax = new Vector2(60, rt.offsetMax.y);
            TL.transform.position = new Vector3(-0f, 60f, gameObject.transform.position.z);
            // These aren't locals because otherwise the delegates/functions wouldn't be able to acces them
            QSLText = CustomOptionsCore.CreateText(__instance, new Vector2(-60f, 30f), "$" + (QuarterSizeLimit * 0.25).ToString("0.00"));
            QSLBar = CustomOptionsCore.CreateAdjustmentBar(__instance, new Vector2(-92f, 0f), "QuarterSize", 30, "Tip_QPSize", QuarterSizeLimit, () =>
            {
                QuarterSizeLimit = QSLBar.GetRaw();
                QSLText.GetLocalizedText("$" + (QuarterSizeLimit * 0.25).ToString("0.00"));
            });
            // attach everything to the options menu
            QSLBar.transform.SetParent(ob.transform, false);
            QSLText.transform.SetParent(ob.transform, false);
            TL.transform.SetParent(ob.transform, false);
        }
    }

    public static class Extensions
    {
        public static PouchManager GetPouchManager(this PlayerManager me)
        {
            //tf?
            if (me == null) return null;
            PouchManager pouchM = Singleton<CoreGameManager>.Instance.gameObject.GetComponents<PouchManager>().Where(x => x.playerIndex == me.playerNumber).FirstOrDefault();
            if (pouchM == null)
            {
                pouchM = Singleton<CoreGameManager>.Instance.gameObject.AddComponent<PouchManager>();
                pouchM.playerIndex = me.playerNumber;
            }
            return pouchM;
        }
    }

    // this class handles all pouches
    public class PouchManager : MonoBehaviour
    {
        public int playerIndex = -1;
        public Pouch[] Pouches => pouches.ToArray();
        private List<Pouch> pouches = new List<Pouch>();
        private Dictionary<Pouch, double> BackedUpValues = new Dictionary<Pouch, double>();

        public void Backup()
        {
            BackedUpValues.Clear();
            foreach (Pouch p in pouches)
            {
                BackedUpValues.Add(p,p.amount);
            }
        }

        public void ReloadBackup()
        {
            foreach (KeyValuePair<Pouch, double> pvd in BackedUpValues)
            {
                pvd.Key.ResetAmountTo(pvd.Value);
            }
        }

        public void Add(Pouch p)
        {
            pouches.Add(p);
            if (p.preserveAfterReset)
            {
                BackedUpValues.Add(p, p.amount);
            }
        }
    }
}