using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using FishNet;
using FishNet.Component.Scenes;
using FishNet.Managing.Scened;
using FishNet.Transporting;
using FishNet.Transporting.Multipass;
using FishNet.Transporting.Tugboat;
using FishNet.Transporting.Yak;
using ScheduleOne.Audio;
using ScheduleOne.DevUtilities;
using ScheduleOne.Networking;
using ScheduleOne.Persistence;
using ScheduleOne.PlayerScripts;
using ScheduleOne.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace LocalMultiplayer;

public class MenuComponent : MonoBehaviour
{
    private GameObject? instructionsUi;
    private TextMeshProUGUI? text;

    void Awake()
    {
        CreateMainMenuUi();
        DontDestroyOnLoad(gameObject);
    }

    private void CreateMainMenuUi()
    {
        var mainMenu = GameObject.Find("MainMenu");

        instructionsUi = new GameObject("LocalMultiplayerInstructions", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        instructionsUi.transform.SetParent(mainMenu.transform);
        var transform = (RectTransform)instructionsUi.transform;
        text = instructionsUi.GetComponent<TextMeshProUGUI>();
        text.autoSizeTextContainer = true;

        transform.anchoredPosition = new Vector2(0, 0);
    }

    private void Update()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "Menu")
            return;

        UpdateInstructionText();

