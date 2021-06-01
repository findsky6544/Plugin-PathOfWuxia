using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Heluo.Platform;
using Heluo.Data;
using Steamworks;
using Heluo.Flow;
using Heluo;
using Heluo.UI;
using Heluo.FSM.Main;
using UnityEngine.UI;

namespace PathOfWuxia
{
    class HookSave : IHook
    {
        private static ConfigEntry<int> saveCount;
		private static ConfigEntry<bool> remindBlankSaveCount;
		private static ConfigEntry<bool> jumpToLatestSave;

		public IEnumerable<Type> GetRegisterTypes()
        {
            return new Type[] { GetType() };
        }

        public void OnRegister(BaseUnityPlugin plugin)
        {
            saveCount = plugin.Config.Bind("游戏设定", "存档数量", 20, "扩充存档数量");
			remindBlankSaveCount = plugin.Config.Bind("游戏设定", "自动存档剩余数量提示", false, "在自动存档剩余空白存档数量不足5个时弹窗提示");
			jumpToLatestSave = plugin.Config.Bind("游戏设定", "自动跳转到最新存档位置", false, "在存档数量太多的时候会有点作用");
		}

        public void OnUpdate()
        {

        }

		//修改存档数量
        [HarmonyPostfix, HarmonyPatch(typeof(SteamPlatform), "ListSaveHeaderFile", new Type[] { typeof(GameSaveType) })]
        public static void ListSaveHeaderFilePatch_changeSaveCount(ref SteamPlatform __instance,ref GameSaveType Type,ref List<PathOfWuxiaSaveHeader> __result)
		{
			string format = (Type == GameSaveType.Auto) ? "PathOfWuxia_{0:00}.autosave" : "PathOfWuxia_{0:00}.save";
			for (int i = 20; i < saveCount.Value; i++)//就改了这里
			{
				PathOfWuxiaSaveHeader pathOfWuxiaSaveHeader = null;
				string text = string.Format(format, i);
				if (SteamRemoteStorage.FileExists(text))
				{
					__instance.GetSaveFileHeader(text, ref pathOfWuxiaSaveHeader);
				}
				else
				{
					pathOfWuxiaSaveHeader = new PathOfWuxiaSaveHeader();
				}
				if (pathOfWuxiaSaveHeader == null)
				{
					pathOfWuxiaSaveHeader = new PathOfWuxiaSaveHeader();
				}
				__result.Add(pathOfWuxiaSaveHeader);
			}
		}

		//提示空白存档剩余数量
		//覆盖原逻辑，基本都是原代码
		//这里是场景中触发的自动存档
		[HarmonyPrefix, HarmonyPatch(typeof(SaveAction), "AutoSave")]
		public static bool AutoSavePatch_changeSaveCount(ref SaveAction __instance)
		{
			UIAutoSave uiautoSave = Game.UI.Open<UIAutoSave>();
			List<PathOfWuxiaSaveHeader> list = Game.Platform.ListSaveHeaderFile(GameSaveType.Auto);
			string format = "PathOfWuxia_{0:00}.{1}";
			int num = -1;
			DateTime saveTime = new DateTime(100L);
			for (int i = 0; i < list.Count; i++)
			{
				PathOfWuxiaSaveHeader pathOfWuxiaSaveHeader = list[i];
				if (!pathOfWuxiaSaveHeader.HasData)
				{
					num = i;
					break;
				}
				if (DateTime.Compare(pathOfWuxiaSaveHeader.SaveTime, saveTime) > 0)
				{
					num = ((i + 1 > saveCount.Value - 1) ? 0 : (i + 1));//存档满了不能只覆盖前20个
					saveTime = pathOfWuxiaSaveHeader.SaveTime;
				}
			}
			string filename = string.Format(format, num, "autosave");
			Game.GameData.AutoSaveTotalTime = Game.GameData.Round.TotalTime;
			Game.SaveAsync(filename, null);
			uiautoSave.Show();
			//提示空白存档剩余数量
			if (remindBlankSaveCount.Value && saveCount.Value - num - 1 <= 5)
            {
				string text = "空白存档数量剩余" + (saveCount.Value - num - 1) + "个，请及时扩容，否则将从头开始覆盖存档";
				Game.UI.OpenMessageWindow(text, null, true);
			}
			return false;
		}

