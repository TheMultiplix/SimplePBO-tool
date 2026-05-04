using FPacker.Builders;

namespace FPacker.Services;

public static class PboBuildService {
    public static void Build(BuildRequest request) {
        Validate(request);

        Directory.CreateDirectory(Path.GetDirectoryName(request.OutputFile)!);

        using var builder = new PboBuilder(request.ModName).WithEntryBuilder(entryBuilder => {
            if (request.RelocateConfigs) {
                entryBuilder.WithRelocatedConfigs();
            }

            if (request.RelocateScripts) {
                entryBuilder.WithRelocatedScripts();
            }

            if (request.ProtectConfigs) {
                entryBuilder.WithConfigProtection();
            }

            if (request.AddJunkFiles) {
                entryBuilder.WithJunkFiles();
            }

            if (!request.BinarizeConfigs) {
                entryBuilder.WithoutBinarizedConfigs();
            }

            entryBuilder.FromDirectory(request.SourceDirectory);
        });

        using var pboStream = builder.Build();
        File.WriteAllBytes(request.OutputFile, pboStream.ToArray());
    }

    private static void Validate(BuildRequest request) {
        if (string.IsNullOrWhiteSpace(request.ModName)) {
            throw new InvalidOperationException("Enter a mod name.");
        }

        if (string.IsNullOrWhiteSpace(request.SourceDirectory) || !Directory.Exists(request.SourceDirectory)) {
            throw new DirectoryNotFoundException("Choose a valid source folder.");
        }

        if (string.IsNullOrWhiteSpace(request.OutputFile)) {
            throw new InvalidOperationException("Choose where to save the PBO file.");
        }

        var outputDirectory = Path.GetDirectoryName(request.OutputFile);
        if (string.IsNullOrWhiteSpace(outputDirectory)) {
            throw new InvalidOperationException("Choose a valid output file path.");
        }
    }
}
