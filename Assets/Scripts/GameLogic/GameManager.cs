using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using TomNet;
using TomNet.Core;


public class GameManager : MonoBehaviour
{

    //----------------------------------------------------------
    // Public properties
    //----------------------------------------------------------

    public GameObject[] playerModels;
    public Material[] playerMaterials;


    //----------------------------------------------------------
    // Private properties
    //----------------------------------------------------------

    private Doraemon sfs;

    private GameObject localPlayer;
    private PlayerController localPlayerController;
    //private Dictionary<SFSUser, GameObject> remotePlayers = new Dictionary<SFSUser, GameObject>();

    //----------------------------------------------------------
    // Unity calback methods
    //----------------------------------------------------------

    void Start()
    {

        if (!SmartFoxConnection.IsInitialized)
        {
            SceneManager.LoadScene("Connection");
            return;
        }

        sfs = SmartFoxConnection.Connection;

        // Register callback delegates
        sfs.AddEventListener(DoraemonEvent.CONNECTION_LOST, OnConnectionLost);
        sfs.AddEventListener(DoraemonEvent.USER_VARIABLES_UPDATE, OnUserVariableUpdate);
        sfs.AddEventListener(DoraemonEvent.PROXIMITY_LIST_UPDATE, OnProximityListUpdate);

        // Get random avatar and color and spawn player
        int numModel = UnityEngine.Random.Range(0, playerModels.Length);
        int numMaterial = UnityEngine.Random.Range(0, playerMaterials.Length);
        SpawnLocalPlayer(numModel, numMaterial);

        // Update settings panel with the selected model and material
        GameUI ui = GameObject.Find("UI").GetComponent("GameUI") as GameUI;
        ui.SetAvatarSelection(numModel);
        ui.SetColorSelection(numMaterial);
    }

    void FixedUpdate()
    {
        if (sfs != null)
        {
            sfs.ProcessEvents();

            // If we spawned a local player, send position if movement is dirty

            /*
			 * NOTE: We have commented the UserVariable relative to the Y Axis because in this example the Y position is fixed (Y = 1.0).
			 * In case your game allows moving on all axis you should transmit all positions.
			 * 
			 * On the server side the UserVariable event is captured and the coordinates are also passed to the MMOApi.SetUserPosition(...) method to update our position in the Room's map.
			 * This in turn will keep us in synch with all the other players within our Area of Interest (AoI).
			 */
            if (localPlayer != null && localPlayerController != null && localPlayerController.MovementDirty)
            {
                /*
                 * TODO
                List<UserVariable> userVariables = new List<UserVariable>();
                userVariables.Add(new SFSUserVariable("x", (double)localPlayer.transform.position.x));
                //userVariables.Add(new SFSUserVariable("y", (double)localPlayer.transform.position.y));
                userVariables.Add(new SFSUserVariable("z", (double)localPlayer.transform.position.z));
                userVariables.Add(new SFSUserVariable("rot", (double)localPlayer.transform.rotation.eulerAngles.y));
                sfs.Send(new SetUserVariablesRequest(userVariables));
                localPlayerController.MovementDirty = false;
                 */
            }
        }
    }

    //----------------------------------------------------------
    // SmartFoxServer event listeners
    //----------------------------------------------------------

    /**
	 * This is where we receive events about people in proximity (AoI).
	 * We get two lists, one of new users that have entered the AoI and one with users that have left our proximity area.
	 */
    public void OnProximityListUpdate(BaseEvent evt)
    {
        /*
         * TODO
        var addedUsers = (List<User>)evt.Params["addedUsers"];
        var removedUsers = (List<User>)evt.Params["removedUsers"];

        // Handle all new Users
        foreach (User user in addedUsers)
        {
            SpawnRemotePlayer(
                (SFSUser)user,
                user.GetVariable("model").GetIntValue(),
                user.GetVariable("mat").GetIntValue(),
                new Vector3(user.AOIEntryPoint.FloatX, user.AOIEntryPoint.FloatY, user.AOIEntryPoint.FloatZ),
                Quaternion.Euler(0, (float)user.GetVariable("rot").GetDoubleValue(), 0)
            );
        }

        // Handle removed users
        foreach (User user in removedUsers)
        {
            RemoveRemotePlayer((SFSUser)user);
        }
         */
    }

    public void OnConnectionLost(BaseEvent evt)
    {
        // Reset all internal states so we kick back to login screen
        sfs.RemoveAllEventListeners();
        SceneManager.LoadScene("Connection");
    }

