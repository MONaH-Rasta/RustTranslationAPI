using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Oxide.Plugins;

[Info("Rust Translation API", "MJSU", "2.0.0")]
[Description("Provides translations for Rust entities & items")]
public class RustTranslationAPI : RustPlugin
{
    #region Class Fields
    private readonly Dictionary<string, Dictionary<string, string>> _languages = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _displayNameTokens = new();
    private readonly Dictionary<string, string> _deployableTokens = new();
    private readonly Dictionary<string, string> _holdableTokens = new();
    private readonly Dictionary<string, string> _monumentTokens = new();
    private readonly Dictionary<string, string> _constructionTokens = new();
    private readonly Dictionary<uint, string> _prefabTokens = new();
    #endregion

    #region Setup & Loading
    private void OnServerInitialized()
    {
        ProcessTranslations();
        ProcessTypes();
    }
    #endregion

    #region Chat Command
    [ChatCommand("rtb")]
    private void RustTranslationDebug(BasePlayer player)
    {
        if (!player.IsAdmin)
        {
            player.ChatMessage("You don't have permission to use this command.");
            return;
        }

        timer.Every(1f, () =>
        {
            if (Physics.Raycast(player.eyes.HeadRay(), out RaycastHit hit, 10f))
            {
                BaseEntity entity = hit.GetEntity();
                if (entity)
                {
                    Puts(string.Join(", ", PrefabAttribute.server.prefabs[entity.prefabID].attributes.Keys.Select(a => a.ToString())));
                    var concs = PrefabAttribute.server.prefabs[entity.prefabID].Find<Construction>();
                    Construction conc = concs.FirstOrDefault();
                    if (conc)
                    {
                        Puts($"{conc} {conc.prefabID} {conc.info.name.token}");
                    }
                    else
                    {
                        Puts($"{concs.Length}");
                    }
                    
                    player.ChatMessage($"Translation: \"{GetPrefabTranslation("en", entity.prefabID)}\"");
                }
                else
                {
                    player.ChatMessage("No Entity");
                }
            }
            else
            {
                player.ChatMessage("No Hit");
            }
        });
        
        player.ChatMessage("Started Debug");
    }
    #endregion

    #region API

    private string GetTranslation(string language, string token)
    {
        if (!string.IsNullOrEmpty(language) && !string.IsNullOrEmpty(token) && _languages.TryGetValue(language, out Dictionary<string, string> tokens) && tokens.TryGetValue(token, out string translation))
        {
            return translation;
        }

        return token;
    }

    private string GetTranslation(string language, Translate.Phrase token) => GetTranslation(language, token?.token);
    private string GetTranslation(string language, Item item) => GetTranslation(language, item?.info);
    private string GetTranslation(string language, ItemDefinition def) => GetTranslation(language, def?.displayName);
    private string GetTranslation(string language, BaseEntity entity) => GetPrefabTranslation(language, entity.prefabID);
    private string GetTranslation(string language, MonumentInfo monument) => GetTranslation(language, monument?.displayPhrase);
    
    private string GetItemDescriptionByID(string language, int itemID) => GetItemDescriptionByDefinition(language, ItemManager.FindItemDefinition(itemID));
    private string GetItemDescriptionByDefinition(string language, ItemDefinition def) => GetTranslation(language, def?.displayDescription);
    
    
    private string GetItemTranslationByID(string language, int itemID) => GetTranslation(language, ItemManager.FindItemDefinition(itemID));
    private string GetItemTranslationByDisplayName(string language, string displayName) => _displayNameTokens.TryGetValue(displayName, out string token) ? GetTranslation(language, token) : null;
    private string GetItemTranslationByDefinition(string language, ItemDefinition def) => GetTranslation(language, def);
    private string GetItemTranslationByShortName(string language, string itemShortName) => GetTranslation(language, ItemManager.FindItemDefinition(itemShortName));
    