		//这里是切换日期的自动存档
		[HarmonyPrefix, HarmonyPatch(typeof(InGame), "AutoSave")]
		public static bool AutoSavePatch2_changeSaveCount(ref InGame __instance)
		{
			List<PathOfWuxiaSaveHeader> list = Game.Platform.ListSaveHeaderFile(GameSaveType.Auto);
			string format = "PathOfWuxia_{0:00}.{1}";
			int num = -1;
			DateTime saveTime = new DateTime(100L);
			for (int i = 0; i < list.Count; i++)
			{
				PathOfWuxiaSaveHeader pathOfWuxiaSaveHeader = list[i];
				if (!pathOfWuxiaSaveHeader.HasData)
				{
					num = i;
					break;
				}
				if (DateTime.Compare(pathOfWuxiaSaveHeader.SaveTime, saveTime) > 0)
				{
					num = ((i + 1 > saveCount.Value - 1) ? 0 : (i + 1));//存档满了不能只覆盖前20个
					saveTime = pathOfWuxiaSaveHeader.SaveTime;
				}
			}
			string filename = string.Format(format, num, "autosave");
			Game.GameData.AutoSaveTotalTime = Game.GameData.Round.TotalTime;
			Game.SaveAsync(filename, null);

			//提示空白存档剩余数量
			if (remindBlankSaveCount.Value && saveCount.Value - num - 1 <= 5)
			{
				string text = "空白存档数量剩余" + (saveCount.Value - num - 1) + "个，请及时扩容，否则将从头开始覆盖存档";
				Game.UI.OpenMessageWindow(text, null, true);
			}
			return false;
		}

		[HarmonyPostfix, HarmonyPatch(typeof(CtrlSaveLoad), "UpdateSaveLoad")]
		public static void UpdateSaveLoadPatch_jumpToLatestSave(ref CtrlSaveLoad __instance,ref int __state)
		{
            if (jumpToLatestSave.Value)
			{
				int categoryIndex = Traverse.Create(__instance).Field("categoryIndex").GetValue<int>();
				List<PathOfWuxiaSaveHeader> saves;
				if (categoryIndex == 0)
				{
					saves = Traverse.Create(__instance).Field("saves").GetValue<List<PathOfWuxiaSaveHeader>>();
				}
				else
				{
					saves = Traverse.Create(__instance).Field("autosaves").GetValue<List<PathOfWuxiaSaveHeader>>();
				}

				int num = -1;
				DateTime saveTime = new DateTime(100L);

				for (int i = 0; i < saves.Count; i++)
				{
					PathOfWuxiaSaveHeader pathOfWuxiaSaveHeader = saves[i];
					if (!pathOfWuxiaSaveHeader.HasData)
					{
						num = i;
						break;
					}
					if (DateTime.Compare(pathOfWuxiaSaveHeader.SaveTime, saveTime) > 0)
					{
						num = ((i + 1 > saveCount.Value - 1) ? 0 : (i + 1));
						saveTime = pathOfWuxiaSaveHeader.SaveTime;
					}
				}

				UISaveLoad view = Traverse.Create(__instance).Field("view").GetValue<UISaveLoad>();
				WGTabScroll saveload = Traverse.Create(view).Field("saveload").GetValue<WGTabScroll>();
				WGInfiniteScroll loopScroll = Traverse.Create(saveload).Field("loopScroll").GetValue<WGInfiniteScroll>();
				ScrollRect scrollRect = Traverse.Create(loopScroll).Field("scrollRect").GetValue<ScrollRect>();
				scrollRect.verticalScrollbar.value = ((float)(saveCount.Value - num + 1))/saveCount.Value;
			}
		}
	}
}
