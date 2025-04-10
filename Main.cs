using BepInEx;
using RoR2;
using R2API;
using R2API.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using System.Security.Permissions;
using RoR2.Projectile;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;
using static RoR2.VFXAttributes;
using System.IO;
using RoR2.UI;
using static ak.wwise.core;
using static RoR2.Console;
using TMPro;
using UnityEngine.UI;
using HG;
using RoR2.Navigation;
using R2API.Networking;
using EmotesAPI;
using RoR2.ContentManagement;
using static ak.wwise;
using System.Collections;
using EntityStates;
using RoR2.Skills;
using DemomanRor2;
using static DemomanRor2.Skills;
using static DemomanRor2.Main;
using static Rewired.InputMapper;
using Newtonsoft.Json.Utilities;
using static Rewired.Demos.GamepadTemplateUI.GamepadTemplateUI;
using static UnityEngine.SendMouseEvents;
using RoR2.HudOverlay;
using RoR2.ExpansionManagement;
using System.Runtime.CompilerServices;
using static Rewired.ComponentControls.Effects.RotateAroundAxis;
using KinematicCharacterController;
using static UnityEngine.UI.GridLayoutGroup;
using static MonoMod.InlineRT.MonoModRule;
using EntityStates.VoidRaidCrab.Leg;
using UnityEngine.Bindings;
using System.Security;
using LoadoutSkillTitles;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using static R2API.SoundAPI.Music.CustomMusicTrackDef;
using UnityEngine.PlayerLoop;
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[assembly: HG.Reflection.SearchableAttribute.OptIn]
[assembly: HG.Reflection.SearchableAttribute.OptInAttribute]
[module: UnverifiableCode]
#pragma warning disable CS0618 // Type or member is obsolete
#pragma warning restore CS0618 // Type or member is obsolete
namespace DemomanRor2
{
    [BepInPlugin(ModGuid, ModName, ModVer)]
    [BepInDependency(R2API.R2API.PluginGUID, R2API.R2API.PluginVersion)]
    [BepInDependency("com.TheTimeSweeper.LoadoutSkillTitles")]
    [BepInDependency("com.weliveinasociety.CustomEmotesAPI", BepInDependency.DependencyFlags.SoftDependency)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    //[R2APISubmoduleDependency(nameof(CommandHelper))]
    [System.Serializable]
    public class Main : BaseUnityPlugin
    {
        public const string ModGuid = "com.brynzananas.demoman";
        public const string ModName = "Demoman";
        public const string ModVer = "1.0.0";
        public static PluginInfo PInfo { get; private set; }
        public static AssetBundle ThunderkitAssets;
        public static AssetBundle UnityAssets;
        public static BepInEx.Logging.ManualLogSource ModLogger;

        public static Dictionary<string, string> ShaderLookup4 = new Dictionary<string, string>()
        {
            {"stubbedror2/base/shaders/hgstandard", "shaders/deferred/hgstandard"},
            {"stubbedror2/base/shaders/hgintersectioncloudremap", "shaders/fx/hgintersectioncloudremap" },
            {"stubbedror2/base/shaders/hgcloudremap", "shaders/fx/hgcloudremap" },
            {"stubbedror2/base/shaders/hgdistortion", "shaders/fx/hgdistortion" },
            {"stubbedror2/base/shaders/hgsnowtopped", "shaders/deferred/hgsnowtopped" },
            {"stubbedror2/base/shaders/hgsolidparallax", "shaders/fx/hgsolidparallax" }
        };

        public void Awake()
        {
            PInfo = Info;
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
                    Texture texture2D = null;
                    if (shaderName == "RoR2/Base/Shaders/HGCloudRemap.shader")
                    {
                        texture2D = material.GetTexture("_MainTex");
                    }
                    material.shader = replacementShader;
                    if (shaderName == "RoR2/Base/Shaders/HGCloudRemap.shader")
                    {
                        //material.SetTexture(shaderName, texture2D);
                    }
                    
                }
                //var replacementShader = Resources.Load<Shader>(ShaderLookup4[material.shader.name.ToLower()]);
                //if (replacementShader)
                //{
                //    material.shader = replacementShader;
                //}
            }
            foreach (SkillFamily skillFamily in ThunderkitAssets.LoadAllAssets<SkillFamily>())
            {
                (skillFamily as ScriptableObject).name = (skillFamily as UnityEngine.Object).name;
                ContentAddition.AddSkillFamily(skillFamily);
            }
            CreateAssets();
            CreateSurvivor();
            CreateSounds();
            Skills.Init();
            skillsToStatsEvent += Main_skillsToStatsEvent;
            onProjectileFiredEvent += Main_onProjectileFiredEvent;
            On.RoR2.CharacterBody.OnKilledOtherServer += CharacterBody_OnKilledOtherServer;
            On.RoR2.HealthComponent.TakeDamageProcess += HealthComponent_TakeDamageProcess;
            //On.RoR2.SurvivorCatalog.Init += SurvivorCatalog_Init;
            On.RoR2.CharacterMotor.FixedUpdate += CharacterMotor_FixedUpdate;
            On.RoR2.CharacterMotor.OnLanded += CharacterMotor_OnLanded;
            //On.RoR2.Projectile.ProjectileManager.FireProjectileServer += ProjectileManager_FireProjectileServer;
            //On.RoR2.BulletAttack.FireMulti += BulletAttack_FireMulti;
            //On.RoR2.BulletAttack.FireSingle += BulletAttack_FireSingle;
            //On.RoR2.BulletAttack.FireSingle_ReturnHit += BulletAttack_FireSingle_ReturnHit;
            //On.RoR2.BulletAttack.Fire_ReturnHit += BulletAttack_Fire_ReturnHit;
            On.RoR2.CharacterBody.OnBuffFirstStackGained += CharacterBody_OnBuffFirstStackGained;
            On.RoR2.CharacterBody.OnBuffFinalStackLost += CharacterBody_OnBuffFinalStackLost;
            R2API.RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
            //On.RoR2.Projectile.ProjectileManager.InitializeProjectile += ProjectileManager_InitializeProjectile;
            //IL.RoR2.CharacterBody.RecalculateStats += CharacterBody_RecalculateStats;
            //On.RoR2.CharacterModel.UpdateMaterials += CharacterModel_UpdateMaterials;
            On.RoR2.GlobalEventManager.IsImmuneToFallDamage += GlobalEventManager_IsImmuneToFallDamage;
        }

        private bool GlobalEventManager_IsImmuneToFallDamage(On.RoR2.GlobalEventManager.orig_IsImmuneToFallDamage orig, GlobalEventManager self, CharacterBody body)
        {
            if (BodyCatalog.GetBodyName(body.bodyIndex) == "DemoBody") return true;
            return orig(self, body);
        }

        private void CharacterModel_UpdateMaterials(On.RoR2.CharacterModel.orig_UpdateMaterials orig, CharacterModel self)
        {
            orig(self);
            DemoComponent demoComponent = self.body ? self.body.GetComponent<DemoComponent>() : null;
            if (demoComponent != null)
            {
                foreach (Renderer renderer in demoComponent.renderers)
                {
                    self.UpdateRendererMaterials(renderer, renderer.material, false);
                }
                
            }
        }

        private void CharacterBody_RecalculateStats(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            c.GotoNext(
                    x => x.MatchLdarg(0),
                    x => x.MatchCall<CharacterBody>("get_bleedChance"),
                    x => x.MatchLdcR4(10),
                    x => x.MatchAdd(),
                    x => x.MatchCall<CharacterBody>("set_bleedChance"),
                    x => x.MatchLdarg(0)
                    );
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Action<CharacterBody>>(SetDemoStats);
        }

        private void SetDemoStats(CharacterBody body)
        {
            Debug.Log("ItWorks");
            DemoComponent demoComponent = body.GetComponent<DemoComponent>();
            if (demoComponent)
            {

            }
        }

        private void ProjectileManager_InitializeProjectile(On.RoR2.Projectile.ProjectileManager.orig_InitializeProjectile orig, ProjectileController projectileController, FireProjectileInfo fireProjectileInfo)
        {
            orig(projectileController, fireProjectileInfo);
            GameObject projectilePrefab = ProjectileCatalog.GetProjectilePrefab(ProjectileCatalog.GetProjectileIndex(projectileController));
            GameObject owner = projectileController.owner;
            EntityStateMachine[] entityStateMachines = owner ? owner.GetComponents<EntityStateMachine>() : null;
            if (entityStateMachines != null)
            {
                EntityStateMachine entityStateMachine = null;
                foreach (var stateMachine in entityStateMachines)
                {
                    onProjectileFiredEvent(projectileController, fireProjectileInfo, projectilePrefab, stateMachine.state);
                }
            }

        }

        private void Main_onProjectileFiredEvent(ProjectileController projectileController, FireProjectileInfo fireProjectileInfo, GameObject projectilePrefab, EntityState entityState)
        {
            if (projectilePrefab == BombProjectile && entityState is BombLauncher)
            {
                BombLauncher bombLauncher = (entityState as BombLauncher);
                ProjectileImpactExplosion projectileImpactExplosion = GetComponent<ProjectileImpactExplosion>();
                if (projectileImpactExplosion)
                {
                    projectileImpactExplosion.lifetime = projectileImpactExplosion.lifetime * (1 - (bombLauncher.charge / bombLauncher.chargeCap));
                }

            }

        }

        private void Main_skillsToStatsEvent(CharacterBody arg1, RecalculateStatsAPI.StatHookEventArgs arg2, GenericSkill arg3)
        {
            DemoComponent demoComponent = arg1.GetComponent<DemoComponent>();
            SkillDef skillDef = arg3.skillDef;
            if (skillDef != null)
            {
                if (skillDef == HeavyShieldSkillDef)
                {
                    arg2.armorAdd += 15;
                }
                if (skillDef == SkullcutterSkillDef)
                {

                    if (demoComponent && demoComponent.isSwapped)
                    {
                    }
                    else
                    {
                        arg2.baseMoveSpeedAdd -= 2;
                    }
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
                    //if (genericSkill.stateMachine != null && genericSkill.stateMachine.state is ShieldCharge)
                    //{
                    //    ShieldCharge shieldCharge = (ShieldCharge)genericSkill.stateMachine.state;
                    //    args.armorAdd += shieldCharge.armor;
                    //}
                }
            }
            //EntityStateMachine[] entityStateMachines = sender.GetComponents<EntityStateMachine>();
            //if (entityStateMachines != null)
            //{
            //    EntityStateMachine entityStateMachine = null;
            //    foreach (var stateMachine in entityStateMachines)
            //    {
            //        if (stateMachine.state is ShieldCharge)
            //        {
            //            ShieldCharge shieldCharge = (ShieldCharge)stateMachine.state;
            //            args.armorAdd += shieldCharge.armor;
            //        }
            //    }
            //}
        }

        private void CharacterBody_OnBuffFinalStackLost(On.RoR2.CharacterBody.orig_OnBuffFinalStackLost orig, CharacterBody self, BuffDef buffDef)
        {
            orig(self, buffDef);
            if (buffDef == VelocityPreserve)
            {
                Transform modelTransform = self.modelLocator.modelTransform;
                ChildLocator childLocator = modelTransform ? modelTransform.GetComponent<ChildLocator>() : null;
                if (childLocator)
                {
                    Transform footR = childLocator.FindChild("FootR");
                    Transform footL = childLocator.FindChild("FootL");

                    if (footR && footR.Find("DemoSmokeFeet"))
                    {
                        do
                        {
                            GameObject smoking = footR.Find("DemoSmokeFeet").gameObject;
                            smoking.name = "gone";
                            ParticleSystem[] particleSystems = smoking.GetComponentsInChildren<ParticleSystem>();
                            foreach (ParticleSystem particleSystem in particleSystems)
                            {
                                particleSystem.enableEmission = false;
                            }
                            Destroy(smoking, 1f);
                        } while (footR.Find("DemoSmokeFeet"));

                    }
                    if (footL && footL.Find("DemoSmokeFeet"))
                    {
                        do
                        {
                            GameObject smoking = footL.Find("DemoSmokeFeet").gameObject;
                            smoking.name = "gone";
                            ParticleSystem[] particleSystems = smoking.GetComponentsInChildren<ParticleSystem>();
                            foreach (ParticleSystem particleSystem in particleSystems)
                            {
                                particleSystem.enableEmission = false;
                            }
                            Destroy(smoking, 1f);
                        } while (footL.Find("DemoSmokeFeet"));
                    }
                }
            }
        }
        private void CharacterBody_OnBuffFirstStackGained(On.RoR2.CharacterBody.orig_OnBuffFirstStackGained orig, CharacterBody self, BuffDef buffDef)
        {
            orig(self, buffDef);
            if (buffDef == VelocityPreserve)
            {
                Transform modelTransform = self.modelLocator.modelTransform;
                ChildLocator childLocator = modelTransform ? modelTransform.GetComponent<ChildLocator>() : null;
                if (childLocator)
                {
                    Transform footR = childLocator.FindChild("FootR");
                    Transform footL = childLocator.FindChild("FootL");
                    if (footR && !footR.Find("DemoSmokeFeet"))
                    {
                        GameObject smoking = Instantiate(SmokeEffect, footR);
                        smoking.transform.rotation = Quaternion.identity;
                        smoking.name = "DemoSmokeFeet";
                    }
                    if (footL && !footL.Find("DemoSmokeFeet"))
                    {
                        GameObject smoking = Instantiate(SmokeEffect, footL);
                        smoking.transform.rotation = Quaternion.identity;
                        smoking.name = "DemoSmokeFeet";
                    }
                }
            }
        }

        //public static event Action<GenericSkill> onRecalculateFinalRechargeInterval;
        //private void GenericSkill_RecalculateFinalRechargeInterval(On.RoR2.GenericSkill.orig_RecalculateFinalRechargeInterval orig, GenericSkill self)
        //{
        //    orig(self);
        //    Action<GenericSkill> action = onRecalculateFinalRechargeInterval;
        //    if (action != null)
        //    {
        //        action(self);
        //    }
        //}

        private void CharacterMotor_OnLanded(On.RoR2.CharacterMotor.orig_OnLanded orig, CharacterMotor self)
        {
            orig(self);
            if (self.body.HasBuff(VelocityPreserve)) self.body.SetBuffCount(VelocityPreserve.buffIndex, 0);
        }

        private Vector3 BulletAttack_Fire_ReturnHit(On.RoR2.BulletAttack.orig_Fire_ReturnHit orig, BulletAttack self)
        {
            IncreaseHitDistance(self);
            return orig(self);
        }

        private Vector3 BulletAttack_FireSingle_ReturnHit(On.RoR2.BulletAttack.orig_FireSingle_ReturnHit orig, BulletAttack self, Vector3 normal, int muzzleIndex)
        {
            IncreaseHitDistance(self);
            return orig(self, normal, muzzleIndex);
        }

        private void BulletAttack_FireSingle(On.RoR2.BulletAttack.orig_FireSingle orig, BulletAttack self, Vector3 normal, int muzzleIndex)
        {
            IncreaseHitDistance(self);
            orig(self, normal, muzzleIndex);
        }

        private void BulletAttack_FireMulti(On.RoR2.BulletAttack.orig_FireMulti orig, BulletAttack self, Vector3 normal, int muzzleIndex)
        {
            IncreaseHitDistance(self);
            orig(self, normal, muzzleIndex);
        }
        private void IncreaseHitDistance(BulletAttack self)
        {
            if (self != null && self.damageType != null && self.damageType.damageSource == DamageSource.SkillMask)
            {
                CharacterBody characterBody = self.owner ? self.owner.GetComponent<CharacterBody>() : null;
                if (characterBody && characterBody.HasBuff(LockedIn))
                {
                    self.maxDistance = 99999f;
                    self.maxSpread = 0f;
                    self.minSpread = 0f;
                    self.falloffModel = BulletAttack.FalloffModel.None;
                    self.isCrit = true;
                    characterBody.RemoveBuff(LockedIn);
                }
            }
        }
        private void ProjectileManager_FireProjectileServer(On.RoR2.Projectile.ProjectileManager.orig_FireProjectileServer orig, ProjectileManager self, FireProjectileInfo fireProjectileInfo, NetworkConnection clientAuthorityOwner, ushort predictionId, double fastForwardTime)
        {
            if (fireProjectileInfo.damageTypeOverride != null && fireProjectileInfo.damageTypeOverride.Value.damageSource == DamageSource.SkillMask)
            {
                CharacterBody characterBody = fireProjectileInfo.owner ? fireProjectileInfo.owner.GetComponent<CharacterBody>() : null;
                if (characterBody && characterBody.HasBuff(LockedIn))
                {
                    fireProjectileInfo.crit = true;
                    RaycastHit raycastHit;
                    if (characterBody.inputBank)
                    {
                        if (characterBody.inputBank.GetAimRaycast(9999, out raycastHit))
                        {
                            fireProjectileInfo.position = raycastHit.point - (characterBody.inputBank.aimDirection * 0.1f);
                            fireProjectileInfo.rotation = Quaternion.LookRotation(characterBody.inputBank.aimDirection);

                        }
                    }
                    characterBody.RemoveBuff(LockedIn);
                }
            }

            orig(self, fireProjectileInfo, clientAuthorityOwner, predictionId, fastForwardTime);

        }

