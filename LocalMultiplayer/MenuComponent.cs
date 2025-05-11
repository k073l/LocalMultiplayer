using UnityEngine;
using System.Text;
using MelonLoader;

#if MONO
using FishNet;
using ScheduleOne.Networking;
using ScheduleOne.Persistence;
using TMPro;
#else
using Il2CppInterop.Runtime;
using Il2CppFishNet;
using Il2CppScheduleOne.Networking;
using Il2CppScheduleOne.Persistence;
using Il2CppTMPro;
#endif

namespace LocalMultiplayer;

[RegisterTypeInIl2Cpp]
public class MenuComponent : MonoBehaviour
{
    private GameObject? instructionsUi;
    private TextMeshProUGUI? text;
    public static object serverCoroutine;
    public static object clientCoroutine;

    enum LaunchMode
    {
        None,
        Server,
        Client,
    }

    private LaunchMode launchMode;

    void Awake()
    {
        CreateMainMenuUi();
        DontDestroyOnLoad(gameObject);
    }

    private void CreateMainMenuUi()
    {
        var mainMenu = GameObject.Find("MainMenu");

#if MONO
    instructionsUi = new GameObject("LocalMultiplayerInstructions", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
#else
        instructionsUi = new GameObject("LocalMultiplayerInstructions", Il2CppType.Of<RectTransform>(), Il2CppType.Of<CanvasRenderer>(), Il2CppType.Of<TextMeshProUGUI>());
#endif

        instructionsUi.transform.SetParent(mainMenu.transform);
        text = instructionsUi.GetComponent<TextMeshProUGUI>();
        text.autoSizeTextContainer = true;
        
        var rectTransform = instructionsUi.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = new Vector2(0, 0);
    }


    private void Start()
    {
        if (LocalMultiplayer.LaunchArguments?.Host == true)
        {
            launchMode = LaunchMode.Server;
        }
        else if (LocalMultiplayer.LaunchArguments?.Join == true)
        {
            launchMode = LaunchMode.Client;
        }
        else
        {
            launchMode = LaunchMode.None;
        }

        ApplyLaunchMode();
    }

    private void ApplyLaunchMode()
    {
        switch (launchMode)
        {
            case LaunchMode.Server:
                {
                    if (!StartHosting())
                    {
                        // prevent infinite loop of trying to start server
                        launchMode = LaunchMode.None;
                    }
                    break;
                }
            case LaunchMode.Client:
                {
                    ConnectToLocalhost();
                    break;
                }
        }
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

    private SaveInfo? GetLastSave() => LoadManager.SaveGames.FirstOrDefault(save => save != null && !save.MetaData.PlayTutorial);

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

        builder.AppendLine("protip: --host or --join launch arguments work too");
        builder.AppendLine("protip: --adjust-window will align the windows side by side");
        builder.AppendLine("protip: --left-offset [pixels] will leave a gap\non the left side of the screen to fit the console windows");

        text.text = builder.ToString();
    }

    private bool StartHosting()
    {
        LocalMultiplayer.Logger.Msg("Starting server...");
        PreChecks();

        var save = GetLastSave();

        if (save == null)
        {
            LocalMultiplayer.Logger.Msg("No save game found (finish tutorial if it's not completed)");
            return false;
        }

        if (serverCoroutine != null)
            MelonCoroutines.Stop(serverCoroutine);
        
        serverCoroutine = MelonCoroutines.Start(LocalMultiplayer.HostOrJoinServer(save, host: true));
        return true;
    }

    private void ConnectToLocalhost()
    {
        LocalMultiplayer.Logger.Msg("Connecting to localhost...");
        PreChecks();

        if (clientCoroutine != null)
            MelonCoroutines.Stop(clientCoroutine);

        clientCoroutine = MelonCoroutines.Start(LocalMultiplayer.HostOrJoinServer(save: null, host: false));
    }

    private void PreChecks()
    {
        if (Lobby.Instance.IsInLobby)
        {
            LocalMultiplayer.Logger.Msg("Leaving current lobby");
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
}
