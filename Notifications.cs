using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EasyOverlay.Overlay;
using EasyOverlay.UI;
using Newtonsoft.Json;
using UnityEngine;

namespace EasyOverlay
{
    public sealed class Notifications : BaseOverlay
    {
        private static Notifications instance;
        
        [SerializeField] public int listenPort = 42069;
        [SerializeField] public float popupLengthSeconds = 4f;
        [SerializeField] public float repeatInSeconds = 0.5f;
        [SerializeField] public EasyUiManager ui;

        private readonly ConcurrentQueue<XSOMessage> messages = new();
        private DateTime canReadAt = DateTime.MinValue;
        private NotificationsReceiver receiver;
        private Coroutine coroutine;

        public Notifications()
        {
            if (instance != null)
                throw new ApplicationException("Can't have more than one NotificationOverlay components!");
            instance = this;
        }

        protected override void Start()
        {
            base.Start();

            receiver = new NotificationsReceiver(listenPort, messages);
            receiver.Start();
        }

        protected override IEnumerator FirstShow()
        {
            yield break;
        }

        protected override void Update()
        {
            base.Update();

            if (canReadAt < DateTime.UtcNow && messages.TryDequeue(out var message))
            {
                canReadAt = DateTime.UtcNow.AddSeconds(repeatInSeconds);
                if (message.content != null)
                    Popup(message.title ?? "Notification", message.content);
            }
        }

        public override void Show()
        {
            ui.uiCamera.enabled = true;
            base.Show();

            UploadPositionRelative(TrackedDevice.Hmd);
        }

        public override void Hide()
        {
            base.Hide();
            ui.uiCamera.enabled = false;
        }

        private void Popup(string title, string text)
        {
            ui.GetTextField("title").text = title;
            ui.GetTextField("text").text = text;

            if (coroutine != null)
            {
                // already displaying something
                StopCoroutine(coroutine);
                textureDirty = true;
            }
            else
                Show();
            coroutine = StartCoroutine(Expire());
        }

        private IEnumerator Expire()
        {
            yield return new WaitForSecondsRealtime(popupLengthSeconds);
            Hide();
            coroutine = null;
        }
    }

    public class NotificationsReceiver : IDisposable
    {
        private readonly IPEndPoint listenEndpoint;
        private readonly Socket listenSocket;
        private readonly byte[] listenBuffer;
        private Task worker;
        private readonly CancellationTokenSource cancel = new();
        private readonly ConcurrentQueue<XSOMessage> messageQueue;

        public NotificationsReceiver(int listenPort, ConcurrentQueue<XSOMessage> messages)
        {
            listenBuffer = new byte[1024];
            listenEndpoint = new IPEndPoint(IPAddress.Loopback, listenPort);
            listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            messageQueue = messages;
        }

        public void Start()
        {
            worker = Task.Run(Pipe, cancel.Token);
        }
        
        private async Task Pipe()
        {
            try
            {
                listenSocket.Bind(listenEndpoint);
                var remoteEp = new IPEndPoint(IPAddress.Any, 0);
            
                Debug.Log($"{GetType().Name} started.");
                while (listenSocket.IsBound && !cancel.IsCancellationRequested)
                {
                    try
                    {
                        var result = await listenSocket.ReceiveFromAsync(listenBuffer, SocketFlags.None, remoteEp);
                        var bytes = new ArraySegment<byte>(listenBuffer, 0, result.ReceivedBytes);

                        var json = Encoding.UTF8.GetString(bytes);
                        var message = JsonConvert.DeserializeObject<XSOMessage>(json);
                        messageQueue.Enqueue(message);
                    }
                    catch (Exception x)
                    {
                        Debug.Log(x.ToString());
                    }
                }

                Debug.Log($"{GetType().Name} exited.");
            }
            catch (Exception x)
            {
                Debug.Log(x);
            }
        }

        public void Dispose()
        {
            listenSocket?.Dispose();
            worker?.Dispose();
            cancel?.Dispose();
        }
    }
    
    // ReSharper disable InconsistentNaming
    // ReSharper disable UnusedMember.Global
    // ReSharper disable UnusedAutoPropertyAccessor.Global
    public struct XSOMessage
    {
        public int messageType { get; set; }
        public int index { get; set; }
        public float volume { get; set; }
        public string audioPath { get; set; }
        public float timeout { get; set; }
        public string title { get; set; }
        public string content { get; set; }
        public string icon { get; set; }
        public float height { get; set; }
        public float opacity { get; set; }
        public bool useBase64Icon { get; set; }
        public string sourceApp { get; set; }
    }
}