        public event Action<ProjectileController, FireProjectileInfo, GameObject, EntityState> onProjectileFiredEvent;
        //private void CharacterBody_Start(On.RoR2.CharacterBody.orig_Start orig, CharacterBody self)
        //{
        //    orig(self);
        //    if (self.skillLocator && self.skillLocator.FindSkillByDef(Skills.AimAssistSkillDef) && self.inventory.GetItemCount(StompItem) <= 0)
        //    {
        //        self.inventory.GiveItem(StompItem);
        //    }
        //}

        private void CharacterMotor_FixedUpdate(On.RoR2.CharacterMotor.orig_FixedUpdate orig, CharacterMotor self)
        {
            orig(self);
            if (self.body.HasBuff(VelocityPreserve))
            {
                InputBankTest inputBankTest = self.body.inputBank;
                if (inputBankTest)
                {
                    var currentVelocity = new Vector3(self.velocity.x, 0, self.velocity.z);
                    var wishDirection = new Vector3(inputBankTest.moveVector.x, 0, inputBankTest.moveVector.z).normalized;
                    var dotProduct = Vector3.Dot(currentVelocity.normalized, wishDirection);
                    //wishDirection = wishDirection * self.walkSpeed;
                    if (dotProduct < 0.10)
                        self.body.characterMotor.velocity += new Vector3(wishDirection.x * self.walkSpeed * 10, 0, wishDirection.z * self.walkSpeed * 10) * Time.fixedDeltaTime;
                        //self.velocity += inputBankTest.moveVector * self.walkSpeed * Time.fixedDeltaTime;
                }
                
            }
            return;
            if (self.body.skillLocator && self.body.skillLocator.FindSkillByDef(Skills.ManthreadsSkillDef))
            {
                CharacterBody characterBody = self.body;
                Ray ray = new Ray
                {
                    origin = characterBody.footPosition,
                    direction = Physics.gravity,
                };
                BulletAttack bulletAttack2 = new BulletAttack()
                {
                    radius = characterBody.radius,
                    aimVector = ray.direction,
                    damage = characterBody.maxHealth,
                    bulletCount = 1,
                    spreadPitchScale = 0f,
                    spreadYawScale = 0f,
                    maxSpread = 0f,
                    minSpread = 0f,
                    maxDistance = 1f,
                    allowTrajectoryAimAssist = false,
                    hitMask = LayerIndex.entityPrecise.mask,
                    procCoefficient = 1f,
                    stopperMask = LayerIndex.entityPrecise.mask | LayerIndex.world.mask,
                    damageType = new DamageTypeCombo(DamageType.Stun1s, DamageTypeExtended.Generic, DamageSource.NoneSpecified),
                    force = 100,
                    falloffModel = BulletAttack.FalloffModel.None,
                    damageColorIndex = DamageColorIndex.Default,
                    isCrit = false,
                    origin = ray.origin,
                    owner = self.gameObject,
                    hitCallback = StompOnHit,

                };
                bulletAttack2.Fire();
                bool StompOnHit(BulletAttack bulletAttack, ref BulletAttack.BulletHit hitInfo)
                {
                    CharacterBody victim = hitInfo.hitHurtBox ? (hitInfo.hitHurtBox.healthComponent ? hitInfo.hitHurtBox.healthComponent.body : null) : null;
                    if (victim && !victim.HasBuff(Stomped))
                    {
                        float speed = self.velocity.magnitude;
                        if (bulletAttack.force * speed > 1200)
                        {
                            bulletAttack.damage *= speed;
                            bulletAttack.force *= speed;
                            victim.AddTimedBuff(Stomped, 0.2f);
                            InputBankTest inputBankTest = self.body.inputBank;
                            bool pressingJump = inputBankTest ? inputBankTest.jump.down : false;
                            self.velocity.y = pressingJump ? 32f : 4f;
                            return BulletAttack.DefaultHitCallbackImplementation(bulletAttack, ref hitInfo);

                        }
                        return false;

                    }
                    else
                    {
                        return false;
                    }

                }
            }
        }

        private void HealthComponent_TakeDamageProcess(On.RoR2.HealthComponent.orig_TakeDamageProcess orig, HealthComponent self, DamageInfo damageInfo)
        {
            CharacterBody characterBody = damageInfo.attacker ? damageInfo.attacker.GetComponent<CharacterBody>() : null;
            if (characterBody && characterBody.HasBuff(ExtraSwordDamage)) damageInfo.damage *= characterBody.GetBuffCount(ExtraSwordDamage) / 100;
            orig(self, damageInfo);
        }

