using BepInEx;
using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;

namespace LordAshes
{
    public partial class RuleSet5EPlugin : BaseUnityPlugin
    {
        public static class Utility
        {
            private static bool postProcessingOn = true;

            public static void PostOnMainPage(System.Reflection.MemberInfo plugin)
            {
                SceneManager.sceneLoaded += (scene, mode) =>
                {
                    try
                    {
                        if (scene.name == "UI")
                        {
                            TextMeshProUGUI betaText = GetUITextByName("BETA");
                            if (betaText)
                            {
                                betaText.text = "INJECTED BUILD - unstable mods";
                            }
                        }
                        else
                        {
                            TextMeshProUGUI modListText = GetUITextByName("TextMeshPro Text");
                            if (modListText)
                            {
                                BepInPlugin bepInPlugin = (BepInPlugin)Attribute.GetCustomAttribute(plugin, typeof(BepInPlugin));
                                if (modListText.text.EndsWith("</size>"))
                                {
                                    modListText.text += "\n\nMods Currently Installed:\n";
                                }
                                modListText.text += "\nLord Ashes' " + bepInPlugin.Name + " - " + bepInPlugin.Version;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Log(ex);
                    }
                };
            }

            /// <summary>
            /// Function to check if the board is loaded
            /// </summary>
            /// <returns></returns>
            public static bool isBoardLoaded()
            {
                return CameraController.HasInstance && BoardSessionManager.HasInstance && !BoardSessionManager.IsLoading;
            }

            /// <summary>
            /// Method to properly evaluate shortcut keys. 
            /// </summary>
            /// <param name="check"></param>
            /// <returns></returns>
            public static bool StrictKeyCheck(KeyboardShortcut check)
            {
                if (!check.IsUp()) { return false; }
                foreach (KeyCode modifier in new KeyCode[] { KeyCode.LeftAlt, KeyCode.RightAlt, KeyCode.LeftControl, KeyCode.RightControl, KeyCode.LeftShift, KeyCode.RightShift })
                {
                    if (Input.GetKey(modifier) != check.Modifiers.Contains(modifier)) { return false; }
                }
                return true;
            }

            public static bool CharacterCheck(string characterName, string rollName)
            {
                CreatureBoardAsset asset = null;
                CreaturePresenter.TryGetAsset(LocalClient.SelectedCreatureId, out asset);
                if (asset == null) { return false; }
                return (GetCharacterName(asset) == characterName);
            }

            public static List<string> FindGMs()
            {
                List<string> names = new List<string>();
                foreach (PlayerGuid player in CampaignSessionManager.PlayersInfo.Keys)
                {
                    List<ClientGuid> list = new List<ClientGuid>();
                    if (BoardSessionManager.PlayersClientsGuids.TryGetValue(player, out list))
                    {
                        int count = list.Count;
                        for (int i = 0; i < count; i++)
                        {
                            ClientMode clientMode;
                            if (BoardSessionManager.ClientsModes.TryGetValue(list[i], out clientMode) && clientMode == ClientMode.GameMaster)
                            {
                                names.Add(CampaignSessionManager.GetPlayerName(player));
                            }
                        }
                    }
                }
                return (names.Count > 0) ? names : new List<string>() { "None" };
            }

            public static List<string> FindOwners(CreatureGuid cid)
            {
                List<string> owners = new List<string>();
                foreach (PlayerGuid player in CampaignSessionManager.PlayersInfo.Keys)
                {
                    if (CreatureManager.PlayerCanControlCreature(player, cid)) { owners.Add(CampaignSessionManager.GetPlayerName(player)); }
                }
                return owners;
            }

            private static TextMeshProUGUI GetUITextByName(string name)
            {
                TextMeshProUGUI[] texts = UnityEngine.Object.FindObjectsOfType<TextMeshProUGUI>();
                for (int i = 0; i < texts.Length; i++)
                {
                    if (texts[i].name == name)
                    {
                        return texts[i];
                    }
                }
                return null;
            }

            public static string GetCharacterName(CreatureBoardAsset creature)
            {
                string name = creature.Name;
                if (name.IndexOf("<") >= 0)
                {
                    name = name.Substring(0, name.IndexOf("<"));
                }
                return name;
            }

            /// <summary>
            /// Function for setting the post processing enabled setting
            /// </summary>
            /// <param name="setting">Boolean indicating if post processing is enabled or not</param>
            public static void DisableProcessing(bool setting)
            {
                var postProcessLayer = Camera.main.GetComponent<PostProcessLayer>();
                if (setting == true)
                {
                    postProcessingOn = GetPostProcessing();
                    postProcessLayer.enabled = false;
                }
                else
                {
                    postProcessLayer.enabled = postProcessingOn;
                }
            }

            /// <summary>
            /// Function for getting the post processing enabled status
            /// </summary>
            /// <returns>Returns a boolean indicating if post processing is enabled</returns>
            private static bool GetPostProcessing()
            {
                var postProcessLayer = Camera.main.GetComponent<PostProcessLayer>();
                return postProcessLayer.enabled;
            }
        }
    }
}
