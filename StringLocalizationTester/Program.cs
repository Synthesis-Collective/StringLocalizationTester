using IniParser;
using IniParser.Model.Configuration;
using IniParser.Parser;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Archives;
using Mutagen.Bethesda.Environments.DI;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Fallout4;
using Mutagen.Bethesda.Inis.DI;
using Mutagen.Bethesda.Installs.DI;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Analysis;
using Mutagen.Bethesda.Plugins.Aspects;
using Mutagen.Bethesda.Plugins.Binary.Streams;
using Mutagen.Bethesda.Plugins.Cache;
using Mutagen.Bethesda.Plugins.Records;
using Mutagen.Bethesda.Plugins.Records.Mapping;
using Mutagen.Bethesda.Starfield;
using Mutagen.Bethesda.Strings;
using Noggog;

namespace StringLocalizationTester
{
    public class Program
    {
        private static Lazy<Settings> _lazySettings = default!;
        
        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .SetAutogeneratedSettings("Settings", "settings.json", out _lazySettings)
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .AddPatch<IFallout4Mod, IFallout4ModGetter>(RunPatch)
                .AddPatch<IStarfieldMod, IStarfieldModGetter>(RunPatch)
                .SetTypicalOpen(GameRelease.SkyrimSE, "YourPatcher.esp")
                .Run(args);
        }

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            RunPatchInternal(state, state.LinkCache);
        }

        public static void RunPatch(IPatcherState<IFallout4Mod, IFallout4ModGetter> state)
        {
            RunPatchInternal(state, state.LinkCache);
        }

        public static void RunPatch(IPatcherState<IStarfieldMod, IStarfieldModGetter> state)
        {
            RunPatchInternal(state, state.LinkCache);
        }

        public static void RunPatchInternal(IPatcherState env, ILinkCache linkCache)
        {
            Console.WriteLine("=============== RUNNING TEST ===============");
            Console.WriteLine($"Running on record {_lazySettings.Value.TargetRecord.FormKey} of type {_lazySettings.Value.TargetRecord} on subrecord {_lazySettings.Value.SubrecordType}");
            
            if (!RecordType.TryFactory(_lazySettings.Value.SubrecordType, out var recType))
            {
                throw new ArgumentException("Need to set the SubrecordType in the settings to a 4 letter character: e.g. 'DESC'");
            }

            if (!GetterTypeMapping.Instance.TryGetGetterType($"Mutagen.Bethesda.{env.GameRelease.ToCategory()}.{_lazySettings.Value.RecordTypeName}", out var targetType))
            {
                Console.WriteLine($"Could not find target type for: {_lazySettings.Value.TargetRecord}.  Set the RecordTypeName in the settings to the name of the record type: e.g. 'Book', 'Weapon', etc");
                return;
            }
            
            var context = linkCache.ResolveSimpleContext(_lazySettings.Value.TargetRecord.FormKey, targetType);
            Test(
                Path.Combine(env.DataFolderPath, context.ModKey.FileName),
                _lazySettings.Value.TargetRecord.FormKey,
                env.GameRelease,
                env.DataFolderPath,
                recType,
                targetType);
        }

        public static void Test(
            ModPath modPath,
            FormKey formKey,
            GameRelease release,
            DirectoryPath dataPath,
            RecordType recType,
            Type targetType)
        {
            var locs = RecordLocator.GetLocations(modPath, release, null);
            var loc = locs.GetRecord(formKey);
            
            using var mod = ModInstantiator.ImportGetter(modPath, release);
            var linkCache = mod.ToUntypedImmutableLinkCache();
            var rec = linkCache.Resolve(formKey, targetType);
            if (rec is not INamedGetter named)
            {
                throw new ArgumentException("Command was given a record that was not Named");
            }
            
            Console.WriteLine($"Analyzing from winning override path: {modPath}");
            Console.WriteLine($"Is mod localized? {mod.UsingLocalization}");
            Console.WriteLine($"Mutagen Name field returned: {named.Name}");

            if (!mod.UsingLocalization)
            {
                Console.WriteLine("Mod was not localized.  String was embedded.  No further testing needed.");
                return;
            }

            var iniPathProvider = new IniPathProvider(
                new GameReleaseInjection(release),
                new IniPathLookup(
                    new GameLocatorLookupCache()));
            Console.WriteLine($"Ini Path: {iniPathProvider.Path}");
            
            // Release exists as parameter, in case future games need different handling
            IniParserConfiguration iniConfig = new()
            {
                AllowDuplicateKeys = true,
                AllowDuplicateSections = true,
                AllowKeysWithoutSection = true,
                AllowCreateSectionsOnFly = true,
                CaseInsensitive = true,
                SkipInvalidLines = true,
            };
            var parser = new FileIniDataParser(new IniDataParser(iniConfig));
            var data = parser.ReadData(new StreamReader(File.OpenRead(iniPathProvider.Path)));
            var basePath = data["Archive"];
            Console.WriteLine("Ini [Archive] section:");
            foreach (var x in basePath)
            {
                Console.WriteLine($"  {x.KeyName}: {x.Value}");
            }
            
            var iniListings = Archive.GetIniListings(release);
            Console.WriteLine("Ini Listings from API:");
            foreach (var iniListing in iniListings)
            {
                Console.WriteLine($"  {iniListing}");
            }
            
            using var stream = new MutagenBinaryReadStream(modPath, release, null);
            stream.Position = loc.Location.Min;

            var majorFrame = stream.ReadMajorRecord();
            if (majorFrame.IsCompressed)
            {
                majorFrame = majorFrame.Decompress(out var _);
            }
            var full = majorFrame.FindSubrecord(recType);
            Console.WriteLine($"{_lazySettings.Value.SubrecordType} record index: {full.AsUInt32()}");

            var stringsOverlay = StringsFolderLookupOverlay.TypicalFactory(
                release,
                modPath.ModKey,
                dataPath,
                null);

            if (!stringsOverlay.TryLookup(StringsSource.Normal, Language.English, full.AsUInt32(), 
                    out var str,
                    out var sourcePath))
            {
                Console.WriteLine($"StringsOverlay lookup found:");
                Console.WriteLine($"  {str}");
                Console.WriteLine($"  {sourcePath}");
            }
            else
            {
                Console.WriteLine($"Couldnt look up index {full.AsUInt32()}");
            }
            
        }
    }
}
