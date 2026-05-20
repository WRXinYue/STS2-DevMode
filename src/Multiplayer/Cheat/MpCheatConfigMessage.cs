namespace DevMode.Multiplayer.Cheat;

public sealed class MpCheatConfigSnapshotMessage {
    public long Revision { get; set; }
    public MpCheatConfig Config { get; set; } = new();
}
