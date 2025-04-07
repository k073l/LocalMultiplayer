using System.Linq;
using System.Text;
using ScheduleOne.DevUtilities;
using ScheduleOne.Networking;
using ScheduleOne.Persistence;
using TMPro;
using UnityEngine;

namespace LocalMultiplayer;

public class MenuComponent : MonoBehaviour
{
    private GameObject? instructionsUi;
    private TextMeshProUGUI? text;

    void Awake()
    {
        CreateMainMenuUi();
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

    private SaveInfo? GetFirstSave() => LoadManager.SaveGames.FirstOrDefault(save => save != null);

    private void UpdateInstructionText()
    {
        if (text == null)
            return;

        var save = GetFirstSave();

        var builder = new StringBuilder();

        if (save == null)
        {
            builder.AppendLine("Can not host server, no save found");
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

        var save = LoadManager.SaveGames.FirstOrDefault(save => save != null);

        if (save == null)
        {
            Plugin.Logger.LogError("No save game found");
            return;
        }
    }

    private void ConnectToLocalhost()
    {
        Plugin.Logger.LogInfo("Connecting to localhost...");

        PreChecks();
    }

    private void PreChecks()
    {
        if (Lobby.Instance.IsInLobby)
        {
            Plugin.Logger.LogInfo("Leaving current lobby");
            Lobby.Instance.LeaveLobby();
        }
    }
}
