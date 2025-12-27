using UnityEngine;
using NetMQ;
using NetMQ.Sockets;
using System.Threading;
using System.Collections.Concurrent;

public class WindowsCameraStreamer : MonoBehaviour
{
    public string address = "tcp://192.168.0.213:5555";
    public int width = 640;
    public int height = 480;
    public int jpgQuality = 60;

    private WebCamTexture webcam;
    private Texture2D tex;

    private Thread sendThread;
    private bool running = true;
    private float sendInterval = 1f / 60f; // 30 FPS
    private float timer;
    private ConcurrentQueue<byte[]> frameQueue = new ConcurrentQueue<byte[]>();

    void Start()
    {
        webcam = new WebCamTexture(width, height);
        webcam.Play();

        tex = new Texture2D(width, height, TextureFormat.RGB24, false);

        sendThread = new Thread(SendLoop);
        sendThread.IsBackground = true;
        sendThread.Start();
    }

    //MAIN THREAD ONLYss
    void Update()
    {
        if (webcam == null || !webcam.didUpdateThisFrame)
            return;

        timer += Time.deltaTime;
        if (timer < sendInterval)
        return;

        timer = 0f;

        tex.SetPixels32(webcam.GetPixels32());
        tex.Apply();

        byte[] frame = tex.EncodeToJPG(jpgQuality);
        frameQueue.Enqueue(frame);
    }

    //BACKGROUND THREAD (NO UNITY API)
    void SendLoop()
    {
        AsyncIO.ForceDotNet.Force();

        using (var socket = new PushSocket())
        {
            socket.Connect(address);

            while (running)
            {
                if (frameQueue.TryDequeue(out byte[] frame))
                {
                    socket.SendFrame(frame);
                }
                else
                {
                    Thread.Sleep(1);
                }
            }
        }

        NetMQConfig.Cleanup();
    }

    void OnApplicationQuit()
    {
        running = false;

        webcam?.Stop();
        sendThread?.Join();
    }
}
