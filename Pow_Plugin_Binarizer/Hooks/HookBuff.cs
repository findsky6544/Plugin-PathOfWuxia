using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Heluo.Battle;
using Heluo;
using Heluo.Flow.Battle;
using Heluo.Data;

namespace PathOfWuxia
{
    class HookBuff : IHook
    {
        private static ConfigEntry<bool> showHideBuff;

        public IEnumerable<Type> GetRegisterTypes()
        {
            return new Type[] { GetType() };
        }

        public void OnRegister(BaseUnityPlugin plugin)
        {
            showHideBuff = plugin.Config.Bind("游戏设定", "显示隐藏buff", false, "显示隐藏buff（图标暂时显示为特质buff图标） 最好配合mod：fixedBuff使用");
        }

        public void OnUpdate()
        {

        }

        //给不显示的buff加上图标（暂时统一用特质buff图标）
        [HarmonyPrefix, HarmonyPatch(typeof(WuxiaBattleBuffer), "AddBuffer", new Type[] { typeof(WuxiaUnit), typeof(Heluo.Data.Buffer), typeof(bool), typeof(bool) })]
        public static bool AddBufferPatch_showHideBuff(ref WuxiaBattleBuffer __instance, ref Heluo.Data.Buffer buffer)
        {
            if (showHideBuff.Value)
            {
                if (buffer == null)
                {
                    Logger.LogError("要附加的Buffer是空的", "AddBuffer", "D:\\Work\\PathOfWuxia2018_Update\\Assets\\Scripts\\Battle\\WuxiaBattleBuffer.cs", 154);
                    return true;
                }
                if (buffer.IconName == null || buffer.IconName.Equals(string.Empty))
                {
                    buffer.IconName = "buff_trait";
                }
            }
            return true;
        }

    }
}