# RustTranslationAPI

Oxide plugin for Rust. Provides translation APIs for Rust items, holdables, deployables, etc.

A simple translation API plugin. This is useful for players whose native language is not English.

**For plugins that support this plugin. You no longer need to translate items, holdables, deployables**

Languages supported by Rust:
```json
"af", "ar", "ca", "cs", "da", "de", "el", "en-PT", "es-ES", "fi", "fr", "he", "hu", "it", "ja", "ko", "nl", "no", "pl", "pt-BR", "pt-PT", "ro", "ru", "sr", "sv-SE", "tr", "uk", "vi", "zh-CN", "zh-TW", "en"
```

## Configuration 

```json
{
  "Translations override": {
    "zh-CN": {//language code (ISO 639-1)
      "fogmachine": "喷雾机"//fogmachine is the translation token. You can find them in the 'engine.json' for each language folder in the oxide/data/Translations
    },
  }
}
```

## HOOK
```csharp
void OnTranslationsInitialized()  // It may be called multiple times
```

## API

```csharp
string GetItemTranslationByID(string language, int itemID)
string GetItemTranslationByDisplayName(string language, string displayName)
string GetItemTranslationByDefinition(string language, ItemDefinition itemDefinition)
string GetItemTranslationByShortName(string language, string itemShortName)

//The following APIs support short prefab names and prefab names
string GetDeployableTranslation(string language, string deployable)
string GetHoldableTranslation(string language, string holdable)
string GetMonumentTranslation(string language, string monumentName)
string GetConstructionTranslation(string language, string constructionName)
```