    private string GetPrefabTranslation(string language, uint prefabId) => _prefabTokens.TryGetValue(prefabId, out string token) ? GetTranslation(language, token) : null;
    private string GetDeployableTranslation(string language, string deployable) => _deployableTokens.TryGetValue(deployable, out string token) ? GetTranslation(language, token) : null;
    private string GetHoldableTranslation(string language, string holdable) => _holdableTokens.TryGetValue(holdable, out string token) ? GetTranslation(language, token) : null;
    private string GetMonumentTranslation(string language, string monumentName) => _monumentTokens.TryGetValue(monumentName, out string token) ? GetTranslation(language, token) : null;
    private string GetConstructionTranslation(string language, string constructionName) => _constructionTokens.TryGetValue(constructionName, out string token) ? GetTranslation(language, token) : null;
    #endregion

    #region Processing
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
            
            Object asset = bundle.LoadAsset(path);
            if (asset is TextAsset text)
            {
                int lastIndex = path.LastIndexOf('/');
                int secondLastIndex = path.LastIndexOf('/', lastIndex - 1) + 1;
                string language = path[secondLastIndex..lastIndex];

                if (!_languages.TryGetValue(language, out Dictionary<string, string> tokens))
                {
                    _languages[language] = tokens = new Dictionary<string, string>();
                }

                foreach ((string token, string translation) in JsonConvert.DeserializeObject<Dictionary<string, string>>(text.text))
                {
                    tokens[token] = translation;
                }
            }
        }
    }

    private void ProcessTypes()
    {
        ProcessItems();
        ProcessMonuments();
        ProcessAttributes();
    }

    private void ProcessItems()
    {
        foreach (ItemDefinition def in ItemManager.GetItemDefinitions())
        {
            _displayNameTokens[def.displayName.english] = def.displayName.token;
            BaseEntity deployableEntity = def.GetComponent<ItemModDeployable>()?.entityPrefab.GetEntity();
            if (deployableEntity)
            {
                _deployableTokens[deployableEntity.ShortPrefabName] = def.displayName.token;
                _prefabTokens[deployableEntity.prefabID] = def.displayName.token;
            }
            
            HeldEntity heldEntity = def.GetComponent<ItemModEntity>()?.entityPrefab?.Get()?.GetComponent<HeldEntity>();
            if (heldEntity&& heldEntity is not Planner && heldEntity is not Deployer)
            {
                _holdableTokens[heldEntity.ShortPrefabName] = def.displayName.token;
                _prefabTokens[heldEntity.prefabID] = def.displayName.token;
                if (heldEntity is ThrownWeapon thrownWeapon)
                {
                    BaseEntity thrownEntity = thrownWeapon.prefabToThrow.GetEntity();
                    _holdableTokens[thrownEntity.ShortPrefabName] = def.displayName.token;
                    _prefabTokens[thrownEntity.prefabID] = def.displayName.token;
                }
            }
        }
    }

    private void ProcessMonuments()
    {
        foreach (MonumentInfo monumentInfo in TerrainMeta.Path.Monuments)
        {
            if (monumentInfo.displayPhrase.IsValid())
            {
                string shortPrefabName = Path.GetFileNameWithoutExtension(monumentInfo.name);
                _monumentTokens[shortPrefabName] = monumentInfo.displayPhrase.token;
            }
        }
    }

    private void ProcessAttributes()
    {
        foreach (PrefabAttribute.AttributeCollection attributes in PrefabAttribute.server.prefabs.Values)
        {
            Construction construction = attributes.Find<Construction>().FirstOrDefault();
            if (construction && !construction!.deployable && construction.info.name.IsValid())
            {
                string shortPrefabName = Path.GetFileNameWithoutExtension(construction.fullName);
                _constructionTokens[shortPrefabName] = construction.info.name.token;
                _prefabTokens[construction.prefabID] = construction.info.name.token;
            }
            
            PrefabInformation prefabInfo =  attributes.Find<PrefabInformation>().FirstOrDefault();
            if (prefabInfo)
            {
                _prefabTokens[prefabInfo!.prefabID] = prefabInfo.title.token;
            }
        }
    }
    #endregion
}