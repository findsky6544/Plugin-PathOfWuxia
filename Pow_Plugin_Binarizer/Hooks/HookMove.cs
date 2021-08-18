using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Heluo.FSM.Player;
using UnityEngine;
using Heluo.Actor;
using Heluo;

namespace PathOfWuxia
{
    class HookMove : IHook
    {
        private static ConfigEntry<float> moveSpeed;

        public IEnumerable<Type> GetRegisterTypes()
        {
            return new Type[] { GetType() };
        }

        public void OnRegister(BaseUnityPlugin plugin)
        {
            moveSpeed = plugin.Config.Bind("游戏设定", "移动速度", 1f, "修改玩家在大地图的移动速度 如果太快可能会穿模"); 
		}


        //修改移动速度
        [HarmonyPrefix, HarmonyPatch(typeof(Move), "FixedUpdate")]
        public static bool FixedUpdatePatch_changeMoveSpeed(ref Move __instance)
        {

            Traverse.Create(__instance).Field("forwardRate").SetValue(2.6f*moveSpeed.Value);
            return true;
        }

            public void OnUpdate()
        {

        }

    }
}
