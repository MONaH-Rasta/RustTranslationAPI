using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using UnityEngine;

namespace Oxide.Plugins;

[Info("Rust Translation API", "MJSU", "2.0.1")]
[Description("Provides translations for Rust entities & items")]
public class RustTranslationAPI : RustPlugin
{
    #region Class Fields

    private static readonly string LOGLine = new('=', 30);

    private readonly Dictionary<string, Dictionary<string, string>> _languages = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _constructionTokens = new();
    private readonly Dictionary<string, string> _deployableTokens = new();
    private readonly Dictionary<string, string> _displayNameTokens = new();
    private readonly Dictionary<string, string> _holdableTokens = new();
    private readonly Dictionary<string, string> _monumentTokens = new();

    public enum LogLevel
    {
        Off = 0,
        Error = 1,
        Warning = 2,
        Info = 3,
        Debug = 4,
    }

    #endregion Class Fields

    #region Initialization

    private void OnServerInitialized()
    {
        Log($"{LOGLine}\nOnServerInitialized: start", LogLevel.Debug);
        ProcessTranslations();
        ProcessItems();
        ProcessMonuments();
        ProcessConstruction();
        Log("OnServerInitialized: finish", LogLevel.Debug);
    }

    private void Unload()
    {
        Log($"Plugin unloaded\n{LOGLine}\n", LogLevel.Debug);
    }

    #endregion Initialization

    #region Configuration

    private PluginConfig _pluginConfig;

    public class PluginConfig
    {
        [JsonConverter(typeof(StringEnumConverter))]
        [DefaultValue(LogLevel.Off)]
        [JsonProperty(PropertyName = "Log Level (Debug, Info, Warning, Error, Off)", Order = 4)]
        public LogLevel LoggingLevel { get; set; }
    }

    protected override void LoadDefaultConfig() => PrintWarning("Loading Default Config");

    protected override void LoadConfig()
    {
        base.LoadConfig();
        Config.Settings.DefaultValueHandling = DefaultValueHandling.Populate;
        _pluginConfig = AdditionalConfig(Config.ReadObject<PluginConfig>());
        Config.WriteObject(_pluginConfig);
    }

    public static PluginConfig AdditionalConfig(PluginConfig config) => config;

    #endregion Configuration

    #region Core Methods

    public void ProcessTranslations()
    {
        AssetBundleBackend assets = (AssetBundleBackend)FileSystem.Backend;
        Dictionary<string, AssetBundle> files = assets.files;

        foreach ((string path, AssetBundle bundle) in files!)
        {
            if (!path.EndsWith(".json"))
            {
                continue;
            }

            if (bundle.LoadAsset(path) is TextAsset textAsset)
            {
                int lastIndex = path.LastIndexOf('/');
                int secondLastIndex = path.LastIndexOf('/', lastIndex - 1) + 1;
                string language = path[secondLastIndex..lastIndex];

                if (!_languages.TryGetValue(language, out Dictionary<string, string> tokens))
                {
                    _languages[language] = tokens = new();
                    Log($"Added language: {language}", LogLevel.Debug);
                }

                foreach ((string token, string translation) in JsonConvert.DeserializeObject<Dictionary<string, string>>(textAsset.text))
                {
                    tokens[token] = translation;
                }

                Log($"Loaded {tokens.Count} tokens for language: {language}", LogLevel.Debug);
            }
        }

        Log($"Loaded {_languages.Count} languages", LogLevel.Debug);
    }

    public void ProcessItems()
    {
        foreach (ItemDefinition def in ItemManager.GetItemDefinitions())
        {
            _displayNameTokens[def.displayName.english] = def.displayName.token;
            if (def.GetComponent<ItemModDeployable>() is { } itemModDeployable)
            {
                string prefabName = itemModDeployable.entityPrefab?.resourcePath;
                if (!string.IsNullOrEmpty(prefabName))
                {
                    string shortPrefabName = Path.GetFileNameWithoutExtension(prefabName);
                    if (!string.IsNullOrEmpty(shortPrefabName))
                    {
                        _deployableTokens[shortPrefabName] = def.displayName.token;
                    }
                }
            }

            if (def.GetComponent<ItemModEntity>() is { } itemModEntity)
            {
                HeldEntity heldEntity = itemModEntity.entityPrefab?.Get()?.GetComponent<HeldEntity>();
                if (heldEntity && heldEntity is not Planner && heldEntity is not Deployer)
                {
                    if (!string.IsNullOrEmpty(heldEntity.PrefabName))
                    {
                        string shortPrefabName = Path.GetFileNameWithoutExtension(heldEntity.PrefabName);
                        if (!string.IsNullOrEmpty(shortPrefabName))
                        {
                            _holdableTokens[shortPrefabName] = def.displayName.token;
                        }
                    }
                    ThrownWeapon thrownWeapon = heldEntity as ThrownWeapon;
                    if (thrownWeapon)
                    {
                        string prefabName = thrownWeapon.prefabToThrow?.resourcePath;
                        if (!string.IsNullOrEmpty(prefabName))
                        {
                            string shortPrefabName = Path.GetFileNameWithoutExtension(prefabName);
                            if (!string.IsNullOrEmpty(shortPrefabName))
                            {
                                _holdableTokens[shortPrefabName] = def.displayName.token;
                            }
                        }
                    }
                }
            }
        }

        Log($"Loaded {_displayNameTokens.Count} display names", LogLevel.Debug);
    }

