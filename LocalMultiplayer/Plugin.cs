using System.Collections;
using MelonLoader;
using UnityEngine;
using Object = UnityEngine.Object;
using LocalMultiplayer;

#if MONO
using HarmonyLib;
using System.Reflection;
using FishNet;
using FishNet.Component.Scenes;
using FishNet.Transporting;
using FishNet.Transporting.Multipass;
using FishNet.Transporting.Tugboat;
using ScheduleOne.Audio;
using ScheduleOne.Persistence;
using ScheduleOne.PlayerScripts;
using ScheduleOne.UI;
#else
using Il2CppFishNet;
using Il2CppFishNet.Component.Scenes;
using Il2CppFishNet.Transporting;
using Il2CppFishNet.Transporting.Multipass;
using Il2CppFishNet.Transporting.Tugboat;
using Il2CppScheduleOne.Audio;
using Il2CppScheduleOne.Persistence;
using Il2CppScheduleOne.Persistence.Loaders;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.UI;
#endif

[assembly:
    MelonInfo(typeof(LocalMultiplayer.LocalMultiplayer), BuildInfo.Name, BuildInfo.Version,
        BuildInfo.Author, BuildInfo.DownloadLink)]
[assembly: HarmonyDontPatchAll]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace LocalMultiplayer;

public static class BuildInfo
{
    public const string Name = "LocalMultiplayer";
    public const string Author = "Skippeh (ported by k073l)";
    public const string Version = "1.0";
    public const string DownloadLink = null;
}

public class LocalMultiplayer : MelonMod
{
    internal static MelonLogger.Instance Logger = null!;
    internal static LaunchArguments? LaunchArguments { get; private set; }

    private static HarmonyLib.Harmony? harmony;

    public override void OnInitializeMelon()
    {
        Logger = LoggerInstance;

        harmony = new HarmonyLib.Harmony("com.skipcast.localmultiplayer");
        harmony.PatchAll();

        ParseLaunchArguments();

        if (LaunchArguments?.SetWindowPositionSize == true)
        {
            SetWindowPositionSize();
        }

        if (LaunchArguments?.Host == true)
        {
            Console.Title += " (SERVER)";
        }
        else if (LaunchArguments?.Join == true)
        {
            Console.Title += " (CLIENT)";
        }

        Logger.Msg($"Mod {BuildInfo.Name} is loaded!");
    }

    public override void OnSceneWasLoaded(int buildIndex, string sceneName)
    {
        if (sceneName == "Menu")
        {
            if (Object.FindObjectOfType<MenuComponent>() != null)
                return;
            var go = new GameObject("LocalMultiplayerSpawner");
            go.AddComponent<MenuComponent>();

            if (Object.FindObjectOfType<AudioComponent>() == null)
            {
                var go2 = new GameObject("LocalMultiplayerAudioController");
                go2.AddComponent<AudioComponent>();
                Object.DontDestroyOnLoad(go2);
            }
        }
    }

