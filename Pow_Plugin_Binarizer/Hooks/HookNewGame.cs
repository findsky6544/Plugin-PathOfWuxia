using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using BepInEx;
using BepInEx.Configuration;
using Heluo;
using Heluo.UI;
using Heluo.Data;
using Heluo.Flow;
using Heluo.Utility;
using System.Reflection.Emit;
using Heluo.Features;

namespace PathOfWuxia
{
    // 开局设定
    public class HookNewGame : IHook
    {
        public IEnumerable<Type> GetRegisterTypes()
        {
            return new Type[] { GetType() };
        }
        enum genderMode
        {
            男,
            女,
            随建模
        }
        public void OnRegister(BaseUnityPlugin plugin)
        {
            newGameAttributePoint = plugin.Config.Bind("开局设定", "增加属性点", 0, "设置开局增加多少属性点");
            newGameTraitPoint = plugin.Config.Bind("开局设定", "增加特性点", 0, "设置开局增加多少特性点");
            newGameExteriorId = plugin.Config.Bind("角色设定", "主角建模", string.Empty, "设定主角建模数据源，请通过CharacterExterior表格查找，使用第一列ID");
            newGamePortraitOverride = plugin.Config.Bind("角色设定", "主角头像", string.Empty, "若已设置建模，则可为空，使用建模的头像，否则用此头像代替");
            newGameGenderOverride = plugin.Config.Bind("角色设定", "主角性别", genderMode.随建模, "可修改主角的性别（如果和建模、头像不符时可能卡死，慎用）");
            newGameSurNameOverride = plugin.Config.Bind("角色设定", "主角姓", string.Empty, "可修改主角的姓");
            newGameNameOverride = plugin.Config.Bind("角色设定", "主角名", string.Empty, "可修改主角的名");



            newGameExteriorId.SettingChanged += (o, e) =>
            {
                ReplacePlayerExteriorData();
            };
            newGamePortraitOverride.SettingChanged += (o, e) =>
            {
                ReplacePlayerExteriorData();
            };
            newGameGenderOverride.SettingChanged += (o, e) =>
            {
                ReplacePlayerExteriorData();
            };
            newGameSurNameOverride.SettingChanged += (o, e) =>
            {
                ReplacePlayerExteriorData();
            };
            newGameNameOverride.SettingChanged += (o, e) =>
            {
                ReplacePlayerExteriorData();
            };
        }

        public void OnUpdate()
        {
        }

        static ConfigEntry<int> newGameAttributePoint;
        static ConfigEntry<int> newGameTraitPoint;
        static ConfigEntry<string> newGameExteriorId;
        static ConfigEntry<string> newGamePortraitOverride;
        static ConfigEntry<string> newGameSurNameOverride;
        static ConfigEntry<string> newGameNameOverride;
        static ConfigEntry<genderMode> newGameGenderOverride;

        // 1 可选多个特性
        //[HarmonyTranspiler]
        //[HarmonyPatch(typeof(CtrlRegistration), "OnTraitClick")]
        //public static IEnumerable<CodeInstruction> StartPatch_UnlockTraitCount(IEnumerable<CodeInstruction> instructions)
        //{
        //    var codes = instructions.ToList();
        //    codes[47].opcode = OpCodes.Ldc_I4_M1;   // 原本为Ldc_I4_2
        //    return codes.AsEnumerable();
        //}

        // 2 纪录开局数据
        private static Dictionary<CharacterUpgradableProperty, int> newAttributeValues;  // +
        private static int dicePoint;	// +
        [HarmonyPostfix, HarmonyPatch(typeof(CtrlRegistration), "CreateNewPlayer")]
        public static void StartPatch_CreateNewPlayer(CtrlRegistration __instance)
        {
            var data = Traverse.Create(__instance).Field("player_info_data").GetValue<CharacterInfoData>();
            newAttributeValues = new Dictionary<CharacterUpgradableProperty, int>
            {
                {
                    CharacterUpgradableProperty.Str,
                    data.UpgradeableProperty[CharacterUpgradableProperty.Str].Level
                },
                {
                    CharacterUpgradableProperty.Vit,
                    data.UpgradeableProperty[CharacterUpgradableProperty.Vit].Level
                },
                {
                    CharacterUpgradableProperty.Dex,
                    data.UpgradeableProperty[CharacterUpgradableProperty.Dex].Level
                },
                {
                    CharacterUpgradableProperty.Spi,
                    data.UpgradeableProperty[CharacterUpgradableProperty.Spi].Level
                }
            };

            var Tpoint = Traverse.Create(__instance).Field("point");
            int attrPoint = Tpoint.GetValue<int>();
            dicePoint = newGameAttributePoint.Value + attrPoint;
            Tpoint.SetValue(dicePoint);

            var Tpoint2 = Traverse.Create(__instance).Field("traitPoint");
            int traitPoint = Tpoint2.GetValue<int>();
            Tpoint2.SetValue(newGameTraitPoint.Value + traitPoint);
        }

