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

namespace PathOfWuxia
{
    class HookMove : IHook
    {
        private static ConfigEntry<int> moveSpeed;

        public IEnumerable<Type> GetRegisterTypes()
        {
            return new Type[] { GetType() };
        }

        public void OnRegister(BaseUnityPlugin plugin)
        {
            moveSpeed = plugin.Config.Bind("游戏设定", "移动速度", 1, "修改玩家在大地图的移动速度 如果太快可能会穿模");
        }

        public void OnUpdate()
        {

        }

		//替换原逻辑
        [HarmonyPrefix, HarmonyPatch(typeof(Move), "FixedUpdate")]
        public static bool UpdateRelationshipPatch_showFavExp(ref Move __instance)
		{
			Vector3 moveVector = Traverse.Create(__instance).Field("moveVector").GetValue<Vector3>();
			Vector3 playerNewForward = Traverse.Create(__instance).Field("playerNewForward").GetValue<Vector3>();
			float forwardSpeed = Traverse.Create(__instance).Field("forwardSpeed").GetValue<float>();
			float forwardRate = Traverse.Create(__instance).Field("forwardRate").GetValue<float>();
			GameObject model = Traverse.Create(__instance).Field("model").GetValue<GameObject>();
			float rotateSpeed = Traverse.Create(__instance).Field("rotateSpeed").GetValue<float>();
			MainActorController actorController = Traverse.Create(__instance).Field("actorController").GetValue< MainActorController>();
			float playerRadius = Traverse.Create(__instance).Field("playerRadius").GetValue<float>();
			LayerMask forwardMask = Traverse.Create(__instance).Field("forwardMask").GetValue<LayerMask>();
			Rigidbody rigidbody = Traverse.Create(__instance).Field("rigidbody").GetValue<Rigidbody>();


			moveVector = playerNewForward * Time.fixedDeltaTime * forwardSpeed * forwardRate * moveSpeed.Value;//实际上就在这里加了一个变量
			float num = Vector3.Angle(model.transform.forward, playerNewForward);
			if (Mathf.Abs(num) > 0.001f)
			{
				float num2 = Time.fixedDeltaTime * num * rotateSpeed;
				if (Vector3.Dot(model.transform.right, playerNewForward) > 0f)
				{
					actorController.Turn = num2;
					model.transform.Rotate(Vector3.up, num2);
				}
				else
				{
					actorController.Turn = -num2;
					model.transform.Rotate(Vector3.up, -num2);
				}
			}
			Vector3 b = moveVector * 2f + moveVector.normalized * playerRadius + playerRadius * Vector3.up;
			Vector3 b2 = moveVector.magnitude * Vector3.up;
			Vector3 vector = model.transform.position + playerRadius * Vector3.up;
			RaycastHit raycastHit = default(RaycastHit);
			bool flag = Physics.Linecast(vector, vector + b + b2 - playerRadius * model.transform.right, out raycastHit, forwardMask);
			if (flag)
			{
				flag = !raycastHit.collider.isTrigger;
			}
			bool flag2 = Physics.Linecast(vector, vector + b + b2 + playerRadius * model.transform.right, out raycastHit, forwardMask);
			if (flag2)
			{
				flag2 = !raycastHit.collider.isTrigger;
			}
			Debug.DrawLine(vector, vector + b + b2 - playerRadius * model.transform.right, Color.red);
			Debug.DrawLine(vector, vector + b + b2 + playerRadius * model.transform.right, Color.red);
			if (!flag && !flag2)
			{
				Vector3 position = model.transform.position + moveVector;
				rigidbody.MovePosition(position);
			}

			return false;
		}
    }
}