    private void ParseLaunchArguments()
    {
        var args = Environment.GetCommandLineArgs();
        var launchArgs = new LaunchArguments();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--host":
                case "-h":
                    launchArgs.Host = true;
                    break;
                case "--join":
                case "-j":
                    launchArgs.Join = true;
                    break;
                case "--adjust-window":
                case "-s":
                    launchArgs.SetWindowPositionSize = true;
                    break;
                case "--left-offset":
                case "-o":
                    if (i + 1 < args.Length && int.TryParse(args[i + 1], out int offset))
                    {
                        launchArgs.LeftOffset = offset;
                        i++;
                    }

                    break;
            }
        }

        LaunchArguments = launchArgs;
    }

    public static void SetWindowPositionSize()
    {
        if (LaunchArguments == null)
            return;

        int offsetLeft = Math.Max(0, LaunchArguments.LeftOffset);
#if !MONO
        var mainDisplay = Screen.GetMainWindowDisplayInfo();
        RectInt workArea = mainDisplay.workArea;
#else
        var getDisplayInfoMethod = AccessTools.Method(typeof(Screen), "GetMainWindowDisplayInfo", Type.EmptyTypes);
        if (getDisplayInfoMethod == null)
        {
            Logger.Warning("Could not find Screen.GetMainWindowDisplayInfo()");
            return;
        }

        var mainDisplay = getDisplayInfoMethod.Invoke(null, null);
        if (mainDisplay == null)
        {
            Logger.Warning("Failed to get main display info");
            return;
        }

        var workAreaField = mainDisplay.GetType().GetField("workArea", BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
        if (workAreaField == null)
        {
            Logger.Warning("Could not access workArea field");
            return;
        }

        RectInt workArea = (RectInt)workAreaField.GetValue(mainDisplay)!;
#endif
        if (offsetLeft > workArea.width)
        {
            Logger.Warning("Left offset is greater than the width of the screen, cancelling window adjustment");
            return;
        }

        workArea.width -= offsetLeft;
        var windowSize = new Vector2Int(workArea.width / 2, workArea.height);
        Vector2Int position = Vector2Int.zero;

        if (LaunchArguments.Host)
            position.x = offsetLeft;
        else if (LaunchArguments.Join)
            position.x = (workArea.width / 2) + offsetLeft;

        Screen.fullScreenMode = FullScreenMode.Windowed;
        Screen.fullScreen = false;
        Screen.SetResolution(windowSize.x, windowSize.y, fullscreen: false);
        var mainDisplayInfo = (DisplayInfo)mainDisplay;
#if MONO
        Screen.MoveMainWindowTo(mainDisplayInfo, position);
#else
        Screen.MoveMainWindowTo(ref mainDisplayInfo, position);
#endif
    }

    // moved from MenuComponent, doesn't play nicely there with Il2Cpp conversion
        public static IEnumerator HostOrJoinServer(SaveInfo? save, bool host)
    {
        if (save == null && host)
            throw new ArgumentException("Need to specify a save if hosting");

        MusicPlayer.Instance.StopAndDisableTracks();

        if (host)
        {
            LoadManager.Instance.ActiveSaveInfo = save;
            LoadManager.Instance.IsLoading = true;
            LoadManager.Instance.TimeSinceGameLoaded = 0;
            LoadManager.Instance.LoadedGameFolderPath = save!.SavePath;
            LoadManager.Instance.LoadStatus = LoadManager.ELoadStatus.LoadingScene;
        }
        else
        {
            LoadManager.Instance.ActiveSaveInfo = null;
            LoadManager.Instance.IsLoading = true;
            LoadManager.Instance.TimeSinceGameLoaded = 0;
            LoadManager.instance.LoadedGameFolderPath = string.Empty;
            LoadManager.Instance.LoadStatus = LoadManager.ELoadStatus.LoadingScene;
        }

        LoadingScreen.Instance.Open(loadingTutorial: false);

        LoadManager.Instance.onPreSceneChange?.Invoke();
        LoadManager.Instance.CleanUp();

        if (host)
        {
            LoadManager.Instance.StoredSaveInfo = null;
            InstanceFinder.NetworkManager.gameObject.GetComponent<DefaultScene>().SetOnlineScene("Main");

            var sceneLoadTask = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("Main");

            while (!sceneLoadTask.isDone)
            {
                yield return new WaitForEndOfFrame();
            }

            Logger.Msg("Main scene loaded");
        }
        else
        {
            InstanceFinder.TransportManager.GetTransport<Multipass>().SetClientTransport<Tugboat>();
            InstanceFinder.NetworkManager.ClientManager.OnClientConnectionState +=
                new Action<ClientConnectionStateArgs>(ClientDone);
            InstanceFinder.NetworkManager.ClientManager.StartConnection();

            void ClientDone(ClientConnectionStateArgs args)
            {
                if (args.ConnectionState != LocalConnectionState.Started)
                {
                    if (args.ConnectionState == LocalConnectionState.Stopped)
                    {
                        Logger.Error($"Failed to connect client: {args.ConnectionState}");
                        LoadingScreen.Instance.Close();
                        LoadManager.Instance.ExitToMenu();
                        var coro = MenuComponent.clientCoroutine;
                        if (coro != null)
                            MelonCoroutines.Stop(coro);
                        MenuComponent.clientCoroutine = MelonCoroutines.Start(LocalMultiplayer.HostOrJoinServer(save: null, host: false));
                    }
                    else
                    {
                        Logger.Msg($"Client status: {args.ConnectionState}");
                    }

                    return;
                }
                
                InstanceFinder.NetworkManager.ClientManager.OnClientConnectionState -= new Action<ClientConnectionStateArgs>(ClientDone);
                Logger.Msg("Client connected");
            }


            Logger.Msg("Waiting for main scene to load...");
            yield return new WaitUntil((Func<bool>)(() => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Main"));
            Logger.Msg("Main scene loaded");
        }

        LoadManager.Instance.LoadStatus = LoadManager.ELoadStatus.Initializing;
        LoadManager.Instance.onPreLoad?.Invoke();

        if (host)
        {
            Logger.Msg("Starting server...");

            InstanceFinder.ServerManager.OnServerConnectionState += new Action<ServerConnectionStateArgs>(ServerDone);
            InstanceFinder.ServerManager.StartConnection();

            void ServerDone(ServerConnectionStateArgs args)
            {
                if (args.ConnectionState != LocalConnectionState.Started)
                {
                    if (args.ConnectionState == LocalConnectionState.Stopped)
                    {
                        Logger.Error($"Failed to start server: {args.ConnectionState}");
                        InstanceFinder.ServerManager.OnServerConnectionState -= new Action<ServerConnectionStateArgs>(ServerDone);
                        LoadingScreen.Instance.Close();
                        LoadManager.Instance.ExitToMenu();
                        var coro = MenuComponent.serverCoroutine;
                        if (coro != null)
                            MelonCoroutines.Stop(coro);
                        MenuComponent.serverCoroutine = MelonCoroutines.Start(LocalMultiplayer.HostOrJoinServer(save: null, host: false));
                    }
                    else
                    {
                        Logger.Msg($"Server status: {args.ConnectionState}");
                    }

                    return;
                }

                Logger.Msg("Server started");
                InstanceFinder.ServerManager.OnServerConnectionState -= new Action<ServerConnectionStateArgs>(ServerDone);

                // Connect client
                InstanceFinder.TransportManager.GetTransport<Multipass>().SetClientTransport<Tugboat>();
                InstanceFinder.NetworkManager.ClientManager.OnClientConnectionState += new Action<ClientConnectionStateArgs>(ClientDone);
                InstanceFinder.NetworkManager.ClientManager.StartConnection();

                void ClientDone(ClientConnectionStateArgs args)
                {
                    if (args.ConnectionState != LocalConnectionState.Started)
                    {
                        Logger.Msg($"Client connection state: {args.ConnectionState}");

                        if (args.ConnectionState == LocalConnectionState.Stopped)
                        {
                            InstanceFinder.NetworkManager.ClientManager.OnClientConnectionState -= new Action<ClientConnectionStateArgs>(ClientDone);
                            Logger.Error($"Failed to connect client");
                            LoadingScreen.Instance.Close();
                            LoadManager.Instance.ExitToMenu();
                            var coro = MenuComponent.clientCoroutine;
                            if (coro != null)
                                MelonCoroutines.Stop(coro);
                            MenuComponent.clientCoroutine = MelonCoroutines.Start(LocalMultiplayer.HostOrJoinServer(save: null, host: false));
                        }
                        return;
                    }

                    Logger.Msg("Client connected");
                    InstanceFinder.NetworkManager.ClientManager.OnClientConnectionState -= new Action<ClientConnectionStateArgs>(ClientDone);
                }
            }

            yield return new WaitUntil((Func<bool>)(() => InstanceFinder.IsServer && InstanceFinder.IsClient));
        }

        Logger.Msg("Waiting for client to initialize...");
        yield return new WaitUntil((Func<bool>)(() => InstanceFinder.IsClient && InstanceFinder.ClientManager.Connection.IsValid));
        Logger.Msg($"Client network initialized (connected to {InstanceFinder.ClientManager.Connection} with client id {InstanceFinder.ClientManager.Connection.ClientId})");

        Logger.Msg("Waiting for local player to spawn...");
        LoadManager.Instance.LoadStatus = LoadManager.ELoadStatus.SpawningPlayer;
        yield return new WaitUntil((Func<bool>)(() => Player.Local != null));
        Logger.Msg("Local player spawned");

        if (host)
        {
            LoadManager.Instance.LoadStatus = LoadManager.ELoadStatus.LoadingData;
            yield return LoadSave(save!);

            IEnumerator LoadSave(SaveInfo save)
            {
                Logger.Msg("Loading save...");

                foreach (IBaseSaveable saveable in SaveManager.Instance.BaseSaveables)
                {
#if MONO
                    new LoadRequest(Path.Combine(LoadManager.Instance.LoadedGameFolderPath, saveable.SaveFolderName), saveable.Loader);
#else
                    var savableAs = saveable.Cast<ISaveable>();
                    var gameFolder = LoadManager.Instance.LoadedGameFolderPath;
                    var saveFolderName = savableAs.SaveFolderName;      
                    var loader = savableAs.Loader;
                    new LoadRequest(Path.Combine(gameFolder, saveFolderName), loader);
#endif
                }
                while (LoadManager.Instance.loadRequests.Count > 0)
                {
                    for (int index = 0; index < 50 && LoadManager.Instance.loadRequests.Count > 0; ++index)
                    {
#if MONO
                        LoadRequest loadRequest = LoadManager.Instance.loadRequests[0];
#else
                        LoadRequest loadRequest = LoadManager.Instance.loadRequests._items[0];
#endif
                        try
                        {
                            loadRequest.Complete();
                        }
                        catch (Exception ex)
                        {
                            Logger.Error($"LOAD ERROR for load request: {loadRequest.Path} : {ex.Message}\nSite: {ex.TargetSite?.ToString()}");
#if MONO
                            if (LoadManager.Instance.loadRequests.FirstOrDefault() == loadRequest)
#else
                            if (LoadManager.Instance.loadRequests._items.FirstOrDefault() == loadRequest)
#endif
                            {
                                LoadManager.Instance.loadRequests.RemoveAt(0);
                            }
                        }
                    }

                    yield return new WaitForEndOfFrame();
                }

                for (int i = 0; i < 2; ++i)
                {
                    yield return new WaitForEndOfFrame();
                }

                LoadManager.Instance.onLoadComplete?.Invoke();
                Logger.Msg("Save loaded");
            }
        }
        else
        {
            LoadManager.Instance.onLoadComplete?.Invoke();
        }

        LoadManager.Instance.LoadStatus = LoadManager.ELoadStatus.None;
        LoadingScreen.Instance.Close();
        LoadManager.Instance.IsLoading = false;
        LoadManager.Instance.IsGameLoaded = true;
    }
}

internal class LaunchArguments
{
    public bool Host { get; set; }

    public bool Join { get; set; }

    public bool SetWindowPositionSize { get; set; }

    public int LeftOffset { get; set; }
}