        // 3 新随机方法
        static void UpdateAttributes(CtrlRegistration instance)
        {
            var data = Traverse.Create(instance).Field("player_info_data").GetValue<CharacterInfoData>();
            var Tpoint = Traverse.Create(instance).Field("point");
            FourAttributesInfo fourAttributesInfo = new FourAttributesInfo
            {
                Str = data.GetUpgradeableProperty(CharacterUpgradableProperty.Str).ToString(),
                Vit = data.GetUpgradeableProperty(CharacterUpgradableProperty.Vit).ToString(),
                Dex = data.GetUpgradeableProperty(CharacterUpgradableProperty.Dex).ToString(),
                Spr = data.GetUpgradeableProperty(CharacterUpgradableProperty.Spi).ToString(),
                Point = Tpoint.GetValue<int>().ToString()
            };
            data.UpgradeProperty(true);
            Traverse.Create(instance).Field("view").GetValue<UIRegistration>().UpdateFourAttributes(fourAttributesInfo);
        }
        [HarmonyPrefix, HarmonyPatch(typeof(CtrlRegistration), "DiceValue")]
        public static bool StartPatch_DiceValue(CtrlRegistration __instance)
        {
            var data = Traverse.Create(__instance).Field("player_info_data").GetValue<CharacterInfoData>();
            List<UpgradeableProperty> list = new List<UpgradeableProperty>();
            foreach (var key in newAttributeValues.Keys)
            {
                data.SetUpgradeablePropertyLevel(key, newAttributeValues[key]);
                list.Add(data.UpgradeableProperty[key]);
            }
            List<int> diceVal = new List<int>
            {
                0,
                dicePoint
            };
            for (int i = 0; i < list.Count - 1; i++)
            {
                diceVal.Add(UnityEngine.Random.Range(0, dicePoint));
            }
            diceVal.Sort();
            for (int j = 0; j < list.Count; j++)
            {
                list[j].Level += diceVal[j + 1] - diceVal[j];
            }

            Traverse.Create(__instance).Field("point").SetValue(0);
            UpdateAttributes(__instance);
            return false;
        }
        [HarmonyPrefix, HarmonyPatch(typeof(CtrlRegistration), "set_character_upgradable_property", new Type[] { typeof(CharacterUpgradableProperty), typeof(int) })]
        public static bool StartPatch_SetProperty(CtrlRegistration __instance, CharacterUpgradableProperty property, int value)
        {
            var Tpoint = Traverse.Create(__instance).Field("point");
            int num = Tpoint.GetValue<int>() - value;
            if (num < 0)
            {
                return false;
            }
            var data = Traverse.Create(__instance).Field("player_info_data").GetValue<CharacterInfoData>();
            int num2 = data.GetUpgradeablePropertyLevel(property);
            num2 = (int)Mathf.Lerp((float)num2, (float)(num2 + value), 1f);
            if (num2 < newAttributeValues[property])
            {
                return false;
            }
            data.SetUpgradeablePropertyLevel(property, num2);
            Tpoint.SetValue(num);
            UpdateAttributes(__instance);
            return false;
        }

