using Everything_To_IMU_SlimeVR.Tracking;
using Google.FlatBuffers;
using ImuToXInput;
using LucHeart.CoreOSC;
using SlimeImuProtocol;
using solarxr_protocol;
using solarxr_protocol.data_feed;
using solarxr_protocol.data_feed.device_data;
using solarxr_protocol.data_feed.tracker;
using solarxr_protocol.datatypes;
using solarxr_protocol.datatypes.math;
using solarxr_protocol.pub_sub;
using solarxr_protocol.rpc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class SlimeVRClient
{

    public class Transform
    {
        public Vector3 Translation;
        public Quaternion Rotation;
    }
    private Dictionary<string, TrackerState> _trackers = new();
    public DataFeedUpdate? Skeleton { get; private set; }
    public Transform SkeletonToCameraTransform { get; private set; }
    public Dictionary<string, TrackerState> Trackers { get => _trackers; set => _trackers = value; }

    private readonly Uri serverUri = new("ws://localhost:21110");
    private const int FPS = 200;
    private const int DataFeedUpdateDelayInMs = 1000 / FPS;

    private readonly CancellationTokenSource cancellationTokenSource = new();
    private ClientWebSocket client;

    public void Start()
    {
        Task.Run(async () => await RunNetworking(cancellationTokenSource.Token));
    }

    void OnDestroy()
    {
        cancellationTokenSource.Cancel();
    }

    private async Task RunNetworking(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await NetworkLoop(cancellationToken);
            } catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }

    private async Task NetworkLoop(CancellationToken cancellationToken)
    {
        this.client = new ClientWebSocket();

        while (true)
        {
            while (true)
            {
                Console.WriteLine($"Connecting to {serverUri}...");
                await client.ConnectAsync(serverUri, cancellationToken);
                if (client.State == WebSocketState.Open)
                {
                    break;
                }

                Console.WriteLine($"Failed to connect to {serverUri}");
                Thread.Sleep(1000);

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
            }

            Console.WriteLine($"Connected to {serverUri}");

            await SendSubDataFeedMsg(client, cancellationToken);
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            while (client.State == WebSocketState.Open)
            {
                var receiveBuffer = new byte[1024 * 1024]; // 1MB

                var receiveResult = await client.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                if (!receiveResult.EndOfMessage)
                {
                    Console.WriteLine($"Received message that does not fit in buffer, ignoring message");
                    while (true)
                    {
                        var nextReceiveResult = await client.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), cancellationToken);
                        if (cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }

                        if (nextReceiveResult.EndOfMessage)
                        {
                            break;
                        }
                    }

                    Console.WriteLine($"Reached end of message");
                    continue;
                }

                switch (receiveResult.MessageType)
                {
                    case WebSocketMessageType.Binary:
                        var messageBundle = MessageBundle.GetRootAsMessageBundle(new ByteBuffer(receiveBuffer));
                        ProcessMessageBundle(messageBundle);
                        break;

                    case WebSocketMessageType.Text:
                        //Console.WriteLine($"Received text message, ignoring message");
                        Console.WriteLine(Encoding.UTF8.GetString(new ReadOnlySpan<byte>(receiveBuffer, 0, receiveResult.Count)));
                        break;

                    case WebSocketMessageType.Close:
                        Console.WriteLine($"Received close message");
                        break;
                }
            }
        }
    }

    private async Task SendSubDataFeedMsg(ClientWebSocket client, CancellationToken cancellationToken)
    {
        var buffer = new byte[1024 * 1024];

        var builder = new FlatBufferBuilder(new ByteBuffer(buffer));

        TrackerDataMask.StartTrackerDataMask(builder);
        TrackerDataMask.AddInfo(builder, true);
        TrackerDataMask.AddStatus(builder, true);
        TrackerDataMask.AddPosition(builder, true);
        TrackerDataMask.AddRotation(builder, true);
        TrackerDataMask.AddRotationIdentityAdjusted(builder, true);
        TrackerDataMask.AddRotationReferenceAdjusted(builder, true);
        var physicalTrackersMask = TrackerDataMask.EndTrackerDataMask(builder);

        DeviceDataMask.StartDeviceDataMask(builder);
        DeviceDataMask.AddTrackerData(builder, physicalTrackersMask);
        DeviceDataMask.AddDeviceData(builder, true);
        var deviceDataMask = DeviceDataMask.EndDeviceDataMask(builder);

        TrackerDataMask.StartTrackerDataMask(builder);
        TrackerDataMask.AddInfo(builder, true);
        TrackerDataMask.AddStatus(builder, true);
        TrackerDataMask.AddPosition(builder, true);
        TrackerDataMask.AddRotationIdentityAdjusted(builder, true);
        TrackerDataMask.AddRotationReferenceAdjusted(builder, true);
        var syntheticTrackersMask = TrackerDataMask.EndTrackerDataMask(builder);

        DataFeedConfig.StartDataFeedConfig(builder);
        DataFeedConfig.AddMinimumTimeSinceLast(builder, DataFeedUpdateDelayInMs);
        DataFeedConfig.AddDataMask(builder, deviceDataMask);
        DataFeedConfig.AddSyntheticTrackersMask(builder, syntheticTrackersMask);
        DataFeedConfig.AddBoneMask(builder, true);
        var dataFeedConfig = DataFeedConfig.EndDataFeedConfig(builder);

        var dataFeedsVector = StartDataFeed.CreateDataFeedsVector(builder, new[] { dataFeedConfig });

        StartDataFeed.StartStartDataFeed(builder);
        StartDataFeed.AddDataFeeds(builder, dataFeedsVector);
        var startDataFeed = StartDataFeed.EndStartDataFeed(builder);

        DataFeedMessageHeader.StartDataFeedMessageHeader(builder);
        DataFeedMessageHeader.AddMessageType(builder, DataFeedMessage.StartDataFeed);
        DataFeedMessageHeader.AddMessage(builder, startDataFeed.Value);
        var dataFeedMsgHeader = DataFeedMessageHeader.EndDataFeedMessageHeader(builder);

        var dataFeedMsgsVector = MessageBundle.CreateDataFeedMsgsVector(builder, new[] { dataFeedMsgHeader });
        var rpcMsgsVector = MessageBundle.CreateRpcMsgsVector(builder, new Offset<RpcMessageHeader>[0]);
        var pubSubMsgsVector = MessageBundle.CreatePubSubMsgsVector(builder, new Offset<PubSubHeader>[0]);

        MessageBundle.StartMessageBundle(builder);
        MessageBundle.AddDataFeedMsgs(builder, dataFeedMsgsVector);
        MessageBundle.AddRpcMsgs(builder, rpcMsgsVector);
        MessageBundle.AddPubSubMsgs(builder, pubSubMsgsVector);
        var messageBundle = MessageBundle.EndMessageBundle(builder);

        builder.Finish(messageBundle.Value);

        Console.WriteLine("Sending data feed subscription message...");

        await client.SendAsync(
            new ReadOnlyMemory<byte>(
                buffer,
                builder.DataBuffer.Position,
                builder.DataBuffer.Length - builder.DataBuffer.Position),
            WebSocketMessageType.Binary,
            true,
            cancellationToken);
    }

    private void ProcessMessageBundle(MessageBundle messageBundle)
    {
        for (var i = 0; i < messageBundle.DataFeedMsgsLength; ++i)
        {
            var dataFeedMsg = messageBundle.DataFeedMsgs(i);
            if (!dataFeedMsg.HasValue) continue;

            if (dataFeedMsg.Value.MessageType != DataFeedMessage.DataFeedUpdate) continue;

            var update = dataFeedMsg.Value.MessageAsDataFeedUpdate();

            // Map BodyPart -> IP (from devices)
            var bodyPartIpMap = new Dictionary<string, string>();
            for (var d = 0; d < update.DevicesLength; ++d)
            {
                var device = update.Devices(d);
                if (!device.HasValue) continue;

                var hwInfo = device.Value.HardwareInfo;
                string ip = "";

                if (hwInfo.HasValue)
                {
                    if (hwInfo.Value.IpAddress != null)
                    {
                        ip = Ipv4ToString(hwInfo.Value.IpAddress.Value.Addr);
                    }
                }

                // Assign IP to all tracked bones for this device
                if (device.Value.TrackersLength > 0)
                {
                    for (var t = 0; t < device.Value.TrackersLength; t++)
                    {
                        var tracker = device.Value.Trackers(t);
                        if (!tracker.HasValue) continue;
                        var bodyPart = tracker.Value.Info.Value.BodyPart.ToString();
                        bodyPartIpMap[bodyPart] = ip;
                    }
                }
            }

            // Process bones
            for (var b = 0; b < update.BonesLength; ++b)
            {
                var optionalBone = update.Bones(b);
                if (!optionalBone.HasValue) continue;

                var bone = optionalBone.Value;
                if (!bone.RotationG.HasValue) continue;

                string ip = bodyPartIpMap.TryGetValue(bone.BodyPart.ToString(), out var mappedIp) ? mappedIp : "";

                var headPosition = RHSToLHSVector3(bone.HeadPositionG.Value);
                var headRotation = RHSToLHSQuaternion(bone.RotationG.Value)
                    * Quaternion.CreateFromAxisAngle(new Vector3(1, 0, 0), -90.0f);

                HandleSolarXRMessage(bone.BodyPart.ToString(), ip, headPosition, headRotation);
            }
        }
    }


    string Ipv4ToString(uint addr)
    {
        byte b1 = (byte)((addr >> 24) & 0xFF);
        byte b2 = (byte)((addr >> 16) & 0xFF);
        byte b3 = (byte)((addr >> 8) & 0xFF);
        byte b4 = (byte)(addr & 0xFF);
        return $"{b1}.{b2}.{b3}.{b4}";
    }


    void HandleSolarXRMessage(string bodyPart, string ipAddress, Vector3 position, Quaternion rotation)
    {
        try
        {
            var eulerCalibration = new Vector3();
            var positionCalibration = new Vector3();

            Quaternion localRotation = rotation;

            Quaternion worldRotation = localRotation;

            if (!_trackers.ContainsKey(bodyPart))
            {
                eulerCalibration = localRotation.QuaternionToEuler();
                positionCalibration = position;
            } else
            {
                eulerCalibration = _trackers[bodyPart].EulerCalibration;
                positionCalibration = _trackers[bodyPart].PositionCalibration;
            }

            _trackers[bodyPart] = new TrackerState
            {
                PositionCalibration = positionCalibration,
                Position = position,
                EulerCalibration = eulerCalibration,
                BodyPart = bodyPart,
                Ip = ipAddress,
                Rotation = localRotation,
                WorldRotation = worldRotation
            };
        } catch (Exception ex)
        {
            Console.WriteLine($"Parse error: {ex.Message}");
        }
    }

    public async Task SendReset(ResetType resetType)
    {
        var buffer = new byte[1024 * 1024];

        var builder = new FlatBufferBuilder(new ByteBuffer(buffer));

        ResetRequest.StartResetRequest(builder);
        ResetRequest.AddResetType(builder, resetType);
        var resetRequestOffset = ResetRequest.EndResetRequest(builder);

        RpcMessageHeader.StartRpcMessageHeader(builder);
        RpcMessageHeader.AddMessageType(builder, RpcMessage.ResetRequest);
        RpcMessageHeader.AddMessage(builder, resetRequestOffset.Value);
        var rpcMessageHeaderOffset = RpcMessageHeader.EndRpcMessageHeader(builder);

        var rpcMessagesOffset = MessageBundle.CreateRpcMsgsVector(builder, new[] { rpcMessageHeaderOffset });

        MessageBundle.StartMessageBundle(builder);
        MessageBundle.AddRpcMsgs(builder, rpcMessagesOffset);
        var messageBundleOffset = MessageBundle.EndMessageBundle(builder);

        builder.Finish(messageBundleOffset.Value);

        Console.WriteLine("Sending full reset request...");

        await client.SendAsync(
            new ReadOnlyMemory<byte>(
                buffer,
                builder.DataBuffer.Position,
                builder.DataBuffer.Length - builder.DataBuffer.Position),
            WebSocketMessageType.Binary,
            true,
            cancellationTokenSource.Token);
    }

    private static Vector3 RHSToLHSVector3(Vec3f v)
    {
        return new Vector3(v.X, v.Y, -v.Z);
    }

    private static Quaternion RHSToLHSQuaternion(Quat q)
    {
        return new Quaternion(q.X, q.Y, -q.Z, -q.W);
    }
}