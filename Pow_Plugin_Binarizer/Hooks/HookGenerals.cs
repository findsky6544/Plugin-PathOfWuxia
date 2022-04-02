using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using BepInEx;
using BepInEx.Configuration;
using Heluo;
using Heluo.Data;
using Heluo.Flow;
using Heluo.Battle;
using Heluo.Components;
using Heluo.Controller;
using Heluo.Utility;
using Heluo.FSM.Main;
using Heluo.UI;

namespace PathOfWuxia
{
    // 一般游戏设定功能
    public class HookGenerals : IHook
    {
        enum ProbablyMode
        {
            None,
            SmallChance,
            FixedRandomValue
        }
        static bool speedOn = false;
        static ConfigEntry<float> speedValue;
        static ConfigEntry<KeyCode> speedKey;
        static ConfigEntry<KeyCode> changeAnim;
        static ConfigEntry<KeyCode> changeAnimBack;
        static ConfigEntry<GameLevel> difficulty;
        static ConfigEntry<ProbablyMode> probablyMode;
        static ConfigEntry<int> probablyValue;
        static ConfigEntry<bool> lockTime;
        static ConfigEntry<bool> onePunch;
        static ConfigEntry<bool> noLockHp;
        enum CameraFocusMode
        {
            Attacker,
            Defender,
            Defender_OnHit
        }
        static ConfigEntry<CameraFocusMode> cameraFocusMode;
        static ConfigEntry<bool> cameraFree;
        static ConfigEntry<bool> cameraFree_Battle;
        static ConfigEntry<float> zoomSpeed;

        public static float customTimeScale = 1f;

        public IEnumerable<Type> GetRegisterTypes()
        {
            return new Type[] { GetType() };
        }
        public void OnRegister(BaseUnityPlugin plugin)
        {
            speedValue = plugin.Config.Bind("游戏设定", "速度值", 1.5f, "调整速度值");
            speedKey = plugin.Config.Bind("游戏设定", "速度热键", KeyCode.F2, "开关速度调节");
            difficulty = plugin.Config.Bind("游戏设定", "难度值", GameLevel.Normal, "调节游戏难度");
            difficulty.SettingChanged += OnGameLevelChange;
            probablyMode = plugin.Config.Bind("游戏设定", "随机事件方式", ProbablyMode.None, "None-原版 SmallChance-小概率事件必发生 FixedRandomValue-设定产生的随机数");
            probablyValue = plugin.Config.Bind("游戏设定", "随机事件值", 50, "SmallChance：多少被界定为小概率 FixedRandomValue：1~100对应必发生/必不发生");
            changeAnim = plugin.Config.Bind("游戏设定", "切换姿势(特殊)", KeyCode.F7, "切换特化战斗姿势(随机选择)");
            changeAnimBack = plugin.Config.Bind("游戏设定", "切换姿势(还原)", KeyCode.F8, "切换回默认战斗姿势");
            lockTime = plugin.Config.Bind("游戏设定", "锁定昼夜时间", false, "目前仅锁定锻炼、游艺等，未锁定传书和主线剧情");
            onePunch = plugin.Config.Bind("游戏设定", "伤害99999……", false, "含攻击、反击，不会击破锁血");
            noLockHp = plugin.Config.Bind("游戏设定", "无视锁血", false, "含攻击、反击。谨慎启用，一些战斗可能会产生错误");

            cameraFocusMode = plugin.Config.Bind("相机设置", "战斗相机跟随方式", CameraFocusMode.Attacker, "战斗时相机如何跟随，游戏默认跟随攻击者");
            cameraFree = plugin.Config.Bind("相机设置", "场景自由视角", false, "是否开启自由视角");
            cameraFree_Battle = plugin.Config.Bind("相机设置", "战斗自由视角", false, "是否开启战斗自由视角，重启战斗生效");
            zoomSpeed = plugin.Config.Bind("相机设置", "缩放速度", 20f, "相机缩放速度");
        }
        public void OnUpdate()
        {
            if (Input.GetKeyDown(speedKey.Value))
            {
                speedOn = !speedOn;
                if (!speedOn)
                {
                    Time.timeScale = 1.0f;
                }
            }
            if (speedOn)
            {
                Time.timeScale = Math.Max(0.1f, speedValue.Value);
            }

            if (Input.GetKeyDown(changeAnim.Value) && Game.BattleStateMachine != null)
            {
                if (IdleAnimOverrides == null)
                {
                    BuildIdleAnimOverrides();
                }
                WuxiaUnit unit = Traverse.Create(Game.BattleStateMachine).Field("_currentUnit").GetValue<WuxiaUnit>();
                if (unit != null && IdleAnimOverrides != null && IdleAnimOverrides.Count > 0)
                {
                    string randomIdleAnim = IdleAnimOverrides.Random();
                    AnimationClip animationClip = Game.Resource.Load<AnimationClip>(GameConfig.AnimationPath + randomIdleAnim + ".anim");
                    if (animationClip != null)
                    {
                        var list = new[] { ("idle", animationClip) };
                        unit.Actor.Override(list);
                    }
                }
            }
            if (Input.GetKeyDown(changeAnimBack.Value) && Game.BattleStateMachine != null)
            {
                WuxiaUnit unit = Traverse.Create(Game.BattleStateMachine).Field("_currentUnit").GetValue<WuxiaUnit>();
                if (unit != null)
                {
                    var weapon = unit.info.Equip.GetEquip(EquipType.Weapon);
                    var weaponType = weapon?.PropsCategory.ToString();
                    unit.Actor.OverrideDefault(Traverse.Create(unit).Field("exterior").GetValue<CharacterExteriorData>(), weaponType);
                }
            }

            // sync difficulty
            if (Game.GameData.GameLevel != difficulty.Value)
                difficulty.Value = Game.GameData.GameLevel;
        }

