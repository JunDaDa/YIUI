using UnityEngine;

namespace ET.Client
{
    [Event(SceneType.Current)]
    public class ChangePosition_SyncGameObjectPos: AEvent<Scene, ChangePosition>
    {
        /// <summary>
        /// 本地玩家偏差超过此阈值时才校正位置（避免与客户端预测冲突导致抖动）
        /// </summary>
        private const float CorrectionThreshold = 0.5f;

        protected override async ETTask Run(Scene scene, ChangePosition args)
        {
            Unit unit = args.Unit;
            GameObjectComponent gameObjectComponent = unit.GetComponent<GameObjectComponent>();
            if (gameObjectComponent == null)
            {
                return;
            }

            Transform transform = gameObjectComponent.Transform;
            Vector3 serverPos = unit.Position;

            // 本地玩家：客户端预测移动中，只在偏差过大时校正
            PlayerComponent playerComponent = scene.Root().GetComponent<PlayerComponent>();
            if (playerComponent != null && unit.Id == playerComponent.MyId)
            {
                float sqrDist = (transform.position - serverPos).sqrMagnitude;
                if (sqrDist > CorrectionThreshold * CorrectionThreshold)
                {
                    // 偏差过大，直接拉回
                    transform.position = serverPos;
                }
                // 偏差小则忽略，让客户端预测继续平滑移动
            }
            else
            {
                // 远程单位：直接同步位置
                transform.position = serverPos;
            }

            await ETTask.CompletedTask;
        }
    }
}
