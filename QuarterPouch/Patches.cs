using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;
using HarmonyLib.Tools;
using MTM101BaldAPI;
using System.Linq;

namespace QuarterPouch
{
    [HarmonyPatch(typeof(CoreGameManager))]
    [HarmonyPatch("SpawnPlayers")]
    public class SpawnPlayerPatch
    {
        static void Postfix(CoreGameManager __instance, PlayerManager[] ___players)
        {
            for (int i = 0; i < ___players.Length; i++)
            {
                // get pouch manager also adds one if it doesn't already exist
                // add a quarter pouch since well. that's the main reason for this mod's existance
                PouchManager pouchM = ___players[i].GetPouchManager();
                if (pouchM != null)
                {
                    // stop the player from getting 2 pouches
                    if (pouchM.Pouches.Length == 0)
                    {
                        QuarterPouchPlugin.CallPouchInit(pouchM);
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(CoreGameManager))]
    [HarmonyPatch("DestroyPlayers")]
    public class DestroyPlayersPatch
    {
        static void Postfix(CoreGameManager __instance)
        {
            __instance.gameObject.GetComponents<PouchManager>().Do(x => {
                UnityEngine.Object.Destroy(x);
            });
        }
    }

    /*public class ItemFitsPatch
    {
        static bool Prefix(Items item, ref bool __result)
        {

        }
    }*/

    [HarmonyPatch(typeof(ItemManager))]
    [HarmonyPatch("UseItem")]
    public class UseItemPatch
    {
        static FieldInfo audUse = AccessTools.DeclaredField(typeof(ITM_Acceptable), "audUse");
        static MethodInfo UpdateSelect = AccessTools.DeclaredMethod(typeof(ItemManager), "UpdateSelect");

        static bool Prefix(ItemManager __instance, PlayerManager ___pm, ItemObject[] ___items, int ___selectedItem)
        {
            RaycastHit hit;
            if (Physics.Raycast(___pm.transform.position, Singleton<CoreGameManager>.Instance.GetCamera(___pm.playerNumber).transform.forward, out hit, ___pm.pc.reach, ___pm.pc.ClickLayers))
            {
                //IItemAcceptor component = hit.transform.GetComponent<IItemAcceptor>();
                foreach (IItemAcceptor component in hit.transform.GetComponents<IItemAcceptor>())
                {
                    foreach (Pouch p in ___pm.GetPouchManager().Pouches)
                    {
                        for (int i = 0; i < p.actingItems.Length; i++)
                        {
                            if (component.ItemFits(p.actingItems[i]))
                            {
                                if (___items[___selectedItem].itemType == p.actingItems[i]) // if we are about to spend the item type we are holding(somehow), don't and just use the item instead
                                {
                                    return true;
                                }
                                else if (p.Spend(p.actingItems[i]))
                                {
                                    Item itmF = ObjectFinders.GetFirstInstance(p.actingItems[i]).item;
                                    if (itmF is ITM_Acceptable)
                                    {
                                        ITM_Acceptable itm = (ITM_Acceptable)itmF;
                                        Singleton<CoreGameManager>.Instance.audMan.PlaySingle((SoundObject)audUse.GetValue(itm));
                                    }
                                    UpdateSelect.Invoke(__instance, new object[] { });
                                    component.InsertItem(___pm, ___pm.ec);
                                    return false;
                                }
                            }
                        }
                    }
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(CoreGameManager))]
    [HarmonyPatch("BackupPlayers")]
    public class BackupPatch
    {
        static void Postfix(CoreGameManager __instance, PlayerManager[] ___players)
        {
            for (int i = 0; i < __instance.setPlayers; i++)
            {
                ___players[i].GetPouchManager().Backup();
            }
        }
    }

    [HarmonyPatch(typeof(CoreGameManager))]
    [HarmonyPatch("RestorePlayers")]
    public class RestorePatch
    {
        static void Postfix(CoreGameManager __instance, PlayerManager[] ___players)
        {
            for (int i = 0; i < __instance.setPlayers; i++)
            {
                ___players[i].GetPouchManager().ReloadBackup();
            }
        }
    }

    [HarmonyPatch(typeof(BaseGameManager))]
    [HarmonyPatch("BeginPlay")]
    public class BeginPlayPatch
    {
        static MethodInfo UpdateSelect = AccessTools.DeclaredMethod(typeof(ItemManager), "UpdateSelect");

        static void Prefix()
        {
            UpdateSelect.Invoke(Singleton<CoreGameManager>.Instance.GetPlayer(0).itm, null);
        }
    }

    [HarmonyPatch(typeof(ItemManager))]
    [HarmonyPatch("AddItem")]
    [HarmonyPatch(new Type[] { typeof(ItemObject) })]
    public class AddItemPatch
    {

        static MethodInfo UpdateSelect = AccessTools.DeclaredMethod(typeof(ItemManager), "UpdateSelect");

        public static bool Prefix(ItemManager __instance, ItemObject item, PlayerManager ___pm)
        {
            bool result = true;
            foreach (Pouch pouch in ___pm.GetPouchManager().Pouches)
            {
                if (pouch.itemConversionRates.TryGetValue(item.itemType, out double v))
                {
                    if (pouch.CanFit(item.itemType))
                    {
                        result = false;
                        pouch.amount += v;
                        UpdateSelect.Invoke(__instance, new object[] { });
                    }
                    else
                    {
                        ___pm.ec.CreateItem(___pm.ec.rooms.First(), item, ___pm.transform.position); //create an item at our position if we don't have room
                        return false;
                    }
                }
            }
            return result;
        }
    }

    [HarmonyPatch(typeof(ItemManager))]
    [HarmonyPatch("AddItem")]
    [HarmonyPatch(new Type[] { typeof(ItemObject), typeof(Pickup) })]
    public class AddItemPickupPatch
    {

        static MethodInfo UpdateSelect = AccessTools.DeclaredMethod(typeof(ItemManager), "UpdateSelect");

        static bool Prefix(ItemManager __instance, ItemObject item, Pickup pickup, PlayerManager ___pm)
        {
            bool result = true;
            foreach (Pouch pouch in ___pm.GetPouchManager().Pouches)
            {
                if (pouch.itemConversionRates.TryGetValue(item.itemType, out double v))
                {
                    if (pouch.CanFit(item.itemType))
                    {
                        result = false;
                        pouch.amount += v;
                        UpdateSelect.Invoke(__instance, new object[] { });
                    }
                    else
                    {
                        pickup.AssignItem(item); //give the pickup its item back because we dont have any room in our bag
                        return false;
                    }
                }
            }
            return result;
        }
    }

    [HarmonyPatch(typeof(HudManager))]
    [HarmonyPatch("SetItemSelect")]
    public class ItemSelectHudPatch
    {
        static void Postfix(TMP_Text ___itemTitle)
        {
            if (___itemTitle != null)
            {
                // IF THERE IS MORE THAN ONE PLAYER THIS CODE WILL BREAK
                PouchManager pouchM = Singleton<CoreGameManager>.Instance.GetPlayer(0).GetPouchManager();
                if (pouchM == null) return;
                foreach (Pouch p in pouchM.Pouches)
                {
                    ___itemTitle.text += "\n" + p.DisplayString();
                }
            }
        }
    }
}
