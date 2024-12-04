# StringLocalizationTester

A patcher that does nothing, but will print details about a specific string localization to help debug issues.

Settings:
- TargetRecord -> FormKey of the record you want to print strings about
- RecordTypeName (optional) -> Text name of the record type you want to print strings about, in Mutagen style, eg `Weapon`, `Npc`, `DialogResponse`
- SubrecordType -> 4 character subrecord you want to analyze: `FULL`, `DESC`

Output:
```
=============== RUNNING TEST ===============
Analyzing from winning override path: Update.esm => D:\SteamLibrary\steamapps\common\Skyrim Special Edition\Data\Update.esm
Mutagen Name field returned: Orcish Dagger
FULL record index: 14506
StringsOverlay lookup found:
  Orcish Dagger
  D:\SteamLibrary\steamapps\common\Skyrim Special Edition\Data\Skyrim - Interface.bsa\strings\update_english.strings
```

This will give information about where a particular string is being retrieved from, if at all.
