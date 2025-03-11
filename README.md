# RustTranslationAPI

Oxide plugin for Rust. Provides translation APIs for various in-game elements such as items, holdables, deployables, and more.

This plugin offers a straightforward translation API, making it especially helpful for players whose first language isn’t English. It simplifies the process of accessing translated names and descriptions for Rust entities.

## Benefits for Plugin Developers

For plugins that integrate with RustTranslationAPI, manual translation of items, holdables, and deployables is no longer necessary. This plugin handles it all seamlessly.

## Configuration

```json
{
  "Log Level (Debug, Info, Warning, Error, Off)": "Off"
}
```

## Developer Hooks

```csharp
void OnTranslationsInitialized()
```

## API

```csharp
bool IsInitialized()
bool IsSupportedLanguage(string language)
    
string GetTranslation(string language, string token)

string GetLanguage(BasePlayer player)
string GetTranslation(string language, Translate.Phrase token)
string GetTranslation(BasePlayer player, Translate.Phrase token)
string GetTranslation(string language, Item item)
string GetTranslation(BasePlayer player, Item item)
string GetTranslation(string language, ItemDefinition def)
string GetTranslation(BasePlayer player, ItemDefinition def)
    
string GetTranslation(string language, BaseEntity entity)
string GetTranslation(BasePlayer player, BaseEntity entity)
string GetTranslation(string language, MonumentInfo monument)
string GetTranslation(BasePlayer player, MonumentInfo monument)
    
string GetTranslation(string language, Construction construction)
string GetTranslation(BasePlayer player, Construction monument)
string GetPrefabTranslation(string language, uint prefabId)
string GetPrefabTranslation(BasePlayer player, uint prefabId)
    
string GetItemDescriptionByID(string language, int itemID)
string GetItemDescriptionByID(BasePlayer player, int itemID)
string GetItemDescriptionByDefinition(string language, ItemDefinition def)
string GetItemDescriptionByDefinition(BasePlayer player, ItemDefinition def)
    
string GetItemTranslationByID(string language, int itemID)
string GetItemTranslationByDisplayName(string language, string displayName)
string GetItemTranslationByDefinition(string language, ItemDefinition def)
string GetItemTranslationByShortName(string language, string itemShortName)
    
string GetDeployableTranslation(string language, string deployable)
string GetHoldableTranslation(string language, string holdable)
string GetMonumentTranslation(string language, string monumentName)
string GetMonumentTranslation(string language, MonumentInfo monumentInfo)
string GetConstructionTranslation(string language, string constructionName)
string GetConstructionTranslation(string language, Construction construction)
```

## Credits

* [**Arainrr**](https://umod.org/user/Arainrr) original author of the plugin
* [**MJSU**](https://umod.org/user/MJSU) author of the rewritten Version 2
