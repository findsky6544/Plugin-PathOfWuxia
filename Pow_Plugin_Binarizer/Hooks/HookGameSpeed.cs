using System;
using HarmonyLib;
using UnityEngine;
using BepInEx.Configuration;
using System.ComponentModel;

namespace PathOfWuxia
{
    [System.ComponentModel.DisplayName("游戏速度设置")]
    [Description("游戏速度设置")]
    // 一般游戏设定功能
    public class HookGameSpeed : IHook
    {
        static bool speedOn = false;
        static ConfigEntry<float> speedValue;
        static ConfigEntry<KeyCode> speedKey;

        public void OnRegister(PluginBinarizer plugin)
        {
            speedValue = plugin.Config.Bind("游戏设定", "速度值", 1.5f, "调整速度值");
            speedKey = plugin.Config.Bind("游戏设定", "速度热键", KeyCode.F2, "开关速度调节");

            plugin.onUpdate += OnUpdate;
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
                Time.timeScale = Math.Max(Time.timeScale, speedValue.Value);
            }
        }




        // 1 Sync await by timeScale for speed correct
		//此处会导致召唤乖乖没有模型、无法移动，先注释掉
        //不知道为啥又好了= =
        [HarmonyPrefix, HarmonyPatch(typeof(AsyncTools), "GetAwaiter", new Type[] { typeof(float) })]
        public static bool SpeedPatch(ref float seconds)
        {
            seconds = seconds / Time.timeScale;
            return true;
        }



    }
}
