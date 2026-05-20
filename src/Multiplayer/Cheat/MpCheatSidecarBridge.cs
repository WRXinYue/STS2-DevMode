using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;

namespace DevMode.Multiplayer.Cheat;

/// <summary>Reflection bridge to RitsuLib Sidecar typed messages for config + commands.</summary>
internal static class MpCheatSidecarBridge {
    private const string TypedRegistryTypeName = "STS2RitsuLib.Networking.Sidecar.RitsuLibSidecarTypedMessageRegistry";
    private const string ConfigMessageKey = "mp_cheat_cfg";
    private const string CommandMessageKey = "mp_cheat_cmd";
    private const string AddCardAckMessageKey = "mp_cheat_add_ack";

    private static Type? _typedRegistryType;
    private static bool _resolved;
    private static bool _bootstrapped;
    private static IDisposable? _configSub;
    private static IDisposable? _commandSub;
    private static IDisposable? _addCardAckSub;
    private static object? _configDescriptor;
    private static object? _commandDescriptor;
    private static object? _addCardAckDescriptor;
    private static long _hostRevision;

    public static bool IsAvailable => ResolveTypes();

    /// <summary>Descriptors registered and handlers subscribed.</summary>
    public static bool IsBootstrapReady =>
        _bootstrapped && _configDescriptor != null && _commandDescriptor != null && _addCardAckDescriptor != null;

    public static void EnsureBootstrapped() {
        if (!IsAvailable || _bootstrapped) return;
        _bootstrapped = true;
        RegisterConfigDescriptor();
        RegisterCommandDescriptor();
        RegisterAddCardAckDescriptor();
        SubscribeConfig();
        SubscribeCommand();
        SubscribeAddCardAck();
    }

    public static void Shutdown() {
        _configSub?.Dispose();
        _configSub = null;
        _commandSub?.Dispose();
        _commandSub = null;
        _addCardAckSub?.Dispose();
        _addCardAckSub = null;
        _bootstrapped = false;
        _hostRevision = 0;
    }

    public static void SendAddCardAck(MpCheatAddCardAckMessage message) {
        if (!IsAvailable || _addCardAckDescriptor == null) return;
        EnsureBootstrapped();
        var netService = RunManager.Instance?.NetService;
        if (netService == null) return;
        SendToHost(netService, _addCardAckDescriptor, message);
    }

    public static void HostPublishConfig(MpCheatConfig config, string reason) {
        if (!MpCheatSession.IsHost || !IsAvailable) return;
        EnsureBootstrapped();

        _hostRevision++;
        MpCheatState.ApplySnapshot(config, _hostRevision, reason);

        var netService = RunManager.Instance?.NetService;
        if (netService == null || _configDescriptor == null) return;

        var msg = new MpCheatConfigSnapshotMessage { Revision = _hostRevision, Config = config };
        Broadcast(netService, _configDescriptor, msg);
        MpCheatRunSavedData.TryWrite(config);
    }

    public static void BroadcastCommand(MpCheatCommandMessage message) {
        if (!MpCheatSession.IsHost || !IsAvailable || _commandDescriptor == null) return;
        EnsureBootstrapped();
        var netService = RunManager.Instance?.NetService;
        if (netService == null) return;
        Broadcast(netService, _commandDescriptor, message);
    }

    private static bool ResolveTypes() {
        if (_resolved) return _typedRegistryType != null;
        _resolved = true;
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            _typedRegistryType ??= asm.GetType(TypedRegistryTypeName, throwOnError: false);
        return _typedRegistryType != null;
    }

    private static void RegisterConfigDescriptor() {
        _configDescriptor = CreateDescriptor<MpCheatConfigSnapshotMessage>(ConfigMessageKey);
    }

    private static void RegisterCommandDescriptor() {
        _commandDescriptor = CreateDescriptor<MpCheatCommandMessage>(CommandMessageKey);
    }

    private static void RegisterAddCardAckDescriptor() {
        _addCardAckDescriptor = CreateDescriptor<MpCheatAddCardAckMessage>(AddCardAckMessageKey);
    }

    private static object? CreateDescriptor<T>(string messageKey) {
        try {
            var descriptorType = _typedRegistryType!.Assembly
                .GetType("STS2RitsuLib.Networking.Sidecar.RitsuLibSidecarMessageDescriptor`1", throwOnError: false);
            if (descriptorType == null) return null;

            var deliveryType = _typedRegistryType.Assembly
                .GetType("STS2RitsuLib.Networking.Sidecar.RitsuLibSidecarDeliverySemantics", throwOnError: false);
            if (deliveryType == null) return null;

            var concrete = descriptorType.MakeGenericType(typeof(T));
            var stableSync = Enum.Parse(deliveryType, "StableSync");
            // C# 12 cannot name Func<ReadOnlySpan<byte>, T> in source; build deserialize via reflection.
            var serialize = (Func<T, byte[]>)((T m) => JsonSerializer.SerializeToUtf8Bytes(m));
            var deserialize = CreateDeserializeDelegate<T>();
            return Activator.CreateInstance(
                concrete,
                MainFile.ModID,
                messageKey,
                serialize,
                deserialize,
                stableSync,
                false)!;
        }
        catch (Exception ex) {
            MainFile.Logger.Warn($"[MpCheat] CreateDescriptor<{typeof(T).Name}> failed: {ex.Message}");
            return null;
        }
    }