        /*[HarmonyPostfix, HarmonyPatch(typeof(InCinematic), "OnDisable")]
        public static void InCinematic_OnDisablePatch_changeTimeScale(ref InCinematic __instance)
        {
                Time.timeScale *= customTimeScale;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIElective), "SetIsViewMode")]
        public static void UIElective_SetIsViewModePatch_changeTimeScale(ref UIElective __instance)
        {
                Time.timeScale *= customTimeScale;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIFastMovie), "OnClick")]
        public static void UIFastMovie_OnClickPatch_changeTimeScale(ref UIFastMovie __instance)
        {
                Time.timeScale *= customTimeScale;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(UIFastMovie), "ResetAll")]
        public static void UIFastMovie_ResetAllPatch_changeTimeScale(ref UIFastMovie __instance)
        {
                Time.timeScale *= customTimeScale;
        }*/



        static void OnGameLevelChange(object o, EventArgs e)
        {
            if (Game.GameData != null)
            {
                Game.GameData.GameLevel = difficulty.Value;
            }
        }

        private static List<string> IdleAnimOverrides;
        static void BuildIdleAnimOverrides()
        {
            var idles = from animMap in Game.Data.Get<AnimationMapping>(am => !am.Idle.IsNullOrEmpty()) select animMap.Idle;
            IdleAnimOverrides = idles.Distinct().ToList();
            var stands = from animMap in Game.Data.Get<AnimationMapping>(am => !am.Stand.IsNullOrEmpty()) select animMap.Stand;
            IdleAnimOverrides.AddRange(stands.Distinct());

            Console.WriteLine("特殊动作表：" + string.Join(",", idles));
        }

        // 1 Sync await by timeScale for speed correct
        [HarmonyPrefix, HarmonyPatch(typeof(AsyncTools), "GetAwaiter", new Type[] { typeof(float) })]
        public static bool SpeedPatch(ref float seconds)
        {
            seconds = seconds / Time.timeScale;
            return true;
        }

        // 2 事件随机数调节
        [HarmonyPostfix, HarmonyPatch(typeof(Probability), "GetValue")]
        public static void ProbabilityPatch(Probability __instance, ref bool __result)
        {
            if (probablyMode.Value == ProbablyMode.FixedRandomValue)
            {
                __result = (probablyValue.Value - 1 < __instance.value);
            }
            else if (probablyMode.Value == ProbablyMode.SmallChance)
            {
                __result = (__instance.value < probablyValue.Value || __instance.value == 100f);
            }
        }

        // 3 战斗相机跟随模式
        [HarmonyPostfix, HarmonyPatch(typeof(BattleProcessStrategy), "ProcessAnimation", new Type[] { typeof(DamageInfo), typeof(float) })]
        public static void CameraPatch_FocusMode(BattleProcessStrategy __instance, DamageInfo damageInfo)
        {
            if (cameraFocusMode.Value == CameraFocusMode.Defender)
            {
                if (damageInfo != null)
                {
                    for (int i = 0; i < damageInfo.damages.Count; i++)
                    {
                        Damage damage = damageInfo.damages[i];
                        if (i == 0)
                        {
                            var manager = Traverse.Create(__instance).Field("manager").GetValue<WuxiaBattleManager>();
                            manager.CameraLookAt = damage.Defender.Cell.transform.position;
                        }
                    }
                }
            }
        }
        [HarmonyPostfix, HarmonyPatch(typeof(BattleProcessStrategy), "HitTarget", new Type[] { typeof(bool) })]
        public static void CameraPatch_FocusMode2(BattleProcessStrategy __instance)
        {
            if (cameraFocusMode.Value == CameraFocusMode.Defender_OnHit)
            {
                var damageInfo = Traverse.Create(__instance).Field("damageInfo").GetValue<DamageInfo>();
                if (damageInfo != null)
                {
                    for (int i = 0; i < damageInfo.damages.Count; i++)
                    {
                        Damage damage = damageInfo.damages[i];
                        if (i == 0)
                        {
                            var manager = Traverse.Create(__instance).Field("manager").GetValue<WuxiaBattleManager>();
                            manager.CameraLookAt = damage.Defender.Cell.transform.position;
                        }
                    }
                }
            }
        }