    /**
	 * When user variable is updated on any client within the AoI, then this event is received.
	 * This is where most of the game logic for this example is contained.
	 */
    public void OnUserVariableUpdate(BaseEvent evt)
    {
        List<string> changedVars = (List<string>)evt.Params["changedVars"];

        /*
         * TODO
        SFSUser user = (SFSUser)evt.Params["user"];

        if (user == sfs.MySelf) return;

        changedVars.Contains("x");
        changedVars.Contains("y");
        changedVars.Contains("z");
        changedVars.Contains("rot");

        // Check if the remote user changed his position or rotation
        if (changedVars.Contains("x") || changedVars.Contains("y") || changedVars.Contains("z") || changedVars.Contains("rot"))
        {
            if (remotePlayers.ContainsKey(user))
            {
                // Move the character to a new position...
                remotePlayers[user].GetComponent<SimpleRemoteInterpolation>().SetTransform(
                    new Vector3((float)user.GetVariable("x").GetDoubleValue(), 1, (float)user.GetVariable("z").GetDoubleValue()),
                    Quaternion.Euler(0, (float)user.GetVariable("rot").GetDoubleValue(), 0),
                    true
                );
            }
        }

        // Remote client selected new model?
        if (changedVars.Contains("model"))
        {
            SpawnRemotePlayer(user, user.GetVariable("model").GetIntValue(), user.GetVariable("mat").GetIntValue(), remotePlayers[user].transform.position, remotePlayers[user].transform.rotation);
        }

        // Remote client selected new material?
        if (changedVars.Contains("mat"))
        {
            remotePlayers[user].GetComponentInChildren<Renderer>().material = playerMaterials[user.GetVariable("mat").GetIntValue()];
        }
         */
    }


    //----------------------------------------------------------
    // Public interface methods for UI
    //----------------------------------------------------------

    public void Disconnect()
    {
        //sfs.Disconnect();
    }

    public void ChangePlayerMaterial(int numMaterial)
    {
        localPlayer.GetComponentInChildren<Renderer>().material = playerMaterials[numMaterial];
        /*
        List<UserVariable> userVariables = new List<UserVariable>();
        userVariables.Add(new SFSUserVariable("mat", numMaterial));
        sfs.Send(new SetUserVariablesRequest(userVariables));
         */
    }

    public void ChangePlayerModel(int numModel)
    {
        //SpawnLocalPlayer(numModel, sfs.MySelf.GetVariable("mat").GetIntValue());
    }

    //----------------------------------------------------------
    // Private player helper methods
    //----------------------------------------------------------

    private void SpawnLocalPlayer(int numModel, int numMaterial)
    {
        Vector3 pos;
        Quaternion rot;

        // See if there already exists a model - if so, take its pos+rot before destroying it
        if (localPlayer != null)
        {
            pos = localPlayer.transform.position;
            rot = localPlayer.transform.rotation;
            Camera.main.transform.parent = null;
            Destroy(localPlayer);
        }
        else
        {
            pos = new Vector3(0, 1, 0);
            rot = Quaternion.identity;
        }

        // Lets spawn our local player model
        localPlayer = GameObject.Instantiate(playerModels[3]) as GameObject;
        localPlayer.transform.position = pos;
        localPlayer.transform.rotation = rot;

        // Assign starting material
        localPlayer.GetComponentInChildren<Renderer>().material = playerMaterials[4];

        // Since this is the local player, lets add a controller and fix the camera
        localPlayer.AddComponent<PlayerController>();
        localPlayerController = localPlayer.GetComponent<PlayerController>();
        //localPlayer.GetComponentInChildren<TextMesh>().text = "zxb";
        Camera.main.transform.parent = localPlayer.transform;

        // Lets set the model, material and position and tell the others about it
        // NOTE: we have commented the UserVariable relative to the Y Axis because in this example the Y position is fixed (Y = 1.0)
        // In case your game allows moving on all axis we should transmit all positions
        /*
         * TODO
        List<UserVariable> userVariables = new List<UserVariable>();

        userVariables.Add(new SFSUserVariable("x", (double)localPlayer.transform.position.x));
        //userVariables.Add(new SFSUserVariable("y", (double)localPlayer.transform.position.y));
        userVariables.Add(new SFSUserVariable("z", (double)localPlayer.transform.position.z));
        userVariables.Add(new SFSUserVariable("rot", (double)localPlayer.transform.rotation.eulerAngles.y));
        userVariables.Add(new SFSUserVariable("model", numModel));
        userVariables.Add(new SFSUserVariable("mat", numMaterial));

        // Send request
        sfs.Send(new SetUserVariablesRequest(userVariables));
         */
    }

    /*
     * TOOD
    private void SpawnRemotePlayer(SFSUser user, int numModel, int numMaterial, Vector3 pos, Quaternion rot)
    {
        // See if there already exists a model so we can destroy it first
        if (remotePlayers.ContainsKey(user) && remotePlayers[user] != null)
        {
            Destroy(remotePlayers[user]);
            remotePlayers.Remove(user);
        }

        // Lets spawn our remote player model
        GameObject remotePlayer = GameObject.Instantiate(playerModels[numModel]) as GameObject;
        remotePlayer.AddComponent<SimpleRemoteInterpolation>();
        remotePlayer.GetComponent<SimpleRemoteInterpolation>().SetTransform(pos, rot, false);

        // Color and name
        remotePlayer.GetComponentInChildren<TextMesh>().text = user.Name;
        remotePlayer.GetComponentInChildren<Renderer>().material = playerMaterials[numMaterial];

        // Lets track the dude
        remotePlayers.Add(user, remotePlayer);
    }

    private void RemoveRemotePlayer(SFSUser user)
    {
        if (user == sfs.MySelf) return;

        if (remotePlayers.ContainsKey(user))
        {
            Destroy(remotePlayers[user]);
            remotePlayers.Remove(user);
        }
    }
     */
}