    private static void SubscribeConfig() {
        if (_configDescriptor == null) return;
        var handler = typeof(MpCheatSidecarBridge).GetMethod(
            nameof(OnConfigReceived),
            BindingFlags.NonPublic | BindingFlags.Static);
        if (handler == null) return;
        _configSub = Subscribe(_configDescriptor, typeof(MpCheatConfigSnapshotMessage), handler);
    }

    private static void SubscribeCommand() {
        if (_commandDescriptor == null) return;
        var handler = typeof(MpCheatSidecarBridge).GetMethod(
            nameof(OnCommandReceived),
            BindingFlags.NonPublic | BindingFlags.Static);
        if (handler == null) return;
        _commandSub = Subscribe(_commandDescriptor, typeof(MpCheatCommandMessage), handler);
    }

    private static void SubscribeAddCardAck() {
        if (_addCardAckDescriptor == null) return;
        var handler = typeof(MpCheatSidecarBridge).GetMethod(
            nameof(OnAddCardAckReceived),
            BindingFlags.NonPublic | BindingFlags.Static);
        if (handler == null) return;
        _addCardAckSub = Subscribe(_addCardAckDescriptor, typeof(MpCheatAddCardAckMessage), handler);
    }

    private static Delegate CreateDeserializeDelegate<T>() {
        var method = typeof(MpCheatSidecarBridge)
            .GetMethod(nameof(DeserializeSpan), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(typeof(T));
        var funcType = typeof(Func<,>).MakeGenericType(typeof(ReadOnlySpan<byte>), typeof(T));
        return Delegate.CreateDelegate(funcType, method);
    }

    private static T DeserializeSpan<T>(ReadOnlySpan<byte> payload) {
        return JsonSerializer.Deserialize<T>(payload)
               ?? throw new InvalidOperationException($"bad payload: {typeof(T).Name}");
    }

    private static IDisposable? Subscribe(object descriptor, Type payloadType, MethodInfo handler) {
        try {
            var subscribe = _typedRegistryType!.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m => m.Name == "Subscribe" && m.IsGenericMethod && m.GetParameters().Length == 2);
            var generic = subscribe.MakeGenericMethod(payloadType);

            var ctxType = _typedRegistryType.Assembly
                .GetType("STS2RitsuLib.Networking.Sidecar.RitsuLibSidecarTypedDispatchContext`1")?
                .MakeGenericType(payloadType);
            if (ctxType == null) return null;

            var actionType = typeof(Action<>).MakeGenericType(ctxType);
            var del = Delegate.CreateDelegate(actionType, handler);
            return generic.Invoke(null, [descriptor, del]) as IDisposable;
        }
        catch (Exception ex) {
            MainFile.Logger.Warn($"[MpCheat] Subscribe failed: {ex.Message}");
            return null;
        }
    }

    private static void OnConfigReceived(object ctx) {
        try {
            var msg = ctx.GetType().GetProperty("Message")?.GetValue(ctx) as MpCheatConfigSnapshotMessage;
            if (msg == null) return;
            if (msg.Revision <= MpCheatState.Revision) return;
            MpCheatState.ApplySnapshot(msg.Config, msg.Revision, "sidecar_snapshot");
        }
        catch (Exception ex) {
            MainFile.Logger.Warn($"[MpCheat] OnConfigReceived failed: {ex.Message}");
        }
    }

    private static void OnCommandReceived(object ctx) {
        try {
            var msg = ctx.GetType().GetProperty("Message")?.GetValue(ctx) as MpCheatCommandMessage;
            if (msg == null) return;
            MpCheatCommandExecutor.Execute(msg);
        }
        catch (Exception ex) {
            MainFile.Logger.Warn($"[MpCheat] OnCommandReceived failed: {ex.Message}");
        }
    }

    private static void OnAddCardAckReceived(object ctx) {
        try {
            var msg = ctx.GetType().GetProperty("Message")?.GetValue(ctx) as MpCheatAddCardAckMessage;
            if (msg == null) return;
            MpCheatCardAddCoordinator.OnAckReceived(msg);
        }
        catch (Exception ex) {
            MainFile.Logger.Warn($"[MpCheat] OnAddCardAckReceived failed: {ex.Message}");
        }
    }

    private static void SendToHost<T>(INetGameService netService, object descriptor, T message) {
        try {
            var send = _typedRegistryType!.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m => m.Name == "SendToHost" && m.IsGenericMethod && m.GetParameters().Length == 3);
            var generic = send.MakeGenericMethod(typeof(T));
            generic.Invoke(null, [netService, descriptor, message]);
        }
        catch (Exception ex) {
            MainFile.Logger.Warn($"[MpCheat] SendToHost failed: {ex.Message}");
        }
    }

    private static void Broadcast<T>(INetGameService netService, object descriptor, T message) {
        try {
            var broadcast = _typedRegistryType!.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .First(m => m.Name == "Broadcast" && m.IsGenericMethod && m.GetParameters().Length == 3);
            var generic = broadcast.MakeGenericMethod(typeof(T));
            generic.Invoke(null, [netService, descriptor, message]);
        }
        catch (Exception ex) {
            MainFile.Logger.Warn($"[MpCheat] Broadcast failed: {ex.Message}");
        }
    }

}