        // 4 战斗自由视角
        [HarmonyPostfix, HarmonyPatch(typeof(GameCamera), "SetBattleCamera")]
        public static void CameraPatch_FreeBattle(GameCamera __instance)
        {
            if (cameraFree_Battle.Value)
            {
                __instance.ylocked = false;
                __instance.minDistance = 0;
                __instance.maxDistance = 10000;
                __instance.ZoomSpeed = zoomSpeed.Value;
            }
        }

        // 5 平时自由视角
        [HarmonyPrefix, HarmonyPatch(typeof(CameraController), "UpdateLimitFollow", new Type[] { typeof(float) })]
        public static bool CameraPatch_Free1(CameraController __instance, float deltaTime)
        {
            if (cameraFree.Value)
            {
                __instance.UpdateTransform(deltaTime);
                return false;
            }
            return true;
        }
        [HarmonyPostfix, HarmonyPatch(typeof(CameraController), "OnCameraDrag", new Type[] { typeof(float), typeof(float) })]
        public static void CameraPatch_Free2(CameraController __instance, float dx, float dy)
        {
            if (cameraFree.Value)
            {
                var cameraMode = Traverse.Create(__instance).Field("mode").GetValue<GameCamera.CameraMode>();
                if (cameraMode == GameCamera.CameraMode.LimitFollow)
                {
                    var param = Traverse.Create(__instance).Field("param").GetValue<GameCamera>();
                    param.x += dx * param.HorizontalSpeed;
                    param.y -= dy * param.VerticalSpeed;
                    param.yMinLimit = -90;
                    param.yMaxLimit = 90;
                    param.y = Traverse.Create(__instance).Method("ClampAngle", new object[] { param.y, param.yMinLimit, param.yMaxLimit }).GetValue<float>();
                }
            }
        }
        [HarmonyPostfix, HarmonyPatch(typeof(CameraController), "Zoom", new Type[] { typeof(float) })]
        public static void CameraPatch_Free3(CameraController __instance, float s)
        {
            if (cameraFree.Value)
            {
                var cameraMode = Traverse.Create(__instance).Field("mode").GetValue<GameCamera.CameraMode>();
                if (cameraMode == GameCamera.CameraMode.LimitFollow)
                {
                    var param = Traverse.Create(__instance).Field("param").GetValue<GameCamera>();
                    param.minDistance = 0;
                    param.maxDistance = 10000;
                    param.distance -= s * zoomSpeed.Value * Time.deltaTime;
                    param.distance = Traverse.Create(__instance).Method("ClampAngle", new object[] { param.distance, param.minDistance, param.maxDistance }).GetValue<float>();
                }
            }
        }

        [HarmonyPrefix, HarmonyPatch(typeof(NurturanceLoadScenesAction), "GetValue")]
        public static bool NurturanceLoadScenesActionPatch_GetValue(NurturanceLoadScenesAction __instance)
        {
            if (lockTime.Value)
            {
                __instance.isNextTime = false;
                __instance.timeStage = 0;
            }
            return true;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(BattleComputer), "Calculate_Final_Damage")]
        public static void BattleComputerPatch_Calculate_Final_Damage(BattleComputer __instance, ref Damage damage,ref SkillData skill)
        {
            Console.WriteLine("BattleComputerPatch_Calculate_Final_Damage");
            if (onePunch.Value)
            {
                    if(damage.Defender.faction == Faction.Enemy || damage.Defender.faction == Faction.Single || damage.Defender.faction == Faction.AbsolutelyNeutral || damage.Defender.faction == Faction.AbsoluteChaos)
                    {
                        damage.final_damage = 999999999;
                        damage.IsDodge = false;//无法闪避
                        damage.IsLethal = true;//击杀
                        damage.IsInvincibility = false;//非霸体
                        damage.DamageToAttacker = 0;//反伤为0
                    }
            }
            //消除锁血
            if (noLockHp.Value)
            {
                    if (damage.Defender.faction == Faction.Enemy || damage.Defender.faction == Faction.Single || damage.Defender.faction == Faction.AbsolutelyNeutral || damage.Defender.faction == Faction.AbsoluteChaos)
                    {
                        damage.Defender[BattleLiberatedState.Lock_HP_Percent] = 0;
                        damage.Defender[BattleLiberatedState.Lock_HP_Value] = 0;
                        List<BufferInfo> BufferList = Traverse.Create(damage.Defender.BattleBuffer).Field("BufferList").GetValue<List<BufferInfo>>();
                        for (int j = 0; j < BufferList.Count; j++)
                        {
                            BufferList[j].BufferAttributes[BattleLiberatedState.Lock_HP_Percent] = 0;
                            BufferList[j].BufferAttributes[BattleLiberatedState.Lock_HP_Value] = 0;
                        }
                        BattleAttributes Mantra_Attributes = Traverse.Create(damage.Defender).Field("Mantra_Attributes").GetValue<BattleAttributes>();
                        Mantra_Attributes[BattleLiberatedState.Lock_HP_Percent] = 0;
                        Mantra_Attributes[BattleLiberatedState.Lock_HP_Value] = 0;
                    }
            }
        }
    }
}
