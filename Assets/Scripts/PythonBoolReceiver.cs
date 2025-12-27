using UnityEngine;
using NetMQ;
using NetMQ.Sockets;
using System.Threading;

public class PythonBoolReceiver : MonoBehaviour
{
    [Header("ZMQ Settings")]
    public string address = "tcp://192.168.0.213:5556";

    public CameraBackgroundSwitcher sceneHelper;

    private Thread receiveThread;
    private bool running = true;

    private bool latestResult;
    private bool hasNewValue = false;

    //One-time latch
    private bool animationTriggered = false;

    void Start()
    {
        receiveThread = new Thread(ReceiveLoop);
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    void ReceiveLoop()
    {
        AsyncIO.ForceDotNet.Force();

        using (var socket = new SubscriberSocket())
        {
            socket.Connect(address);
            socket.Subscribe("");

            while (running)
            {
                if (socket.TryReceiveFrameString(out string msg))
                {
                    latestResult = (msg == "1");
                    hasNewValue = true;
                }
                else
                {
                    Thread.Sleep(1);
                }
            }
        }

        NetMQConfig.Cleanup();
    }

    // MAIN THREAD
    void Update()
    {
        if (!hasNewValue)
            return;

        hasNewValue = false;

        Debug.Log($"Python CV Result: {latestResult}");

        // Trigger animation ONLY ONCE
        if (latestResult && !animationTriggered)
        {
            animationTriggered = true;
            sceneHelper.FingerSnapDetected();
            
        }
    }

    void OnApplicationQuit()
    {
        running = false;
        receiveThread?.Join();
    }
}
