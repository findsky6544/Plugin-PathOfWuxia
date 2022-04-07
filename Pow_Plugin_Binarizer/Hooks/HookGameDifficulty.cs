using BepInEx.Configuration;
using Heluo;
using Heluo.Data;
using System;
using System.ComponentModel;

namespace PathOfWuxia
{
    [System.ComponentModel.DisplayName("修改难度")]
    [Description("修改难度")]
    class HookGameDifficulty : IHook
    {
        static ConfigEntry<GameLevel> difficulty;
        public void OnRegister(PluginBinarizer plugin)
        {
            difficulty = plugin.Config.Bind("游戏设定", "难度值", GameLevel.Normal, "调节游戏难度");
            difficulty.SettingChanged += OnGameLevelChange;

            plugin.onUpdate += OnUpdate;
        }
        public void OnUpdate()
        {
            difficulty.Value = Game.GameData.GameLevel;
        }

        static void OnGameLevelChange(object o, EventArgs e)
        {
            if (Game.GameData != null)
            {
                Game.GameData.GameLevel = difficulty.Value;
            }
        }
    }
}