using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GK2Coop
{
    [BepInPlugin(Id, Name, Version)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Id = "de.fredwuz.gk2coop";
        public const string Name = "Graveyard Keeper 2 Co-op Prototype";
        public const string Version = "0.65.6";

        private ConfigEntry<KeyCode> overlayKey;
        private ConfigEntry<string> defaultAddress;
        private ConfigEntry<int> defaultPort;
        private ConfigEntry<bool> autoInitialize;
        private ConfigEntry<string> startupMode;
        private ConfigEntry<bool> unityLogBridge;
        private ConfigEntry<bool> detailedLogs;
        private ConfigEntry<bool> playerCensus;
        private ConfigEntry<bool> commandTelemetry;
        private ConfigEntry<bool> fixRemoteBodyKinematic;
        private ConfigEntry<bool> seedRemoteSpawnPosition;
        private ConfigEntry<bool> matchRemoteBodyScene;
        private ConfigEntry<bool> fixRemoteBodyAppearance;
        private ConfigEntry<string> playerName;
        private ConfigEntry<bool> showStatus;
        private ConfigEntry<bool> showNameTags;
        private ConfigEntry<KeyCode> nameTagKey;
        private ConfigEntry<KeyCode> statusKey;
        private ConfigEntry<bool> showCoopMenu;
        private ConfigEntry<bool> clockSync;
        private ConfigEntry<bool> rebindAfterClientLeaves;
        private ConfigEntry<bool> autoUpdateFromWorkshop;
        private ConfigEntry<bool> keepPlayerProgress;
        private ConfigEntry<string> playerKey;
        private ConfigEntry<ulong> workshopItemId;
        private ConfigEntry<string> workshopFolderOverride;
        private ConfigEntry<bool> relayLogToHost;
        private ConfigEntry<bool> replicateToolUse;
        private ConfigEntry<bool> traceDrops;
        private ConfigEntry<bool> profiler;
        private ConfigEntry<bool> clientRunsDeathLogic;
        private ConfigEntry<bool> replicateWorldDeaths;
        private ConfigEntry<bool> remoteDeathDrops;
        private ConfigEntry<bool> shareQuestProgress;
        private ConfigEntry<bool> shareDrops;
        private ConfigEntry<bool> shareContainers;
        private ConfigEntry<bool> shareCrafting;
        private ConfigEntry<bool> stableReplacementIds;
        private ConfigEntry<bool> sharePlanting;
        private ConfigEntry<bool> shareCraftEnds;
        private ConfigEntry<bool> sharedSleep;
        private ConfigEntry<bool> shareAppearance;
        private ConfigEntry<bool> shareVendors;
        private ConfigEntry<bool> shareKnowledge;
        private ConfigEntry<bool> hideOtherScenes;
        private ConfigEntry<bool> shareWorldValues;
        private ConfigEntry<bool> shareWeather;
        private ConfigEntry<bool> shareActionGems;
        private ConfigEntry<bool> chatEnabled;
        private ConfigEntry<KeyCode> chatKey;
        private ConfigEntry<bool> gameStyle;
        private ConfigEntry<bool> shareScenes;
        private ConfigEntry<bool> shareSpeech;
        private ConfigEntry<KeyCode> sceneWatchKey;
        private ConfigEntry<KeyCode> sceneSkipKey;
        private ConfigEntry<bool> shareBuilding;
        private ConfigEntry<bool> shareZombies;
        private ConfigEntry<string> transportMode;
        private ConfigEntry<int> maxPlayers;
        private ConfigEntry<string> nightPasses;
        private ConfigEntry<KeyCode> inviteKey;
        private ConfigEntry<KeyCode> appearanceKey;
        private ConfigEntry<bool> autoStartNewGame;
        private ConfigEntry<bool> autoStartSkipIntro;
        private static ConfigEntry<bool> relaxStartupGate;
        private static ConfigEntry<bool> sendJoinSnapshot;
        private static ConfigEntry<bool> relayClientCommands;
        private static ConfigEntry<bool> measureNetworkVolume;
        private static ConfigEntry<int> connectRetries;
        private static ConfigEntry<float> connectRetrySeconds;
        private bool autoStartAttempted;
        private float mainSceneReadyAt = -1f;
        private string playgroundSlot;
        private string autoJoinAddress;
        private bool playgroundStarted;
        private bool autoJoinStarted;
        private float nextAutoJoin;
        private Rect window = new Rect(20f, 20f, 620f, 510f);
        private Vector2 scroll;
        private bool overlayVisible;
        private string address;
        private string port;
        private string report = "Waiting for first runtime scan...";
        private string lastAction = "Plugin loaded.";
        private bool startupActionAttempted;
        private float connectAttemptStartedAt = -1f;
        private int connectAttempts;
        private bool awaitingShutdownForRetry;
        private float nextStartupCheck;
        // The one-second ticks, each at its own moment in the second (see RunSecondTicks).
        private Action[] secondTicks;
        private float[] secondTickDue;
        private string lastNetworkState;
        private bool clientIdentityFixed;
        private bool normalFlowNetworking;
        private object attachedHostNetcode;
        private Delegate hostConnectedHandler;
        private Delegate hostDisconnectedHandler;

        private ManualLogSource LogSource => Logger;

        /// <summary>Set in Awake so static helpers can reach the logger and config.</summary>
        internal static Plugin Instance;

        private void Awake()
        {
            Instance = this;
            Application.runInBackground = true;
            if (Environment.GetEnvironmentVariable("GK2COOP_TEST_VANILLA") == "1" && CoopSaveBootstrap.TestSaveFolder != null)
            {
                // Test copies only: the game as it is without the mod, for a performance baseline.
                // Only the test save folder and the test slot (so no player's save is touched),
                // and the profiler's frame times.
                var vanillaHarmony = new Harmony(Id + ".vanilla");
                CoopSaveBootstrap.Init(Logger);
                CoopSaveBootstrap.Install(vanillaHarmony);
                CoopProfiler.Install(Logger, gameObject, true);
                Logger.LogInfo($"{Name} {Version} loaded as a vanilla baseline: no co-op, no patches but the test save folder.");
                enabled = false;
                return;
            }
            InstallSerializerPatch();
            KeepFormerSettings();
            overlayKey = Config.Bind("UI", "OverlayKey", KeyCode.F8, "Show or hide the co-op diagnostics window.");
            defaultAddress = Config.Bind("Network", "Address", "127.0.0.1", "Host address used by the prototype.");
            defaultPort = Config.Bind("Network", "Port", 8889, "UDP port used by Unity Transport.");
            autoInitialize = Config.Bind("Network", "AutoInitialize", true, "Initialize the dormant native network services after their scene objects are ready.");
            startupMode = Config.Bind("Network", "StartupMode", "None", "Development automation: None, Host, or Connect.");
            inviteKey = Config.Bind("UI", "InviteKey", KeyCode.F10, "While hosting over Steam: open Steam's overlay to invite friends.");
            maxPlayers = Config.Bind("Network", "MaxPlayers", 4, "Players in a session hosted over Steam, including you: 2, 3 or 4. Sets the size of the Steam lobby friends join.");
            nightPasses = Config.Bind("Network", "NightPasses", "Everyone", "When you host: when the night passes. Everyone: only when every player sleeps. Host: when you sleep; the others stay awake and their clock jumps ahead with yours.");
            transportMode = Config.Bind("Network", "Transport", "IP", "IP: the game's own UDP connection (port forwarding or VPN for internet play). Steam: Steam's networking — join a Steam friend by their SteamID (address steam:<id>) through Valve's relay, no port forwarding; plain addresses still work through Steam's sockets. Falls back to IP when Steam is not running.");
            detailedLogs = Config.Bind("Diagnostics", "DetailedLogs", false, "Write the detailed logs of the switches below (player census, command telemetry, network volume, drop trace, the game's informational messages, the joiners' logs on the host). Off: only the switches' errors and the usual messages. The detailed logs cost frame time; turn them on only to look into a problem.");
            unityLogBridge = Config.Bind("Diagnostics", "UnityLogBridge", true, "Forward the game's own errors and networking messages into the BepInEx log.");
            playerCensus = Config.Bind("Diagnostics", "PlayerCensus", true, "Log the component graph, ownership, and position of every player body when it changes.");
            commandTelemetry = Config.Bind("Diagnostics", "CommandTelemetry", true, "Count serialized/deserialized commands and report replicated movement.");
            fixRemoteBodyKinematic = Config.Bind("Fixes", "RemoteBodyKinematic", true, "Keep remote player bodies kinematic so replicated MoveByPosition is accepted instead of rejected.");
            fixRemoteBodyAppearance = Config.Bind("Fixes", "RemoteBodyAppearance", false, "Re-apply the standard preset and colour palette to remote bodies. Off by default: the prefab appearance is already correct, and the intro chains this was meant to solve were an animation state, not a skin.");
            seedRemoteSpawnPosition = Config.Bind("Fixes", "SeedRemoteSpawnPosition", true, "Spawn a remote player at the local player's position instead of the origin, until its first movement update arrives.");
            matchRemoteBodyScene = Config.Bind("Fixes", "MatchRemoteBodyScene", true, "Move a remote body into the same scene as the local player body instead of leaving it in whichever scene happened to be active.");
            playerName = Config.Bind("Session", "PlayerName", "", "Name shown above your character and in the status panel. Blank uses Host or Player N.");
            replicateToolUse = Config.Bind("Coop", "ReplicateToolUse", false, "Phase 2 experiment: replicate applying a tool to a world object inside the acting player's context. Off by default: world-object removal is not replicated yet, so a harvested object survives on the other machine as an unusable ghost.");
            clientRunsDeathLogic = Config.Bind("Fixes", "ClientRunsDeathLogic", true, "The game skips every post-death effect on a co-op client, so a client gets no drops and no quest progress from anything it kills or harvests. Let the client run that logic locally. Turn off once world state is genuinely replicated, or effects apply twice.");
            replicateWorldDeaths = Config.Bind("Coop", "ReplicateWorldDeaths", true, "When a world object dies on one machine, kill it on the other so both players see the same world.");
            remoteDeathDrops = Config.Bind("Coop", "RemoteDeathDrops", false, "Let a mirrored death also produce its drops and quest effects locally, duplicating the resource so neither player is blocked. Superseded by ShareDrops, which creates the drop once and lets the host decide who gets it; while ShareDrops is on this is ignored. Only turn it on if ShareDrops is off.");
            shareQuestProgress = Config.Bind("Coop", "ShareQuestProgress", true, "Mirror quest transitions between players, so one player doing the work advances the story for both. Without this a shared world is unplayable: a harvested object is gone for everyone but only advances whoever took it.");
            shareDrops = Config.Bind("Coop", "ShareDrops", true, "Create each drop once and mirror it. The host authorizes pickup before crediting one player, so contested and failed pickup conserve items. Overrides RemoteDeathDrops.");
            shareKnowledge = Config.Bind("Coop", "ShareKnowledge", true, "One tech tree and recipe book for everyone: techs, recipes, buildings, alchemy formulas and dialogue phrases one player learns, every player knows. Action gems are pooled or not by ShareActionGems.");
            hideOtherScenes = Config.Bind("UI", "HidePlayersInOtherScenes", true, "Hide another player (and their name tag) while they are in a different scene, such as the prison or a sewer, which share coordinates with the open world.");
            shareWorldValues = Config.Bind("Coop", "ShareWorldValues", true, "World values kept in the player's resource store are the same for everyone: reputation, congregation, happiness, sermon readiness and the like. Money and energy stay each player's own; action gems follow ShareActionGems.");
            shareWeather = Config.Bind("Coop", "ShareWeather", true, "One sky for everyone: only the host rolls the weather, and every player sees the host's rain, fog or sunshine.");
            shareActionGems = Config.Bind("Coop", "ShareActionGems", true, "Action gems (red, green and blue tech points) are one pool for everyone: what any player earns, every player can spend on the tech tree. The host's setting applies to its game. Off: each player earns and keeps their own.");
            chatEnabled = Config.Bind("Coop", "Chat", true, "In-game chat between players.");
            CoopMenu.RecentAddresses = Config.Bind("Coop", "RecentAddresses", string.Empty, "Addresses joined before (host:port, newest first), offered on the Join page.");
            shareSpeech = Config.Bind("Coop", "ShowOthersConversations", true, "Show the lines of another player's conversation with an NPC to players standing nearby.");
            shareScenes = Config.Bind("Coop", "OfferScenes", true, "When a story scene starts for one player, ask the others whether to watch it too. Those who accept are taken to the scene and back.");
            sceneWatchKey = Config.Bind("Coop", "WatchSceneKey", KeyCode.Y, "Key that answers \"Watch\" when another player's story scene is offered. On a controller: Y.");
            sceneSkipKey = Config.Bind("Coop", "SkipSceneKey", KeyCode.N, "Key that answers \"Keep playing\", and stops watching a scene. On a controller: B (hold B to stop watching).");
            gameStyle = Config.Bind("UI", "GameStyle", true, "Draw the co-op menu, status, announcements, name tags and chat with the game's own windows, buttons and fonts. Off: the plain look of earlier versions.");
            chatKey = Config.Bind("UI", "ChatKey", KeyCode.T, "During a co-op game, open the chat line. Enter sends, Escape closes.");
            shareBuilding = Config.Bind("Coop", "ShareBuilding", true, "Buildings placed or removed in build mode appear or disappear for every player.");
            shareZombies = Config.Bind("Coop", "ShareZombies", true, "Zombies are placed for every player and run by the host; joiners see the host's zombies move and work.");
            shareVendors = Config.Bind("Coop", "ShareVendors", true, "Vendors are the same shop for every player: stock, the vendor's money and its weekly budget follow every trade. Each player's own money stays their own.");
            shareAppearance = Config.Bind("Coop", "ShareAppearance", true, "Every player is drawn with their own look on everyone's screen, and a returning player keeps their look. Off: other players are drawn with your own look.");
            sharedSleep = Config.Bind("Coop", "NightPassesWhenAllSleep", true, "Sleeping speeds up time only when every player is asleep. A player sleeping alone still recovers energy, at normal speed. Off: each machine speeds up its own time when its player sleeps, and the worlds drift apart.");
            shareCraftEnds = Config.Bind("Coop", "ShareFinishedWorldCrafts", true, "When a craft that changes the world object finishes (a repair, a cleared blockage, an opened door, a grown crop), it finishes on every player's machine.");
            sharePlanting = Config.Bind("Coop", "SharePlanting", true, "A seed planted by one player grows in the same bed for everyone. The seed is spent only by the player who planted it.");
            stableReplacementIds = Config.Bind("Coop", "StableReplacementIds", true, "When a world object turns into another (a finished crop, a felled tree, a repaired object), give the new object the same id on every machine, so later syncs still find it. Off: each machine invents its own id and the two worlds drift apart.");
            shareCrafting = Config.Bind("Coop", "ShareCrafting", true, "Crafting stations that open the craft window (workbenches, anvils, ovens, kilns) are run by the host and shown to joiners; a joiner's craft is queued on the host and takes its ingredients from that joiner. Off: each player's station runs separately and crafts are not shared.");
            shareContainers = Config.Bind("Coop", "ShareContainers", true, "Mirror world-container contents through a host-assigned snapshot revision. Concurrent edits converge to the host's last received snapshot; they are not merged or safe as crafting transactions.");
            sendJoinSnapshot = Config.Bind("Coop", "SendJoinSnapshot", true, "When a client joins, replay world changes made since this game session started: deaths, containers, drops, and quest transitions. This does not synchronize a host's older save with a fresh client's world, or restore per-player inventory and energy.");
            relayClientCommands = Config.Bind("Coop", "RelayClientCommands", true, "Forward a client's command to the other clients. The game executes it on the host and then passes the connection state, not the command, to its own send path, so it is silently dropped. Invisible at two players; at three, one client never sees the other move.");
            measureNetworkVolume = Config.Bind("Diagnostics", "MeasureNetworkVolume", true, "Count messages and bytes leaving this machine, reported every 30 seconds with the peer count. The host sends every message to every peer, so its outbound volume grows with the number of clients; this measures that rather than leaving it to argument.");
            connectRetries = Config.Bind("Coop", "ConnectRetries", 5, "How many times a client retries a connection that starts but never reaches the host. Starting a client only means Netcode accepted the call; the connection can still fail a few seconds later, and without a retry one miss leaves the player alone in their own world.");
            connectRetrySeconds = Config.Bind("Coop", "ConnectRetrySeconds", 10f, "Seconds to wait for a connection attempt to reach the host before retrying it.");
            profiler = Config.Bind("Diagnostics", "Profiler", false, "Measure the mod's own cost: time per frame, the slowest parts, frame times, garbage collections and network traffic, written to the log every 10 seconds. Costs a little time itself; turn it on only to find a slowdown.");
            traceDrops = Config.Bind("Diagnostics", "TraceDrops", true, "Log where harvested items go: drop spawn, collector trigger, and which player data CollectDrop credits.");
            autoStartNewGame = Config.Bind("Testing", "AutoStartNewGame", false, "Automation only: press New Game by itself once the main menu is up, so a two-instance test can run unattended. Invokes exactly what the menu button invokes.");
            autoStartSkipIntro = Config.Bind("Testing", "AutoStartSkipIntro", true, "With AutoStartNewGame, pass the game's own skipMainScene flag so the intro cinematic is bypassed. The intro waits for input, which an unattended run cannot provide.");
            relaxStartupGate = Config.Bind("Testing", "RelaxStartupGate", false, "Automation only: attach networking as soon as the world is in game with an active body, without waiting for player control. The intro cinematic takes control until a key is pressed, which an unattended run cannot do.");
            relayLogToHost = Config.Bind("Session", "RelayLogToHost", true, "Stream this machine's plugin log to the host when running as a client, so one log file holds both sides of a test.");
            clockSync = Config.Bind("Session", "ClockSync", true, "Let the host's in-game clock correct the client's. One in-game day is five real minutes, so drift accumulates quickly.");
            rebindAfterClientLeaves = Config.Bind("Session", "RebindAfterClientLeaves", true, "Host only. Refresh the network socket a second after a player leaves, so a player whose game crashed can rejoin. Works around a receive-buffer leak in the game's network library; turn off only to diagnose connection problems.");
            keepPlayerProgress = Config.Bind("Coop", "KeepPlayerProgress", true, "Each joining player keeps their own inventory, money, tech points, energy and health between sessions. The host stores them next to its own config, per save slot; the game's save files are not changed. Off: a joiner who copies the host's world plays with a copy of the host's inventory.");
            playerKey = Config.Bind("Session", "PlayerKey", "", "Identifies this player to hosts so they can return your inventory next time. Generated automatically; keep it if you reinstall to keep your progress.");
            if (!System.Text.RegularExpressions.Regex.IsMatch(playerKey.Value ?? string.Empty, "^[0-9a-f]{32}$"))
            {
                playerKey.Value = Guid.NewGuid().ToString("N");
            }
            autoUpdateFromWorkshop = Config.Bind("Updates", "AutoUpdateFromWorkshop", true, "If you subscribed to the co-op mod on the Steam Workshop, copy a newer Workshop version into this game folder at startup. Steam downloads updates but cannot install BepInEx mods; this does, and the update is active from the next launch.");
            workshopItemId = Config.Bind("Updates", "WorkshopItemId", 0UL, "Workshop item to update from. 0 uses the official co-op mod item built into this version. Only change this if you know the item is trustworthy: its files replace this mod's code.");
            workshopFolderOverride = Config.Bind("Testing", "WorkshopFolderOverride", "", "Automation only: read updates from this folder instead of the Steam Workshop download folder.");
            appearanceKey = Config.Bind("UI", "ChangeLookKey", KeyCode.F11, "During a co-op game, open the game's character customization to change your look. The other players see the new look.");
            showStatus = Config.Bind("UI", "ShowSessionStatus", true, "Show the small multiplayer status panel.");
            showCoopMenu = Config.Bind("UI", "ShowCoopMenu", true, "Show the co-op setup panel on the main menu, so hosting or joining needs no config editing.");
            showNameTags = Config.Bind("UI", "ShowNameTags", true, "Show player names above characters.");
            statusKey = Config.Bind("UI", "SessionStatusKey", KeyCode.F6, "Toggle the multiplayer status panel.");
            nameTagKey = Config.Bind("UI", "NameTagKey", KeyCode.F7, "Toggle name tags above characters.");
            CoopDiagnostics.Init(Logger);
            // The detailed logs are written only with [Diagnostics] DetailedLogs (the tests turn it on):
            // each of them was on by default, and together they cost players frame time.
            bool detailed = detailedLogs.Value;
            CoopDiagnostics.Detailed = detailed;
            CoopDiagnostics.CensusEnabled = detailed && playerCensus.Value;
            CoopDiagnostics.TelemetryEnabled = detailed && commandTelemetry.Value;
            if (unityLogBridge.Value)
            {
                CoopDiagnostics.InstallUnityLogBridge();
            }
            CoopPlayerContext.Init(Logger);
            CoopToolSync.Init(Logger);
            CoopToolSync.Enabled = replicateToolUse.Value;
            CoopDropTrace.Init(Logger);
            CoopDropTrace.Enabled = detailed && traceDrops.Value;
            CoopDeathLogicFix.Init(Logger);
            CoopDeathLogicFix.Enabled = clientRunsDeathLogic.Value;
            CoopWorldSync.Init(Logger);
            CoopWorldSync.Enabled = replicateWorldDeaths.Value;
            CoopWorldSync.RemoteDeathDrops = remoteDeathDrops.Value;
            CoopQuestSync.Init(Logger);
            CoopQuestSync.Enabled = shareQuestProgress.Value;
            CoopDropSync.Init(Logger);
            CoopDropSync.Enabled = shareDrops.Value;
            CoopContainerSync.Init(Logger);
            CoopContainerSync.Enabled = shareContainers.Value;
            CoopCraftSync.Init(Logger);
            CoopStableIds.Init(Logger);
            CoopGardenSync.Init(Logger);
            CoopCraftEndSync.Init(Logger);
            CoopSleepSync.Init(Logger);
            CoopPauseSync.Init(Logger);
            CoopConveyorSync.Init(Logger);
            CoopAppearanceSync.Init(Logger);
            CoopVendorSync.Init(Logger);
            CoopVendorSync.Enabled = shareVendors.Value;
            CoopKnowledgeSync.Init(Logger);
            CoopKnowledgeSync.Enabled = shareKnowledge.Value;
            CoopSceneSync.Init(Logger);
            CoopSceneSync.Enabled = hideOtherScenes.Value;
            CoopWorldResSync.Init(Logger);
            CoopWorldResSync.Enabled = shareWorldValues.Value;
            CoopWeatherSync.Init(Logger);
            CoopWeatherSync.Enabled = shareWeather.Value;
            CoopSharedGems.Init(Logger);
            CoopSharedGems.Configured = shareActionGems.Value;
            GameUi.Init(Logger);
            GameUi.Enabled = gameStyle.Value;
            CoopChat.Init(Logger);
            CoopChat.Enabled = chatEnabled.Value;
            CoopChat.Key = chatKey.Value;
            CoopBuildSync.Init(Logger);
            CoopBuildSync.Enabled = shareBuilding.Value;
            CoopZombieSync.Init(Logger);
            CoopZombieSync.Enabled = shareZombies.Value;
            CoopAppearanceSync.Enabled = shareAppearance.Value;
            CoopSleepSync.Enabled = sharedSleep.Value;
            CoopCraftEndSync.Enabled = shareCraftEnds.Value;
            CoopGardenSync.Enabled = sharePlanting.Value;
            CoopStableIds.Enabled = stableReplacementIds.Value;
            CoopCraftSync.Enabled = shareCrafting.Value;
            CoopJoinSnapshot.Init(Logger);
            CoopJoinSnapshot.Enabled = sendJoinSnapshot.Value;
            CoopHostRelay.Init(Logger);
            CoopHostRelay.Enabled = relayClientCommands.Value;
            CoopStatus.Init(Logger);
            CoopWorkshopUpdater.Run(Logger, autoUpdateFromWorkshop.Value, workshopItemId.Value, workshopFolderOverride.Value);
            CoopMenu.Enabled = showCoopMenu.Value;
            CoopMenu.Init(Logger, startupMode, defaultAddress, defaultPort, playerName, ArmStartupAction);
            CoopNetStats.Init(Logger);
            CoopNetStats.Enabled = detailed && measureNetworkVolume.Value;
            if (shareDrops.Value && remoteDeathDrops.Value)
            {
                // Both would spawn a drop for the same death; sharing supersedes duplicating.
                CoopWorldSync.RemoteDeathDrops = false;
                Logger.LogInfo("ShareDrops is on, so RemoteDeathDrops is ignored for this session.");
            }
            var coopHarmony = new Harmony(Id + ".coop");
            CoopPatches.Install(coopHarmony, Logger, fixRemoteBodyKinematic.Value, detailed && commandTelemetry.Value, matchRemoteBodyScene.Value, fixRemoteBodyAppearance.Value);
            CoopBodies.Install(coopHarmony, Logger);
            CoopToolSync.Install(coopHarmony);
            CoopDropTrace.Install(coopHarmony);
            CoopDeathLogicFix.Install(coopHarmony);
            CoopWorldSync.Install(coopHarmony);
            CoopQuestSync.Install(coopHarmony);
            CoopHostRelay.Install(coopHarmony);
            CoopNetStats.Install(coopHarmony);
            CoopDropSync.Install(coopHarmony);
            CoopContainerSync.Install(coopHarmony);
            CoopStableIds.Install(coopHarmony);
            CoopGardenSync.Install(coopHarmony);
            CoopCraftEndSync.Install(coopHarmony);
            CoopSleepSync.Install(coopHarmony);
            CoopPauseSync.Install(coopHarmony);
            CoopConveyorSync.Install(coopHarmony);
            CoopVendorSync.Install(coopHarmony);
            CoopStaleBodyFix.Install(coopHarmony, Logger);
            CoopSaveHygiene.Install(coopHarmony, Logger);
            CoopWeatherSync.Install(coopHarmony);
            CoopCutsceneTrace.Init(Logger);
            CoopCutsceneTrace.Install(coopHarmony);
            CoopInput.Init(Logger);
            CoopPauseEntry.Init(Logger);
            CoopInput.Install(coopHarmony);
            GameUiPanel.HideUsedButtonsFromGame();
            CoopScenePrompt.Init(Logger);
            CoopScenePrompt.WatchKey = sceneWatchKey.Value;
            CoopScenePrompt.SkipKey = sceneSkipKey.Value;
            CoopScenePrompt.Install();
            CoopFightWatch.Init(Logger);
            CoopFightWatch.Install(coopHarmony);
            CoopFightLock.Init(Logger);
            CoopFightLock.Install(coopHarmony);
            CoopRemoteBodyGuard.Init(Logger);
            CoopRemoteBodyGuard.Install(coopHarmony);
            CoopWispFollow.Init(Logger);
            CoopWispFollow.Install(coopHarmony);
            CoopArenaRescue.Init(Logger);
            CoopHostLeft.Init(Logger);
            CoopLifecycle.Init(Logger);
            CoopWatchdog.Init(Logger);
            CoopHostLeft.Install(coopHarmony);
            CoopLifecycle.Install(coopHarmony);
            CoopTestTools.Init(Logger);
            CoopTestTools.Enabled = Config.Bind("Testing", "TestTools", false, "Test copies only: Pause > Co-op > Test tools builds a test yard, hands out a kit, starts scenes and sermons, teleports and sets the weather. Not for normal play.").Value;
            string continueSlot = Config.Bind("Testing", "ContinueSlot", string.Empty, "Test copies only, with TestTools: the save slot the main menu's Continue opens (the playground), whatever its date. Continue is pressed by itself.").Value;
            if (CoopTestTools.Enabled && !string.IsNullOrEmpty(continueSlot))
            {
                CoopSaveBootstrap.ForceActiveSlotForTest(continueSlot);
                playgroundSlot = continueSlot;
            }
            string autoJoin = Config.Bind("Testing", "AutoJoin", string.Empty, "Test copies only, with TestTools: copy the world of the host at this address and join, by itself, from the main menu (the playground).").Value;
            if (CoopTestTools.Enabled && !string.IsNullOrEmpty(autoJoin))
            {
                autoJoinAddress = autoJoin.Trim();
            }
            CoopWorkLock.Init(Logger);
            CoopWorkLock.Install(coopHarmony);
            CoopSpeechShare.Init(Logger);
            CoopSpeechShare.Enabled = shareSpeech.Value;
            CoopSpeechShare.Install(coopHarmony);
            CoopSceneShare.Init(Logger);
            CoopSceneShare.Enabled = shareScenes.Value;
            CoopSceneShare.Install(coopHarmony);
            CoopSteamTransportSwitch.UseSteam = string.Equals(transportMode.Value, "Steam", StringComparison.OrdinalIgnoreCase);
            CoopSteamTransportSwitch.Install(coopHarmony, Logger);
            CoopSteamLobby.Init(Logger);
            CoopSteamLobby.MaxPlayers = Math.Max(2, Math.Min(4, maxPlayers.Value));
            CoopSteamLobby.TestJoinOverLocalIp = Environment.GetEnvironmentVariable("GK2COOP_TEST_STEAM_LOCALIP") == "1";
            CoopMenu.TransportSetting = transportMode;
            CoopMenu.MaxPlayersSetting = maxPlayers;
            CoopMenu.NightSetting = nightPasses;
            CoopSleepSync.HostNightRule = nightPasses;
            CoopMenu.InviteKeyName = inviteKey.Value.ToString();
            CoopFightSync.Init(Logger);
            CoopFightSync.Install();
            CoopBuildSync.Install(coopHarmony);
            CoopZombieSync.Install(coopHarmony);
            if (shareCrafting.Value)
            {
                CoopCraftSync.Install(coopHarmony);
            }
            CoopHud.Init(Logger);
            CoopHud.ShowStatus = showStatus.Value;
            CoopHud.ShowNameTags = showNameTags.Value;
            CoopSession.Init(Logger, playerName.Value);
            CoopSession.ClockSyncEnabled = clockSync.Value;
            CoopSaveBootstrap.Init(Logger);
            CoopSaveBootstrap.TidyWorldCopies();
            CoopSaveBootstrap.Install(coopHarmony);
            CoopTransportGuard.Init(Logger);
            CoopPlayerProfiles.Init(Logger, playerKey.Value);
            CoopPlayerProfiles.SaveKey = key => playerKey.Value = key;
            CoopPlayerProfiles.Enabled = keepPlayerProgress.Value;
            CoopTransportGuard.Enabled = rebindAfterClientLeaves.Value;
            CoopLogRelay.Enabled = detailed && relayLogToHost.Value;
            CoopLogRelay.Init(Logger);
            address = defaultAddress.Value;
            port = defaultPort.Value.ToString();
            SceneManager.sceneLoaded += OnSceneLoaded;
            CoopProfiler.Install(Logger, gameObject, profiler.Value);
            Logger.LogInfo($"{Name} {Version} loaded. Press {overlayKey.Value} for diagnostics.");
            InvokeRepeating(nameof(RefreshShownReport), 1f, 3f);
        }

        /// <summary>
        /// The plugin id was com.fabio.gk2coop until the fork's maintainer took it over. BepInEx
        /// names the settings file after the id, so a player's settings would stay behind in the old
        /// file: it is copied once, before the first setting is read. The old file is left alone.
        /// </summary>
        private void KeepFormerSettings()
        {
            string former = System.IO.Path.Combine(Paths.ConfigPath, "com.fabio.gk2coop.cfg");
            if (System.IO.File.Exists(Config.ConfigFilePath) || !System.IO.File.Exists(former))
            {
                return;
            }
            try
            {
                System.IO.File.Copy(former, Config.ConfigFilePath);
                Config.Reload();
                Logger.LogInfo("Settings carried over from com.fabio.gk2coop.cfg.");
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Could not carry over the settings from com.fabio.gk2coop.cfg: " + Unwrap(ex).Message);
            }
        }

        private void InstallSerializerPatch()
        {
            Type serializer = RequireType("LazyBearTechnology.LazySerializer");
            MethodInfo deserializeInternal = serializer
                .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                .Single(method => method.Name == "DeserializeInternal"
                    && !method.IsGenericMethod
                    && method.GetParameters().Length == 3
                    && method.GetParameters()[0].ParameterType == typeof(Type));
            MethodInfo transpiler = typeof(Plugin).GetMethod(nameof(AllowPrivateSerializerConstructors), BindingFlags.Static | BindingFlags.NonPublic);
            new Harmony(Id).Patch(deserializeInternal, transpiler: new HarmonyMethod(transpiler));
            Logger.LogInfo("Patched LazySerializer to allow private parameterless constructors.");
        }

        private static IEnumerable<CodeInstruction> AllowPrivateSerializerConstructors(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo publicConstructorOnly = AccessTools.Method(typeof(Activator), nameof(Activator.CreateInstance), new[] { typeof(Type) });
            MethodInfo allowPrivate = AccessTools.Method(typeof(Plugin), nameof(CreateSerializableInstance));
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.Calls(publicConstructorOnly))
                {
                    instruction.operand = allowPrivate;
                }
                yield return instruction;
            }
        }

        private static object CreateSerializableInstance(Type type)
        {
            if (type == null)
            {
                return null;
            }
            try
            {
                return Activator.CreateInstance(type, true);
            }
            catch (MissingMethodException)
            {
                return FormatterServices.GetUninitializedObject(type);
            }
        }

        private void OnApplicationQuit()
        {
            CoopPlayerProfiles.UploadNow("quitting");
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            CoopDiagnostics.RemoveUnityLogBridge();
            CoopSession.UnregisterHandlers();
            CoopLogRelay.Shutdown();
            DetachHostCallbacks();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Logger.LogInfo($"Scene loaded: {scene.name} ({mode})");
            if (scene.name == "MainScene")
            {
                mainSceneReadyAt = Time.unscaledTime;
            }
            CancelInvoke(nameof(LogSnapshot));
            Invoke(nameof(LogSnapshot), 2f);
        }

        /// <summary>
        /// After a scene load or a player leaving: starts the game's dormant networking once it
        /// can, and writes the state. The full report searches all memory for seven types (about
        /// 60 ms in a day-18 world), so it is written only with DetailedLogs or on F9; otherwise
        /// a short line.
        /// </summary>
        private void LogSnapshot()
        {
            if (autoInitialize.Value && !IsNativeInitialized() && CanInitialize(out _))
            {
                RunAction("Automatic native initialization", InitializeNativeNetwork);
            }
            if (CoopDiagnostics.Detailed)
            {
                WriteSnapshot();
            }
            else
            {
                Logger.LogInfo("Native network snapshot: " + BriefState());
            }
        }

        private void WriteSnapshot()
        {
            RefreshReport();
            Logger.LogInfo("Native network snapshot:\n" + report);
        }

        private static string BriefState()
        {
            try
            {
                return "scene " + SceneManager.GetActiveScene().name + "; LazyNetwork initialized: " + IsNativeInitialized() + "; " +
                       DescribeMemberValues(CoopDiagnostics.GetStatic(FindGameType("Unity.Netcode.NetworkManager"), "Singleton"), "Netcode", "IsHost", "IsClient", "IsListening", "LocalClientId") + "; " +
                       DescribeMemberValues(CoopDiagnostics.GetStatic(FindGameType("MainGame"), "Instance"), "Main game", "gameState", "IsGamePaused");
            }
            catch (Exception ex)
            {
                return "(" + ex.Message + ")";
            }
        }

        /// <summary>Every 3 s: the F8 window's report, only while the window is open.</summary>
        private void RefreshShownReport()
        {
            if (overlayVisible)
            {
                RefreshReport();
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(inviteKey.Value))
            {
                if (!CoopSteamLobby.OpenInviteDialog("the " + inviteKey.Value + " key"))
                {
                    CoopStatus.Announce(L.T("Inviting works while you host on Steam."), false, 5f);
                }
            }
            CoopZombieSync.Tick();
            CoopCraftSync.Pump();
            CoopChat.Update(IsOnMainMenu);
            try
            {
                CoopSceneShare.Update();
            }
            catch (Exception ex)
            {
                LogSource.LogWarning("Scene share: " + Unwrap(ex));
            }
            try
            {
                CoopTestTools.Update();
            }
            catch (Exception ex)
            {
                LogSource.LogWarning("Test tools: " + Unwrap(ex));
            }
            try
            {
                CoopWorkLock.Update();
            }
            catch (Exception ex)
            {
                LogSource.LogWarning("Work lock: " + Unwrap(ex));
            }
            try
            {
                CoopFightLock.Update();
                CoopWispFollow.Tick();
                CoopArenaRescue.Tick();
            }
            catch (Exception ex)
            {
                LogSource.LogWarning("Fight lock: " + Unwrap(ex));
            }
            try
            {
                bool inSession = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !IsOnMainMenu();
                CoopPauseEntry.Update(inSession);
            }
            catch (Exception ex)
            {
                LogSource.LogWarning("Pause menu entry: " + Unwrap(ex));
            }
            try
            {
                CoopMenu.Tick(IsOnMainMenu);
            }
            catch (Exception ex)
            {
                GameUi.Enabled = false;
                LogSource.LogError("Game-styled co-op menu failed; using the plain look: " + Unwrap(ex));
            }
            try
            {
                CoopKeyboard.Update();
            }
            catch (Exception ex)
            {
                GameUi.Enabled = false;
                LogSource.LogError("Game-styled co-op menu failed; using the plain look: " + Unwrap(ex));
            }
            if (Input.GetKeyDown(overlayKey.Value))
            {
                overlayVisible = !overlayVisible;
            }

            if (Input.GetKeyDown(KeyCode.F9))
            {
                WriteSnapshot();
            }

            if (Input.GetKeyDown(statusKey.Value))
            {
                CoopHud.ShowStatus = !CoopHud.ShowStatus;
            }

            if (Input.GetKeyDown(nameTagKey.Value))
            {
                CoopHud.ShowNameTags = !CoopHud.ShowNameTags;
            }

            if (Input.GetKeyDown(appearanceKey.Value) && NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !IsOnMainMenu())
            {
                CoopAppearanceSync.OpenCustomizationWindow();
            }

            DriveAutoStart();
            DrivePlayground();

            if (!startupActionAttempted && Time.unscaledTime >= nextStartupCheck)
            {
                nextStartupCheck = Time.unscaledTime + 1f;
                TryRunConfiguredStartupAction();
            }

            DriveConnectRetry();
            CoopSaveBootstrap.Tick();
            CoopTransportGuard.Tick();
            CoopLifecycle.Tick();

            RunSecondTicks();
            CoopPlayerContext.VerifyNotLeaked();
        }

        /// <summary>
        /// The ticks that run once a second, spread evenly over the second. They all ran in the
        /// same frame before, so their costs added up to one long frame every second.
        /// </summary>
        private void RunSecondTicks()
        {
            float now = Time.unscaledTime;
            if (secondTicks == null)
            {
                secondTicks = new Action[]
                {
                    () => { PollNetworkState(); DriveSessionAutomation(); },
                    CoopDiagnostics.Tick, CoopNetStats.Tick, CoopHostLeft.Tick, CoopWatchdog.Tick, CoopSession.Tick,
                    CoopPlayerProfiles.Tick, CoopCraftSync.Tick, CoopSleepSync.Tick, CoopAppearanceSync.Tick,
                    CoopVendorSync.Tick, CoopKnowledgeSync.Tick, CoopSceneSync.Tick, CoopWorldResSync.Tick,
                    CoopSharedGems.Tick, CoopWeatherSync.Tick, CoopSteamLobby.Tick, CoopLogRelay.Tick,
                    CoopDropTrace.Tick, CoopHud.Tick, CoopDropSync.Tick,
                };
                secondTickDue = new float[secondTicks.Length];
                for (int i = 0; i < secondTicks.Length; i++)
                {
                    secondTickDue[i] = now + (float)i / secondTicks.Length;
                }
            }
            for (int i = 0; i < secondTicks.Length; i++)
            {
                if (now < secondTickDue[i])
                {
                    continue;
                }
                // Each keeps its place in the second; after a long frame it starts again from now.
                secondTickDue[i] += 1f;
                if (secondTickDue[i] <= now)
                {
                    secondTickDue[i] = now + 1f;
                }
                secondTicks[i]();
            }
        }

        /// <summary>
        /// Presses New Game on our own behalf, for unattended two-instance testing. This calls the
        /// same <c>MainGame.StartNewGame()</c> the menu button calls, so the normal startup flow —
        /// intro included — is preserved exactly.
        /// </summary>
        private void DriveAutoStart()
        {
            if (autoStartAttempted || !autoStartNewGame.Value || mainSceneReadyAt < 0f)
            {
                return;
            }
            if (Time.unscaledTime - mainSceneReadyAt < 6f)
            {
                return;
            }
            try
            {
                object mainGame = CoopDiagnostics.GetStatic(RequireType("MainGame"), "Instance");
                if (mainGame == null ||
                    !string.Equals(Convert.ToString(CoopDiagnostics.GetMember(mainGame, "gameState")), "MainMenu", StringComparison.Ordinal))
                {
                    return;
                }
                autoStartAttempted = true;
                bool skipIntro = autoStartSkipIntro.Value;
                Invoke(mainGame, "StartNewGame", skipIntro);
                LogSource.LogInfo("AutoStartNewGame invoked MainGame.StartNewGame(skipMainScene: " + skipIntro + ").");
            }
            catch (Exception ex)
            {
                autoStartAttempted = true;
                LogSource.LogError("AutoStartNewGame failed: " + Unwrap(ex));
            }
        }

        /// <summary>
        /// The playground on test copies: the host's Continue and the joiners' "copy host world and
        /// join" are pressed by themselves at the main menu, through what the buttons call. A join
        /// is tried again every 10 s until it gets in: the host may still be loading, and it sends
        /// its world to one joiner at a time.
        /// </summary>
        private void DrivePlayground()
        {
            if (playgroundStarted || (playgroundSlot == null && autoJoinAddress == null) || mainSceneReadyAt < 0f)
            {
                return;
            }
            float now = Time.unscaledTime;
            if (now - mainSceneReadyAt < 6f || now < nextAutoJoin || CoopSaveBootstrap.Active)
            {
                return;
            }
            if (autoJoinStarted && !CoopSaveBootstrap.HasFailed)
            {
                // The world copy arrived and is loading (the game stays at MainMenu until it is in).
                playgroundStarted = true;
                return;
            }
            try
            {
                object mainGame = CoopDiagnostics.GetStatic(RequireType("MainGame"), "Instance");
                if (mainGame == null || Convert.ToString(CoopDiagnostics.GetMember(mainGame, "gameState")) != "MainMenu")
                {
                    return;
                }
                nextAutoJoin = now + 10f;
                if (playgroundSlot != null)
                {
                    Component menu = Resources.FindObjectsOfTypeAll(RequireType("UIMainMenuWindow")).OfType<Component>()
                        .FirstOrDefault(window => window != null && window.gameObject.activeInHierarchy);
                    if (menu == null)
                    {
                        return;
                    }
                    playgroundStarted = true;
                    menu.GetType().GetMethod("OnContinueButtonClicked", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(menu, null);
                    LogSource.LogInfo("Playground: pressed Continue for " + playgroundSlot + ".");
                }
                else
                {
                    CoopMenu.CopyAndJoin(autoJoinAddress);
                    autoJoinStarted = true;
                    LogSource.LogInfo("Playground: joining " + autoJoinAddress + ".");
                }
            }
            catch (Exception ex)
            {
                LogSource.LogInfo("Playground: not yet (" + Unwrap(ex).Message + "); trying again in 10 s.");
            }
        }

        private void DriveSessionAutomation()
        {
            if (!normalFlowNetworking || clientIdentityFixed || !IsNativeInitialized())
            {
                return;
            }
            object native = CoopDiagnostics.GetStatic(FindGameType("LazyNetwork"), "NetworkManager");
            if (Convert.ToBoolean(CoopDiagnostics.GetMember(native, "IsClient")) && !Convert.ToBoolean(CoopDiagnostics.GetMember(native, "IsHost")))
            {
                AttachClientToExistingWorld();
            }
        }

        private void PollNetworkState()
        {
            if (!IsNativeInitialized())
            {
                return;
            }
            object netcode = CoopDiagnostics.GetStatic(FindGameType("Unity.Netcode.NetworkManager"), "Singleton");
            object native = CoopDiagnostics.GetStatic(FindGameType("LazyNetwork"), "NetworkManager");
            object peers = CoopDiagnostics.GetMember(native, "OtherClients");
            int peerCount = peers is System.Collections.ICollection collection ? collection.Count : -1;
            string state = DescribeMemberValues(netcode, "Netcode", "IsHost", "IsClient", "IsServer", "IsListening", "LocalClientId") +
                           "; peers=" + peerCount;
            if (!string.Equals(state, lastNetworkState, StringComparison.Ordinal))
            {
                lastNetworkState = state;
                LogSource.LogInfo("Network state changed: " + state);
            }
        }

        private void TryRunConfiguredStartupAction()
        {
            if (string.Equals(startupMode.Value, "None", StringComparison.OrdinalIgnoreCase) ||
                !IsNativeInitialized() || !IsBackgroundPreloadComplete())
            {
                return;
            }

            if (string.Equals(startupMode.Value, "Host", StringComparison.OrdinalIgnoreCase))
            {
                object mainGame = CoopDiagnostics.GetStatic(RequireType("MainGame"), "Instance");
                string state = Convert.ToString(CoopDiagnostics.GetMember(mainGame, "gameState"));
                if (string.Equals(state, "MainMenu", StringComparison.Ordinal))
                {
                    return;
                }
                if (!IsNormalLocalGameReady(mainGame))
                {
                    return;
                }
                startupActionAttempted = true;
                CoopStatus.Set(CoopPhase.Hosting, L.F("Waiting for players on port {0}.", port));
                RunAction("Automatic host startup on normal game", HostExistingWorld);
            }
            else if (string.Equals(startupMode.Value, "Connect", StringComparison.OrdinalIgnoreCase))
            {
                object mainGame = CoopDiagnostics.GetStatic(RequireType("MainGame"), "Instance");
                string state = Convert.ToString(CoopDiagnostics.GetMember(mainGame, "gameState"));
                if (string.Equals(state, "MainMenu", StringComparison.Ordinal))
                {
                    return;
                }
                if (!IsNormalLocalGameReady(mainGame))
                {
                    return;
                }
                startupActionAttempted = true;
                normalFlowNetworking = true;
                CoopStatus.Set(CoopPhase.Connecting, L.F("Connecting to {0}…", address + ":" + port));
                RunAction("Automatic client connection on normal game", ConnectToHost);
            }
            else
            {
                startupActionAttempted = true;
                lastAction = "Unknown StartupMode: " + startupMode.Value;
                LogSource.LogWarning(lastAction);
            }
        }

        private static bool IsNormalLocalGameReady(object mainGame)
        {
            if (!string.Equals(Convert.ToString(CoopDiagnostics.GetMember(mainGame, "gameState")), "InGame", StringComparison.Ordinal))
            {
                return false;
            }
            object controller = CoopDiagnostics.GetMember(mainGame, "playerController");
            if (!Convert.ToBoolean(CoopDiagnostics.GetMember(controller, "IsControlsEnabled")) &&
                (relaxStartupGate == null || !relaxStartupGate.Value))
            {
                // The intro cinematic holds control until the player presses a key, so an
                // unattended test never clears this gate.
                return false;
            }
            return CoopBodies.All().Any(body => body.gameObject.activeInHierarchy);
        }

        /// <summary>
        /// Lets a choice made on the menu take effect without restarting the game. The startup
        /// driver reads the mode each time it runs, so clearing the one-shot guard is enough.
        /// </summary>
        private void ArmStartupAction()
        {
            // The menu (or a Steam invite) has just chosen where to connect: use that, not the
            // address the settings held when the game started. Without this the world was copied
            // from the chosen host and the connection that follows went to the old address — for
            // a Steam join, an IP from the settings instead of the friend's SteamID.
            address = defaultAddress.Value;
            port = defaultPort.Value.ToString();
            startupActionAttempted = false;
            connectAttempts = 0;
            connectAttemptStartedAt = -1f;
            awaitingShutdownForRetry = false;
            CoopStatus.Set(CoopPhase.Idle, string.Empty);
        }

        private bool hudGameUiFailed;
        private float nextHudPlacement;

        private void LateUpdate()
        {
            if (hudGameUiFailed)
            {
                return;
            }
            try
            {
                GameUi.RestyleOnLanguageChange();
                CoopHud.UpdateGameUi();
                CoopChat.UpdateGameUi();
                CoopProgressWindow.Update();
                if (Time.unscaledTime >= nextHudPlacement)
                {
                    nextHudPlacement = Time.unscaledTime + 0.5f;
                    GameUi.PlaceHud(IsOnMainMenu());
                }
            }
            catch (Exception ex)
            {
                hudGameUiFailed = true;
                GameUi.Enabled = false;
                LogSource.LogError("Game-styled HUD failed; using the plain look: " + Unwrap(ex));
            }
        }

        private void OnGUI()
        {
            try
            {
                CoopMenu.Draw(IsOnMainMenu());
            }
            catch (Exception ex)
            {
                CoopMenu.Enabled = false;
                LogSource.LogError("Co-op menu disabled after a drawing failure: " + Unwrap(ex));
            }

            try
            {
                CoopHud.Draw();
            }
            catch (Exception ex)
            {
                CoopHud.ShowStatus = false;
                CoopHud.ShowNameTags = false;
                LogSource.LogError("Multiplayer HUD disabled after a drawing failure: " + Unwrap(ex));
            }

            try
            {
                CoopSceneShare.DrawPlain();
            }
            catch (Exception ex)
            {
                LogSource.LogWarning("Scene share: " + Unwrap(ex));
            }

            try
            {
                CoopChat.Draw();
            }
            catch (Exception ex)
            {
                CoopChat.Enabled = false;
                LogSource.LogError("Chat disabled after a drawing failure: " + Unwrap(ex));
            }

            if (!overlayVisible)
            {
                return;
            }

            window = GUI.Window(GetInstanceID(), window, DrawWindow, "GK2 Co-op Prototype " + Version);
        }

        /// <summary>True while the game is sitting on its main menu, where setup belongs.</summary>
        private static bool IsOnMainMenu()
        {
            // Asked several times a frame (each OnGUI pass): read directly. By reflection it cost
            // about 60 microseconds a call.
            try
            {
                MainGame mainGame = MainGame.Instance;
                return mainGame != null && mainGame.gameState == MainGame.GameState.MainMenu;
            }
            catch
            {
                return false;
            }
        }

        private void DrawWindow(int id)
        {
            // Read-only: its old "Host new test world" button swapped the loaded world for a blank
            // one, which the game's next save wrote into the player's slot.
            GUILayout.Label("F8 toggles this window. F9 writes a detailed snapshot to the BepInEx log.");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh", GUILayout.Width(90f)))
            {
                RefreshReport();
            }
            GUI.enabled = CanInitialize(out _);
            if (GUILayout.Button("Initialize native network"))
            {
                RunAction("Initialize", InitializeNativeNetwork);
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            GUILayout.Label("Last action: " + lastAction);
            scroll = GUILayout.BeginScrollView(scroll, GUI.skin.box, GUILayout.ExpandHeight(true));
            GUILayout.TextArea(report, GUILayout.ExpandHeight(true));
            GUILayout.EndScrollView();
            GUI.DragWindow(new Rect(0f, 0f, window.width, 24f));
        }

        private void RunAction(string label, Action action)
        {
            try
            {
                action();
                lastAction = label + " completed.";
                LogSource.LogInfo(lastAction);
            }
            catch (Exception ex)
            {
                Exception useful = ex is TargetInvocationException && ex.InnerException != null ? ex.InnerException : ex;
                lastAction = label + " failed: " + useful.Message;
                LogSource.LogError(useful);
            }
            finally
            {
                RefreshShownReport();
            }
        }

        private void RefreshReport()
        {
            try
            {
                var lines = new List<string>
                {
                    $"Game: {Application.productName} {Application.version}",
                    $"Unity: {Application.unityVersion}",
                    $"Scene: {SceneManager.GetActiveScene().name}",
                    $"LazyNetwork initialized: {IsNativeInitialized()}",
                    DescribeSingleton("Unity.Netcode.NetworkManager", "Singleton"),
                    DescribeMemberValues(CoopDiagnostics.GetStatic(FindGameType("Unity.Netcode.NetworkManager"), "Singleton"), "Netcode", "IsHost", "IsClient", "IsServer", "IsListening", "LocalClientId"),
                    DescribeSingleton("ConnectionNotificationManager", "Singleton"),
                    "Background preload complete: " + IsBackgroundPreloadComplete(),
                    DescribeMemberValues(IsNativeInitialized() ? CoopDiagnostics.GetStatic(FindGameType("LazyNetwork"), "NetworkManager") : null, "Native manager", "IsCoopGame", "IsHost", "IsClient", "MyId", "ServerClientId"),
                    DescribeMemberValues(CoopDiagnostics.GetStatic(FindGameType("MainGame"), "Instance"), "Main game", "gameState", "IsGamePaused"),
                    DescribeMemberValues(CoopDiagnostics.GetMember(CoopDiagnostics.GetStatic(FindGameType("MainGame"), "Instance"), "playerController"), "Local player", "IsControlsEnabled", "IsPaused"),
                    DescribeObjects("LazyNetwork"),
                    DescribeObjects("UNetworkManager"),
                    DescribeObjects("GameNetworkManager"),
                    DescribeObjects("NetworkDataSync"),
                    DescribeObjects("MainGame"),
                    DescribeObjects("PlayerPhysicalBody"),
                    DescribeObjects("PlayerController")
                };

                if (!CanInitialize(out string reason) && !IsNativeInitialized())
                {
                    lines.Add("Initialization gate: " + reason);
                }

                report = string.Join("\n", lines);
            }
            catch (Exception ex)
            {
                report = "Runtime scan failed: " + ex;
                LogSource.LogError(ex);
            }
        }

        private bool CanInitialize(out string reason)
        {
            if (IsNativeInitialized())
            {
                reason = "Already initialized.";
                return false;
            }
            if (FindRuntimeObject(FindGameType("UNetworkManager")) == null)
            {
                reason = "No live UNetworkManager component was found.";
                return false;
            }
            if (CoopDiagnostics.GetStatic(FindGameType("Unity.Netcode.NetworkManager"), "Singleton") == null)
            {
                reason = "Unity Netcode NetworkManager.Singleton is absent.";
                return false;
            }
            if (CoopDiagnostics.GetStatic(FindGameType("ConnectionNotificationManager"), "Singleton") == null)
            {
                reason = "ConnectionNotificationManager.Singleton is absent.";
                return false;
            }
            reason = "Ready.";
            return true;
        }

        private void InitializeNativeNetwork()
        {
            if (!CanInitialize(out string reason))
            {
                throw new InvalidOperationException(reason);
            }

            Type lazyType = RequireType("LazyNetwork");
            Type managerType = RequireType("UNetworkManager");
            Type channelsType = RequireType("UNetworkMessageChannelManager");
            Component lazy = FindRuntimeObject(lazyType);
            if (lazy == null)
            {
                var holder = new GameObject("GK2Coop.NativeNetwork");
                DontDestroyOnLoad(holder);
                lazy = holder.AddComponent(lazyType);
            }

            Component manager = FindRuntimeObject(managerType);
            object channels = Activator.CreateInstance(channelsType);
            MethodInfo init = lazyType.GetMethod("Init", BindingFlags.Instance | BindingFlags.Public);
            if (init == null)
            {
                throw new MissingMethodException("LazyNetwork.Init was not found.");
            }
            init.Invoke(lazy, new[] { (object)manager, channels });
        }

        private void HostExistingWorld()
        {
            ushort parsedPort = ParsePort();
            object manager = CoopDiagnostics.GetStatic(RequireType("LazyNetwork"), "NetworkManager");
            bool started = (bool)Invoke(manager, "StartHostGame", address, parsedPort);
            if (!started)
            {
                throw new InvalidOperationException("Unity Netcode refused to start the host.");
            }

            Type mainGameType = RequireType("MainGame");
            object mainGame = CoopDiagnostics.GetStatic(mainGameType, "Instance");
            object save = CoopDiagnostics.GetMember(mainGame, "GameSave");
            object playerData = CoopDiagnostics.GetMember(save, "playerData");
            object netcode = CoopDiagnostics.GetStatic(RequireType("Unity.Netcode.NetworkManager"), "Singleton");
            int clientId = Convert.ToInt32(CoopDiagnostics.GetMember(netcode, "LocalClientId"));
            object networkPlayer = Activator.CreateInstance(RequireType("NetworkPlayer"), clientId, playerData);
            CoopSession.AssignStablePlayerGuid(networkPlayer);
            SetInstanceMember(save, "hostPlayer", networkPlayer);
            object commandHolder = CoopDiagnostics.GetMember(mainGame, "PlayerUniqueCommandHolder");
            Invoke(commandHolder, "RegisterData", networkPlayer);
            AttachCorrectedHostCallbacks(netcode);
            normalFlowNetworking = true;
            LogSource.LogInfo("Attached native host networking to the normally initialized game world.");
        }

        /// <summary>
        /// Creates and spawns the record for another client, so a player can see peers other than
        /// the host. A client builds records for itself and the host only; nothing in the shipped
        /// code or in this mod ever told it that a third player exists, so at three players each
        /// client saw the host and itself and the other client was simply absent.
        /// </summary>
        internal static bool EnsureRemotePlayer(int clientId, string label)
        {
            Type mainGameType = RequireType("MainGame");
            object mainGame = CoopDiagnostics.GetStatic(mainGameType, "Instance");
            object save = mainGame == null ? null : CoopDiagnostics.GetMember(mainGame, "GameSave");
            if (save == null)
            {
                return false;
            }
            var clients = CoopDiagnostics.GetMember(save, "clientPlayers") as System.Collections.IList;
            if (clients == null)
            {
                return false;
            }
            foreach (object existing in clients)
            {
                if (Convert.ToInt32(CoopDiagnostics.GetMember(existing, "clientId")) == clientId)
                {
                    return false;
                }
            }

            Type playerDataType = RequireType("PlayerData");
            object playerData = InvokeStatic(playerDataType, "CreatePlayerData");
            Invoke(playerData, "TryApplyStartState");
            object player = Activator.CreateInstance(RequireType("NetworkPlayer"), clientId, playerData);
            CoopSession.AssignStablePlayerGuid(player);
            clients.Add(player);
            Instance.SeedRemoteSpawnPosition(player, label);
            Invoke(mainGame, "SpawnPlayer", player);
            Instance.LogSource.LogInfo("Created and spawned a record for " + label + " (client " + clientId + ").");
            return true;
        }

        private void AttachClientToExistingWorld()
        {
            object netcode = CoopDiagnostics.GetStatic(RequireType("Unity.Netcode.NetworkManager"), "Singleton");
            if (!Convert.ToBoolean(CoopDiagnostics.GetMember(netcode, "IsConnectedClient")))
            {
                return;
            }

            Type mainGameType = RequireType("MainGame");
            object mainGame = CoopDiagnostics.GetStatic(mainGameType, "Instance");
            object save = CoopDiagnostics.GetMember(mainGame, "GameSave");
            object localPlayerData = CoopDiagnostics.GetMember(save, "playerData");
            int localId = Convert.ToInt32(CoopDiagnostics.GetMember(netcode, "LocalClientId"));
            Type networkPlayerType = RequireType("NetworkPlayer");
            object localPlayer = Activator.CreateInstance(networkPlayerType, localId, localPlayerData);
            CoopSession.AssignStablePlayerGuid(localPlayer);
            var clients = CoopDiagnostics.GetMember(save, "clientPlayers") as System.Collections.IList;
            // Safe to run again in the same world: no second record for us, no second host body.
            if (clients != null)
            {
                foreach (object stale in clients.Cast<object>().Where(c => ReferenceEquals(CoopDiagnostics.GetMember(c, "playerData"), localPlayerData)).ToList())
                {
                    clients.Remove(stale);
                }
            }
            object previousHost = CoopDiagnostics.GetMember(save, "hostPlayer");
            if (previousHost != null)
            {
                CoopWatchdog.DestroyBodiesOf(CoopDiagnostics.GetMember(previousHost, "playerData"));
            }
            clients?.Add(localPlayer);
            SetStaticMember(mainGameType, "PlayerData", localPlayerData);
            object commandHolder = CoopDiagnostics.GetMember(mainGame, "PlayerUniqueCommandHolder");
            Invoke(commandHolder, "RegisterData", localPlayer);

            Type playerDataType = RequireType("PlayerData");
            object hostPlayerData = InvokeStatic(playerDataType, "CreatePlayerData");
            Invoke(hostPlayerData, "TryApplyStartState");
            object hostPlayer = Activator.CreateInstance(networkPlayerType, 0, hostPlayerData);
            CoopSession.AssignStablePlayerGuid(hostPlayer);
            SetInstanceMember(save, "hostPlayer", hostPlayer);
            SeedRemoteSpawnPosition(hostPlayer, "host");
            Invoke(mainGame, "SpawnPlayer", hostPlayer);
            clientIdentityFixed = true;
            // The drops come from the host's join snapshot, all of them: the copy's are as they lay
            // when the copy was taken (one grown by a merge since kept its old count, one picked up
            // since stayed on the ground).
            CoopDropSync.ClearForJoinSnapshot();
            LogSource.LogInfo("Attached client networking to the normally initialized local game world.");
        }

        private void AttachCorrectedHostCallbacks(object netcode)
        {
            EventInfo connected = netcode.GetType().GetEvent("OnClientConnectedCallback", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            EventInfo disconnected = netcode.GetType().GetEvent("OnClientDisconnectCallback", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            MethodInfo callback = GetType().GetMethod(nameof(CorrectedHostOnClientConnected), BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo disconnectCallback = GetType().GetMethod(nameof(CorrectedHostOnClientDisconnected), BindingFlags.Instance | BindingFlags.NonPublic);
            if (connected == null || disconnected == null || callback == null || disconnectCallback == null)
            {
                throw new MissingMemberException("Could not attach the corrected host connection callbacks.");
            }
            // Hosting again in the same game (after going to the main menu) must not leave the first
            // session's callbacks attached: they fired again for the new session, twice for every
            // player, and made a player's body for the host's own client.
            DetachHostCallbacks();
            attachedHostNetcode = netcode;
            hostConnectedHandler = Delegate.CreateDelegate(connected.EventHandlerType, this, callback);
            hostDisconnectedHandler = Delegate.CreateDelegate(disconnected.EventHandlerType, this, disconnectCallback);
            connected.AddEventHandler(netcode, hostConnectedHandler);
            disconnected.AddEventHandler(netcode, hostDisconnectedHandler);
        }

        /// <summary>
        /// A freshly created remote <c>PlayerData</c> carries the new-game start position, which is
        /// the origin. <c>InitNetworkPlayer</c> places the body there, and it stays there until the
        /// peer's first <c>CharMoveCommand</c> arrives — so a peer who does not move is left at
        /// (0,0,0). Seeding the local player's position instead puts the remote body next to us,
        /// which is where the first replicated update would have put it anyway.
        /// </summary>
        private void SeedRemoteSpawnPosition(object networkPlayer, string label)
        {
            if (!seedRemoteSpawnPosition.Value || networkPlayer == null)
            {
                return;
            }
            try
            {
                object localPlayerData = CoopDiagnostics.GetStatic(RequireType("MainGame"), "PlayerData");
                object remotePlayerData = CoopDiagnostics.GetMember(networkPlayer, "playerData");
                if (localPlayerData == null || remotePlayerData == null)
                {
                    return;
                }
                Vector3 local = CoopDiagnostics.ReadPositionValue(localPlayerData);
                object remotePosition = CoopDiagnostics.GetMember(remotePlayerData, "position");
                if (remotePosition == null)
                {
                    return;
                }
                // Set before SpawnPlayer: no body is subscribed yet, so this seeds the value that
                // InitNetworkPlayer reads rather than driving a replicated move.
                SetInstanceMember(remotePosition, "Value", local);
                LogSource.LogInfo("Seeded spawn position for " + label + " to " + CoopDiagnostics.Format(local) + ".");
            }
            catch (Exception ex)
            {
                LogSource.LogWarning("Could not seed the remote spawn position for " + label + ": " + Unwrap(ex).Message);
            }
        }

        private void CorrectedHostOnClientConnected(ulong clientId)
        {
            // The host's own client is the host player, never another player's body (found in play:
            // hosting a second time in one game showed a third keeper called "Host").
            NetworkManager self = NetworkManager.Singleton;
            if (clientId == NetworkManager.ServerClientId || (self != null && clientId == self.LocalClientId))
            {
                return;
            }
            object mainGame = CoopDiagnostics.GetStatic(RequireType("MainGame"), "Instance");
            object save = CoopDiagnostics.GetMember(mainGame, "GameSave");
            Invoke(save, "CreateClient", checked((int)clientId));

            Type lobbyType = RequireType("LobbyHelper");
            FieldInfo clientsField = lobbyType.GetField("connectedClients", BindingFlags.Static | BindingFlags.NonPublic);
            if (!(clientsField?.GetValue(null) is System.Collections.IDictionary clients))
            {
                throw new MissingFieldException(lobbyType.FullName, "connectedClients");
            }
            // Indexed, not Add: an entry left from an earlier session with the same id threw here and
            // stopped the rest of the join bookkeeping.
            clients[clientId] = false;

            object onClientAdded = CoopDiagnostics.GetStatic(lobbyType, "OnClientAdded");
            (onClientAdded as Delegate)?.DynamicInvoke(clientId);
            if (normalFlowNetworking && string.Equals(Convert.ToString(CoopDiagnostics.GetMember(mainGame, "gameState")), "InGame", StringComparison.Ordinal))
            {
                MethodInfo getClient = save.GetType().GetMethod("GetClient", BindingFlags.Instance | BindingFlags.Public);
                object[] args = { checked((int)clientId), null, false };
                if (getClient != null && (bool)getClient.Invoke(save, args))
                {
                    CoopSession.AssignStablePlayerGuid(args[1]);
                    SeedRemoteSpawnPosition(args[1], "client " + clientId);
                    Invoke(mainGame, "SpawnPlayer", args[1]);
                }
            }
            LogSource.LogInfo("Created deferred remote player record for client " + clientId + ".");
        }

        private void CorrectedHostOnClientDisconnected(ulong clientId)
        {
            if (clientId == NetworkManager.ServerClientId)
            {
                return;
            }

            object mainGame = CoopDiagnostics.GetStatic(RequireType("MainGame"), "Instance");
            object save = CoopDiagnostics.GetMember(mainGame, "GameSave");
            var clientPlayers = CoopDiagnostics.GetMember(save, "clientPlayers") as System.Collections.IList;
            object disconnectedPlayer = null;
            if (clientPlayers != null)
            {
                foreach (object player in clientPlayers)
                {
                    if (Convert.ToUInt64(CoopDiagnostics.GetMember(player, "clientId")) == clientId)
                    {
                        disconnectedPlayer = player;
                        break;
                    }
                }
            }

            if (disconnectedPlayer != null)
            {
                object playerData = CoopDiagnostics.GetMember(disconnectedPlayer, "playerData");
                Type bodyType = RequireType("PlayerPhysicalBody");
                foreach (Component body in Resources.FindObjectsOfTypeAll(bodyType).OfType<Component>())
                {
                    if (ReferenceEquals(CoopDiagnostics.GetMember(body, "playerData"), playerData))
                    {
                        Destroy(body.gameObject);
                    }
                }
                clientPlayers.Remove(disconnectedPlayer);
            }

            Type lobbyType = RequireType("LobbyHelper");
            FieldInfo clientsField = lobbyType.GetField("connectedClients", BindingFlags.Static | BindingFlags.NonPublic);
            (clientsField?.GetValue(null) as System.Collections.IDictionary)?.Remove(clientId);
            // A friend who copied the world goes away and opens it, then connects again.
            string name = CoopSaveBootstrap.TakeDownloader(clientId, out string downloaderName, out bool loading) && !CoopSession.KnowsName(clientId)
                ? downloaderName
                : CoopSession.NameFor(clientId);
            CoopSession.AnnouncePeerLeft(clientId, name, loading);
            CoopSession.ForgetPeer(clientId);
            CoopPlayerProfiles.ForgetClient(clientId);
            CoopSceneSync.Forget(clientId);
            CoopStatus.Announce(loading ? L.F("{0} is loading the world…", name) : L.F("{0} left.", name));
            LogSource.LogInfo($"Cleaned up disconnected client {clientId}; remaining remote records={clientPlayers?.Count ?? 0}.");
            CancelInvoke(nameof(LogSnapshot));
            Invoke(nameof(LogSnapshot), 1f);
        }

        /// <summary>
        /// At the end of a joined session. The next connection (joining again in the same game,
        /// after the main menu) must attach its world again; this stayed set from the first join,
        /// so the second one never made the records for itself and the host — found by the
        /// re-host test, and what a friend saw after rejoining without restarting the game.
        /// </summary>
        internal void ForgetJoinedWorld()
        {
            clientIdentityFixed = false;
        }

        internal void DetachHostCallbacks()
        {
            if (attachedHostNetcode == null)
            {
                return;
            }
            Type netcodeType = attachedHostNetcode.GetType();
            if (hostConnectedHandler != null)
            {
                netcodeType.GetEvent("OnClientConnectedCallback")?.RemoveEventHandler(attachedHostNetcode, hostConnectedHandler);
            }
            if (hostDisconnectedHandler != null)
            {
                netcodeType.GetEvent("OnClientDisconnectCallback")?.RemoveEventHandler(attachedHostNetcode, hostDisconnectedHandler);
            }
            attachedHostNetcode = null;
            hostConnectedHandler = null;
            hostDisconnectedHandler = null;
        }

        private void ConnectToHost()
        {
            ushort parsedPort = ParsePort();
            object manager = CoopDiagnostics.GetStatic(RequireType("LazyNetwork"), "NetworkManager");
            bool started = (bool)Invoke(manager, "ConnectToHost", address, parsedPort);
            if (!started)
            {
                throw new InvalidOperationException("Unity Netcode refused to start the client.");
            }
            connectAttempts++;
            connectAttemptStartedAt = Time.unscaledTime;
        }

        /// <summary>
        /// Starting a client only means Netcode accepted the call. The connection itself succeeds
        /// or fails asynchronously — the game logs "Failed to connect to server" a few seconds
        /// later and falls back to OfflineState. Without a retry a single miss is terminal, which
        /// is what made reconnecting after a disconnect fail: the relaunched client attempted
        /// once, lost the race, and then sat in a world of its own forever.
        ///
        /// Retrying takes two frames on purpose. <c>NetworkManager.Shutdown</c> is asynchronous,
        /// and starting a client while it is still tearing down is refused outright — the first
        /// version of this retry did exactly that and logged "Unity Netcode refused to start the
        /// client" on every attempt.
        /// </summary>
        private void DriveConnectRetry()
        {
            if (connectAttemptStartedAt < 0f ||
                !string.Equals(startupMode.Value, "Connect", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            NetworkManager netcode = NetworkManager.Singleton;
            if (netcode != null && netcode.IsConnectedClient)
            {
                if (connectAttemptStartedAt >= 0f)
                {
                    CoopStatus.Set(CoopPhase.Connected, L.F("Connected to {0}.", address + ":" + ParsePortOrZero()));
                }
                connectAttemptStartedAt = -1f;
                awaitingShutdownForRetry = false;
                return;
            }

            if (awaitingShutdownForRetry)
            {
                if (netcode != null && (netcode.ShutdownInProgress || netcode.IsListening))
                {
                    return;
                }
                awaitingShutdownForRetry = false;
                RunAction("Client connection retry " + connectAttempts, ConnectToHost);
                return;
            }

            if (Time.unscaledTime - connectAttemptStartedAt < Math.Max(2f, connectRetrySeconds.Value))
            {
                return;
            }
            if (connectAttempts > connectRetries.Value)
            {
                connectAttemptStartedAt = -1f;
                lastAction = "Gave up connecting after " + connectAttempts + " attempts.";
                string advice = CoopStatus.DescribeUnreachable(address, ParsePortOrZero(), connectAttempts);
                CoopStatus.Set(CoopPhase.Failed, advice);
                // The status window says it; only the plain look needs the notice as well.
                if (!GameUi.Ready) CoopStatus.Announce(L.F("Could not join. {0}", advice), true, 20f);
                LogSource.LogWarning(lastAction + " " + advice);
                return;
            }

            CoopStatus.Set(CoopPhase.Connecting, L.F("No reply from the host. Trying again ({0} of {1})…", connectAttempts, connectRetries.Value));
            CoopStatus.Announce(L.F("No reply from the host. Trying again ({0} of {1})…", connectAttempts, connectRetries.Value), true);
            LogSource.LogInfo("Connection attempt " + connectAttempts + " did not reach the host; retrying.");
            connectAttemptStartedAt = Time.unscaledTime;
            try
            {
                if (netcode != null && (netcode.IsListening || netcode.IsClient))
                {
                    // Torn down first: a failed attempt leaves the transport half-configured.
                    netcode.Shutdown();
                }
                awaitingShutdownForRetry = true;
            }
            catch (Exception ex)
            {
                LogSource.LogWarning("Could not shut down the failed client before retrying: " + ex.Message);
                awaitingShutdownForRetry = true;
            }
        }

        /// <summary>The configured port for a message, without throwing on a bad value.</summary>
        private int ParsePortOrZero()
        {
            ushort parsed;
            return ushort.TryParse(port, out parsed) ? parsed : 0;
        }

        private ushort ParsePort()
        {
            if (!ushort.TryParse(port, out ushort parsedPort))
            {
                throw new ArgumentException("Port must be between 0 and 65535.");
            }
            defaultAddress.Value = address;
            defaultPort.Value = parsedPort;
            return parsedPort;
        }

        private static bool IsNativeInitialized()
        {
            Type type = FindGameType("LazyNetwork");
            object value = CoopDiagnostics.GetStatic(type, "IsInitialized");
            return value is bool result && result;
        }

        private static bool IsBackgroundPreloadComplete()
        {
            try
            {
                if (FindRuntimeObject(FindGameType("MainGame")) == null)
                {
                    return false;
                }
                Type pipelineType = FindGameType("LoadingPipeline");
                Type stageType = FindGameType("LoadingStage");
                object pipeline = CoopDiagnostics.GetStatic(pipelineType, "Instance");
                if (pipeline == null || stageType == null)
                {
                    return false;
                }

                object stage = Enum.Parse(stageType, "BackgroundPreload");
                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                MethodInfo hasStage = pipelineType.GetMethod("HasStage", flags);
                MethodInfo complete = pipelineType.GetMethod("IsStageComplete", flags);
                return hasStage != null && complete != null &&
                       (bool)hasStage.Invoke(pipeline, new[] { stage }) &&
                       (bool)complete.Invoke(pipeline, new[] { stage });
            }
            catch
            {
                return false;
            }
        }

        private static string DescribeSingleton(string typeName, string member)
        {
            Type type = FindGameType(typeName);
            object singleton = CoopDiagnostics.GetStatic(type, member);
            return typeName + "." + member + ": " + DescribeObject(singleton as UnityEngine.Object);
        }

        private static string DescribeObjects(string typeName)
        {
            Type type = FindGameType(typeName);
            if (type == null)
            {
                return typeName + ": type missing";
            }
            var objects = Resources.FindObjectsOfTypeAll(type).OfType<UnityEngine.Object>().ToArray();
            string details = string.Join(", ", objects.Take(5).Select(DescribeObject).ToArray());
            return $"{typeName}: {objects.Length} [{details}]";
        }

        private static string DescribeObject(UnityEngine.Object obj)
        {
            if (obj == null)
            {
                return "null";
            }
            var component = obj as Component;
            if (component == null)
            {
                return obj.name;
            }
            GameObject go = component.gameObject;
            return $"{go.name}; active={go.activeInHierarchy}; scene={go.scene.name}; hide={go.hideFlags}";
        }

        private static string DescribeMemberValues(object target, string label, params string[] names)
        {
            if (target == null)
            {
                return label + ": null";
            }
            var values = new List<string>();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (string name in names)
            {
                try
                {
                    PropertyInfo property = target.GetType().GetProperty(name, flags);
                    FieldInfo field = target.GetType().GetField(name, flags);
                    object value = property != null ? property.GetValue(target, null) : field?.GetValue(target);
                    values.Add(name + "=" + (value ?? "null"));
                }
                catch (Exception ex)
                {
                    values.Add(name + "=<" + ex.GetType().Name + ">");
                }
            }
            return label + ": " + string.Join(", ", values.ToArray());
        }

        private static Component FindRuntimeObject(Type type)
        {
            if (type == null)
            {
                return null;
            }
            return Resources.FindObjectsOfTypeAll(type)
                .OfType<Component>()
                .Where(c => c.gameObject.scene.IsValid())
                .OrderByDescending(c => c.gameObject.activeInHierarchy)
                .FirstOrDefault();
        }

        private static Type RequireType(string name)
        {
            Type type = FindGameType(name);
            if (type == null)
            {
                throw new TypeLoadException("Game type not found: " + name);
            }
            return type;
        }

        private static readonly Dictionary<string, KeyValuePair<Type, int>> typeLookups = new Dictionary<string, KeyValuePair<Type, int>>();

        /// <summary>
        /// A game type by name, looked up once (a type not found, again after 30 s): the ticks
        /// ask for the same few types every second, and each search went through every loaded
        /// assembly, a miss through every type in them.
        /// </summary>
        internal static Type FindGameType(string name)
        {
            int now = Environment.TickCount;
            lock (typeLookups)
            {
                if (typeLookups.TryGetValue(name, out KeyValuePair<Type, int> known) &&
                    (known.Key != null || now - known.Value < 0))
                {
                    return known.Key;
                }
            }
            Type found = SearchType(name);
            lock (typeLookups)
            {
                typeLookups[name] = new KeyValuePair<Type, int>(found, now + 30000);
            }
            return found;
        }

        private static Type SearchType(string name)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(name, false);
                if (type != null)
                {
                    return type;
                }
            }
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    Type type = assembly.GetTypes().FirstOrDefault(candidate => candidate.Name == name);
                    if (type != null)
                    {
                        return type;
                    }
                }
                catch (ReflectionTypeLoadException ex)
                {
                    Type type = ex.Types.FirstOrDefault(candidate => candidate != null && candidate.Name == name);
                    if (type != null)
                    {
                        return type;
                    }
                }
            }
            return null;
        }

        private static void SetStaticMember(Type type, string name, object value)
        {
            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            PropertyInfo property = type.GetProperty(name, flags);
            if (property != null)
            {
                MethodInfo setter = property.GetSetMethod(true);
                if (setter != null)
                {
                    setter.Invoke(null, new[] { value });
                    return;
                }
            }
            FieldInfo field = type.GetField(name, flags) ?? type.GetField("<" + name + ">k__BackingField", flags);
            if (field == null)
            {
                throw new MissingFieldException(type.FullName, name);
            }
            field.SetValue(null, value);
        }

        private static void SetInstanceMember(object target, string name, object value)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            PropertyInfo property = target.GetType().GetProperty(name, flags);
            MethodInfo setter = property?.GetSetMethod(true);
            if (setter != null)
            {
                setter.Invoke(target, new[] { value });
                return;
            }
            FieldInfo field = target.GetType().GetField(name, flags) ?? target.GetType().GetField("<" + name + ">k__BackingField", flags);
            if (field == null)
            {
                throw new MissingFieldException(target.GetType().FullName, name);
            }
            field.SetValue(target, value);
        }

        private static void SetEntryScene(Type mainGameType)
        {
            object entryScene = CoopDiagnostics.GetStatic(mainGameType, "EntrySceneToLoad");
            FieldInfo field = mainGameType.GetField("entrySceneToLoadCached", BindingFlags.Static | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new MissingFieldException(mainGameType.FullName, "entrySceneToLoadCached");
            }
            field.SetValue(null, entryScene);
        }

        private static object Invoke(object target, string method, params object[] args)
        {
            if (target == null)
            {
                throw new NullReferenceException("Invocation target for " + method + " is null.");
            }
            MethodInfo found = target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (found == null)
            {
                throw new MissingMethodException(target.GetType().FullName, method);
            }
            return found.Invoke(target, args);
        }

        private static object InvokeStatic(Type type, string method, params object[] args)
        {
            MethodInfo found = type.GetMethod(method, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (found == null)
            {
                throw new MissingMethodException(type.FullName, method);
            }
            return found.Invoke(null, args);
        }

        internal static Exception Unwrap(Exception exception)
        {
            while (exception is TargetInvocationException && exception.InnerException != null)
            {
                exception = exception.InnerException;
            }
            return exception;
        }
    }
}
