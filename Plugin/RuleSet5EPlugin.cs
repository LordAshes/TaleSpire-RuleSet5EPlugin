using BepInEx;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Data;

namespace LordAshes
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency(RadialUI.RadialUIPlugin.Guid)]
    [BepInDependency(FileAccessPlugin.Guid)]
    public partial class RuleSet5EPlugin : BaseUnityPlugin
    {
        // Plugin info
        public const string Name = "RuleSet 5E Plug-In";
        public const string Guid = "org.lordashes.plugins.ruleset5e";
        public const string Version = "1.3.0.0";

        // Reference to plugin instance
        public static RuleSet5EPlugin Instance = null;

        // User configurations
        private string iconSelector = "type";

        // Character dictionary
        private Dictionary<string, Character> characters = new Dictionary<string, Character>();

        /// <summary>
        /// Function for initializing plugin
        /// This function is called once by TaleSpire
        /// </summary>
        void Awake()
        {
            UnityEngine.Debug.Log("RuleSet 5E Plugin: Active.");

            Instance = this;

            var harmony = new Harmony(Guid);
            harmony.PatchAll();

            // Read and apply configuration settings

            iconSelector = Config.Bind("Appearance", "Attack Icons Base On", "type").Value;
            string[] existence = Config.Bind("Appearance", "Dice Side Existance", "-100,0,0,45,0,0").Value.Split(',');
            diceSideExistance = new Existence(new Vector3(float.Parse(existence[0]), float.Parse(existence[1]), float.Parse(existence[2])), new Vector3(float.Parse(existence[3]), float.Parse(existence[4]), float.Parse(existence[5])));
            string[] colorCode = Config.Bind("Appearance", "Dice Color", "0,0,0").Value.Split(',');
            if (colorCode.Length == 3)
            {
                diceColor = new UnityEngine.Color(float.Parse(colorCode[0]), float.Parse(colorCode[1]), float.Parse(colorCode[2]));
            }
            else if (colorCode.Length > 3)
            {
                diceColor = new UnityEngine.Color(float.Parse(colorCode[0]), float.Parse(colorCode[1]), float.Parse(colorCode[2]), float.Parse(colorCode[3]));
            }
            colorCode = Config.Bind("Appearance", "Dice Highlight Color", "1.0,1.0,0").Value.Split(',');
            if (colorCode.Length == 3)
            {
                diceHighlightColor = new UnityEngine.Color32((byte)(255 * float.Parse(colorCode[0])), (byte)(255 * float.Parse(colorCode[1])), (byte)(255 * float.Parse(colorCode[2])), 255);
            }
            else if (colorCode.Length > 3)
            {
                diceHighlightColor = new UnityEngine.Color32((byte)(255 * float.Parse(colorCode[0])), (byte)(255 * float.Parse(colorCode[1])), (byte)(255 * float.Parse(colorCode[2])), (byte)(255 * float.Parse(colorCode[3])));
            }

            missAnimation = Config.Bind("Appearance", "Miss Animation Name", "TLA_Wiggle").Value;
            deadAnimation = Config.Bind("Appearance", "Dead Animation Name", "TLA_Action_Knockdown").Value;

            rollingSystem = Config.Bind("Settings", "Rolling Style", RollMode.automaticDice).Value;
            processSpeed = (Config.Bind("Settings", "Process Delay Percentage", 100).Value / 100);

            Debug.Log("RuleSet 5E Plugin: Dice Side Location = " + diceSideExistance.position);

            Debug.Log("RuleSet 5E Plugin: Speed = " + processSpeed + "x");

            RadialUI.RadialUIPlugin.RemoveOnCharacter("Attacks");

            RadialUI.RadialSubmenu.EnsureMainMenuItem(RuleSet5EPlugin.Guid + ".Attacks", RadialUI.RadialSubmenu.MenuType.character, "Scripted Attacks", FileAccessPlugin.Image.LoadSprite("Attack.png"));
            RadialUI.RadialSubmenu.EnsureMainMenuItem(RuleSet5EPlugin.Guid + ".Saves", RadialUI.RadialSubmenu.MenuType.character, "Saves", FileAccessPlugin.Image.LoadSprite("Saves.png"));
            RadialUI.RadialSubmenu.EnsureMainMenuItem(RuleSet5EPlugin.Guid + ".Skills", RadialUI.RadialSubmenu.MenuType.character, "Skills", FileAccessPlugin.Image.LoadSprite("Skills.png"));

            foreach (string item in FileAccessPlugin.File.Find(".Dnd5e"))
            {
                string characterName = System.IO.Path.GetFileNameWithoutExtension(item);
                if (!characters.ContainsKey(characterName))
                {
                    Debug.Log("RuleSet 5E Plugin: Loading Character '" + characterName + "' From '"+item+"'");
                    characters.Add(characterName, JsonConvert.DeserializeObject<Character>(FileAccessPlugin.File.ReadAllText(item)));

                    foreach (Roll roll in characters[characterName].attacks)
                    {
                        Debug.Log("RuleSet 5E Plugin: Adding Character '" + characterName + "' Attack '" + roll.name + "'");

                        RadialUI.RadialSubmenu.CreateSubMenuItem(
                                                                    RuleSet5EPlugin.Guid + ".Attacks",
                                                                    roll.name,
                                                                    FileAccessPlugin.Image.LoadSprite(PatchAssistant.GetField(roll,iconSelector) + ".png"),
                                                                    (cid, obj, mi) => Attack(roll, cid, obj, mi),
                                                                    true,
                                                                    () => { return Utility.CharacterCheck(characterName, roll.name); }
                                                                );
                    }

                    foreach (Roll roll in characters[characterName].saves)
                    {
                        Debug.Log("RuleSet 5E Plugin: Adding Character '" + characterName + "' Save '" + roll.name + "'");

                        RadialUI.RadialSubmenu.CreateSubMenuItem(
                                                                    RuleSet5EPlugin.Guid + ".Saves",
                                                                    roll.name,
                                                                    (FileAccessPlugin.File.Exists(roll.name + ".png")==true) ? FileAccessPlugin.Image.LoadSprite(roll.name + ".png") : FileAccessPlugin.Image.LoadSprite("Saves.png"),
                                                                    (cid, obj, mi) => Save(roll, cid, obj, mi),
                                                                    true,
                                                                    () => { return Utility.CharacterCheck(characterName, roll.name); }
                                                                );
                    }

                    foreach (Roll roll in characters[characterName].skills)
                    {
                        Debug.Log("RuleSet 5E Plugin: Adding Character '" + characterName + "' Skill '" + roll.name + "'");

                        RadialUI.RadialSubmenu.CreateSubMenuItem(
                                                                    RuleSet5EPlugin.Guid + ".Skills",
                                                                    roll.name,
                                                                    (FileAccessPlugin.File.Exists(roll.name + ".png") == true) ? FileAccessPlugin.Image.LoadSprite(roll.name + ".png") : FileAccessPlugin.Image.LoadSprite("Skills.png"),
                                                                    (cid, obj, mi) => Skill(roll, cid, obj, mi),
                                                                    true,
                                                                    () => { return Utility.CharacterCheck(characterName, roll.name); }
                                                                );
                    }
                }
                else
                {
                    Debug.LogWarning("RuleSet 5E Plugin: Character '" + characterName + "' Already Added.");
                }
            }

            Utility.PostOnMainPage(this.GetType());
        }

        /// <summary>
        /// Function for determining if view mode has been toggled and, if so, activating or deactivating Character View mode.
        /// This function is called periodically by TaleSpire.
        /// </summary>
        void Update()
        {
            if (Utility.isBoardLoaded())
            {
                if(callbackRollReady==null)
                {
                    callbackRollReady = NewDiceSet;
                    callbackRollResult = ResultDiceSet;
                    chatManager = GameObject.FindObjectOfType<ChatManager>();
                    StartCoroutine((IEnumerator)Executor());
                }
            }
        }

        void OnGUI()
        {
            if (messageContent != "")
            {
                GUIStyle gs1 = new GUIStyle();
                gs1.normal.textColor = Color.black;
                gs1.alignment = TextAnchor.UpperCenter;
                gs1.fontSize = 32;
                GUIStyle gs2 = new GUIStyle();
                gs2.normal.textColor = Color.yellow;
                gs2.alignment = TextAnchor.UpperCenter;
                gs2.fontSize = 32;
                GUI.Label(new Rect(0f, 40f, 1920, 30), messageContent, gs1);
                GUI.Label(new Rect(3f, 43f, 1920, 30), messageContent, gs2);
            }
            if (Utility.isBoardLoaded())
            {
                RenderDisNormAdvSelector();
            }
        }

        public void Attack(Roll roll, CreatureGuid cid, object obj, MapMenuItem mi)
        {
            Debug.Log("RuleSet 5E Plugin: Attack: " + roll.name);
            lastRollRequest = roll;
            CreaturePresenter.TryGetAsset(LocalClient.SelectedCreatureId, out instigator);
            CreaturePresenter.TryGetAsset(new CreatureGuid(RadialUI.RadialUIPlugin.GetLastRadialTargetCreature()), out victim);
            if(instigator!=null && victim != null) { stateMachineState = StateMachineState.attackAttackRangeCheck; }
        }

        public void Skill(Roll roll, CreatureGuid cid, object obj, MapMenuItem mi)
        {
            Debug.Log("RuleSet 5E Plugin: Save: " + roll.name);
            lastRollRequest = roll;
            Debug.Log("Roll: " + roll.roll);
            CreaturePresenter.TryGetAsset(LocalClient.SelectedCreatureId, out instigator);
            if (instigator != null) { stateMachineState = StateMachineState.skillRollSetup; }
        }

        public void Save(Roll roll, CreatureGuid cid, object obj, MapMenuItem mi)
        {
            Debug.Log("RuleSet 5E Plugin: Skill: " + roll.name);
            lastRollRequest = roll;
            Debug.Log("Roll: " + roll.roll);
            CreaturePresenter.TryGetAsset(LocalClient.SelectedCreatureId, out instigator);
            if (instigator != null) { stateMachineState = StateMachineState.skillRollSetup; }
        }

        private void RenderDisNormAdvSelector()
        {
            bool tempDis = GUI.Toggle(new Rect(1300, 5, 60, 20), totalDis, "Dis");
            bool tempNorm = GUI.Toggle(new Rect(1365, 5, 60, 20), totalNorm, "Norm");
            bool tempAdv = GUI.Toggle(new Rect(1430, 5, 60, 20), totalAdv, "Adv");
            if (tempDis != totalDis)
            {
                if (tempDis == true)
                {
                    totalDis = true;
                    totalNorm = false;
                    totalAdv = false;
                    lastRollRequestTotal = RollTotal.disadvantage;
                }
                else
                {
                    totalDis = false;
                    totalNorm = true;
                    totalAdv = false;
                    lastRollRequestTotal = RollTotal.normal;
                }
            }
            else if (tempNorm != totalNorm)
            {
                totalDis = false;
                totalNorm = true;
                totalAdv = false;
                lastRollRequestTotal = RollTotal.normal;
            }
            else if (tempAdv != totalAdv)
            {
                if (tempAdv == true)
                {
                    totalDis = false;
                    totalNorm = false;
                    totalAdv = true;
                    lastRollRequestTotal = RollTotal.advantage;
                }
                else
                {
                    totalDis = false;
                    totalNorm = true;
                    totalAdv = false;
                    lastRollRequestTotal = RollTotal.normal;
                }
            }
        }
    }
}
