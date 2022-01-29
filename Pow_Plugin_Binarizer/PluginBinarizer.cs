using System;
using System.Collections.Generic;
using HarmonyLib;
using BepInEx;
using System.Linq;

namespace PathOfWuxia
{
    [BepInPlugin("binarizer.plugin.pow.function_sets", "功能合集 by Binarizer，修改 by 寻宇", "1.7.0")]
    public class PluginBinarizer : BaseUnityPlugin
    {
        void RegisterHook(IHook hook)
        {
            hook.OnRegister(this);
            hook.GetRegisterTypes().Do(t => { Harmony.CreateAndPatchAll(t); Console.WriteLine("Patch " + t.Name); });
            hooks.Add(hook);
        }

        private List<IHook> hooks = new List<IHook>();

        void Awake()
        {
            Console.WriteLine("美好的初始化开始");

            RegisterHook(new HookModSupport());
            RegisterHook(new HookGenerals());
            RegisterHook(new HookEnglishTranslate());
            RegisterHook(new HookNewGame());
            RegisterHook(new HookFeaturesAndFixes());
            RegisterHook(new HookMoreAccessories());
            RegisterHook(new HookModExtensions());
            RegisterHook(new HookModDebug());
            RegisterHook(new HookUniqueItem());
            RegisterHook(new HookSkillExp());
            RegisterHook(new HookTeamManage());
            RegisterHook(new HookInitiactiveBattle());
            RegisterHook(new HookDuelPractice());
            //add by 寻宇
            RegisterHook(new HookFavExp());
            RegisterHook(new HookSave());
            RegisterHook(new HookMove());
            RegisterHook(new HookBuff());
            RegisterHook(new HookPropsFilterAndSort());
            RegisterHook(new HooknMartialArts());
            RegisterHook(new HookElective());
        }

        void Start()
        {
            Console.WriteLine("美好的第一帧开始");
        }

        void Update()
        {
            foreach(IHook hook in hooks)
            {
                hook.OnUpdate();
            }
        }
    }
}
