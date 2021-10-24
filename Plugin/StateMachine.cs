using BepInEx;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LordAshes
{
    public partial class RuleSet5EPlugin : BaseUnityPlugin
    {
        // Variables to track previous and current state in the state machine
        public static RollMode rollingSystem = RollMode.automaticDice;
        public static StateMachineState stateMachineState = StateMachineState.idle;
        public static StateMachineState stateMachineLastState = StateMachineState.idle;

        // Rolling related variables
        private Roll lastRollRequest = null;
        private RollTotal lastRollRequestTotal = RollTotal.normal;
        private Roll loadedRollRequest = null;
        private int lastRollId = -2;
        private Dictionary<string, object> lastResult = null;
        private float damageMultiplier = 1.0f;

        // Sequence actors
        private CreatureBoardAsset instigator = null;
        private CreatureBoardAsset victim = null;

        // Animation names // Valid Names are: "TLA_Twirl", "TLA_Action_Knockdown", "TLA_Wiggle", "TLA_MeleeAttack", "TLA_Surprise", "TLA_MagicMissileAttack"
        private string missAnimation = "TLA_Wiggle";
        private string deadAnimation = "TLA_Action_Knockdown";

        // Misc variables
        private Existence saveCamera = null;
        private string messageContent = "";
        private ChatManager chatManager = null;
        private bool totalNorm = true;
        private bool totalAdv = false;
        private bool totalDis = false;
        public static float processSpeed = 1.0f;
        private Existence diceSideExistance = null;

        public enum RollTotal
        {
            normal = 0,
            advantage = 1,
            disadvantage = 2
        }

        public enum RollMode
        {
            manual = 0,
            manual_side = 1,
            automaticDice = 2,
            automaticGenerator = 3
        }

        public enum StateMachineState
        {
            idle = 0,
            // Attack Sequence
            attackAttackRangeCheck,
            attackAttackIntention,
            attackRollSetup,
            attackAttackDieCreate,
            attackAttackDieWaitCreate,
            attackAttackDieRollExecute,
            attackAttackDieWaitRoll,
            attackAttackDieRollReport,
            attackAttackDefenceCheck,
            attackAttackMissReport,
            attackAttackHitReport,
            attackDamageDieCreate,
            attackDamageDieWaitCreate,
            attackDamageDieRollExecute,
            attackDamageDieWaitRoll,
            attackDamageDieRollReport,
            attackDamageDieDamageReport,
            attackDamageDieDamageTake,
            attackRollCleanup,
            // Saves Roll
            skillRollSetup,
            skillRollDieCreate,
            skillRollDieWaitCreate,
            skillRollDieRollExecute,
            skillRollDieWaitRoll,
            skillRollDieRollReport,
            skillRollCleanup,
        }

        private IEnumerator Executor()
        {
            DiceManager dm = GameObject.FindObjectOfType<DiceManager>();
            UIDiceTray dt = GameObject.FindObjectOfType<UIDiceTray>();
            List<Damage> damages = new List<Damage>();
            Roll tmp = null;
            string players = "";
            string owner = "";
            string gm = "";
            while (true)
            {
                if (stateMachineState != stateMachineLastState) { Debug.Log("RuleSet 5E Plugin: State = " + stateMachineState); stateMachineLastState = stateMachineState; }
                float stepDelay = 0.100f;
                players = "";
                owner = "";
                gm = "";
                switch (stateMachineState)
                {
                    // *******************
                    // * Attack Sequence *
                    // *******************
                    case StateMachineState.attackAttackRangeCheck:
                        stateMachineState = StateMachineState.attackAttackIntention;
                        float dist = (5.0f * Vector3.Distance(instigator.transform.position, victim.transform.position));
                        Debug.Log("RuleSet 5E Plugin: Attack: Range=" + dist);
                        foreach (DistanceUnit unit in CampaignSessionManager.DistanceUnits)
                        {
                            Debug.Log(unit.Name + " : " + unit.NumberPerTile);
                        }
                        if (lastRollRequest.type.ToUpper() == "MELEE")
                        {
                            if (dist >= 7.0f)
                            {
                                StartCoroutine(DisplayMessage(Utility.GetCharacterName(instigator.Creature) + " is out of range of " + Utility.GetCharacterName(victim.Creature) + " for a melee attack.", 1.0f));
                                stateMachineState = StateMachineState.idle;
                            }
                        }
                        if ((lastRollRequest.type.ToUpper() == "RANGE") || (lastRollRequest.type.ToUpper() == "RANGED"))
                        {
                            if (dist < 7.0f)
                            {
                                StartCoroutine(DisplayMessage(Utility.GetCharacterName(instigator.Creature) + " is in melee with " + Utility.GetCharacterName(victim.Creature) + ". Disadvantage on ranged attacks.", 1.0f));
                                stateMachineState = StateMachineState.idle;
                            }
                        }
                        break;
                    case StateMachineState.attackAttackIntention:
                        stateMachineState = StateMachineState.attackRollSetup;
                        instigator.Creature.SpeakEx("Attack!");
                        players = "[" + RuleSet5EPlugin.Utility.GetCharacterName(instigator.Creature) + " targets " + RuleSet5EPlugin.Utility.GetCharacterName(victim.Creature) + "]<size=4>\r\n";
                        owner = players;
                        gm = players;
                        chatManager.SendChatMessageEx(players, owner, gm, instigator.Creature.CreatureId, LocalClient.Id.Value);
                        for (int r = 0; r < 10; r++)
                        {
                            instigator.RotateTowards(victim.transform.position);
                            yield return new WaitForSeconds(0.100f * processSpeed);
                        }
                        break;
                    case StateMachineState.attackRollSetup:
                        stateMachineState = StateMachineState.attackAttackDieCreate;
                        RollSetup(dm, ref stepDelay);
                        if (rollingSystem == RollMode.automaticDice) { GameObject.Find("dolly").transform.position = new Vector3(-100f, 2f, -1.5f); }
                        damageMultiplier = 1.0f;
                        break;
                    case StateMachineState.attackAttackDieCreate:
                        stateMachineState = StateMachineState.attackAttackDieWaitCreate;
                        RollCreate(dt, $"talespire://dice/" + SafeForProtocolName(lastRollRequest.name) + ":" + lastRollRequest.roll, ref stepDelay);
                        if (rollingSystem.ToString().ToUpper().Contains("MANUAL")) { StartCoroutine(DisplayMessage("Please Roll Provided Die Or Dice To Continue...", 3f)); }
                        break;
                    case StateMachineState.attackAttackDieWaitCreate:
                        // Callback propagates to next phase
                        break;
                    case StateMachineState.attackAttackDieRollExecute:
                        stateMachineState = StateMachineState.attackAttackDieWaitRoll;
                        RollExecute(dm, ref stepDelay);
                        break;
                    case StateMachineState.attackAttackDieWaitRoll:
                        // Callback propagates to next phase
                        break;
                    case StateMachineState.attackAttackDieRollReport:
                        stateMachineState = StateMachineState.attackAttackDefenceCheck;
                        dm.ClearAllDice(lastRollId);
                        if ((bool)lastResult["IsMax"] == true)
                        {
                            instigator.Creature.SpeakEx(lastRollRequest.name + " " + lastResult["Total"] + " (Critical Hit)");
                        }
                        else if ((bool)lastResult["IsMin"] == true)
                        {
                            instigator.Creature.SpeakEx(lastRollRequest.name + " " + lastResult["Total"] + " (Critical Miss)");
                        }
                        else
                        {
                            instigator.Creature.SpeakEx(lastRollRequest.name + " " + lastResult["Total"]);
                        }
                        players = "[" + RuleSet5EPlugin.Utility.GetCharacterName(instigator.Creature) + "]";
                        players = players + "<size=32>" + lastRollRequest.name + " " + lastResult["Total"] + "\r\n";
                        owner = players;
                        owner = owner + "<size=16>" + lastResult["Roll"] + " = ";
                        owner = owner + "<size=16>" + lastResult["Expanded"];
                        if ((bool)lastResult["IsMax"] == true)
                        {
                            owner = owner + " (Critical Hit)";
                        }
                        else if ((bool)lastResult["IsMin"] == true)
                        {
                            owner = owner + " (Critical Miss)";
                        }
                        gm = owner;
                        chatManager.SendChatMessageEx(players, owner, gm, victim.Creature.CreatureId, new Bounce.Unmanaged.NGuid(LocalClient.Id.ToString()));
                        stepDelay = 1.0f;
                        break;
                    case StateMachineState.attackAttackDefenceCheck:
                        Debug.Log("Getting Total from '" + lastResult["Total"] + "'");
                        int attack = (int)lastResult["Total"];
                        int ac = (int)(victim.Creature.Stat1.Value);
                        Debug.Log("Getting Min from '" + lastResult["IsMin"] + "'");
                        if ((attack < ac) || ((bool)lastResult["IsMin"] == true))
                        {
                            stateMachineState = StateMachineState.attackAttackMissReport;
                            victim.Creature.StartTargetEmote(instigator.Creature, missAnimation);
                        }
                        else
                        {
                            stateMachineState = StateMachineState.attackAttackHitReport;
                            if (lastRollRequest.info != "")
                            {
                                instigator.Creature.StartTargetEmote(victim.Creature, lastRollRequest.info);
                            }
                            else
                            {
                                switch (lastRollRequest.type.ToUpper())
                                {
                                    case "MAGIC":
                                        instigator.Creature.StartTargetEmote(victim.Creature, "TLA_MagicMissileAttack");
                                        break;
                                    case "RANGE":
                                    case "RANGED":
                                        instigator.Creature.StartTargetEmote(victim.Creature, "TLA_MagicMissileAttack");
                                        break;
                                    default:
                                        instigator.Creature.StartTargetEmote(victim.Creature, "TLA_MeleeAttack");
                                        break;
                                }
                            }
                            instigator.Creature.Attack(victim.Creature.CreatureId, victim.transform.position);
                        }
                        stepDelay = 0f;
                        break;
                    case StateMachineState.attackAttackMissReport:
                        stateMachineState = StateMachineState.attackRollCleanup;
                        victim.Creature.SpeakEx("Miss!");
                        players = "[" + RuleSet5EPlugin.Utility.GetCharacterName(victim.Creature) + " Evades]<size=4>";
                        owner = players;
                        gm = players + "<size=16>" + lastResult["Total"] + " vs AC" + victim.Creature.Stat1.Value;
                        chatManager.SendChatMessageEx(players, owner, gm, victim.Creature.CreatureId, LocalClient.Id.Value);
                        break;
                    case StateMachineState.attackAttackHitReport:
                        stateMachineState = StateMachineState.attackDamageDieCreate;
                        victim.Creature.SpeakEx("Hit!");
                        players = "[" + RuleSet5EPlugin.Utility.GetCharacterName(victim.Creature) + " Is Hit]<size=4>";
                        owner = players;
                        gm = players + "<size=16>" + lastResult["Total"] + " vs AC" + victim.Creature.Stat1.Value;
                        chatManager.SendChatMessageEx(players, owner, gm, victim.Creature.CreatureId, LocalClient.Id.Value);
                        tmp = lastRollRequest.link;
                        damages.Clear();
                        if ((bool)lastResult["IsMax"] == true) { damageMultiplier = 2.0f; } else { damageMultiplier = 1.0f; }
                        stepDelay = 1f;
                        break;
                    case StateMachineState.attackDamageDieCreate:
                        if (tmp != null)
                        {
                            lastRollRequest = tmp;
                            if (rollingSystem == RollMode.automaticDice)
                            {
                                if (int.Parse(tmp.roll.Substring(0, tmp.roll.IndexOf("D"))) > 3)
                                {
                                    GameObject dolly = GameObject.Find("dolly");
                                    Debug.Log("RuleSet 5E Plugin: Adjusting Dolly And Camera For Large Dice Count");
                                    dolly.transform.position = new Vector3(-100f, 4f, -3f);
                                }
                                else
                                {
                                    GameObject dolly = GameObject.Find("dolly");
                                    Debug.Log("RuleSet 5E Plugin: Adjusting Dolly And Camera For Small Dice Count");
                                    dolly.transform.position = new Vector3(-100f, 2f, -1.5f);
                                }
                            }
                            stateMachineState = StateMachineState.attackDamageDieWaitCreate;
                            RollCreate(dt, $"talespire://dice/" + SafeForProtocolName(tmp.name) + ":" + tmp.roll, ref stepDelay);
                        }
                        else
                        {
                            stateMachineState = StateMachineState.attackDamageDieDamageReport;
                        }
                        break;
                    case StateMachineState.attackDamageDieWaitCreate:
                        // Callback propagates to next phase
                        break;
                    case StateMachineState.attackDamageDieRollExecute:
                        stateMachineState = StateMachineState.attackDamageDieWaitRoll;
                        dt.SpawnAt(Vector3.zero, Vector3.zero);
                        RollExecute(dm, ref stepDelay);
                        if (rollingSystem.ToString().ToUpper().Contains("MANUAL")) { StartCoroutine(DisplayMessage("Please Roll Provided Die Or Dice To Continue...", 3f)); }
                        break;
                    case StateMachineState.attackDamageDieWaitRoll:
                        // Callback propagates to next phase
                        break;
                    case StateMachineState.attackDamageDieRollReport:
                        dm.ClearAllDice(lastRollId);
                        stateMachineState = StateMachineState.attackDamageDieCreate;
                        instigator.Creature.SpeakEx(lastRollRequest.name + ":\r\n" + lastResult["Total"] + " " + lastRollRequest.type);
                        damages.Add(new Damage(lastRollRequest.name, lastRollRequest.type, lastRollRequest.roll, lastResult["Expanded"].ToString(), (int)lastResult["Total"]));
                        stepDelay = 1.0f;
                        tmp = tmp.link;
                        break;
                    case StateMachineState.attackDamageDieDamageReport:
                        stateMachineState = StateMachineState.attackDamageDieDamageTake;
                        int total = 0;
                        string info = "";
                        foreach (Damage dmg in damages)
                        {
                            total = total + dmg.total;
                            info = info + dmg.total + " " + dmg.type + " (" + dmg.name + ") " + dmg.roll + " = " + dmg.expansion + "\r\n";
                        }
                        if (damages.Count > 1)
                        {
                            yield return new WaitForSeconds(0.5f * processSpeed);
                            instigator.Creature.SpeakEx("Total Damage: " + total);
                        }
                        players = "[" + Utility.GetCharacterName(instigator.Creature) + "]<size=32>Damage " + total + "<size=16>" + (((bool)lastResult["IsMax"] == true) ? " (Critical Hit)" : "");
                        owner = players + "\r\n" + info;
                        gm = owner;
                        chatManager.SendChatMessageEx(players, owner, gm, instigator.Creature.CreatureId, LocalClient.Id.Value);
                        break;
                    case StateMachineState.attackDamageDieDamageTake:
                        stateMachineState = StateMachineState.attackRollCleanup;
                        bool fullDamage = true;
                        int adjustedDamage = 0;
                        string damageList = "";
                        if (characters.ContainsKey(RuleSet5EPlugin.Utility.GetCharacterName(victim.Creature)))
                        {
                            foreach (Damage dmg in damages)
                            {
                                foreach (string immunity in characters[RuleSet5EPlugin.Utility.GetCharacterName(victim.Creature)].immunity)
                                {
                                    if (dmg.type == immunity) { dmg.total = 0; dmg.type = dmg.type + ":Immunity"; fullDamage = false; }
                                }
                                foreach (string resisitance in characters[RuleSet5EPlugin.Utility.GetCharacterName(victim.Creature)].resistance)
                                {
                                    if (dmg.type == resisitance) { dmg.total = (int)(dmg.total / 2); dmg.type = dmg.type + ":Resistance"; fullDamage = false; }
                                }
                                adjustedDamage = adjustedDamage + dmg.total;
                                damageList = damageList + dmg.total + " " + dmg.type + " (" + dmg.name + ") " + dmg.roll + " = " + dmg.expansion + "\r\n";
                            }
                        }
                        int hp = Math.Max((int)(victim.Creature.Hp.Value - adjustedDamage), 0);
                        int hpMax = (int)victim.Creature.Hp.Max;
                        CreatureManager.SetCreatureStatByIndex(victim.Creature.CreatureId, new CreatureStat(hp, hpMax), -1);
                        damageList = "<size=32>Damage: " + adjustedDamage + "<size=16>\r\n" + damageList;
                        if (adjustedDamage == 0)
                        {
                            victim.Creature.SpeakEx("Your attempts are futile!");
                            players = "[" + RuleSet5EPlugin.Utility.GetCharacterName(victim.Creature) + " takes no damage]<size=4>\r\n";
                            owner = players;
                            gm = players + "<size=16>" + damageList;
                        }
                        else if (!fullDamage)
                        {
                            if (hp > 0)
                            {
                                victim.Creature.SpeakEx("I resist your efforts!");
                            }
                            else
                            {
                                victim.Creature.SpeakEx("I resist your efforts\r\nbut I am slain!");
                                if (deadAnimation.ToUpper() != "REMOVE")
                                {
                                    Debug.Log("RuleSet 5E Plugin: Playing Death Animation '" + deadAnimation + "'");
                                    victim.Creature.StartTargetEmote(instigator.Creature, deadAnimation);
                                }
                                else
                                {
                                    yield return new WaitForSeconds(1f);
                                    Debug.Log("RuleSet 5E Plugin: Requesting Mini Remove");
                                    victim.RequestDelete();
                                }
                            }
                            players = "[" + RuleSet5EPlugin.Utility.GetCharacterName(victim.Creature) + " takes some damage]<size=4>\r\n";
                            owner = players;
                            gm = players + "<size=16>" + damageList;
                        }
                        else
                        {
                            if (hp > 0)
                            {
                                victim.Creature.SpeakEx("Ouch!");
                            }
                            else
                            {
                                victim.Creature.SpeakEx("I am slain!");
                                if (deadAnimation.ToUpper() != "REMOVE")
                                {
                                    Debug.Log("RuleSet 5E Plugin: Playing Death Animation '" + deadAnimation + "'");
                                    victim.Creature.StartTargetEmote(instigator.Creature, deadAnimation);
                                }
                                else
                                {
                                    yield return new WaitForSeconds(1f);
                                    Debug.Log("RuleSet 5E Plugin: Requesting Mini Remove");
                                    victim.RequestDelete();
                                }
                            }
                            players = "[" + RuleSet5EPlugin.Utility.GetCharacterName(victim.Creature) + " takes the damage]<size=4>\r\n";
                            owner = players;
                            gm = players + "<size=16>" + damageList;
                        }
                        gm = gm + "\r\nRemaining HP: " + hp + " of " + hpMax;
                        CreatureManager.SetCreatureStatByIndex(victim.Creature.CreatureId, new CreatureStat(hp, hpMax), -1);
                        chatManager.SendChatMessageEx(players, owner, gm, victim.Creature.CreatureId, LocalClient.Id.Value);
                        break;
                    case StateMachineState.attackRollCleanup:
                        stateMachineState = StateMachineState.idle;
                        RollCleanup(dm, ref stepDelay);
                        break;
                    // ******************
                    // * Skill Sequence *
                    // *****************
                    case StateMachineState.skillRollSetup:
                        stateMachineState = StateMachineState.skillRollDieCreate;
                        RollSetup(dm, ref stepDelay);
                        if (rollingSystem == RollMode.automaticDice) { GameObject.Find("dolly").transform.position = new Vector3(-100f, 2f, -1.5f); }
                        damageMultiplier = 1.0f;
                        break;
                    case StateMachineState.skillRollDieCreate:
                        stateMachineState = StateMachineState.skillRollDieWaitCreate;
                        RollCreate(dt, $"talespire://dice/" + SafeForProtocolName(lastRollRequest.name) + ":" + lastRollRequest.roll, ref stepDelay);
                        if (rollingSystem.ToString().ToUpper().Contains("MANUAL")) { StartCoroutine(DisplayMessage("Please Roll Provided Die Or Dice To Continue...", 3f)); }
                        break;
                    case StateMachineState.skillRollDieWaitCreate:
                        // Callback propagates to next phase
                        break;
                    case StateMachineState.skillRollDieRollExecute:
                        stateMachineState = StateMachineState.skillRollDieWaitRoll;
                        RollExecute(dm, ref stepDelay);
                        break;
                    case StateMachineState.skillRollDieWaitRoll:
                        // Callback propagates to next phase
                        break;
                    case StateMachineState.skillRollDieRollReport:
                        stateMachineState = StateMachineState.skillRollCleanup;
                        dm.ClearAllDice(lastRollId);
                        players = "[" + RuleSet5EPlugin.Utility.GetCharacterName(instigator.Creature) + "]<size=32>" + lastRollRequest.name + " " + lastResult["Total"] + "\r\n";
                        owner = "[" + RuleSet5EPlugin.Utility.GetCharacterName(instigator.Creature) + "]<size=32>" + lastRollRequest.name + " " + lastResult["Total"] + "\r\n"; ;
                        owner = owner + "<size=16>" + lastResult["Roll"] + " = ";
                        owner = owner + "<size=16>" + lastResult["Expanded"];
                        gm = owner;
                        if ((bool)lastResult["IsMax"] == true)
                        {
                            owner = owner + " (Max)";
                        }
                        else if ((bool)lastResult["IsMin"] == true)
                        {
                            owner = owner + " (Min)";
                        }
                        if (lastRollRequest.type.ToUpper().Contains("SECRET"))
                        {
                            players = null;
                        }
                        else if (lastRollRequest.type.ToUpper().Contains("PRIVATE"))
                        {
                            instigator.Creature.SpeakEx(lastRollRequest.name);
                            players = "[" + RuleSet5EPlugin.Utility.GetCharacterName(instigator.Creature) + "]<size=32>" + lastRollRequest.name + "\r\n";
                        }
                        else // if (lastRollRequest.type.ToUpper().Contains("PUBLIC"))
                        {
                            instigator.Creature.SpeakEx(lastRollRequest.name + " " + lastResult["Total"]);
                        }
                        Debug.Log("RuleSet 5E Plugin: Checking For GM Modifier");
                        if (lastRollRequest.type.ToUpper().Contains("GM"))
                        {
                            players = null;
                            owner = null;
                        }
                        Debug.Log("RuleSet 5E Plugin: Speaking");
                        chatManager.SendChatMessageEx(players, owner, gm, instigator.Creature.CreatureId, new Bounce.Unmanaged.NGuid(LocalClient.Id.ToString()));
                        stepDelay = 1.0f;
                        break;
                    case StateMachineState.skillRollCleanup:
                        stateMachineState = StateMachineState.idle;
                        RollCleanup(dm, ref stepDelay);
                        break;
                }
                yield return new WaitForSeconds(stepDelay * processSpeed);
            }
        }

        public void RollSetup(DiceManager dm, ref float stepDelay)
        {
            switch (rollingSystem)
            {
                case RollMode.manual:
                    break;
                case RollMode.manual_side:
                    Utility.DisableProcessing(true);
                    saveCamera = new Existence(Camera.main.transform.position, Camera.main.transform.rotation.eulerAngles);
                    // new Existence(new Vector3(-100, 12, -12), new Vector3(45, 0, 0)).Apply(Camera.main.transform);
                    diceSideExistance.Apply(Camera.main.transform);
                    break;
                case RollMode.automaticDice:
                    Debug.Log("RuleSet 5E Plugin: Creating Dolly And Camera");
                    GameObject dolly = new GameObject();
                    dolly.name = "dolly";
                    Camera camera = dolly.AddComponent<Camera>();
                    camera.rect = new Rect(0.005f, 0.20f, 0.20f, 0.25f);
                    dolly.transform.position = diceSideExistance.position; // new Vector3(-100f, 2f, -1.5f);
                    camera.transform.rotation = Quaternion.Euler(diceSideExistance.rotation); // Quaternion.Euler(new Vector3(55, 0, 0));
                    stepDelay = 0.1f;
                    break;
                case RollMode.automaticGenerator:
                    stepDelay = 0.0f;
                    break;
            }
        }

        public void RollCreate(UIDiceTray dt, string formula, ref float stepDelay)
        {
            switch (rollingSystem)
            {
                case RollMode.manual:
                    dt.SpawnAt(new Vector3(instigator.Creature.transform.position.x + 1.0f, 1, instigator.Creature.transform.position.z + 1.0f), Vector3.zero);
                    System.Diagnostics.Process.Start(formula).WaitForExit();
                    break;
                case RollMode.manual_side:
                case RollMode.automaticDice:
                    dt.SpawnAt(new Vector3(diceSideExistance.position.x, diceSideExistance.position.y + ((rollingSystem == RollMode.automaticDice) ? 5 : 1), diceSideExistance.position.z), Vector3.zero);
                    System.Diagnostics.Process.Start(formula).WaitForExit();
                    break;
                case RollMode.automaticGenerator:
                    formula = formula.Substring("talespire://dice/".Length);
                    loadedRollRequest = new Roll()
                    {
                        name = formula.Substring(0, formula.IndexOf(":")),
                        roll = formula.Substring(formula.IndexOf(":") + 1)
                    };
                    NewDiceSet(-2);
                    break;
            }
        }

        public void RollExecute(DiceManager dm, ref float stepDelay)
        {
            stepDelay = 0.0f;
            switch (rollingSystem)
            {
                case RollMode.manual:
                case RollMode.manual_side:
                    // Do Nothing - Let player roll manually
                    break;
                case RollMode.automaticDice:
                    dm.GatherDice(new Vector3(-100, 5, 0), lastRollId);
                    dm.ThrowDice(lastRollId);
                    break;
                case RollMode.automaticGenerator:
                    ResultDiceSet(ResolveRoll(loadedRollRequest.roll));
                    break;
            }
        }

        public void RollCleanup(DiceManager dm, ref float stepDelay)
        {
            switch (rollingSystem)
            {
                case RollMode.manual:
                    break;
                case RollMode.manual_side:
                    Utility.DisableProcessing(false);
                    saveCamera.Apply(Camera.main.transform);
                    saveCamera = null;
                    break;
                case RollMode.automaticDice:
                    GameObject dolly = GameObject.Find("dolly");
                    GameObject.Destroy(dolly);
                    break;
                case RollMode.automaticGenerator:
                    loadedRollRequest = null;
                    break;
            }
        }

        public void NewDiceSet(int rollId)
        {
            switch (stateMachineState)
            {
                case StateMachineState.attackAttackDieWaitCreate:
                case StateMachineState.attackDamageDieWaitCreate:
                case StateMachineState.skillRollDieWaitCreate:
                    Debug.Log("RuleSet 5E Plugin: Dice Set Ready");
                    lastRollId = rollId;
                    stateMachineState++;
                    Debug.Log("RuleSet 5E Plugin: Transitioned To " + stateMachineState);
                    break;
                default:
                    break;
            }
        }

        public void ResultDiceSet(Dictionary<string, object> result)
        {
            if (lastRollId == (int)result["Identifier"])
            {
                switch (stateMachineState)
                {
                    case StateMachineState.attackAttackDieWaitRoll:
                    case StateMachineState.attackDamageDieWaitRoll:
                    case StateMachineState.skillRollDieWaitRoll:
                        Debug.Log("RuleSet 5E Plugin: Dice Set Roll Result Ready");
                        lastResult = result;
                        stateMachineState++;
                        Debug.Log("RuleSet 5E Plugin: Transitioned To " + stateMachineState);
                        break;
                    default:
                        break;
                }
            }
            else
            {
                Debug.Log("RuleSet 5E Plugin: Request '" + lastRollId + "' Result '" + result["Identifier"] + "'. Ignoring.");
            }
        }

        public IEnumerator DisplayMessage(string text, float duration)
        {
            string origMessage = text;
            messageContent = text;
            Debug.Log("RuleSet 5E Plugin: Displaying Message For " + Math.Max(1.0f, duration * processSpeed) + " Seconds");
            yield return new WaitForSeconds(Math.Max(1.0f, duration * processSpeed));
            Debug.Log("RuleSet 5E Plugin: Displaying Message Duration Expired");
            if (messageContent == origMessage) { messageContent = ""; }
        }

        private static string SafeForProtocolName(string tmp)
        {
            tmp = tmp.Replace(" ", " "); // Space => Alt255
            tmp = tmp.Replace("&", " And ");
            return tmp;
        }

        private Dictionary<string, object> ResolveRoll(string roll)
        {
            try
            {
                Debug.Log("Roll: " + roll + " (" + ((lastRollRequest.type == "x2") ? "x2" : "x1") + ")");
                bool min = true;
                bool max = true;
                System.Random ran = new System.Random();
                roll = roll.Substring(roll.IndexOf(" ") + 1).Trim();
                roll = "0+" + roll + "+0";
                roll = roll.ToUpper();
                string originalRoll = roll;
                string expanded = originalRoll;
                while (roll.Contains("D"))
                {
                    int total = 0;
                    int pos = roll.IndexOf("D");
                    int sPos = pos - 1;
                    int ePos = pos + 1;
                    while ("0123456789".Contains(roll.Substring(sPos, 1))) { sPos--; if (sPos == 0) { break; } }
                    while ("0123456789".Contains(roll.Substring(ePos, 1))) { ePos++; if (ePos > roll.Length) { break; } }
                    int dice = int.Parse(roll.Substring(sPos + 1, pos - (sPos + 1)));
                    int sides = int.Parse(roll.Substring(pos + 1, ePos - (pos + 1)));
                    string rolls = "[";
                    for (int d = 0; d < dice; d++)
                    {
                        int pick = ran.Next(1, sides + 1);
                        if (damageMultiplier == 1.0f)
                        {
                            rolls = rolls + pick + ",";
                            total = total + pick;
                        }
                        else
                        {
                            rolls = rolls + pick + "x" + damageMultiplier.ToString("0") + ",";
                            total = total + (int)(damageMultiplier * pick);
                        }
                        if (pick != 1) { min = false; }
                        if (pick != sides) { max = false; }
                    }
                    roll = roll.Substring(0, sPos + 1) + total + roll.Substring(ePos);
                    rolls = rolls.Substring(0, rolls.Length - 1) + "]";
                    int expPos = expanded.IndexOf(dice + "D" + sides);
                    expanded = expanded.Substring(0, expPos) + rolls + expanded.Substring(expPos + (dice.ToString() + "D" + sides.ToString()).Length);
                }
                DataTable dt = new DataTable();
                Dictionary<string, object> results = new Dictionary<string, object>();
                results.Add("Identifier", -2);
                results.Add("Roll", originalRoll.Substring(2).Substring(0, originalRoll.Substring(2).Length - 2));
                results.Add("Total", (int)dt.Compute(roll, null));
                results.Add("Expanded", expanded.Substring(2).Substring(0, expanded.Substring(2).Length - 2));
                results.Add("IsMax", (bool)max);
                results.Add("IsMin", (bool)min);
                return results;
            }
            catch (Exception e)
            {
                Dictionary<string, object> results = new Dictionary<string, object>();
                results.Add("Identifier", -2);
                results.Add("Roll", roll.Substring(2).Substring(0, roll.Substring(2).Length - 2));
                results.Add("Total", 0);
                results.Add("Expanded", e.Message);
                results.Add("IsMax", false);
                results.Add("IsMin", false);
                return results;
            }
        }
    }
}
