namespace FPacker.Services;

public sealed class BuildRequest {
    public required string ModName { get; init; }
    public required string SourceDirectory { get; init; }
    public required string OutputFile { get; init; }
    public bool RelocateConfigs { get; init; }
    public bool RelocateScripts { get; init; }
    public bool ProtectConfigs { get; init; }
    public bool AddJunkFiles { get; init; }
    public bool BinarizeConfigs { get; init; }
}
