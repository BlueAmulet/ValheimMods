﻿using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace DiscardInventoryItem
{
    [BepInPlugin("aedenthorn.DiscardInventoryItem", "Discard Inventory Items", "0.1.1")]
    public class BepInExPlugin: BaseUnityPlugin
    {
        private static readonly bool isDebug = true;

        public static ConfigEntry<string> m_hotkey;
        public static ConfigEntry<bool> modEnabled;
        public static ConfigEntry<bool> returnResources;
        public static ConfigEntry<int> nexusID;

        public static void Dbgl(string str = "", bool pref = true)
        {
            if (isDebug)
                Debug.Log((pref ? typeof(BepInExPlugin).Namespace + " " : "") + str);
        }
        private void Awake()
        {
            m_hotkey = Config.Bind<string>("General", "DiscardHotkey", "delete", "The hotkey to discard an item");
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            returnResources = Config.Bind<bool>("General", "ReturnResources", true, "Enable this mod");
            nexusID = Config.Bind<int>("General", "NexusID", 45, "Nexus mod ID for updates");

            if (!modEnabled.Value)
                return;

            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        [HarmonyPatch(typeof(InventoryGui), "UpdateItemDrag")]
        static class UpdateItemDrag_Patch
        {
            static void Postfix(InventoryGui __instance, ItemDrop.ItemData ___m_dragItem, Inventory ___m_dragInventory, int ___m_dragAmount, ref GameObject ___m_dragGo)
            {
                if(Input.GetKeyDown(m_hotkey.Value) && ___m_dragItem != null && ___m_dragInventory.ContainsItem(___m_dragItem))
                {
                    Dbgl($"Discarding {___m_dragAmount}/{___m_dragItem.m_stack} {___m_dragItem.m_shared.m_name}");

                    if (returnResources.Value)
                    {
                        Recipe recipe = ObjectDB.instance.GetRecipe(___m_dragItem);

                        if (recipe != null)
                        {
                            for(int i = 0; i < ___m_dragAmount; i++)
                            {
                                foreach (Piece.Requirement req in recipe.m_resources)
                                {
                                    ItemDrop.ItemData newItem = req.m_resItem.m_itemData.Clone();
                                    newItem.m_stack = recipe.m_amount;
                                    if (!Player.m_localPlayer.GetInventory().AddItem(newItem))
                                    {
                                        ItemDrop itemDrop = ItemDrop.DropItem(newItem, newItem.m_stack, Player.m_localPlayer.transform.position + Player.m_localPlayer.transform.forward + Player.m_localPlayer.transform.up, Player.m_localPlayer.transform.rotation);
                                        itemDrop.GetComponent<Rigidbody>().velocity = (Player.m_localPlayer.transform.forward + Vector3.up) * 5f;
                                    }
                                }
                            }
                        }
                    }

                    if (___m_dragAmount == ___m_dragItem.m_stack)
                    {
                        ___m_dragInventory.RemoveItem(___m_dragItem);

                    }
                    else
                        ___m_dragInventory.RemoveItem(___m_dragItem, ___m_dragAmount);
                    Destroy(___m_dragGo);
                    ___m_dragGo = null; 
                    __instance.GetType().GetMethod("UpdateCraftingPanel", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(__instance, new object[] { false });
                }
            }
        }
    }
}
