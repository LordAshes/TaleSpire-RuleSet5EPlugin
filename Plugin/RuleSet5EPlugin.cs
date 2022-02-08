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
        public const string Version = "1.7.0.0";

        // Reference to plugin instance
        public static RuleSet5EPlugin Instance = null;

        // User configurations
        private string iconSelector = "type";

        // Character dictionary
        private Dictionary<string, Character> characters = new Dictionary<string, Character>();

        // Last selected
        CreatureGuid lastSelectedMini = CreatureGuid.Empty;

        // Private variables
        private Texture reactionStopIcon = null;
        private bool reactionStopContinue = false;
        private int reactionRollTotal = 0;

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

            RadialUI.RadialUIPlugin.RemoveCustomButtonOnCharacter("Attacks");

            RadialUI.RadialSubmenu.EnsureMainMenuItem(RuleSet5EPlugin.Guid + ".Attacks", RadialUI.RadialSubmenu.MenuType.character, "Scripted Attacks", FileAccessPlugin.Image.LoadSprite("Attack.png"));
            RadialUI.RadialSubmenu.EnsureMainMenuItem(RuleSet5EPlugin.Guid + ".Saves", RadialUI.RadialSubmenu.MenuType.character, "Saves", FileAccessPlugin.Image.LoadSprite("Saves.png"));
            RadialUI.RadialSubmenu.EnsureMainMenuItem(RuleSet5EPlugin.Guid + ".Skills", RadialUI.RadialSubmenu.MenuType.character, "Skills", FileAccessPlugin.Image.LoadSprite("Skills.png"));
            RadialUI.RadialSubmenu.EnsureMainMenuItem(RuleSet5EPlugin.Guid + ".Healing", RadialUI.RadialSubmenu.MenuType.character, "Healing", FileAccessPlugin.Image.LoadSprite("Healing.png"));

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

                    foreach (Roll roll in characters[characterName].healing)
                    {
                        Debug.Log("RuleSet 5E Plugin: Adding Character '" + characterName + "' Healing '" + roll.name + "'");

                        RadialUI.RadialSubmenu.CreateSubMenuItem(
                                                                    RuleSet5EPlugin.Guid + ".Healing",
                                                                    roll.name,
                                                                    (FileAccessPlugin.File.Exists(roll.name + ".png") == true) ? FileAccessPlugin.Image.LoadSprite(roll.name + ".png") : FileAccessPlugin.Image.LoadSprite("Healing.png"),
                                                                    (cid, obj, mi) => Heal(roll, cid, obj, mi),
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

            reactionStopIcon = FileAccessPlugin.Image.LoadTexture("ReactionStop.png");

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
                if((LocalClient.SelectedCreatureId!=lastSelectedMini) && (LocalClient.SelectedCreatureId != CreatureGuid.Empty))
                {
                    Debug.Log("RuleSet 5E Plugin: New Mini ("+LocalClient.SelectedCreatureId+") Selected.");
                    lastSelectedMini = LocalClient.SelectedCreatureId;
                    CreatureBoardAsset asset;
                    CreaturePresenter.TryGetAsset(LocalClient.SelectedCreatureId, out asset);
                    if (asset != null)
                    {
                        Debug.Log("RuleSet 5E Plugin: New Valid Mini (" + LocalClient.SelectedCreatureId + ") Selected.");
                        if (characters.ContainsKey(Utility.GetCharacterName(asset.Creature)))
                        {
                            Debug.Log("RuleSet 5E Plugin: New Character Sheet Mini (" + LocalClient.SelectedCreatureId + ") Selected.");
                            Character character = characters[Utility.GetCharacterName(asset.Creature)];
                            Debug.Log("RuleSet 5E Plugin: Restoring "+ character._usingAttackBonus+"/"+ character._usingDamageBonus+"/"+ character._usingSkillBonus);
                            useAttackBonusDie = character._usingAttackBonus;
                            useDamageBonusDie = character._usingDamageBonus;
                            useSkillBonusDie = character._usingSkillBonus;
                            amountAttackBonusDie = character._usingAttackBonusAmount;
                            amountDamageBonusDie = character._usingDamageBonusAmount;
                            amountSkillBonusDie = character._usingSkillBonusAmonunt;
                        }
                    }
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

            if(reactionStopContinue)
            {
                GUIStyle gs2 = new GUIStyle();
                gs2.normal.textColor = Color.yellow;
                gs2.alignment = TextAnchor.UpperCenter;
                gs2.fontSize = 32;
                GUI.Label(new Rect((1920f / 2f) - 40f, 35, 80, 30), "Roll: " + reactionRollTotal, gs2);
                if (GUI.Button(new Rect((1920f / 2f) - 130f, 70, 40, 30), "Hit"))
                {
                    reactionStopContinue = false;
                    string message = "Forced Hit Reaction Used";
                    chatManager.SendChatMessageEx(message, message, message, instigator.Creature.CreatureId, LocalClient.Id.Value);
                    stateMachineState = StateMachineState.attackAttackHitReport;
                }
                if (GUI.Button(new Rect((1920f/2f)-85f,70,80,30),"Continue"))
                {
                    reactionStopContinue = false;
                    stateMachineState = StateMachineState.attackAttackDieRollReport;
                }
                if (GUI.Button(new Rect((1920f / 2f) + 5f, 70, 80, 30), "Cancel"))
                {
                    reactionStopContinue = false;
                    string message = "Cancel Attack Reaction Used";
                    chatManager.SendChatMessageEx(message, message, message, instigator.Creature.CreatureId, LocalClient.Id.Value);
                    stateMachineState = StateMachineState.attackRollCleanup;
                }
                if (GUI.Button(new Rect((1920f / 2f) + 90f, 70, 40, 30), "Miss"))
                {reactionStopContinue = false;
                    string message = "Forced Hit Reaction Used Miss Reaction Used";
                    chatManager.SendChatMessageEx(message, message, message, instigator.Creature.CreatureId, LocalClient.Id.Value);
                    stateMachineState = StateMachineState.attackAttackMissReport;
                }
            }

            if (Utility.isBoardLoaded())
            {
                if (PlayMode.CurrentStateId != PlayMode.Ids.Cutscene)
                {
                    RenderToolBarAddons();
                }
            }
        }

        public void Attack(Roll roll, CreatureGuid cid, object obj, MapMenuItem mi)
        {
            Debug.Log("RuleSet 5E Plugin: Attack: " + roll.name);
            lastRollRequest = new Roll(roll);
            Debug.Log("RuleSet 5E Plugin: Attack: " + lastRollRequest.name);
            Roll find = lastRollRequest;
            while (true)
            {
                Debug.Log("Damage Stack: " + find.name + " : " + find.type + " : " + find.roll);
                find = find.link;
                if (find == null) { break; }
            }
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

        public void Heal(Roll roll, CreatureGuid cid, object obj, MapMenuItem mi)
        {
            Debug.Log("RuleSet 5E Plugin: Heal: " + roll.name);
            lastRollRequest = roll;
            Debug.Log("Roll: " + roll.roll);
            CreaturePresenter.TryGetAsset(LocalClient.SelectedCreatureId, out instigator);
            CreaturePresenter.TryGetAsset(new CreatureGuid(RadialUI.RadialUIPlugin.GetLastRadialTargetCreature()), out victim);
            if (instigator != null && victim != null) { stateMachineState = StateMachineState.healingRollStart; }
        }

        private void RenderToolBarAddons()
        {
            reactionStop = GUI.Toggle(new Rect(1240, 5, 40, 20), reactionStop, reactionStopIcon);
            bool tempAdv = GUI.Toggle(new Rect(1280, 5, 30, 20), totalAdv, "+");
            bool tempDis = GUI.Toggle(new Rect(1310, 5, 30, 20), totalDis, "-");
            bool boolUseAttackBonusDie = GUI.Toggle(new Rect(1345, 5, 25, 20), useAttackBonusDie, "A");
            string strAmountAttackBonusDie = GUI.TextField(new Rect(1375, 5, 40, 20), amountAttackBonusDie, 6);
            bool boolUseDamageBonusDie = GUI.Toggle(new Rect(1420, 5, 25, 20), useDamageBonusDie, "D");
            string strAmountDamageBonusDie = GUI.TextField(new Rect(1450, 5, 40, 20), amountDamageBonusDie, 6);
            bool boolUseSkillBonusDie = GUI.Toggle(new Rect(1495, 5, 25, 20), useSkillBonusDie, "S");
            string strAmountSkillBonusDie = GUI.TextField(new Rect(1525, 5, 40, 20), amountSkillBonusDie, 6);
            int update = 0;
            if (tempDis != totalDis)
            {
                totalDis = tempDis;
                totalAdv = false;
                update = 1;
            }
            else if (tempAdv != totalAdv)
            {
                totalAdv = tempAdv;
                totalDis = false;
                update = 2;
            }
            if (useAttackBonusDie != boolUseAttackBonusDie) { useAttackBonusDie = boolUseAttackBonusDie; update = 3; }
            if (useDamageBonusDie != boolUseDamageBonusDie) { useDamageBonusDie = boolUseDamageBonusDie; update = 4; }
            if (useSkillBonusDie != boolUseSkillBonusDie) { useSkillBonusDie = boolUseSkillBonusDie; update = 5; }
            if (amountAttackBonusDie != strAmountAttackBonusDie) { amountAttackBonusDie = strAmountAttackBonusDie; update = 6; }
            if (amountDamageBonusDie != strAmountDamageBonusDie) { amountDamageBonusDie = strAmountDamageBonusDie; update = 7; }
            if (amountSkillBonusDie != strAmountSkillBonusDie) { amountSkillBonusDie = strAmountSkillBonusDie; update = 8; }
            if (update>0)
            {
                Debug.Log("RuleSet 5E Plugin: Toolbar Selection Changed ("+update+")");
                CreatureBoardAsset asset;
                CreaturePresenter.TryGetAsset(LocalClient.SelectedCreatureId, out asset);
                if(asset!=null)
                {
                    Debug.Log("RuleSet 5E Plugin: Valid Mini Selected For Update");
                    if (characters.ContainsKey(Utility.GetCharacterName(asset.Creature)))
                    {
                        Debug.Log("RuleSet 5E Plugin: Character Sheet Mini Selected For Update");
                        Character character = characters[Utility.GetCharacterName(asset.Creature)];
                        character._usingAttackBonus = useAttackBonusDie;
                        character._usingDamageBonus = useDamageBonusDie;
                        character._usingSkillBonus = useSkillBonusDie;
                        character._usingAttackBonusAmount = amountAttackBonusDie;
                        character._usingDamageBonusAmount = amountDamageBonusDie;
                        character._usingSkillBonusAmonunt = amountSkillBonusDie;
                        Debug.Log("RuleSet 5E Plugin: Settings Are Now "+characters[Utility.GetCharacterName(asset.Creature)]._usingAttackBonus+"/"+characters[Utility.GetCharacterName(asset.Creature)]._usingDamageBonus + "/" + characters[Utility.GetCharacterName(asset.Creature)]._usingSkillBonus);
                    }
                }
            }
            if (totalAdv) { lastRollRequestTotal = RollTotal.advantage; }
            else if (totalDis) { lastRollRequestTotal = RollTotal.disadvantage; }
            else { lastRollRequestTotal = RollTotal.normal; }
        }
    }
}
