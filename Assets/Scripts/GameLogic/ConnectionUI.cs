using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using TomNet;
using TomNet.Core;

using Google.Protobuf;


public class ConnectionUI : MonoBehaviour
{

    //----------------------------------------------------------
    // Editor public properties
    //----------------------------------------------------------

    [Tooltip("IP address or domain name of the TomNetServer")]
    public string Host = "127.0.0.1";

    [Tooltip("TCP port listened by the TomNetServer ")]
    public int TcpPort = 8888;
    //----------------------------------------------------------
    // UI elements
    //----------------------------------------------------------

    public InputField nameInput;
    public Button loginButton;
    public Text errorText;

    //----------------------------------------------------------
    // Private properties
    //----------------------------------------------------------

    private Doraemon doraemon;

    //----------------------------------------------------------
    // Unity calback methods
    //----------------------------------------------------------

    void Start()
    {
        // Initialize UI
        errorText.text = "";
    }

    void Update()
    {
        if (doraemon != null)
            doraemon.ProcessEvents();
    }

    //----------------------------------------------------------
    // Public interface methods for UI
    //----------------------------------------------------------

    public void OnLoginButtonClick()
    {
        enableLoginUI(false);


        doraemon = new Doraemon();
        doraemon.AddEventListener(DoraemonEvent.CONNECTION, OnConnection);
        doraemon.AddEventListener(DoraemonEvent.CONNECTION_LOST, OnConnectionLost);
        doraemon.AddEventListener(DoraemonEvent.LOGIN, OnLogin);
        doraemon.AddEventListener(DoraemonEvent.LOGIN_ERROR, OnLoginError);
        doraemon.AddEventListener(DoraemonEvent.ROOM_JOIN, OnRoomJoin);
        doraemon.AddEventListener(DoraemonEvent.ROOM_JOIN_ERROR, OnRoomJoinError);

        // Connect to SFS2X
        doraemon.Connect(Host, TcpPort);
    }

    //----------------------------------------------------------
    // Private helper methods
    //----------------------------------------------------------

    private void enableLoginUI(bool enable)
    {
        nameInput.interactable = enable;
        loginButton.interactable = enable;
        errorText.text = "";
    }

    private void reset()
    {
        // Remove SFS2X listeners
        doraemon.RemoveAllEventListeners();

        // Enable interface
        enableLoginUI(true);
    }

    //----------------------------------------------------------
    // SmartFoxServer event listeners
    //----------------------------------------------------------

    private void OnConnection(BaseEvent evt)
    {
        if ((bool)evt.Params["success"])
        {
            // Save reference to the SmartFox instance in a static field, to share it among different scenes
            SmartFoxConnection.Connection = doraemon;

            //Debug.Log("SFS2X API version: " + doraemon.Version);
            //Debug.Log("Connection mode is: " + doraemon.ConnectionMode);

            // Login
            //doraemon.Send(new Tom.Requests.LoginRequest(nameInput.text));
            SceneManager.LoadScene("Game");
        }
        else
        {
            // Remove SFS2X listeners and re-enable interface
            reset();

            // Show error message
            errorText.text = "Connection failed; is the server running at all?";
        }
    }

    private void OnConnectionLost(BaseEvent evt)
    {
        // Remove SFS2X listeners and re-enable interface
        reset();

        string reason = (string)evt.Params["reason"];

        //if (reason != ClientDisconnectionReason.MANUAL)
        {
            // Show error message
            errorText.text = "Connection was lost; reason is: " + reason;
        }
    }

    private void OnLogin(BaseEvent evt)
    {
        string roomName = "UnityMMODemo";

        // We either create the Game Room or join it if it exists already
        //if (doraemon.RoomManager.ContainsRoom(roomName))
        {
           // doraemon.Send(new JoinRoomRequest(roomName));
        }
        //else
        {
            //MMORoomSettings settings = new MMORoomSettings(roomName);
            //settings.DefaultAOI = new Vec3D(25f, 1f, 25f);
            //settings.MapLimits = new MapLimits(new Vec3D(-100f, 1f, -100f), new Vec3D(100f, 1f, 100f));
            //settings.MaxUsers = 100;
            //settings.Extension = new RoomExtension("MMORoomDemo", "sfs2x.extension.mmo.MMORoomDemoExtension");
            //doraemon.Send(new CreateRoomRequest(settings, true));
        }
    }

    private void OnLoginError(BaseEvent evt)
    {
        // Disconnect
        //doraemon.Disconnect();

        // Remove SFS2X listeners and re-enable interface
        reset();

        // Show error message
        errorText.text = "Login failed: " + (string)evt.Params["errorMessage"];
    }

    private void OnRoomJoin(BaseEvent evt)
    {
        // Remove SFS2X listeners and re-enable interface before moving to the main game scene
        reset();

        // Go to main game scene
        SceneManager.LoadScene("Game");
    }

    private void OnRoomJoinError(BaseEvent evt)
    {
        // Show error message
        errorText.text = "Room join failed: " + (string)evt.Params["errorMessage"];
    }
}