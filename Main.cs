using BepInEx;
using RoR2;
using R2API;
using R2API.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Security.Permissions;
using RoR2.Projectile;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;
using RoR2.UI;
using TMPro;
using UnityEngine.UI;
using EntityStates;
using RoR2.Skills;
using Demolisher;
using static Demolisher.Skills;
using static Demolisher.Main;
using static Demolisher.ContentPacks;
using Newtonsoft.Json.Utilities;
using RoR2.HudOverlay;
using KinematicCharacterController;
using System.Security;
using RoR2.ContentManagement;
using System.Collections;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;
using JetBrains.Annotations;
using BodyModelAdditionsAPI;
using static BodyModelAdditionsAPI.Main;
using R2API.Networking.Interfaces;
using R2API.Networking;
using HG;
using UnityEngine.EventSystems;
using BepInEx.Configuration;
using RiskOfOptions;
using RiskOfOptions.Options;
using static NetworkConfigs.Main;
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[assembly: HG.Reflection.SearchableAttribute.OptIn]
[assembly: HG.Reflection.SearchableAttribute.OptInAttribute]
[module: UnverifiableCode]
#pragma warning disable CS0618
#pragma warning restore CS0618
namespace Demolisher
{
    [BepInPlugin(ModGuid, ModName, ModVer)]
    [BepInDependency(R2API.R2API.PluginGUID, R2API.R2API.PluginVersion)]
    [BepInDependency(R2API.Networking.NetworkingAPI.PluginGUID)]
    [BepInDependency(BodyModelAdditionsAPI.Main.ModGuid, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(NetworkConfigs.Main.ModGuid, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.weliveinasociety.CustomEmotesAPI", BepInDependency.DependencyFlags.SoftDependency)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    //[R2APISubmoduleDependency(nameof(CommandHelper))]
    [System.Serializable]
    public class Main : BaseUnityPlugin
    {
        public const string ModGuid = "com.brynzananas.demolisher";
        public const string ModName = "Demolisher";
        public const string ModVer = "1.0.4";

        private static bool emotesEnabled;
        public static BepInEx.PluginInfo PInfo { get; private set; }
        public static AssetBundle ThunderkitAssets;
        public static Dictionary<string, UnityEngine.Object> assetsDictionary = new Dictionary<string, UnityEngine.Object>();
        public static Dictionary<string, string> tokenReplace = new Dictionary<string, string>();
        public static List<BuffDef> buffsToTrack = new List<BuffDef>();
        public static Dictionary<int, GameObject> idToEffect = new Dictionary<int, GameObject>();
        public static Dictionary<GameObject, int> effectToId = new Dictionary<GameObject, int>();
        public static Dictionary<string, string> objectsActualNames = new Dictionary<string, string>();
        public static Dictionary<string, string> tokenModifications = new Dictionary<string, string>();
        
        public static List<string> tokensToModify = new List<string>();
        public static SurvivorDef DemoSurvivorDef;
        public static BodyIndex DemoBodyIndex;
        public static SkinDef DemoDefaultSkin;
        public static SkinDef DemoNuclearSkin;
        public static ConfigFile ConfigFile;
        public void Awake()
        {
            PInfo = Info;
            ConfigFile = Config;
            ThunderkitAssets = AssetBundle.LoadFromFile(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(PInfo.Location), "assetbundles", "demomanpackage"));
            SoundAPI.SoundBanks.Add(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(PInfo.Location), "soundbanks", "Demoman.bnk"));
            foreach (Material material in ThunderkitAssets.LoadAllAssets<Material>())
            {
                if (!material.shader.name.StartsWith("StubbedRoR2"))
                {
                    continue;
                }
                string shaderName = material.shader.name.Replace("StubbedRoR2", "RoR2") + ".shader";
                Shader replacementShader = Addressables.LoadAssetAsync<Shader>(shaderName).WaitForCompletion();
                if (replacementShader)
                {
                    material.shader = replacementShader;
                }
            }

            foreach (SkillFamily skillFamily in ThunderkitAssets.LoadAllAssets<SkillFamily>())
            {
                string name = (skillFamily as UnityEngine.Object).name;
                (skillFamily as ScriptableObject).name = name;
                assetsDictionary.Add(name, skillFamily);
                skillFamilies.Add(skillFamily);
                //ContentAddition.AddSkillFamily(skillFamily);
            }
            emotesEnabled = BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(EmoteCompatAbility.customEmotesApiGUID);
            //CreateGmodAssets();
            CreateConfigs();
            CreateINetMessages();
            CreateAssets();
            CreateSurvivor();
            CreateSounds();
            Skills.Init();
            skillsToStatsEvent += Main_skillsToStatsEvent;
            On.RoR2.CharacterBody.OnKilledOtherServer += CharacterBody_OnKilledOtherServer;
            On.RoR2.HealthComponent.TakeDamageProcess += HealthComponent_TakeDamageProcess;
            On.RoR2.CharacterMotor.FixedUpdate += CharacterMotor_FixedUpdate;
            On.RoR2.CharacterMotor.OnLanded += CharacterMotor_OnLanded;
            On.RoR2.CharacterMotor.OnMovementHit += CharacterMotor_OnMovementHit;
            On.RoR2.CharacterBody.OnBuffFirstStackGained += CharacterBody_OnBuffFirstStackGained;
            On.RoR2.CharacterBody.OnBuffFinalStackLost += CharacterBody_OnBuffFinalStackLost;
            R2API.RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
            On.RoR2.GlobalEventManager.IsImmuneToFallDamage += GlobalEventManager_IsImmuneToFallDamage;
            On.RoR2.GlobalEventManager.OnHitEnemy += GlobalEventManager_OnHitEnemy;
            On.RoR2.UI.LoadoutPanelController.Row.AddButton += Row_AddButton;
            Run.onRunStartGlobal += Run_onRunStartGlobal;
            On.RoR2.Run.OnEnable += Run_OnEnable;
            On.RoR2.Run.OnDisable += Run_OnDisable;
            On.EntityStates.GenericCharacterMain.CanExecuteSkill += GenericCharacterMain_CanExecuteSkill;
            On.EntityStates.GenericCharacterMain.HandleMovements += GenericCharacterMain_HandleMovements;
            On.RoR2.MapZone.TryZoneStart += MapZone_TryZoneStart;
            On.RoR2.BodyCatalog.SetBodyPrefabs += BodyCatalog_SetBodyPrefabs;
            On.RoR2.Language.GetLocalizedStringByToken += Language_GetLocalizedStringByToken;
            ContentManager.collectContentPackProviders += (addContentPackProvider) =>
            {
                addContentPackProvider(new ContentPacks());
            };

        }

        private string Language_GetLocalizedStringByToken(On.RoR2.Language.orig_GetLocalizedStringByToken orig, Language self, string token)
        {
            if (tokensToModify.Contains(token))
            {
                int i = 0;
                if (!self.stringsByToken.ContainsKey(token + "_MODIFY"))
                {
                    tokensToModify.Remove(token);
                    string modificatedString = Language.GetString(token);
                    Debug.Log("Created string: " +  modificatedString);
                    foreach (var key in tokenModifications)
                    {
                        i++;
                        if (modificatedString.Contains(key.Key))
                        {
                            Debug.Log("Turning " + key.Key + " into " + key.Value);
                            modificatedString = modificatedString.Replace(key.Key, key.Value);
                        }
                    }
                    Debug.Log("Final result: " + modificatedString);
                    self.stringsByToken.Add(token + "_MODIFY", modificatedString);
                    tokensToModify.Add(token);
                }
                else
                {
                    tokensToModify.Remove(token);
                    string modificatedString = Language.GetString(token);
                    Debug.Log("Created string: " + modificatedString);
                    foreach (var key in tokenModifications)
                    {
                        i++;
                        if (modificatedString.Contains(key.Key))
                        {
                            Debug.Log("Turning " + key.Key + " into " + key.Value);
                            modificatedString = modificatedString.Replace(key.Key, key.Value);
                        }
                    }
                    Debug.Log("Final result: " + modificatedString);
                    self.stringsByToken[token + "_MODIFY"] = modificatedString;
                    tokensToModify.Add(token);
                }
                if (i > 0)
                {
                    token += "_MODIFY";
                    Debug.LogWarning("returning token: " + token);
                }
                    
            }
            return orig(self, token);
        }

        private void CreateConfigs()
        {
        }
       

        private void BodyCatalog_SetBodyPrefabs(On.RoR2.BodyCatalog.orig_SetBodyPrefabs orig, GameObject[] newBodyPrefabs)
        {
            orig(newBodyPrefabs);
            DemoBodyIndex = BodyCatalog.FindBodyIndex("DemolisherBody");
        }

        private void CreateINetMessages()
        {
            NetworkingAPI.RegisterMessageType<HookNetMessage>();
            NetworkingAPI.RegisterMessageType<AddBuffNetMessage>();
            NetworkingAPI.RegisterMessageType<UngroundNetMessage>();
            NetworkingAPI.RegisterMessageType<SwordEffectNetMessage>();
            NetworkingAPI.RegisterMessageType<AddBodyEffectNetMessage>();
            NetworkingAPI.RegisterMessageType<RemoveBodyEffectNetMessage>();
            NetworkingAPI.RegisterMessageType<ModifyVectorOfRigidBodyNetMessage>();
            NetworkingAPI.RegisterMessageType<ModifyFloatOfCharacterMotortNetMessage>();
            NetworkingAPI.RegisterMessageType<ModifyVectorOfCharacterMotorNetMessage>();
            NetworkingAPI.RegisterMessageType<ModifyBooleanOfCharacterMotorNetMessage>();
            NetworkingAPI.RegisterMessageType<RemoveComponentFromNetworkObjectNetMessage>();
        }
        public class RemoveBodyEffectNetMessage : INetMessage
        {
            NetworkInstanceId instanceId;
            string effectName;
            string bone;
            public void Deserialize(NetworkReader reader)
            {
                instanceId = reader.ReadNetworkId();
                effectName = reader.ReadString();
                bone = reader.ReadString();
            }
            public RemoveBodyEffectNetMessage(NetworkInstanceId networkInstanceId, string effectName, string bone)
            {
                instanceId = networkInstanceId;
                this.effectName = effectName;
                this.bone = bone;
            }
            public RemoveBodyEffectNetMessage()
            {

            }
            public void OnReceived()
            {
                GameObject characterObject = Util.FindNetworkObject(instanceId);
                if (!characterObject) return;
                CharacterBody characterBody = characterObject.GetComponent<CharacterBody>();
                if(characterBody == null) return;
                Transform modelTransform = characterBody.modelLocator.modelTransform;
                ChildLocator childLocator = modelTransform ? modelTransform.GetComponent<ChildLocator>() : null;
                if(childLocator == null) return;
                if (bone != null && bone != "" && childLocator && childLocator.FindChild(bone))
                {
                    do
                    {
                        GameObject gameObject = childLocator.FindChild(bone).Find(effectName).gameObject;
                        gameObject.name = "gone";
                        StopAndDestroyVFX(gameObject, 1f);
                    } while (childLocator.FindChild(bone).Find(effectName));
                }
                else
                {
                    do
                    {
                        GameObject gameObject = modelTransform.Find(effectName).gameObject;
                        gameObject.name = "gone";
                        StopAndDestroyVFX(gameObject, 1f);
                    } while (modelTransform.Find(effectName));
                }
            }

            public void Serialize(NetworkWriter writer)
            {
                writer.Write(instanceId);
                writer.Write(effectName);
                writer.Write(bone);
            }
        }
        public class RemoveComponentFromNetworkObjectNetMessage : INetMessage
        {
            NetworkInstanceId instanceId;
            string componentName;
            public RemoveComponentFromNetworkObjectNetMessage(NetworkInstanceId networkInstanceId, string componentName)
            {
                instanceId = networkInstanceId;
                this.componentName = componentName;
            }
            public RemoveComponentFromNetworkObjectNetMessage()
            {
                
            }
            public void Deserialize(NetworkReader reader)
            {
                instanceId = reader.ReadNetworkId();
                componentName = reader.ReadString();
            }

            public void OnReceived()
            {
                GameObject gameObject = Util.FindNetworkObject(instanceId);
                if (!gameObject) return;
                Component component = gameObject.GetComponentByName(componentName);
                if(component == null) return;
                Destroy(component);
            }

            public void Serialize(NetworkWriter writer)
            {
                writer.Write(instanceId);
                writer.Write(componentName);
            }
        }
        public class ModifyVectorOfCharacterMotorNetMessage : INetMessage
        {
            NetworkInstanceId instanceId;
            string vectorField;
            Vector3 value;
            public ModifyVectorOfCharacterMotorNetMessage(NetworkInstanceId networkInstanceId, string vectorField, Vector3 value)
            {
                instanceId = networkInstanceId;
                this.vectorField = vectorField;
                this.value = value;
            }
            public ModifyVectorOfCharacterMotorNetMessage()
            {

            }
            public void Deserialize(NetworkReader reader)
            {
                instanceId = reader.ReadNetworkId();
                vectorField = reader.ReadString();
                value = reader.ReadVector3();
            }

            public void OnReceived()
            {
                GameObject gameObject = Util.FindNetworkObject(instanceId);
                if (gameObject == null) return;
                var component = gameObject.GetComponent<CharacterMotor>();
                if (component == null) return;
                component.SetFieldValue<Vector3>(vectorField, value);
            }

            public void Serialize(NetworkWriter writer)
            {
                writer.Write(instanceId);
                writer.Write(vectorField);
                writer.Write(value);

            }
        }
        public class ModifyVectorOfRigidBodyNetMessage : INetMessage
        {
            NetworkInstanceId instanceId;
            string vectorField;
            Vector3 value;
            public ModifyVectorOfRigidBodyNetMessage(NetworkInstanceId networkInstanceId, string vectorField, Vector3 value)
            {
                instanceId = networkInstanceId;
                this.vectorField = vectorField;
                this.value = value;
            }
            public ModifyVectorOfRigidBodyNetMessage()
            {

            }
            public void Deserialize(NetworkReader reader)
            {
                instanceId = reader.ReadNetworkId();
                vectorField = reader.ReadString();
                value = reader.ReadVector3();
            }

            public void OnReceived()
            {
                GameObject gameObject = Util.FindNetworkObject(instanceId);
                if (gameObject == null) return;
                var component = gameObject.GetComponent<Rigidbody>();
                if (component == null) return;
                component.SetFieldValue<Vector3>(vectorField, value);
            }

            public void Serialize(NetworkWriter writer)
            {
                writer.Write(instanceId);
                writer.Write(vectorField);
                writer.Write(value);

            }
        }
        public class ModifyFloatOfCharacterMotortNetMessage : INetMessage
        {
            NetworkInstanceId instanceId;
            string floatField;
            float value;
            public ModifyFloatOfCharacterMotortNetMessage(NetworkInstanceId networkInstanceId, string floatField, float value)
            {
                instanceId = networkInstanceId;
                this.floatField = floatField;
                this.value = value;
            }
            public ModifyFloatOfCharacterMotortNetMessage()
            {

            }
            public void Deserialize(NetworkReader reader)
            {
                instanceId = reader.ReadNetworkId();
                floatField = reader.ReadString();
                value = reader.ReadSingle();
            }

            public void OnReceived()
            {
                GameObject gameObject = Util.FindNetworkObject(instanceId);
                if (gameObject == null) return;
                var component = gameObject.GetComponent<CharacterMotor>();
                if (component == null) return;
                component.SetFieldValue<float>(floatField, value);
            }

            public void Serialize(NetworkWriter writer)
            {
                writer.Write(instanceId);
                writer.Write(floatField);
                writer.Write(value);

            }
        }
        public class UngroundNetMessage : INetMessage
        {
            NetworkInstanceId instanceId;
            public UngroundNetMessage(NetworkInstanceId networkInstanceId)
            {
                instanceId = networkInstanceId;
            }
            public UngroundNetMessage()
            {

            }
            public void Deserialize(NetworkReader reader)
            {
                instanceId = reader.ReadNetworkId();
            }

            public void OnReceived()
            {
                GameObject gameObject = Util.FindNetworkObject(instanceId);
                if (gameObject == null) return;
                var component = gameObject.GetComponent<KinematicCharacterMotor>();
                if (component == null) return;
                component.ForceUnground();
            }

            public void Serialize(NetworkWriter writer)
            {
                writer.Write(instanceId);
            }
        }
        public class ModifyBooleanOfCharacterMotorNetMessage : INetMessage
        {
            NetworkInstanceId instanceId;
            string boolean;
            bool value;
            public ModifyBooleanOfCharacterMotorNetMessage(NetworkInstanceId networkInstanceId, string boolean, bool value)
            {
                instanceId = networkInstanceId;
                this.boolean = boolean;
                this.value = value;
            }
            public ModifyBooleanOfCharacterMotorNetMessage()
            {

            }
            public void Deserialize(NetworkReader reader)
            {
                instanceId = reader.ReadNetworkId();
                boolean = reader.ReadString();
                value = reader.ReadBoolean();
            }

            public void OnReceived()
            {
                GameObject gameObject = Util.FindNetworkObject(instanceId);
                if(gameObject == null) return;
                var component = gameObject.GetComponent<CharacterMotor>();
                if(component == null) return;
                component.SetFieldValue<bool>(boolean, value);
            }

            public void Serialize(NetworkWriter writer)
            {
                writer .Write(instanceId);
                writer.Write(boolean);
                writer.Write(value);

            }
        }
        public class AddBodyEffectNetMessage : INetMessage
        {
            NetworkInstanceId instanceId;
            int effectId;
            string effectName;
            string bone;
            Vector3 position;
            bool isLocalSpace;
            Vector3 scale;
            public AddBodyEffectNetMessage(NetworkInstanceId networkInstanceId, int effectId, string effectName, string bone, Vector3 position, bool isLocalSpace, Vector3 scale)
            {
                instanceId = networkInstanceId;
                this.effectId = effectId;
                this.effectName = effectName;
                this.bone = bone;
                this.position = position;
                this.isLocalSpace = isLocalSpace;
                this.scale = scale;
            }
            public AddBodyEffectNetMessage()
            {

            }
            public void Deserialize(NetworkReader reader)
            {
                instanceId = reader.ReadNetworkId();
                effectId = reader.ReadInt32();
                effectName = reader.ReadString();
                bone = reader.ReadString();
                position = reader.ReadVector3();
                isLocalSpace = reader.ReadBoolean();
                scale = reader.ReadVector3();
            }

            public void OnReceived()
            {
                GameObject effectObject = idToEffect[effectId];
                GameObject characterObject = Util.FindNetworkObject(instanceId);
                if (!characterObject) return;
                CharacterBody characterBody = characterObject.GetComponent<CharacterBody>();
                if (characterBody == null) return;
                Transform modelTransform = characterBody.modelLocator.modelTransform;
                ChildLocator childLocator = modelTransform ? modelTransform.GetComponent<ChildLocator>() : null;
                if (childLocator == null) return;
                if (bone != null && bone != "" && childLocator && childLocator.FindChild(bone))
                {
                    SpawnEffect(effectObject, position, isLocalSpace, Quaternion.identity, scale, childLocator.FindChild(bone), effectName);
                }
                else
                {
                    SpawnEffect(effectObject, position, isLocalSpace, Quaternion.identity, scale, modelTransform, effectName);
                }

            }

            public void Serialize(NetworkWriter writer)
            {
                writer.Write(instanceId);
                writer.Write(effectId);
                writer.Write(effectName);
                writer.Write(bone);
                writer.Write(position);
                writer.Write(isLocalSpace);
                writer.Write(scale);
            }
        }
        public class AddBuffNetMessage : INetMessage
        {
            NetworkInstanceId instanceId;
            int buffIndex;
            int amount;
            float buffTime;
            public AddBuffNetMessage(NetworkInstanceId networkInstanceId, int buffIndex, int amount, float buffTime)
            {
                this.instanceId = networkInstanceId;
                this.buffIndex = buffIndex;
                this.amount = amount;
                this.buffTime = buffTime;
            }
            public AddBuffNetMessage(NetworkInstanceId networkInstanceId, BuffIndex buffIndex, int amount, float buffTime)
            {
                this.instanceId = networkInstanceId;
                this.buffIndex = (int)buffIndex;
                this.amount = amount;
                this.buffTime = buffTime;
            }
            public AddBuffNetMessage(NetworkInstanceId networkInstanceId, BuffDef buffDef, int amount, float buffTime)
            {
                this.instanceId = networkInstanceId;
                this.buffIndex = (int)buffDef.buffIndex;
                this.amount = amount;
                this.buffTime = buffTime;
            }
            public AddBuffNetMessage()
            {

            }
            public void Deserialize(NetworkReader reader)
            {
                instanceId = reader.ReadNetworkId();
                buffIndex = reader.ReadInt32();
                amount = reader.ReadInt32();
                buffTime = reader.ReadSingle();
            }

            public void OnReceived()
            {
                if (!NetworkServer.active) return;
                GameObject gameObject = Util.FindNetworkObject(instanceId);
                if (gameObject == null) return;
                CharacterBody characterBody = gameObject.GetComponent<CharacterBody>();
                if (characterBody == null) return;
                for (int i = 0; i < amount; i++)
                {
                    if (amount < 0)
                    {
                        characterBody.RemoveBuff((BuffIndex)buffIndex);
                    }
                    else
                    {
                        if (buffTime > 0)
                        {
                            characterBody.AddTimedBuff((BuffIndex)buffIndex, buffTime);
                        }
                        else
                        {
                            characterBody.AddBuff((BuffIndex)buffIndex);
                        }
                    }
                }

            }

            public void Serialize(NetworkWriter writer)
            {
                writer.Write(instanceId);
                writer.Write(buffIndex);
                writer.Write(amount);
                writer.Write(buffTime);
            }
        }
        private void MapZone_TryZoneStart(On.RoR2.MapZone.orig_TryZoneStart orig, MapZone self, Collider other)
        {
            CharacterBody component = other.GetComponent<CharacterBody>();
            if (component && component.HasBuff(AfterSlam) && other.transform.position.y > self.transform.position.y)
            {
                AboveMapZoneTracker aboveMapZoneTracker = other.gameObject.GetOrAddComponent<AboveMapZoneTracker>();
                aboveMapZoneTracker.characterBody = component;
                aboveMapZoneTracker.zone = self;
                aboveMapZoneTracker.timer = 1f;
                return;
            }
            
            orig(self, other);
        }
        public class AboveMapZoneTracker : MonoBehaviour
        {
            public CharacterBody characterBody;
            public MapZone zone;
            public float timer = 1f;
            public Collider collider;
            public float radius = 1f;
            public void Start()
            {
                if (!characterBody) characterBody = GetComponent<CharacterBody>();
                if (!collider) collider = GetComponent<Collider>();
                radius = collider.bounds.size.magnitude / 3;
            }
            public void FixedUpdate()
            {
                if (!characterBody.HasBuff(AfterSlam))
                {
                    Destroy(this); return;
                }
                if(timer >= 0)
                timer -= Time.fixedDeltaTime;
                bool save = false;
                
                RaycastHit[] raycastHits = Physics.RaycastAll(transform.position, new Vector3(0f, Physics.gravity.y, 0f).normalized, 9999f, LayerIndex.collideWithCharacterHullOnly.mask, QueryTriggerInteraction.Collide);
                if (raycastHits != null && raycastHits.Length > 0)
                {
                    
                    foreach (RaycastHit raycastHit in raycastHits)
                    {
                        MapZone zone2 = raycastHit.collider.GetComponent<MapZone>();
                        if (zone && zone == zone2)
                        {
                            save = true;
                            break;
                        }
                        
                    }
                    
                    
                }
                if (!save)
                {
                    Collider[] colliders = Physics.OverlapSphere(collider.transform.position, radius, LayerIndex.collideWithCharacterHullOnly.mask, QueryTriggerInteraction.Collide);
                    if (colliders != null && colliders.Length > 0)
                    {
                        foreach (Collider collider in colliders)
                        {
                            MapZone zone2 = collider.GetComponent<MapZone>();
                            if (zone && zone == zone2)
                            {
                                save = true;
                                break;
                            }
                        }
                    }
                }
                
                if (!save && timer < 0)
                {
                    characterBody.SetBuffCount(AfterSlam.buffIndex, 0);
                    if (zone)
                        zone.TryZoneStart(characterBody.GetComponent<Collider>());
                    Destroy(this);
                }
            }
        }

        public delegate void CodeAfterInstantiating(GameObject gameObject, ChildLocator childLocator, SkillLocator skillLocator, DemoComponent demoComponent);
        private void GenericCharacterMain_HandleMovements(On.EntityStates.GenericCharacterMain.orig_HandleMovements orig, GenericCharacterMain self)
        {
            if (self.characterBody && self.characterBody.HasBuff(DisableInputs)) return;
            orig(self);
        }

        private bool GenericCharacterMain_CanExecuteSkill(On.EntityStates.GenericCharacterMain.orig_CanExecuteSkill orig, GenericCharacterMain self, GenericSkill skillSlot)
        {
            if (self.characterBody && self.characterBody.HasBuff(DisableInputs)) return false;
            return orig(self, skillSlot);
        }
        private void GlobalEventManager_OnHitEnemy(On.RoR2.GlobalEventManager.orig_OnHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
        {
            orig(self, damageInfo, victim);
            CharacterBody characterBody = victim.GetComponent<CharacterBody>();
            if (characterBody && characterBody.HasBuff(AfterSlam))
            {
                List<CharacterBody> characterBodies = runDemoInstance.GetBuffBodies(AfterSlam);
                if (characterBodies != null && characterBodies.Count > 0)
                {
                    foreach (CharacterBody characterBody1 in characterBodies)
                    {
                        if (characterBody1 && characterBody1.healthComponent && characterBody1.healthComponent.alive)
                        {
                            if (characterBody1.HasBuff(DisableInputs))
                            {
                                characterBody1.AddTimedBuff(RoR2Content.Buffs.Cripple, 12f);
                            }
                            if (characterBody1.isPlayerControlled)
                            {
                                if (characterBody1.characterMotor)
                                {
                                    new ModifyVectorOfCharacterMotorNetMessage(characterBody1.netId, "velocity", new Vector3(characterBody1.characterMotor.velocity.x, 24f, characterBody1.characterMotor.velocity.z)).Send(NetworkDestination.Clients);
                                }
                                else if (characterBody1.rigidbody)
                                {
                                    new ModifyVectorOfRigidBodyNetMessage(characterBody1.netId, "velocity", new Vector3(characterBody1.rigidbody.velocity.x, 24f, characterBody1.rigidbody.velocity.z)).Send(NetworkDestination.Clients);
                                }
                            }
                            else
                            {
                                if (characterBody1.characterMotor)
                                {
                                    characterBody1.characterMotor.velocity.y = 24f;
                                }
                                else if (characterBody1.rigidbody)
                                {
                                    Vector3 vector = characterBody1.rigidbody.velocity;
                                    vector.y = 24f;
                                }
                            }
                            
                        }
                        
                    }

                }
                characterBody.RemoveBuff(AfterSlam);
            }
        }

        private void Run_OnDisable(On.RoR2.Run.orig_OnDisable orig, Run self)
        {
            orig(self);
            DemoRunComponent demoRunComponent = self.gameObject.GetComponent<DemoRunComponent>();
            if (demoRunComponent)
            {
                runDemoInstance = SingletonHelper.Unassign<DemoRunComponent>(runDemoInstance, demoRunComponent);
            }
        }

        private void Run_OnEnable(On.RoR2.Run.orig_OnEnable orig, Run self)
        {
            orig(self);
            DemoRunComponent demoRunComponent = self.gameObject.GetComponent<DemoRunComponent>();
            if (demoRunComponent)
            {
                runDemoInstance = SingletonHelper.Assign<DemoRunComponent>(runDemoInstance, demoRunComponent);
            }
        }

        private void Run_onRunStartGlobal(Run obj)
        {
            if (NetworkServer.active)
            {
                DemoRunComponent demoRunComponent = obj.gameObject.AddComponent<DemoRunComponent>();
                runDemoInstance = SingletonHelper.Assign<DemoRunComponent>(runDemoInstance, demoRunComponent);
            };
        }
        public static DemoRunComponent runDemoInstance { get; private set; }
        public class DemoRunComponent : MonoBehaviour
        {
            public Dictionary<BuffDef, List<CharacterBody>> charactersWithBuffs = new Dictionary<BuffDef, List<CharacterBody>>();
            public void AddBuffBody(BuffDef buffDef, CharacterBody characterBody)
            {
                if (charactersWithBuffs.ContainsKey(buffDef))
                {
                    charactersWithBuffs[buffDef].Add(characterBody);
                }
                else
                {
                    charactersWithBuffs.Add(buffDef, new List<CharacterBody>());
                    charactersWithBuffs[buffDef].Add(characterBody);
                }
            }
            public void RemoveBuffBody(BuffDef buffDef, CharacterBody characterBody)
            {
                if (charactersWithBuffs.ContainsKey(buffDef))
                {
                    charactersWithBuffs[buffDef].Remove(characterBody);
                }
                if (charactersWithBuffs[buffDef].Count <= 0)
                {
                    charactersWithBuffs.Remove(buffDef);
                }
            }
            public List<CharacterBody> GetBuffBodies(BuffDef buffDef)
            {
                if (charactersWithBuffs.ContainsKey(buffDef))
                {
                    return charactersWithBuffs[buffDef];
                }
                return null;
            }
        }
        private void Row_AddButton(On.RoR2.UI.LoadoutPanelController.Row.orig_AddButton orig, object self, LoadoutPanelController owner, Sprite icon, string titleToken, string bodyToken, Color tooltipColor, UnityEngine.Events.UnityAction callback, string unlockableName, ViewablesCatalog.Node viewableNode, bool isWIP, int defIndex)
        {
            if (tokenReplace.ContainsKey(bodyToken))
            {
                bodyToken = tokenReplace[bodyToken];
            }
            //if (bodyToken == DetonateSkillDef.skillDescriptionToken)                         //!!!
            //{
            //    Destroy((self as LoadoutPanelController.Row).rowPanelTransform.gameObject);
            //    return;
            //}
            orig(self, owner, icon, titleToken, bodyToken, tooltipColor, callback, unlockableName, viewableNode, isWIP, defIndex);
        }

        private void CharacterMotor_OnMovementHit(On.RoR2.CharacterMotor.orig_OnMovementHit orig, CharacterMotor self, Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
            orig(self, hitCollider, hitNormal, hitPoint, ref hitStabilityReport);
            if (self.body.HasBuff(VelocityPreserve))
            {
                CharacterBody characterBody = self.body;
                /*BlastAttack landingExplosion = new BlastAttack
                {
                    attacker = self.gameObject,
                    attackerFiltering = AttackerFiltering.Default,
                    baseDamage = characterBody.damage * 3f * (1 + self.lastVelocity.magnitude),
                    baseForce = 3f,
                    bonusForce = Vector3.up,
                    canRejectForce = true,
                    crit = false,
                    damageColorIndex = DamageColorIndex.Default,
                    teamIndex = characterBody.teamComponent.teamIndex,
                    damageType = DamageTypeCombo.Generic,
                    falloffModel = BlastAttack.FalloffModel.SweetSpot,
                    impactEffect = default,
                    radius = 1 + self.lastVelocity.magnitude,
                    position = hitPoint + hitNormal * 0.01f,
                    inflictor = self.gameObject,
                    losType = BlastAttack.LoSType.None,
                    procChainMask = default,
                    procCoefficient = 0f,
                };
                BlastAttack.Result result = landingExplosion.Fire();
                EffectData effectData = new EffectData
                {
                    scale = landingExplosion.radius,
                    rotation = Quaternion.identity,
                    origin = hitPoint

                };
                EffectManager.SpawnEffect(explosionVFX, effectData, true);*/
                self.body.SetBuffCount(VelocityPreserve.buffIndex, 0);
            }

        }

        private bool GlobalEventManager_IsImmuneToFallDamage(On.RoR2.GlobalEventManager.orig_IsImmuneToFallDamage orig, GlobalEventManager self, CharacterBody body)
        {
            if (body.skillLocator && body.skillLocator.FindSkillByDef(ManthreadsSkillDef)) return true;
            return orig(self, body);
        }

        private void Main_skillsToStatsEvent(CharacterBody arg1, RecalculateStatsAPI.StatHookEventArgs arg2, GenericSkill arg3)
        {
            DemoComponent demoComponent = arg1.GetComponent<DemoComponent>();
            SkillDef skillDef = arg3.skillDef;
            if (skillDef != null)
            {
                if (skillDef == HeavyShieldSkillDef)
                {
                    arg2.armorAdd += 15f;
                }
                if (skillDef == SkullcutterSkillDef)
                {

                    if (demoComponent && demoComponent.isSwapped)
                    {
                    }
                    else
                    {
                        arg2.baseMoveSpeedAdd -= 2f;
                    }
                }
                if (skillDef == EyelanderSkillDef)
                {
                    arg2.healthMultAdd -= 0.25f;
                    arg2.baseMoveSpeedAdd -= 1f;
                    arg2.damageMultAdd -= 0.25f;
                }
            }
            EntityState entityState = arg3.stateMachine ? arg3.stateMachine.state : null;
            if (entityState != null)
            {
                if (entityState is ShieldCharge)
                {
                    ShieldCharge shieldCharge = (ShieldCharge)entityState;
                    arg2.armorAdd += shieldCharge.armor;
                }
            }

        }
        public event Action<CharacterBody, RecalculateStatsAPI.StatHookEventArgs, GenericSkill> skillsToStatsEvent;
        private void RecalculateStatsAPI_GetStatCoefficients(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
        {
            if (sender.skillLocator)
            {
                foreach (GenericSkill genericSkill in sender.skillLocator.allSkills)
                {
                    skillsToStatsEvent(sender, args, genericSkill);
                }
            }
        }
        public static void StopAndDestroyVFX(Transform transform, float timeToDestroy)
        {
            StopAndDestroyVFX(transform.gameObject, timeToDestroy);
        }
        public static void StopAndDestroyVFX(GameObject gameObject, float timeToDestroy)
        {
            gameObject.name = "gone";
            ParticleSystem[] particleSystems = gameObject.GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem particleSystem in particleSystems)
            {
                particleSystem.enableEmission = false;
            }
            Destroy(gameObject, timeToDestroy);
        }
        public static void AddEffect(GameObject gameObject)
        {
            int id = idToEffect.Count;
            idToEffect.Add(id, gameObject);
            effectToId.Add(gameObject, id);
        }
        private void CharacterBody_OnBuffFinalStackLost(On.RoR2.CharacterBody.orig_OnBuffFinalStackLost orig, CharacterBody self, BuffDef buffDef)
        {
            orig(self, buffDef);
            if (!NetworkServer.active) return;
            if (buffsToTrack.Contains(buffDef))
            {
                runDemoInstance.RemoveBuffBody(buffDef, self);
            }
            if (buffDef == AfterSlam)
            {
                self.SetBuffCount(DisableInputs.buffIndex, 0);
                new RemoveBodyEffectNetMessage(self.netId, "SMAAASH", "").Send(NetworkDestination.Clients);
                //new RemoveComponentFromNetworkObjectNetMessage(self.netId, "AboveMapZoneTracker").Send(NetworkDestination.Clients);
            }
            if (buffDef == VelocityPreserve)
            {
                new RemoveBodyEffectNetMessage(self.netId, "DemoSmokeFeet", "FootR").Send(NetworkDestination.Clients);
                new RemoveBodyEffectNetMessage(self.netId, "DemoSmokeFeet", "FootL").Send(NetworkDestination.Clients);
                new ModifyBooleanOfCharacterMotorNetMessage(self.netId, "disableAirControlUntilCollision", false).Send(NetworkDestination.Clients);
            }
        }
        private void CharacterBody_OnBuffFirstStackGained(On.RoR2.CharacterBody.orig_OnBuffFirstStackGained orig, CharacterBody self, BuffDef buffDef)
        {
            orig(self, buffDef);
            if (!NetworkServer.active) return;
            if (buffsToTrack.Contains(buffDef))
            {
                runDemoInstance.AddBuffBody(buffDef, self);
            }
            if (buffDef == AfterSlam)
            {
                new AddBodyEffectNetMessage(self.netId, effectToId[SlamableEffect], "SMAAASH", "", self.corePosition, false, OneVector(self.radius * 0.7f)).Send(NetworkDestination.Clients);
            }
            if (buffDef == VelocityPreserve)
            {
                new AddBodyEffectNetMessage(self.netId, effectToId[SmokeEffect], "DemoSmokeFeet", "FootR", Vector3.zero, true, OneVector(1f)).Send(NetworkDestination.Clients);
                new AddBodyEffectNetMessage(self.netId, effectToId[SmokeEffect], "DemoSmokeFeet", "FootL", Vector3.zero, true, OneVector(1f)).Send(NetworkDestination.Clients);
            }
        }

        private void CharacterMotor_OnLanded(On.RoR2.CharacterMotor.orig_OnLanded orig, CharacterMotor self)
        {
            orig(self);
            if (self.body.HasBuff(AfterSlam))
            {
                if (self.body.GetBuffCount(AfterSlam) > 1)
                {
                    self.body.RemoveOldestTimedBuff(AfterSlam.buffIndex);
                }
                self.body.SetBuffCount(AfterSlam.buffIndex, 0);
            }

        }
        private void CharacterMotor_FixedUpdate(On.RoR2.CharacterMotor.orig_FixedUpdate orig, CharacterMotor self)
        {
            orig(self);
            if (self.body.HasBuff(AfterSlam))
            {
                self.velocity = new Vector3(0f, self.velocity.y, 0f);
                self.rootMotion += self.moveDirection * self.walkSpeed * Time.fixedDeltaTime;
            }
            if (self.body.HasBuff(VelocityPreserve))
            {
                self.disableAirControlUntilCollision = true;
                InputBankTest inputBankTest = self.body.inputBank;
                if (inputBankTest)
                {
                    var currentVelocity = new Vector3(self.velocity.x, 0, self.velocity.z);
                    var wishDirection = new Vector3(inputBankTest.moveVector.x, 0, inputBankTest.moveVector.z).normalized;
                    var dotProduct = Vector3.Dot(currentVelocity.normalized, wishDirection);
                    //wishDirection = wishDirection * self.walkSpeed;
                    if (dotProduct < 0.10)
                        self.velocity += new Vector3(wishDirection.x * self.walkSpeed * 10, 0, wishDirection.z * self.walkSpeed * 10) * Time.fixedDeltaTime;
                    //self.velocity += inputBankTest.moveVector * self.walkSpeed * Time.fixedDeltaTime;
                }

            }
        }

        private void HealthComponent_TakeDamageProcess(On.RoR2.HealthComponent.orig_TakeDamageProcess orig, HealthComponent self, DamageInfo damageInfo)
        {
            CharacterBody characterBody = damageInfo.attacker ? damageInfo.attacker.GetComponent<CharacterBody>() : null;
            if (characterBody)
            {
                if (characterBody.HasBuff(ExtraSwordDamage) && damageInfo.damageType.damageSource == DamageSource.Primary) damageInfo.damage *= 1 + (characterBody.GetBuffCount(ExtraSwordDamage) / 100);
            }
            
            
            orig(self, damageInfo);
        }

        private void CharacterBody_OnKilledOtherServer(On.RoR2.CharacterBody.orig_OnKilledOtherServer orig, CharacterBody self, DamageReport damageReport)
        {
            CharacterBody victimBody = damageReport.victimBody;
            Vector3 victimPosition = victimBody.mainHurtBox.transform.position;
            bool validForUpgrade = victimBody && (victimBody.isChampion || victimBody.isBoss) ? victimBody.HasBuff(UpgradeOnKill) : false;
            bool victimZatoichi = victimBody ? victimBody.HasBuff(HealOnKill) : false;
            bool attackerZatoichi = self ? self.HasBuff(HealOnKill) : false;
            orig(self, damageReport);
            if (victimBody && validForUpgrade)
            {
                DeathRewards deathRewards = victimBody.GetComponent<DeathRewards>();
                PickupIndex pickupIndex = PickupIndex.none;
                if (deathRewards)
                {
                    if (deathRewards.bossDropTable)
                    {
                        pickupIndex = deathRewards.bossDropTable.GenerateDrop(Run.instance.bossRewardRng);
                    }
                    else
                    {
                        pickupIndex = PickupCatalog.itemIndexToPickupIndex[((int)RoR2Content.Items.ShinyPearl.itemIndex)];
                    }

                }
                else
                {
                    pickupIndex = PickupCatalog.itemIndexToPickupIndex[((int)RoR2Content.Items.ShinyPearl.itemIndex)];
                }

                PickupDropletController.CreatePickupDroplet(pickupIndex, victimPosition, Physics.gravity * -1f);
            }
            if (victimZatoichi && attackerZatoichi)
            {
                HealthComponent healthComponent = self.healthComponent;
                Overheal(healthComponent, 0.15f);
            }

        }
        public static List<SkillDef> StickySkills = new List<SkillDef>();
        public static BuffDef ExtraCritChance;
        public static BuffDef HealOnKill;
        public static BuffDef ExtraSwordDamage;
        public static BuffDef VelocityPreserve;
        public static BuffDef UpgradeOnKill;
        public static BuffDef AfterSlam;
        public static BuffDef DisableInputs;
        public static BuffDef SkullcutterDamageIncrease;
        public static GameObject PillProjectile;
        public static GameObject RocketProjectile;
        public static GameObject BombProjectile;
        public static GameObject StickyProjectile;
        public static GameObject QuickiebombProjectile;
        public static GameObject MineProjectile;
        public static GameObject AntigravProjectile;
        public static GameObject NukeProjectile;
        public static GameObject JumperProjectile;
        public static GameObject HookProjectile;

        public static GameObject DemoBody;
        public static GameObject SmokeEffect;
        public static GameObject SwingEffect;
        public static GameObject SpecialSpinner;
        public static GameObject SpinEffect;
        public static GameObject ArmedEffect;
        public static GameObject FullyArmedEffect;
        public static GameObject SwordTrail;
        public static GameObject Tracer;
        public static GameObject SlamEffect;
        public static GameObject SlamableEffect;
        public static GameObject HitEffect;
        public static Sprite StickyIndicator;
        public static Sprite SwordIndicator;
        public static Sprite ShieldIndicator;
        public static Sprite DetonateIndicator;
        public static ModelPart DemoSmokes;
        public static GameObject GmodHud;
        public static GameObject GmodPropButton;
        public static GameObject GmodPropPlane;
        public static GameObject GmodPropThuster;
        public static BuffDef AddBuff(string name, bool canStack, bool isDebuff, bool isCooldown, bool isHidden, bool ignoreGrowthNectar, Sprite sprite = null)
        {
            BuffDef buff = ScriptableObject.CreateInstance<BuffDef>();
            buff.name = "Demo" + name;
            buff.buffColor = Color.white;
            buff.canStack = canStack;
            buff.isDebuff = isDebuff;
            buff.ignoreGrowthNectar = ignoreGrowthNectar;
            buff.iconSprite = sprite;
            buff.isHidden = isHidden;
            buff.isCooldown = isCooldown;
            buffs.Add(buff);
            return buff;
        }
        public static ItemDef AddItem(string name, ItemTier tier, Sprite sprite, GameObject prefab, bool canRemove, bool hidden, ItemTag[] tags, ItemDisplayRuleDict itemDisplayRuleDict)
        {
            ItemDef item = ScriptableObject.CreateInstance<ItemDef>();
            item.name = "DEMO_" + name.Replace(" ", "");
            item.nameToken = "DEMO_" + name.ToUpper().Replace(" ", "") + "_NAME";
            item.pickupToken = "DEMO_" + name.ToUpper().Replace(" ", "") + "_PICKUP";
            item.descriptionToken = "DEMO_" + name.ToUpper().Replace(" ", "") + "_DESC";
            item.loreToken = "DEMO_" + name.ToUpper().Replace(" ", "") + "_LORE";
            item.deprecatedTier = tier;
            item.pickupIconSprite = sprite;
            item.pickupModelPrefab = prefab;
            item.canRemove = canRemove;
            item.hidden = hidden;
            item.tags = tags;
            ItemAPI.Add(new CustomItem(item, itemDisplayRuleDict));
            return item;
        }
        private static void CreateGmodAssets()
        {
            GmodHud = ThunderkitAssets.LoadAsset<GameObject>("Assets/Gmod/GmodPropsHud.prefab");
            GmodPropButton = ThunderkitAssets.LoadAsset<GameObject>("Assets/Gmod/GmodProp.prefab");
            GmodPropButton.AddComponent<GmodPropUIButton>();
            GmodPropPlane = ThunderkitAssets.LoadAsset<GameObject>("Assets/Gmod/1x1Plane.prefab");
            GmodPropThuster = ThunderkitAssets.LoadAsset<GameObject>("Assets/Gmod/Thuster.prefab");
            GmodPropThuster.AddComponent<GmodPropThusterComponent>();
            GmodPropsCatalog.AddProp(new GmodPropDef { gameObject = GmodPropPlane, icon = ThunderkitAssets.LoadAsset<Sprite>("Assets/Gmod/1x1plane.png"), placeObjectDelegate = PlanePlace });
            GmodPropsCatalog.AddProp(new GmodPropDef { gameObject = GmodPropThuster, icon = ThunderkitAssets.LoadAsset<Sprite>("Assets/Gmod/thruster.png"), placeObjectDelegate = ThusterPlace });
        }

        private static void PlanePlace(GameObject gameObject, Collider hitCollider, RaycastHit raycastHit, GameObject owner)
        {
            gameObject.transform.position += new Vector3(0f, 2f, 0f);
        }
        private static void ThusterPlace(GameObject gameObject, Collider hitCollider, RaycastHit raycastHit, GameObject owner)
        {
            gameObject.transform.parent = hitCollider.transform;
            gameObject.transform.position += raycastHit.normal;
            gameObject.transform.rotation = Quaternion.LookRotation(raycastHit.normal * -1);
            gameObject.GetComponent<GmodPropThusterComponent>().inputBank = owner.GetComponent<InputBankTest>();
        }
        public const string StickyLauncherName = "Sticky Launcher";
        public static NetworkConfig<float> StickyLauncherDamage;
        public static NetworkConfig<float> StickyLauncherFireRate;
        public static NetworkConfig<float> StickyLauncherChargeTime;
        public const string JumperLauncherName = "Jumper Launcher";
        public const string PillLauncherName = "Pill Launcher";
        public const string RocketLauncherName = "Rocket Launcher";
        public const string HookLauncherName = "Hook Launcher";
        public const string NukeLauncherName = "Nuke Launcher";
        public const string BombLauncherName = "Bomb Launcher";
        public const string AntigravLauncherName = "Antigrav Launcher";
        public const string MineLayerName = "Mine Layer";
        public const string SkullcutterName = "Skullcutter";
        public const string ZatoichiName = "Zatoichi";
        public const string CaberName = "Caber";
        public const string EyelanderName = "Eyelander";
        public const string HeavyShieldName = "Heavy Shield";
        public const string LightShieldName = "Light Shield";
        public const string WhirlwindName = "Whirlwind";
        public const string HitstormName = "Hit storm";
        public const string SlamName = "Slam";
        public const string HeavySmashName = "Heavy Smash";
        public const string DamageName = "Damage";
        public const string FireRateName = "Fire Rate";
        public const string StocksName = "Base Max Stocks";
        public const string ReloadTimeName = "Reload Time";
        public const string StocksToReloadName = "Stocks to Reload";
        public const string StocksToConsume = "Stocks to Consume";
        public const string BlastRadiusName = "Blast Radius";
        private void CreateAssets()
        {
            Main.ExtraCritChance = Main.AddBuff("ExtraCritChance", true, false, false, false, false, ThunderkitAssets.LoadAsset<Sprite>("Assets/Demoman/Buffs/DemoSkullcutterBuff.png"));//, RoR2Content.Buffs.FullCrit.iconSprite);
            Main.HealOnKill = Main.AddBuff("HealOnKill", false, false, false, true, false);//, RoR2Content.Buffs.HealingDisabled.iconSprite);
            //Main.ExtraSwordDamage = Main.AddBuff("ExtraSwordDamagePrepare", true, false, false, true, false);
            Main.ExtraSwordDamage = Main.AddBuff("ExtraSwordDamage", true, false, false, true, false);
            VelocityPreserve = AddBuff("PreserveVelocity", false, false, false, true, true);
            UpgradeOnKill = AddBuff("UpgradeOnKill", false, false, false, true, false);
            AfterSlam = AddBuff("SlamClutch", false, false, false, true, false);
            DisableInputs = AddBuff("DisableAI", false, false, false, true, false);
            SkullcutterDamageIncrease = AddBuff("SkullcutterDamageIncrease", false, false, false, true, false);
            buffsToTrack.Add(AfterSlam);
            SmokeEffect = ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/SmokingEffect.prefab");
            SmokeEffect.AddComponent<DontRotate>();
            AddEffect(SmokeEffect);
            SwordIndicator = ThunderkitAssets.LoadAsset<Sprite>("Assets/Demoman/UI/DemoSwordIndicatorThinHalf.png");
            StickyIndicator = ThunderkitAssets.LoadAsset<Sprite>("Assets/Demoman/UI/DemoStickyIndicatorThinHalf.png");
            DetonateIndicator = ThunderkitAssets.LoadAsset<Sprite>("Assets/Demoman/UI/DemoDetonateIndicatorThin.png");
            ShieldIndicator = ThunderkitAssets.LoadAsset<Sprite>("Assets/Demoman/UI/DemoShieldIndicatorThin.png");
            SwingEffect = ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/SwortSwing.prefab");
            SpecialSpinner = ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/SpecialSwing.prefab");
            //SwordSpecialSpinner.AddComponent<SpecialSwordSpiner>();
            ArmedEffect = ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/StickyArmedVFX.prefab");
            FullyArmedEffect = ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/StickyFullyArmedVFX.prefab");
            SwordTrail = ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/SwordTrail.prefab");
            Tracer = ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/DemoTracer.prefab");
            SpinEffect = ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/SpecialSwingNoTrigger.prefab");
            SlamableEffect = ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/SlammableVFX.prefab");
            AddEffect(SlamableEffect);
            SlamableEffect.AddComponent<DontRotate>();
            SlamEffect = ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/SlamVFX.prefab");
            AddEffect(SlamEffect);
            HitEffect = ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/HitVFX.prefab");
            //UpgradeItem = AddItem("DemoStompItem", ItemTier.NoTier, null, null, false, true, new ItemTag[] {ItemTag.WorldUnique}, null);
            List<GameObject> projectiles = new List<GameObject>();
            Main.PillProjectile = ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/Projectiles/Pill/PillProjectile.prefab");
            projectiles.Add(Main.PillProjectile);
            objectsActualNames.Add(PillProjectile.name, PillLauncherName);
            RocketProjectile = ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/Projectiles/Rocket/RocketProjectile.prefab");
            //RocketProjectile.AddComponent<RotateToVelocity>();
            projectiles.Add(Main.RocketProjectile);
            objectsActualNames.Add(RocketProjectile.name, RocketLauncherName);
            HookProjectile = ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/Projectiles/Hook/HookProjectile.prefab");
            //HookProjectile.AddComponent<RotateToVelocity>();
            var hookComponent = HookProjectile.AddComponent<HookComponent>();
            //hookComponent.seekerState = (typeof(HookLauncher));
            projectiles.Add(Main.HookProjectile);
            objectsActualNames.Add(HookProjectile.name, HookLauncherName);
            NukeProjectile = ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/Projectiles/Nuke/NukeProjectile.prefab");
            //NukeProjectile.AddComponent<RotateToVelocity>();
            //NukeProjectile.AddComponent<NukeComponent>();
            projectiles.Add(Main.NukeProjectile);
            objectsActualNames.Add(NukeProjectile.name, NukeLauncherName);
            BombProjectile = ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/Projectiles/Bomb/BombProjectile.prefab");
            projectiles.Add(Main.BombProjectile);
            BombProjectile.AddComponent<BombComponent>();
            objectsActualNames.Add(BombProjectile.name, BombLauncherName);
            Main.StickyProjectile = ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/Projectiles/Stickybomb/StickybombProjectile.prefab");
            StickyProjectile.AddComponent<DefaultSticky>();
            projectiles.Add(Main.StickyProjectile);
            objectsActualNames.Add(StickyProjectile.name, StickyLauncherName);
            Main.JumperProjectile = ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/Projectiles/JumperStickybomb 1/JumperStickybombProjectile.prefab");
            JumperProjectile.AddComponent<DefaultSticky>();
            var jumperComponent = JumperProjectile.AddComponent<DemoExplosionComponent>();
            jumperComponent.enemyPower = 7f;
            jumperComponent.selfPower = 10f;
            projectiles.Add(Main.JumperProjectile);
            objectsActualNames.Add(JumperProjectile.name, JumperLauncherName);
            Main.AntigravProjectile = ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/Projectiles/AntigravityStickyBomb/AntigravityStickyBombProjectile.prefab");
            projectiles.Add(Main.AntigravProjectile);
            AntigravProjectile.AddComponent<AntigravSticky>();
            objectsActualNames.Add(AntigravProjectile.name, AntigravLauncherName);
            Main.MineProjectile = ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/Projectiles/Mine/MineProjectile.prefab");
            projectiles.Add(Main.MineProjectile);
            MineProjectile.AddComponent<Mine>();
            MineProjectile.transform.GetChild(0).gameObject.AddComponent<MineDetector>();
            objectsActualNames.Add(MineProjectile.name, MineLayerName);
            GameObject explosionVFX = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Toolbot/OmniExplosionVFXToolbotQuick.prefab").WaitForCompletion();
            foreach (GameObject projectile in projectiles)
            {
                NetworkIdentity networkIdentity = projectile.GetComponent<NetworkIdentity>();
                if (networkIdentity != null)
                {
                    networkIdentity.localPlayerAuthority = true;
                    networkIdentity.serverOnly = false;
                }
                ProjectileImpactExplosion projectileImpactExplosion = projectile.GetComponent<ProjectileImpactExplosion>();
                if (projectileImpactExplosion)
                {
                    if (projectileImpactExplosion.impactEffect == null)
                    {
                        projectileImpactExplosion.impactEffect = explosionVFX;
                    }
                    
                    ProjectileSimple projectileSimple = projectile.GetComponent<ProjectileSimple>();
                    if (projectileSimple)
                    {
                        projectileSimple.lifetime = float.PositiveInfinity;
                    }
                    projectile.AddComponent<HitHelper>();
                    SphereCollider sphereCollider = projectile.AddComponent<SphereCollider>();
                    sphereCollider.isTrigger = true;
                    sphereCollider.radius = 0.5f;
                }
                DemoExplosionComponent rocketjumpComponent = projectile.GetComponent<DemoExplosionComponent>();
                if (!rocketjumpComponent && projectileImpactExplosion)
                {
                    rocketjumpComponent = projectile.AddComponent<DemoExplosionComponent>();
                }
                if (rocketjumpComponent)
                {
                    string key = GenerateReplacement(objectsActualNames[projectile.name], BlastRadiusName);
                    NetworkConfig<float> blastRadiusConfig = CreateConfig<float>(ConfigFile, objectsActualNames[projectile.name], BlastRadiusName, projectileImpactExplosion.blastRadius, "");
                    blastRadiusConfig.OnConfigApplied = UpdateConfig;
                    rocketjumpComponent.blastRadiusConfigId = blastRadiusConfig.id;
                    
                    if (tokenModifications.ContainsKey(key))
                    {
                        tokenModifications[key] = blastRadiusConfig.Value.ToString();
                    }
                    else
                    {
                        tokenModifications.Add(key, blastRadiusConfig.Value.ToString());
                    }
                    void UpdateConfig(int id, INetworkConfig networkConfig)
                    {
                        Debug.Log("Updating key: " + key);
                        Debug.Log("To value: " + blastRadiusConfig.Value);
                        if (tokenModifications.ContainsKey(key))
                        {
                            tokenModifications[key] = blastRadiusConfig.Value.ToString();
                        }
                        else
                        {
                            tokenModifications.Add(key, blastRadiusConfig.Value.ToString());
                        }
                    }
                }
                
                ProjectileController projectileController = projectile.GetComponent<ProjectileController>();
                projectileController.allowPrediction = false;
                ContentPacks.networkPrefabs.Add(projectile);
                //PrefabAPI.RegisterNetworkPrefab(projectile);
                ContentPacks.projectiles.Add(projectile);
                //ContentAddition.AddProjectile(projectile);
            }
        }
        private static void CreateSurvivor()
        {
            DemoBody = Main.ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/DemolisherBody.prefab");
            DemoDefaultSkin = Main.ThunderkitAssets.LoadAsset<SkinDef>("Assets/Demoman/DemoDefaultSkin.asset");
            DemoNuclearSkin = Main.ThunderkitAssets.LoadAsset<SkinDef>("Assets/Demoman/DemoNuclearSkin.asset");
            LanguageAPI.Add("DEMOLISHER_BODY_NAME", "Demolisher");
            LanguageAPI.Add("DEMO_SKIN_DEFAULT", "Default");
            LanguageAPI.Add("DEMOLISHER_BODY_SUBTITLE", "Reborn Demon");
            LanguageAPI.Add("DEMOLISHER_NAME", "Demolisher");
            LanguageAPI.Add("DEMOLISHER_DESC", "Demolisher is a powerfull character that can switch between melee and ranged styles at any moment. To switch styles, hold Utility button and click Secondary/Special button.\n" +
                "\n" +
                "<style=cSub>\r\n\r\n< ! > Passive allows Demolisher for harmless landing. His explosives have knockback that can be used as a quick position relocation. Holding jump button will redirect knockback force to face your aim direction." +
                "\r\n\r\n< ! > Swords have a small radius of attack, so you mast aim at the target you want to hit. They compensate it with their high burst damage and strong effects. Swords recharge 25% of Utility charge on hit." +
                "\r\n\r\n< ! > In ranged style swords are replaced with trap bomb launchers. On impact they stick to the surface and wait for user input for detonation signal. After a while they fully arm, gaining additional damage and blast radius." +
                "\r\n\r\n< ! > Grenades are available for both styles and are a quick way of dealing with targets on distance." +
                "\r\n\r\n< ! > Shield charge is a great movement skill while also having attack capabilities of increasing sword damage. Shield charge is cancelled upon sword attack, but retains damage increase for a short time." +
                "\r\n\r\n< ! > In ranged style shield charge is replaced with traps detonation. Traps require time to be armed for detonation and have time before explosion after detonation request." +
                "\r\n\r\n< ! > Specials can utilise primary selection and base their effects from them.");
            LanguageAPI.Add("DEMOLISHER_OUTRO_FLAVOR", "And so he left... enjoying every moment of it...");
            LanguageAPI.Add("DEMOLISHER_OUTRO_FAILURE", "If he was a good Demolisher... maybe he could escape...");
            CharacterBody demoCharacterBody = DemoBody.GetComponent<CharacterBody>();
            demoCharacterBody.preferredPodPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/SurvivorPod/SurvivorPod.prefab").WaitForCompletion();
            demoCharacterBody._defaultCrosshairPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/UI/StandardCrosshair.prefab").WaitForCompletion();
            GameObject gameObject = DemoBody.GetComponent<ModelLocator>().modelTransform.gameObject;
            gameObject.GetComponent<FootstepHandler>().footstepDustPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/VFX/GenericFootstepDust.prefab").WaitForCompletion();
            var component = DemoBody.AddComponent<DemoComponent>();
            var skillLocator = DemoBody.GetComponent<SkillLocator>();
            GameObject hudObject = ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/DemoExtraCrosshairRework.prefab");
            DemoBody.GetComponent<CharacterDeathBehavior>().deathState = new SerializableEntityStateType((typeof(DemoDeathState)));
            hudObject.transform.localScale = new Vector3(-1.4f, 1.4f, 1.4f);
            component.chargeMeter = hudObject;
            component.altCrosshair = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/UI/StandardCrosshair.prefab").WaitForCompletion();
            InteractionDriver interactionDriver = DemoBody.GetComponent<InteractionDriver>();
            EntityStateMachine entityStateMachine = DemoBody.GetComponent<EntityStateMachine>();
            entityStateMachine.mainStateType = new SerializableEntityStateType(typeof(DemoCharacterMain));
            ModelPartInfo modelPartInfo = new ModelPartInfo
            {
                bodyName = "DemolisherBody",
                gameObject = ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/DemoSmokes.prefab"),
                inputString = "Head",
                codeAfterApplying = CustomParticleSimulationSpace,
            };
            DemoSmokes = new ModelPart(modelPartInfo);
            void CustomParticleSimulationSpace(GameObject gameObject, ChildLocator childLocator, CharacterModel characterModel, ActivePartsComponent activePartsComponent)
            {
                EmoteCompatAbility.DemoEmotesComponent demoEmotesComponent = characterModel.GetComponentInChildren<EmoteCompatAbility.DemoEmotesComponent>();
                ParticleSystem[] particleSystems = gameObject.GetComponentsInChildren<ParticleSystem>();
                foreach (ParticleSystem particleSystem in particleSystems)
                {
                    ParticleSystem.MainModule mainModule = particleSystem.main;
                    particleSystem.simulationSpace = ParticleSystemSimulationSpace.Custom;
                    mainModule.customSimulationSpace = childLocator.FindChild("Base");
                    if (demoEmotesComponent != null)
                    {
                        demoEmotesComponent.customSpaceParticles.Add(mainModule);
                    }

                }

            }
            ModelPartInfo modelPartInfo2 = new ModelPartInfo
            {
                bodyName = "DemolisherBody",
                gameObject = ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/ChestDistorionSmokes.prefab"),
                codeAfterApplying = IncreaseSize,
                modelPartOverride = DemoSmokes,
                inputString = "LowerChest",
                skinDef = DemoNuclearSkin,
            };
            new ModelPart(modelPartInfo2);
            void IncreaseSize(GameObject gameObject, ChildLocator childLocator, CharacterModel characterModel, ActivePartsComponent activePartsComponent)
            {
                if (characterModel)
                {
                    //NuclearSizeIncreaseComponent nuclearSizeIncreaseComponent = characterModel.GetComponent<NuclearSizeIncreaseComponent>();
                    if (true)
                    {
                        characterModel.transform.localScale += OneVector(0.35f);
                        NuclearSizeIncreaseComponent nuclearSizeIncreaseComponent2 = characterModel.gameObject.AddComponent<NuclearSizeIncreaseComponent>();
                        activePartsComponent.components.Add(nuclearSizeIncreaseComponent2);
                    }
                }
                
                
            }
            bodies.Add(DemoBody);
            DemoSurvivorDef = Main.ThunderkitAssets.LoadAsset<SurvivorDef>("Assets/Demoman/DemoSurvivor.asset");
            survivors.Add(DemoSurvivorDef);
            if (emotesEnabled)
            {
                EmoteCompatAbility.EmoteCompatability();
            }
            
        }
        public class NuclearSizeIncreaseComponent : MonoBehaviour
        {
            public void OnDestroy()
            {
                transform.localScale -= OneVector(0.35f);
            }
        }
        public static DemoSoundClass DemoChargeHitFleshSound = new DemoSoundClass("DemoChargeFleshHit");
        public static DemoSoundClass DemoChargeWindUpSound = new DemoSoundClass("DemoChargeWindUp");
        public static DemoSoundClass DemoChargeWorldHitSound = new DemoSoundClass("DemoChargeWorldHit");
        public static DemoSoundClass DemoGrenadeImpactSound = new DemoSoundClass("DemoGrenadeImpact");
        public static DemoSoundClass DemoSwordHitWorldSound = new DemoSoundClass("DemoSwordHitWorld");
        public static DemoSoundClass DemoSwordSwingSound = new DemoSoundClass("DemoSwordSwing");
        public static DemoSoundClass DemoSwordSwingCritSound = new DemoSoundClass("demo_sword_swing_crit");
        public static DemoSoundClass DemoChargeHitFleshRange1Sound = new DemoSoundClass("demo_charge_hit_flesh_range1");
        public static DemoSoundClass DemoChargeHitFleshRange2Sound = new DemoSoundClass("demo_charge_hit_flesh_range2");
        public static DemoSoundClass DemoChargeHitFleshRange3Sound = new DemoSoundClass("demo_charge_hit_flesh_range3");
        public static DemoSoundClass DemoGrenadeReloadSound = new DemoSoundClass("grenade_launcher_worldreload");
        public static DemoSoundClass DemoGrenadeShootSound = new DemoSoundClass("grenade_launcher_shoot");
        public static DemoSoundClass DemoGrenadeShootCritSound = new DemoSoundClass("grenade_launcher_shoot_crit");
        public static DemoSoundClass DemoCannonImpactSound = new DemoSoundClass("loose_cannon_ball_impact");
        public static DemoSoundClass DemoCannonChargeSound = new DemoSoundClass("loose_cannon_charge");
        public static DemoSoundClass DemoCannonShootSound = new DemoSoundClass("loose_cannon_shoot");
        public static DemoSoundClass DemoCannonShootCritSound = new DemoSoundClass("loose_cannon_shootcrit");
        public static DemoSoundClass DemoStickyDetonationSound = new DemoSoundClass("stickybomblauncher_det");
        public static DemoSoundClass DemoStickyChargeSound = new DemoSoundClass("stickybomblauncher_charge_up");
        public static DemoSoundClass DemoStickyReloadSound = new DemoSoundClass("stickybomblauncher_worldreload");
        public static DemoSoundClass DemoTackyShootSound = new DemoSoundClass("tacky_grenadier_shoot");
        public static DemoSoundClass DemoTackyShootCritSound = new DemoSoundClass("tacky_grenadier_shoot_crit");
        private static void CreateSounds()
        {

        }
        public class DemoSoundClass
        {
            public DemoSoundClass(string soundName)
            {
                playSoundString = "Play_" + soundName;
                stopSoundString = "Stop_" + soundName;
                playSound = ScriptableObject.CreateInstance<NetworkSoundEventDef>();
                playSound.eventName = playSoundString;
                sounds.Add(playSound);
                stopSound = ScriptableObject.CreateInstance<NetworkSoundEventDef>();
                stopSound.eventName = stopSoundString;
                sounds.Add(stopSound);
            }
            public string playSoundString;
            public string stopSoundString;
            private NetworkSoundEventDef playSound;
            private NetworkSoundEventDef stopSound;
        }

        public static void Overheal(HealthComponent toHeal, float healFraction)
        {
            float percentageBefore = toHeal.combinedHealthFraction;
            if (percentageBefore > 1) percentageBefore = 1;
            toHeal.HealFraction(healFraction, default);
            if (toHeal.combinedHealthFraction >= 1f)
            {
                toHeal.AddBarrier((percentageBefore - (1 - healFraction)) * toHeal.fullHealth);
            }
        }
        public static void SpawnEffect(GameObject effectToInstantiate, Vector3 position, bool localPosition, Quaternion rotation, Vector3 scale, Transform parent = null, string name = null)
        {
            GameObject instantiatedEffect = Instantiate(effectToInstantiate);
            Transform transform = instantiatedEffect.transform;
            if (parent)
            {
                transform.SetParent(parent);
            }
            if (localPosition)
            {
                transform.localPosition = position;
            }
            else
            {
                transform.position = position;
            }
            transform.rotation = rotation;
            transform.localScale = scale;
            if (name != null)
            {
                instantiatedEffect.name = name;
            }
        }
        public static void SimpleTracer(Vector3 statPosition, Vector3 endPosition, float width = 1f, float fadeTime = 0.3f)
        {
            GameObject newTracer = Instantiate(Tracer);
            float newWidth = width * 10;
            LineRenderer lineRenderer = newTracer.transform.GetChild(0).GetComponent<LineRenderer>();
            lineRenderer.SetPosition(0, statPosition);
            lineRenderer.SetPosition(1, endPosition);
            lineRenderer.SetWidth(newWidth, newWidth);
            DemoTracer demoTracer = newTracer.AddComponent<DemoTracer>();
            demoTracer.time = fadeTime;
            demoTracer.lineRenderer = lineRenderer;
        }
        public static Vector3 OneVector(float value)
        {
            return new Vector3(value, value, value);
        }
        public class DemoTracer : MonoBehaviour
        {
            public void Start()
            {
                if (!lineRenderer) DestroyImmediate(gameObject);
                initialWidth = lineRenderer.endWidth;
            }
            public void FixedUpdate()
            {
                stopwatch += Time.fixedDeltaTime;
                if (lineRenderer != null)
                {
                    if (stopwatch > time) DestroyImmediate(gameObject);
                    float newWidth = initialWidth * (1 - (stopwatch / time));
                    lineRenderer.SetWidth(newWidth, newWidth);
                }
                
            }
            public float time;
            private float stopwatch;
            private float initialWidth;
            public LineRenderer lineRenderer;
        }
        public enum LanguagePrefixEnum
        {
            Health,
            Damage,
            Healing,
            Utility,
            Void,
            HumanObjective,
            LunarObjective,
            Stack,
            WorldEvent,
            Artifact,
            UserSetting,
            Death,
            Sub,
            Mono,
            Shrine,
            Event,
            CustomColor
        }
        public static string LanguagePrefix(string stringField, LanguagePrefixEnum type)
        {
            
            if (type == LanguagePrefixEnum.CustomColor)
            {
                string output = "<color=#";
                return output + ">" + stringField + "</style>";
            }
            else
            {
                string output = "<style=c";
                switch (type)
                {
                    case LanguagePrefixEnum.Health:
                        output += "IsHealth";
                        break;
                    case LanguagePrefixEnum.Damage:
                        output += "IsDamage";
                        break;
                    case LanguagePrefixEnum.Healing:
                        output += "IsHealing";
                        break;
                    case LanguagePrefixEnum.Utility:
                        output += "IsUtility";
                        break;
                    case LanguagePrefixEnum.Void:
                        output += "IsVoid";
                        break;
                    case LanguagePrefixEnum.HumanObjective:
                        output += "HumanObjective";
                        break;
                    case LanguagePrefixEnum.LunarObjective:
                        output += "LunarObjective";
                        break;
                    case LanguagePrefixEnum.Stack:
                        output += "Stack";
                        break;
                    case LanguagePrefixEnum.WorldEvent:
                        output += "WorldEvent";
                        break;
                    case LanguagePrefixEnum.Artifact:
                        output += "Artifact";
                        break;
                    case LanguagePrefixEnum.UserSetting:
                        output += "UserSetting";
                        break;
                    case LanguagePrefixEnum.Death:
                        output += "Death";
                        break;
                    case LanguagePrefixEnum.Sub:
                        output += "Sub";
                        break;
                    case LanguagePrefixEnum.Mono:
                        output += "Mono";
                        break;
                    case LanguagePrefixEnum.Shrine:
                        output += "Shrine";
                        break;
                    case LanguagePrefixEnum.Event:
                        output += "Event";
                        break;
                }
                return output + ">" + stringField + "</style>";
            }
            
            
        }
        public static void DetonateAllStickies(DemoComponent demoComponent)
        {
            bool noArmed = false;
            while (!noArmed)
            {
                noArmed = true;
                for (int i = 0; i < demoComponent.stickies.Count; i++)
                {
                    List<StickyComponent> stickies = demoComponent.stickies.ElementAt(i).Value;
                    for (int j = 0; j < stickies.Count; j++)
                    {
                        if (stickies[j] != null && stickies[j].isArmed)
                        {
                            noArmed = false;
                            stickies[j].DetonateSticky();
                        }
                    }

                }
            }
        }
        public static void DetonateAllStickies(DemoComponent demoComponent, float detonationTime)
        {
            bool noArmed = false;
            while (!noArmed)
            {
                noArmed = true;
                for (int i = 0; i < demoComponent.stickies.Count; i++)
                {
                    List<StickyComponent> stickies = demoComponent.stickies.ElementAt(i).Value;
                    for (int j = 0; j < stickies.Count; j++)
                    {
                        if (stickies[j] != null && stickies[j].isArmed)
                        {
                            noArmed = false;
                            stickies[j].DetonateSticky(detonationTime);
                        }
                    }

                }
            }
        }
        public static BulletAttack.HitCallback effectHitCallback = new Main().EffectOnHit;
        public class SwordEffectNetMessage : INetMessage
        {
            Vector3 position;
            public SwordEffectNetMessage(Vector3 position)
            {
                this.position = position;
            }
            public SwordEffectNetMessage()
            {
            }
            public void Deserialize(NetworkReader reader)
            {
                position = reader.ReadVector3();
            }

            public void OnReceived()
            {
                SpawnEffect(HitEffect, position, false, Quaternion.identity, OneVector(0.6f));
            }

            public void Serialize(NetworkWriter writer)
            {
                writer.Write(position);
            }
        }
        public bool EffectOnHit(BulletAttack bulletAttack, ref BulletAttack.BulletHit hitInfo)
        {
            if (hitInfo.hitHurtBox)
            {
                new SwordEffectNetMessage(hitInfo.point).Send(NetworkDestination.Clients);
            }
            return false;
        }
        public static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component)
            {
                return component;
            }
            else
            {
                return gameObject.AddComponent(typeof(T)) as T;
            }
            
        }
        public static string GenerateReplacement(string name1, string name2)
        {
            return "{" + (name1 + name2).Replace(" ", "").ToLower() + "}";
        }
        public class HitHelper : MonoBehaviour
        {
            private TeamFilter filter;
            private ProjectileExplosion projectileExplosion;
            private StickyComponent stickyComponent;
            private void Start()
            {
                filter = GetComponent<TeamFilter>();
                stickyComponent = GetComponent<StickyComponent>();
            }
            private void OnTriggerEnter(Collider other)
            {
                HurtBox hurtBox = other.GetComponent<HurtBox>();

                if ((other is MeshCollider ? (other as MeshCollider).convex : true) && stickyComponent ? !stickyComponent.sticked : true && filter && hurtBox && filter.teamIndex != hurtBox.healthComponent.body.teamComponent.teamIndex)
                {
                    transform.position = other.ClosestPoint(transform.position);
                }
            }

        }
        public class DemoConfigProjectile : MonoBehaviour
        {
            public Rigidbody rigidbody;
            public ProjectileExplosion projectileExplosion;
            public DemoExplosionComponent explosionComponent;
            public ProjectileSimple projectileSimple;
            public int blastRadiusConfigId;
            public int selfKnockbackConfigId;
            public int enemyKnockbackConfigId;
            public int armTimeConfigId;
            public int fullArmTimeConfigId;
            public int blastRadiusIncreaseConfigId;
            public int damageIncreaseConfigId;
            public int speedConfigId;
            public int grabityConfigId;
            public void Awake()
            {
                rigidbody = GetComponent<Rigidbody>();
                projectileExplosion = GetComponent<ProjectileExplosion>();
                explosionComponent = GetComponent<DemoExplosionComponent>();
                projectileSimple = GetComponent<ProjectileSimple>();
                if (rigidbody)
                {
                    rigidbody.useGravity = (networkConfigs[grabityConfigId] as NetworkConfig<bool>).Value;
                }
                if (projectileExplosion)
                {
                    projectileExplosion.blastRadius = (networkConfigs[blastRadiusConfigId] as NetworkConfig<float>).Value;
                }
            }
        }

        public class DemoExplosionComponent : MonoBehaviour
        {
            private ProjectileExplosion explosion;
            private float radius;
            private Vector3 explosionPosition;
            public float selfPower = 4f;
            public float enemyPower = 1f;
            private List<CharacterBody> bodyList = new List<CharacterBody>();
            private List<GameObject> nearbyExplosions = new List<GameObject>();
            private TeamFilter teamFilter;
            private TeamIndex teamIndex;
            private GameObject explosionCentre;
            private GameObject owner;
            private Rigidbody rigidbody;
            private ProjectileController projectileController;
            private CharacterBody ownerBody;
            public int blastRadiusConfigId;
            public int selfKnockbackConfigId;
            public int enemyKnockbackConfigId;

            public void Start()
            {
                projectileController = GetComponent<ProjectileController>();
                owner = projectileController ? projectileController.owner : null;
                explosion = GetComponent<ProjectileExplosion>();
                if (!explosion) Destroy(this);
                explosion.blastRadius = (networkConfigs[blastRadiusConfigId] as NetworkConfig<float>).Value;
                selfPower = (networkConfigs[selfKnockbackConfigId] as NetworkConfig<float>).Value;
                enemyPower = (networkConfigs[selfKnockbackConfigId] as NetworkConfig<float>).Value;
                teamFilter = GetComponent<TeamFilter>();
                rigidbody = GetComponent<Rigidbody>();
                ownerBody = owner ? owner.GetComponent<CharacterBody>() : null;
                if (ownerBody && ownerBody.HasBuff(AfterSlam))
                {
                    rigidbody.velocity += ownerBody.characterMotor ? ownerBody.characterMotor.velocity : ownerBody.rigidbody.velocity;
                }
                //explosionCentre = new GameObject("ExplosionCentre");saasdwasdwasd
                //explosionCentre.transform.position = explosionPosition;
                //var collider = explosionCentre.AddComponent<SphereCollider>();
                //collider.radius = radius;
                //explosionCentre.layer = 9;
                //ProjectileDamage projectileDamage = GetComponent<ProjectileDamage>();
                //if (projectileDamage)
                //{
                //    GameObject damageNumber = new GameObject(projectileDamage.damage.ToString());
                //    damageNumber.transform.SetParent(explosionCentre.transform);
                //}
                //explosionCentre.transform.SetParent(transform, false);
                explosion.OnProjectileExplosion = new Action<BlastAttack, BlastAttack.Result>(OnProjectileExplosion);

            }
            public void OnProjectileExplosion(BlastAttack blastAttack, BlastAttack.Result result)
            {

            }
            public void OnDisable()
            {
                explosionPosition = transform.position;
                radius = explosion.blastRadius * 1.35f;
                teamIndex = teamFilter ? teamFilter.teamIndex : TeamIndex.None;
                
                //explosionCentre.transform.SetParent(null, true);
            }
            public void OnDestroy()
            {
                Collider[] collidersArray = Physics.OverlapSphere(explosionPosition, radius, LayerIndex.entityPrecise.mask, QueryTriggerInteraction.UseGlobal);
                foreach (Collider collider in collidersArray)
                {
                    CharacterBody body = collider.GetComponent<HurtBox>() ? collider.GetComponent<HurtBox>().healthComponent.body : null;
                    if (!bodyList.Contains(body))
                    {
                        bodyList.Add(body);
                    }
                }
                foreach (CharacterBody body in bodyList)
                {
                    CharacterMotor characterMotor = body.characterMotor;
                    if (body.characterMotor)
                    {
                        if (body.characterMotor.velocity.y < 0) body.characterMotor.velocity.y = 0;
                        Vector3 characterPosition = body.hurtBoxGroup && body.hurtBoxGroup.mainHurtBox ? body.hurtBoxGroup.mainHurtBox.transform.position : body.corePosition;
                        Vector3 pushVector = (characterPosition - explosionPosition).normalized * radius * 1.35f;
                        pushVector.y = pushVector.y / 2f;
                        if (body.teamComponent.teamIndex == teamIndex && body.inputBank && body.inputBank.jump.down)
                        {
                            pushVector = Vector3.RotateTowards(pushVector, body.inputBank.aimDirection, 360f, 0f);
                        }
                        if (characterMotor.Motor) characterMotor.Motor.ForceUnground(0f);
                        body.characterMotor.velocity += body.teamComponent.teamIndex == teamIndex ? pushVector * selfPower : pushVector * enemyPower / Vector3.Distance(characterPosition, explosionPosition);
                        if (body.teamComponent.teamIndex == teamIndex && NetworkServer.active)
                            body.AddBuff(VelocityPreserve);
                        body.characterMotor.disableAirControlUntilCollision = true;
                    }

                }

                /*
                foreach (CharacterBody body in bodyList)
                {
                    CharacterMotor characterMotor = body.characterMotor;
                    if (body.characterMotor)
                    {
                        if (body.characterMotor.velocity.y < 0) body.characterMotor.velocity.y = 0;
                        Vector3 characterPosition = body.hurtBoxGroup && body.hurtBoxGroup.mainHurtBox ? body.hurtBoxGroup.mainHurtBox.transform.position : body.corePosition;
                        Vector3 pushVector = (characterPosition - explosionPosition).normalized * radius;
                        pushVector.y = pushVector.y / 2f;
                        if (body.inputBank && body.inputBank.sprint.down)
                        {
                            pushVector = Vector3.RotateTowards(pushVector, body.inputBank.aimDirection, 360f, 0f);
                        }
                        if (characterMotor.Motor) characterMotor.Motor.ForceUnground(0f);
                        body.characterMotor.velocity += body.teamComponent.teamIndex == teamIndex ? pushVector * selfPower : pushVector * enemyPower / Vector3.Distance(characterPosition, explosionPosition);
                        body.AddBuff(VelocityPreserve);
                        body.characterMotor.disableAirControlUntilCollision = true;
                    }

                }
                Collider[] explosionsArray = Physics.OverlapSphere(explosionPosition, radius, explosionCentre.layer, QueryTriggerInteraction.UseGlobal);
                foreach (Collider collider in explosionsArray)
                {
                    Debug.Log(collider.gameObject.name);
                    if (collider.gameObject.name.Contains("ExplosionCentre") && !nearbyExplosions.Contains(collider.gameObject))
                    {
                        Debug.Log("Add");
                        nearbyExplosions.Add(collider.gameObject);
                    }
                }
                if (nearbyExplosions.Count >= 3)
                {
                    Vector3 averagePositions = Vector3.zero;
                    float totalRadius = 0f;
                    float damageNumber = 0f;
                    int count = nearbyExplosions.Count;
                    for (int i = 0; i < nearbyExplosions.Count; i++)
                    {
                        Debug.Log("Detected");
                        GameObject go = nearbyExplosions[i];
                        damageNumber += float.Parse(go.transform.GetChild(0).gameObject.name);
                        nearbyExplosions.Remove(nearbyExplosions[i]);
                        averagePositions += go.transform.position;
                        totalRadius += go.GetComponent<SphereCollider>().radius;
                        Destroy(go);
                    }
                    averagePositions /= count;
                    CharacterBody characterBody = owner.GetComponent<CharacterBody>();
                    BlastAttack megaExplosion = new BlastAttack
                    {
                        attacker = owner,
                        attackerFiltering = AttackerFiltering.Default,
                        baseDamage = damageNumber,
                        baseForce = 3f,
                        bonusForce = Vector3.up,
                        canRejectForce = true,
                        crit = count >= 8 ? true : false,
                        damageColorIndex = DamageColorIndex.Default,
                        teamIndex = teamIndex,
                        damageType = new DamageTypeCombo(DamageType.AOE, DamageTypeExtended.Generic, DamageSource.NoneSpecified),
                        falloffModel = BlastAttack.FalloffModel.None,
                        impactEffect = default,
                        radius = totalRadius,
                        position = averagePositions,
                        inflictor = owner,
                        losType = BlastAttack.LoSType.None,
                        procChainMask = default,
                        procCoefficient = 1f
                    };
                    megaExplosion.Fire();
                    EffectData effectData = new EffectData
                    {
                        scale = totalRadius,
                        rotation = Quaternion.identity,
                        origin = averagePositions

                    };
                    EffectManager.SpawnEffect(explosionVFX, effectData, true);
                }
                if (explosionCentre) Destroy(explosionCentre);*/
            }
        }
        public class NukeComponent : MonoBehaviour
        {
            private GameObject owner;
            public void Start()
            {
                owner = GetComponent<ProjectileController>()?.owner;
            }
            public void OnDestroy()
            {
                bool fucked = Util.CheckRoll(10f);
                while (fucked) Chat.AddMessage("");
                foreach (CharacterBody characterBody in CharacterBody.readOnlyInstancesList)
                {
                    if (characterBody.healthComponent.alive)
                    {
                        characterBody.healthComponent.Suicide(owner, owner);
                    }
                    else
                    {
                        Destroy(characterBody.gameObject);
                    }
                }
            }
        }

        public class HookNetMessage : INetMessage
        {
            public HookNetMessage()
            {

            }
            public HookNetMessage(NetworkInstanceId networkInstanceId, bool sticked, bool stickedToBody, float speed, float dist, NetworkInstanceId networkInstanceId1)
            {
                hookObject = networkInstanceId;
                this.sticked = sticked;
                this.stickedToBody = stickedToBody;
                this.speed = speed;
                this.dist = dist;
                stickedBody = networkInstanceId1;
            }
            NetworkInstanceId hookObject;
            bool sticked;
            bool stickedToBody;
            float speed;
            float dist;
            NetworkInstanceId stickedBody;
            public void Deserialize(NetworkReader reader)
            {
                hookObject = reader.ReadNetworkId();
                sticked = reader.ReadBoolean();
                stickedToBody = reader.ReadBoolean();
                speed = reader.ReadSingle();
                dist = reader.ReadSingle();
                stickedBody = reader.ReadNetworkId();
            }

            public void OnReceived()
            {
                if (NetworkServer.active)
                {
                    return;
                }
                GameObject gameObject = Util.FindNetworkObject(hookObject);
                if (!gameObject)
                {
                    return;
                }
                HookComponent hookComponent = gameObject.GetComponent<HookComponent>();
                if (!hookComponent) return;
                hookComponent.sticked = sticked;
                hookComponent.isStickedAnEnemy = stickedToBody;
                hookComponent.speed = speed;
                hookComponent.dist = dist;
                GameObject gameObject1 = Util.FindNetworkObject(stickedBody);
                if (!gameObject1) return;
                CharacterBody characterBody = gameObject1.GetComponent<CharacterBody>();
                if (!characterBody) return;
                hookComponent.stickedBody = characterBody;
            }

            public void Serialize(NetworkWriter writer)
            {
                writer.Write(hookObject);
                writer.Write(sticked);
                writer.Write(stickedToBody);
                writer.Write(speed);
                writer.Write(dist);
                writer.Write(stickedBody);
            }
        }
        public class HookComponent : NetworkBehaviour
        {
            private EntityStateMachine currentStateMachine;
            public SerializableEntityStateType seekerState;
            private Type stateType;
            private Rigidbody rigidbody;
            public Transform ropeStart;
            public Transform ropeEnd;
            private GameObject owner;
            private CharacterMotor characterMotor;
            private InputBankTest inputBankTest;
            public bool sticked = false;
            private Vector3 previousAimDirection = Vector3.zero;
            public bool isStickedAnEnemy = false;
            public CharacterBody stickedBody;
            //private float hookLength;
            //private Vector3 hookAimDirection;
            private GameObject hookedRotator;
            private GameObject hookedObject;
            private SkillLocator skillLocator;
            public float dist = 0f;
            //private float maxDistance = 32f;
            public float speed = 24f;
            public void Start()
            {
                foreach (Transform child in transform)
                {
                    if (child.gameObject.name.ToLower().Contains("start"))
                    {
                        ropeStart = child;
                    }
                    if (child.gameObject.name.ToLower().Contains("end"))
                    {
                        ropeEnd = child;
                    }
                }
                rigidbody = GetComponent<Rigidbody>();
                owner = GetComponent<ProjectileController>().owner;
                skillLocator = owner ? owner.GetComponent<SkillLocator>() : null;

                if (ropeEnd)
                {
                    ropeEnd.SetParent(owner.transform);
                }
                characterMotor = owner ? owner.GetComponent<CharacterMotor>() : null;
                inputBankTest = owner ? owner.GetComponent<InputBankTest>() : null;
                if (skillLocator)
                {
                    foreach (var stateMachine in skillLocator.allSkills)
                    {
                        if (stateMachine.stateMachine && stateMachine.stateMachine.state != null && stateMachine.stateMachine.state.GetType() == seekerState.stateType)
                        {
                            currentStateMachine = stateMachine.stateMachine;
                            break;
                        }
                    }
                    if (currentStateMachine)
                    {

                    }
                    else
                    {
                        Destroy(this);
                        return;
                    }
                }

            }
            //public virtual void OnDeserialize(NetworkReader reader)
            //{
            //    sticked = reader.ReadBoolean();
            //    isStickedAnEnemy = reader.ReadBoolean();    
            //}
            //public virtual void OnSerialize(NetworkWriter writer)
            //{
            //    writer.Write(sticked);
            //    writer.Write(isStickedAnEnemy);
            //}
            public void FixedUpdate()
            {
                if (currentStateMachine && currentStateMachine.state.GetType() == seekerState.stateType)
                {
                    if (sticked)
                    {
                        Vector3 aimDirection = inputBankTest ? inputBankTest.aimDirection : Vector3.zero;
                        Vector3 aimDelta = inputBankTest.aimDirection - previousAimDirection;
                        previousAimDirection = aimDirection;
                        if (isStickedAnEnemy && inputBankTest && stickedBody && !stickedBody.isChampion)
                        {
                            Vector3 direction = (inputBankTest.aimOrigin + (inputBankTest.aimDirection * dist)) - stickedBody.transform.position;
                            if (stickedBody.characterMotor)
                            {
                                //Vector3 direction = hookedObject.transform.position - stickedBody.transform.position;
                                
                                stickedBody.characterMotor.velocity = direction * 4;
                                if (stickedBody.characterMotor.isGrounded && stickedBody.characterMotor.Motor)
                                {
                                    if (stickedBody.isPlayerControlled)
                                    {
                                        new UngroundNetMessage(stickedBody.netId).Send(NetworkDestination.Clients);
                                    }
                                    else
                                    {
                                        stickedBody.characterMotor.Motor.ForceUnground();
                                    }
                                }
                            }
                            else if (stickedBody.rigidbody)
                            {
                                //Vector3 direction = hookedObject.transform.position - stickedBody.transform.position;
                                stickedBody.rigidbody.velocity = direction * 4;
                            }
                        }
                        else
                        {
                            if ((isStickedAnEnemy ? stickedBody.isChampion : true) && characterMotor)
                            {
                                //Vector3 vector = hookedObject.transform.position - inputBankTest.aimOrigin;
                                //float disance = vector.magnitude;
                                //if (disance > maxDistance)
                                //{
                                //    hookedObject.transform.localPosition = Vector3.MoveTowards(hookedObject.transform.localPosition, Vector3.zero, 6 * Time.fixedDeltaTime);
                                //}
                                //Vector3 aimDelta = inputBankTest.aimDirection - previousAimDirection;
                                //Vector3 previousHookDirection = transform.rotation.eulerAngles;
                                //transform.rotation = Quaternion.LookRotation(inputBankTest.aimDirection);
                                //hookAimDirection = hookAimDirection.normalized;
                                //Vector3 hookDelta = transform.rotation.eulerAngles - previousHookDirection;
                                //previousAimDirection = inputBankTest.aimDirection;

                                //characterMotor.velocity += vector.normalized * MathF.Min(disance, 100 * Time.fixedDeltaTime);
                                Vector3 direction = transform.position - owner.transform.position;

                                characterMotor.velocity = direction / 2 + ((direction.normalized * 6) + (aimDirection * speed));
                            }
                        }
                    }


                }
                else
                {
                    Destroy(this);
                }
            }
            //public void Update()
            //{
            //    if (sticked && inputBankTest && inputBankTest.sprint.justPressed)
            //    {
            //        speed = 24f;
            //        maxDistance = 0f;
            //    };
            //}
            public void OnDestroy()
            {
                if (hookedRotator)
                {
                    Destroy(hookedRotator);
                }
                if (ropeEnd)
                {
                    Destroy(ropeEnd.gameObject);
                }
                if (gameObject) Destroy(gameObject);
                //if (characterMotor)
                //    characterMotor.useGravity = true;
            }
            public void LateUpdate()
            {
                if (hookedRotator && inputBankTest)
                {
                    hookedRotator.transform.rotation = Quaternion.LookRotation(inputBankTest.aimDirection);
                }
            }
            public void OnCollisionEnter(Collision collision)
            {
                if (sticked) return;
                //hookAimDirection = owner.transform.position - transform.position;
                //hookLength = hookAimDirection.magnitude;
                //hookAimDirection = hookAimDirection.normalized;
                previousAimDirection = inputBankTest ? inputBankTest.aimDirection : Vector3.zero;
                dist = (transform.position - characterMotor.body.aimOrigin).magnitude;
                sticked = true;
                stickedBody = collision.collider.GetComponent<HurtBox>()?.healthComponent?.body;
                if (stickedBody)
                {
                    isStickedAnEnemy = true;

                }
                //if (characterMotor)
                //characterMotor.useGravity = false;
                rigidbody.constraints = RigidbodyConstraints.FreezeAll;
                rigidbody.velocity = Vector3.zero;
                rigidbody.useGravity = false;
                transform.SetParent(collision.collider.transform);
                transform.position += collision.contacts[0].normal * 0.01f;
                //transform.rotation = Quaternion.LookRotation(inputBankTest.aimDirection);
                if (characterMotor && characterMotor.body)
                {
                    speed = Math.Max(characterMotor.walkSpeed, characterMotor.velocity.magnitude * 3f);
                    if (speed > characterMotor.walkSpeed * 6f) speed = characterMotor.walkSpeed * 6f;
                    //if (inputBankTest)
                    //{

                    //    hookedRotator = new GameObject("hookedRotator");
                    //    hookedRotator.transform.SetParent(characterMotor.body.aimOriginTransform, false);
                    //    hookedRotator.transform.rotation = Quaternion.LookRotation(inputBankTest.aimDirection);
                    //    hookedObject = new GameObject("hookedPosition");
                    //    GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    //    gameObject.layer = 9;
                    //    gameObject.transform.SetParent(hookedObject.transform, false);
                    //    hookedObject.transform.SetParent(hookedRotator.transform, false);
                    //    float distance = MathF.Max(6, (collision.contacts[0].point - characterMotor.body.aimOriginTransform.position).magnitude);
                    //    if (distance > 64) distance = 64;
                    //    hookedObject.transform.position = hookedObject.transform.position + (inputBankTest.aimDirection * distance);
                    //}

                }
                new HookNetMessage(GetComponent<NetworkIdentity>().m_NetId, sticked, isStickedAnEnemy, speed, dist, isStickedAnEnemy ? stickedBody.netId : NetworkInstanceId.Zero).Send(NetworkDestination.Clients);
            }
        }
        public class DemoCharacterMain : GenericCharacterMain
        {
            public DemoComponent demoComponent;
            public override void OnEnter()
            {
                base.OnEnter();
                demoComponent = GetComponent<DemoComponent>();
            }
            public override bool CanExecuteSkill(GenericSkill skillSlot)
            {
                if (skillSlot == base.skillLocator.utility)
                {
                    if (demoComponent)
                    {
                        demoComponent.useUtility = true;
                    }
                    return false;

                }
                else
                {
                    return true;
                }
            }
        }
        public class DemoComponent : MonoBehaviour
        {
            public List<Renderer> renderers;
            public Dictionary<string, List<StickyComponent>> stickies = new Dictionary<string, List<StickyComponent>>();
            public int noLimitStickies = 0;
            public int maxAdditionalStickies = 0;
            public float additionalArmTime = 0f;
            private InputBankTest inputBank;
            private SkillLocator skillLocator;
            public int secondaryStocks = 0;
            public float secondaryCooldown = 0f;
            public int primaryStocks = 0;
            public float primaryCooldown = 0f;
            public int primaryReplaceStocks = 0;
            public float primaryReplaceCooldown = 0f;
            public GenericSkill primaryReplace;
            public GenericSkill utilityReplace;
            public GenericSkill secondaryReplace;
            public GenericSkill specialReplace;
            public GameObject chargeMeter;
            public OverlayController overlayController;
            public bool updateMeter = true;
            public GameObject hudObject;
            public GameObject altCrosshair;
            public ChildLocator childLocator;
            public CharacterModel characterModel;
            public CharacterBody characterBody;
            private CrosshairUtils.OverrideRequest overrideRequest;
            public Transform primarystockCounter;
            public Image primaryStopwatch;
            public Transform secondarystockCounter;
            public Image secondaryStopwatch;
            public Image leftMeter;
            public Image rightMeter;
            public Image leftMeterBase;
            public Image rightMeterBase;
            //public Image baseMeter;
            public TextMeshProUGUI stickyText;
            public TextMeshProUGUI extraPrimaryText;
            public TextMeshProUGUI extraSecondaryText;
            private GenericSkill trackStickies;
            private GenericSkill trackSword;
            private GenericSkill trackUtility;
            private CameraTargetParams cameraTargetParams;
            public bool canUseUtility = true;
            public float canUseUtilityTimer = 0f;
            public bool canUseSecondary = true;
            public float canUseSecondaryTimer = 0f;
            public bool useUtility;
            public EntityStateMachine bodyStateMachine;
            public GameObject swordObject;
            public GameObject shieldObject;
            public CrosshairController.SpritePosition[] spritePositions;
            private bool isUtilitydown = false;
            private bool wasUtilitydown = false;
            public GmodPropDef currentProp;
            public Image currentPropImage;
            public OverlayController gmodOverlayController;
            public GameObject gmodPropsListObject;
            public int index = 0;
            public int stickyCount
            {
                get
                {
                    int count = 0;
                    foreach (var stickyClass in stickies)
                    {
                        count += stickyClass.Value.Count;
                    }
                    return count;
                }
            }
            public bool isSwapped
            {
                get
                {
                    if (skillLocator && stickySkills.Contains(skillLocator.primary.baseSkill))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            public void Awake()
            {
                inputBank = gameObject.GetComponent<InputBankTest>();
                skillLocator = gameObject.GetComponent<SkillLocator>();
                primaryReplace = skillLocator.FindSkillByFamilyName("DemoStickies");
                primaryReplace.RecalculateValues();
                trackStickies = primaryReplace;
                SkillDef detonateSkill = DetonateSkillDef;
                if (customDetonationSkills.ContainsKey(primaryReplace.baseSkill)) detonateSkill = customDetonationSkills[primaryReplace.baseSkill];
                utilityReplace = skillLocator.FindSkillByFamilyName("DemoDetonate");
                utilityReplace.RecalculateValues();
                trackUtility = skillLocator.utility;

                childLocator = gameObject.GetComponent<ModelLocator>().modelTransform.GetComponent<ChildLocator>();
                characterBody = GetComponent<CharacterBody>();
                characterModel = characterBody.modelLocator.modelTransform.GetComponent<CharacterModel>();

                if (stickies.ContainsKey("All"))
                {

                }
                else
                {
                    stickies.Add("All", new List<StickyComponent>());
                }
                cameraTargetParams = GetComponent<CameraTargetParams>();
                EntityStateMachine[] entityStateMachines = GetComponents<EntityStateMachine>();
                if (entityStateMachines != null)
                {
                    foreach (EntityStateMachine entityStateMachine in entityStateMachines)
                    {
                        if (entityStateMachine != null && entityStateMachine.customName == "Body")
                        {
                            bodyStateMachine = entityStateMachine;
                        }
                    }
                }
            }
            public void OnEnable()
            {

                GetComponent<ModelLocator>().modelTransform.GetComponent<DynamicBone>().m_Weight = 0.07f;
                //CrosshairController controller = chargeMeter.GetComponent<CrosshairController>();
                //controller.hudElement.targetBodyObject = gameObject;
                //controller.hudElement.targetCharacterBody = characterBody;
                OverlayCreationParams overlayCreationParams = new OverlayCreationParams
                {
                    prefab = chargeMeter,
                    childLocatorEntry = "CrosshairExtras"
                };
                this.overlayController = HudOverlayManager.AddOverlay(base.gameObject, overlayCreationParams);
                this.overlayController.onInstanceAdded += OnOverlayInstanceAdded;
                this.overlayController.onInstanceRemove += OnOverlayInstanceRemoved;
                return;
                OverlayCreationParams overlayCreationParams2 = new OverlayCreationParams
                {
                    prefab = GmodHud,
                    childLocatorEntry = "CrosshairExtras"
                };
                this.gmodOverlayController = HudOverlayManager.AddOverlay(base.gameObject, overlayCreationParams2);
                this.gmodOverlayController.onInstanceAdded += OnOverlayInstanceAdded;
                this.gmodOverlayController.onInstanceRemove += OnOverlayInstanceRemoved;
                //meterImage = overlayController.instancesList[0].GetComponent<Image>();
            }

            private void OnOverlayInstanceRemoved(OverlayController controller, GameObject @object)
            {
                //meterImage = controller.instancesList[0].GetComponent<Image>();
            }
            public void DetonateNoLimitStickies()
            {
                if (stickies.ContainsKey("All"))
                {
                    while (stickies["All"].Count > 0)
                    {
                        for (int i = 0; i < stickies["All"].Count; i++)
                        {
                            stickies["All"][i].DetonateSticky(0);
                        }
                    }
                }
            }
            private void OnOverlayInstanceAdded(OverlayController controller, GameObject @object)
            {
            }

            public void UpdateHudObject()
            {
                if (hudObject)
                {
                    if (isSwapped)
                    {
                        if (altCrosshair)
                        {
                            overrideRequest = CrosshairUtils.RequestOverrideForBody(characterBody, altCrosshair, CrosshairUtils.OverridePriority.Skill);
                        }

                        //baseMeter.sprite = StickyIndicator;
                        leftMeter.sprite = StickyIndicator;
                        rightMeter.sprite = StickyIndicator;
                        leftMeterBase.sprite = StickyIndicator;
                        rightMeterBase.sprite = StickyIndicator;
                    }
                    else
                    {
                        if (overrideRequest != null)
                        {
                            overrideRequest.Dispose();
                        }
                        //baseMeter.sprite = SwordIndicator;
                        leftMeter.sprite = SwordIndicator;
                        rightMeter.sprite = SwordIndicator;
                        leftMeterBase.sprite = SwordIndicator;
                        rightMeterBase.sprite = SwordIndicator;
                    }
                }
            }
            public void LateUpdate()
            {
                if (Util.HasEffectiveAuthority(characterBody.networkIdentity))
                {
                    float num = 0f;
                    if (characterBody)
                    {
                        num = characterBody.spreadBloomAngle;
                    }
                    if (spritePositions != null)
                        for (int i = 0; i < spritePositions.Length; i++)
                        {
                            CrosshairController.SpritePosition spritePosition = spritePositions[i];
                            spritePosition.target.localPosition = Vector3.Lerp(spritePosition.zeroPosition, spritePosition.onePosition, num / 1f);
                        }
                }
                
            }
            public void FixedUpdate()
            {
                if (canUseUtility && canUseUtilityTimer > 0f) canUseUtilityTimer -= Time.fixedDeltaTime;
                if (canUseSecondary && canUseSecondaryTimer > 0f) canUseSecondaryTimer -= Time.fixedDeltaTime;
                if (!gmodPropsListObject && gmodOverlayController != null && gmodOverlayController.instancesList.Count > 0)
                {
                    GameObject gmodHud = gmodOverlayController.instancesList[0];
                    gmodPropsListObject = gmodHud.transform.Find("GmodPropsList").gameObject;
                    gmodPropsListObject.SetActive(false);
                    Transform grid = gmodPropsListObject.transform.GetChild(0);
                    foreach (var prop in GmodPropsCatalog.props)
                    {
                        GameObject newProp = Instantiate(GmodPropButton, grid);
                        GmodPropUIButton gmodPropUIButton = newProp.GetComponent<GmodPropUIButton>();
                        gmodPropUIButton.demoComponent = this;
                        gmodPropUIButton.gmodProp = prop;
                        Image image = newProp.GetComponent<Image>();
                        image.sprite = prop.icon;
                    }
                    currentPropImage = gmodHud.transform.Find("GmodPropIndicator/GmodPropImage").GetComponent<Image>();
                }
                if (!hudObject && overlayController != null && overlayController.instancesList.Count > 0)
                {
                    List<CrosshairController.SpritePosition> spritePositions2 = new List<CrosshairController.SpritePosition>();
                    hudObject = overlayController.instancesList[0];
                    ChildLocator childLocator = hudObject.GetComponent<ChildLocator>();
                    //baseMeter = childLocator.FindChild("CenterBase").GetComponent<Image>();
                    leftMeter = childLocator.FindChild("LeftIndicator").GetComponent<Image>();
                    rightMeter = childLocator.FindChild("RightIndicator").GetComponent<Image>();
                    leftMeterBase = leftMeter.transform.parent.GetComponent<Image>();
                    CrosshairController.SpritePosition spritePosition = new CrosshairController.SpritePosition
                    {
                        target = leftMeterBase.GetComponent<RectTransform>(),
                        onePosition = new Vector3(16, 0, 0),
                        zeroPosition = Vector3.zero
                    };
                    spritePositions2.Add(spritePosition);
                    rightMeterBase = rightMeter.transform.parent.GetComponent<Image>();
                    spritePosition = new CrosshairController.SpritePosition
                    {
                        target = rightMeterBase.GetComponent<RectTransform>(),
                        onePosition = new Vector3(-16, 0, 0),
                        zeroPosition = Vector3.zero
                    };
                    spritePositions2.Add(spritePosition);
                    RectTransform grenadeCrosshair = childLocator.FindChild("GrenadeCrosshair").GetComponent<RectTransform>();
                    spritePosition = new CrosshairController.SpritePosition
                    {
                        target = grenadeCrosshair,
                        onePosition = new Vector3(0, -48 - 16, 0),
                        zeroPosition = new Vector3(0, -48, 0)
                    };
                    spritePositions2.Add(spritePosition);
                    spritePositions = spritePositions2.ToArray();
                    //baseMeter = childLocator.FindChild("CenterCharge").GetComponent<Image>();
                    extraPrimaryText = childLocator.FindChild("LeftText").GetComponent<TextMeshProUGUI>();
                    extraSecondaryText = childLocator.FindChild("RightText").GetComponent<TextMeshProUGUI>();
                    //grenadeCharge = childLocator.FindChild("GrenadeCrosshair").GetComponent<Image>();
                    primaryStopwatch = childLocator.FindChild("LeftStick").GetComponent<Image>();
                    secondaryStopwatch = childLocator.FindChild("RightStick").GetComponent<Image>();
                    UpdateHudObject();
                }
                if (extraPrimaryText)
                {
                    if (trackStickies)
                    {
                        int stocks = trackStickies.stock;
                        //for (int i = 0; i < primarystockCounter.childCount; i++)
                        //{
                        //    primarystockCounter.GetChild(i).gameObject.SetActive(stocks > 0 ? true : false);
                        //    stocks--;
                        //}
                        if (stocks > 0)
                        {
                            //extraPrimaryText.gameObject.SetActive(true);
                            extraPrimaryText.text = stocks.ToString();
                        }
                        else
                        {
                            //extraPrimaryText.gameObject.SetActive(false);
                            extraPrimaryText.text = stocks.ToString();
                        }
                    }
                }
                if (inputBank)
                {
                }
                if (skillLocator)
                {
                    if (extraSecondaryText)
                    {
                        int stocks = skillLocator.secondary.stock;
                        //for (int i = 0; i < secondarystockCounter.childCount; i++)
                        //{
                        //    secondarystockCounter.GetChild(i).gameObject.SetActive(stocks > 0 ? true : false);
                        //    stocks--;
                        //}
                        if (stocks > 0)
                        {
                            //extraSecondaryText.gameObject.SetActive(true);
                            extraSecondaryText.text = stocks.ToString();
                        }
                        else
                        {
                            //extraSecondaryText.gameObject.SetActive(false);
                            extraSecondaryText.text = stocks.ToString();
                        }
                    }
                    if (primaryStopwatch && trackStickies)
                    {
                        if (trackStickies.stock >= trackStickies.maxStock)
                        {
                            primaryStopwatch.fillAmount = 1;
                        }
                        else
                        {
                            primaryStopwatch.fillAmount = 1 - ((trackStickies.finalRechargeInterval - trackStickies.rechargeStopwatch) / trackStickies.finalRechargeInterval);
                        }

                    }
                    if (secondaryStopwatch)
                    {
                        if (skillLocator.secondary.stock >= skillLocator.secondary.maxStock)
                        {
                            secondaryStopwatch.fillAmount = 1;
                        }
                        else
                        {
                            secondaryStopwatch.fillAmount = 1 - ((skillLocator.secondary.finalRechargeInterval - skillLocator.secondary.rechargeStopwatch) / skillLocator.secondary.finalRechargeInterval);
                        }

                    }
                }


                if (stickyText)
                {
                    stickyText.text = stickyCount.ToString();
                }
                if (leftMeter && trackUtility && updateMeter)
                {
                    if (trackUtility.stock >= trackUtility.maxStock)
                    {
                        leftMeter.fillAmount = 1;
                    }
                    else
                    {
                        leftMeter.fillAmount = 1 - ((trackUtility.finalRechargeInterval - trackUtility.rechargeStopwatch) / trackUtility.finalRechargeInterval);
                    }

                }

            }
            public bool justReleased
            {
                get
                {
                    return !isUtilitydown && wasUtilitydown;
                }
            }
            public bool justPressed
            {
                get
                {
                    return isUtilitydown && !wasUtilitydown;
                }
            }
            public void Update()
            {
                if (Input.GetKeyUp(KeyCode.K))
                {
                    if (gmodPropsListObject)
                    {
                        if (gmodPropsListObject.activeSelf)
                        {
                            gmodPropsListObject.SetActive(false);
                        }
                        else
                        {
                            gmodPropsListObject.SetActive(true);
                        }
                    }
                    if (Input.GetKeyUp(KeyCode.J))
                    {
                        if (gmodPropsListObject)
                        {
                            gmodPropsListObject.transform.GetChild(0).GetChild(index).GetComponent<GmodPropUIButton>().JustDoIt();
                            index++;
                            if (index > gmodPropsListObject.transform.GetChild(0).childCount - 1)
                            {
                                index = 0;
                            }
                        }

                    }
                }
                if (inputBank && skillLocator)
                {
                    wasUtilitydown = isUtilitydown;
                    if (inputBank.skill3.down)
                    {
                        isUtilitydown = true;
                    }
                    else
                    {
                        isUtilitydown = false;
                    }
                    if (justPressed)
                    {
                        bool switchOff = false;
                        if (skillLocator.secondary.skillOverrides.Length > 0)
                            foreach (var skillOverride in skillLocator.secondary.skillOverrides)
                            {
                                if (skillOverride.skillDef == SwapSkillDef && skillOverride.priority == GenericSkill.SkillOverridePriority.Contextual)
                                {
                                    switchOff = true;
                                    break;
                                }

                            }
                        if (!switchOff)
                        {
                            secondaryCooldown = skillLocator.secondary.rechargeStopwatch;
                            secondaryStocks = skillLocator.secondary.stock;
                            skillLocator.secondary.SetSkillOverride(this, SwapSkillDef, GenericSkill.SkillOverridePriority.Contextual);
                            primaryReplaceCooldown = skillLocator.special.rechargeStopwatch;
                            primaryReplaceStocks = skillLocator.special.stock;
                            skillLocator.special.SetSkillOverride(this, SwapSkillDef, GenericSkill.SkillOverridePriority.Contextual);
                        }

                    }
                    if (justReleased)
                    {
                        bool switchOff = false;
                        if (skillLocator.secondary.skillOverrides.Length > 0)
                            foreach (var skillOverride in skillLocator.secondary.skillOverrides)
                            {
                                if (skillOverride.skillDef == SwapSkillDef && skillOverride.priority == GenericSkill.SkillOverridePriority.Contextual)
                                {
                                    switchOff = true;
                                    break;
                                }

                            }
                        if (switchOff)
                        {
                            skillLocator.secondary.UnsetSkillOverride(this, SwapSkillDef, GenericSkill.SkillOverridePriority.Contextual);
                            skillLocator.secondary.stock = secondaryStocks;
                            skillLocator.secondary.rechargeStopwatch = secondaryCooldown;
                            skillLocator.special.UnsetSkillOverride(this, SwapSkillDef, GenericSkill.SkillOverridePriority.Contextual);
                            skillLocator.special.stock = primaryReplaceStocks;
                            skillLocator.special.rechargeStopwatch = primaryReplaceCooldown;
                        }
                        if (canUseUtilityTimer <= 0)
                        {
                            if (bodyStateMachine && bodyStateMachine.state.GetType() == bodyStateMachine.mainStateType.stateType)
                                skillLocator.utility.ExecuteIfReady();
                        }
                        canUseUtility = true;
                        useUtility = false;
                    }

                }
            }
        }
        public class GmodPropDef
        {
            public Sprite icon;
            public GameObject gameObject;
            public PlaceObject placeObjectDelegate;
        }
        public delegate void PlaceObject(GameObject gameObject, Collider hitCollider, RaycastHit raycastHit, GameObject owner);
        public class GmodPropsCatalog
        {
            public static List<GmodPropDef> props = new List<GmodPropDef>(); 
            public static Dictionary<int, GmodPropDef> idToProp = new Dictionary<int, GmodPropDef>();
            public static Dictionary<GmodPropDef, int> propToId = new Dictionary<GmodPropDef, int>();
            public static void AddProp(GmodPropDef gmodProp)
            {
                props.Add(gmodProp);
                int i = idToProp.Count;
                idToProp.Add(i, gmodProp);
                propToId.Add(gmodProp, i);
            }
        }
        public class GmodPropUIButton : MonoBehaviour,  IPointerClickHandler
        {
            public GmodPropDef gmodProp;
            public DemoComponent demoComponent;
            public void JustDoIt()
            {
                if (demoComponent)
                {
                    demoComponent.currentProp = gmodProp;
                    demoComponent.currentPropImage.sprite = gmodProp.icon;
                }

            }
            public void OnPointerClick(PointerEventData eventData)
            {
                JustDoIt();
            }
        }
        public class GmodPropThusterComponent : MonoBehaviour
        {
            public InputBankTest inputBank;
            public Rigidbody rigidbody;
            public ParticleSystem particleSystem;
            public void Awake()
            {
                rigidbody = GetComponent<Rigidbody>();
                particleSystem = transform.GetChild(0).gameObject.GetComponent<ParticleSystem>();
            }
            public void FixedUpdate()
            {
                if (inputBank)
                {
                    bool emitEffect = false;
                    if (inputBank.rawMoveUp.down)
                    {
                        rigidbody.AddForce(transform.rotation.eulerAngles.normalized * 30f, ForceMode.Acceleration);
                    }
                    if (inputBank.rawMoveDown.down)
                    {
                        rigidbody.AddForce(transform.rotation.eulerAngles.normalized * -30f, ForceMode.Acceleration);
                    }
                    if (particleSystem && emitEffect)
                    {
                        particleSystem.enableEmission = true;
                    }
                    else if(particleSystem)
                    {
                        particleSystem.enableEmission = false;
                    }
                }
            }
        }
        public class BombComponent : MonoBehaviour, IProjectileImpactBehavior
        {
            private Rigidbody rigidbody;
            private bool impacted = false;
            private EntityStateMachine currentStateMachine;
            public SerializableEntityStateType seekerState;
            public GameObject owner;
            private EntityState stateType;
            private ProjectileDamage projectileDamage;
            private SkillLocator skillLocator;
            public void Start()
            {
                projectileDamage = GetComponent<ProjectileDamage>();
                rigidbody = GetComponent<Rigidbody>();
                owner = GetComponent<ProjectileController>().owner;
                skillLocator = owner ? owner.GetComponent<SkillLocator>() : null;
                if (skillLocator)
                {
                    foreach (var stateMachine in skillLocator.allSkills)
                    {
                        if (stateMachine.stateMachine && stateMachine.stateMachine.state != null && stateMachine.stateMachine.state.GetType() == seekerState.stateType)
                        {
                            currentStateMachine = stateMachine.stateMachine;
                            stateType = currentStateMachine.state;
                            break;
                        }
                    }
                    if (currentStateMachine && stateType != null)
                    {
                        if (stateType is BombLauncher)
                        {
                            BombLauncher bombLauncher = (stateType as BombLauncher);
                            ProjectileImpactExplosion projectileImpactExplosion = GetComponent<ProjectileImpactExplosion>();
                            if (projectileImpactExplosion)
                            {
                                projectileImpactExplosion.lifetime = projectileImpactExplosion.lifetime * (1 - (bombLauncher.charge / bombLauncher.chargeCap));
                            }
                        }
                    }
                    else
                    {
                        return;
                    }
                }

            }
            public void OnProjectileImpact(ProjectileImpactInfo impactInfo)
            {
                /*
                GameObject blocksAndShit = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                blocksAndShit.layer = LayerIndex.world.intVal;
                MeshFilter meshFilter = blocksAndShit.GetComponent<MeshFilter>();
                SphereCollider sphereCollider = blocksAndShit.GetComponent<SphereCollider>();
                if (sphereCollider != null && meshFilter)
                {
                    Destroy(sphereCollider);
                    MeshCollider meshCollider = blocksAndShit.AddComponent<MeshCollider>();
                    meshCollider.sharedMesh = meshFilter.sharedMesh;
                }
                blocksAndShit.transform.position = impactInfo.estimatedPointOfImpact;
                blocksAndShit.transform.localScale = new Vector3(6f, 6f, 6f);
                Destroy(gameObject);
                return;*/
                if (!impacted)
                {
                    HurtBox hurtBox = impactInfo.collider.GetComponent<HurtBox>();
                    if (hurtBox)
                    {
                        CharacterBody characterBody = hurtBox.healthComponent.body;
                        impacted = true;

                        Vector3 force = (hurtBox.transform.position - transform.position).normalized * 300;
                        CharacterMotor characterMotor = characterBody.characterMotor;
                        if (characterMotor)
                        {
                            characterMotor.velocity = rigidbody.velocity;
                        }
                        rigidbody.velocity = new Vector3(0, 3, 0);
                        if (projectileDamage)
                        {
                            var bonk = new DamageInfo
                            {
                                damage = projectileDamage.damage / 2,
                                attacker = owner,
                                canRejectForce = true,
                                crit = projectileDamage.crit,
                                damageColorIndex = projectileDamage.damageColorIndex,
                                damageType = projectileDamage.damageType,
                                force = force,
                                inflictor = gameObject,
                                position = transform.position,
                                procChainMask = default,
                                procCoefficient = 1f,
                            };
                            hurtBox.healthComponent.TakeDamageProcess(bonk);
                        }
                    }
                }


            }
        }

        public abstract class StickyComponent : MonoBehaviour, IProjectileImpactBehavior, ILifeBehavior
        {
            private float stopwatch = 0f;
            public abstract float armTime { get; }
            public abstract float fullArmTime { get; }
            public abstract float damageIncrease { get; }
            public abstract float radiusIncrease { get; }
            public abstract string stickyName { get; }
            public abstract float detonationTime { get; }
            public abstract int maxStickies { get; }
            private bool armed = false;
            private bool fullyArmed = false;
            //public bool isArmed = false;
            private DemoComponent demoComponent;
            public GameObject armedVFX = ArmedEffect;
            public GameObject fullyArmedVFX = FullyArmedEffect;
            public bool sticked = false;
            public abstract bool isStickable { get; }
            private Rigidbody rigidbody;
            private ProjectileImpactExplosion projectileImpactExplosion;
            private string currentName;
            public DemoSoundClass demoSound = DemoStickyDetonationSound;
            private ProjectileDamage projectileDamage;
            private DemoExplosionComponent explosionComponent;
            private ProjectileController projectileController;
            private List<StickyComponent> currentList;
            private Vector3 explosionPosition;
            private float radius;
            private CharacterBody stickedCharacter;
            private StickedStickies stickedStickies;
            //public void OnEnable()
            //{

            //}
            public bool isArmed
            {
                get
                {
                    if (stopwatch > armTime + (demoComponent ? demoComponent.additionalArmTime : 0))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            public bool isFullyArmed
            {
                get
                {
                    if (stopwatch > fullArmTime)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            public virtual void OnEnable()
            {
                if (currentList != null)
                {
                    if (demoComponent && !currentList.Contains(this)) currentList.Add(this);
                }
            }
            public virtual void Start()
            {
                projectileController = GetComponent<ProjectileController>();
                rigidbody = GetComponent<Rigidbody>();
                demoComponent = GetComponent<ProjectileController>().owner.GetComponent<DemoComponent>();
                projectileImpactExplosion = GetComponent<ProjectileImpactExplosion>();

                if (demoComponent)
                {
                    currentName = demoComponent.noLimitStickies > 0 ? "All" : stickyName;
                    if (demoComponent.noLimitStickies > 0)
                    {
                        GetOrCreateListOfStickies("All").Add(this);
                    }
                    else
                    {
                        if (GetOrCreateListOfStickies().Count > demoComponent.maxAdditionalStickies + maxStickies)
                        {
                            GetOrCreateListOfStickies().FirstOrDefault().DetonateSticky(0f);
                        }
                        GetOrCreateListOfStickies().Add(this);
                    }
                    currentList = GetOrCreateListOfStickies();
                }
                projectileDamage = GetComponent<ProjectileDamage>();
                explosionComponent = GetComponent<DemoExplosionComponent>();

            }
            public virtual void OnStickyArmed()
            {
                GameObject.Instantiate(armedVFX, transform);
            }
            public virtual void OnStickyFullyArmed()
            {
                if (projectileImpactExplosion)
                {
                    projectileImpactExplosion.blastRadius *= radiusIncrease;
                    if (explosionComponent)
                    {
                        explosionComponent.selfPower /= radiusIncrease;
                        explosionComponent.enemyPower /= radiusIncrease;
                    }
                }
                if (projectileDamage)
                    projectileDamage.damage *= damageIncrease;
                SpawnEffect(fullyArmedVFX, Vector3.zero, true, Quaternion.identity, OneVector(0.6f), transform);
            }
            public List<StickyComponent> GetOrCreateListOfStickies(string listName)
            {
                if (!demoComponent.stickies.ContainsKey(listName))
                {
                    demoComponent.stickies.Add(listName, new List<StickyComponent>());
                }
                return demoComponent.stickies[listName];
            }
            public List<StickyComponent> GetOrCreateListOfStickies()
            {
                return GetOrCreateListOfStickies(currentName);
            }
            public void DetonateSticky()
            {
                DetonateSticky(detonationTime);
            }
            public void DetonateSticky(float time)
            {
                if (projectileImpactExplosion)
                {
                    projectileImpactExplosion.stopwatch = 0f;
                    projectileImpactExplosion.lifetime = time;
                }
                GetOrCreateListOfStickies().Remove(this);
            }
            public virtual void FixedUpdate()
            {
                stopwatch += Time.fixedDeltaTime;

                if (!armed && isArmed)
                {
                    OnStickyArmed();
                    armed = true;
                }
                if (!fullyArmed && isFullyArmed)
                {
                    OnStickyFullyArmed();
                    fullyArmed = true;
                }
                if (stickedCharacter) OnStickedCharacterFixedUpdate(stickedCharacter);
            }
            public virtual void OnStickedCharacterFixedUpdate(CharacterBody characterBody)
            {
            }
            public virtual void OnDisable()
            {
                if (demoComponent) GetOrCreateListOfStickies().Remove(this);
                explosionPosition = transform.position;
                radius = projectileImpactExplosion.blastRadius;
            }
            public virtual void OnDestroy()
            {/*
                Collider[] collidersArray2 = Physics.OverlapSphere(explosionPosition, radius);
                foreach (Collider collider in collidersArray2)
                {
                    MeshFilter meshFilter = collider.GetComponent<MeshFilter>();
                    if (meshFilter)
                    {
                        Mesh mesh = Instantiate(meshFilter.sharedMesh);
                        if (!mesh.isReadable)
                        {
                            mesh = MakeReadableMeshCopy(mesh);
                        }
                        //mesh.UploadMeshData(false);
                        Vector3[] vertices = mesh.vertices;
                        Vector3[] normals = mesh.normals;
                        for (int i = 0; i < vertices.Length; i++)
                        {
                            
                            Vector3 vector3 = collider.transform.TransformPoint(vertices[i]);
                            if (Vector3.Distance(vector3, explosionPosition) < radius)
                            {
                                vertices[i] += (vector3 - (explosionPosition + normals[i])).normalized  * 3f;
                            }
                        }
                        mesh.SetVertices(vertices);
                        mesh.RecalculateBounds();
                        mesh.RecalculateNormals();
                        meshFilter.sharedMesh = mesh;
                        if (collider is MeshCollider)
                        {
                            (collider as MeshCollider).sharedMesh = mesh;
                        }
                        //mesh.UploadMeshData(true);
                    }
                }*/
                if (stickedStickies && stickedStickies.stickyComponents.Contains(this)) stickedStickies.stickyComponents.Remove(this);
            }
            public virtual void Unstick()
            {
                if (!sticked) return;
                sticked = false;
                rigidbody.constraints = RigidbodyConstraints.None;
                rigidbody.velocity = Vector3.zero;
                rigidbody.useGravity = true;
                transform.SetParent(null, true);
                if (stickedStickies && stickedStickies.stickyComponents.Contains(this)) stickedStickies.stickyComponents.Remove(this);
            }
            public virtual void OnProjectileImpact(ProjectileImpactInfo impactInfo)
            {
                if (isStickable && !sticked)
                {
                    sticked = true;
                    CharacterBody characterBody = impactInfo.collider.GetComponent<HurtBox>()?.healthComponent?.body;
                    if (characterBody)
                    {
                        stickedCharacter = characterBody;
                        stickedStickies = GetOrAddComponent<StickedStickies>(stickedCharacter.gameObject);
                        if (!stickedStickies.stickyComponents.Contains(this)) stickedStickies.stickyComponents.Add(this);
                    }
                    rigidbody.constraints = RigidbodyConstraints.FreezeAll;
                    rigidbody.velocity = Vector3.zero;
                    rigidbody.useGravity = false;
                    transform.SetParent(impactInfo.collider.transform);
                    transform.position += impactInfo.estimatedImpactNormal * 0.01f;
                }
                

            }

            public void OnDeathStart()
            {
                throw new NotImplementedException();
            }
        }
        public class StickedStickies : MonoBehaviour , ILifeBehavior
        {
            public List<StickyComponent> stickyComponents = new List<StickyComponent>();

            public void OnDeathStart()
            {
                while (stickyComponents.Count > 0)
                {
                    for (int i = 0; i < stickyComponents.Count; i++)
                    {
                        stickyComponents[i].Unstick();
                    }
                }

            }
        }
        public static Mesh MakeReadableMeshCopy(Mesh nonReadableMesh)
        {
            Mesh meshCopy = new Mesh();
            meshCopy.indexFormat = nonReadableMesh.indexFormat;

            // Handle vertices
            GraphicsBuffer verticesBuffer = nonReadableMesh.GetVertexBuffer(0);
            int totalSize = verticesBuffer.stride * verticesBuffer.count;
            byte[] data = new byte[totalSize];
            verticesBuffer.GetData(data);
            meshCopy.SetVertexBufferParams(nonReadableMesh.vertexCount, nonReadableMesh.GetVertexAttributes());
            meshCopy.SetVertexBufferData(data, 0, 0, totalSize);
            verticesBuffer.Release();

            // Handle triangles
            meshCopy.subMeshCount = nonReadableMesh.subMeshCount;
            GraphicsBuffer indexesBuffer = nonReadableMesh.GetIndexBuffer();
            int tot = indexesBuffer.stride * indexesBuffer.count;
            byte[] indexesData = new byte[tot];
            indexesBuffer.GetData(indexesData);
            meshCopy.SetIndexBufferParams(indexesBuffer.count, nonReadableMesh.indexFormat);
            meshCopy.SetIndexBufferData(indexesData, 0, 0, tot);
            indexesBuffer.Release();

            // Restore submesh structure
            uint currentIndexOffset = 0;
            for (int i = 0; i < meshCopy.subMeshCount; i++)
            {
                uint subMeshIndexCount = nonReadableMesh.GetIndexCount(i);
                meshCopy.SetSubMesh(i, new SubMeshDescriptor((int)currentIndexOffset, (int)subMeshIndexCount));
                currentIndexOffset += subMeshIndexCount;
            }

            // Recalculate normals and bounds
            meshCopy.RecalculateNormals();
            meshCopy.RecalculateBounds();

            return meshCopy;
        }
        public class DefaultSticky : StickyComponent
        {
            public override float armTime => 0.7f;

            public override bool isStickable => true;

            public override string stickyName => "Default";

            public override float detonationTime => 0.4f;

            public override int maxStickies => 8;

            public override float fullArmTime => 3f;

            public override float damageIncrease => 1.5f;

            public override float radiusIncrease => 2f;
        }
        public class AntigravSticky : StickyComponent
        {
            public override float armTime => 0.3f;

            public override bool isStickable => false;

            public override string stickyName => "PlasmaTrap";

            public override float detonationTime => 0.2f;

            public override int maxStickies => 4;

            public override float fullArmTime => 3f;

            public override float damageIncrease => 1.5f;

            public override float radiusIncrease => 2f;
        }
        public class Mine : StickyComponent
        {
            public override float armTime => 0.7f;

            public override bool isStickable => true;

            public override string stickyName => "MineTrap";

            public override float detonationTime => 0.4f;

            public override int maxStickies => 16;
            public override float fullArmTime => 5f;

            public override float damageIncrease => 2f;

            public override float radiusIncrease => 2f;

            public override void Start()
            {
                base.Start();
            }
            public override void OnProjectileImpact(ProjectileImpactInfo impactInfo)
            {
                base.OnProjectileImpact(impactInfo);
                transform.rotation = Quaternion.LookRotation(impactInfo.estimatedImpactNormal);
                Vector3 vector3 = transform.rotation.eulerAngles;
                transform.rotation = Quaternion.Euler(new Vector3(vector3.x + 90, vector3.y, vector3.z));
            }
            public override void OnStickyArmed()
            {
                base.OnStickyArmed();
                TeamFilter teamFilter = GetComponent<TeamFilter>();
                SphereCollider detector = transform.GetChild(0).GetComponent<SphereCollider>();
                Collider[] colliders = Physics.OverlapSphere(transform.position, detector.radius, LayerIndex.entityPrecise.mask, QueryTriggerInteraction.UseGlobal);
                foreach (Collider collider in colliders)
                {
                    CharacterBody characterBody = collider.GetComponent<CharacterBody>();
                    if (teamFilter && characterBody && characterBody.teamComponent.teamIndex != teamFilter.teamIndex)
                    {
                        DetonateSticky();
                        break;
                    }
                }
            }
        }
        public class MineDetector : MonoBehaviour
        {
            private TeamFilter teamFilter;
            private StickyComponent stickyComponent;
            public Dictionary<Collider, CharacterBody> keyValuePairs = new Dictionary<Collider, CharacterBody>();
            public bool detected = false;
            private float stopwatch = 0.2f;
            public void Start()
            {
                stickyComponent = transform.parent.GetComponent<StickyComponent>();

                teamFilter = stickyComponent.GetComponent<TeamFilter>();
            }
            public void FixedUpdate()
            {
                if(stopwatch > 0 ) stopwatch -= Time.fixedDeltaTime;
            }
            public void OnTriggerStay(Collider other)
            {
                if (stopwatch > 0) return;
                if (!detected && stickyComponent.isFullyArmed)
                {
                    CharacterBody characterBody = null;
                    if (keyValuePairs.ContainsKey(other))
                    {
                        characterBody = keyValuePairs[other];
                    }
                    else
                    {
                        HurtBox hurtBox = other.GetComponent<HurtBox>();
                        if (hurtBox)
                        {
                            characterBody = hurtBox.healthComponent.body;
                        }
                        keyValuePairs.Add(other, characterBody);
                    }
                    if (!characterBody) return;
                    if (!teamFilter)
                    {
                        if (!stickyComponent)
                        {
                            stickyComponent = transform.parent.GetComponent<StickyComponent>();
                        }
                        teamFilter = stickyComponent ? stickyComponent.GetComponent<TeamFilter>() : null;
                    }
                    if (teamFilter && characterBody && characterBody.teamComponent.teamIndex != teamFilter.teamIndex)
                    {
                        stickyComponent.DetonateSticky();
                        detected = true;
                    }
                }

            }
        }
        public class EnergyBallComponent : MonoBehaviour
        {
            private readonly BullseyeSearch search = new BullseyeSearch();
            private HurtBox trackingTarget;
            private TeamFilter teamFilter;
            private List<HurtBox> hurtBoxes = new List<HurtBox>();
            private Rigidbody rigidbody;
            private void Start()
            {
                rigidbody = GetComponent<Rigidbody>();
                teamFilter = GetComponent<TeamFilter>();
            }
            private void SearchForTarget(Ray aimRay)
            {
                this.search.teamMaskFilter = TeamMask.all;
                this.search.teamMaskFilter.RemoveTeam(teamFilter.teamIndex);
                this.search.filterByLoS = true;
                this.search.searchOrigin = aimRay.origin;
                this.search.searchDirection = aimRay.direction;
                this.search.sortMode = BullseyeSearch.SortMode.Distance;
                this.search.maxDistanceFilter = 32f;
                this.search.maxAngleFilter = 360f;
                this.search.RefreshCandidates();
                this.search.FilterOutGameObject(base.gameObject);
                trackingTarget = null;
                if (search.GetResults().Count() <= 0) hurtBoxes.Clear();
                foreach (HurtBox hurtBox in search.GetResults())
                {
                    if (hurtBoxes.Contains(hurtBox))
                    {
                    }
                    else
                    {
                        trackingTarget = hurtBox;
                        break;
                    }

                }
                //this.trackingTarget = this.search.GetResults().FirstOrDefault<HurtBox>();
                hurtBoxes.Add(trackingTarget);
            }
            public void OnProjectileImpact(ProjectileImpactInfo impactInfo)
            {
                Collider collider = impactInfo.collider;
                HurtBox component = collider.GetComponent<HurtBox>();
                if (component)
                {
                    Ray ray = new Ray
                    {
                        origin = transform.position,
                        direction = transform.forward,
                    };
                    SearchForTarget(ray);
                    if (trackingTarget)
                    {
                        rigidbody.velocity = Vector3.RotateTowards(rigidbody.velocity, trackingTarget.transform.position - transform.position, 360f, 0f);
                    }
                }
            }
        }
        public class RotateToVelocity : MonoBehaviour
        {
            private ProjectileController controller;
            private Rigidbody rigidbody;
            private Transform ghost;
            private void Start()
            {
                controller = GetComponent<ProjectileController>();
                rigidbody = GetComponent<Rigidbody>();
                ghost = controller.ghost.transform.GetChild(0);
            }
            private void LateUpdate()
            {
                if (rigidbody.velocity != Vector3.zero)
                    ghost.rotation = Quaternion.LookRotation(rigidbody.velocity.normalized);
            }
        }
        public class DontRotate : MonoBehaviour
        {
            public void LateUpdate()
            {
                transform.rotation = Quaternion.identity;
            }
        }

    }

    public class Skills
    {
        public static GameObject explosionVFX = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Toolbot/OmniExplosionVFXToolbotQuick.prefab").WaitForCompletion();
        public static GameObject groundSlamVFX = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Beetle/BeetleGuardGroundSlam.prefab").WaitForCompletion();
        public static SkillFamily demoPrimaryFamily;
        public static SkillFamily demoSecondaryFamily;
        public static SkillFamily demoUtilityFamily;
        public static SkillFamily demoSpecialFamily;
        public static SkillFamily demoStickyFamily;
        public static SkillFamily demoDetonateFamily;
        public static SkillFamily demoPassiveFamily;
        public static DemoSwordClass DefaultSword;
        public static ModelPart DefaultSwordSkin;
        public static SkillDef SkullcutterSkillDef;
        public static ModelPart SkullcutterSwordSkin;
        public static ModelPart SkullcutterNuclearSwordSkin;
        public static DemoSwordClass Skullcutter;
        public static SkillDef ZatoichiSkillDef;
        public static ModelPart ZatoichiSwordSkin;
        public static DemoSwordClass Zatoichi;
        public static SkillDef CaberSkillDef;
        public static ModelPart CaberSwordSkin;
        public static DemoSwordClass Caber;
        public static SkillDef DeflectorSkillDef;
        public static DemoSwordClass Deflector;
        public static SkillDef EyelanderSkillDef;
        public static ModelPart EyelanderSwordSkin;
        public static DemoSwordClass Eyelander;
        public static SkillDef PillLauncherSkillDef;
        public static SkillDef RocketLauncherSkillDef;
        public static SkillDef HookLauncherSkillDef;
        public static SkillDef NukeLauncherSkillDef;
        public static SkillDef BombLauncherSkillDef;
        public static SkillDef ToolgunSkillDef;
        public static SkillDef StickyLauncherSkillDef;
        public static DemoStickyClass StickyLauncherObject;
        public static SkillDef JumperLauncherSkillDef;
        public static DemoStickyClass JumperLauncherObject;
        public static SkillDef LaserTrapLauncherSkillDef;
        public static DemoStickyClass LaserTrapLauncherObject;
        public static SkillDef MineLayerSkillDef;
        public static DemoStickyClass MineLayerObject;
        public static SkillDef AntigravLauncherSkillDef;
        public static DemoStickyClass AntigravLauncherObject;
        public static SkillDef DetonateSkillDef;
        public static SkillDef LaserTrapDetonateSkillDef;
        public static SkillDef SpecialOneSkillDef;
        public static SkillDef AltSpecialOneSkillDef;
        public static SkillDef SpecialTwoSkillDef;
        public static SkillDef AltSpecialTwoSkillDef;
        public static SkillDef SlamSkillDef;
        public static SkillDef BigAssSwordSkillDef;
        public static SkillDef LockInSkillDef;
        public static SkillDef SwapSkillDef;
        public static SkillDef ManthreadsSkillDef;
        public static SkillDef HeavyShieldSkillDef;
        public static ModelPart HeavyShieldSkin;
        public static ModelPart HeavyShieldNuclearSkin;
        public static DemoShieldClass HeavyShield;
        public static SkillDef LightShieldSkillDef;
        public static ModelPart LightShieldSkin;
        public static ModelPart LightShieldNuclearSkin;
        public static DemoShieldClass LightShield;
        public static SkillDef SpaceShieldSkillDef;
        public static List<SkillDef> stickySkills = new List<SkillDef>();
        public static Dictionary<SkillDef, SkillDef> customDetonationSkills = new Dictionary<SkillDef, SkillDef>();
        public static Dictionary<SkillDef, SkillDef> altSpecialSkills = new Dictionary<SkillDef, SkillDef>();
        public static Dictionary<SkillDef, DemoSwordClass> swordDictionary = new Dictionary<SkillDef, DemoSwordClass>();
        public static Dictionary<SkillDef, DemoShieldClass> shieldDictionary = new Dictionary<SkillDef, DemoShieldClass>();
        public static Dictionary<SkillDef, DemoStickyClass> bombProjectiles = new Dictionary<SkillDef, DemoStickyClass>();
        public static Dictionary<SkillDef, Action<CharacterBody, RecalculateStatsAPI.StatHookEventArgs>> skillsToStats = new Dictionary<SkillDef, Action<CharacterBody, RecalculateStatsAPI.StatHookEventArgs>>();
        
        public const float generalSwordRadius = 1.5f;
        public const float generalSwordRange = 6f;
        public const float generalSwordDamage = 5f;
        //public delegate void SwordOnHitEffect(ref BulletAttack bullet, ref BulletAttack.BulletHit hitInfo, List<HurtBox> hurtboxes = null, OverlapAttack overlapAttack = null);

        public static void Init()
        {
            //InitSwordAttacks();
            demoPrimaryFamily = assetsDictionary["DemoPrimary"] as SkillFamily;
            demoSecondaryFamily = assetsDictionary["DemoSecondary"] as SkillFamily;
            demoUtilityFamily = assetsDictionary["DemoUtility"] as SkillFamily;
            demoSpecialFamily = assetsDictionary["DemoSpecial"] as SkillFamily;
            demoStickyFamily = assetsDictionary["DemoStickies"] as SkillFamily;
            demoDetonateFamily = assetsDictionary["DemoDetonate"] as SkillFamily;
            demoPassiveFamily = assetsDictionary["DemoPassive"] as SkillFamily;
            BulletAttack DefaultBulletAttack = new BulletAttack()
            {
                radius = generalSwordRadius,
                damage = generalSwordDamage,
                bulletCount = 1,
                maxDistance = generalSwordRange,
                allowTrajectoryAimAssist = true,
                hitMask = LayerIndex.entityPrecise.mask,
                procCoefficient = 1f,
                stopperMask = LayerIndex.world.mask,
                force = 3f,
                hitCallback = DefaultOnHit,

            };
            //DefaultSwordSkin = new DemoSkinClass(DemoDefaultSkin, null);
            DefaultSword = new DemoSwordClass(DefaultBulletAttack);
            bool DefaultOnHit(BulletAttack bulletAttack, ref BulletAttack.BulletHit hitInfo)
            {
                return BulletAttack.DefaultHitCallbackImplementation(bulletAttack, ref hitInfo);
            }
            BulletAttack SkullcutterBulletAttack = new BulletAttack()
            {
                radius = generalSwordRadius,
                damage = generalSwordDamage / 1.25f,
                bulletCount = 1,
                maxDistance = generalSwordRange * 1.25f,
                allowTrajectoryAimAssist = true,
                hitMask = LayerIndex.entityPrecise.mask,
                procCoefficient = 1f,
                stopperMask = LayerIndex.world.mask,
                force = 3f,
                hitCallback = SkullcutterOnHit

            };
            Skullcutter = new DemoSwordClass(SkullcutterBulletAttack);
            bool SkullcutterOnHit(BulletAttack bulletAttack, ref BulletAttack.BulletHit hitInfo)
            {
                if (hitInfo.hitHurtBox)
                {
                    CharacterBody ownerBody = bulletAttack.owner.GetComponent<CharacterBody>();
                    if (!bulletAttack.isCrit)
                    {
                        bulletAttack.isCrit = Util.CheckRoll(ownerBody.crit + ownerBody.GetBuffCount(ExtraCritChance) * 10, ownerBody.master);
                    }
                    if (!bulletAttack.isCrit)
                    {
                        ownerBody.AddBuff(Main.ExtraCritChance);
                    }
                    else
                    {
                        ownerBody.SetBuffCount(ExtraCritChance.buffIndex, 0);
                    }
                    CharacterBody body = hitInfo.hitHurtBox.healthComponent.body;
                    BulletAttack bulletAttack2 = new BulletAttack()
                    {
                        radius = bulletAttack.radius,
                        aimVector = bulletAttack.aimVector,
                        damage = body ? (body.GetBuffCount(SkullcutterDamageIncrease) < 1 ? bulletAttack.damage * 2.4f : bulletAttack.damage) : bulletAttack.damage,
                        bulletCount = bulletAttack.bulletCount,
                        spreadPitchScale = bulletAttack.spreadPitchScale,
                        spreadYawScale = bulletAttack.spreadYawScale,
                        maxSpread = bulletAttack.maxSpread,
                        minSpread = bulletAttack.minSpread,
                        maxDistance = bulletAttack.maxDistance,
                        allowTrajectoryAimAssist = bulletAttack.allowTrajectoryAimAssist,
                        hitMask = bulletAttack.hitMask,
                        procCoefficient = bulletAttack.procCoefficient,
                        stopperMask = bulletAttack.stopperMask,
                        damageType = bulletAttack.damageType,
                        force = bulletAttack.force,
                        falloffModel = bulletAttack.falloffModel,
                        damageColorIndex = bulletAttack.damageColorIndex,
                        isCrit = bulletAttack.isCrit,
                        origin = bulletAttack.origin,
                        owner = bulletAttack.owner,
                        hitCallback = bulletAttack.hitCallback,

                    };
                    if (body)
                        body.AddBuff(SkullcutterDamageIncrease);
                    return BulletAttack.DefaultHitCallbackImplementation(bulletAttack2, ref hitInfo);

                }
                return BulletAttack.DefaultHitCallbackImplementation(bulletAttack, ref hitInfo);
            }
            objectsActualNames.Add("DemoSkullcutter", SkullcutterName);
            SkullcutterSkillDef = SwordInit(Skullcutter, typeof(DemoSword), ThunderkitAssets.LoadAsset<Sprite>("Assets/Demoman/Skills/DemoSkullcutterSkillIcon.png"), "DemoSkullcutter", "DEMOMAN_SKULLCUTTER_NAME", "DEMOMAN_SKULLCUTTER_DESC", new string[] { "DEMOMAN_SKULLCUTTER_KEY" });
            LanguageAPI.Add("DEMOMAN_SKULLCUTTER_NAME", "Skullcutter");
            LanguageAPI.Add("DEMOMAN_SKULLCUTTER_DESC", "Swing Skullcutter for " + LanguagePrefix((Skullcutter.bulletAttack.damage * 100).ToString() + "% damage", LanguagePrefixEnum.Damage) +". Deal " + LanguagePrefix("240% extra damage", LanguagePrefixEnum.Damage) + " on first hit and increase " + LanguagePrefix("crit chance", LanguagePrefixEnum.Damage) + " by " + LanguagePrefix("10%", LanguagePrefixEnum.Damage) + " on sequential hits.");
            GenerateSwordKeywordToken("DEMOMAN_SKULLCUTTER_KEY", Skullcutter, "Skullcutter.",
                "On hit: Increase next melee attack crit chance by 10%. Resets on crit success\n" +
                "Special: Deals 2.4x more damage to the targets you hit for the first time\n" +
                "Passive: Reduces base movement speed by 2m/s when held");
            BulletAttack ZatoichiBulletAttack = new BulletAttack()
            {
                radius = generalSwordRadius,
                damage = generalSwordDamage,
                bulletCount = 1,
                maxDistance = generalSwordRange,
                allowTrajectoryAimAssist = true,
                hitMask = LayerIndex.entityPrecise.mask,
                procCoefficient = 1f,
                stopperMask = LayerIndex.world.mask,
                force = 3f,
                hitCallback = ZatoichiOnHit

            };
            Zatoichi = new DemoSwordClass(ZatoichiBulletAttack);
            bool ZatoichiOnHit(BulletAttack bulletAttack, ref BulletAttack.BulletHit hitInfo)
            {
                if (hitInfo.hitHurtBox)
                {
                    CharacterBody ownerBody = bulletAttack.owner.GetComponent<CharacterBody>();
                    Main.Overheal(ownerBody.healthComponent, 0.04f * bulletAttack.procCoefficient);
                    ownerBody.AddTimedBuff(Main.HealOnKill, 0.2f);
                    CharacterBody body = hitInfo.hitHurtBox.hurtBoxGroup.transform.GetComponent<CharacterModel>().body;
                    if (body)
                    {
                        body.AddTimedBuff(Main.HealOnKill, 0.2f);
                    }
                }
                return BulletAttack.DefaultHitCallbackImplementation(bulletAttack, ref hitInfo);
            }
            objectsActualNames.Add("DemoZatoichi", ZatoichiName);
            ZatoichiSkillDef = SwordInit(Zatoichi, typeof(DemoSword), ThunderkitAssets.LoadAsset<Sprite>("Assets/Demoman/Skills/DemoZatoichiSkillIcon.png"), "DemoZatoichi", "DEMOMAN_ZATOICHI_NAME", "DEMOMAN_ZATOICHI_DESC", new string[] { "DEMOMAN_ZATOICHI_KEY" });
            LanguageAPI.Add("DEMOMAN_ZATOICHI_NAME", "Zatoichi");
            LanguageAPI.Add("DEMOMAN_ZATOICHI_DESC", "Swing Zatoichi for " + LanguagePrefix((Zatoichi.bulletAttack.damage * 100).ToString() + "% damage", LanguagePrefixEnum.Damage) + ". " + LanguagePrefix("Heal", LanguagePrefixEnum.Healing) + " on hit and kill.");
            GenerateSwordKeywordToken("DEMOMAN_ZATOICHI_KEY", Zatoichi, "Zatoichi.",
                "On hit: " + LanguagePrefix("Heal", LanguagePrefixEnum.Healing) + " for " + LanguagePrefix("4%", LanguagePrefixEnum.Healing) + " of the " + LanguagePrefix("maximum health", LanguagePrefixEnum.Health) + "\n" +
                "On kill: " + LanguagePrefix("Heal", LanguagePrefixEnum.Healing) + " for " + LanguagePrefix("15%", LanguagePrefixEnum.Healing) + " of the " + LanguagePrefix("maximum health", LanguagePrefixEnum.Health));
            BulletAttack CaberBulletAttack = new BulletAttack()
            {
                radius = generalSwordRadius / 2f,
                damage = generalSwordDamage / 2f,
                bulletCount = 1,
                maxDistance = generalSwordRange,
                allowTrajectoryAimAssist = true,
                hitMask = LayerIndex.entityPrecise.mask + LayerIndex.world.mask,
                procCoefficient = 1f,
                stopperMask = LayerIndex.entityPrecise.mask + LayerIndex.world.mask,
                force = 3f,
                hitCallback = CaberOnHit

            };
            Caber = new DemoSwordClass(CaberBulletAttack, 0.1f, 0.2f);
            bool CaberOnHit(BulletAttack bulletAttack, ref BulletAttack.BulletHit hitInfo)
            {
                CharacterBody ownerBody = bulletAttack.owner.GetComponent<CharacterBody>();


                if (ownerBody.skillLocator && ownerBody.skillLocator && Check())
                {
                    ownerBody.skillLocator.primary.DeductStock(1);
                    if (ownerBody.characterMotor) ownerBody.characterMotor.velocity += new Vector3(0f, 20f, 0f);
                    BlastAttack caberExplosion = new BlastAttack
                    {
                        attacker = bulletAttack.owner,
                        attackerFiltering = AttackerFiltering.Default,
                        baseDamage = bulletAttack.damage * 4f,
                        baseForce = 3f,
                        bonusForce = Vector3.up,
                        canRejectForce = true,
                        crit = bulletAttack.isCrit,
                        damageColorIndex = DamageColorIndex.Default,
                        teamIndex = ownerBody.teamComponent.teamIndex,
                        damageType = bulletAttack.damageType,
                        falloffModel = BlastAttack.FalloffModel.SweetSpot,
                        impactEffect = default,
                        radius = 5f,
                        position = hitInfo.point + hitInfo.surfaceNormal * 0.01f,
                        inflictor = bulletAttack.owner,
                        losType = BlastAttack.LoSType.None,
                        procChainMask = default,
                        procCoefficient = bulletAttack.procCoefficient,
                    };
                    BlastAttack.Result result = caberExplosion.Fire();
                    EffectData effectData = new EffectData
                    {
                        scale = caberExplosion.radius,
                        rotation = Quaternion.identity,
                        origin = hitInfo.point

                    };
                    EffectManager.SpawnEffect(explosionVFX, effectData, true);
                }


                return BulletAttack.DefaultHitCallbackImplementation(bulletAttack, ref hitInfo);
                bool Check()
                {
                    bool check = false;
                    if (ownerBody.skillLocator && ownerBody.skillLocator.primary && ownerBody.skillLocator.primary.stock > 0)
                    {
                        check = true;
                    }
                    return check;
                }
            }
            objectsActualNames.Add("DemoCaber", CaberName);
            CaberSkillDef = SwordInit(Caber, typeof(DemoSword), ThunderkitAssets.LoadAsset<Sprite>("Assets/Demoman/Skills/DemoCaberSkillIcon.png"), "DemoCaber", "DEMOMAN_CABER_NAME", "DEMOMAN_CABER_DESC", new string[] { "DEMOMAN_CABER_KEY" }, requiredStock: 0, rechargeInterval: 6f, stockToConsume: 0, baseStock: 1); ;
            LanguageAPI.Add("DEMOMAN_CABER_NAME", "Caber");
            LanguageAPI.Add("DEMOMAN_CABER_DESC", "Swing Caber for " + LanguagePrefix((Caber.bulletAttack.damage * 100).ToString() + "% damage", LanguagePrefixEnum.Damage) + ". Produce a strong " + LanguagePrefix("explosion", LanguagePrefixEnum.Damage) + " on hit.");
            GenerateSwordKeywordToken("DEMOMAN_CABER_KEY", Caber, "Caber.", "On Hit: Make an explosion for " + LanguagePrefix("1000% damage", LanguagePrefixEnum.Damage) + " in " + LanguagePrefix("3 meters radius", LanguagePrefixEnum.Damage) + ". Reloads 6 seconds");
            BulletAttack EyelanderBulletAttack = new BulletAttack()
            {
                radius = generalSwordRadius,
                damage = generalSwordDamage,
                bulletCount = 1,
                maxDistance = generalSwordRange,
                allowTrajectoryAimAssist = true,
                hitMask = LayerIndex.entityPrecise.mask,
                procCoefficient = 1f,
                stopperMask = LayerIndex.world.mask,
                force = 3f,
                hitCallback = EyelanderOnHit,

            };
            Eyelander = new DemoSwordClass(EyelanderBulletAttack);
            bool EyelanderOnHit(BulletAttack bulletAttack, ref BulletAttack.BulletHit hitInfo)
            {
                if (hitInfo.hitHurtBox)
                {
                    CharacterBody body = hitInfo.hitHurtBox.healthComponent.body;
                    if (body)
                    {
                        body.AddTimedBuff(Main.UpgradeOnKill, 10f);
                    }
                }
                return BulletAttack.DefaultHitCallbackImplementation(bulletAttack, ref hitInfo);
            }
            objectsActualNames.Add("DemoEyelander", EyelanderName);
            EyelanderSkillDef = SwordInit(Eyelander, typeof(DemoSword), ThunderkitAssets.LoadAsset<Sprite>("Assets/Demoman/Skills/DemoEyelanderSkillIcon.png"), "DemoEyelander", "DEMOMAN_EYELANDER_NAME", "DEMOMAN_EYELANDER_DESC", new string[] { "DEMOMAN_EYELANDER_KEY" });
            LanguageAPI.Add("DEMOMAN_EYELANDER_NAME", "Eyelander");
            LanguageAPI.Add("DEMOMAN_EYELANDER_DESC", "Swing Eyelander for " + LanguagePrefix((Eyelander.bulletAttack.damage * 100).ToString() + "% damage", LanguagePrefixEnum.Damage) + ". " + LanguagePrefix("Trade", LanguagePrefixEnum.Death) + " " + LanguagePrefix("health", LanguagePrefixEnum.Health) + " and " + LanguagePrefix("damage", LanguagePrefixEnum.Damage) + " for " + LanguagePrefix("boss items gerenarion", LanguagePrefixEnum.Shrine) + ".");
            GenerateSwordKeywordToken("DEMOMAN_EYELANDER_KEY", Eyelander, "Eyelabder.", "On chanpion hit: Mark the target for 10 seconds. If it dies within 10 seconds generate boss reward.\nPassive: " + LanguagePrefix("Reduces", LanguagePrefixEnum.Death) + " base " + LanguagePrefix("damage", LanguagePrefixEnum.Damage) + " and " + LanguagePrefix("health", LanguagePrefixEnum.Health) + " by " + LanguagePrefix("25%", LanguagePrefixEnum.Death) + ". " + LanguagePrefix("Reduces", LanguagePrefixEnum.Death) + " " + LanguagePrefix("movement speed", LanguagePrefixEnum.Utility) + " by 1m/s.");

            LanguageAPI.Add("DEMOMAN_DEFLECTOR_NAME", "Deflector");
            LanguageAPI.Add("DEMOMAN_DEFLECTOR_DESC", $"Feedbacker.");
            objectsActualNames.Add("DemoPillLauncher", PillLauncherName);
            PillLauncherSkillDef = GrenadeLauncherInit(typeof(PillLauncher), ThunderkitAssets.LoadAsset<Sprite>("Assets/Demoman/Skills/DemoPillSkillIcon.png"), "DemoPillLauncher", "DEMOMAN_PILLLAUNCHER_NAME", "DEMOMAN_PILLLAUNCHER_DESC", new string[] { "DEMOMAN_PILLLAUNCHER_KEY" }, false, 4, 3f, 1, 6, 1);
            objectsActualNames.Add("DemoRocketLauncher", RocketLauncherName);
            RocketLauncherSkillDef = GrenadeLauncherInit(typeof(RocketLauncher), ThunderkitAssets.LoadAsset<Sprite>("Assets/Demoman/Skills/DemoRocketSkillIcon.png"), "DemoRocketLauncher", "DEMOMAN_ROCKETLAUNCHER_NAME", "DEMOMAN_ROCKETLAUNCHER_DESC", new string[] { "DEMOMAN_ROCKETLAUNCHER_KEY" }, false, 3, 3f, 1, 4, 1);
            objectsActualNames.Add("DemoBombLauncher", BombLauncherName);
            BombLauncherSkillDef = GrenadeLauncherInit(typeof(BombLauncher), ThunderkitAssets.LoadAsset<Sprite>("Assets/Demoman/Skills/DemoBombSkillIcon.png"), "DemoBombLauncher", "DEMOMAN_BOMBLAUNCHER_NAME", "DEMOMAN_BOMBLAUNCHER_DESC", new string[] { "DEMOMAN_BOMBLAUNCHER_KEY" }, false, 4, 3f, 1, 6, 1);
            BombProjectile.GetComponent<BombComponent>().seekerState = BombLauncherSkillDef.activationState;
            objectsActualNames.Add("DemoStickyLauncher", StickyLauncherName);
            StickyLauncherSkillDef = GrenadeLauncherInit(typeof(StickyLauncher), ThunderkitAssets.LoadAsset<Sprite>("Assets/Demoman/Skills/DemoStickySkillIcon.png"), "DemoStickyLauncher", "DEMOMAN_STICKYLAUNCHER_NAME", "DEMOMAN_STICKYLAUNCHER_DESC", new string[] { "DEMOMAN_STICKYLAUNCHER_KEY" }, true, 8, 1.1f, 1);
            objectsActualNames.Add("DemoJumperLauncher", JumperLauncherName);
            JumperLauncherSkillDef = GrenadeLauncherInit(typeof(JumperLauncher), ThunderkitAssets.LoadAsset<Sprite>("Assets/Demoman/Skills/DemoJumperStickySkillIcon.png"), "DemoJumperLauncher", "DEMOMAN_JUMPERLAUNCHER_NAME", "DEMOMAN_JUMPERLAUNCHER_DESC", new string[] { "DEMOMAN_JUMPERLAUNCHER_KEY" }, true, 8, 1.1f, 1);
            objectsActualNames.Add("DemoHookLauncher", HookLauncherName);
            HookLauncherSkillDef = GrenadeLauncherInit(typeof(HookLauncher), ThunderkitAssets.LoadAsset<Sprite>("Assets/Demoman/Skills/DemoHookSkillIcon.png"), "DemoHookLauncher", "DEMOMAN_HOOKLAUNCHER_NAME", "DEMOMAN_HOOKLAUNCHER_DESC", new string[] { "DEMOMAN_HOOKLAUNCHER_KEY" }, false, 1, 4f, 1, 1, 1);
            HookProjectile.GetComponent<HookComponent>().seekerState = HookLauncherSkillDef.activationState;
            objectsActualNames.Add("DemoNukeLauncher", NukeLauncherName);
            //NukeLauncherSkillDef = GrenadeLauncherInit(typeof(NukeLauncher), ThunderkitAssets.LoadAsset<Sprite>("Assets/Demoman/Skills/DemoNukeSkillIcon.png"), "DemoNukeLauncher", "DEMOMAN_ROCKETLAUNCHER_NAME", "DEMOMAN_ROCKETLAUNCHER_DESC", false, 1, 60, 1, 1, 1);

            //QuickiebombLauncherSkillDef = GrenadeLauncherInit(typeof(QuickiebombLauncher), "DEMOMAN_QUICKIEBOMBLAUNCHER_NAME", "DEMOMAN_QUICKIEBOMBLAUNCHER_DESC", true, 4, 1, 1);
            objectsActualNames.Add("DemoMineLayer", MineLayerName);
            MineLayerSkillDef = GrenadeLauncherInit(typeof(MineLayer), ThunderkitAssets.LoadAsset<Sprite>("Assets/Demoman/Skills/DemoMineSkillIcon.png"), "DemoMineLayer", "DEMOMAN_MINELAYER_NAME", "DEMOMAN_MINELAYER_DESC", new string[] { "DEMOMAN_MINELAYER_KEY" }, true, 8, 1, 1);
            //ToolgunSkillDef = GrenadeLauncherInit(typeof(ToolGun), null, "DemoToolgun", "DEMOMAN_TOOLGUN_NAME", "DEMOMAN_TOOLGUN_DESC", new string[] { "DEMOMAN_TOOLGUN_KEY" }, false, 1, 0f, 1, 1, 0);
            LanguageAPI.Add("DEMOMAN_MINELAYER_NAME", "Mine Layer");
            GrenadeLauncher mineDesc = new MineLayer();
            LanguageAPI.Add("DEMOMAN_MINELAYER_DESC", "Fires a sticky trap with remote and proximity detonation that sticks on impact for " + LanguagePrefix((mineDesc.damage * 100).ToString() + "%", LanguagePrefixEnum.Damage) + " damage.");
            GenerateGrenadeKeywordToken(MineLayerName, "DEMOMAN_MINELAYER_KEY", MineProjectile, MineLayerSkillDef, mineDesc, "Mines detonate automatically on enemy contact.");
            objectsActualNames.Add("DemoAntigravityBombLauncher", AntigravLauncherName);
            AntigravLauncherSkillDef = GrenadeLauncherInit(typeof(AntigravLauncher), ThunderkitAssets.LoadAsset<Sprite>("Assets/Demoman/Skills/DemoAntigravBombSkillIcon.png"), "DemoAntigravityBombLauncher", "DEMOMAN_QUICKIEBOMBLAUNCHER_NAME", "DEMOMAN_QUICKIEBOMBLAUNCHER_DESC", new string[] { "DEMOMAN_QUICKIEBOMBLAUNCHER_KEY" }, true, 4, 1.1f, 1);
            LanguageAPI.Add("DEMOMAN_PILLLAUNCHER_NAME", "Grenade Launcher");
            mineDesc = new PillLauncher();
            LanguageAPI.Add("DEMOMAN_PILLLAUNCHER_DESC", "Fires a rolling projectile for " + LanguagePrefix((mineDesc.damage * 100).ToString() + "%", LanguagePrefixEnum.Damage) + " damage.");
            GenerateGrenadeKeywordToken(PillLauncherName, "DEMOMAN_PILLLAUNCHER_KEY", PillProjectile, PillLauncherSkillDef, mineDesc, "Grenade Laucnher.");
            LanguageAPI.Add("DEMOMAN_ROCKETLAUNCHER_NAME", "Rocket Launcher");
            LanguageAPI.Add("DEMOMAN_ROCKETLAUNCHER_DESC", "Fires a explosive projectile for " + LanguagePrefix((mineDesc.damage * 100).ToString() + "%", LanguagePrefixEnum.Damage) + " damage.");
            mineDesc = new RocketLauncher();
            GenerateGrenadeKeywordToken(RocketLauncherName,"DEMOMAN_ROCKETLAUNCHER_KEY", RocketProjectile, RocketLauncherSkillDef, mineDesc, "Rocket Launcher.");
            LanguageAPI.Add("DEMOMAN_HOOKLAUNCHER_NAME", "Hook Launcher");
            LanguageAPI.Add("DEMOMAN_HOOKLAUNCHER_DESC", "Fires a fast projectile that sticks on impact and pulls user to itself.");
            GenerateGrenadeKeywordToken(HookLauncherName,"DEMOMAN_HOOKLAUNCHER_KEY", HookProjectile, HookLauncherSkillDef, mineDesc, "Hook Launcher.");
            //LanguageAPI.Add("DEMOMAN_ROCKETLAUNCHER_DESC", $"Fire your head cannon.");
            LanguageAPI.Add("DEMOMAN_BOMBLAUNCHER_NAME", "Bomb Cannon");
            mineDesc = new BombLauncher();
            LanguageAPI.Add("DEMOMAN_BOMBLAUNCHER_DESC", "Fires a bomb for " + LanguagePrefix((mineDesc.damage * 100).ToString() + "%", LanguagePrefixEnum.Damage) + " damage that deals additional contact damage. Lifetime can be reduced by charging");
            GenerateGrenadeKeywordToken(BombLauncherName, "DEMOMAN_BOMBLAUNCHER_KEY", BombProjectile, BombLauncherSkillDef, mineDesc, "Bomb Launcher.");
            //LanguageAPI.Add("DEMOMAN_BOMBLAUNCHER_DESC", $"Fire your head cannon.");
            LanguageAPI.Add("DEMOMAN_STICKYLAUNCHER_NAME", "Sticky Launcher");
            mineDesc = new StickyLauncher();
            LanguageAPI.Add("DEMOMAN_STICKYLAUNCHER_DESC", "Fires a sticky trap with remote detonation that sticks on impact for " + LanguagePrefix((mineDesc.damage * 100).ToString() + "%", LanguagePrefixEnum.Damage) + " damage.");
            GenerateGrenadeKeywordToken(StickyLauncherName, "DEMOMAN_STICKYLAUNCHER_KEY", StickyProjectile, StickyLauncherSkillDef, mineDesc, "Sticky Launcher.");
            LanguageAPI.Add("DEMOMAN_JUMPERLAUNCHER_NAME", "Jumper Launcher");
            LanguageAPI.Add("DEMOMAN_JUMPERLAUNCHER_DESC", "Fires a sticky trap with remote detonation that sticks on impact. Trades all damage for additional knockback");
            GenerateGrenadeKeywordToken(JumperLauncherName, "DEMOMAN_JUMPERLAUNCHER_KEY", JumperProjectile, JumperLauncherSkillDef, mineDesc, "Jumper Launcher.");
            //LanguageAPI.Add("DEMOMAN_STICKYLAUNCHER_DESC", $"Fire your sticky launcher.");
            LanguageAPI.Add("DEMOMAN_QUICKIEBOMBLAUNCHER_NAME", "Antigrav Launcher");
            mineDesc = new AntigravLauncher();
            LanguageAPI.Add("DEMOMAN_QUICKIEBOMBLAUNCHER_DESC", "Fires a bouncing trap with remote detonation that bounces on impact for " + LanguagePrefix((mineDesc.damage * 100).ToString() + "%", LanguagePrefixEnum.Damage) + " damage. Has no gravitation");
            GenerateGrenadeKeywordToken(AntigravLauncherName,"DEMOMAN_QUICKIEBOMBLAUNCHER_KEY", AntigravProjectile, AntigravLauncherSkillDef, mineDesc, "Antigrav Launcher.");
            //LanguageAPI.Add("DEMOMAN_QUICKIEBOMBLAUNCHER_DESC", $"Fire your quickiebomb launcher.");
            objectsActualNames.Add("DemoHeavyShield", HeavyShieldName);
            HeavyShieldSkillDef = ShieldChargeInit(typeof(ShieldChargeHeavy), ThunderkitAssets.LoadAsset<Sprite>("Assets/Demoman/Skills/DemoHeavyShieldSkillIcon.png"), "DemoHeavyShield", "DEMOMAN_SHIELDHEAVY_NAME", "DEMOMAN_SHIELDHEAVY_DESC", new string[] { "DEMOMAN_SHIELDHEAVY_KEY" });
            objectsActualNames.Add("DemoLightShield", LightShieldName);
            LightShieldSkillDef = ShieldChargeInit(typeof(ShieldChargeLight), ThunderkitAssets.LoadAsset<Sprite>("Assets/Demoman/Skills/DemoLightShieldSkillIcon.png"), "DemoLightShield", "DEMOMAN_SHIELDLIGHT_NAME", "DEMOMAN_SHIELDLIGHT_DESC", new string[] { "DEMOMAN_SHIELDLIGHT_KEY" });
            //SpaceShield = ShieldChargeInit(typeof(ShieldChargeAntigravity), ThunderkitAssets.LoadAsset<Sprite>("Assets/Demoman/Skills/DemoHookSkillIcon.png"), "DemoFlyingShield", "DEMOMAN_SHIELDANTIGRAVITY_NAME", "DEMOMAN_SHIELDANTIGRAVITY_DESC");
            LanguageAPI.Add("DEMOMAN_SHIELDHEAVY_NAME", "Heavy Shield");
            LanguageAPI.Add("DEMOMAN_SHIELDHEAVY_DESC", "Charges user in a straight line with low control. Greatly increases sword damage based on charge stage.");
            GenerateShieldKeywordToken("DEMOMAN_SHIELDHEAVY_KEY", new ShieldChargeHeavy(), "Heavy Shield.");
            LanguageAPI.Add("DEMOMAN_SHIELDLIGHT_NAME", "Light Shield");
            LanguageAPI.Add("DEMOMAN_SHIELDLIGHT_DESC", "Charges user in a straight line with full control. Slightly increases sword damage based on charge stage.");
            GenerateShieldKeywordToken("DEMOMAN_SHIELDLIGHT_KEY", new ShieldChargeLight(), "Light Shield.");
            LanguageAPI.Add("DEMOMAN_SHIELDANTIGRAVITY_NAME", "Antigravity Shield");
            LanguageAPI.Add("DEMOMAN_SHIELDANTIGRAVITY_DESC", $"charge.");
            DetonateSkillDef = DetonateInit();
            //AntigravDetonateSkillDef = AntigravDetonateInit();
            customDetonationSkills.Add(AntigravLauncherSkillDef, LaserTrapDetonateSkillDef);
            SwapSkillDef = Swap(ThunderkitAssets.LoadAsset<Sprite>("Assets/Demoman/Skills/DemoSwapSKillIcon.png"));
            objectsActualNames.Add("DemoWhirlwind", WhirlwindName);
            SpecialOneSkillDef = SpecialInit(typeof(SpecialOneRedirector), ThunderkitAssets.LoadAsset<Sprite>("Assets/Demoman/Skills/DemoSpecial1SKillIcon.png"), "DemoWhirlwind", "DEMOMAN_SPECIALONE_NAME", "DEMOMAN_SPECIALONE_DESC", "Extra");
            objectsActualNames.Add("DemoHitStorm", HitstormName);
            SpecialTwoSkillDef = SpecialInit<UltraInstinctSkillDef>(typeof(UltraInstinctRedirector), ThunderkitAssets.LoadAsset<Sprite>("Assets/Demoman/Skills/DemoSpecial2SKillIcon.png"), "DemoHitStorm", "DEMOMAN_SPECIALTWO_NAME", "DEMOMAN_SPECIALTWO_DESC", "Extra", baseStocks: 3, rechargeInterval: 4f);
            objectsActualNames.Add("DemoSlam", SlamName);
            SlamSkillDef = SpecialInit(typeof(Slam), ThunderkitAssets.LoadAsset<Sprite>("Assets/Demoman/Skills/DemoSlamSkillIcon.png"), "DemoSlam", "DEMOMAN_SLAM_NAME", "DEMOMAN_SLAM_DESC", "Extra", rechargeInterval: 6f);
            objectsActualNames.Add("DemoBigAssAttack", HeavySmashName);
            BigAssSwordSkillDef = SpecialInit(typeof(BigAssAttackRedirector), ThunderkitAssets.LoadAsset<Sprite>("Assets/Demoman/Skills/DemoSlamSkillIcon.png"), "DemoBigAssAttack", "DEMOMAN_BIGASSATTACK_NAME", "DEMOMAN_BIGASSATTACK_DESC", "Extra");
            //LockInSkillDef = SpecialInit(typeof(LockIn), null, "DemoLockIn", "DEMOMAN_LOCKIN_NAME", "DEMOMAN_LOCKIN_DESC", "Extra");
            LanguageAPI.Add("DEMOMAN_SPECIALONE_NAME", "Whirlwind");
            LanguageAPI.Add("DEMOMAN_SPECIALONE_DESC", $"Spin around, hitting all enemies within your range.");
            GenerateSpecialKeyWordToken("DEMOMAN_SPECIALONE_KEY", "" +
                "Melee style." +
                "\n\n" +
                "Hit's all targets with your sword within the sword range every 0.25 second for 4 seconds\n\n" +
                "Ranged style." +
                "\n\n" +
                "Shoots traps upwards every 0.1 second for 1 second");
            //LanguageAPI.Add("DEMOMAN_SPECIALONEALT_NAME", "Trap Barrage");
            //LanguageAPI.Add("DEMOMAN_SPECIALONEALT_DESC", $"Stick.");
            LanguageAPI.Add("DEMOMAN_SPECIALTWO_NAME", "Hit Storm");
            LanguageAPI.Add("DEMOMAN_SPECIALTWO_DESC", $"Hit all enemies within 24 meters radius almost instantly. Has 3 base stocks.");
            LanguageAPI.Add("DEMOMAN_SLAM_NAME", "Slam");
            LanguageAPI.Add("DEMOMAN_SLAM_DESC", $"Quickly slam down. On impact launch all enemies within 12 meters radius up. Pressing Jump buttom after landing will launch you with them. Hitting launched enemies will launch them again and cripple them.");
            LanguageAPI.Add("DEMOMAN_BIGASSATTACK_NAME", "Heavy Smash");
            LanguageAPI.Add("DEMOMAN_BIGASSATTACK_DESC", $"Hold Special button to charge your weapon and deal massive damage on full charge.");
            //LanguageAPI.Add("DEMOMAN_SPECIALTWOALT_NAME", "Trap Storm");
            //LanguageAPI.Add("DEMOMAN_SPECIALTWOALT_DESC", $"Stick.");
            ManthreadsSkillDef = PassiveInit(ThunderkitAssets.LoadAsset<Sprite>("Assets/Demoman/Skills/DemoMannthreadsSkillIcon.png"), "DemoStompPassive", "DEMOMAN_STOMPSKILL_NAME", "DEMOMAN_STOMPSKILL_DESC");
            LanguageAPI.Add("DEMOMAN_STOMPSKILL_NAME", "Hell support");
            LanguageAPI.Add("DEMOMAN_STOMPSKILL_DESC", $"Negate all fall damage.");
            foreach (var variant in demoStickyFamily.variants)
            {
                stickySkills.Add(variant.skillDef);
            }
            InitProjectiles();
            InitWeaponModels();
            InitStates();
        }



        public static void AddSkillToFamily(ref SkillFamily skillFamily, SkillDef skillDef)
        {
            Array.Resize(ref skillFamily.variants, skillFamily.variants.Length + 1);
            skillFamily.variants[skillFamily.variants.Length - 1] = new SkillFamily.Variant
            {
                skillDef = skillDef,
                unlockableName = "",
                viewableNode = new ViewablesCatalog.Node(skillDef.skillNameToken, false, null)
            };
        }
        public static T AddSkill<T>(Type state, string activationState, Sprite sprite, string name, string nameToken, string descToken, string[] keywordTokens, int maxStocks, float rechargeInterval, bool beginSkillCooldownOnSkillEnd, bool canceledFromSprinting, bool cancelSprinting, bool fullRestockOnAssign, InterruptPriority interruptPriority, bool isCombat, bool mustKeyPress, int requiredStock, int rechargeStock, int stockToConsume, SkillFamily skillFamily, bool resetCooldownTimerOnUse = false) where T : SkillDef
        {
            GameObject commandoBodyPrefab = Main.DemoBody;

            SkillDef mySkillDef = ScriptableObject.CreateInstance<T>();
            mySkillDef.activationState = new SerializableEntityStateType(state);
            mySkillDef.activationStateMachineName = activationState;
            mySkillDef.baseMaxStock = maxStocks;
            mySkillDef.baseRechargeInterval = rechargeInterval;
            mySkillDef.beginSkillCooldownOnSkillEnd = beginSkillCooldownOnSkillEnd;
            mySkillDef.canceledFromSprinting = canceledFromSprinting;
            mySkillDef.cancelSprintingOnActivation = cancelSprinting;
            mySkillDef.fullRestockOnAssign = fullRestockOnAssign;
            mySkillDef.interruptPriority = interruptPriority;
            mySkillDef.isCombatSkill = isCombat;
            mySkillDef.mustKeyPress = mustKeyPress;
            mySkillDef.rechargeStock = rechargeStock;
            mySkillDef.requiredStock = requiredStock;
            mySkillDef.stockToConsume = stockToConsume;
            mySkillDef.icon = sprite;
            mySkillDef.skillDescriptionToken = descToken;
            mySkillDef.skillName = nameToken;
            mySkillDef.skillNameToken = nameToken;
            mySkillDef.keywordTokens = keywordTokens;
            mySkillDef.resetCooldownTimerOnUse = resetCooldownTimerOnUse;
            (mySkillDef as ScriptableObject).name = name;
            skills.Add(mySkillDef);
            //ContentAddition.AddSkillDef(mySkillDef);
            //SkillLocator skillLocator = commandoBodyPrefab.GetComponent<SkillLocator>();
            //SkillFamily skillFamily = skillLocator.primary.skillFamily;
            if (skillFamily)
                AddSkillToFamily(ref skillFamily, mySkillDef);
            //swordDictionary.Add(mySkillDef, demoSword);
            return mySkillDef as T;
        }
        public static SkillDef SwordInit(DemoSwordClass demoSword, Type state, Sprite sprite, string name, string nameToken, string descToken, string[] keyWords, int requiredStock = 0, int rechargeStock = 1, int stockToConsume = 0, float rechargeInterval = 0f, int baseStock = 1)
        {
            SkillDef skillDef = SwordInit<SkillDef>(demoSword, state, sprite, name, nameToken, descToken, keyWords, requiredStock, rechargeStock, stockToConsume, rechargeInterval, baseStock);
            return skillDef;
        }
        public static T SwordInit<T>(DemoSwordClass demoSword, Type state, Sprite sprite, string name, string nameToken, string descToken, string[] keyWords, int requiredStock = 0, int rechargeStock = 1, int stockToConsume = 0, float rechargeInterval = 0f, int baseStock = 1) where T : SkillDef
        {
            T skillDef = AddSkill<T>(state, "Weapon", sprite, name, nameToken, descToken, keyWords, baseStock, rechargeInterval, false, false, false, true, InterruptPriority.Any, true, false, requiredStock, rechargeStock, stockToConsume, demoPrimaryFamily);
            swordDictionary.Add(skillDef, demoSword);
            return skillDef;
        }
        public static SkillDef GrenadeLauncherInit(Type state, Sprite sprite, string name, string nameToken, string descToken, string[] keyWordTokens, bool isSticky = false, int baseStocks = 4, float rechargeInterval = 4f, int requiredStock = 1, int rechargeStock = 1, int stockToConsume = 1)
        {
            SkillDef skillDef = GrenadeLauncherInit<SkillDef>(state, sprite, name, nameToken, descToken, keyWordTokens, isSticky, baseStocks, rechargeInterval, requiredStock, rechargeStock, stockToConsume);
            return skillDef;
        }
        public static T GrenadeLauncherInit<T>(Type state, Sprite sprite, string name, string nameToken, string descToken, string[] keyWordTokens, bool isSticky = false, int baseStocks = 4, float rechargeInterval = 4f, int requiredStock = 1, int rechargeStock = 1, int stockToConsume = 1) where T : SkillDef
        {
            T skillDef = AddSkill<T>(state, isSticky ? "Weapon" : "Weapon2", sprite, name, nameToken, descToken, keyWordTokens, baseStocks, rechargeInterval, true, false, false, true, InterruptPriority.Any, true, false, requiredStock, rechargeStock, stockToConsume, isSticky ? demoStickyFamily : demoSecondaryFamily, true);
            CreateConfig<int>(Main.ConfigFile, objectsActualNames[name], StocksName, baseStocks, "", ApplyStocks);
            CreateConfig<float>(Main.ConfigFile, objectsActualNames[name], ReloadTimeName, rechargeInterval, "", ApplyReloadTime);
            CreateConfig<int>(Main.ConfigFile, objectsActualNames[name], StocksToConsume, stockToConsume, "", ApplyStocksToConsume);
            CreateConfig<int>(Main.ConfigFile, objectsActualNames[name], StocksToReloadName, rechargeStock, "", ApplyStocksToReload);
            void ApplyStocks(int id, INetworkConfig networkConfig)
            {
                skillDef.baseMaxStock = (networkConfig as NetworkConfig<int>).Value;
            }
            void ApplyReloadTime(int id, INetworkConfig networkConfig)
            {
                skillDef.baseRechargeInterval = (networkConfig as NetworkConfig<float>).Value;
            }
            void ApplyStocksToConsume(int id, INetworkConfig networkConfig)
            {
                skillDef.stockToConsume = (networkConfig as NetworkConfig<int>).Value;
            }
            void ApplyStocksToReload(int id, INetworkConfig networkConfig)
            {
                skillDef.rechargeStock = (networkConfig as NetworkConfig<int>).Value;
            }
            if (isSticky) Main.StickySkills.Add(skillDef);
            return skillDef;
        }
        public static OnConfigApplied testDelegate = Test;
        public static void Test(int id, INetworkConfig networkConfig)
        {

        }
        public static SkillDef ShieldChargeInit(Type state, Sprite sprite, string name, string nameToken, string descToken, string[] keywords, int baseStocks = 1, float rechargeInterval = 6f, int requiredStock = 1, int rechargeStock = 1, int stockToConsume = 1)
        {
            SkillDef skillDef = ShieldChargeInit<SkillDef>(state, sprite, name, nameToken, descToken, keywords, baseStocks, rechargeInterval, requiredStock, rechargeStock, stockToConsume);
            return skillDef;
        }
        public static T ShieldChargeInit<T>(Type state, Sprite sprite, string name, string nameToken, string descToken, string[] keywords, int baseStocks = 1, float rechargeInterval = 6f, int requiredStock = 1, int rechargeStock = 1, int stockToConsume = 1) where T : SkillDef
        {
            T skillDef = AddSkill<T>(state, "Body", sprite, name, nameToken, descToken, keywords, baseStocks, rechargeInterval, true, false, false, true, InterruptPriority.Any, false, true, requiredStock, rechargeStock, stockToConsume, demoUtilityFamily);
            return skillDef;
        }
        public static SkillDef SpecialInit(Type state, Sprite sprite, string name, string nameToken, string descToken, string stateName, int baseStocks = 1, float rechargeInterval = 12f, int requiredStock = 1, int rechargeStock = 1, int stockToConsume = 1, bool beginSkillCooldownOnSkillEnd = true)
        {
            SkillDef skillDef = SpecialInit<SkillDef>(state, sprite, name, nameToken, descToken, stateName, baseStocks, rechargeInterval, requiredStock, rechargeStock, stockToConsume, beginSkillCooldownOnSkillEnd);
            return skillDef;
        }
        public static T SpecialInit<T>(Type state, Sprite sprite, string name, string nameToken, string descToken, string stateName, int baseStocks = 1, float rechargeInterval = 12f, int requiredStock = 1, int rechargeStock = 1, int stockToConsume = 1, bool beginSkillCooldownOnSkillEnd = true) where T : SkillDef
        {
            T skillDef = AddSkill<T>(state, stateName, sprite, name, nameToken, descToken, null, baseStocks, rechargeInterval, beginSkillCooldownOnSkillEnd, false, false, true, InterruptPriority.Any, true, true, requiredStock, rechargeStock, stockToConsume, demoSpecialFamily);
            return skillDef;
        }
        private static SkillDef DetonateInit()
        {
            //GameObject commandoBodyPrefab = DemomanSurvivor.survivor;
            LanguageAPI.Add("DEMOMAN_DETONATE_NAME", "Detonate");
            LanguageAPI.Add("DEMOMAN_DETONATE_DESC", $"Detonate all placed traps.");
            SkillDef skillDef = AddSkill<SkillDef>(typeof(Detonate), "Detonate", ThunderkitAssets.LoadAsset<Sprite>("Assets/Demoman/Skills/DemoDetonateSkillIcon.png"), "DemoDetonate", "DEMOMAN_DETONATE_NAME", "DEMOMAN_DETONATE_DESC", default, 1, 0, false, false, false, true, InterruptPriority.Any, false, true, 0, 1, 0, demoDetonateFamily);
            return skillDef;
        }
        public static SkillDef PassiveInit(Sprite sprite, string name, string nameToken, string descToken)
        {

            GameObject commandoBodyPrefab = Main.DemoBody;

            SkillDef mySkillDef = ScriptableObject.CreateInstance<SkillDef>();
            mySkillDef.activationState = default;
            mySkillDef.activationStateMachineName = "Body";
            mySkillDef.baseMaxStock = default;
            mySkillDef.baseRechargeInterval = default;
            mySkillDef.beginSkillCooldownOnSkillEnd = default;
            mySkillDef.canceledFromSprinting = default;
            mySkillDef.cancelSprintingOnActivation = default;
            mySkillDef.fullRestockOnAssign = default;
            mySkillDef.interruptPriority = default;
            mySkillDef.isCombatSkill = default;
            mySkillDef.mustKeyPress = default;
            mySkillDef.rechargeStock = default;
            mySkillDef.requiredStock = default;
            mySkillDef.stockToConsume = default;
            mySkillDef.icon = sprite;
            mySkillDef.skillDescriptionToken = descToken;
            mySkillDef.skillName = nameToken;
            mySkillDef.skillNameToken = nameToken;
            (mySkillDef as ScriptableObject).name = name;
            ContentAddition.AddSkillDef(mySkillDef);
            AddSkillToFamily(ref demoPassiveFamily, mySkillDef);
            return mySkillDef;
        }
        private static SkillDef Swap(Sprite sprite)
        {
            GameObject commandoBodyPrefab = DemoBody;
            LanguageAPI.Add("DEMOMAN_SWAP_NAME", "Swap");
            LanguageAPI.Add("DEMOMAN_SWAP_DESC", $"Swap Weapons.");
            SkillDef skillDef = AddSkill<SkillDef>(typeof(ChangeWeapons), "Detonate", sprite, "DemoSwap", "DEMOMAN_SWAP_NAME", "DEMOMAN_SWAP_DESC", default, 1, 0, false, false, false, true, InterruptPriority.Any, false, true, 0, 1, 0, null);
            return skillDef;
        }
        public class UltraInstinctSkillDef : SkillDef
        {
            public override SkillDef.BaseSkillInstanceData OnAssigned([NotNull] GenericSkill skillSlot)
            {
                GameObject trackerGameObject = new GameObject("tracker");
                trackerGameObject.layer = LayerIndex.triggerZone.intVal;
                Transform trackerTransform = trackerGameObject.transform;
                trackerTransform.parent = skillSlot.characterBody.transform;
                trackerTransform.localPosition = Vector3.zeroVector;
                SphereCollider sphereCollider = trackerGameObject.AddComponent<SphereCollider>();
                sphereCollider.isTrigger = true;
                sphereCollider.radius = 24;
                NearbyTargetsTracker nearbyTargetsTracker = trackerGameObject.AddComponent<NearbyTargetsTracker>();
                nearbyTargetsTracker.owner = skillSlot.characterBody;
                return new InstanceData
                {
                    tracker = nearbyTargetsTracker
                };
            }
            public override void OnUnassigned([NotNull] GenericSkill skillSlot)
            {
                base.OnUnassigned(skillSlot);
                Destroy(((UltraInstinctSkillDef.InstanceData)skillSlot.skillInstanceData).tracker.gameObject);
            }
            private static bool HasTarget([NotNull] GenericSkill skillSlot)
            {
                NearbyTargetsTracker huntressTracker = ((UltraInstinctSkillDef.InstanceData)skillSlot.skillInstanceData).tracker;
                return (huntressTracker != null) ? huntressTracker.targets.Count > 0 : false;
            }
            public override bool CanExecute([NotNull] GenericSkill skillSlot)
            {
                return HasTarget(skillSlot) && base.CanExecute(skillSlot);
            }
            public override bool IsReady([NotNull] GenericSkill skillSlot)
            {
                return base.IsReady(skillSlot) && HasTarget(skillSlot);
            }
            protected class InstanceData : SkillDef.BaseSkillInstanceData
            {
                public NearbyTargetsTracker tracker;
            }
        }
        public class NearbyTargetsTracker : MonoBehaviour
        {
            public List<CharacterBody> targets = new List<CharacterBody>();
            public List<Collider> colliders2 = new List<Collider>();
            public CharacterBody owner;
            public void OnTriggerEnter(Collider collider)
            {
                if (colliders2.Contains(collider)) return;
                CharacterBody characterBody = collider.GetComponent<HurtBox>()?.healthComponent?.body;
                if (!characterBody)
                {
                    colliders2.Add(collider);
                }
                if (characterBody && owner && characterBody.teamComponent.teamIndex != owner.teamComponent.teamIndex)
                {
                    if (!targets.Contains(characterBody))
                    {
                        UltraInstinctTarget ultraInstinctTarget = GetOrAddComponent<UltraInstinctTarget>(characterBody.gameObject);
                        ultraInstinctTarget.targets = targets;
                        ultraInstinctTarget.self = characterBody;
                    }
                }
                
                
            }
            public void OnTriggerExit(Collider collider)
            {
                if (colliders2.Contains(collider)) return;
                CharacterBody characterBody = collider.GetComponent<HurtBox>()?.healthComponent?.body;
                if (characterBody)
                {
                    if (targets.Contains(characterBody))
                    {
                        UltraInstinctTarget ultraInstinctTarget = characterBody.gameObject.GetComponent<UltraInstinctTarget>();
                        if (ultraInstinctTarget != null) Destroy(ultraInstinctTarget);
                    }
                }
            }
        }
        public class UltraInstinctTarget : MonoBehaviour
        {
            public CharacterBody self;
            public List<CharacterBody> targets;
            private void AddToList()
            {
                if (self && targets != null && !targets.Contains(self)) targets.Add(self);
            }
            private void RemoveFromList()
            {
                if (self && targets != null && targets.Contains(self)) targets.Remove(self);
            }
            public void OnEnable()
            {
                AddToList();
            }
            public void Start()
            {
                AddToList();
            }
            public void OnDisable()
            {
                RemoveFromList();
            }
            public void Destroy()
            {
                RemoveFromList();
            }
        }
        public class DemoSwordClass
        {
            public DemoSwordClass(BulletAttack inpuBulletAttack, float inputSwingUpTime = 0.4f, float inpuSwingDownTime = 0.4f)
            {
                bulletAttack = inpuBulletAttack;
                swingDownTime = inpuSwingDownTime;
                swingUpTime = inputSwingUpTime;
            }
            public BulletAttack bulletAttack;
            public float swingUpTime;
            public float swingDownTime;
            
        }
        public class DemoShieldClass
        {
        }
        public class DemoStickyClass(GameObject stickyObject, GrenadeLauncher stickyState)
        {
            public GameObject stickyObject = stickyObject;
            public GrenadeLauncher stickyState = stickyState;
        }
        private static void InitStates()
        {
            states.Add(typeof(DemoSword));
            states.Add(typeof(PillLauncher));
            states.Add(typeof(RocketLauncher));
            states.Add(typeof(StickyLauncher));
            states.Add(typeof(JumperLauncher));
            states.Add(typeof(MineLayer));
            states.Add(typeof(AntigravLauncher));
            states.Add(typeof(HookLauncher));
            states.Add(typeof(BombLauncher));
            states.Add(typeof(Detonate));
            states.Add(typeof(SpecialOneStickySpiner));
            states.Add(typeof(SpecialOneSwordSpiner));
            states.Add(typeof(SpecialOneRedirector));
            states.Add(typeof(BigAssSword));
            states.Add(typeof(BigAssSwordFire));
            states.Add(typeof(BigAssSticky));
            states.Add(typeof(BigAssAttackRedirector));
            states.Add(typeof(UltraInstinctRedirector));
            states.Add(typeof(UltraInstinctSticky));
            states.Add(typeof(UltraInstinctSword));
            states.Add(typeof(Slam));
            states.Add(typeof(Slamming));
            states.Add(typeof(ShieldChargeAntigravity));
            states.Add(typeof(ShieldChargeHeavy));
            states.Add(typeof(ShieldChargeLight));
            states.Add(typeof(ChangeWeapons));
        }
        public static void GenerateSwordKeywordToken(string token, DemoSwordClass demoSword, string before = "", string after = "")
        {
            LanguageAPI.Add(token, $"" +
                before +
                "\n\n" +
                "Base damage: " + LanguagePrefix((demoSword.bulletAttack.damage * 100).ToString() + "%", LanguagePrefixEnum.Damage) + "\n" +
                "Swing up time: " + LanguagePrefix(demoSword.swingUpTime.ToString() + "s", LanguagePrefixEnum.Damage) + "\n" +
                "Swing down time: " + LanguagePrefix(demoSword.swingDownTime.ToString() + "s", LanguagePrefixEnum.Damage) + "\n" +
                "Range: " + LanguagePrefix(demoSword.bulletAttack.maxDistance.ToString() + "m", LanguagePrefixEnum.Damage) + "\n" +
                "Piercing: " + LanguagePrefix(demoSword.bulletAttack.stopperMask == (demoSword.bulletAttack.stopperMask | 1 << LayerIndex.entityPrecise.mask) ? "False" : "True", LanguagePrefixEnum.Damage) + "\n" +
                "Radius: " + LanguagePrefix(demoSword.bulletAttack.radius.ToString() + "m", LanguagePrefixEnum.Damage) + "\n" +
                "Proc: " + LanguagePrefix(demoSword.bulletAttack.procCoefficient.ToString(), LanguagePrefixEnum.Damage) + (after != "" ? "\n" : "") + after);
            string descToken = token.Replace("_KEY", "_DESC");
            tokenReplace.Add(descToken, token);

        }
        public static void GenerateGrenadeKeywordToken(string name, string token, GameObject projectile, SkillDef skillDef, GrenadeLauncher grenadeLauncher, string before = "", string after = "")
        {
            ProjectileImpactExplosion projectileImpactExplosion = projectile.GetComponent<ProjectileImpactExplosion>();
            DemoExplosionComponent demoExplosionComponent = projectile.GetComponent<DemoExplosionComponent>();
            StickyComponent stickyComponent = projectile.GetComponent<StickyComponent>();
            ProjectileSimple projectileSimple = projectile.GetComponent<ProjectileSimple>();
            Rigidbody rigidbody = projectile.GetComponent<Rigidbody>();
            string explosionString = "";
            string finalString = "";
            if (projectileSimple)
            {
                string speedKey = GenerateReplacement(name, "Speed");
                explosionString += "" +
                    "Speed: " + LanguagePrefix((speedKey).ToString() + "m/s", LanguagePrefixEnum.Damage) + "\n";
                
                string speedValue = projectileSimple.desiredForwardSpeed.ToString();
                if (tokenModifications.ContainsKey(speedKey))
                {
                    tokenModifications[speedKey] = speedValue;
                }
                else
                {
                    tokenModifications.Add(speedKey, speedValue);
                }
            }
            if (rigidbody)
            {
                explosionString += "" +
                    "Affected by gravity: " + LanguagePrefix(rigidbody.useGravity ? "True" : "False", LanguagePrefixEnum.Damage) + "\n";
            }
            if (projectileImpactExplosion)
            {
                //explosionString += "\n";
                string blastRadiusKey = GenerateReplacement(name, BlastRadiusName);
                explosionString += "" +
                    "Explosion radius: " + LanguagePrefix((blastRadiusKey).ToString() + "m", LanguagePrefixEnum.Damage) + "\n" +
                    "Projectile lifetime: " + LanguagePrefix((projectileImpactExplosion.lifetime).ToString() + (projectileImpactExplosion.lifetime == float.PositiveInfinity ? "" : "s"), LanguagePrefixEnum.Damage) + "\n";
                string speedValue = projectileImpactExplosion.blastRadius.ToString();
                //if (tokenModifications.ContainsKey(blastRadiusKey))
                //{
                //    tokenModifications[blastRadiusKey] = speedValue;
                //}
                //else
                //{
                //    tokenModifications.Add(blastRadiusKey, speedValue);
                //}
            }
            if (stickyComponent)
            {
                explosionString += "" +
                    "Can stick: " + LanguagePrefix(stickyComponent.isStickable ? "True" : "False", LanguagePrefixEnum.Damage) + "\n" +
                    "Arm time: " + LanguagePrefix((stickyComponent.armTime).ToString() + "s", LanguagePrefixEnum.Damage) + "\n" +
                    "Full arm time: " + LanguagePrefix((stickyComponent.fullArmTime).ToString() + "s", LanguagePrefixEnum.Damage) + "\n" +
                    "Damage increase: " + LanguagePrefix((stickyComponent.damageIncrease * 100).ToString() + "%", LanguagePrefixEnum.Damage) + "\n" +
                    "Radius increase: " + LanguagePrefix((stickyComponent.radiusIncrease * 100).ToString() + "%", LanguagePrefixEnum.Damage) + "\n" +
                    "Detonation time: " + LanguagePrefix((stickyComponent.detonationTime).ToString() + "s", LanguagePrefixEnum.Damage) + "\n" +
                    "";
            }
            if (demoExplosionComponent)
            {
                explosionString += "" +
                    "Knockback: " + LanguagePrefix((demoExplosionComponent.enemyPower).ToString(), LanguagePrefixEnum.Damage) + "\n" +
                    "Self knockback: " + LanguagePrefix((demoExplosionComponent.selfPower).ToString(), LanguagePrefixEnum.Damage) + "\n" +
                    "";
            }
            finalString = before +
                "\n\n" +
                "Base damage: " + LanguagePrefix((grenadeLauncher.damage * 100).ToString() + "%", LanguagePrefixEnum.Damage) + "\n" +
                "Fire rate: " + LanguagePrefix((grenadeLauncher.fireRate).ToString() + "s", LanguagePrefixEnum.Damage) + "\n" +
                "Base stocks: " + LanguagePrefix((skillDef.baseMaxStock).ToString(), LanguagePrefixEnum.Damage) + "\n" +
                "Reload time: " + LanguagePrefix((skillDef.baseRechargeInterval).ToString(), LanguagePrefixEnum.Damage) + "\n" +
                "Stocks to reload: " + LanguagePrefix((skillDef.rechargeStock).ToString(), LanguagePrefixEnum.Damage) + "\n" +
                "Charge time: " + LanguagePrefix(grenadeLauncher.canBeCharged ? ((grenadeLauncher.chargeCap).ToString() + "s") : "Can't be charged", LanguagePrefixEnum.Damage) + "\n" +
                explosionString + (after != "" ? "\n" : "") + after;
            LanguageAPI.Add(token, $"" +
                finalString);
            tokensToModify.Add(token);
            string descToken = token.Replace("_KEY", "_DESC");
            if (!tokenReplace.ContainsKey(descToken))
            {
                tokenReplace.Add(descToken, token);
            }
            tokensToModify.Add(descToken);
        }
        public static void GenerateShieldKeywordToken(string token, ShieldCharge shieldCharge, string before = "", string after = "")
        {
            LanguageAPI.Add(token, $"" +
                before +
                "\n\n" +
                "Armour on charge: " + LanguagePrefix((shieldCharge.armor).ToString(), LanguagePrefixEnum.Damage) + "\n" +
                "Charge time: " + LanguagePrefix(shieldCharge.chargeMaxMeter.ToString() + "s", LanguagePrefixEnum.Damage) + "\n" +
                "Charge control: " + LanguagePrefix(shieldCharge.chargeControl.ToString() + "radian/second", LanguagePrefixEnum.Damage) + "\n" +
                "Speed increase: " + LanguagePrefix((shieldCharge.chargeSpeed * 100).ToString() + "%", LanguagePrefixEnum.Damage) + "\n" +
                "Buff on charge: " + LanguagePrefix((shieldCharge.buffOnCharge.name).ToString(), LanguagePrefixEnum.Damage) + "\n" +
                "Stage one percentage time: " + LanguagePrefix((shieldCharge.stageOnePercentage).ToString() + "%", LanguagePrefixEnum.Damage) + "\n" +
                "Stage one buff amount: " + LanguagePrefix((shieldCharge.stageOneBuffs).ToString(), LanguagePrefixEnum.Damage) + "\n" +
                "Stage two percentage time: " + LanguagePrefix((shieldCharge.stageTwoPercentage).ToString() + "%", LanguagePrefixEnum.Damage) + "\n" +
                "Stage two buff amount: " + LanguagePrefix((shieldCharge.stageTwoBuffs).ToString(), LanguagePrefixEnum.Damage) + (after != "" ? "\n" : "") + after);
            string descToken = token.Replace("_KEY", "_DESC");
            tokenReplace.Add(descToken, token);
        }
        public static void GenerateSpecialKeyWordToken(string token, string text)
        {
            LanguageAPI.Add(token, text);
            string descToken = token.Replace("_KEY", "_DESC");
            tokenReplace.Add(descToken, token);
        }
        private static void InitProjectiles()
        {
            StickyLauncherObject = new DemoStickyClass(StickyProjectile, new StickyLauncher());
            bombProjectiles.Add(StickyLauncherSkillDef, StickyLauncherObject);
            MineLayerObject = new DemoStickyClass(MineProjectile, new MineLayer());
            bombProjectiles.Add(MineLayerSkillDef, MineLayerObject);
            AntigravLauncherObject = new DemoStickyClass(AntigravProjectile, new AntigravLauncher());
            bombProjectiles.Add(AntigravLauncherSkillDef, AntigravLauncherObject);
            JumperLauncherObject = new DemoStickyClass(JumperProjectile, new JumperLauncher());
            bombProjectiles.Add(JumperLauncherSkillDef, JumperLauncherObject);
        }
        private static void InitWeaponModels()
        {
            ModelPartInfo modelPartInfo = new ModelPartInfo
            {
                bodyName = "DemolisherBody",
                gameObject = ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/Weapons/Swords/skullcutter.prefab"),
                inputString = "WeaponR",
                skillDef = SkullcutterSkillDef,
                codeAfterApplying = Rotate
            };
            SkullcutterSwordSkin = new ModelPart(modelPartInfo);
            
            modelPartInfo.gameObject = ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/Weapons/Swords/zatoichi.prefab");
            modelPartInfo.skillDef = ZatoichiSkillDef;
            ZatoichiSwordSkin = new ModelPart(modelPartInfo);
            modelPartInfo.gameObject = ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/Weapons/Swords/caber.prefab");
            modelPartInfo.skillDef = CaberSkillDef;
            CaberSwordSkin = new ModelPart(modelPartInfo);
            modelPartInfo.gameObject = ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/Weapons/Swords/eyelander.prefab");
            modelPartInfo.skillDef = EyelanderSkillDef;
            EyelanderSwordSkin = new ModelPart(modelPartInfo);
            modelPartInfo.gameObject = ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/Weapons/Swords/whip.prefab");
            modelPartInfo.skillDef = SkullcutterSkillDef;
            modelPartInfo.modelPartOverride = SkullcutterSwordSkin;
            modelPartInfo.skinDef = DemoNuclearSkin;
            SkullcutterNuclearSwordSkin = new ModelPart(modelPartInfo);
            modelPartInfo.gameObject = ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/Weapons/Shield/HeavyShield.prefab");
            modelPartInfo.skillDef = HeavyShieldSkillDef;
            modelPartInfo.inputString = "Shield";
            modelPartInfo.codeAfterApplying = null;
            modelPartInfo.modelPartOverride = null;
            modelPartInfo.skinDef = null;
            HeavyShieldSkin = new ModelPart(modelPartInfo);
            modelPartInfo.gameObject = ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/Weapons/Shield/LightShield.prefab");
            modelPartInfo.skillDef = LightShieldSkillDef;
            LightShieldSkin = new ModelPart(modelPartInfo);
            modelPartInfo.gameObject = ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/Weapons/Shield/HeavyShieldNuclear.prefab");
            modelPartInfo.skillDef = HeavyShieldSkillDef;
            modelPartInfo.skinDef = DemoNuclearSkin;
            modelPartInfo.modelPartOverride = HeavyShieldSkin;
            HeavyShieldNuclearSkin = new ModelPart(modelPartInfo);
            modelPartInfo.gameObject = ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/Weapons/Shield/LightShieldNuclear.prefab");
            modelPartInfo.skillDef = LightShieldSkillDef;
            modelPartInfo.modelPartOverride = LightShieldSkin;
            LightShieldNuclearSkin = new ModelPart(modelPartInfo);
            void Rotate(GameObject gameObject, ChildLocator childLocator, CharacterModel characterModel, ActivePartsComponent activePartsComponent)
            {
                gameObject.transform.localEulerAngles = new Vector3(0f, 180f, 0f);
            }
        }

        //public abstract class BombProjectileInfo
        //{
        //    public abstract GameObject projectile {  get; }
        //    public abstract float damage { get; }
        //    public abstract DamageType damageType { get; }
        //}
        public class DemoSword : BaseSkillState
        {
            private float stopwatch = 0f;
            private bool fired = true;
            public float range;
            public float radius;
            public bool isCrit;
            public GameObject swingEffect = SwingEffect;
            public BulletAttack bulletAttack;
            public DemoSwordClass swordClass;
            private GameObject swordTrail;
            private GenericSkill utilitySkill;
            public override void OnEnter()
            {
                base.OnEnter();
                if (skillLocator)
                {
                    swordClass = swordDictionary.ContainsKey(skillLocator.primary.skillDef) ? swordDictionary[skillLocator.primary.skillDef] : DefaultSword;
                    //bulletAttack = ModifyAttack(swordClass.bulletAttack);
                    utilitySkill = skillLocator.utility;
                }
                if (stopwatch <= 0)
                {
                    float animationTime = swordClass.swingUpTime / base.attackSpeedStat;
                    PlayAnimation("Gesture, Override", "SwingUp", "Slash.playbackRate", animationTime, animationTime / 2f);
                    base.StartAimMode();
                    stopwatch = animationTime;
                    fired = false;
                }

            }
            public override void FixedUpdate()
            {
                base.FixedUpdate();
                if (stopwatch > 0)
                {
                    stopwatch -= GetDeltaTime();
                }
                if (!fired && stopwatch < 0)
                {
                    bulletAttack = ModifyAttack(swordClass.bulletAttack);
                    //BulletAttack bulletAttack = swordDictionary.ContainsKey(activatorSkillSlot.skillDef) ? ModifyAttack(swordDictionary[activatorSkillSlot.skillDef].bulletAttack) : DefaultSword.bulletAttack;
                    SwingSword(bulletAttack);
                    fired = true;
                    stopwatch = swordClass.swingDownTime / characterBody.attackSpeed;

                }
                else if (isAuthority && stopwatch <= 0)
                {
                    outer.SetNextStateToMain();
                }


            }
            public override void OnExit()
            {
                base.OnExit();
                if (swordTrail) Destroy(swordTrail);

            }
            public virtual BulletAttack ModifyAttack(BulletAttack bulletAttack)
            {
                BulletAttack bulletAttack2 = new BulletAttack()
                {
                    radius = bulletAttack.radius,
                    aimVector = GetAimRay().direction,
                    damage = base.damageStat * bulletAttack.damage,
                    bulletCount = bulletAttack.bulletCount,
                    spreadPitchScale = 0f,
                    spreadYawScale = 0f,
                    maxSpread = 0f,
                    minSpread = 0f,
                    maxDistance = bulletAttack.maxDistance,
                    allowTrajectoryAimAssist = true,
                    hitMask = bulletAttack.hitMask,
                    procCoefficient = bulletAttack.procCoefficient,
                    stopperMask = bulletAttack.stopperMask,
                    damageType = new DamageTypeCombo(DamageType.Generic, DamageTypeExtended.Generic, DamageSource.Primary),
                    force = bulletAttack.force,
                    falloffModel = BulletAttack.FalloffModel.None,
                    damageColorIndex = DamageColorIndex.Default,
                    isCrit = RollCrit(),
                    origin = GetAimRay().origin,
                    owner = gameObject,
                    hitCallback = bulletAttack.hitCallback + ChargeOnHit + effectHitCallback,

                };
                return bulletAttack2;
            }
            public virtual bool ChargeOnHit(BulletAttack bulletAttack, ref BulletAttack.BulletHit hitInfo)
            {
                if (hitInfo.hitHurtBox)
                {
                    if (utilitySkill && utilitySkill.stock < utilitySkill.maxStock)
                    {
                        utilitySkill.rechargeStopwatch += MathF.Min(utilitySkill.finalRechargeInterval - utilitySkill.rechargeStopwatch, 0.25f) * utilitySkill.finalRechargeInterval;
                    }

                }
                return false;
            }
            public virtual void SwingSword(BulletAttack bulletAttack)
            {
                if (NetworkServer.active)
                    bulletAttack.Fire();
                GameObject swingEffectcopy = GameObject.Instantiate(swingEffect);
                swingEffectcopy.transform.position = inputBank ? inputBank.aimOrigin : transform.position;
                swingEffectcopy.transform.rotation = Quaternion.LookRotation(inputBank ? inputBank.aimDirection : transform.forward);
                List<ParticleSystem> particleSystems = new List<ParticleSystem>();
                ParticleSystem particleSystem = swingEffectcopy.transform.Find("mainSwing").GetComponent<ParticleSystem>();
                particleSystems.Add(particleSystem);
                //ParticleSystem particleSystem2 = swingEffectcopy.transform.Find("mainSwing2").GetComponent<ParticleSystem>();
                //particleSystems.Add(particleSystem2);
                foreach (ParticleSystem particle in particleSystems)
                {
                    var vel = particleSystem.velocityOverLifetime;
                    vel.speedModifier = bulletAttack.maxDistance;
                    var size = particleSystem.main.startSizeY;
                    size.constant = bulletAttack.radius;
                    size.constantMax = bulletAttack.radius;
                    size.constantMin = bulletAttack.radius;
                }
                Util.PlaySound(DemoSwordSwingSound.playSoundString, gameObject);
                characterBody.AddSpreadBloom(0.5f);
                float animationTime = swordClass.swingDownTime / characterBody.attackSpeed;
                PlayAnimation("Gesture, Override", "SwingDown1", "Slash.playbackRate", animationTime, animationTime / 2f);


            }
            public override InterruptPriority GetMinimumInterruptPriority()
            {
                return InterruptPriority.Skill;
            }


        }
        public abstract class ShieldCharge : BaseState
        {
            public float chargeMeter = 0f;
            public abstract float chargeMaxMeter { get; }
            public Vector3 chargeVector;
            public int shieldStageCount = 0;
            public abstract float chargeControl { get; }
            public abstract float chargeSpeed { get; }
            public Image chargeMeterHUD;
            public DemoComponent demoComponent;
            public abstract float stageOnePercentage { get; }
            public abstract BuffDef buffOnCharge { get; }
            public abstract int stageOneBuffs { get; }
            public abstract float stageTwoPercentage { get; }
            public abstract int stageTwoBuffs { get; }
            public abstract float armor { get; }
            public float chargePercentage => chargeMeter / chargeMaxMeter;
            public Vector3 previousMoveVector;
            public Vector3 savedMoveVector;
            public virtual void OnEnterGlobal()
            {
                chargeMeter = chargeMaxMeter;
                chargeVector = base.inputBank.aimDirection;
                chargeVector.y = 0f;
                chargeVector = chargeVector.normalized;
                base.characterBody.isSprinting = true;
                demoComponent = characterBody.GetComponent<DemoComponent>();
                characterBody.armor += armor;
                if (NetworkServer.active)
                {
                    Util.CleanseBody(characterBody, true, false, false, true, true, false);
                }

                characterBody.RecalculateStats();
                previousMoveVector = transform.position;
                Util.PlaySound(DemoChargeWindUpSound.playSoundString, gameObject);
            }
            public virtual void OnEnterAuthority()
            {
                chargeMeterHUD = demoComponent ? demoComponent.leftMeter : null;
                if (chargeMeterHUD)
                {
                    demoComponent.updateMeter = false;
                    chargeMeterHUD.color = Color.green;
                }
            }
            public override void OnEnter()
            {
                base.OnEnter();
                OnEnterGlobal();
                if (isAuthority) OnEnterAuthority();
            }
            public virtual void FixedUpdateGlobal()
            {

                if (base.characterMotor)
                {

                    savedMoveVector = transform.position - previousMoveVector;
                    previousMoveVector = transform.position;
                    base.characterMotor.rootMotion += chargeVector * moveSpeedStat * chargeSpeed * GetDeltaTime();

                }
                RaycastHit hit;
                Ray ray = new Ray
                {
                    direction = characterDirection.forward,
                    origin = transform.position
                };
                if (Physics.Raycast(ray.origin, ray.direction, out hit, (chargeVector * moveSpeedStat * chargeSpeed * GetDeltaTime()).magnitude + characterBody.radius, LayerIndex.world.mask | LayerIndex.entityPrecise.mask, QueryTriggerInteraction.UseGlobal))
                {
                    if (characterMotor && Vector3.Angle(characterMotor.Motor.CharacterUp, hit.normal) <= characterMotor.slopeLimit)
                    {

                    }
                    else
                    {
                        HealthComponent healthComponent = hit.collider.GetComponent<HurtBox>()?.healthComponent;
                        if (NetworkServer.active && healthComponent && healthComponent.body.teamComponent.teamIndex != characterBody.teamComponent.teamIndex)
                        {
                            var bonk = new DamageInfo
                            {
                                damage = damageStat * 3 * (1 + chargePercentage),
                                attacker = gameObject,
                                canRejectForce = true,
                                crit = false,
                                damageColorIndex = DamageColorIndex.Default,
                                damageType = DamageTypeCombo.GenericUtility,
                                force = chargeVector * moveSpeedStat * chargeSpeed,
                                inflictor = gameObject,
                                position = transform.position,
                                procChainMask = default,
                                procCoefficient = 1f,
                            };
                            healthComponent.TakeDamageProcess(bonk);
                            Util.PlaySound(DemoChargeHitFleshSound.playSoundString, gameObject);
                        }
                        else
                        {
                            Util.PlaySound(DemoChargeWorldHitSound.playSoundString, gameObject);
                        }
                        if (isAuthority)
                        {
                            outer.SetNextStateToMain();

                        }
                    }


                }

            }
            public virtual void FixedUpdateAuthority()
            {

                if (chargeMeterHUD)
                {
                    chargeMeterHUD.fillAmount = chargePercentage;
                }

            }
            public virtual void OnStageOne()
            {
                if (stageOneBuffs > 0)
                {
                    if (NetworkServer.active)
                    {
                        characterBody.SetBuffCount(buffOnCharge.buffIndex, stageOneBuffs);
                    }
                }

            }
            public virtual void OnStageOneAuthority()
            {
                if (chargeMeterHUD && stageOneBuffs > 0) chargeMeterHUD.color = Color.yellow;
            }
            public virtual void OnStageTwo()
            {
                if (stageTwoBuffs > 0)
                {
                    if (NetworkServer.active)
                    {
                        characterBody.SetBuffCount(buffOnCharge.buffIndex, stageTwoBuffs);
                    }
                }
            }
            public virtual void OnStageTwoAuthority()
            {
                if (chargeMeterHUD && stageTwoBuffs > 0) chargeMeterHUD.color = Color.red;
            }
            public virtual void OnChargeEnd()
            {
                outer.SetNextStateToMain();
            }
            public virtual void DeductCharge()
            {
                chargeMeter -= GetDeltaTime();
            }
            public virtual Vector3 CalculateVector()
            {
                return Vector3.RotateTowards(chargeVector, new Vector3(inputBank.aimDirection.x, 0f, inputBank.aimDirection.z).normalized, chargeControl * GetDeltaTime(), 0f);
            }
            public override void FixedUpdate()
            {
                base.FixedUpdate();
                DeductCharge();
                chargeVector = CalculateVector();
                FixedUpdateGlobal();
                if (isAuthority) FixedUpdateAuthority();
                if (1 - (chargePercentage) > stageOnePercentage && shieldStageCount < 1)
                {
                    OnStageOne();
                    shieldStageCount++;
                    if (isAuthority) OnStageOneAuthority();
                }
                if (1 - (chargePercentage) > stageTwoPercentage && shieldStageCount < 2)
                {
                    OnStageTwo();
                    shieldStageCount++;
                    if (isAuthority) OnStageTwoAuthority();
                }
                if (chargeMeter < 0f) OnChargeEnd();
            }
            public override void Update()
            {
                base.Update();
                if (base.isAuthority)
                {
                    if (characterDirection)
                        characterDirection.forward = chargeVector;
                    if (skillLocator && inputBank && inputBank.skill1.justPressed && skillLocator.primary.ExecuteIfReady())
                    {
                        //int buffCount = characterBody.GetBuffCount(ExtraSwordDamagePrepare);
                        //for (int i = 0; i < buffCount; i++)
                        //{
                        //    characterBody.AddTimedBuff(ExtraSwordDamage, 1f);
                        //}
                        //characterBody.SetBuffCount(ExtraSwordDamagePrepare.buffIndex, 0);
                        outer.SetNextStateToMain();
                    }
                }
            }
            public virtual void OnExitGlobal()
            {
                if (characterMotor)
                {
                    characterMotor.velocity.y = 0f;
                    characterMotor.velocity += savedMoveVector / Time.fixedDeltaTime;
                    characterMotor.disableAirControlUntilCollision = true;
                }
                if (NetworkServer.active)
                {
                    int buffCount = characterBody.GetBuffCount(buffOnCharge);
                    characterBody.SetBuffCount(buffOnCharge.buffIndex, 0);
                    for (int i = 0; i < buffCount; i++)
                    {
                        characterBody.AddTimedBuff(buffOnCharge, 0.6f);
                    }
                }

                characterBody.RecalculateStats();
            }
            public virtual void OnExitAuthority()
            {
                if (chargeMeterHUD)
                {
                    demoComponent.updateMeter = true;
                    //chargeMeterHUD.fillAmount = 1f;
                    chargeMeterHUD.color = Color.white;
                }
            }
            public override void OnExit()
            {
                base.OnExit();
                OnExitGlobal();
                if (base.isAuthority) OnExitAuthority();
            }
            public override InterruptPriority GetMinimumInterruptPriority()
            {
                return InterruptPriority.Skill;
            }
        }
        public enum GrenadeLauncherChargeAffection
        {
            Damage,
            Speed,
            Lifetime
        }
        public abstract class GrenadeLauncher : BaseSkillState
        {
            public float stopwatch = 0f;
            public bool fired = false;
            public bool stopCharge = false;
            public abstract float damage { get; }
            public abstract float fireRate { get; }
            public abstract bool canBeCharged { get; }
            public abstract float chargeCap { get; }
            public abstract bool isPrimary { get; }
            public abstract GameObject projectile { get; }
            public abstract string fireSound { get; }
            public abstract GrenadeLauncherChargeAffection[] chargeTags { get; }

            public DemoComponent demoComponent;

            public float charge = 0f;
            private bool released = false;
            private Image chargeMeter;
            private bool changeMeter = true;
            public virtual void OnFullChargeEnd()
            {
                if (!fired)
                    FireProjectile(true);
            }
            public virtual void OnEarlyChargeEnd()
            {
                if (!fired)
                    FireProjectile(true);
            }
            public virtual void OnProjectileFired()
            {
                fired = true;
                stopwatch = fireRate / base.attackSpeedStat;
            }
            public override void OnEnter()
            {
                base.OnEnter();
                demoComponent = gameObject.GetComponent<DemoComponent>();
                if (canBeCharged && isAuthority) Util.PlaySound(DemoCannonChargeSound.playSoundString, gameObject);
                if (base.isAuthority)
                {

                    chargeMeter = demoComponent ? demoComponent.rightMeter : null;
                    if (chargeMeter)
                    {
                        //demoComponent.updateMeter = false;
                        chargeMeter.fillAmount = 0f;
                    }

                }

            }
            public virtual void FireProjectile(bool stopCharge = true)
            {
                PlayAnimation("Gun, Override", "ShootGun");
                base.StartAimMode(snap: true);
                Ray aimRay = base.GetAimRay();
                if (stopCharge)
                {
                    stopCharge = true;
                    changeMeter = false;
                }
                TrajectoryAimAssist.ApplyTrajectoryAimAssist(ref aimRay, projectile, base.gameObject, 1f);
                if (canBeCharged) Util.PlaySound(DemoCannonChargeSound.stopSoundString, gameObject);

                if (isAuthority)
                {
                    FireProjectileInfo fireProjectileInfo = new FireProjectileInfo
                    {
                        projectilePrefab = projectile,
                        position = aimRay.origin,
                        rotation = Util.QuaternionSafeLookRotation(aimRay.direction),
                        owner = base.gameObject,
                        damage = damage * this.damageStat * (chargeTags.Contains(GrenadeLauncherChargeAffection.Damage) ? 1 + charge : 1),
                        force = 1f,
                        crit = Util.CheckRoll(this.critStat, base.characterBody.master),
                        speedOverride = chargeTags.Contains(GrenadeLauncherChargeAffection.Speed) ? projectile.GetComponent<ProjectileSimple>().desiredForwardSpeed * (1 + charge) : -1f,
                        damageTypeOverride = new DamageTypeCombo?(new DamageTypeCombo(DamageType.Generic, DamageTypeExtended.Generic, GetDamageSource()))
                    };
                    ModifiyProjectileFireInfo(ref fireProjectileInfo);
                    ProjectileManager.instance.FireProjectile(fireProjectileInfo);

                }
                characterBody.AddSpreadBloom(0.5f);
                Util.PlaySound(fireSound, gameObject);
                if (chargeMeter)
                {
                    chargeMeter.fillAmount = 1f;
                }



                OnProjectileFired();
            }
            private DamageSource GetDamageSource()
            {
                if (skillLocator)
                {
                    if (skillLocator.primary == activatorSkillSlot)
                    {
                        return DamageSource.Primary;
                    }
                    if (skillLocator.secondary == activatorSkillSlot)
                    {
                        return DamageSource.Secondary;
                    }
                    if (skillLocator.utility == activatorSkillSlot)
                    {
                        return DamageSource.Utility;
                    }
                    if (skillLocator.special == activatorSkillSlot)
                    {
                        return DamageSource.Special;
                    }
                    return DamageSource.NoneSpecified;
                }
                return DamageSource.NoneSpecified;
            }
            private InputBankTest.ButtonState GetButton()
            {
                if (skillLocator.primary == activatorSkillSlot)
                {
                    return inputBank.skill1;
                }
                if (skillLocator.secondary == activatorSkillSlot)
                {
                    return inputBank.skill2;
                }
                if (skillLocator.utility == activatorSkillSlot)
                {
                    return inputBank.skill3;
                }
                if (skillLocator.special == activatorSkillSlot)
                {
                    return inputBank.skill4;
                }
                return inputBank.skill1;
            }
            public virtual void ModifiyProjectileFireInfo(ref FireProjectileInfo fireProjectileInfo)
            {

            }
            public virtual void IncreaseChargeOnFixedUpdate()
            {
                charge += GetDeltaTime() * attackSpeedStat;
            }
            public override void FixedUpdate()
            {
                base.FixedUpdate();
                IncreaseChargeOnFixedUpdate();
                if (stopwatch > 0)
                {
                    stopwatch -= GetDeltaTime();
                }
                if (true)
                {

                    if (changeMeter && isAuthority && !stopCharge && chargeMeter)
                    {
                        chargeMeter.fillAmount = charge / chargeCap;
                    }
                    if (!stopCharge && (!inputBank || !GetButton().down || charge >= chargeCap))
                    {
                        if (charge < chargeCap)
                        {
                            OnEarlyChargeEnd();
                        }
                        else
                        {
                            OnFullChargeEnd();
                        }
                        released = true;
                    }
                    if (isAuthority && fired && stopwatch <= 0)
                    {
                        outer.SetNextStateToMain();
                    }
                }

            }
            public override void OnExit()
            {
                base.OnExit();
                if (isAuthority)
                {
                    if (chargeMeter)
                    {
                        //demoComponent.updateMeter = true;
                        chargeMeter.fillAmount = 1f;
                    }
                }

            }
            public override InterruptPriority GetMinimumInterruptPriority()
            {
                return InterruptPriority.Skill;
            }
        }

        public class PillLauncher : GrenadeLauncher
        {
            public override float damage => 4.5f;

            public override GameObject projectile => Main.PillProjectile;

            public override bool canBeCharged => false;

            public override float fireRate => 0.5f;

            public override bool isPrimary => false;

            public override float chargeCap => 0f;

            public override GrenadeLauncherChargeAffection[] chargeTags => new GrenadeLauncherChargeAffection[] { };

            public override string fireSound => DemoGrenadeShootSound.playSoundString;
        }
        public class RocketLauncher : GrenadeLauncher
        {
            public override float damage => 4f;

            public override GameObject projectile => Main.RocketProjectile;
            public override GrenadeLauncherChargeAffection[] chargeTags => new GrenadeLauncherChargeAffection[] { };

            public override bool canBeCharged => false;

            public override float fireRate => 0.5f;
            public override bool isPrimary => false;
            public override float chargeCap => 0f;
            public override string fireSound => DemoGrenadeShootSound.playSoundString;

        }
        public class HookLauncher : GrenadeLauncher
        {
            public override float damage => 0f;
            public override GameObject projectile => Main.HookProjectile;
            public override GrenadeLauncherChargeAffection[] chargeTags => new GrenadeLauncherChargeAffection[] { };
            public override bool canBeCharged => true;
            public override float fireRate => 0.5f;
            public override bool isPrimary => false;
            public override float chargeCap => 12f;
            public override string fireSound => DemoGrenadeShootSound.playSoundString;
            public override void OnEnter()
            {
                base.OnEnter();
                //if (isAuthority)
                //{
                FireProjectile(false);
                //}
            }
            public override void OnProjectileFired()
            {
            }
            public override void OnEarlyChargeEnd()
            {
                if (isAuthority)
                outer.SetNextStateToMain();
            }
            public override void OnFullChargeEnd()
            {
                outer.SetNextStateToMain();
            }
            public override void IncreaseChargeOnFixedUpdate()
            {
            }
        }
        public class NukeLauncher : GrenadeLauncher
        {
            public override float damage => 666f;

            public override GameObject projectile => Main.NukeProjectile;

            public override bool canBeCharged => false;

            public override float fireRate => 0.5f;

            public override bool isPrimary => false;

            public override float chargeCap => 0f;
            public override GrenadeLauncherChargeAffection[] chargeTags => new GrenadeLauncherChargeAffection[] { };
            public override string fireSound => DemoGrenadeShootSound.playSoundString;

        }
        public class BombLauncher : GrenadeLauncher
        {
            public override float damage => 6f;
            public override GameObject projectile => Main.BombProjectile;
            public override bool canBeCharged => true;
            public override float fireRate => 0.5f;
            public override bool isPrimary => false;
            public override float chargeCap => 1f;
            public override GrenadeLauncherChargeAffection[] chargeTags => new GrenadeLauncherChargeAffection[] { };
            public override string fireSound => DemoGrenadeShootSound.playSoundString;
            public override void IncreaseChargeOnFixedUpdate()
            {
                charge += GetDeltaTime();
            }

        }
        public class StickyLauncher : GrenadeLauncher
        {
            public override float damage => 4f;
            public override float fireRate => 0.5f;

            public override GameObject projectile => Main.StickyProjectile;

            public override bool canBeCharged => true;

            public override bool isPrimary => true;

            public override float chargeCap => 1f;
            public override GrenadeLauncherChargeAffection[] chargeTags => new GrenadeLauncherChargeAffection[] { GrenadeLauncherChargeAffection.Speed };
            public override string fireSound => DemoGrenadeShootSound.playSoundString;

        }
        public class JumperLauncher : GrenadeLauncher
        {
            public override float damage => 0f;
            public override float fireRate => 0.3f;

            public override GameObject projectile => Main.JumperProjectile;

            public override bool canBeCharged => true;

            public override bool isPrimary => true;

            public override float chargeCap => 1f;
            public override GrenadeLauncherChargeAffection[] chargeTags => new GrenadeLauncherChargeAffection[] { GrenadeLauncherChargeAffection.Speed };
            public override string fireSound => DemoGrenadeShootSound.playSoundString;

        }
        public class AntigravLauncher : GrenadeLauncher
        {
            public override float damage => 5f;
            public override float fireRate => 0.6f;

            public override GameObject projectile => Main.AntigravProjectile;

            public override bool canBeCharged => true;
            public override bool isPrimary => true;

            public override float chargeCap => 2f;
            public override GrenadeLauncherChargeAffection[] chargeTags => new GrenadeLauncherChargeAffection[] { GrenadeLauncherChargeAffection.Speed };
            public override string fireSound => DemoGrenadeShootSound.playSoundString;

        }
        public class MineLayer : GrenadeLauncher
        {
            public override float damage => 3f;
            public override float fireRate => 0.3f;

            public override GameObject projectile => Main.MineProjectile;

            public override bool canBeCharged => false;
            public override bool isPrimary => true;

            public override float chargeCap => 0f;
            public override GrenadeLauncherChargeAffection[] chargeTags => new GrenadeLauncherChargeAffection[] { GrenadeLauncherChargeAffection.Speed };
            public override string fireSound => DemoGrenadeShootSound.playSoundString;

        }

        public class ShieldChargeHeavy : ShieldCharge
        {
            public override float chargeMaxMeter => 1.75f;

            public override float chargeControl => 2.5f;

            public override float stageOnePercentage => 0.25f;

            public override float stageTwoPercentage => 0.6f;

            public override float chargeSpeed => 2.5f;

            public override float armor => 100f;

            public override BuffDef buffOnCharge => ExtraSwordDamage;

            public override int stageOneBuffs => 35;

            public override int stageTwoBuffs => 300;
        }
        public class ShieldChargeLight : ShieldCharge
        {
            public override float chargeMaxMeter => 1.75f;

            public override float chargeControl => 90f;

            public override float stageOnePercentage => 0.25f;

            public override float stageTwoPercentage => 0.6f;

            public override float chargeSpeed => 2.5f;

            public override float armor => 20f;
            public override BuffDef buffOnCharge => ExtraSwordDamage;

            public override int stageOneBuffs => 35;

            public override int stageTwoBuffs => 35;
        }
        public class ShieldChargeAntigravity : ShieldCharge
        {
            public override float chargeMaxMeter => 1f;

            public override float chargeControl => 90f;

            public override float stageOnePercentage => 0.2f;

            public override float stageTwoPercentage => 0.9f;

            public override float chargeSpeed => 2f;

            public override float armor => 0f;
            public override BuffDef buffOnCharge => ExtraSwordDamage;

            public override int stageOneBuffs => 0;

            public override int stageTwoBuffs => 0;

            public override Vector3 CalculateVector()
            {
                return inputBank.aimDirection;
            }
        }

        public class Detonate : BaseState
        {
            private Main.DemoComponent demoComponent;
            public override void OnEnter()
            {
                base.OnEnter();
                demoComponent = gameObject.GetComponent<Main.DemoComponent>();
                if (demoComponent != null)
                {
                    DetonateAllStickies();
                }
                outer.SetNextStateToMain();
            }
            public void DetonateAllStickies()
            {
                bool noArmed = false;
                int stickyCount = 0;
                while (!noArmed)
                {
                    noArmed = true;
                    for (int i = 0; i < demoComponent.stickies.Count; i++)
                    {
                        List<StickyComponent> stickies = demoComponent.stickies.ElementAt(i).Value;
                        for (int j = 0; j < stickies.Count; j++)
                        {
                            if (stickies[j] != null && stickies[j].isArmed)
                            {
                                stickyCount++;
                                noArmed = false;
                                stickies[j].DetonateSticky();
                            }
                        }

                    }
                }
                if (stickyCount > 0)
                {
                    Util.PlaySound(DemoStickyDetonationSound.playSoundString, gameObject);
                    Transform modelTransform = GetComponent<ModelLocator>()?.modelTransform;
                    ChildLocator childLocator = modelTransform? modelTransform.GetComponent<ChildLocator>() : null;
                    if (childLocator)
                    {
                        Transform transform = childLocator.FindChild("Head");
                        if (transform)
                        {
                            GameObject detonatingVFX = GameObject.Instantiate(ArmedEffect, transform);
                            Transform transform1 = detonatingVFX.transform;
                            transform1.localPosition = new Vector3(0.078f, 0.195f, 0.068f);
                            transform1.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                            transform1.rotation = Quaternion.identity;
                        }
                    }
                }
            }
            public override void FixedUpdate()
            {
                base.FixedUpdate();


            }
            public override void OnExit()
            {
                base.OnExit();

            }
            public virtual InterruptPriority GetMinimumInterruptPriority()
            {
                return InterruptPriority.Skill;
            }
        }
        /*
        public class AntigravDetonate : BaseState
        {
            private Main.DemoComponent demoComponent;
            private Vector3 chargeVector;
            private StickyComponent stickyComponent;
            private float chargeMeter = 0f;
            private float stopwatch = 0f;
            private float speed;
            private KinematicCharacterMotor kinematicCharacterMotor;
            public override void OnEnter()
            {
                base.OnEnter();
                if (isAuthority)
                {
                    bool foundAntigrav = false;
                    Vector3 toSticky = Vector3.zero;
                    Vector3 toStickyPrevious = Vector3.zero;
                    float distance = 0f;
                    float maxDistance = 512f;
                    chargeMeter = 0f;
                    foreach (var sticky in base.characterBody.GetComponent<DemoComponent>().limitedStickies)
                    {
                        if (sticky.isArmed)
                        {
                            toSticky = sticky.transform.position - base.inputBank.aimOrigin;
                            //distance = Vector3.Distance(sticky.transform.position, inputBank.aimOrigin);
                            RaycastHit hit = new RaycastHit();
                            if (Vector3.Angle(base.inputBank.aimDirection, toSticky.normalized) < 45f && toSticky.sqrMagnitude < maxDistance * maxDistance) //&& Util.CharacterRaycast(gameObject, GetAimRay(), out hit, maxDistance, LayerIndex.world.mask, QueryTriggerInteraction.Collide))
                            {
                                maxDistance = toSticky.magnitude;
                                foundAntigrav = true;
                                stickyComponent = sticky;
                                //break;
                            }
                        }

                    }
                    if (stickyComponent != null)
                    {
                        chargeMeter = 8f;
                        chargeVector = toSticky.normalized;
                        speed = 64f;
                        chargeVector = stickyComponent.transform.position - transform.position;
                        stopwatch = chargeVector.magnitude / speed;
                        PlayAnimation("FullBody, Override", "Ball", "Slash.playbackRate", stopwatch);
                        if (characterMotor) characterMotor.useGravity = false;
                        kinematicCharacterMotor = GetComponent<KinematicCharacterMotor>();
                        if (kinematicCharacterMotor)
                            kinematicCharacterMotor.ForceUnground(0f);
                        if (characterDirection) characterDirection.forward = chargeVector.normalized;
                    }
                    else
                    {
                        outer.SetNextStateToMain();
                    }
                }

            }
            public override void FixedUpdate()
            {
                base.FixedUpdate();
                if (isAuthority)
                {
                    stopwatch -= GetDeltaTime();
                    if (stopwatch < 0f)
                    {
                        stickyComponent.DetonateSticky(0f);
                        chargeMeter = 0f;
                        outer.SetNextStateToMain();
                        if (base.characterMotor) characterMotor.velocity = new Vector3(0f, 32f, 0f);
                    }
                    else
                    {
                        if (base.characterMotor)
                            base.characterMotor.rootMotion += chargeVector.normalized * speed * GetDeltaTime();
                    }
                }


            }
            public override void OnExit()
            {
                base.OnExit();
                if (isAuthority && characterMotor) characterMotor.useGravity = true;

            }
            public virtual InterruptPriority GetMinimumInterruptPriority()
            {
                return InterruptPriority.Skill;
            }
        }*/
        public class SpecialOneRedirector : BaseState
        {
            public override void OnEnter()
            {
                base.OnEnter();
                DemoComponent demoComponent = GetComponent<DemoComponent>();
                EntityStateMachine entityStateMachine = GetComponent<EntityStateMachine>();
                if (isAuthority)
                {
                    if (demoComponent && demoComponent.isSwapped)
                    {
                        entityStateMachine.SetNextState(new SpecialOneStickySpiner
                        {
                            previousStateMachine = outer,
                        });
                    }
                    else
                    {
                        entityStateMachine.SetNextState(new SpecialOneSwordSpiner
                        {
                            previousStateMachine = outer,
                        });
                    }
                }
            }
        }
        public class SpecialSwordSpiner : MonoBehaviour
        {
            public SpecialOneSwordSpiner Sword;
            public BulletAttack bulletAttack = DefaultSword.bulletAttack;
            public List<CharacterBody> charactersBlacklist = new List<CharacterBody>();
            public Dictionary<Collider, CharacterBody> keyValuePairs = new Dictionary<Collider, CharacterBody>();
            public void Start()
            {
            }
            public void OnTriggerStay(Collider collider)
            {
                if (!NetworkServer.active) return;
                CharacterBody characterBody = null;
                if (keyValuePairs.ContainsKey(collider))
                {
                    characterBody = keyValuePairs[collider];
                }
                else
                {
                    HurtBox hurtBox = collider.GetComponent<HurtBox>();
                    if (hurtBox)
                    {
                        characterBody = hurtBox.healthComponent.body;
                    }
                    keyValuePairs.Add(collider, characterBody);
                }
                if (characterBody && !charactersBlacklist.Contains(characterBody))
                {
                    BulletAttack bulletAttack2 = new BulletAttack()
                    {
                        radius = 0.1f,
                        aimVector = Vector3.up,
                        damage = Sword.damageStat * (bulletAttack != null ? bulletAttack.damage : 3f) / 2,
                        bulletCount = 1,
                        spreadPitchScale = 0f,
                        spreadYawScale = 0f,
                        maxSpread = 0f,
                        minSpread = 0f,
                        maxDistance = 0.1f,
                        allowTrajectoryAimAssist = false,
                        hitMask = LayerIndex.entityPrecise.mask,
                        procCoefficient = 0.4f,
                        stopperMask = LayerIndex.entityPrecise.mask,
                        damageType = new DamageTypeCombo(DamageType.Generic, DamageTypeExtended.Generic, DamageSource.Special),
                        force = bulletAttack != null ? bulletAttack.force : 1f,
                        falloffModel = BulletAttack.FalloffModel.None,
                        damageColorIndex = DamageColorIndex.Default,
                        isCrit = Sword.RollCrit(),
                        origin = characterBody.mainHurtBox.transform.position,
                        owner = Sword.gameObject,
                        hitCallback = (bulletAttack != null ? bulletAttack.hitCallback : null) + new Main().EffectOnHit,

                    };
                    bulletAttack2.Fire();
                    charactersBlacklist.Add(characterBody);
                }
            }
        }
        public class SpecialStickySpiner : MonoBehaviour
        {
            public BoxCollider boxCollider;
            public void Start()
            {
                boxCollider = GetComponent<BoxCollider>();
            }
            public void FireProjectile(GameObject projectile, float damage, bool crit, GameObject owner)
            {
                Bounds bounds = boxCollider.bounds;
                FireProjectileInfo fireProjectileInfo = new FireProjectileInfo
                {
                    projectilePrefab = projectile,
                    position = new Vector3(
                    Random.Range(bounds.min.x, bounds.max.x),
                    Random.Range(bounds.min.y, bounds.max.y),
                    Random.Range(bounds.min.z, bounds.max.z)
                ),
                    rotation = Quaternion.LookRotation(Physics.gravity * -1f),
                    procChainMask = default,
                    owner = owner,
                    damage = damage,
                    crit = crit,
                    force = 200f,
                    damageColorIndex = DamageColorIndex.Default,
                    damageTypeOverride = new DamageTypeCombo?(new DamageTypeCombo(DamageType.Generic, DamageTypeExtended.Generic, DamageSource.Special)),
                    speedOverride = 30
                };
                ProjectileManager.instance.FireProjectile(fireProjectileInfo);
            }
        }
        public abstract class SpecialSpinner : BaseState
        {
            private float stopwatch = 0f;
            public abstract float timer { get; }
            public abstract float rotationsPerSecond { get; }
            private float hitStopwatch = 0f;
            private float hitTimer = 1f;
            private bool fired = false;
            private float animationTimer = 1f;
            private Vector3 currentVector = Vector3.zero;
            public Vector3 rotationVector = Vector3.zero;
            public EntityStateMachine previousStateMachine;
            public override void OnEnter()
            {
                base.OnEnter();
                if (!characterDirection) outer.SetNextStateToMain();
                fired = true;
                PlayAnimation("FullBody, Override", "Spin", "Slash.playbackRate", timer);
                currentVector = inputBank ? inputBank.moveVector : transform.forward;
                if (NetworkServer.active)
                    characterBody.AddBuff(RoR2Content.Buffs.SmallArmorBoost);
            }
            public override void OnExit()
            {
                base.OnExit();
                if (NetworkServer.active)
                    characterBody.RemoveBuff(RoR2Content.Buffs.SmallArmorBoost);
                if (previousStateMachine != null) previousStateMachine.SetNextStateToMain();
            }
            public virtual void OnFullRotation()
            {
                hitStopwatch = 0f;
            }
            public override void FixedUpdate()
            {
                base.FixedUpdate();
                stopwatch += GetDeltaTime();
                if (inputBank && characterMotor)
                {
                    currentVector = Vector3.MoveTowards(currentVector, inputBank.moveVector, 1 * GetDeltaTime());
                    characterMotor.rootMotion += ((currentVector * characterMotor.walkSpeed * 2) * GetDeltaTime());
                }
                rotationVector = Quaternion.AngleAxis(360 * rotationsPerSecond * Time.fixedDeltaTime * characterBody.attackSpeed, Vector3.up) * rotationVector;
                characterDirection.forward = rotationVector;
                hitStopwatch += GetDeltaTime();
                if (hitStopwatch >= hitTimer / rotationsPerSecond / characterBody.attackSpeed)
                {
                    OnFullRotation();

                }
                if (stopwatch > timer)
                {
                    PlayAnimation("FullBody, Override", "SpinEnd", "Slash.playbackRate", 1 / 2);
                    outer.SetNextStateToMain();
                }


            }
        }
        public class SpecialOneSwordSpiner : SpecialSpinner
        {
            public override float timer => 4f;

            public override float rotationsPerSecond => 4f;
            private BulletAttack bulletAttack;
            private SpecialSwordSpiner specialSwordSpiner;
            private GameObject spinner;
            public override void OnEnter()
            {
                base.OnEnter();
                bulletAttack = swordDictionary.ContainsKey(skillLocator.primary.baseSkill) ? swordDictionary[skillLocator.primary.baseSkill].bulletAttack : DefaultSword.bulletAttack;
                spinner = GameObject.Instantiate(Main.SpecialSpinner);
                spinner.transform.parent = characterDirection.targetTransform;
                spinner.transform.rotation = characterDirection.targetTransform.rotation;
                spinner.transform.position = inputBank.aimOrigin;
                spinner.transform.localScale = new Vector3(bulletAttack.maxDistance, 1f, bulletAttack.maxDistance);
                rotationVector = characterDirection.forward;
                specialSwordSpiner = spinner.AddComponent<SpecialSwordSpiner>();
                specialSwordSpiner.Sword = this;
                specialSwordSpiner.bulletAttack = bulletAttack;
            }
            public override void OnExit()
            {
                base.OnExit();
                GameObject.Destroy(spinner, 0.2f);
            }
            public override void OnFullRotation()
            {
                base.OnFullRotation();
                specialSwordSpiner.charactersBlacklist.Clear();
            }
        }
        public class SpecialOneStickySpiner : SpecialSpinner
        {
            public override float timer => 1f;
            public SpecialStickySpiner specialStickySpiner;
            public override float rotationsPerSecond => 4f;
            private GameObject spinner;
            private GameObject projectile;
            private float damage;
            private float newStopwatch = 0;
            private DemoComponent demoComponent;
            private bool crit = false;
            public override void OnEnter()
            {
                base.OnEnter();
                DemoStickyClass demoSticky = bombProjectiles.ContainsKey(skillLocator.primary.baseSkill) ? bombProjectiles[skillLocator.primary.baseSkill] : null;
                projectile = demoSticky != null ? demoSticky.stickyObject : LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/FireMeatBall");
                damage = demoSticky != null ? demoSticky.stickyState.damage : 4;
                spinner = GameObject.Instantiate(Main.SpecialSpinner);
                spinner.transform.parent = characterDirection.targetTransform;
                spinner.transform.rotation = characterDirection.targetTransform.rotation;
                spinner.transform.position = inputBank.aimOrigin;
                spinner.transform.localScale = new Vector3(2f, 1f, 2f);
                rotationVector = characterDirection.forward;
                demoComponent = GetComponent<DemoComponent>();
                if (demoComponent)
                {
                    demoComponent.DetonateNoLimitStickies();
                    demoComponent.noLimitStickies++;
                }
                specialStickySpiner = spinner.AddComponent<SpecialStickySpiner>();
            }
            public override void OnExit()
            {
                base.OnExit();
                GameObject.Destroy(spinner, 0.2f);
                if (demoComponent)
                {
                    demoComponent.noLimitStickies--;
                }
            }
            public override void FixedUpdate()
            {
                base.FixedUpdate();
                newStopwatch += GetDeltaTime();
                if (newStopwatch > 0.1f / characterBody.attackSpeed)
                {
                    if (isAuthority)
                    {
                        specialStickySpiner.FireProjectile(projectile, base.damageStat * damage, RollCrit(), gameObject);
                    }
                    Util.PlaySound(DemoGrenadeShootSound.playSoundString, gameObject);
                    PlayAnimation("Gun, Override", "ShootGun");
                    newStopwatch = 0;

                }
            }
            public override void OnFullRotation()
            {
                base.OnFullRotation();
            }
        }
        public abstract class BigAssAttackFire : BaseState
        {
            public float chargePercentage = 0f;
            public override void OnSerialize(NetworkWriter writer)
            {
                base.OnSerialize(writer);
                writer.Write(chargePercentage);
            }
            public override void OnDeserialize(NetworkReader reader)
            {
                base.OnDeserialize(reader);
                chargePercentage = reader.ReadSingle();
            }
            public override void OnEnter()
            {
                base.OnEnter();
                
            }
        }
        public abstract class BigAssAttack : BaseState
        {
            private int stage = 0;
            public float power = 0f;
            public abstract float chargeCap { get; }
            public bool isCharged = false;
            public DemoComponent demoComponent;
            public Image chargeImage;
            public EntityStateMachine previousStateMachine;
            private bool stoppedSound = false;
            public override void OnEnter()
            {
                base.OnEnter();
                if (isAuthority)
                    Util.PlaySound(DemoCannonChargeSound.playSoundString, gameObject);
                demoComponent = GetComponent<DemoComponent>();
                chargeImage = demoComponent ? demoComponent.rightMeter : null;
            }
            public override void FixedUpdate()
            {
                base.FixedUpdate();
                if (stage == 0)
                {
                    OnHold();
                    if (power < chargeCap)
                    {
                        if (isAuthority)
                            chargeImage.fillAmount = power / chargeCap;
                        power += GetDeltaTime() * characterBody.attackSpeed;
                    }
                    else if (!isCharged)
                    {
                        isCharged = true;
                        OnFullCharge();
                    }
                    if (isAuthority && inputBank && !inputBank.skill4.down)
                    {
                        OnRelease();
                    }
                }


            }
            public virtual void OnFullCharge()
            {
                if (isAuthority)
                {
                    if (chargeImage)
                        chargeImage.fillAmount = 1f;
                    Util.PlaySound(DemoCannonChargeSound.stopSoundString, gameObject);
                    stoppedSound = false;
                }
            }
            public virtual void OnHold()
            {
                characterBody.SetAimTimer(2f);
            }
            public override void OnExit()
            {
                base.OnExit();
                if (isAuthority)
                {
                    if (!stoppedSound)
                    Util.PlaySound(DemoCannonChargeSound.stopSoundString, gameObject);
                    if (chargeImage)
                        chargeImage.fillAmount = 1f;
                    if (previousStateMachine)
                    {
                        previousStateMachine.SetNextStateToMain();
                    }
                }

            }
            public virtual void OnRelease()
            {
            }
        }
        public class BigAssSwordFire : BigAssAttackFire
        {
            public DemoSwordClass swordClass;
            public override void OnEnter()
            {
                base.OnEnter();
                swordClass = swordDictionary.ContainsKey(skillLocator.primary.baseSkill) ? swordDictionary[skillLocator.primary.baseSkill] : DefaultSword;
                BulletAttack swordAttack = swordClass.bulletAttack;
                Vector3 center = inputBank ? inputBank.aimOrigin + characterDirection.forward * swordAttack.maxDistance : transform.position + characterDirection.forward * swordAttack.maxDistance;
                if (NetworkServer.active)
                {
                    BulletAttack bulletAttack2 = new BulletAttack()
                    {
                        radius = swordAttack.maxDistance,
                        aimVector = Vector3.up,
                        damage = base.damageStat * (swordAttack != null ? swordAttack.damage : generalSwordDamage) * 1.5f * (1 + (chargePercentage * 2f)),
                        bulletCount = 1,
                        spreadPitchScale = 0f,
                        spreadYawScale = 0f,
                        maxSpread = 0f,
                        minSpread = 0f,
                        maxDistance = swordAttack.maxDistance,
                        allowTrajectoryAimAssist = false,
                        hitMask = LayerIndex.entityPrecise.mask,
                        stopperMask = LayerIndex.noCollision.mask,
                        procCoefficient = swordAttack != null ? swordAttack.procCoefficient : 1f,
                        damageType = new DamageTypeCombo(DamageType.Generic, DamageTypeExtended.Generic, DamageSource.Special),
                        force = 300f,
                        falloffModel = BulletAttack.FalloffModel.None,
                        damageColorIndex = DamageColorIndex.Default,
                        isCrit = base.RollCrit(),
                        origin = center,
                        owner = gameObject,
                        hitCallback = swordAttack != null ? swordAttack.hitCallback : default,

                    };
                    bulletAttack2.Fire();

                }
                EffectData effectData = new EffectData
                {
                    origin = center,
                    scale = swordAttack.radius
                };
                EffectManager.SpawnEffect(groundSlamVFX, effectData, true);
                PlayAnimation("Gesture, Override", "SwingDown1", "Slash.playbackRate", swordClass.swingDownTime / characterBody.attackSpeed, 0.2f);
                outer.SetNextStateToMain();
            }
        }
        public class BigAssSword : BigAssAttack
        {
            public override float chargeCap => 3f;
            public DemoSwordClass swordClass;
            public Transform swordTransform;
            public Vector3 previousScale;
            public override void OnEnter()
            {
                base.OnEnter();
                swordClass = swordDictionary.ContainsKey(skillLocator.primary.baseSkill) ? swordDictionary[skillLocator.primary.baseSkill] : DefaultSword;
                Transform modelTransform = GetComponent<ModelLocator>()?.modelTransform;
                ChildLocator childLocator = modelTransform ? modelTransform.GetComponent<ChildLocator>() : null;
                if (childLocator)
                {
                    swordTransform = childLocator.FindChild("WeaponR");
                    if (swordTransform != null)
                    {
                        previousScale = swordTransform.localScale;
                    }
                }
                PlayAnimation("Gesture, Override", "SwingUp", "Slash.playbackRate", swordClass.swingUpTime / base.attackSpeedStat, 0.2f);
            }
            public override void OnExit()
            {
                base.OnExit();
                if (swordTransform)
                {
                    swordTransform.localScale = previousScale;
                }
            }
            public override void OnHold()
            {
                base.OnHold();
                if (!isCharged && swordTransform)
                {
                    swordTransform.localScale = Vector3.MoveTowards(swordTransform.localScale, previousScale + new Vector3(chargeCap, chargeCap, chargeCap), GetDeltaTime());
                }
            }
            public override void OnRelease()
            {
                if (isAuthority)
                    outer.SetNextState(new BigAssSwordFire { chargePercentage = power / chargeCap });
                base.OnRelease();
            }
        }
        public class BigAssSticky : BigAssAttack
        {
            public override float chargeCap => 3f;
            public GameObject projectile;
            public float damage;
            public Transform cannonTransform;
            public Vector3 previousScale;
            public float speedOverride = -1;
            public override void OnEnter()
            {
                base.OnEnter();
                DemoStickyClass demoSticky = bombProjectiles.ContainsKey(skillLocator.primary.baseSkill) ? bombProjectiles[skillLocator.primary.baseSkill] : null;
                projectile = demoSticky != null ? demoSticky.stickyObject : LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/FireMeatBall");
                damage = demoSticky != null ? demoSticky.stickyState.damage : 4;
                demoComponent = GetComponent<DemoComponent>();
                speedOverride = demoSticky != null ? -1 : 60;
                Transform modelTransform = GetComponent<ModelLocator>()?.modelTransform;
                ChildLocator childLocator = modelTransform ? modelTransform.GetComponent<ChildLocator>() : null;
                if (childLocator)
                {
                    cannonTransform = childLocator.FindChild("HeadCannon");
                    if (cannonTransform != null)
                    {
                        previousScale = cannonTransform.localScale;
                    }
                }
            }
            public override void OnHold()
            {
                base.OnHold();
                if (!isCharged && cannonTransform)
                {
                    cannonTransform.localScale = Vector3.MoveTowards(cannonTransform.localScale, previousScale + new Vector3(chargeCap, chargeCap, chargeCap), GetDeltaTime());
                }
            }
            public override void OnRelease()
            {
                if (demoComponent)
                {
                    demoComponent.DetonateNoLimitStickies();
                    demoComponent.noLimitStickies++;
                }
                if (isAuthority)
                {
                    FireProjectileInfo fireProjectileInfo = new FireProjectileInfo
                    {
                        projectilePrefab = projectile,
                        position = inputBank ? inputBank.aimOrigin : transform.position,
                        rotation = Util.QuaternionSafeLookRotation(inputBank ? inputBank.aimDirection : transform.rotation.eulerAngles),
                        procChainMask = default,
                        owner = gameObject,
                        damage = base.damageStat * damage * 2.5f * (1 + (power / chargeCap * 2f)),
                        crit = RollCrit(),
                        force = 200f,
                        damageColorIndex = DamageColorIndex.Default,
                        damageTypeOverride = new DamageTypeCombo?(new DamageTypeCombo(DamageType.Generic, DamageTypeExtended.Generic, DamageSource.Special)),
                        speedOverride = speedOverride
                    };
                    ProjectileManager.instance.FireProjectile(fireProjectileInfo);
                }
                PlayAnimation("Gun, Override", "ShootGun");
                Util.PlaySound(DemoGrenadeShootCritSound.playSoundString, gameObject);
                if (cannonTransform)
                {
                    cannonTransform.localScale = previousScale;
                }
                if (demoComponent)
                {
                    demoComponent.noLimitStickies--;
                }
                base.OnRelease();
                outer.SetNextStateToMain();
            }
        }
        public class BigAssAttackRedirector : BaseState
        {
            public override void OnEnter()
            {
                base.OnEnter();
                DemoComponent demoComponent = GetComponent<DemoComponent>();
                EntityStateMachine bodyStateMachine = GetComponent<EntityStateMachine>();
                EntityStateMachine weaponStateMachine = null;
                if (skillLocator)
                {
                    foreach (GenericSkill genericSkill in skillLocator.allSkills)
                    {
                        EntityStateMachine entityStateMachine = genericSkill.stateMachine;
                        if (genericSkill.stateMachine && genericSkill.stateMachine.customName == "Weapon")
                        {
                            weaponStateMachine = genericSkill.stateMachine;
                            break;
                        }
                    }
                }
                
                if (isAuthority)
                {
                    EntityStateMachine entityStateMachine = weaponStateMachine ? weaponStateMachine : bodyStateMachine;
                    if (demoComponent && demoComponent.isSwapped)
                    {
                        entityStateMachine.SetNextState(new BigAssSticky
                        {
                            previousStateMachine = outer,
                        });
                    }
                    else
                    {
                        entityStateMachine.SetNextState(new BigAssSword
                        {
                            previousStateMachine = outer,
                        });
                    }
                }
            }
        }
        public abstract class UltraInstinctRework : BaseSkillState
        {
            public List<CharacterBody> bodies = new List<CharacterBody>();
            public abstract float searchRadius { get; }
            private float stopwatch = 0f;
            private int stage = 0;
            private CharacterBody previousBody;
            private Vector3 direction;
            private Vector3 position;
            public GameObject effect = SwingEffect;
            public EntityStateMachine previousStateMachine;
            public override void OnEnter()
            {
                base.OnEnter();
                Collider[] collidersArray = Physics.OverlapSphere(transform.position, searchRadius, LayerIndex.entityPrecise.mask, QueryTriggerInteraction.UseGlobal);
                foreach (Collider collider in collidersArray)
                {
                    CharacterBody body = collider.GetComponent<HurtBox>() ? collider.GetComponent<HurtBox>().healthComponent.body : null;
                    if (body && body.teamComponent.teamIndex != characterBody.teamComponent.teamIndex && !bodies.Contains(body))
                    {
                        bodies.Add(body);
                    }
                }
                if (bodies.Count > 0)
                {
                    stopwatch = 0.5f;
                }
                else
                {
                    outer.SetNextStateToMain();
                }
            }
            public override void OnExit()
            {
                base.OnExit();
                if (previousStateMachine) previousStateMachine.SetNextStateToMain();
            }
            public override void FixedUpdate()
            {
                base.FixedUpdate();
                stopwatch -= Time.fixedDeltaTime;
                if (stopwatch < 0 && stage == 0)
                {
                    for (int i = 0; i < bodies.Count; i++)
                    {
                        CharacterBody enemyBody = bodies[i];
                        Vector3 enemyBodyPosition = enemyBody.mainHurtBox.transform.position;
                        Vector3 previousBodyPosition = previousBody ? previousBody.mainHurtBox.transform.position : characterBody.aimOrigin;
                        if (previousBody)
                        {
                            position = previousBodyPosition;
                            direction = enemyBody.mainHurtBox.transform.position - previousBodyPosition;
                        }
                        else
                        {
                            position = characterBody.aimOrigin;
                            direction = enemyBody.mainHurtBox.transform.position - characterBody.aimOrigin;
                        }

                        Action(enemyBody, enemyBodyPosition, previousBodyPosition);
                        previousBody = enemyBody;
                    }
                    stage++;
                    outer.SetNextStateToMain();
                }
            }
            public virtual void Action(CharacterBody enemyBody, Vector3 enemyPosition, Vector3 previousPosition)
            {
                SimpleTracer(previousPosition, enemyPosition, 1f);
                SpawnEffect(HitEffect, enemyPosition, false, Quaternion.identity, OneVector(0.7f));
            }
        }
        public class UltraInstinctReworkSword : UltraInstinctRework
        {
            public BulletAttack bulletAttack;
            public override float searchRadius => 24;
            public override void OnEnter()
            {
                base.OnEnter();
                bulletAttack = swordDictionary.ContainsKey(skillLocator.primary.baseSkill) ? swordDictionary[skillLocator.primary.baseSkill].bulletAttack : DefaultSword.bulletAttack;
            }
            public override void Action(CharacterBody enemyBody, Vector3 enemyPosition, Vector3 previousPosition)
            {
                base.Action(enemyBody, enemyPosition, previousPosition);
                if (NetworkServer.active)
                {
                    BulletAttack bulletAttack2 = new BulletAttack()
                    {
                        radius = 0.1f,
                        aimVector = Vector3.up,
                        damage = base.damageStat * (bulletAttack != null ? bulletAttack.damage : 3f),
                        bulletCount = 1,
                        spreadPitchScale = 0f,
                        spreadYawScale = 0f,
                        maxSpread = 0f,
                        minSpread = 0f,
                        maxDistance = 0.1f,
                        allowTrajectoryAimAssist = false,
                        hitMask = LayerIndex.entityPrecise.mask,
                        procCoefficient = bulletAttack != null ? bulletAttack.procCoefficient : 1f,
                        stopperMask = LayerIndex.entityPrecise.mask,
                        damageType = new DamageTypeCombo(DamageType.Generic, DamageTypeExtended.Generic, DamageSource.Special),
                        force = bulletAttack != null ? bulletAttack.force : 1f,
                        falloffModel = BulletAttack.FalloffModel.None,
                        damageColorIndex = DamageColorIndex.Default,
                        isCrit = base.RollCrit(),
                        origin = enemyPosition,
                        owner = gameObject,
                        hitCallback = bulletAttack != null ? bulletAttack.hitCallback : default,

                    };
                    bulletAttack2.Fire();
                }

            }
        }
        public class UltraInstinctReworkSticky : UltraInstinctRework
        {
            public GameObject projectile;
            public float damage;
            public DemoComponent demoComponent;
            public override float searchRadius => 24f;
            public override void OnEnter()
            {
                base.OnEnter();
                DemoStickyClass demoSticky = bombProjectiles.ContainsKey(skillLocator.primary.baseSkill) ? bombProjectiles[skillLocator.primary.baseSkill] : null;
                projectile = demoSticky != null ? demoSticky.stickyObject : LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/FireMeatBall");
                damage = demoSticky != null ? demoSticky.stickyState.damage : 4;
                demoComponent = GetComponent<DemoComponent>();
                if (demoComponent)
                {
                    demoComponent.DetonateNoLimitStickies();
                    demoComponent.noLimitStickies++;
                }
            }
            public override void OnExit()
            {
                base.OnExit();
                if (demoComponent)
                {
                    demoComponent.noLimitStickies--;
                }
            }
            public override void Action(CharacterBody enemyBody, Vector3 enemyPosition, Vector3 previousPosition)
            {
                base.Action(enemyBody, enemyPosition, previousPosition);
                if (NetworkServer.active)
                {
                    FireProjectileInfo fireProjectileInfo = new FireProjectileInfo
                    {
                        projectilePrefab = projectile,
                        position = enemyPosition,
                        rotation = Util.QuaternionSafeLookRotation(Vector3.up),
                        procChainMask = default,
                        owner = gameObject,
                        damage = base.damageStat * damage,
                        crit = RollCrit(),
                        force = 200f,
                        damageColorIndex = DamageColorIndex.Default,
                        damageTypeOverride = new DamageTypeCombo?(new DamageTypeCombo(DamageType.Generic, DamageTypeExtended.Generic, DamageSource.Special)),
                        speedOverride = 1,
                        useSpeedOverride = true
                    };
                    ProjectileManager.instance.FireProjectile(fireProjectileInfo);
                }

            }
        }
        public abstract class UltraInstinctState : BaseState, ISkillState
        {
            public List<CharacterBody> bodyList = new List<CharacterBody>();
            public CharacterBody currentTarget;
            public Vector3 targetDirection;
            public Vector3 initialPosition;
            public Vector3 previousPosition;
            public Vector3 targetPosition;
            private bool returning = false;
            private float stopwatch = 0f;
            private float stopwatch2 = 0f;
            public abstract float speed { get; }
            public abstract float searchRadius { get; }
            public abstract bool overcharge { get; }
            private float speed2 = 0f;
            private int phase = 0;
            public EntityStateMachine previousStateMachine;

            public GenericSkill activatorSkillSlot { get; set; }
            public virtual void OnFound()
            {

            }
            public virtual void OnContact()
            {

            }
            public override InterruptPriority GetMinimumInterruptPriority()
            {
                return InterruptPriority.Skill;
            }
            public override void OnEnter()
            {
                base.OnEnter();
                if (NetworkServer.active)
                    characterBody.AddBuff(RoR2Content.Buffs.SmallArmorBoost);
                if (isAuthority)
                {

                    FindTargets();

                    if (bodyList.Count <= 0)
                    {
                        outer.SetNextStateToMain();
                        return;
                    }
                    OnFound();
                    stopwatch = 1f / attackSpeedStat;
                    speed2 = speed * base.attackSpeedStat;
                }

            }
            public void FindTargets()
            {
                Collider[] collidersArray = Physics.OverlapSphere(transform.position, searchRadius, LayerIndex.entityPrecise.mask, QueryTriggerInteraction.UseGlobal);
                foreach (Collider collider in collidersArray)
                {
                    CharacterBody body = collider.GetComponent<HurtBox>() ? collider.GetComponent<HurtBox>().healthComponent.body : null;
                    if (body && body.teamComponent.teamIndex != characterBody.teamComponent.teamIndex && !bodyList.Contains(body))
                    {
                        bodyList.Add(body);
                    }
                }
            }
            public void Update1()
            {

            }
            public virtual void Swap()
            {
                phase++;
                stopwatch = -69f;
                currentTarget = bodyList.Count > 0 ? bodyList.FirstOrDefault() : null;
                targetDirection = currentTarget ? currentTarget.transform.position - transform.position : Vector3.zero;
                initialPosition = transform.position;
                if (characterMotor) characterMotor.useGravity = false;
                Calculate();
            }
            public void Calculate()
            {
                bodyList.RemoveAll(s => s == null);
                if (bodyList.Count <= 0)
                {
                    returning = true;

                }
                previousPosition = transform.position;
                //if (colliders.Count <= 0) outer.SetNextStateToMain();
                if (!returning)
                {
                    currentTarget = bodyList.FirstOrDefault();
                    if (currentTarget == null)
                    {

                    }
                    else
                    {
                        targetPosition = currentTarget.transform.position;
                    }

                }
                else
                {
                    targetPosition = initialPosition;

                }
                targetDirection = targetPosition - transform.position;
                stopwatch2 = targetDirection.magnitude / speed2;
                PlayAnimation("FullBody, Override", "Ball", "Slash.playbackRate", stopwatch2);
            }
            public void Update2()
            {
                stopwatch2 -= Time.fixedDeltaTime;
                if (stopwatch2 < 0)
                {
                    if (!returning)
                    {
                        if (currentTarget && bodyList.Contains(currentTarget))
                        {
                            if (bodyList.Contains(currentTarget)) bodyList.Remove(currentTarget);
                            //if (isAuthority)
                            OnContact();
                        }

                        Calculate();
                    }
                    else if (isAuthority && overcharge && activatorSkillSlot.stock > 0)
                    {
                        FindTargets();
                        if (bodyList.Count <= 0)
                        {
                            outer.SetNextStateToMain();
                            return;
                        }
                        else if (!returning)
                        {
                            activatorSkillSlot.stock--;
                        }
                    }
                    else
                    {
                        outer.SetNextStateToMain();
                    }

                }
                else
                {

                    if (characterMotor) characterMotor.rootMotion += targetDirection.normalized * speed * GetDeltaTime();
                }
            }
            public override void FixedUpdate()
            {
                base.FixedUpdate();
                if (true)
                {
                    if (stopwatch > 0f) stopwatch -= GetDeltaTime();
                    if (stopwatch <= 0f && phase == 0)
                        Swap();
                    if (stopwatch > 0f)
                    {
                        Update1();
                    }
                    else
                    {
                        Update2();
                    }


                }
            }
            public override void OnExit()
            {
                base.OnExit();
                if (isAuthority)
                {
                    if (characterMotor) characterMotor.useGravity = true;
                }
                if (NetworkServer.active)
                    characterBody.RemoveBuff(RoR2Content.Buffs.SmallArmorBoost);
                if (previousStateMachine) previousStateMachine.SetNextStateToMain();
            }
            public override void Update()
            {
                base.Update();
                if (isAuthority && characterDirection) characterDirection.forward = targetDirection.normalized;
            }
        }
        public class UltraInstinctRedirector : BaseState, ISkillState
        {
            public GenericSkill activatorSkillSlot { get; set; }
            public override void OnEnter()
            {
                base.OnEnter();
                DemoComponent demoComponent = GetComponent<DemoComponent>();
                EntityStateMachine bodyStateMachine = GetComponent<EntityStateMachine>();
                if (isAuthority)
                {
                    if (demoComponent && demoComponent.isSwapped)
                    {
                        bodyStateMachine.SetNextState(new UltraInstinctReworkSticky
                        {
                            previousStateMachine = outer
                        });
                    }
                    else
                    {
                        bodyStateMachine.SetNextState(new UltraInstinctReworkSword
                        {
                            previousStateMachine = outer
                        });
                    }
                }

            }
        }
        public class UltraInstinctSword : UltraInstinctState
        {
            public override float speed => 96;

            public override bool overcharge => true;

            public override float searchRadius => 24f;
            private BulletAttack bulletAttack;
            public override void OnEnter()
            {
                base.OnEnter();
                if (characterMotor) characterMotor.velocity = new Vector3(0, 24, 0);
            }
            public override void OnFound()
            {
                base.OnFound();
                bulletAttack = swordDictionary.ContainsKey(skillLocator.primary.baseSkill) ? swordDictionary[skillLocator.primary.baseSkill].bulletAttack : DefaultSword.bulletAttack;
            }
            public override void OnContact()
            {
                base.OnContact();
                BulletAttack bulletAttack2 = new BulletAttack()
                {
                    radius = 0.1f,
                    aimVector = Vector3.up,
                    damage = base.damageStat * (bulletAttack != null ? bulletAttack.damage : 3f),
                    bulletCount = 1,
                    spreadPitchScale = 0f,
                    spreadYawScale = 0f,
                    maxSpread = 0f,
                    minSpread = 0f,
                    maxDistance = 0.1f,
                    allowTrajectoryAimAssist = false,
                    hitMask = LayerIndex.entityPrecise.mask,
                    procCoefficient = bulletAttack != null ? bulletAttack.procCoefficient : 1f,
                    stopperMask = LayerIndex.entityPrecise.mask,
                    damageType = new DamageTypeCombo(DamageType.Generic, DamageTypeExtended.Generic, DamageSource.Special),
                    force = bulletAttack != null ? bulletAttack.force : 1f,
                    falloffModel = BulletAttack.FalloffModel.None,
                    damageColorIndex = DamageColorIndex.Default,
                    isCrit = base.RollCrit(),
                    origin = currentTarget.transform.position,
                    owner = gameObject,
                    hitCallback = bulletAttack != null ? bulletAttack.hitCallback : default,

                };
                bulletAttack2.Fire();
            }
        }
        public class UltraInstinctSticky : UltraInstinctState
        {
            public override float speed => 96;

            public override bool overcharge => true;

            public override float searchRadius => 24f;
            private GameObject projectile;
            private DemoComponent demoComponent;
            private float damage;
            public override void OnEnter()
            {
                base.OnEnter();
                DemoStickyClass demoSticky = bombProjectiles.ContainsKey(skillLocator.primary.baseSkill) ? bombProjectiles[skillLocator.primary.baseSkill] : null;
                projectile = demoSticky != null ? demoSticky.stickyObject : LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/FireMeatBall");
                damage = demoSticky != null ? demoSticky.stickyState.damage : 4;
                demoComponent = GetComponent<DemoComponent>();
                if (demoComponent)
                {
                    demoComponent.noLimitStickies++;
                }
                if (characterMotor) characterMotor.velocity = new Vector3(0, 24, 0);
            }
            public override void OnExit()
            {
                base.OnExit();
                if (demoComponent)
                {
                    demoComponent.noLimitStickies--;
                }
            }
            public override void OnContact()
            {
                base.OnContact();
                FireProjectileInfo fireProjectileInfo = new FireProjectileInfo
                {
                    projectilePrefab = projectile,
                    position = currentTarget.hurtBoxGroup.mainHurtBox.gameObject.transform.position,
                    rotation = Util.QuaternionSafeLookRotation(Vector3.up),
                    procChainMask = default,
                    owner = gameObject,
                    damage = base.damageStat * damage,
                    crit = RollCrit(),
                    force = 200f,
                    damageColorIndex = DamageColorIndex.Default,
                    damageTypeOverride = new DamageTypeCombo?(new DamageTypeCombo(DamageType.Generic, DamageTypeExtended.Generic, DamageSource.Special)),
                    speedOverride = 1,
                    useSpeedOverride = true
                };
                ProjectileManager.instance.FireProjectile(fireProjectileInfo);
            }
        }/*
    public class SpecialTwoAlt : BaseState
    {
        private BoxCollider collider;
        public override void OnEnter()
        {
            base.OnEnter();
            if (isAuthority)
            {
                FireProjectileInfo fireProjectileInfo = new FireProjectileInfo
                {
                    projectilePrefab = bombProjectiles.ContainsKey(skillLocator.primary.skillDef) ? bombProjectiles[skillLocator.primary.skillDef] : LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/FireMeatBall"),
                    position = RandomPointInBounds(transform.position + new Vector3(0f, 16f, 0f)),
                    rotation = Quaternion.LookRotation(Vector3.down),
                    procChainMask = default,
                    owner = gameObject,
                    damage = base.damageStat * 4f,
                    crit = RollCrit(),
                    force = 200f,
                    damageColorIndex = DamageColorIndex.Default,
                    damageTypeOverride = new DamageTypeCombo?(new DamageTypeCombo(DamageType.Generic, DamageTypeExtended.Generic, DamageSource.Special)),
                };
                for (int i = 0; i < 64; i++)
                {
                    fireProjectileInfo.position = RandomPointInBounds(transform.position + new Vector3(0f, 16f, 0f));

                    ProjectileManager.instance.FireProjectile(fireProjectileInfo);
                }
                outer.SetNextStateToMain();
            }

        }
        public static Vector3 RandomPointInBounds(Vector3 point)
        {
            return new Vector3(
                UnityEngine.Random.Range(point.x - 28, point.x + 28),
                UnityEngine.Random.Range(point.y - 4, point.y + 4),
                UnityEngine.Random.Range(point.z - 28, point.z + 28)
            );
        }
        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Skill;
        }
    }*/
        public class Slamming : BaseState
        {
            public float height;
            private float stopwatch = 0f;
            public override void OnDeserialize(NetworkReader reader)
            {
                base.OnDeserialize(reader);
                height = reader.ReadSingle();
            }
            public override void OnSerialize(NetworkWriter writer)
            {
                base.OnSerialize(writer);
                writer.Write(height);
            }
            public override void OnEnter()
            {
                base.OnEnter();
                SpawnEffect(SlamEffect, characterBody.footPosition, false, Quaternion.identity, new Vector3(24f, 1f, 24f));
                Collider[] collidersArray = Physics.OverlapSphere(transform.position, 24f, LayerIndex.entityPrecise.mask, QueryTriggerInteraction.UseGlobal);
                List<CharacterBody> bodies = new List<CharacterBody>();
                foreach (Collider collider in collidersArray)
                {
                    CharacterBody body = collider.GetComponent<HurtBox>() ? collider.GetComponent<HurtBox>().healthComponent.body : null;
                    if (body && body.teamComponent.teamIndex != characterBody.teamComponent.teamIndex && !bodies.Contains(body))
                    {
                        bodies.Add(body);
                    }
                }
                foreach (CharacterBody body in bodies)
                {
                    if (body.characterMotor)
                    {
                        if (body.characterMotor.Motor)
                            body.characterMotor.Motor.ForceUnground(0f);
                        body.characterMotor.velocity.y = height + 5f;
                    }
                    else if (body.rigidbody)
                    {
                        Vector3 rigidbody = body.rigidbody.velocity;
                        rigidbody.y = height + 5f;
                    }

                    if (NetworkServer.active)
                    {
                        body.AddBuff(AfterSlam);
                        body.AddBuff(DisableInputs);
                    }

                }

                stopwatch = 0.5f;
            }
            public override void FixedUpdate()
            {
                base.FixedUpdate();
                if (stopwatch < 0f && isAuthority) outer.SetNextStateToMain();
            }
            public override void Update()
            {
                base.Update();
                if (stopwatch > 0 && characterMotor && inputBank && inputBank.jump.justPressed)
                {
                    SpawnEffect(SlamEffect, characterBody.footPosition, false, Quaternion.identity, new Vector3(1f, 1f, 1f));
                    if (characterMotor.Motor)
                        characterMotor.Motor.ForceUnground(0f);
                    characterMotor.velocity.y = height + 5f;
                    characterBody.AddBuffAuthotiry(AfterSlam);
                    characterBody.AddTimedBuffAuthotiry(AfterSlam, 1, 0.5f);
                    if (isAuthority)
                        outer.SetNextStateToMain();
                }
            }
        }
        public class Slam : BaseState
        {
            private float height;
            private float radius = 24f;
            public override void OnEnter()
            {
                base.OnEnter();
                if (isAuthority)
                {
                    if (characterMotor && !characterMotor.isGrounded)
                    {
                        RaycastHit raycastHit = new RaycastHit();
                        Ray ray = new Ray
                        {
                            origin = transform.position,
                            direction = Physics.gravity,
                        };
                        height = Util.CharacterRaycast(gameObject, ray, out raycastHit, 9999f, LayerIndex.world.mask, QueryTriggerInteraction.UseGlobal) ? (raycastHit.point - characterBody.footPosition).magnitude + ((Physics.gravity * -1).magnitude) : 0;
                        if (characterMotor)
                        {
                            if (characterMotor.velocity.y < 0) characterMotor.velocity.y = 0;
                            characterMotor.velocity.y += 42f;
                        }

                    }
                    else
                    {
                        outer.SetNextStateToMain();
                    }
                }

            }
            public override void FixedUpdate()
            {
                base.FixedUpdate();
                if (true)
                {
                    if (characterMotor)
                    {
                        characterMotor.velocity += Physics.gravity * 3 * GetDeltaTime();
                        if (characterMotor.isGrounded)
                        {
                            outer.SetNextState(new Slamming { height = height });
                        }
                    }

                    
                }

            }
            public override void Update()
            {
                base.Update();
                
            }
        }
        public class ToolGun : BaseState
        {
            public DemoComponent demoComponent;
            public override void OnEnter()
            {
                base.OnEnter();
                demoComponent = GetComponent<DemoComponent>();
                if (demoComponent && demoComponent.currentProp != null)
                {
                    RaycastHit hit = new RaycastHit();
                    if (Physics.Raycast(inputBank.aimOrigin, inputBank.aimDirection, out hit, 9999f, LayerIndex.world.mask, QueryTriggerInteraction.UseGlobal))
                    {
                        GameObject placedObject = GameObject.Instantiate(demoComponent.currentProp.gameObject);
                        placedObject.transform.position = hit.point;
                        if (demoComponent.currentProp.placeObjectDelegate != null)
                        {
                            demoComponent.currentProp.placeObjectDelegate(placedObject, hit.collider, hit, gameObject);
                        }
                        
                    }
                }
                outer.SetNextStateToMain();
            }
        }
        public class ChangeWeapons : BaseState
        {
            //private Main.DemoComponent demoComponent;
            private float stopwatch = 0;
            private bool fired = false;
            public override void OnEnter()
            {
                base.OnEnter();
                DemoComponent demoComponent = GetComponent<DemoComponent>();
                if (skillLocator && demoComponent != null)
                {
                    GenericSkill genericSkill2 = skillLocator.primary;
                    skillLocator.primary = demoComponent.primaryReplace;
                    demoComponent.primaryReplace = genericSkill2;
                    GenericSkill genericSkill4 = skillLocator.utility;
                    skillLocator.utility = demoComponent.utilityReplace;
                    demoComponent.utilityReplace = genericSkill4;
                    demoComponent.canUseUtilityTimer = 0.1f;
                    demoComponent.canUseUtility = false;
                    demoComponent.canUseSecondaryTimer = 0.1f;
                    demoComponent.UpdateHudObject();

                }
                outer.SetNextStateToMain();
            }
            public override void OnExit()
            {
                base.OnExit();
                characterBody.RecalculateStats();
                characterBody.OnInventoryChanged();
            }
            public override InterruptPriority GetMinimumInterruptPriority()
            {
                return InterruptPriority.Skill;
            }
        }
        public class DemoDeathState : GenericCharacterDeath
        {
            public bool gone = false;
            public CharacterModel characterModel;
            public override void OnEnter()
            {
                base.OnEnter();
                Vector3 vector = Vector3.up * 3f;
                if (base.characterMotor)
                {
                    vector += base.characterMotor.velocity;
                    base.characterMotor.enabled = false;
                }
                if (base.cachedModelTransform)
                {
                    characterModel = cachedModelTransform.GetComponent<CharacterModel>();
                    RagdollController component = base.cachedModelTransform.GetComponent<RagdollController>();
                    if (component)
                    {
                        component.BeginRagdoll(vector);
                    }
                }
            }
            public override void PlayDeathAnimation(float crossfadeDuration = 0.1f)
            {
            }
            public override bool shouldAutoDestroy
            {
                get
                {
                    return false;
                }
            }
            public override void FixedUpdate()
            {
                base.FixedUpdate();
                if (fixedAge > 2f && !gone)
                {
                    gone = true;
                    characterModel.invisibilityCount++;
                    SpawnEffect(HitEffect, transform.position, false, Quaternion.identity, OneVector(1f));
                }
                if (NetworkServer.active && base.fixedAge > 4f)
                {
                    EntityState.Destroy(base.gameObject);
                }
            }
            public override InterruptPriority GetMinimumInterruptPriority()
            {
                return InterruptPriority.Death;
            }
            private Vector3 previousPosition;
            private float upSpeedVelocity;
            private float upSpeed;
            private Animator modelAnimator;
        }


    }
    public class ContentPacks : IContentPackProvider
    {
        internal ContentPack contentPack = new ContentPack();
        public string identifier => Main.ModGuid + ".ContentProvider";
        public static List<GameObject> bodies = new List<GameObject>();
        public static List<BuffDef> buffs = new List<BuffDef>();
        public static List<SkillDef> skills = new List<SkillDef>();
        public static List<SkillFamily> skillFamilies = new List<SkillFamily>();
        public static List<GameObject> projectiles = new List<GameObject>();
        public static List<GameObject> networkPrefabs = new List<GameObject>();
        public static List<SurvivorDef> survivors = new List<SurvivorDef>();
        public static List<Type> states = new List<Type>();
        public static List<NetworkSoundEventDef> sounds = new List<NetworkSoundEventDef>();
        public IEnumerator FinalizeAsync(FinalizeAsyncArgs args)
        {
            args.ReportProgress(1f);
            yield break;
        }

        public IEnumerator GenerateContentPackAsync(GetContentPackAsyncArgs args)
        {
            ContentPack.Copy(this.contentPack, args.output);
            args.ReportProgress(1f);
            yield break;
        }

        public IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
        {
            this.contentPack.identifier = this.identifier;
            contentPack.skillDefs.Add(skills.ToArray());
            contentPack.skillFamilies.Add(skillFamilies.ToArray());
            contentPack.bodyPrefabs.Add(bodies.ToArray());
            contentPack.buffDefs.Add(buffs.ToArray());
            contentPack.projectilePrefabs.Add(projectiles.ToArray());
            contentPack.survivorDefs.Add(survivors.ToArray());
            contentPack.entityStateTypes.Add(states.ToArray());
            contentPack.networkSoundEventDefs.Add(sounds.ToArray());
            contentPack.networkedObjectPrefabs.Add(networkPrefabs.ToArray());
            yield break;
        }
    }

}

public static class EmoteCompatAbility
{
    public const string customEmotesApiGUID = "com.weliveinasociety.CustomEmotesAPI";
    public static void EmoteCompatability()
    {
        string path = "Assets/Demoman/DemoEmotes.prefab";
        var skele = ThunderkitAssets.LoadAsset<GameObject>(path);
        skele.AddComponent<DemoEmotesComponent>();
        EmotesAPI.CustomEmotesAPI.ImportArmature(DemoBody, skele);
        skele.GetComponentInChildren<BoneMapper>().scale = 1f;
        skele.transform.localPosition = new Vector3(0f, -0f, 0f);
        skele.transform.localRotation = Quaternion.identity;
        EmotesAPI.CustomEmotesAPI.animChanged += CustomEmotesAPI_animChanged;
    }
    private static void CustomEmotesAPI_animChanged(string newAnimation, BoneMapper mapper)
    {
        if (mapper.transform.name == "DemoEmotes")
        {
            DemoEmotesComponent demoEmotesComponent = mapper.GetComponent<DemoEmotesComponent>();
            ChildLocator childLocator = mapper.transform.parent.GetComponent<ChildLocator>();
            ChildLocator childLocator2 = mapper.GetComponent<ChildLocator>();
            //Transform smokes = childLocator.FindChild("Smokes");
            //Transform smokes2 = childLocator2.FindChild("Smokes");
            Transform weaponR = childLocator.FindChild("WeaponR");
            Transform weaponL = childLocator.FindChild("WeaponL");
            Transform shield = childLocator.FindChild("Shield");
            if (newAnimation != "none")
            {
                //smokes2.gameObject.SetActive(true);
                //smokes.gameObject.SetActive(false);
                weaponR.gameObject.SetActive(false);
                weaponL.gameObject.SetActive(false);
                shield.gameObject.SetActive(false);
                for (int i = 0; i < demoEmotesComponent.customSpaceParticles.Count; i++)
                {
                    ParticleSystem.MainModule mainModule = demoEmotesComponent.customSpaceParticles[i];
                    mainModule.simulationSpace = ParticleSystemSimulationSpace.Local;
                }
            }
            else
            {
                //smokes2.gameObject.SetActive(false);
                //smokes.gameObject.SetActive(true);
                weaponR.gameObject.SetActive(true);
                weaponL.gameObject.SetActive(true);
                shield.gameObject.SetActive(true);
                for (int i = 0; i < demoEmotesComponent.customSpaceParticles.Count; i++)
                {
                    ParticleSystem.MainModule mainModule = demoEmotesComponent.customSpaceParticles[i];
                    mainModule.simulationSpace = ParticleSystemSimulationSpace.Custom;
                }
            }
        }
        
    }
    public class DemoEmotesComponent : MonoBehaviour
    {
        public List<ParticleSystem.MainModule> customSpaceParticles = new List<ParticleSystem.MainModule>();
    }
}
public static class Extensions
{
    public static void AddBuffAuthotiry(this CharacterBody characterBody, BuffDef buffDef)
    {
        characterBody.AddBuffAuthotiry(buffDef.buffIndex);
    }
    public static void AddBuffAuthotiry(this CharacterBody characterBody, BuffIndex buffIndex)
    {
        characterBody.AddOrRemoveBuffAuthotiry(buffIndex, 1);
    }
    public static void RemoveBuffAuthotiry(this CharacterBody characterBody, BuffDef buffDef)
    {
        characterBody.RemoveBuffAuthotiry(buffDef.buffIndex);
    }
    public static void RemoveBuffAuthotiry(this CharacterBody characterBody, BuffIndex buffIndex)
    {
        characterBody.AddOrRemoveBuffAuthotiry(buffIndex, -1);
    }
    public static void AddOrRemoveBuffAuthotiry(this CharacterBody characterBody, BuffDef buffDef, int amount)
    {
        characterBody.AddOrRemoveBuffAuthotiry(buffDef.buffIndex, amount);
    }
    public static void AddOrRemoveBuffAuthotiry(this CharacterBody characterBody, BuffIndex buffIndex, int amount)
    {
        new AddBuffNetMessage(characterBody.netId, buffIndex, amount, -1f).Send(NetworkDestination.Server);
    }
    public static void AddTimedBuffAuthotiry(this CharacterBody characterBody, BuffDef buffDef, int amount, float duration)
    {
        characterBody.AddTimedBuffAuthotiry(buffDef.buffIndex, amount, duration);
    }
    public static void AddTimedBuffAuthotiry(this CharacterBody characterBody, BuffIndex buffIndex, int amount, float duration)
    {
        new AddBuffNetMessage(characterBody.netId, buffIndex, amount, duration).Send(NetworkDestination.Server);
    }
}