    public void ProcessMonuments()
    {
        foreach (MonumentInfo monumentInfo in TerrainMeta.Path.Monuments)
        {
            if (monumentInfo.displayPhrase.IsValid())
            {
                string shortPrefabName = Path.GetFileNameWithoutExtension(monumentInfo.name);
                _monumentTokens[shortPrefabName] = monumentInfo.displayPhrase.token;
            }
        }

        Log($"Loaded {_monumentTokens.Count} monuments", LogLevel.Debug);
    }

    public void ProcessConstruction()
    {
        foreach (PrefabAttribute.AttributeCollection attributes in PrefabAttribute.server.prefabs.Values)
        {
            Construction construction = attributes.Find<Construction>().FirstOrDefault();
            if (construction && !construction!.deployable && construction.info.name.IsValid())
            {
                string shortPrefabName = Path.GetFileNameWithoutExtension(construction.fullName);
                _constructionTokens[shortPrefabName] = construction.info.name.token;
            }
        }

        Log($"Loaded {_constructionTokens.Count} constructions", LogLevel.Debug);
    }

    #endregion Core Methods

    #region API Methods

    private string GetTranslation(string language, string token)
    {
        if (!string.IsNullOrEmpty(language) && !string.IsNullOrEmpty(token) &&
            _languages.TryGetValue(language, out Dictionary<string, string> tokens) &&
            tokens.TryGetValue(token, out string translation))
        {
            return translation;
        }
        return string.Empty;
    }

    private string GetTranslation(string language, Translate.Phrase token)
        => GetTranslation(language, token?.token ?? string.Empty);

    private string GetItemTranslationByID(string language, int itemID)
        => GetItemTranslationByDefinition(language, ItemManager.FindItemDefinition(itemID));

    private string GetItemTranslationByDisplayName(string language, string displayName)
        => _displayNameTokens.TryGetValue(displayName, out string token) ? GetTranslation(language, token) : string.Empty;

    private string GetItemTranslationByDefinition(string language, ItemDefinition def)
        => GetTranslation(language, def?.displayName.token ?? string.Empty);

    private string GetItemTranslationByShortName(string language, string itemShortName)
        => GetItemTranslationByDefinition(language, ItemManager.FindItemDefinition(itemShortName));

    private string GetPrefabTranslation(string language, uint prefabId)
        => GetTranslation(language, PrefabAttribute.server.Find<PrefabInformation>(prefabId)?.title.token ?? string.Empty);

    private string GetTranslation(string language, BaseEntity entity)
        => entity.IsValid() ? GetPrefabTranslation(language, entity.prefabID) : string.Empty;

    private string GetDeployableTranslation(string language, string deployable)
        => _deployableTokens.TryGetValue(deployable, out string token) ? GetTranslation(language, token) : string.Empty;

    private string GetHoldableTranslation(string language, string holdable)
        => _holdableTokens.TryGetValue(holdable, out string token) ? GetTranslation(language, token) : string.Empty;

    private string GetMonumentTranslation(string language, MonumentInfo monument)
        => GetTranslation(language, monument?.displayPhrase.token ?? string.Empty);

    private string GetMonumentTranslation(string language, string monumentName)
        => _monumentTokens.TryGetValue(monumentName, out string token) ? GetTranslation(language, token) : string.Empty;

    private string GetConstructionTranslation(string language, string constructionName)
        => _constructionTokens.TryGetValue(constructionName, out string token) ? GetTranslation(language, token) : string.Empty;

    #endregion API Methods

    #region Helpers

    public void Log(string message, LogLevel level = LogLevel.Info, string filename = "log", [CallerMemberName] string methodName = null)
    {
        switch (level)
        {
            case LogLevel.Error:
                PrintError(message);
                message = $"{DateTime.Now:HH:mm:ss} {methodName} {message}";
                break;
            case LogLevel.Warning:
                PrintWarning(message);
                message = $"{DateTime.Now:HH:mm:ss} {methodName} {message}";
                break;
            case LogLevel.Debug:
                message = $"{DateTime.Now:HH:mm:ss} {methodName} {message}";
                break;
            case LogLevel.Off:
            case LogLevel.Info:
                break;
            default:
                message = $"{DateTime.Now:HH:mm:ss} {message}";
                break;
        }

        if ((int)_pluginConfig.LoggingLevel >= (int)level)
        {
            LogToFile(filename, message, this);
        }
    }

    #endregion Helpers
}