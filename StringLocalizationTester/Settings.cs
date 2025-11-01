using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Strings;

namespace StringLocalizationTester;

public class Settings
{
    public IFormLinkGetter<IMajorRecordGetter> TargetRecord { get; set; } = new FormLink<IMajorRecordGetter>();
    public string? RecordTypeName { get; set; } = null;
    public string SubrecordType { get; set; } = "FULL";
    public Language Language { get; set; } = Language.English;
}