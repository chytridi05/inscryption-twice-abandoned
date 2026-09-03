using BepInEx;
using DiskCardGame;
using HarmonyLib;
using InscryptionAPI.Ascension;
using InscryptionAPI.Guid;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace TwiceAbandoned;

[BepInPlugin(GUID, NAME, VERSION)]
[BepInDependency("cyantist.inscryption.api")]
[HarmonyPatch]
public class GBCPlugin : BaseUnityPlugin
{
    public const string GUID = "chytridi05.inscryption.twiceabandoned";
    public const string NAME = "TwiceAbandoned";
    public const string VERSION = "0.0.1";

    internal static bool AlwaysActive;

    private static readonly Harmony harmony = new(GUID);

    private static readonly AscensionChallenge challenge = GuidManager.GetEnumValue<AscensionChallenge>(GUID, "Coward’s Hand");

    private void Awake() 
    {
        AlwaysActive = Config.Bind("Ascension", "Always Active", false, "Toggle whether the challenge should be always active, or in the challenge page.").Value;

        harmony.PatchAll(Assembly.GetExecutingAssembly());

        if (!AlwaysActive)
        {
            LoadChallenge();
        }

        Logger.LogInfo($"Loaded {NAME} {VERSION}");
    }
    private void OnDestroy()
    {
        harmony.UnpatchSelf();
    }
    private void LoadChallenge()
    {
        AscensionChallengeInfo info = ScriptableObject.CreateInstance<AscensionChallengeInfo>();

        info.pointValue = 10;
        info.title = "Coward’s Hand";
        info.description = "Begin each run as though you had abandoned twice. Your Rabbit Pelts are replaced by an Opossum and a Ring Worm.";

        info.iconSprite = ResourceBank.Get<Sprite>(Path.Combine(ASCENSION_PATH, "ascensionicon_nohook"));
        info.activatedSprite = ResourceBank.Get<Sprite>(Path.Combine(ASCENSION_PATH, "ascensionicon_activated_nohook"));

        ChallengeManager.Add(GUID, info, 0);
    }
    [HarmonyPostfix, HarmonyPatch(typeof(AscensionSaveData), nameof(AscensionSaveData.NewRun))]
    private static void NewRun(RunState ___currentRun, int ___numRunsSinceReachedFirstBoss)
    {
        if (AlwaysActive || AscensionSaveData.Data.ChallengeIsActive(challenge))
        {
            if (___numRunsSinceReachedFirstBoss == 0)
            {
                ___currentRun.playerDeck.RemoveCardByName("PeltHare");
                ___currentRun.playerDeck.RemoveCardByName("PeltHare");

                ___currentRun.playerDeck.AddCard(CardLoader.GetCardByName("Opossum"));
                ___currentRun.playerDeck.AddCard(CardLoader.GetCardByName("RingWorm"));
            }
            else if (___numRunsSinceReachedFirstBoss == 1)
            {
                ___currentRun.playerDeck.RemoveCardByName("Opossum");
                ___currentRun.playerDeck.RemoveCardByName("PeltHare");

                ___currentRun.playerDeck.AddCard(CardLoader.GetCardByName("Opossum"));
                ___currentRun.playerDeck.AddCard(CardLoader.GetCardByName("RingWorm"));
            }
        }
    }
    [HarmonyPostfix, HarmonyPatch(typeof(MapGenerator), nameof(MapGenerator.ForceFirstNodeTraderForAscension))]
    private static void ForceFirstNodeTraderForAscension(ref bool __result)
    {
        if (AlwaysActive || AscensionSaveData.Data.ChallengeIsActive(challenge))
        {
            __result = false;
        }
    }
    [HarmonyPostfix, HarmonyPatch(typeof(RunIntroSequencer), nameof(RunIntroSequencer.RunIntroSequence))]
    private static IEnumerator RunIntroSequence(IEnumerator original)
    {
        if (AlwaysActive || AscensionSaveData.Data.ChallengeIsActive(challenge))
        {
            int numRunsSinceReachedFirstBoss = AscensionSaveData.Data.numRunsSinceReachedFirstBoss;
            AscensionSaveData.Data.numRunsSinceReachedFirstBoss = 0;

            try
            {
                yield return original;
            }
            finally
            {
                AscensionSaveData.Data.numRunsSinceReachedFirstBoss = numRunsSinceReachedFirstBoss;
            }
        }
        else
        {
            yield return original;
        }
    }

    private const string ASCENSION_PATH = "art/ui/ascension";
}
