using BepInEx;
using Bounce.Unmanaged;
using GameChat.UI;
using HarmonyLib;

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

namespace LordAshes
{
    public partial class RuleSet5EPlugin : BaseUnityPlugin
    {
        [HarmonyPatch(typeof(UIChatMessageManager), "AddChatMessage")]
        public static class PatchAddChatMessage
        {
            public static bool Prefix(ref string creatureName, Texture2D icon, ref string chatMessage, UIChatMessageManager.IChatFocusable focus = null)
            {
                Debug.Log("RuleSet 5E Plugin: Patch: Checking Message Content");
                if (chatMessage != null)
                {
                    chatMessage = chatMessage.Replace("(Whisper)", "").Trim();
                    if (chatMessage.StartsWith("[") && chatMessage.Contains("]"))
                    {
                        creatureName = chatMessage.Substring(0, chatMessage.IndexOf("]"));
                        creatureName = creatureName.Substring(1);
                        Debug.Log("RuleSet 5E Plugin: Patch: Speaker Changed To '" + creatureName + "'");
                        chatMessage = chatMessage.Substring(chatMessage.IndexOf("]") + 1);
                    }
                }
                return true;
            }
        }
    }

    /// <summary>
    /// Extension methods
    /// </summary>
    public static class SpeakExtensions
    {
        /// <summary>
        /// SpawnAt methods sets or clears the forced spawn position and orientation
        /// </summary>
        /// <param name="creature">Speaking creature</param>
        /// <param name="text">Content to be spoken</param>
        public static void SpeakEx(this Creature creature, string text)
        {
            if(LordAshes.RuleSet5EPlugin.rollingSystem != RuleSet5EPlugin.RollMode.manual_side)
            {
                creature.Speak(text);
            }
            else
            {
                RuleSet5EPlugin.Instance.StartCoroutine(RuleSet5EPlugin.Instance.DisplayMessage(RuleSet5EPlugin.Utility.GetCharacterName(creature)+": "+text,3f));
            }
        }

        /// <summary>
        /// Method to send, potentially different, chat messages to players, owner and GM
        /// </summary>
        /// <param name="chatManager">Insatnce of Chat Manager (not used since SendChatMessage is static</param>
        /// <param name="playersMessage">Message to be sent to all players (including owner and GM)</param>
        /// <param name="ownerMessage">Message to be sent to owner only</param>
        /// <param name="gmMessage">Message to be sent to GM only</param>
        /// <param name="speaker">Guid of the speaker</param>
        public static void SendChatMessageEx(this ChatManager chatManager, string playersMessage, string ownerMessage, string gmMessage, CreatureGuid subject, NGuid speaker)
        {
            List<string> gms = RuleSet5EPlugin.Utility.FindGMs();
            List<string> owners = RuleSet5EPlugin.Utility.FindOwners(subject);
            if (gmMessage!=null)
            {
                foreach (string gmName in gms)
                {
                    Debug.Log("RuleSet 5E Plugin: Chat Extension: Sending Chat Message To GM '" + gmName + "' Content: " + gmMessage.Replace("\r\n","|"));
                    ChatManager.SendChatMessage("/w " + gmName+ " " + gmMessage, speaker);
                }
            }
            if(ownerMessage!=null)
            {
                foreach (string ownerName in owners)
                {
                    if (!gms.Contains(ownerName))
                    {
                        Debug.Log("RuleSet 5E Plugin: Chat Extension: Sending Chat Message To Owner '" + ownerName + "' Content: " + gmMessage.Replace("\r\n", "|"));
                        ChatManager.SendChatMessage("/w " + ownerName + " " + ownerMessage, speaker);
                    }
                }
            }
            if (playersMessage != null)
            {
                foreach (PlayerGuid pid in CampaignSessionManager.PlayersInfo.Keys)
                {
                    string playerName = CampaignSessionManager.GetPlayerName(pid);
                    if (!gms.Contains(playerName) && !owners.Contains(playerName))
                    {
                        Debug.Log("RuleSet 5E Plugin: Chat Extension: Sending Chat Message To Player '" + CampaignSessionManager.GetPlayerName(pid) + "' Content: " + playersMessage.Replace("\r\n", "|"));
                        ChatManager.SendChatMessage("/w "+ CampaignSessionManager.GetPlayerName(pid) + " "+playersMessage, speaker);
                    }
                }
            }
        }
    }
}
