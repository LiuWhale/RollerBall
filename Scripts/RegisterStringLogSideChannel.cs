using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.SideChannels;
using System.Text;
using System;

public class RegisterStringLogSideChannel : MonoBehaviour
{
    [HideInInspector]
    public StringLogSideChannel stringChannel;
    public void Awake()
    {
        // We create the Side Channel
        stringChannel = new StringLogSideChannel();

        // When a Debug.Log message is created, we send it to the stringChannel
        // Application.logMessageReceived += stringChannel.SendStatementToPython;

        // The channel must be registered with the SideChannelManager class
        SideChannelManager.RegisterSideChannel(stringChannel);
    }

    public void OnDestroy()
    {
        // De-register the Debug.Log callback
        // Application.logMessageReceived -= stringChannel.SendStatementToPython;
        if (Academy.IsInitialized){
            SideChannelManager.UnregisterSideChannel(stringChannel);
        }
    }

    // public void Update()
    // {
    //     // Optional : If the space bar is pressed, raise an error !
    //     if (Input.GetKeyDown(KeyCode.Space))
    //     {
    //         Debug.LogError("This is a fake error. Space bar was pressed in Unity.");
    //     }
    // }
}

public class StringLogSideChannel : SideChannel
{
    public StringLogSideChannel()
    {
        ChannelId = new Guid("eb7a9da8-d880-b7ff-7944-c300c6a17e79");
    }

    protected override void OnMessageReceived(IncomingMessage msg)
    {
        var receivedString = msg.ReadString();
        // Debug.Log("From Python : " + receivedString);
    }

    public void SendStatementToPython(string logString, string stackTrace, LogType type)
    {
        if (type == LogType.Error)
        {
            var stringToSend = type.ToString() + ": " + logString + "\n" + stackTrace;
            using (var msgOut = new OutgoingMessage())
            {
                msgOut.WriteString(stringToSend);
                QueueMessageToSend(msgOut);
            }
        }

        if (type == LogType.Log)
        {
            using (var msgOut = new OutgoingMessage())
            {
                msgOut.WriteString(logString);
                QueueMessageToSend(msgOut);
            }
        }
    }

    public void SendStringToPython(string message)
    {
        using (var msgOut = new OutgoingMessage())
        {
            msgOut.WriteString(message);
            QueueMessageToSend(msgOut);
        }
    }
}