        if (Input.GetKeyDown(KeyCode.F5))
        {
            StartHosting();
        }
        else if (Input.GetKeyDown(KeyCode.F6))
        {
            ConnectToLocalhost();
        }
    }

    private SaveInfo? GetLastSave() => LoadManager.LastPlayedGame;

    private void UpdateInstructionText()
    {
        if (text == null)
            return;

        var save = GetLastSave();

        var builder = new StringBuilder();

        if (save == null)
        {
            builder.AppendLine("Can not host server, no save found (finish tutorial if it's not completed)");
        }
        else
        {
            builder.AppendLine($"Press F5 to host using save: \"{save.OrganisationName}\" - Net worth: {save.Networth:#,#}");
        }

        builder.AppendLine("Press F6 to connect to localhost");
        text.text = builder.ToString();
    }

    private void StartHosting()
    {
        Plugin.Logger.LogInfo("Starting server...");
        PreChecks();

        var save = LoadManager.SaveGames.FirstOrDefault(save => save != null && !save.MetaData.PlayTutorial);

        if (save == null)
        {
            Plugin.Logger.LogError("No save game found (finish tutorial if it's not completed)");
            return;
        }

        StartCoroutine(HostOrJoinServer(save, host: true));
    }

    private void ConnectToLocalhost()
    {
        Plugin.Logger.LogInfo("Connecting to localhost...");
        PreChecks();

        StartCoroutine(HostOrJoinServer(save: null, host: false));
    }

    private void PreChecks()
    {
        if (Lobby.Instance.IsInLobby)
        {
            Plugin.Logger.LogInfo("Leaving current lobby");
            Lobby.Instance.LeaveLobby();
        }

        if (InstanceFinder.IsServer)
        {
            InstanceFinder.NetworkManager.ServerManager.StopConnection(sendDisconnectMessage: false);
        }
        else if (InstanceFinder.IsClient)
        {
            InstanceFinder.NetworkManager.ClientManager.StopConnection();
        }
    }

    private IEnumerator HostOrJoinServer(SaveInfo? save, bool host)
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
            LoadManager.LoadHistory.Add("Loading game: " + save.OrganisationName);
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

            Plugin.Logger.LogInfo("Main scene loaded");
        }
        else
        {
            InstanceFinder.TransportManager.GetTransport<Multipass>().SetClientTransport<Tugboat>();
            InstanceFinder.NetworkManager.ClientManager.OnClientConnectionState += ClientDone;
            InstanceFinder.NetworkManager.ClientManager.StartConnection();

            void ClientDone(ClientConnectionStateArgs args)
            {
                if (args.ConnectionState != LocalConnectionState.Started)
                {
                    if (args.ConnectionState == LocalConnectionState.Stopped)
                    {
                        Plugin.Logger.LogError($"Failed to connect client: {args.ConnectionState}");
                    }
                    else
                    {
                        Plugin.Logger.LogInfo($"Client status: {args.ConnectionState}");
                    }

                    return;
                }

                InstanceFinder.NetworkManager.ClientManager.OnClientConnectionState -= ClientDone;
                Plugin.Logger.LogInfo("Client connected");
            }


            Plugin.Logger.LogInfo("Waiting for main scene to load...");
            yield return new WaitUntil(() => UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Main");
            Plugin.Logger.LogInfo("Main scene loaded");
        }

        LoadManager.Instance.LoadStatus = LoadManager.ELoadStatus.Initializing;
        LoadManager.Instance.onPreLoad?.Invoke();

        if (host)
        {
            Plugin.Logger.LogInfo("Starting server...");

            InstanceFinder.ServerManager.OnServerConnectionState += ServerDone;
            InstanceFinder.ServerManager.StartConnection();

            void ServerDone(ServerConnectionStateArgs args)
            {
                if (args.ConnectionState != LocalConnectionState.Started)
                {
                    if (args.ConnectionState == LocalConnectionState.Stopped)
                    {
                        Plugin.Logger.LogError($"Failed to start server: {args.ConnectionState}");
                        InstanceFinder.ServerManager.OnServerConnectionState -= ServerDone;
                    }
                    else
                    {
                        Plugin.Logger.LogInfo($"Server status: {args.ConnectionState}");
                    }

                    return;
                }

                Plugin.Logger.LogInfo("Server started");
                InstanceFinder.ServerManager.OnServerConnectionState -= ServerDone;

                // Connect client
                InstanceFinder.TransportManager.GetTransport<Multipass>().SetClientTransport<Tugboat>();
                InstanceFinder.NetworkManager.ClientManager.OnClientConnectionState += ClientDone;
                InstanceFinder.NetworkManager.ClientManager.StartConnection();

                void ClientDone(ClientConnectionStateArgs args)
                {
                    if (args.ConnectionState != LocalConnectionState.Started)
                    {
                        Plugin.Logger.LogInfo($"Client connection state: {args.ConnectionState}");

                        if (args.ConnectionState == LocalConnectionState.Stopped)
                        {
                            InstanceFinder.NetworkManager.ClientManager.OnClientConnectionState -= ClientDone;
                            Plugin.Logger.LogError($"Failed to connect client");
                        }

                        return;
                    }

                    Plugin.Logger.LogInfo("Client connected");
                    InstanceFinder.NetworkManager.ClientManager.OnClientConnectionState -= ClientDone;
                }
            }

            yield return new WaitUntil(() => InstanceFinder.IsServer && InstanceFinder.IsClient);
        }

        Plugin.Logger.LogInfo("Waiting for client to initialize...");
        yield return new WaitUntil(() => InstanceFinder.IsClient && InstanceFinder.ClientManager.Connection.IsValid);
        Plugin.Logger.LogInfo($"Client network initialized (connected to {InstanceFinder.ClientManager.Connection} with client id {InstanceFinder.ClientManager.Connection.ClientId})");

        Plugin.Logger.LogInfo("Waiting for local player to spawn...");
        LoadManager.Instance.LoadStatus = LoadManager.ELoadStatus.SpawningPlayer;
        yield return new WaitUntil(() => Player.Local != null);
        Plugin.Logger.LogInfo("Local player spawned");

        if (host)
        {
            LoadManager.Instance.LoadStatus = LoadManager.ELoadStatus.LoadingData;
            yield return LoadSave(save!);

            IEnumerator LoadSave(SaveInfo save)
            {
                Plugin.Logger.LogInfo("Loading save...");

                foreach (IBaseSaveable saveable in SaveManager.Instance.BaseSaveables)
                {
                    new LoadRequest(Path.Combine(LoadManager.Instance.LoadedGameFolderPath, saveable.SaveFolderName), saveable.Loader);
                }
                while (LoadManager.Instance.loadRequests.Count > 0)
                {
                    for (int index = 0; index < 50 && LoadManager.Instance.loadRequests.Count > 0; ++index)
                    {
                        LoadRequest loadRequest = LoadManager.Instance.loadRequests[0];
                        try
                        {
                            loadRequest.Complete();
                        }
                        catch (Exception ex)
                        {
                            Plugin.Logger.LogError($"LOAD ERROR for load request: {loadRequest.Path} : {ex.Message}\nSite: {ex.TargetSite?.ToString()}");

                            if (LoadManager.Instance.loadRequests.FirstOrDefault() == loadRequest)
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
                Plugin.Logger.LogInfo("Save loaded");
            }
        }

        LoadManager.Instance.LoadStatus = LoadManager.ELoadStatus.None;
        LoadingScreen.Instance.Close();
        LoadManager.Instance.IsLoading = false;
        LoadManager.Instance.IsGameLoaded = true;
    }
}