        private void CharacterBody_OnKilledOtherServer(On.RoR2.CharacterBody.orig_OnKilledOtherServer orig, CharacterBody self, DamageReport damageReport)
        {
            CharacterBody attackerBody = damageReport.attackerBody;
            bool validForUpgrade = self && (self.isChampion || self.isBoss) ? self.HasBuff(UpgradeOnKill) : false;
            bool victimZatoichi = self.HasBuff(HealOnKill);
            bool attackerZatoichi = attackerBody ? attackerBody.HasBuff(HealOnKill) : false;
            orig(self, damageReport);
            if (attackerBody && validForUpgrade)
            {
                attackerBody.inventory.GiveItem(RoR2Content.Items.ShinyPearl);
            }
            if (victimZatoichi && attackerZatoichi)
            {
                HealthComponent healthComponent = attackerBody.healthComponent;
                Overheal(healthComponent, 0.5f);
            }

        }
        public static List<SkillDef> StickySkills = new List<SkillDef>();
        public static BuffDef ExtraCritChance;
        public static BuffDef HealOnKill;
        public static BuffDef ExtraSwordDamage;
        public static BuffDef Stomped;
        public static BuffDef LockedIn;
        public static BuffDef VelocityPreserve;
        public static BuffDef UpgradeOnKill;
        public static ItemDef UpgradeItem;
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
        public static SurvivorDef DemoSurvivorDef;
        public static GameObject DemoBody;
        public static GameObject SmokeEffect;
        public static GameObject SwingEffect;
        public static GameObject SpinEffect;
        public static GameObject ArmedEffect;
        public static Sprite hudStickyMeter;
        public static Sprite hudBaseMeter;
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
            ContentAddition.AddBuffDef(buff);
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
        private static void CreateAssets()
        {
            Main.ExtraCritChance = Main.AddBuff("ExtraCritChance", true, false, false, false, false);//, RoR2Content.Buffs.FullCrit.iconSprite);
            Main.HealOnKill = Main.AddBuff("HealOnKill", false, false, false, true, false);//, RoR2Content.Buffs.HealingDisabled.iconSprite);
            Main.ExtraSwordDamage = Main.AddBuff("ExtraSwordDamagePrepare", true, false, false, true, false);
            Main.ExtraSwordDamage = Main.AddBuff("ExtraSwordDamage", true, false, false, true, false);
            Stomped = AddBuff("Stomped", false, true, false, true, true);
            LockedIn = AddBuff("LockedIn", true, false, false, false, false);
            VelocityPreserve = AddBuff("PreserveVelocity", false, false, false, true, true);
            UpgradeOnKill = AddBuff("UpgradeOnKill", false, false, false, true, false);
            SmokeEffect = ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/SmokingEffect.prefab");
            SmokeEffect.AddComponent<DontRotate>();
            hudBaseMeter = ThunderkitAssets.LoadAsset<Sprite>("Assets/Demoman/UI/DemoSwordIndicator.png");
            hudStickyMeter = ThunderkitAssets.LoadAsset<Sprite>("Assets/Demoman/UI/DemoStickyIndicator.png");
            SwingEffect = ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/SwortSwing.prefab");
            SpinEffect = ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/SpecialSwing.prefab");
            SpinEffect.AddComponent<SpecialSwordSpiner>();
            ArmedEffect = ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/StickyArmedVFX.prefab");
            //UpgradeItem = AddItem("DemoStompItem", ItemTier.NoTier, null, null, false, true, new ItemTag[] {ItemTag.WorldUnique}, null);
            List<GameObject> projectiles = new List<GameObject>();
            Main.PillProjectile = ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/Projectiles/Pill/PillProjectile.prefab");
            projectiles.Add(Main.PillProjectile);
            RocketProjectile = ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/Projectiles/Rocket/RocketProjectile.prefab");
            RocketProjectile.AddComponent<RotateToVelocity>();
            projectiles.Add(Main.RocketProjectile);
            HookProjectile = ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/Projectiles/Hook/HookProjectile.prefab");
            HookProjectile.AddComponent<RotateToVelocity>();
            var hookComponent = HookProjectile.AddComponent<HookComponent>();
            //hookComponent.seekerState = (typeof(HookLauncher));
            projectiles.Add(Main.HookProjectile);
            NukeProjectile = ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/Projectiles/Nuke/NukeProjectile.prefab");
            NukeProjectile.AddComponent<RotateToVelocity>();
            //NukeProjectile.AddComponent<NukeComponent>();
            projectiles.Add(Main.NukeProjectile);
            BombProjectile = ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/Projectiles/Bomb/BombProjectile.prefab");
            projectiles.Add(Main.BombProjectile);
            BombProjectile.AddComponent<BombComponent>();
            Main.StickyProjectile = ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/Projectiles/Stickybomb/StickybombProjectile.prefab");
            StickyProjectile.AddComponent<DefaultSticky>();
            projectiles.Add(Main.StickyProjectile);
            Main.JumperProjectile = ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/Projectiles/JumperStickybomb 1/JumperStickybombProjectile.prefab");
            JumperProjectile.AddComponent<DefaultSticky>();
            var jumperComponent = JumperProjectile.AddComponent<DemoExplosionComponent>();
            jumperComponent.enemyPower = 12f;
            jumperComponent.selfPower = 15f;
            projectiles.Add(Main.JumperProjectile);
            Main.AntigravProjectile = ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/Projectiles/AntigravityStickyBomb/AntigravityStickyBombProjectile.prefab");
            projectiles.Add(Main.AntigravProjectile);
            AntigravProjectile.AddComponent<AntigravSticky>();
            Main.MineProjectile = ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/Projectiles/Mine/MineProjectile.prefab");
            projectiles.Add(Main.MineProjectile);
            MineProjectile.AddComponent<Mine>();
            MineProjectile.transform.GetChild(0).gameObject.AddComponent<MineDetector>();
            GameObject explosionVFX = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Toolbot/OmniExplosionVFXToolbotQuick.prefab").WaitForCompletion();
            foreach (GameObject projectile in projectiles)
            {
                DemoExplosionComponent rocketjumpComponent = projectile.GetComponent<DemoExplosionComponent>();
                if (!rocketjumpComponent)
                {
                    projectile.AddComponent<DemoExplosionComponent>();
                }
                ProjectileImpactExplosion projectileImpactExplosion = projectile.GetComponent<ProjectileImpactExplosion>();
                if (projectileImpactExplosion && projectileImpactExplosion.impactEffect == null)
                {
                    projectileImpactExplosion.impactEffect = explosionVFX;
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
                PrefabAPI.RegisterNetworkPrefab(projectile);
                ContentAddition.AddProjectile(projectile);
            }
        }
        private static void CreateSurvivor()
        {
            DemoBody = Main.ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/DemomanBody.prefab");
            CharacterBody demoCharacterBody = DemoBody.GetComponent<CharacterBody>();
            demoCharacterBody.preferredPodPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/SurvivorPod/SurvivorPod.prefab").WaitForCompletion();
            demoCharacterBody._defaultCrosshairPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/UI/StandardCrosshair.prefab").WaitForCompletion();
            GameObject gameObject = DemoBody.GetComponent<ModelLocator>().modelTransform.gameObject;
            gameObject.GetComponent<FootstepHandler>().footstepDustPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/VFX/GenericFootstepDust.prefab").WaitForCompletion();
            //DemoBody.transform.Find("ModelBase/mdlDemoman/DemoRig/root/base/based/stomach/lower_chest/upper_chest/neck/head/HeadHurtbox").gameObject.layer = LayerIndex.entityPrecise.intVal;
            //DemoBody.transform.Find("ModelBase/mdlDemoman/DemoRig/root/base/based/stomach/lower_chest/upper_chest/neck/head/HeadHurtbox").gameObject.layer = LayerIndex.entityPrecise.intVal;
            var component = DemoBody.AddComponent<DemoComponent>();
            var skillLocator = DemoBody.GetComponent<SkillLocator>();
            //skillLocator.allSkills[5].hideInCharacterSelect = true;
            //DemoBody.AddComponent<CachedSkillsLocator>();
            GameObject hudObject = ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/DemoExtraCrosshair.prefab");
            hudObject.transform.localScale = new Vector3(-1f, 1f, 1f);
            component.chargeMeter = hudObject;
            component.altCrosshair = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Toolbot/ToolbotGrenadeLauncherCrosshair.prefab").WaitForCompletion();
            InteractionDriver interactionDriver = DemoBody.GetComponent<InteractionDriver>();
            EntityStateMachine entityStateMachine = DemoBody.GetComponent<EntityStateMachine>();
            entityStateMachine.mainStateType = new SerializableEntityStateType(typeof(DemoCharacterMain));
            //DemoBody.GetComponent<CharacterCameraParams>().data = Addressables.LoadAssetAsync<CharacterCameraParams>("RoR2/Base/Common/ccpStandard.asset").WaitForCompletion().data;
            BodyCatalog.getAdditionalEntries += BodyCatalog_getAdditionalEntries;
            SurvivorCatalog.getAdditionalSurvivorDefs += SurvivorCatalog_getAdditionalSurvivorDefs;
            if (true)
            {
                string path = "Assets/Demoman/DemoEmotes.prefab";
                var skele = Main.ThunderkitAssets.LoadAsset<GameObject>(path);
                CustomEmotesAPI.ImportArmature(DemoBody, skele);
                skele.GetComponentInChildren<BoneMapper>().scale = 1f;
                skele.transform.localPosition = new Vector3(0f, -0f, 0f);
                skele.transform.localRotation = Quaternion.identity;
                CustomEmotesAPI.animChanged += CustomEmotesAPI_animChanged;
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
        public static DemoSoundClass DemoStickyDetonationSound = new DemoSoundClass("stickybomblauncher_charge_up");
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
                R2API.ContentAddition.AddNetworkSoundEventDef(playSound);
                stopSound = ScriptableObject.CreateInstance<NetworkSoundEventDef>();
                stopSound.eventName = stopSoundString;
                R2API.ContentAddition.AddNetworkSoundEventDef(stopSound);
            }
            public string playSoundString;
            public string stopSoundString;
            private NetworkSoundEventDef playSound;
            private NetworkSoundEventDef stopSound;
        }
        private static void CustomEmotesAPI_animChanged(string newAnimation, BoneMapper mapper)
        {
            if (mapper.transform.name == "DemoEmotes")
            {
                ChildLocator childLocator = mapper.transform.parent.GetComponent<ChildLocator>();
                ChildLocator childLocator2 = mapper.GetComponent<ChildLocator>();
                Transform smokes = childLocator.FindChild("Smokes");
                Transform smokes2 = childLocator2.FindChild("Smokes");
                Transform weaponR = childLocator.FindChild("WeaponR");
                Transform weaponL = childLocator.FindChild("WeaponL");
                Transform shield = childLocator.FindChild("Shield");
                if (newAnimation != "none")
                {
                    smokes2.gameObject.SetActive(true);
                    smokes.gameObject.SetActive(false);
                    //smokes.SetParent(childLocator2.FindChild("Head"));
                    weaponR.gameObject.SetActive(false);
                    weaponL.gameObject.SetActive(false);
                    shield.gameObject.SetActive(false);
                    //smokes.localPosition = Vector3.zero;
                    //smokes.localRotation = Quaternion.identity;
                    //smokes.localScale = Vector3.one;

                }
                else
                {
                    smokes2.gameObject.SetActive(false);
                    smokes.gameObject.SetActive(true);
                    //smokes.SetParent(childLocator.FindChild("Head"));
                    weaponR.gameObject.SetActive(true);
                    weaponL.gameObject.SetActive(true);
                    shield.gameObject.SetActive(true);
                    //smokes.localPosition = Vector3.zero;
                    //smokes.localRotation = Quaternion.identity;
                    //smokes.localScale = Vector3.one;

                }
            }
        }

        private static bool setup = false;
        private static void SurvivorCatalog_Init(On.RoR2.SurvivorCatalog.orig_Init orig)
        {
            orig();
            if (!setup)
            {
                setup = true;
                foreach (var item in SurvivorCatalog.allSurvivorDefs)
                {
                    if (item.bodyPrefab.name == "DemomanBody")
                    {
                        string path = "Assets/Demoman/DemoEmotes.prefab";
                        //Debug.Log("Path: " + path);

                        var skele = Main.ThunderkitAssets.LoadAsset<GameObject>(path);
                        //Debug.Log(skele);
                        CustomEmotesAPI.ImportArmature(item.bodyPrefab, skele);
                        skele.GetComponentInChildren<BoneMapper>().scale = 1f;
                        skele.transform.localPosition = new Vector3(0f, -0f, 0f);
                        skele.transform.localRotation = Quaternion.identity;
                    }
                }
                //foreach (var item in BodyCatalog.allBodyPrefabs)
                //{
                //    var component = item.GetComponent<ExpansionRequirementComponent>();
                //    if (component) Destroy(component);
                //}
            }
        }
        private static void BodyCatalog_getAdditionalEntries(List<GameObject> obj)
        {
            obj.Add(DemoBody);
        }

        private static void SurvivorCatalog_getAdditionalSurvivorDefs(List<SurvivorDef> obj)
        {
            obj.Add(Main.ThunderkitAssets.LoadAsset<SurvivorDef>("Assets/Demoman/DemoSurvivor.asset"));
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
            Event
        }
        public static string LangueagePrefix(string stringField, LanguagePrefixEnum type)
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
        public class HitHelper : MonoBehaviour
        {
            private TeamFilter filter;
            private ProjectileExplosion projectileExplosion;
            private void Start()
            {
                filter = GetComponent<TeamFilter>();
            }
            private void OnTriggerEnter(Collider other)
            {
                HurtBox hurtBox = other.GetComponent<HurtBox>();
                if (filter && hurtBox && filter.teamIndex != hurtBox.healthComponent.body.teamComponent.teamIndex)
                {
                    transform.position = other.ClosestPoint(transform.position);
                }
            }

        }
       
    
        public class DemoExplosionComponent : MonoBehaviour
        {
            private ProjectileExplosion explosion;
            private float radius;
            private Vector3 explosionPosition;
            public float selfPower = 10f;
            public float enemyPower = 2f;
            private List<CharacterBody> bodyList = new List<CharacterBody>();
            private List<GameObject> nearbyExplosions = new List<GameObject>();
            private TeamFilter teamFilter;
            private TeamIndex teamIndex;
            private GameObject explosionCentre;
            private GameObject owner;

            public void Start()
            {
                explosion = GetComponent<ProjectileExplosion>();
                if (!explosion) Destroy(this);
                teamFilter = GetComponent<TeamFilter>();
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
                radius = explosion.blastRadius;
                teamIndex = teamFilter ? teamFilter.teamIndex : TeamIndex.None;
                ProjectileController projectileController = GetComponent<ProjectileController>();
                owner = projectileController ? projectileController.owner : null;
                
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
                        Vector3 pushVector = (characterPosition - explosionPosition).normalized * radius;
                        pushVector.y = pushVector.y / 2f;
                        if (body.inputBank && body.inputBank.jump.down)
                        {
                            pushVector = Vector3.RotateTowards(pushVector, body.inputBank.aimDirection, 360f, 0f);
                        }
                        if (characterMotor.Motor) characterMotor.Motor.ForceUnground(0f);
                        body.characterMotor.velocity += body.teamComponent.teamIndex == teamIndex ? pushVector * selfPower : pushVector * enemyPower / Vector3.Distance(characterPosition, explosionPosition);
                        if (body.teamComponent.teamIndex == teamIndex)
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
        public class HookComponent : MonoBehaviour
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
            private bool isStickedAnEnemy = false;
            private CharacterBody stickedBody;
            //private float hookLength;
            //private Vector3 hookAimDirection;
            private GameObject hookedRotator;
            private GameObject hookedObject;
            //private float maxDistance = 32f;
            private float speed = 24f;
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

                if (ropeEnd)
                {
                    ropeEnd.SetParent(owner.transform);
                }
                characterMotor = owner ? owner.GetComponent<CharacterMotor>() : null;
                inputBankTest = owner ? owner.GetComponent<InputBankTest>() : null;

                stateType = seekerState.stateType;
                EntityStateMachine[] entityStateMachines = owner.GetComponents<EntityStateMachine>();
                foreach (var stateMachine in entityStateMachines)
                {
                    if (stateMachine.state.GetType() == stateType)
                    {
                        currentStateMachine = stateMachine;
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
            public void FixedUpdate()
            {
                if (currentStateMachine && currentStateMachine.state.GetType() == stateType)
                {
                    if (sticked)
                    {
                        Vector3 aimDirection = inputBankTest ? inputBankTest.aimDirection : Vector3.zero;
                        Vector3 aimDelta = inputBankTest.aimDirection - previousAimDirection;
                        previousAimDirection = aimDirection;
                        if (isStickedAnEnemy && hookedObject)
                        {
                            if (stickedBody)
                            {
                                if (stickedBody.characterMotor)
                                {
                                    Vector3 direction = hookedObject.transform.position - stickedBody.transform.position;
                                    stickedBody.characterMotor.velocity = direction * 4;
                                }
                            }
                        }
                        else
                        {
                            if (characterMotor && inputBankTest)
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
                    speed = Math.Max(characterMotor.walkSpeed,  characterMotor.velocity.magnitude * 3f);
                    if (speed > characterMotor.walkSpeed * 6f) speed = characterMotor.walkSpeed * 6f;
                    if (inputBankTest)
                    {
                        hookedRotator = new GameObject("hookedRotator");
                        hookedRotator.transform.SetParent(characterMotor.body.aimOriginTransform, false);
                        hookedRotator.transform.rotation = Quaternion.LookRotation(inputBankTest.aimDirection);
                        hookedObject = new GameObject("hookedPosition");
                        GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        gameObject.layer = 9;
                        gameObject.transform.SetParent(hookedObject.transform, false);
                        hookedObject.transform.SetParent(hookedRotator.transform, false);
                        float distance = MathF.Max(6,  (collision.contacts[0].point - characterMotor.body.aimOriginTransform.position).magnitude);
                        if (distance > 64) distance = 64;
                        hookedObject.transform.position = hookedObject.transform.position + (inputBankTest.aimDirection * distance);
                    }
                    
                }
                

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
            public int stock = 0;
            public float cooldown = 0f;
            public GenericSkill primaryReplace;
            public GenericSkill utilityReplace;
            public GenericSkill secondaryReplace;
            public GenericSkill specialReplace;
            public GameObject chargeMeter;
            public OverlayController overlayController;
            public bool updateMeter = true;
            public GameObject hudObject;
            public Image meterImage;
            public Image baseMeter;
            public TextMeshProUGUI stickyText;
            public GameObject altCrosshair;
            private ChildLocator childLocator;
            private CharacterModel characterModel;
            private CharacterBody characterBody;
            private CrosshairUtils.OverrideRequest overrideRequest;
            public Transform primarystockCounter;
            public Image primaryStopwatch;
            public Transform secondarystockCounter;
            public Image secondaryStopwatch;
            private GenericSkill trackPrimary;
            private GenericSkill trackUtility;
            private CameraTargetParams cameraTargetParams;
            public bool canUseUtility = true;
            public float canUseUtilityTimer = 0f;
            public bool canUseSecondary = true;
            public float canUseSecondaryTimer = 0f;
            public bool useUtility;
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
                trackPrimary = primaryReplace;
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
            }
            public void OnEnable()
            {
                GetComponent<ModelLocator>().modelTransform.GetComponent<DynamicBone>().m_Weight = 0.07f;
                OverlayCreationParams overlayCreationParams = new OverlayCreationParams
                {
                    prefab = chargeMeter,
                    childLocatorEntry = "CrosshairExtras"
                };
                this.overlayController = HudOverlayManager.AddOverlay(base.gameObject, overlayCreationParams);
                this.overlayController.onInstanceAdded += OnOverlayInstanceAdded;
                this.overlayController.onInstanceRemove += OnOverlayInstanceRemoved;
                //meterImage = overlayController.instancesList[0].GetComponent<Image>();
            }

            private void OnOverlayInstanceRemoved(OverlayController controller, GameObject @object)
            {
                //meterImage = controller.instancesList[0].GetComponent<Image>();
            }

            private void OnOverlayInstanceAdded(OverlayController controller, GameObject @object)
            {
            }

            public void Start()
            {
                SetSkillsModels();
            }
            public void SetSkillsModels()
            {
                if (skillLocator && characterModel)
                {
                    List<GameObject> list = new List<GameObject>();
                    foreach (var skill in skillLocator.allSkills)
                    {
                        if (swordDictionary.ContainsKey(skill.baseSkill))
                        {
                            GameObject swordModel = swordDictionary[skill.baseSkill].swordObject;
                            Transform weaponTransform = childLocator.FindChild("WeaponR");
                            if (swordModel && weaponTransform)
                            {
                                GameObject swordNewModel = Instantiate(swordModel, weaponTransform);
                                swordNewModel.transform.localEulerAngles = new Vector3(0f, 180f, 0f);
                                list.Add(swordNewModel);
                                /*if (characterModel)
                                {

                                    ItemDisplay itemDisplay = swordNewModel.AddComponent<ItemDisplay>();
                                    CharacterModel.ParentedPrefabDisplay parentedPrefabDisplay = new CharacterModel.ParentedPrefabDisplay
                                    {
                                        instance = swordNewModel,
                                        itemDisplay = itemDisplay,
                                        equipmentIndex = 0,
                                        itemIndex = 0
                                    };
                                    characterModel.parentedPrefabDisplays.Add(parentedPrefabDisplay);
                                }*/

                            }
                        }
                        if (shieldDictionary.ContainsKey(skill.baseSkill))
                        {
                            GameObject shieldModel = shieldDictionary[skill.baseSkill];
                            Transform shieldTransform = childLocator.FindChild("Shield");
                            if (shieldModel && shieldTransform)
                            {
                                GameObject shieldNewModel = Instantiate(shieldModel, shieldTransform);
                                list.Add(shieldNewModel);

                            }
                        }
                    }
                    List<CharacterModel.RendererInfo> rendererInfos = characterModel.baseRendererInfos.ToList();
                    foreach (GameObject weaponModel in list)
                    {
                        Renderer[] newRenderers = weaponModel.GetComponentsInChildren<Renderer>();
                        foreach(Renderer renderer in newRenderers)
                        {
                            
                            rendererInfos.Add(new CharacterModel.RendererInfo
                            {
                                defaultMaterial = renderer.material,
                                defaultShadowCastingMode = renderer.shadowCastingMode,
                                hideOnDeath = false,
                                ignoreOverlays = false,
                                renderer = renderer
                            });


                        }
                    }
                    characterModel.baseRendererInfos = rendererInfos.ToArray();
                }

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
                        
                        baseMeter.sprite = hudStickyMeter;
                        meterImage.sprite = hudStickyMeter;
                    }
                    else
                    {
                        if (overrideRequest != null)
                        {
                            overrideRequest.Dispose();
                        }
                        baseMeter.sprite = hudBaseMeter;
                        meterImage.sprite = hudBaseMeter;
                    }
                }
            }
            public void FixedUpdate()
            {
                //if (isSwapped) return;
                if (canUseUtility && canUseUtilityTimer > 0f) canUseUtilityTimer -= Time.fixedDeltaTime;
                if (canUseSecondary && canUseSecondaryTimer > 0f) canUseSecondaryTimer -= Time.fixedDeltaTime;
                if (!hudObject && overlayController != null && overlayController.instancesList.Count > 0)
                {
                    if (!hudObject)
                    {
                        hudObject = overlayController.instancesList[0];
                    }
                    if (!baseMeter)
                    {
                        baseMeter = overlayController.instancesList[0].transform.GetChild(0).GetComponent<Image>();
                    }
                    if (!meterImage)
                    {
                        meterImage = hudObject.transform.Find("chargeMeterBase/chargeMeter").GetComponent<Image>();
                    }
                    if (!stickyText)
                    {
                        stickyText = hudObject.transform.Find("chargeMeterBase/stickyCount").GetComponent<TextMeshProUGUI>();
                    }
                    if (!primarystockCounter)
                    {
                        primarystockCounter = hudObject.transform.Find("chargeMeterBase/primaryStocksCount");
                    }
                    if (!secondarystockCounter)
                    {
                        secondarystockCounter = hudObject.transform.Find("chargeMeterBase/secondaryStocksCount");
                    }
                    if (!primaryStopwatch)
                    {
                        primaryStopwatch = hudObject.transform.Find("chargeMeterBase/primaryStopwatchBase/primaryStopwatch").GetComponent<Image>();
                    }
                    if (!secondaryStopwatch)
                    {
                        secondaryStopwatch = hudObject.transform.Find("chargeMeterBase/secondaryStopwatchBase/secondaryStopwatch").GetComponent<Image>();
                    }
                    UpdateHudObject();
                }
                if (primarystockCounter)
                {
                    if (trackPrimary)
                    {
                        int stocks = trackPrimary.stock;
                        for (int i = 0; i < primarystockCounter.childCount; i++)
                        {
                            primarystockCounter.GetChild(i).gameObject.SetActive(stocks > 0 ? true : false);
                            stocks--;
                        }
                    }
                }
                if (inputBank)
                {
                }
                if (skillLocator)
                {
                    if (secondarystockCounter)
                    {
                        int stocks = skillLocator.secondary.stock;
                        for (int i = 0; i < secondarystockCounter.childCount; i++)
                        {
                            secondarystockCounter.GetChild(i).gameObject.SetActive(stocks > 0 ? true : false);
                            stocks--;
                        }
                    }
                    if (primaryStopwatch && trackPrimary)
                    {
                        primaryStopwatch.fillAmount = 1 - ((trackPrimary.finalRechargeInterval - trackPrimary.rechargeStopwatch) / trackPrimary.finalRechargeInterval);
                    }
                    if (secondaryStopwatch)
                    {
                        secondaryStopwatch.fillAmount = 1 - ((skillLocator.secondary.finalRechargeInterval - skillLocator.secondary.rechargeStopwatch) / skillLocator.secondary.finalRechargeInterval);
                    }
                }
                
                
                if (stickyText)
                {
                    stickyText.text = stickyCount.ToString();
                }
                if (meterImage && trackUtility && updateMeter)
                {
                    meterImage.fillAmount = 1 - ((trackUtility.finalRechargeInterval - trackUtility.rechargeStopwatch) / trackUtility.finalRechargeInterval);
                }
                if (primaryReplace)
                {
                    primaryReplace.RunRecharge(Time.fixedDeltaTime);
                }
                if (secondaryReplace)
                {
                    secondaryReplace.RunRecharge(Time.fixedDeltaTime);
                }
                if (specialReplace)
                {
                    specialReplace.RunRecharge(Time.fixedDeltaTime);
                }
                if (utilityReplace)
                {
                    utilityReplace.RunRecharge(Time.fixedDeltaTime);
                }

            }
            public void Update()
            {
                if (inputBank)
                {
                    if (inputBank.skill3.justPressed)
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
                            cooldown = skillLocator.secondary.rechargeStopwatch;
                            stock = skillLocator.secondary.stock;
                            //GenericSkill skill1 = skillLocator.utility;
                            //skillLocator.utility = utilityReplace;
                            //utilityReplace = skill1;
                            skillLocator.secondary.SetSkillOverride(this, SwapSkillDef, GenericSkill.SkillOverridePriority.Contextual);
                        }

                    }
                    if (inputBank.skill3.justReleased)
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
                            //GenericSkill skill1 = skillLocator.utility;
                            //skillLocator.utility = utilityReplace;
                            //utilityReplace = skill1;
                            skillLocator.secondary.UnsetSkillOverride(this, SwapSkillDef, GenericSkill.SkillOverridePriority.Contextual);
                            skillLocator.secondary.stock = stock;
                            skillLocator.secondary.rechargeStopwatch = cooldown;
                        }
                        if (canUseUtilityTimer <= 0)
                        {
                            skillLocator.utility.ExecuteIfReady();
                        }
                        canUseUtility = true;
                        useUtility = false;
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
            public void Start()
            {
                projectileDamage = GetComponent<ProjectileDamage>();
                rigidbody = GetComponent<Rigidbody>();
                owner = GetComponent<ProjectileController>().owner;
                //EntityStateMachine[] entityStateMachines = owner.GetComponents<EntityStateMachine>();
                //foreach (var stateMachine in entityStateMachines)
                //{
                //    if (stateMachine.state.GetType() == seekerState.stateType)
                //    {
                //        currentStateMachine = stateMachine;
                //        stateType = currentStateMachine.state;
                //        break;
                //    }
                //}
                //if (currentStateMachine && stateType != null)
                //{
                //    if (stateType is BombLauncher)
                //    {
                //        BombLauncher bombLauncher = (stateType as BombLauncher);
                //        ProjectileImpactExplosion projectileImpactExplosion = GetComponent<ProjectileImpactExplosion>();
                //        if (projectileImpactExplosion)
                //        {
                //            projectileImpactExplosion.lifetime = projectileImpactExplosion.lifetime * (1 - (bombLauncher.charge / bombLauncher.chargeCap));
                //        }
                //    }
                //}
                //else
                //{
                //    Destroy(this);
                //    return;
                //}
            }
            public void OnProjectileImpact(ProjectileImpactInfo impactInfo)
            {
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

        public abstract class StickyComponent : MonoBehaviour, IProjectileImpactBehavior
        {
            private float stopwatch = 0f;
            public abstract float armTime { get; }
            public abstract string stickyName { get; }
            public abstract float detonationTime { get; }
            public abstract int maxStickies { get; }
            private bool armed = true;
            //public bool isArmed = false;
            private DemoComponent demoComponent;
            public GameObject armedVFX = ArmedEffect;// Addressables.LoadAssetAsync<GameObject>("RoR2/DLC2/Items/IncreaseDamageOnMultiKill/IncreaseDamageOnMultiKillVFX.prefab").WaitForCompletion();
            public bool sticked = false;
            public abstract bool isStickable { get; }
            private Rigidbody rigidbody;
            private ProjectileImpactExplosion projectileImpactExplosion;
            private string currentName;
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
            public virtual void Start()
            {
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

                }
                
            }
            public virtual void OnStickyArmed()
            {
                //EffectData effectData = new EffectData
                //{
                //    scale = 1f,
                //    rotation = Quaternion.identity,
                //    origin = transform.position,

                //};
                //EffectManager.SpawnEffect(armedVFX, effectData, true);
                GameObject.Instantiate(armedVFX, transform);
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
                //EffectData effectData = new EffectData
                //{
                //    scale = 1f,
                //    rotation = Quaternion.identity,
                //    origin = transform.position,

                //};

                //GameObject.Instantiate(armedVFX, transform);
                //EffectManager.SpawnEffect(armedVFX, effectData, true);
                GetOrCreateListOfStickies().Remove(this);
            }
            public virtual void FixedUpdate()
            {
                if (!isArmed)
                    stopwatch += Time.fixedDeltaTime;
                if (isArmed && armed)
                {
                    OnStickyArmed();
                    armed = false;
                }

            }
            public virtual void OnDisable()
            {
                if (demoComponent) GetOrCreateListOfStickies().Remove(this);
            }

            public virtual void OnProjectileImpact(ProjectileImpactInfo impactInfo)
            {
                if (!isStickable || sticked) return;
                sticked = true;
                rigidbody.constraints = RigidbodyConstraints.FreezeAll;
                rigidbody.velocity = Vector3.zero;
                rigidbody.useGravity = false;
                transform.SetParent(impactInfo.collider.transform);
                transform.position += impactInfo.estimatedImpactNormal * 0.01f;

            }
        }
        public class DefaultSticky : StickyComponent
        {
            public override float armTime => 0.7f;

            public override bool isStickable => true;

            public override string stickyName => "Default";

            public override float detonationTime => 0.4f;

            public override int maxStickies => 8;
        }
        public class AntigravSticky : StickyComponent
        {
            public override float armTime => 0.3f;

            public override bool isStickable => false;

            public override string stickyName => "PlasmaTrap";

            public override float detonationTime => 0.2f;

            public override int maxStickies => 4;
        }
        public class Mine : StickyComponent
        {
            public override float armTime => 0.7f;

            public override bool isStickable => true;

            public override string stickyName => "MineTrap";

            public override float detonationTime => 0.4f;

            public override int maxStickies => 16;

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
            public void Start()
            {
                stickyComponent = transform.parent.GetComponent<StickyComponent>();

                teamFilter = stickyComponent.GetComponent<TeamFilter>();
            }
            public void OnTriggerEnter(Collider other)
            {
                HurtBox hurtBox = other.GetComponent<HurtBox>();
                if (!hurtBox) return;
                CharacterBody characterBody = hurtBox.healthComponent.body;
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
            private void FixedUpdate()
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
        public static SkillFamily demoStickyFamily;
        public static SkillFamily demoDetonateFamily;
        public static SkillFamily demoPassiveFamily;
        public static DemoSwordClass DefaultSword;
        public static SkillDef SkullcutterSkillDef;
        public static DemoSwordClass Skullcutter;
        public static SkillDef ZatoichiSkillDef;
        public static DemoSwordClass Zatoichi;
        public static SkillDef CaberSkillDef;
        public static DemoSwordClass Caber;
        public static SkillDef DeflectorSkillDef;
        public static DemoSwordClass Deflector;
        public static SkillDef EyelanderSkillDef;
        public static DemoSwordClass Eyelander;
        public static SkillDef PillLauncherSkillDef;
        public static SkillDef RocketLauncherSkillDef;
        public static SkillDef HookLauncherSkillDef;
        public static SkillDef NukeLauncherSkillDef;
        public static SkillDef BombLauncherSkillDef;
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
        public static SkillDef LockInSkillDef;
        public static SkillDef SwapSkillDef;
        public static SkillDef ManthreadsSkillDef;
        public static SkillDef HeavyShieldSkillDef;
        public static SkillDef LightShieldSkillDef;
        public static SkillDef SpaceShieldSkillDef;
        public static List<SkillDef> stickySkills = new List<SkillDef>();
        public static Dictionary<SkillDef, SkillDef> customDetonationSkills = new Dictionary<SkillDef, SkillDef>();
        public static Dictionary<SkillDef, SkillDef> altSpecialSkills = new Dictionary<SkillDef, SkillDef>();
        public static Dictionary<SkillDef, DemoSwordClass> swordDictionary = new Dictionary<SkillDef, DemoSwordClass>();
        public static Dictionary<SkillDef, GameObject> shieldDictionary = new Dictionary<SkillDef, GameObject>();
        public static Dictionary<SkillDef, DemoStickyClass> bombProjectiles = new Dictionary<SkillDef, DemoStickyClass>();
        public static Dictionary<SkillDef, Action<CharacterBody, RecalculateStatsAPI.StatHookEventArgs>> skillsToStats = new Dictionary<SkillDef, Action<CharacterBody, RecalculateStatsAPI.StatHookEventArgs>>();
        //public delegate void SwordOnHitEffect(ref BulletAttack bullet, ref BulletAttack.BulletHit hitInfo, List<HurtBox> hurtboxes = null, OverlapAttack overlapAttack = null);

        public static void Init()
        {
            //InitSwordAttacks();
            
            demoStickyFamily = Main.ThunderkitAssets.LoadAsset<SkillFamily>("Assets/Demoman/DemoStickies.asset");
            demoDetonateFamily = Main.ThunderkitAssets.LoadAsset<SkillFamily>("Assets/Demoman/DemoDetonate.asset");
            demoPassiveFamily = Main.ThunderkitAssets.LoadAsset<SkillFamily>("Assets/Demoman/DemoPassive.asset");
            BulletAttack DefaultBulletAttack = new BulletAttack()
            {
                radius = 1f,
                damage = 3,
                bulletCount = 1,
                maxDistance = 6,
                allowTrajectoryAimAssist = true,
                hitMask = LayerIndex.entityPrecise.mask,
                procCoefficient = 1f,
                stopperMask = LayerIndex.world.mask,
                force = 3f,
                hitCallback = null

            };
            DefaultSword = new DemoSwordClass(DefaultBulletAttack);
            BulletAttack SkullcutterBulletAttack = new BulletAttack()
            {
                radius = 1f,
                damage = 3,
                bulletCount = 1,
                maxDistance = 7,
                allowTrajectoryAimAssist = true,
                hitMask = LayerIndex.entityPrecise.mask,
                procCoefficient = 1f,
                stopperMask = LayerIndex.world.mask,
                force = 3f,
                hitCallback = SkullcutterOnHit

            };
            Skullcutter = new DemoSwordClass(SkullcutterBulletAttack, swordObject: ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/Weapons/Swords/skullcutter.prefab"));
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
                    CharacterBody body = hitInfo.hitHurtBox.hurtBoxGroup.transform.GetComponent<CharacterModel>().body;

                    if (body && body.healthComponent && (body.healthComponent.combinedHealthFraction == 1f))
                    {
                        bulletAttack.damage *= 3f;
                    }
                }
                return BulletAttack.DefaultHitCallbackImplementation(bulletAttack, ref hitInfo);
            }
            SkullcutterSkillDef = SwordInit(Skullcutter, typeof(DemoSword), ThunderkitAssets.LoadAsset<Sprite>("Assets/Demoman/Skills/DemoSkullcutterSkillIcon.png"), "DemoSkullcutter", "DEMOMAN_SKULLCUTTER_NAME", "DEMOMAN_SKULLCUTTER_DESC");
            LanguageAPI.Add("DEMOMAN_SKULLCUTTER_NAME", "Skullcutter");
            GenerateSwordDescriptionToken("DEMOMAN_SKULLCUTTER_DESC", Skullcutter, "Skullcutter is a relieble weapon for killing weak targets and consistently damaging stronger targets",
                "On hit: Increase next melee attack crit chance by 10%. Resets on crit success\n" +
                "Special: Deals 3x more damage to the targets you hit for the first time\n" +
                "Passive: Reduces base movement speed by 2m/s when held");
            BulletAttack ZatoichiBulletAttack = new BulletAttack()
            {
                radius = 1f,
                damage = 3,
                bulletCount = 1,
                maxDistance = 6,
                allowTrajectoryAimAssist = true,
                hitMask = LayerIndex.entityPrecise.mask,
                procCoefficient = 1f,
                stopperMask = LayerIndex.world.mask,
                force = 3f,
                hitCallback = ZatoichiOnHit

            };
            Zatoichi = new DemoSwordClass(ZatoichiBulletAttack, swordObject: ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/Weapons/Swords/zatoichi.prefab"));
            bool ZatoichiOnHit(BulletAttack bulletAttack, ref BulletAttack.BulletHit hitInfo)
            {
                if (hitInfo.hitHurtBox)
                {
                    CharacterBody ownerBody = bulletAttack.owner.GetComponent<CharacterBody>();
                    Main.Overheal(ownerBody.healthComponent, 0.1f);
                    ownerBody.AddTimedBuff(Main.HealOnKill, 0.2f);
                    CharacterBody body = hitInfo.hitHurtBox.hurtBoxGroup.transform.GetComponent<CharacterModel>().body;
                    if (body)
                    {
                        body.AddTimedBuff(Main.HealOnKill, 0.2f);
                    }
                }
                return BulletAttack.DefaultHitCallbackImplementation(bulletAttack, ref hitInfo);
            }
            ZatoichiSkillDef = SwordInit(Zatoichi, typeof(DemoSword), ThunderkitAssets.LoadAsset<Sprite>("Assets/Demoman/Skills/DemoZatoichiSkillIcon.png"), "DemoZatoichi", "DEMOMAN_ZATOICHI_NAME", "DEMOMAN_ZATOICHI_DESC");
            LanguageAPI.Add("DEMOMAN_ZATOICHI_NAME", "Zatoichi");
            GenerateSwordDescriptionToken("DEMOMAN_ZATOICHI_DESC", Zatoichi, "Zatoichi is a great weapon for mainatining your health in a heat of battle. Heals from this weapon will give barrier when on full health",
                "On hit: Heal for 4% of the maximum health\n" +
                "On kill: Heal for 15% of the maximum health");
            BulletAttack CaberBulletAttack = new BulletAttack()
            {
                radius = 1f,
                damage = 1,
                bulletCount = 1,
                maxDistance = 4,
                allowTrajectoryAimAssist = true,
                hitMask = LayerIndex.entityPrecise.mask + LayerIndex.world.mask,
                procCoefficient = 1f,
                stopperMask = LayerIndex.entityPrecise.mask + LayerIndex.world.mask,
                force = 3f,
                hitCallback = CaberOnHit

            };
            Caber = new DemoSwordClass(CaberBulletAttack, swordObject: ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/Weapons/Swords/caber.prefab"));
            bool CaberOnHit(BulletAttack bulletAttack, ref BulletAttack.BulletHit hitInfo)
            {
                CharacterBody ownerBody = bulletAttack.owner.GetComponent<CharacterBody>();


                if (ownerBody.skillLocator && ownerBody.skillLocator && ChechState())
                {
                    ownerBody.skillLocator.primary.DeductStock(1);
                    if (ownerBody.characterMotor) ownerBody.characterMotor.velocity += new Vector3(0f, 20f, 0f);
                    BlastAttack caberExplosion = new BlastAttack
                    {
                        attacker = bulletAttack.owner,
                        attackerFiltering = AttackerFiltering.Default,
                        baseDamage = ownerBody.damage * (ownerBody.characterMotor ? ownerBody.characterMotor.velocity.magnitude : ownerBody.rigidbody.velocity.magnitude) * 2,
                        baseForce = 3f,
                        bonusForce = Vector3.up,
                        canRejectForce = true,
                        crit = bulletAttack.isCrit,
                        damageColorIndex = DamageColorIndex.Default,
                        teamIndex = ownerBody.teamComponent.teamIndex,
                        damageType = bulletAttack.damageType,
                        falloffModel = BlastAttack.FalloffModel.SweetSpot,
                        impactEffect = default,
                        radius = 3f,
                        position = hitInfo.point + hitInfo.surfaceNormal * 0.01f,
                        inflictor = bulletAttack.owner,
                        losType = BlastAttack.LoSType.None,
                        procChainMask = default,
                        procCoefficient = 1f
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
                bool ChechState()
                {
                    bool check = false;
                    foreach (var skill in ownerBody.GetComponents<GenericSkill>())
                    {
                        if (skill.stateMachine && skill.stateMachine.state is DemoSword)
                        {
                            if (skill.stock > 1)
                            {
                                check = true;
                            }

                            break;
                        }
                    }
                    return check;
                }
            }
            CaberSkillDef = SwordInit(Caber, typeof(DemoSword), ThunderkitAssets.LoadAsset<Sprite>("Assets/Demoman/Skills/DemoCaberSkillIcon.png"), "DemoCaber", "DEMOMAN_CABER_NAME", "DEMOMAN_CABER_DESC", requiredStock: 0, rechargeInterval: 8f, stockToConsume: 0, baseStock: 2); ;
            LanguageAPI.Add("DEMOMAN_CABER_NAME", "Caber");
            GenerateSwordDescriptionToken("DEMOMAN_CABER_DESC", Caber, "Caber is a weak, but fast handheld grenade that explodes on enemy or terrain hit, dealing massive damage based on current velocity. Explosion reloads 8 seconds");
            BulletAttack EyelanderBulletAttack = new BulletAttack()
            {
                radius = 1f,
                damage = 2,
                bulletCount = 1,
                maxDistance = 6,
                allowTrajectoryAimAssist = true,
                hitMask = LayerIndex.entityPrecise.mask,
                procCoefficient = 1f,
                stopperMask = LayerIndex.world.mask,
                force = 3f,
                hitCallback = EyelanderOnHit,

            };
            Eyelander = new DemoSwordClass(EyelanderBulletAttack, swordObject: ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/Weapons/Swords/eyelander.prefab"));
            bool EyelanderOnHit(BulletAttack bulletAttack, ref BulletAttack.BulletHit hitInfo)
            {
                if (hitInfo.hitHurtBox)
                {
                    //CharacterBody ownerBody = bulletAttack.owner.GetComponent<CharacterBody>();
                    //Main.Overheal(ownerBody.healthComponent, 0.1f);
                    //ownerBody.AddTimedBuff(Main.HealOnKill, 0.2f);
                    CharacterBody body = hitInfo.hitHurtBox.healthComponent.body;
                    if (body)
                    {
                        body.AddTimedBuff(Main.UpgradeOnKill, 10f);
                    }
                }
                return BulletAttack.DefaultHitCallbackImplementation(bulletAttack, ref hitInfo);
            }
            EyelanderSkillDef = SwordInit(Eyelander, typeof(DemoSword), ThunderkitAssets.LoadAsset<Sprite>("Assets/Demoman/Skills/DemoEyelanderSkillIcon.png"), "DemoEyelander", "DEMOMAN_EYELANDER_NAME", "DEMOMAN_EYELANDER_DESC");
            LanguageAPI.Add("DEMOMAN_EYELANDER_NAME", "Eyelander");
            GenerateSwordDescriptionToken("DEMOMAN_EYELANDER_DESC", Eyelander, "Eyelander is a weak sword, but generates Shiny Pearl on champion kill",
            "On hit: Heal for 4% of the maximum health\n" +
            "On kill: Heal for 15% of the maximum health");

            LanguageAPI.Add("DEMOMAN_DEFLECTOR_NAME", "Deflector");
            LanguageAPI.Add("DEMOMAN_DEFLECTOR_DESC", $"Feedbacker.");
            PillLauncherSkillDef = GrenadeLauncherInit(typeof(PillLauncher), ThunderkitAssets.LoadAsset<Sprite>("Assets/Demoman/Skills/DemoPillSkillIcon.png"), "DemoPillLauncher", "DEMOMAN_PILLLAUNCHER_NAME", "DEMOMAN_PILLLAUNCHER_DESC", false, 4, 4, 1, 4, 1);
            RocketLauncherSkillDef = GrenadeLauncherInit(typeof(RocketLauncher), ThunderkitAssets.LoadAsset<Sprite>("Assets/Demoman/Skills/DemoRocketSkillIcon.png"), "DemoRocketLauncher", "DEMOMAN_ROCKETLAUNCHER_NAME", "DEMOMAN_ROCKETLAUNCHER_DESC", false, 4, 4, 1, 4, 1);
            BombLauncherSkillDef = GrenadeLauncherInit(typeof(BombLauncher), ThunderkitAssets.LoadAsset<Sprite>("Assets/Demoman/Skills/DemoBombSkillIcon.png"), "DemoBombLauncher", "DEMOMAN_BOMBLAUNCHER_NAME", "DEMOMAN_BOMBLAUNCHER_DESC", false, 4, 4, 1, 4, 1);
            BombProjectile.GetComponent<BombComponent>().seekerState = BombLauncherSkillDef.activationState;
            StickyLauncherSkillDef = GrenadeLauncherInit(typeof(StickyLauncher), ThunderkitAssets.LoadAsset<Sprite>("Assets/Demoman/Skills/DemoStickySkillIcon.png"), "DemoStickyLauncher", "DEMOMAN_STICKYLAUNCHER_NAME", "DEMOMAN_STICKYLAUNCHER_DESC", true, 8, 1, 1);
            JumperLauncherSkillDef = GrenadeLauncherInit(typeof(JumperLauncher), ThunderkitAssets.LoadAsset<Sprite>("Assets/Demoman/Skills/DemoJumperStickySkillIcon.png"), "DemoJumperLauncher", "DEMOMAN_STICKYLAUNCHER_NAME", "DEMOMAN_STICKYLAUNCHER_DESC", true, 10, 1, 1);
            HookLauncherSkillDef = GrenadeLauncherInit(typeof(HookLauncher), ThunderkitAssets.LoadAsset<Sprite>("Assets/Demoman/Skills/DemoHookSkillIcon.png"), "DemoHookLauncher", "DEMOMAN_ROCKETLAUNCHER_NAME", "DEMOMAN_ROCKETLAUNCHER_DESC", false, 1, 2, 1, 1, 1);
            HookProjectile.GetComponent<HookComponent>().seekerState = HookLauncherSkillDef.activationState;
            //NukeLauncherSkillDef = GrenadeLauncherInit(typeof(NukeLauncher), ThunderkitAssets.LoadAsset<Sprite>("Assets/Demoman/Skills/DemoNukeSkillIcon.png"), "DemoNukeLauncher", "DEMOMAN_ROCKETLAUNCHER_NAME", "DEMOMAN_ROCKETLAUNCHER_DESC", false, 1, 60, 1, 1, 1);

            //QuickiebombLauncherSkillDef = GrenadeLauncherInit(typeof(QuickiebombLauncher), "DEMOMAN_QUICKIEBOMBLAUNCHER_NAME", "DEMOMAN_QUICKIEBOMBLAUNCHER_DESC", true, 4, 1, 1);
            MineLayerSkillDef = GrenadeLauncherInit(typeof(MineLayer), ThunderkitAssets.LoadAsset<Sprite>("Assets/Demoman/Skills/DemoMineSkillIcon.png"), "DemoMineLayer", "DEMOMAN_MINELAYER_NAME", "DEMOMAN_MINELAYER_DESC", true, 3, 1, 1);
            LanguageAPI.Add("DEMOMAN_MINELAYER_NAME", "Mine Layer");
            LanguageAPI.Add("DEMOMAN_MINELAYER_DESC", $"Mines!.");
            AntigravLauncherSkillDef = GrenadeLauncherInit(typeof(AntigravLauncher), ThunderkitAssets.LoadAsset<Sprite>("Assets/Demoman/Skills/DemoAntigravBombSkillIcon.png"), "DemoAntigravityBombLauncher", "DEMOMAN_QUICKIEBOMBLAUNCHER_NAME", "DEMOMAN_QUICKIEBOMBLAUNCHER_DESC", true, 4, 1, 1);
            LanguageAPI.Add("DEMOMAN_PILLLAUNCHER_NAME", "Grenade Launcher");
            GenerateGrenadeDescriptionToken("DEMOMAN_PILLLAUNCHER_DESC", PillProjectile, PillLauncherSkillDef, new PillLauncher());
            LanguageAPI.Add("DEMOMAN_ROCKETLAUNCHER_NAME", "Rocket Launcher");
            GenerateGrenadeDescriptionToken("DEMOMAN_ROCKETLAUNCHER_DESC", RocketProjectile, RocketLauncherSkillDef, new RocketLauncher());
            //LanguageAPI.Add("DEMOMAN_ROCKETLAUNCHER_DESC", $"Fire your head cannon.");
            LanguageAPI.Add("DEMOMAN_BOMBLAUNCHER_NAME", "Bomb Cannon");
            GenerateGrenadeDescriptionToken("DEMOMAN_BOMBLAUNCHER_DESC", BombProjectile, BombLauncherSkillDef, new BombLauncher());
            //LanguageAPI.Add("DEMOMAN_BOMBLAUNCHER_DESC", $"Fire your head cannon.");
            LanguageAPI.Add("DEMOMAN_STICKYLAUNCHER_NAME", "Sticky Launcher");
            GenerateGrenadeDescriptionToken("DEMOMAN_STICKYLAUNCHER_DESC", StickyProjectile, StickyLauncherSkillDef, new StickyLauncher());
            //LanguageAPI.Add("DEMOMAN_STICKYLAUNCHER_DESC", $"Fire your sticky launcher.");
            LanguageAPI.Add("DEMOMAN_QUICKIEBOMBLAUNCHER_NAME", "Quickiebomb Launcher");
            GenerateGrenadeDescriptionToken("DEMOMAN_QUICKIEBOMBLAUNCHER_DESC", AntigravProjectile, AntigravLauncherSkillDef, new AntigravLauncher());
            //LanguageAPI.Add("DEMOMAN_QUICKIEBOMBLAUNCHER_DESC", $"Fire your quickiebomb launcher.");
            HeavyShieldSkillDef = ShieldChargeInit(typeof(ShieldChargeHeavy), ThunderkitAssets.LoadAsset<Sprite>("Assets/Demoman/Skills/DemoHeavyShieldSkillIcon.png"), "DemoHeavyShield", "DEMOMAN_SHIELDHEAVY_NAME", "DEMOMAN_SHIELDHEAVY_DESC");
            LightShieldSkillDef = ShieldChargeInit(typeof(ShieldChargeLight), ThunderkitAssets.LoadAsset<Sprite>("Assets/Demoman/Skills/DemoLightShieldSkillIcon.png"), "DemoHeavyShield", "DEMOMAN_SHIELDHEAVY_NAME", "DEMOMAN_SHIELDHEAVY_DESC");
            //SpaceShield = ShieldChargeInit(typeof(ShieldChargeAntigravity), ThunderkitAssets.LoadAsset<Sprite>("Assets/Demoman/Skills/DemoHookSkillIcon.png"), "DemoFlyingShield", "DEMOMAN_SHIELDANTIGRAVITY_NAME", "DEMOMAN_SHIELDANTIGRAVITY_DESC");
            LanguageAPI.Add("DEMOMAN_SHIELDHEAVY_NAME", "Heavy Shield");
            LanguageAPI.Add("DEMOMAN_SHIELDHEAVY_DESC", $"charge.");
            LanguageAPI.Add("DEMOMAN_SHIELDANTIGRAVITY_NAME", "Antigravity Shield");
            LanguageAPI.Add("DEMOMAN_SHIELDANTIGRAVITY_DESC", $"charge.");
            DetonateSkillDef = DetonateInit();
            //AntigravDetonateSkillDef = AntigravDetonateInit();
            customDetonationSkills.Add(AntigravLauncherSkillDef, LaserTrapDetonateSkillDef);
            SwapSkillDef = Swap(ThunderkitAssets.LoadAsset<Sprite>("Assets/Demoman/Skills/DemoSwapSKillIcon.png"));
            SpecialOneSkillDef = SpecialInit(typeof(SpecialOneRedirector), ThunderkitAssets.LoadAsset<Sprite>("Assets/Demoman/Skills/DemoSpecial1SKillIcon.png"), "DemoSwordTornado", "DEMOMAN_SPECIALONE_NAME", "DEMOMAN_SPECIALONE_DESC", "Extra");
            //AltSpecialOneSkillDef = AltSpecialInit(SpecialOneSkillDef, typeof(SpecialOneSticky), SpecialOneSkillDef.icon, "DemoBombTornado", "DEMOMAN_SPECIALONEALT_NAME", "DEMOMAN_SPECIALTWOALT_DESC", "Extra");
            SpecialTwoSkillDef = SpecialInit(typeof(UltraInstinctRedirect), ThunderkitAssets.LoadAsset<Sprite>("Assets/Demoman/Skills/DemoSpecial2SKillIcon.png"), "DemoSwordStorm", "DEMOMAN_SPECIALONE_NAME", "DEMOMAN_SPECIALTWO_DESC", "Body");
            //AltSpecialTwoSkillDef = AltSpecialInit(SpecialTwoSkillDef, typeof(UltraInstinctSticky), SpecialTwoSkillDef.icon, "DemoBombStorm", "DEMOMAN_SPECIALTWOALT_NAME", "DEMOMAN_SPECIALTWOALT_DESC", "Extra");
            SlamSkillDef = SpecialInit(typeof(Slam), ThunderkitAssets.LoadAsset<Sprite>("Assets/Demoman/Skills/DemoSlamSkillIcon.png"), "DemoSlam", "DEMOMAN_SLAM_NAME", "DEMOMAN_SLAM_DESC", "Extra");
            //LockInSkillDef = SpecialInit(typeof(LockIn), null, "DemoLockIn", "DEMOMAN_LOCKIN_NAME", "DEMOMAN_LOCKIN_DESC", "Extra");
            LanguageAPI.Add("DEMOMAN_SPECIALONE_NAME", "Tornado");
            LanguageAPI.Add("DEMOMAN_SPECIALONE_DESC", $"Swor.");
            //LanguageAPI.Add("DEMOMAN_SPECIALONEALT_NAME", "Trap Barrage");
            //LanguageAPI.Add("DEMOMAN_SPECIALONEALT_DESC", $"Stick.");
            LanguageAPI.Add("DEMOMAN_SPECIALTWO_NAME", "Hit Storm");
            LanguageAPI.Add("DEMOMAN_SPECIALTWO_DESC", $"Swor.");
            //LanguageAPI.Add("DEMOMAN_SPECIALTWOALT_NAME", "Trap Storm");
            //LanguageAPI.Add("DEMOMAN_SPECIALTWOALT_DESC", $"Stick.");
            ManthreadsSkillDef = PassiveInit(ThunderkitAssets.LoadAsset<Sprite>("Assets/Demoman/Skills/DemoMannthreadsSkillIcon.png"), "DemoStompPassive", "DEMO_STOMPSKILL_NAME", "DEMO_STOMPSKILL_DESC");
            
            LoadoutSkillTitlesPlugin.AddTitleToken("DemoBody", 0, "LOADOUT_SKILL_PASSIVE");
            LoadoutSkillTitlesPlugin.AddTitleToken("DemoBody", 2, "LOADOUT_SKILL_PRIMARY");
            LoadoutSkillTitlesPlugin.AddTitleToken("DemoBody", 5, "DEMO_DETONATE_LOADOUT_TOKEN");
            LanguageAPI.Add("DEMO_DETONATE_LOADOUT_TOKEN", "Detonate");

            foreach (var variant in demoStickyFamily.variants)
            {
                stickySkills.Add(variant.skillDef);
            }
            InitSkillStats();
            InitProjectiles();
            InitShields();
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
        public static SkillDef SwordInit(DemoSwordClass demoSword, Type state, Sprite sprite, string name, string nameToken, string descToken, int requiredStock = 0, int rechargeStock = 1, int stockToConsume = 0, float rechargeInterval = 0f, int baseStock = 1)
        {
            
            GameObject commandoBodyPrefab = Main.DemoBody;

            SkillDef mySkillDef = ScriptableObject.CreateInstance<SkillDef>();
            mySkillDef.activationState = new SerializableEntityStateType(state);
            mySkillDef.activationStateMachineName = "Weapon";
            mySkillDef.baseMaxStock = baseStock;
            mySkillDef.baseRechargeInterval = rechargeInterval;
            mySkillDef.beginSkillCooldownOnSkillEnd = true;
            mySkillDef.canceledFromSprinting = false;
            mySkillDef.cancelSprintingOnActivation = true;
            mySkillDef.fullRestockOnAssign = true;
            mySkillDef.interruptPriority = InterruptPriority.Any;
            mySkillDef.isCombatSkill = true;
            mySkillDef.mustKeyPress = false;
            mySkillDef.rechargeStock = rechargeStock;
            mySkillDef.requiredStock = requiredStock;
            mySkillDef.stockToConsume = stockToConsume;
            mySkillDef.icon = sprite;
            mySkillDef.skillDescriptionToken = descToken;
            mySkillDef.skillName = nameToken;
            mySkillDef.skillNameToken = nameToken;
            (mySkillDef as ScriptableObject).name = name;
            ContentAddition.AddSkillDef(mySkillDef);
            SkillLocator skillLocator = commandoBodyPrefab.GetComponent<SkillLocator>();
            SkillFamily skillFamily = skillLocator.primary.skillFamily;
            AddSkillToFamily(ref skillFamily, mySkillDef);
            swordDictionary.Add(mySkillDef, demoSword);
            return mySkillDef;
        }
        private static SkillDef GrenadeLauncherInit(Type state, Sprite sprite, string name, string nameToken, string descToken, bool isSticky = false, int baseStocks = 4, float rechargeInterval = 4f, int requiredStock = 1, int rechargeStock = 1, int stockToConsume = 1)
        {
            GameObject commandoBodyPrefab = DemoBody;

            SkillDef mySkillDef = ScriptableObject.CreateInstance<SkillDef>();
            mySkillDef.activationState = new SerializableEntityStateType(state);
            if (isSticky)
            {
                mySkillDef.activationStateMachineName = "Weapon";
                Main.StickySkills.Add(mySkillDef);
            }
            else
            {
                mySkillDef.activationStateMachineName = "Weapon2";
            }
            mySkillDef.baseMaxStock = baseStocks;
            mySkillDef.baseRechargeInterval = rechargeInterval;
            mySkillDef.resetCooldownTimerOnUse = true;
            mySkillDef.beginSkillCooldownOnSkillEnd = true;
            mySkillDef.canceledFromSprinting = false;
            mySkillDef.cancelSprintingOnActivation = true;
            mySkillDef.fullRestockOnAssign = true;
            mySkillDef.interruptPriority = InterruptPriority.Any;
            mySkillDef.isCombatSkill = true;
            mySkillDef.mustKeyPress = false;
            mySkillDef.rechargeStock = rechargeStock;
            mySkillDef.requiredStock = requiredStock;
            mySkillDef.stockToConsume = stockToConsume;
            mySkillDef.icon = sprite;
            mySkillDef.skillDescriptionToken = descToken;
            mySkillDef.skillName = nameToken;
            mySkillDef.skillNameToken = nameToken;
            (mySkillDef as ScriptableObject).name = name;
            ContentAddition.AddSkillDef(mySkillDef);

            SkillLocator skillLocator = commandoBodyPrefab.GetComponent<SkillLocator>();
            SkillFamily skillFamily = null;
            if (isSticky)
            {
                skillFamily = demoStickyFamily;
            }
            else
            {
                skillFamily = skillLocator.secondary.skillFamily;
            }
            AddSkillToFamily(ref skillFamily, mySkillDef);
            return mySkillDef;
        }
        private static SkillDef ShieldChargeInit(Type state, Sprite sprite, string name, string nameToken, string descToken, int baseStocks = 1, float rechargeInterval = 6f, int requiredStock = 1, int rechargeStock = 1, int stockToConsume = 1)
        {
            GameObject commandoBodyPrefab = DemoBody;
            SkillDef mySkillDef = ScriptableObject.CreateInstance<SkillDef>();
            mySkillDef.activationState = new SerializableEntityStateType(state);
            mySkillDef.activationStateMachineName = "Body";
            mySkillDef.baseMaxStock = baseStocks;
            mySkillDef.baseRechargeInterval = rechargeInterval;
            mySkillDef.beginSkillCooldownOnSkillEnd = true;
            mySkillDef.canceledFromSprinting = false;
            mySkillDef.cancelSprintingOnActivation = false;
            mySkillDef.fullRestockOnAssign = true;
            mySkillDef.interruptPriority = InterruptPriority.Any;
            mySkillDef.isCombatSkill = false;
            mySkillDef.mustKeyPress = true;
            mySkillDef.rechargeStock = rechargeStock;
            mySkillDef.requiredStock = requiredStock;
            mySkillDef.stockToConsume = stockToConsume;
            mySkillDef.icon = sprite;
            mySkillDef.skillDescriptionToken = descToken;
            mySkillDef.skillName = nameToken;
            mySkillDef.skillNameToken = nameToken;
            (mySkillDef as ScriptableObject).name = name;
            ContentAddition.AddSkillDef(mySkillDef);
            SkillLocator skillLocator = commandoBodyPrefab.GetComponent<SkillLocator>();

            SkillFamily skillFamily = skillLocator.utility.skillFamily;
            AddSkillToFamily(ref skillFamily, mySkillDef);
            return mySkillDef;
        }
        private static SkillDef SpecialInit(Type state, Sprite sprite, string name, string nameToken, string descToken, string stateName, float recharge = 10f)
        {
            GameObject commandoBodyPrefab = DemoBody;
            SkillDef mySkillDef = ScriptableObject.CreateInstance<SkillDef>();
            mySkillDef.activationState = new SerializableEntityStateType(state);
            mySkillDef.activationStateMachineName = stateName;
            mySkillDef.baseMaxStock = 1;
            mySkillDef.baseRechargeInterval = recharge;
            mySkillDef.beginSkillCooldownOnSkillEnd = true;
            mySkillDef.canceledFromSprinting = false;
            mySkillDef.cancelSprintingOnActivation = true;
            mySkillDef.fullRestockOnAssign = true;
            mySkillDef.interruptPriority = InterruptPriority.Any;
            mySkillDef.isCombatSkill = true;
            mySkillDef.mustKeyPress = true;
            mySkillDef.rechargeStock = 1;
            mySkillDef.requiredStock = 1;
            mySkillDef.stockToConsume = 1;
            mySkillDef.icon = sprite;
            mySkillDef.skillDescriptionToken = descToken;
            mySkillDef.skillName = nameToken;
            mySkillDef.skillNameToken = nameToken;
            (mySkillDef as ScriptableObject).name = name;
            ContentAddition.AddSkillDef(mySkillDef);
            SkillLocator skillLocator = commandoBodyPrefab.GetComponent<SkillLocator>();
            SkillFamily skillFamily = skillLocator.special.skillFamily;
            AddSkillToFamily(ref skillFamily, mySkillDef);
            return mySkillDef;
        }
        private static SkillDef DetonateInit()
        {
            //GameObject commandoBodyPrefab = DemomanSurvivor.survivor;
            LanguageAPI.Add("DEMOMAN_DETONATE_NAME", "Detonate");
            LanguageAPI.Add("DEMOMAN_DETONATE_DESC", $"Detonate.");
            SkillDef mySkillDef = ScriptableObject.CreateInstance<SkillDef>();
            mySkillDef.activationState = new SerializableEntityStateType(typeof(Detonate));
            mySkillDef.activationStateMachineName = "Extra";
            mySkillDef.baseMaxStock = 1;
            mySkillDef.baseRechargeInterval = 0f;
            mySkillDef.beginSkillCooldownOnSkillEnd = false;
            mySkillDef.canceledFromSprinting = false;
            mySkillDef.cancelSprintingOnActivation = false;
            mySkillDef.fullRestockOnAssign = true;
            mySkillDef.interruptPriority = InterruptPriority.Any;
            mySkillDef.isCombatSkill = false;
            mySkillDef.mustKeyPress = true;
            mySkillDef.rechargeStock = 1;
            mySkillDef.requiredStock = 1;
            mySkillDef.stockToConsume = 1;
            mySkillDef.icon = ThunderkitAssets.LoadAsset<Sprite>("Assets/Demoman/Skills/DemoDetonateSkillIcon.png");
            mySkillDef.skillDescriptionToken = "DEMOMAN_DETONATE_DESC";
            mySkillDef.skillName = "DEMOMAN_DETONATE_NAME";
            mySkillDef.skillNameToken = "DEMOMAN_DETONATE_NAME";
            ContentAddition.AddSkillDef(mySkillDef);
            SkillFamily skillFamily = demoDetonateFamily;
            AddSkillToFamily(ref skillFamily, mySkillDef);
            return mySkillDef;
        }
        /*
        private static SkillDef AntigravDetonateInit()
        {
            //GameObject commandoBodyPrefab = DemomanSurvivor.survivor;
            LanguageAPI.Add("DEMOMAN_ANTIGRAVDETONATE_NAME", "Antigrav Charge");
            LanguageAPI.Add("DEMOMAN_ANTIGRAVDETONATE_DESC", $"Charge into an antigrav bomb.");
            SkillDef mySkillDef = ScriptableObject.CreateInstance<SkillDef>();
            mySkillDef.activationState = new SerializableEntityStateType(typeof(AntigravDetonate));
            mySkillDef.activationStateMachineName = "Extra";
            mySkillDef.baseMaxStock = 1;
            mySkillDef.baseRechargeInterval = 0f;
            mySkillDef.beginSkillCooldownOnSkillEnd = true;
            mySkillDef.canceledFromSprinting = false;
            mySkillDef.cancelSprintingOnActivation = false;
            mySkillDef.fullRestockOnAssign = true;
            mySkillDef.interruptPriority = InterruptPriority.Any;
            mySkillDef.isCombatSkill = false;
            mySkillDef.mustKeyPress = true;
            mySkillDef.rechargeStock = 1;
            mySkillDef.requiredStock = 1;
            mySkillDef.stockToConsume = 1;
            mySkillDef.icon = null;
            mySkillDef.skillDescriptionToken = "DEMOMAN_ANTIGRAVDETONATE_DESC";
            mySkillDef.skillName = "DEMOMAN_ANTIGRAVDETONATE_NAME";
            mySkillDef.skillNameToken = "DEMOMAN_ANTIGRAVDETONATE_NAME";
            ContentAddition.AddSkillDef(mySkillDef);
            return mySkillDef;
        }*/
        public static SkillDef PassiveInit(Sprite sprite, string name, string nameToken, string descToken)
        {
            GameObject commandoBodyPrefab = Main.DemoBody;

            SkillDef mySkillDef = ScriptableObject.CreateInstance<SkillDef>();
            mySkillDef.activationState = default;
            mySkillDef.activationStateMachineName = default;
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
            SkillDef mySkillDef = ScriptableObject.CreateInstance<SkillDef>();
            mySkillDef.activationState = new SerializableEntityStateType(typeof(ChangeWeapons));
            mySkillDef.activationStateMachineName = "Extra";
            mySkillDef.baseMaxStock = 1;
            mySkillDef.baseRechargeInterval = 0f;
            mySkillDef.beginSkillCooldownOnSkillEnd = false;
            mySkillDef.canceledFromSprinting = false;
            mySkillDef.cancelSprintingOnActivation = false;
            mySkillDef.fullRestockOnAssign = true;
            mySkillDef.interruptPriority = InterruptPriority.Any;
            mySkillDef.isCombatSkill = false;
            mySkillDef.mustKeyPress = true;
            mySkillDef.rechargeStock = 1;
            mySkillDef.requiredStock = 1;
            mySkillDef.stockToConsume = 1;
            mySkillDef.icon = sprite;
            mySkillDef.skillDescriptionToken = "DEMOMAN_SWAP_DESC";
            mySkillDef.skillName = "DEMOMAN_SWAP_NAME";
            mySkillDef.skillNameToken = "DEMOMAN_SWAP_NAME";
            ContentAddition.AddSkillDef(mySkillDef);
            return mySkillDef;
        }
        public class DemoSwordClass(BulletAttack bulletAttack, float swingUpTime = 0.5f, float swingDownTime = 0.5f, GameObject swordObject = null)
        {
            public BulletAttack bulletAttack = bulletAttack;
            public float swingUpTime = swingUpTime;
            public float swingDownTime = swingDownTime;
            public GameObject swordObject = swordObject;
        }
        public class DemoStickyClass(GameObject stickyObject, float damage)
        {
            public GameObject stickyObject = stickyObject;
            public float damage = damage;
        }
        public static void InitSwordAttacks()
        {

            
            swordDictionary.Add(SkullcutterSkillDef, Skullcutter);
            
            swordDictionary.Add(ZatoichiSkillDef, Zatoichi);
            
            swordDictionary.Add(CaberSkillDef, Caber);
            BulletAttack DeflectorBulletAttack = new BulletAttack()
            {
                radius = 2f,
                damage = 2,
                bulletCount = 1,
                maxDistance = 8,
                allowTrajectoryAimAssist = false,
                hitMask = LayerIndex.entityPrecise.mask + LayerIndex.projectile.mask,
                procCoefficient = 1f,
                stopperMask = LayerIndex.world.mask,
                force = 3f,
                hitCallback = DeflectorOnHit

            };
            Deflector = new DemoSwordClass(DeflectorBulletAttack, swordObject: ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/Weapons/Swords/eyelander.prefab"));
            bool DeflectorOnHit(BulletAttack bulletAttack, ref BulletAttack.BulletHit hitInfo)
            {
                ProjectileController projectile = hitInfo.collider.GetComponent<ProjectileController>();
                CharacterBody characterBody = bulletAttack.owner.GetComponent<CharacterBody>();
                if (projectile)
                {

                    projectile.rigidbody.velocity = characterBody.inputBank.aimDirection * (projectile.rigidbody.velocity.magnitude * 3);
                    projectile.teamFilter.teamIndex = characterBody.teamComponent.teamIndex;
                }
                CharacterBody enemyBody = hitInfo.collider.GetComponent<HurtBox>() ? hitInfo.collider.GetComponent<HurtBox>().healthComponent.body : null;
                if (enemyBody != null)
                {
                    foreach (var state in enemyBody.GetComponents<EntityStateMachine>())
                    {
                        state.SetNextStateToMain();
                    }
                }
                return BulletAttack.DefaultHitCallbackImplementation(bulletAttack, ref hitInfo);
            }
            //swordDictionary.Add(DeflectorSkillDef, Deflector);
            
            swordDictionary.Add(EyelanderSkillDef, Eyelander);
        }
        public static void GenerateSwordDescriptionToken(string token, DemoSwordClass demoSword, string before = "", string after = "")
        {
            LanguageAPI.Add(token, $"" +
                before +
                "\n\n" +
                "Base damage: " + LangueagePrefix((demoSword.bulletAttack.damage * 100).ToString() + "%", LanguagePrefixEnum.Damage) + "\n" +
                "Swing up time: " + LangueagePrefix(demoSword.swingUpTime.ToString() + "s", LanguagePrefixEnum.Damage) + "\n" +
                "Swing down time: " + LangueagePrefix(demoSword.swingDownTime.ToString() + "s", LanguagePrefixEnum.Damage) + "\n" +
                "Range: " + LangueagePrefix(demoSword.bulletAttack.maxDistance.ToString() + "m", LanguagePrefixEnum.Damage) + "\n" +
                "Piercing: " + LangueagePrefix(demoSword.bulletAttack.stopperMask == (demoSword.bulletAttack.stopperMask | 1 << LayerIndex.entityPrecise.mask) ? "False" : "True", LanguagePrefixEnum.Damage) + "\n" +
                "Radius: " + LangueagePrefix(demoSword.bulletAttack.radius.ToString() + "m", LanguagePrefixEnum.Damage) + "\n" +
                "Proc: " + LangueagePrefix(demoSword.bulletAttack.procCoefficient.ToString(), LanguagePrefixEnum.Damage) + "\n" +
                after);
        }
        public static void GenerateGrenadeDescriptionToken(string token, GameObject projectile, SkillDef skillDef, GrenadeLauncher grenadeLauncher, string before = "", string after = "")
        {
            ProjectileImpactExplosion projectileImpactExplosion = projectile.GetComponent<ProjectileImpactExplosion>();
            DemoExplosionComponent demoExplosionComponent = projectile.GetComponent<DemoExplosionComponent>();
            StickyComponent stickyComponent = projectile.GetComponent<StickyComponent>();
            ProjectileSimple projectileSimple = projectile.GetComponent<ProjectileSimple>();
            Rigidbody rigidbody = projectile.GetComponent<Rigidbody>();
            string explosionString = "";
            if (projectileSimple)
            {
                explosionString += "" +
                    "Speed: " + LangueagePrefix((projectileSimple.desiredForwardSpeed).ToString() + "m/s", LanguagePrefixEnum.Damage) + "\n";
            }
            if (rigidbody)
            {
                explosionString += "" +
                    "Affected by gravity: " + LangueagePrefix(rigidbody.useGravity ? "True" : "False", LanguagePrefixEnum.Damage) + "\n";
            }
            if (projectileImpactExplosion)
            {
                //explosionString += "\n";
                explosionString += "" +
                    "Explosion radius: " + LangueagePrefix((projectileImpactExplosion.blastRadius).ToString(), LanguagePrefixEnum.Damage) + "\n" +
                    "Projectile lifetime: " + LangueagePrefix((projectileImpactExplosion.lifetime).ToString(), LanguagePrefixEnum.Damage) + "\n";
            }
            if (stickyComponent)
            {
                explosionString += "" +
                    "Can stick: " + LangueagePrefix(stickyComponent.isStickable ? "True" : "False", LanguagePrefixEnum.Damage) + "\n" +
                    "Arm time: " + LangueagePrefix((stickyComponent.armTime).ToString() + "s", LanguagePrefixEnum.Damage) + "\n" +
                    "Detonation time: " + LangueagePrefix((stickyComponent.detonationTime).ToString() + "s", LanguagePrefixEnum.Damage) + "\n" +
                    "";
            }
            if (demoExplosionComponent)
            {
                explosionString += "" +
                    "Knockback: " + LangueagePrefix((demoExplosionComponent.enemyPower).ToString(), LanguagePrefixEnum.Damage) + "\n" +
                    "Self knockback: " + LangueagePrefix((demoExplosionComponent.selfPower).ToString(), LanguagePrefixEnum.Damage) + "\n" +
                    "";
            }
            
            LanguageAPI.Add(token, $"" +
                before +
                "\n\n" +
                "Base damage: " + LangueagePrefix((grenadeLauncher.damage * 100).ToString() + "%", LanguagePrefixEnum.Damage) + "\n" +
                "Fire rate: " + LangueagePrefix((grenadeLauncher.fireRate).ToString() + "s", LanguagePrefixEnum.Damage) + "\n" +
                "Base stocks: " + LangueagePrefix((skillDef.baseMaxStock).ToString(), LanguagePrefixEnum.Damage) + "\n" +
                "Reload time" + LangueagePrefix((skillDef.baseRechargeInterval).ToString(), LanguagePrefixEnum.Damage) + "\n" +
                "Stocks to reload" + LangueagePrefix((skillDef.rechargeStock).ToString(), LanguagePrefixEnum.Damage) + "\n" +
                "Charge time: " + LangueagePrefix(grenadeLauncher.canBeCharged ? ((grenadeLauncher.chargeCap).ToString() + "s") : "Can't be charged", LanguagePrefixEnum.Damage) + "\n" +
                "");
        }
        private static void InitProjectiles()
        {
            StickyLauncherObject = new DemoStickyClass(StickyProjectile, 6f);
            bombProjectiles.Add(StickyLauncherSkillDef, StickyLauncherObject);
            MineLayerObject = new DemoStickyClass(MineProjectile, 9f);
            bombProjectiles.Add(MineLayerSkillDef, MineLayerObject);
            AntigravLauncherObject = new DemoStickyClass(AntigravProjectile, 7f);
            bombProjectiles.Add(AntigravLauncherSkillDef, AntigravLauncherObject);
            JumperLauncherObject = new DemoStickyClass(JumperProjectile, 0f);
            bombProjectiles.Add(JumperLauncherSkillDef, JumperLauncherObject);
        }
        private static void InitShields()
        {
            shieldDictionary.Add(HeavyShieldSkillDef, ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/Weapons/Shield/HeavyShield.prefab"));
            shieldDictionary.Add(LightShieldSkillDef, ThunderkitAssets.LoadAsset<GameObject>("Assets/Demoman/Weapons/Shield/LightShield.prefab"));
        }
        private static void InitSkillStats()
        {

            Action<CharacterBody, RecalculateStatsAPI.StatHookEventArgs> HeavyShieldStats = new Action<CharacterBody, RecalculateStatsAPI.StatHookEventArgs>(ApplyHeavyShieldStats);
            void ApplyHeavyShieldStats(CharacterBody characterBody, RecalculateStatsAPI.StatHookEventArgs stats)
            {
                stats.sprintSpeedAdd -= 2;
            }
            skillsToStats.Add(HeavyShieldSkillDef, HeavyShieldStats);
            Action<CharacterBody, RecalculateStatsAPI.StatHookEventArgs> SkullcutterStats = new Action<CharacterBody, RecalculateStatsAPI.StatHookEventArgs>(ApplySkullcutterStats);
            void ApplySkullcutterStats(CharacterBody characterBody, RecalculateStatsAPI.StatHookEventArgs stats)
            {
                stats.armorAdd += 15f;
            }
            skillsToStats.Add(SkullcutterSkillDef, SkullcutterStats);
        }

        //public abstract class BombProjectileInfo
        //{
        //    public abstract GameObject projectile {  get; }
        //    public abstract float damage { get; }
        //    public abstract DamageType damageType { get; }
        //}
        public class DemoSword : BaseState, ISkillState
        {
            private float stopwatch = 0f;
            private bool fired = true;
            public GenericSkill activatorSkillSlot { get; set; }

            public float range;
            public float radius;
            public bool isCrit;
            public GameObject swingEffect = SwingEffect;
            public override void OnEnter()
            {
                base.OnEnter();
                if (stopwatch <= 0)
                {
                    PlayAnimation("Gesture, Override", "Swing", "Slash.playbackRate", 1f / base.attackSpeedStat);
                    base.StartAimMode();
                    stopwatch = 0.5f / base.attackSpeedStat;
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
                if (isAuthority && !fired && stopwatch < 0)
                {
                    BulletAttack bulletAttack = swordDictionary.ContainsKey(activatorSkillSlot.skillDef) ? ModifyAttack(swordDictionary[activatorSkillSlot.skillDef].bulletAttack) : DefaultSword.bulletAttack;
                    SwingSword(bulletAttack);
                    fired = true;
                    stopwatch = 0.5f / base.attackSpeedStat;

                }
                else if (isAuthority && stopwatch <= 0)
                {
                    outer.SetNextStateToMain();
                }


            }
            public override void OnExit()
            {
                base.OnExit();

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
                    isCrit = isCrit,
                    origin = GetAimRay().origin,
                    owner = gameObject,
                    hitCallback = bulletAttack.hitCallback,

                };
                return bulletAttack2;
            }
            public virtual void SwingSword(BulletAttack bulletAttack)
            {
                GameObject swingEffectcopy = GameObject.Instantiate(swingEffect);
                swingEffectcopy.transform.position = inputBank ? inputBank.aimOrigin : transform.position;
                swingEffectcopy.transform.rotation = Quaternion.LookRotation(inputBank ? inputBank.aimDirection : transform.forward);
                ParticleSystem particleSystem = swingEffectcopy.transform.GetChild(0).GetComponent<ParticleSystem>();
                var vel = particleSystem.velocityOverLifetime;
                vel.speedModifier = bulletAttack.maxDistance;
                Util.PlaySound(DemoSwordSwingSound.playSoundString, gameObject);
                bulletAttack.Fire();
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
            public abstract float stageTwoPercentage { get; }
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
                Util.CleanseBody(characterBody, true, false, false, true, true, false);
                base.characterBody.isSprinting = true;
                demoComponent = characterBody.GetComponent<DemoComponent>();
                characterBody.armor += armor;
                characterBody.RecalculateStats();
                previousMoveVector = transform.position;
                Util.PlaySound(DemoChargeWindUpSound.playSoundString, gameObject);
            }
            public virtual void OnEnterAuthority()
            {
                chargeMeterHUD = demoComponent ? demoComponent.meterImage : null;
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

            }
            public virtual void FixedUpdateAuthority()
            {
                RaycastHit hit;
                if (chargeMeterHUD)
                {
                    chargeMeterHUD.fillAmount = chargePercentage;
                }
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
                        outer.SetNextStateToMain();
                        HealthComponent healthComponent = hit.collider.GetComponent<HurtBox>()?.healthComponent;
                        if (healthComponent)
                        {
                            var bonk = new DamageInfo
                            {
                                damage = damageStat * 3 * chargePercentage,
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
                    }
                    
                    
                }
            }
            public virtual void OnStageOne()
            {
                characterBody.SetBuffCount(Main.ExtraSwordDamage.buffIndex, 35);
            }
            public virtual void OnStageOneAuthority()
            {
                if (chargeMeterHUD) chargeMeterHUD.color = Color.yellow;
            }
            public virtual void OnStageTwo()
            {
                characterBody.SetBuffCount(Main.ExtraSwordDamage.buffIndex, 300);
            }
            public virtual void OnStageTwoAuthority()
            {
                if (chargeMeterHUD) chargeMeterHUD.color = Color.red;
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
                int buffCount = characterBody.GetBuffCount(ExtraSwordDamage);
                characterBody.SetBuffCount(Main.ExtraSwordDamage.buffIndex, 0);
                for (int i = 0; i < buffCount; i++)
                {
                    characterBody.AddTimedBuff(ExtraSwordDamage, 0.6f);
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
        public abstract class GrenadeLauncher : BaseState, ISkillState
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
            public abstract GrenadeLauncherChargeAffection[] chargeTags { get; }
            public GenericSkill activatorSkillSlot { get; set; }
            public DemoComponent demoComponent;

            public float charge = 0f;
            private bool released = false;
            private Image chargeMeter;
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
                if (base.isAuthority)
                {
                    demoComponent = gameObject.GetComponent<DemoComponent>();
                    chargeMeter = demoComponent ? demoComponent.meterImage : null;
                    if (chargeMeter)
                    {
                        demoComponent.updateMeter = false;
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
                    demoComponent.updateMeter = true;
                }
                TrajectoryAimAssist.ApplyTrajectoryAimAssist(ref aimRay, projectile, base.gameObject, 1f);
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
                if (chargeMeter)
                {
                    chargeMeter.fillAmount = 0f;
                }
                ModifiyProjectileFireInfo(ref fireProjectileInfo);
                ProjectileManager.instance.FireProjectile(fireProjectileInfo);
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
            public override void FixedUpdate()
            {
                base.FixedUpdate();
                charge += GetDeltaTime();
                if (stopwatch > 0)
                {
                    stopwatch -= GetDeltaTime();
                }
                if (isAuthority)
                {
                    if (!stopCharge && chargeMeter)
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
                    if (fired && stopwatch <= 0)
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
                        demoComponent.updateMeter = true;
                        chargeMeter.fillAmount = 0f;
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
            public override float damage => 5f;

            public override GameObject projectile => Main.PillProjectile;

            public override bool canBeCharged => false;

            public override float fireRate => 0.5f;

            public override bool isPrimary => false;

            public override float chargeCap => 0f;

            public override GrenadeLauncherChargeAffection[] chargeTags => new GrenadeLauncherChargeAffection[] { };
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
            public override void OnProjectileFired()
            {
            }
            public override void OnEnter()
            {
                base.OnEnter();
                if (isAuthority)
                {
                    FireProjectile(false);
                }
            }
            public override void OnEarlyChargeEnd()
            {
                outer.SetNextStateToMain();
            }
            public override void OnFullChargeEnd()
            {
                outer.SetNextStateToMain();
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

        }
        public class BombLauncher : GrenadeLauncher
        {
            public override float damage => 4f;

            public override GameObject projectile => Main.BombProjectile;

            public override bool canBeCharged => true;

            public override float fireRate => 0.5f;

            public override bool isPrimary => false;

            public override float chargeCap => 1f;
            public override GrenadeLauncherChargeAffection[] chargeTags => new GrenadeLauncherChargeAffection[] { };

        }
        public class StickyLauncher : GrenadeLauncher
        {
            public override float damage => 6f;
            public override float fireRate => 0.3f;

            public override GameObject projectile => Main.StickyProjectile;

            public override bool canBeCharged => true;

            public override bool isPrimary => true;

            public override float chargeCap => 1f;
            public override GrenadeLauncherChargeAffection[] chargeTags => new GrenadeLauncherChargeAffection[] { GrenadeLauncherChargeAffection.Speed };

        }
        public class JumperLauncher : GrenadeLauncher
        {
            public override float damage => 0f;
            public override float fireRate => 0.2f;

            public override GameObject projectile => Main.JumperProjectile;

            public override bool canBeCharged => true;

            public override bool isPrimary => true;

            public override float chargeCap => 1f;
            public override GrenadeLauncherChargeAffection[] chargeTags => new GrenadeLauncherChargeAffection[] { GrenadeLauncherChargeAffection.Speed };

        }
        public class AntigravLauncher : GrenadeLauncher
        {
            public override float damage => 7f;
            public override float fireRate => 0.6f;

            public override GameObject projectile => Main.AntigravProjectile;

            public override bool canBeCharged => true;
            public override bool isPrimary => true;

            public override float chargeCap => 2f;
            public override GrenadeLauncherChargeAffection[] chargeTags => new GrenadeLauncherChargeAffection[] { GrenadeLauncherChargeAffection.Speed };

        }
        public class MineLayer : GrenadeLauncher
        {
            public override float damage => 9f;
            public override float fireRate => 0.6f;

            public override GameObject projectile => Main.MineProjectile;

            public override bool canBeCharged => false;
            public override bool isPrimary => true;

            public override float chargeCap => 0f;
            public override GrenadeLauncherChargeAffection[] chargeTags => new GrenadeLauncherChargeAffection[] { GrenadeLauncherChargeAffection.Speed };

        }
    }
    public class ShieldChargeHeavy : ShieldCharge
    {
        public override float chargeMaxMeter => 1.75f;

        public override float chargeControl => 2.5f;

        public override float stageOnePercentage => 0.25f;

        public override float stageTwoPercentage => 0.6f;

        public override float chargeSpeed => 2.5f;

        public override float armor => 100f;
    }
    public class ShieldChargeLight : ShieldCharge
    {
        public override float chargeMaxMeter => 1.75f;

        public override float chargeControl => 90f;

        public override float stageOnePercentage => 0.25f;

        public override float stageTwoPercentage => 0.6f;

        public override float chargeSpeed => 2.5f;

        public override float armor => 20f;
        public override void OnStageTwo()
        {
        }
        public override void OnStageTwoAuthority()
        {
        }
    }
    public class ShieldChargeAntigravity : ShieldCharge
    {
        public override float chargeMaxMeter => 1f;

        public override float chargeControl => 90f;

        public override float stageOnePercentage => 0.2f;

        public override float stageTwoPercentage => 0.9f;

        public override float chargeSpeed => 2f;

        public override float armor => 0f;

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
                DetonateAllStickies(demoComponent);
            }
            outer.SetNextStateToMain();
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
            EntityStateMachine[] entityStateMachines = gameObject.GetComponents<EntityStateMachine>();
            EntityStateMachine bodyStateMachine = null;
            EntityStateMachine weaponStateMachine = null;
            foreach (EntityStateMachine entityStateMachine in entityStateMachines)
            {
                if (entityStateMachine.customName == "Weapon")
                {
                    weaponStateMachine = entityStateMachine;
                }
                if (entityStateMachine.customName == "Body")
                {
                    bodyStateMachine = entityStateMachine;
                }
            }
            if (demoComponent && demoComponent.isSwapped)
            {
                if (weaponStateMachine)
                {
                    weaponStateMachine.SetNextState(new SpecialOneSticky());
                    outer.SetNextStateToMain();
                }
                else
                {
                    outer.SetNextState(new SpecialOneSticky());
                }
                
            }
            else
            {
                if (bodyStateMachine)
                {
                    bodyStateMachine.SetNextState(new SpecialOneSword());
                    outer.SetNextStateToMain();
                }
                else
                {
                    GetComponent<EntityStateMachine>().SetNextState(new SpecialOneSword());
                    outer.SetNextStateToMain();
                }
                
            }
        }
    }
    public class SpecialSwordSpiner : MonoBehaviour
    {
        public SpecialOneSword Sword;
        public BulletAttack bulletAttack = DefaultSword.bulletAttack;
        public List<CharacterBody> charactersBlacklist = new List<CharacterBody>();
        public Dictionary<Collider, CharacterBody> keyValuePairs = new Dictionary<Collider, CharacterBody>();
        public void Start()
        {
        }
        public void OnTriggerStay(Collider collider)
        {
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
                    radius = 1f,
                    aimVector = Vector3.up,
                    damage = Sword.damageStat * (bulletAttack != null ? bulletAttack.damage : 3f),
                    bulletCount = 1,
                    spreadPitchScale = 0f,
                    spreadYawScale = 0f,
                    maxSpread = 0f,
                    minSpread = 0f,
                    maxDistance = 1f,
                    allowTrajectoryAimAssist = false,
                    hitMask = LayerIndex.entityPrecise.mask,
                    procCoefficient = bulletAttack != null ? bulletAttack.procCoefficient : 1f,
                    stopperMask = LayerIndex.entityPrecise.mask,
                    damageType = new DamageTypeCombo(DamageType.Generic, DamageTypeExtended.Generic, DamageSource.Special),
                    force = bulletAttack != null ? bulletAttack.force : 1f,
                    falloffModel = BulletAttack.FalloffModel.None,
                    damageColorIndex = DamageColorIndex.Default,
                    isCrit = Sword.RollCrit(),
                    origin = characterBody.mainHurtBox.transform.position,
                    owner = Sword.gameObject,
                    hitCallback = bulletAttack != null ? bulletAttack.hitCallback : null,

                };
                bulletAttack2.Fire();
                charactersBlacklist.Add(characterBody);
            }
        }
    }
    public class SpecialOneSword : BaseState
    {
        //public OverlapAttack overlapAttack;
        private float stopwatch = 0f;
        private float timer = 5f;
        private float hitStopwatch = 0f;
        private float hitTimer = 1f;
        private bool fired = false;
        private float speed = 1f;
        private float animationTimer = 1f;
        private Vector3 currentVector = Vector3.zero;
        private BulletAttack bulletAttack;
        private SpecialSwordSpiner specialSwordSpiner;
        private GameObject spinner;
        private CharacterDirection characterDirection;
        private Vector3 rotationVector = Vector3.zero;
        public override void OnEnter()
        {
            base.OnEnter();
            characterDirection = GetComponent<CharacterDirection>();
            if (!characterDirection) outer.SetNextStateToMain();
            fired = true;
            speed = base.attackSpeedStat;
            PlayAnimation("FullBody, Override", "SpinStart", "Slash.playbackRate", 0.5f / 1.2f / speed);
            currentVector = inputBank ? inputBank.moveVector : transform.forward;
            if (characterMotor)
            {
                characterMotor.useGravity = false;
                characterMotor.velocity.y = 0f;
            }
            bulletAttack = swordDictionary.ContainsKey(skillLocator.primary.skillDef) ? swordDictionary[skillLocator.primary.skillDef].bulletAttack : DefaultSword.bulletAttack;
            characterBody.AddBuff(RoR2Content.Buffs.ArmorBoost);
            spinner = GameObject.Instantiate(SpinEffect);
            spinner.transform.parent = characterDirection.targetTransform;
            spinner.transform.rotation = characterDirection.targetTransform.rotation;
            spinner.transform.position = inputBank.aimOrigin;
            spinner.transform.localScale = new Vector3(6f, 1f, 6f);
            rotationVector = characterDirection.forward;
            specialSwordSpiner = spinner.GetComponent<SpecialSwordSpiner>();
            specialSwordSpiner.Sword = this;
            specialSwordSpiner.bulletAttack = bulletAttack;
        }
        public override void OnExit()
        {
            base.OnExit();
            if (characterMotor) characterMotor.useGravity = true;
            characterBody.RemoveBuff(RoR2Content.Buffs.ArmorBoost);
            Destroy(spinner);
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (inputBank && characterMotor)
            {
                Vector3 otherVector = new Vector3(0f, inputBank.aimDirection.y, 0f);
                if (inputBank.jump.down) otherVector.y += 1;
                if (otherVector.y > 1) otherVector.y = 1f;
                currentVector = Vector3.MoveTowards(currentVector, inputBank.moveVector + new Vector3(0f, otherVector.y, 0f), 1 * GetDeltaTime());
                
                characterMotor.rootMotion += currentVector * characterMotor.walkSpeed * 3 * GetDeltaTime();
            }
            rotationVector = Quaternion.AngleAxis(1800 * Time.fixedDeltaTime * attackSpeedStat, Vector3.up) * rotationVector;
            characterDirection.forward = rotationVector;
            hitStopwatch += Time.fixedDeltaTime;
            if (hitStopwatch >= hitTimer / 5 / attackSpeedStat)
            {
                specialSwordSpiner.charactersBlacklist.Clear();
                hitStopwatch = 0f;
            }
            timer -= Time.fixedDeltaTime;
            if (timer < 0)
            {
                PlayAnimation("FullBody, Override", "SpinEnd", "Slash.playbackRate", 1 / speed / 2);
                outer.SetNextStateToMain();
            }
            /*
            animationTimer += GetDeltaTime();
            if (animationTimer > 0.5f / speed / 3)
            {
                PlayAnimation("FullBody, Override", "Spin", "Slash.playbackRate", 1 / 3 / speed);
                animationTimer = 0f;
            }
            stopwatch += Time.fixedDeltaTime;
            if (!fired || stopwatch > 0.5f / speed)
            {
                fired = false;
                timer -= Time.fixedDeltaTime;
                if (inputBank && characterMotor)
                {
                    currentVector = Vector3.MoveTowards(currentVector, inputBank.moveVector + new Vector3(0f, inputBank.aimDirection.y, 0f), 1 * GetDeltaTime());
                    characterMotor.rootMotion += currentVector * characterMotor.walkSpeed * 3 * GetDeltaTime();
                }
                
                if (stopwatch > 0.2f)
                {
                    stopwatch = 0f;
                    if (isAuthority)
                    {
                        BulletAttack bulletAttack = swordDictionary.ContainsKey(skillLocator.primary.skillDef) ? swordDictionary[skillLocator.primary.skillDef].bulletAttack : DefaultSword.bulletAttack;
                        bool isParry = false;
                        //if (bulletAttack != null && bulletAttack == DeflectorBulletAttack)
                        //{
                        //    isParry = true;
                        //}
                        Collider[] colliders = Physics.OverlapBox(transform.position, new Vector3(3f, 1f, 3f), transform.rotation, isParry ? LayerIndex.entityPrecise.mask | LayerIndex.projectile.mask : LayerIndex.entityPrecise.mask, QueryTriggerInteraction.UseGlobal);
                        List<GameObject> characterBodies = new List<GameObject>();
                        foreach (var collider in colliders)
                        {
                            ProjectileController projectileController = collider.GetComponent<ProjectileController>();
                            CharacterBody colliderrBody = projectileController ? null : collider.GetComponent<HurtBox>().healthComponent.body;
                            if (colliderrBody || projectileController && (projectileController ? projectileController.teamFilter.teamIndex : colliderrBody.teamComponent.teamIndex) != characterBody.teamComponent.teamIndex)
                            {
                                if (!characterBodies.Contains(projectileController ? projectileController.gameObject : colliderrBody.gameObject))
                                    characterBodies.Add(projectileController ? projectileController.gameObject : colliderrBody.gameObject);
                            }
                        }
                        foreach (GameObject body in characterBodies)
                        {
                            BulletAttack bulletAttack2 = new BulletAttack()
                            {
                                radius = 1f,
                                aimVector = Vector3.up,
                                damage = base.damageStat * (bulletAttack != null ? bulletAttack.damage : 3f),
                                bulletCount = 1,
                                spreadPitchScale = 0f,
                                spreadYawScale = 0f,
                                maxSpread = 0f,
                                minSpread = 0f,
                                maxDistance = 1f,
                                allowTrajectoryAimAssist = false,
                                hitMask = LayerIndex.entityPrecise.mask,
                                procCoefficient = bulletAttack != null ? bulletAttack.procCoefficient : 1f,
                                stopperMask = LayerIndex.entityPrecise.mask,
                                damageType = new DamageTypeCombo(DamageType.Generic, DamageTypeExtended.Generic, DamageSource.Special),
                                force = bulletAttack != null ? bulletAttack.force : 1f,
                                falloffModel = BulletAttack.FalloffModel.None,
                                damageColorIndex = DamageColorIndex.Default,
                                isCrit = base.RollCrit(),
                                origin = body.transform.position,
                                owner = gameObject,
                                hitCallback = bulletAttack != null ? bulletAttack.hitCallback : default,

                            };
                            bulletAttack2.Fire();
                        }
                    }

                }
                if (timer < 0)
                {
                    PlayAnimation("FullBody, Override", "SpinEnd", "Slash.playbackRate", 1 / speed / 2);
                    outer.SetNextStateToMain();
                }

            }*/


        }
    }
    public class SpecialOneSticky : BaseState
    {
        private float stopwatch = 0f;
        private float timer = 0f;
        private int fireTimes = 0;
        private int fireCount = 0;
        private DemoComponent demoComponent;
        private Vector3 forward;
        private GameObject projectile;
        private float damage;
        public override void OnEnter()
        {
            base.OnEnter();
            demoComponent = GetComponent<DemoComponent>();
            
            timer = 0.1f / base.attackSpeedStat;
            forward = Vector3.up;
            if (inputBank) forward = inputBank.aimDirection;
            if (demoComponent)
            {
                demoComponent.noLimitStickies++;
                demoComponent.additionalArmTime -= 10f;
            }
            DemoStickyClass demoSticky = bombProjectiles.ContainsKey(skillLocator.primary.skillDef) ? bombProjectiles[skillLocator.primary.skillDef] : null;
            projectile = demoSticky != null ? demoSticky.stickyObject : LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/FireMeatBall");
            damage = demoSticky != null ? demoSticky.damage : 4;
            StickyComponent stickyComponent = demoSticky != null ? demoSticky.stickyObject.GetComponent<StickyComponent>() : null;
            fireTimes = stickyComponent != null ? stickyComponent.maxStickies : 8;
        }
        public override void OnExit()
        {
            base.OnExit();
            if (demoComponent)
            {
                //DetonateAllStickies(demoComponent);
                demoComponent.noLimitStickies--;
                demoComponent.additionalArmTime += 10f;
            }
        }
        public virtual void ModifyProjectileFireInfo(ref FireProjectileInfo fireProjectileInfo)
        {

        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            stopwatch += Time.fixedDeltaTime;
            if (stopwatch > timer && fireCount <= fireTimes)
            {
                if (isAuthority)
                {
                    Vector3 vector2 = characterBody.characterMotor ? (transform.position + Vector3.up * (characterBody.characterMotor.capsuleHeight * 0.5f + 2f)) : (transform.position + Vector3.up * 2f);
                    InputBankTest component6 = characterBody.inputBank;
                    Vector3 forward2 = component6 ? component6.aimDirection : transform.forward;
                    float num15 = 20f;
                    float minInclusive = 15f;
                    float maxInclusive = 30f;
                    float speedOverride2 = UnityEngine.Random.Range(minInclusive, maxInclusive);
                    float num17 = (float)(360 / fireTimes);
                    float num18 = num17 / 360f;
                    float num19 = 1f;
                    float num20 = num17;
                    float num21 = (float)fireCount * 3.1415927f * 2f / (float)fireTimes;
                    num20 += num17;

                    //forward2.x += UnityEngine.Random.Range(-num15, num15);
                    //forward2.z += UnityEngine.Random.Range(-num15, num15);
                    //forward2.y += UnityEngine.Random.Range(-num15, num15);

                    FireProjectileInfo fireProjectileInfo = new FireProjectileInfo
                    {
                        projectilePrefab = projectile,
                        position = component6 ? component6.aimOrigin : transform.position,
                        rotation = Util.QuaternionSafeLookRotation(forward2),
                        procChainMask = default,
                        owner = gameObject,
                        damage = base.damageStat * damage,
                        crit = RollCrit(),
                        force = 200f,
                        damageColorIndex = DamageColorIndex.Default,
                        damageTypeOverride = new DamageTypeCombo?(new DamageTypeCombo(DamageType.Generic, DamageTypeExtended.Generic, DamageSource.Special)),
                        speedOverride = speedOverride2,
                        useSpeedOverride = true
                    };
                    ModifyProjectileFireInfo(ref fireProjectileInfo);
                    ProjectileManager.instance.FireProjectile(fireProjectileInfo);
                }

                stopwatch = 0f;
                fireCount++;
            }


            if (fireCount > fireTimes)
            {

                outer.SetNextStateToMain();
            }


        }


    }

    public abstract class UltraInstinctState : BaseState, ISkillState
    {
        public List<CharacterBody> bodies = new List<CharacterBody>();
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
            if (isAuthority)
            {
                characterBody.AddBuff(RoR2Content.Buffs.ArmorBoost);
                FindTargets();

                if (bodies.Count <= 0)
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
                if (body && body.teamComponent.teamIndex != characterBody.teamComponent.teamIndex && !bodies.Contains(body))
                {
                    bodies.Add(body);
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
            currentTarget = bodies.Count > 0 ? bodies.FirstOrDefault() : null;
            targetDirection = currentTarget ? currentTarget.transform.position - transform.position : Vector3.zero;
            initialPosition = transform.position;
            if (characterMotor) characterMotor.useGravity = false;
            Calculate();
        }
        public void Calculate()
        {
            bodies.RemoveAll(s => s == null);
            if (bodies.Count <= 0)
            {
                returning = true;

            }
            previousPosition = transform.position;
            //if (colliders.Count <= 0) outer.SetNextStateToMain();
            if (!returning)
            {
                currentTarget = bodies.FirstOrDefault();
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
            Chat.AddMessage(stopwatch2.ToString());
            if (stopwatch2 < 0)
            {
                if (!returning)
                {
                    if (currentTarget && bodies.Contains(currentTarget))
                    {
                        if (bodies.Contains(currentTarget)) bodies.Remove(currentTarget);
                        OnContact();
                    }

                    Calculate();
                }
                else if (overcharge && activatorSkillSlot.stock > 0)
                {
                    FindTargets();
                    if (bodies.Count <= 0)
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
            if (isAuthority)
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
            characterBody.RemoveBuff(RoR2Content.Buffs.ArmorBoost);
        }
        public override void Update()
        {
            base.Update();
            if (isAuthority && characterDirection) characterDirection.forward = targetDirection.normalized;
        }
    }
    public class UltraInstinctRedirect : BaseState, ISkillState
    {
        public GenericSkill activatorSkillSlot { get; set; }
        public override void OnEnter()
        {
            base.OnEnter();
            DemoComponent demoComponent = GetComponent<DemoComponent>();
            if (demoComponent && demoComponent.isSwapped)
            {
                UltraInstinctSticky ultraInstinctSticky = new UltraInstinctSticky();
                ultraInstinctSticky.activatorSkillSlot = activatorSkillSlot;
                outer.SetNextState(ultraInstinctSticky);
            }
            else
            {
                UltraInstinctSword ultraInstinctSword = new UltraInstinctSword();
                ultraInstinctSword.activatorSkillSlot = activatorSkillSlot;
                outer.SetNextState(ultraInstinctSword);
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
            bulletAttack = swordDictionary.ContainsKey(skillLocator.primary.skillDef) ? swordDictionary[skillLocator.primary.skillDef].bulletAttack : DefaultSword.bulletAttack;
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
            DemoStickyClass demoSticky = bombProjectiles.ContainsKey(skillLocator.primary.skillDef) ? bombProjectiles[skillLocator.primary.skillDef] : null;
            projectile = demoSticky != null ? demoSticky.stickyObject : LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/FireMeatBall");
            damage = demoSticky != null ? demoSticky.damage : 4;
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
    }
    public class SpecialTwo : BaseState, ISkillState
    {
        private BulletAttack bulletAttack;
        private List<CharacterBody> bodies = new List<CharacterBody>();
        private CharacterBody currentTarget;
        private Vector3 targetDirection;
        private Vector3 initialPosition;
        private Vector3 previousPosition;
        private Vector3 targetPosition;
        private bool returning = false;
        private float stopwatch = 0f;
        private float stopwatch2 = 0f;
        private int phase = 0;
        private float speed = 96f;

        public GenericSkill activatorSkillSlot { get; set; }

        public override void OnEnter()
        {
            base.OnEnter();
            if (isAuthority)
            {
                bulletAttack = swordDictionary.ContainsKey(skillLocator.primary.skillDef) ? swordDictionary[skillLocator.primary.skillDef].bulletAttack : DefaultSword.bulletAttack;
                FindTargets();

                if (characterMotor)
                {
                    KinematicCharacterMotor kinematicCharacterMotor = GetComponent<KinematicCharacterMotor>();
                    if (kinematicCharacterMotor)
                        kinematicCharacterMotor.ForceUnground(0f);
                    characterMotor.velocity += new Vector3(0f, 20f, 0f);
                }

                if (bodies.Count <= 0)
                {
                    outer.SetNextStateToMain();
                    return;
                }
                stopwatch = 1f;
                speed *= base.attackSpeedStat;
            }

        }
        public void FindTargets()
        {
            Collider[] collidersArray = Physics.OverlapSphere(transform.position, 24f, LayerIndex.entityPrecise.mask, QueryTriggerInteraction.UseGlobal);
            foreach (Collider collider in collidersArray)
            {
                CharacterBody body = collider.GetComponent<HurtBox>() ? collider.GetComponent<HurtBox>().healthComponent.body : null;
                if (body && body.teamComponent.teamIndex != characterBody.teamComponent.teamIndex && !bodies.Contains(body))
                {
                    bodies.Add(body);
                }
            }
        }
        public void Update1()
        {

        }
        public void Swap()
        {
            phase++;
            stopwatch = -69f;


            currentTarget = bodies.Count > 0 ? bodies.FirstOrDefault() : null;
            targetDirection = currentTarget ? currentTarget.transform.position - transform.position : Vector3.zero;
            initialPosition = transform.position;
            if (characterMotor) characterMotor.useGravity = false;
            Calculate();

        }
        public void Calculate()
        {
            bodies.RemoveAll(s => s == null);
            if (bodies.Count <= 0)
            {
                returning = true;

            }
            previousPosition = transform.position;
            //if (colliders.Count <= 0) outer.SetNextStateToMain();
            if (!returning)
            {
                currentTarget = bodies.FirstOrDefault();
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
            stopwatch2 = targetDirection.magnitude / speed;
            PlayAnimation("FullBody, Override", "Ball", "Slash.playbackRate", stopwatch2);
        }
        public void Update2()
        {
            stopwatch2 -= Time.fixedDeltaTime;
            Chat.AddMessage(stopwatch2.ToString());
            if (stopwatch2 < 0)
            {
                if (!returning)
                {
                    if (currentTarget && bodies.Contains(currentTarget))
                    {
                        if (bodies.Contains(currentTarget)) bodies.Remove(currentTarget);
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

                    Calculate();
                }
                else if (activatorSkillSlot.stock > 0)
                {
                    FindTargets();
                    if (bodies.Count <= 0)
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
            if (isAuthority)
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
        }
        public override void Update()
        {
            base.Update();
            if (isAuthority && characterDirection) characterDirection.forward = targetDirection.normalized;
        }
        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Skill;
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
    public class Slam : BaseState
    {
        private float height;
        private float stopwatch = 0f;
        private bool slaming = false;
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
                    height = Util.CharacterRaycast(gameObject, ray, out raycastHit, 9999f, LayerIndex.world.mask, QueryTriggerInteraction.UseGlobal) ? (raycastHit.point - characterBody.footPosition).magnitude : 0;
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
            if (isAuthority)
            {
                if (!slaming && characterMotor)
                {
                    characterMotor.velocity += Physics.gravity * 3 * GetDeltaTime();
                    if (characterMotor.isGrounded)
                    {
                        SlamMethod();
                    }
                }
                if (stopwatch > 0f) stopwatch -= GetDeltaTime();

                if (slaming && stopwatch < 0f) outer.SetNextStateToMain();
            }

        }
        public override void Update()
        {
            base.Update();
            if (isAuthority && stopwatch > 0 && characterMotor && inputBank && inputBank.jump.justPressed)
            {
                if (characterMotor.Motor)
                    characterMotor.Motor.ForceUnground(0f);
                characterMotor.velocity.y = height;
                outer.SetNextStateToMain();
            }
        }
        public void SlamMethod()
        {
            slaming = true;
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
                    KinematicCharacterMotor kinematicCharacterMotor2 = body.GetComponent<KinematicCharacterMotor>();
                    if (kinematicCharacterMotor2)
                        kinematicCharacterMotor2.ForceUnground(0f);
                    body.characterMotor.velocity.y = height;
                }
            }
            stopwatch = 0.5f;
        }
    }
    public class LockIn : BaseState
    {
        public override void OnEnter()
        {
            base.OnEnter();
            if (isAuthority)
            {
                characterBody.SetBuffCount(LockedIn.buffIndex, 8);
                outer.SetNextStateToMain();
            }


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
                //Debug.Log(genericSkill1.skillDef);
                //Debug.Log(genericSkill2.skillDef);
                //Util.Swap<GenericSkill>(ref genericSkill1, ref genericSkill2);
                //Debug.Log(genericSkill1.skillDef);
                //Debug.Log(genericSkill2.skillDef);w
                GenericSkill genericSkill4 = skillLocator.utility;
                skillLocator.utility = demoComponent.utilityReplace;
                demoComponent.utilityReplace = genericSkill4;
                demoComponent.canUseUtilityTimer = 0.1f;
                demoComponent.canUseUtility = false;
                demoComponent.canUseSecondaryTimer = 0.1f;
                //demoComponent.canUseSecondary = false;
                //Debug.Log(genericSkill3.skillDef);
                //Debug.Log(genericSkill4.skillDef);
                //Util.Swap<GenericSkill>(ref genericSkill3, ref genericSkill4);
                //Debug.Log(genericSkill3.skillDef);
                //Debug.Log(genericSkill4.skillDef);
                //Util.Swap<GenericSkill>(ref genericSkill2, ref genericSkill2);
                //genericSkill1 = demoComponent.specialReplace;
                //genericSkill2 = skillLocator.special;
                //Util.Swap<GenericSkill>(ref genericSkill2, ref genericSkill2);
                demoComponent.UpdateHudObject();
            }
            /*
            //GenericSkill primarySkill = base.skillLocator.allSkills[0];
            //GenericSkill primarySkill2 = base.skillLocator.FindSkillByFamilyName("DemoStickyFamily");
            //Util.Swap<GenericSkill>(ref primarySkill, ref primarySkill2);
            //GenericSkill utilitySkill = base.skillLocator.allSkills[0];
            //GenericSkill utilitySkill2 = DetonateSkillDef;
            //DemoComponent demo = gameObject.GetComponent<DemoComponent>();
            //CachedSkillsLocator cachedSkillsLocator = GetComponent<CachedSkillsLocator>();
            if (true)
            {
                GenericSkill genericSkill = base.skillLocator.FindSkillByFamilyName("DemoStickies");
                bool switchOff = false;
                if (base.skillLocator.primary.skillOverrides.Length > 0)
                    foreach (var skillOverride in base.skillLocator.primary.skillOverrides)
                    {
                        if (skillOverride.skillDef == genericSkill.baseSkill && skillOverride.priority == GenericSkill.SkillOverridePriority.Loadout)
                        {
                            switchOff = true;
                            break;
                        }

                    }
                SkillDef detonateSkill = DetonateSkillDef;
                if (customDetonationSkills.ContainsKey(genericSkill.baseSkill)) detonateSkill = customDetonationSkills[genericSkill.baseSkill];
                if (switchOff)
                {
                    //float primaryStopwatch = skillLocator.primary.rechargeStopwatch;
                    //int primaryStocks = skillLocator.primary.stock;
                    //SkillDef primarySkillDef = skillLocator.primary.skillDef;
                    //base.skillLocator.primary.UnsetSkillOverride(base.characterBody, base.skillLocator.FindSkillByFamilyName("DemoStickies").baseSkill, GenericSkill.SkillOverridePriority.Loadout);
                    base.skillLocator.utility.UnsetSkillOverride(base.characterBody, detonateSkill, GenericSkill.SkillOverridePriority.Loadout);
                    if (altSpecialSkills.ContainsKey(skillLocator.special.baseSkill))
                    {
                        float specialRecharge = skillLocator.special.rechargeStopwatch;
                        int specialStocks = skillLocator.special.stock;
                        base.skillLocator.special.UnsetSkillOverride(base.characterBody, altSpecialSkills[skillLocator.special.baseSkill], GenericSkill.SkillOverridePriority.Loadout);
                        skillLocator.special.rechargeStopwatch = specialRecharge;
                        skillLocator.special.stock = specialStocks;
                    }
                    //skillLocator.primary.rechargeStopwatch = demo.primaryCooldown;
                    //skillLocator.primary.stock = demo.primaryStockCount;
                    //demo.primarySkillDef = primarySkillDef;
                    //demo.primaryCooldown = primaryStopwatch;
                    //demo.primaryStockCount = primaryStocks;
                }
                else
                {

                    //float primaryStopwatch = skillLocator.primary.rechargeStopwatch;
                    //int primaryStocks = skillLocator.primary.stock;
                    //SkillDef primarySkillDef = skillLocator.primary.skillDef;
                    //cachedSkillsLocator.GetOrCreateCachedGenericSkill(skillLocator.primary, "DemoSticky");
                    //cachedSkillsLocator.DeleteCachedSkill("DemoSword");
                    //base.skillLocator.primary.SetSkillOverride(base.characterBody, base.skillLocator.FindSkillByFamilyName("DemoStickies").baseSkill, GenericSkill.SkillOverridePriority.Loadout);
                    base.skillLocator.utility.SetSkillOverride(base.characterBody, detonateSkill, GenericSkill.SkillOverridePriority.Loadout);
                    if (altSpecialSkills.ContainsKey(skillLocator.special.baseSkill))
                    {
                        float specialRecharge = skillLocator.special.rechargeStopwatch;
                        int specialStocks = skillLocator.special.stock;
                        base.skillLocator.special.SetSkillOverride(base.characterBody, altSpecialSkills[skillLocator.special.baseSkill], GenericSkill.SkillOverridePriority.Loadout);
                        skillLocator.special.rechargeStopwatch = specialRecharge;
                        skillLocator.special.stock = specialStocks;
                    }
                    //skillLocator.primary.rechargeStopwatch = demo.primaryCooldown;
                    //skillLocator.primary.stock = demo.primaryStockCount;
                    //demo.primarySkillDef = primarySkillDef;
                    //demo.primaryCooldown = primaryStopwatch;
                    //demo.primaryStockCount = primaryStocks;

                }
            }*/



            outer.SetNextStateToMain();
        }
        public override void OnExit()
        {
            base.OnExit();
            characterBody.RecalculateStats();
        }
        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Skill;
        }
    }

}