        // 4 头像模型名称性别替换
        public static void ReplacePlayerExteriorData()
        {
            string[] characters = new string[] { GameConfig.Player , "in0196", "in0197", "in0101", "in0115" };
            for(int i = 0;i < characters.Length; i++)
            {
                CharacterExteriorData playerExteriorData = Game.GameData.Exterior[characters[i]];
                if (playerExteriorData != null && newGameExteriorId.Value.Trim() != string.Empty)
                {
                    CharacterExterior characterExterior = Game.Data.Get<CharacterExterior>(newGameExteriorId.Value.Trim());
                    if (characterExterior != null)
                    {
                        //CharacterExterior exterior = Game.Data.Get<CharacterExterior>(playerExteriorData.Id);
                        playerExteriorData.Id = characterExterior.Id;
                        playerExteriorData.Model = characterExterior.Model;
                        playerExteriorData.Gender = characterExterior.Gender;
                        playerExteriorData.Size = characterExterior.Size;
                        playerExteriorData.Protrait = characterExterior.Protrait;
                    }
                }
                if (!newGamePortraitOverride.Value.Trim().IsNullOrEmpty())
                {
                    CharacterExterior characterExterior = Game.Data.Get<CharacterExterior>(newGamePortraitOverride.Value.Trim());
                    playerExteriorData.Protrait = characterExterior.Protrait;
                }

                if(newGameGenderOverride.Value != genderMode.随建模)
                {
                    playerExteriorData.Gender = (Gender)newGameGenderOverride.Value;
                }

                if (!newGameSurNameOverride.Value.Trim().IsNullOrEmpty())
                {
                    playerExteriorData.SurName = newGameSurNameOverride.Value.Trim();
                }
                if (!newGameNameOverride.Value.Trim().IsNullOrEmpty())
                {
                    playerExteriorData.Name = newGameNameOverride.Value.Trim();
                }
            }
        }
        //EnterGame之后会直接开始游戏，创建playerEntity，之后再执行InitialRewards。在InitialRewards后再替换模型就晚了一些
        [HarmonyPrefix, HarmonyPatch(typeof(UIRegistration), "EnterGame")]
        public static bool StartPatch_SetPlayerModel()
        {
            ReplacePlayerExteriorData();
            return true;
        }
        [HarmonyPrefix, HarmonyPatch(typeof(PlayerTemplate), "BuildEntity")]
        public static bool PlayerTemplatePatch_BuildEntity()
        {
            Heluo.Logger.LogError("是这里的问题吗？");
            ReplacePlayerExteriorData();
            return true;
        }
        [HarmonyPostfix, HarmonyPatch(typeof(ChangeCharacterProtraitAndModel), "GetValue")]
        public static void StartPatch_SetPlayerModel2(ChangeCharacterProtraitAndModel __instance, bool __result)
        {
            if (__instance.id == GameConfig.Player)
            {
                ReplacePlayerExteriorData();
            }
        }
        [HarmonyPostfix, HarmonyPatch(typeof(ChangeCharacterIdentity), "GetValue")]
        public static void StartPatch_SetPlayerModel3(ChangeCharacterIdentity __instance, bool __result)
        {
            if (__instance.id == GameConfig.Player && __result)
            {
                ReplacePlayerExteriorData();
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(UIRegistration), "UpdateView")]
        public static bool UIRegistrationPatch_UpdateView(UIRegistration __instance, ref RegistrationInfo _info)
        {
            _info.SurName = newGameSurNameOverride.Value;
            _info.Name = newGameNameOverride.Value;
            return true;
        }

        [HarmonyPrefix, HarmonyPatch(typeof(UIRegistration), "UpdateName")]
        public static bool UIRegistrationPatch_UpdateName(UIRegistration __instance, ref string surName, ref string Name)
        {
            surName = newGameSurNameOverride.Value;
            Name = newGameNameOverride.Value;
            return true;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(CtrlRegistration), "SetLastName")]
        public static void CtrlRegistrationPatch_SetLastName(CtrlRegistration __instance,ref string value)
        {
            newGameSurNameOverride.Value = value;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(CtrlRegistration), "SetFitstName")]
        public static void CtrlRegistrationPatch_SetFitstName(CtrlRegistration __instance, ref string value)
        {
            newGameNameOverride.Value = value;
        }

        // 5 防止人物模型动作出现问题
        [HarmonyPrefix, HarmonyPatch(typeof(Heluo.Actor.ActorController), "OverrideStand", new Type[] { typeof(string) })]
        public static bool StartPatch_SetPlayerModel4(Heluo.Actor.ActorController __instance, ref string clipName)
        {
            AnimationClip animationClip = __instance.GetAnimationClip(clipName);
            if (animationClip == null)
            {
                int index = clipName.IndexOf("_special_await");
                if (index >= 0)
                {
                    Console.WriteLine("OverrideStand = " + clipName);
                    string ModelName = clipName.Substring(0, index);
                    var collection = from ce in Game.Data.Get<CharacterExterior>().Values where ce.Model == ModelName select ce;
                    if (collection.Count() > 0)
                    {
                        var characterExterior = collection.First();
                        if (!characterExterior.AnimMapId.IsNullOrEmpty())
                        {
                            clipName = string.Format("{0}_special_await{1:00}", characterExterior.AnimMapId, 0);
                            animationClip = __instance.GetAnimationClip(clipName);
                            if (animationClip == null)
                            {
                                var animMap = Game.Data.Get<AnimationMapping>(characterExterior.AnimMapId);
                                foreach (var (state, clip) in animMap)
                                {
                                    if (!clip.IsNullOrEmpty() && __instance.GetAnimationClip(clip) != null)
                                    {
                                        clipName = clip;
                                        break;
                                    }
                                }
                            }
                        }
                        animationClip = __instance.GetAnimationClip(clipName);
                        if (animationClip == null)
                            clipName = characterExterior.Gender == Gender.Male ? "in0101_special_await00" : "in0115_special_await00";
                    }
                }
            }
            return true;
        }
    }
}
