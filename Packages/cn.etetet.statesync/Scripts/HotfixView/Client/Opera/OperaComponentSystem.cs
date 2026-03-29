using Unity.Mathematics;
using UnityEngine;

namespace ET.Client
{
    [EntitySystemOf(typeof(OperaComponent))]
    public static partial class OperaComponentSystem
    {
        [EntitySystem]
        private static void Awake(this OperaComponent self)
        {
            self.mapMask = LayerMask.GetMask("Map");

            // 缓存移动速度，避免每帧查配置表
            var param = GlobalParamConfigCategory.Instance?.GetOrDefault("PlayerMoveSpeed");
            self.MoveSpeed = param?.Value ?? 5f;
        }

        [EntitySystem]
        private static void Update(this OperaComponent self)
        {
            // === WASD Continuous Input (MOVE-01, MOVE-06) ===
            float x = 0f;
            float z = 0f;
            if (Input.GetKey(KeyCode.W)) z += 1f;
            if (Input.GetKey(KeyCode.S)) z -= 1f;
            if (Input.GetKey(KeyCode.A)) x -= 1f;
            if (Input.GetKey(KeyCode.D)) x += 1f;

            float3 direction = float3.zero;
            if (x != 0f || z != 0f)
            {
                direction = math.normalize(new float3(x, 0f, z));
            }

            // === 本地预测移动：每帧按方向移动 Transform，避免等服务器回包造成抖动 ===
            if (self.IsDirectMoving && self.MyUnitTransform != null)
            {
                float3 delta = self.LastDirection * self.MoveSpeed * Time.deltaTime;
                self.MyUnitTransform.position += (Vector3)delta;
            }

            // Only send when direction changes (MOVE-07: avoid per-frame message creation)
            if (!direction.Equals(self.LastDirection))
            {
                bool wasMoving = self.IsDirectMoving;
                self.LastDirection = direction;
                self.IsDirectMoving = math.lengthsq(direction) > 0.001f;

                C2M_JoystickMove msg = C2M_JoystickMove.Create();
                msg.Direction = direction;
                self.Root().GetComponent<ClientSenderComponent>().Send(msg);

                // 缓存本地玩家 Transform（首次或切换时）
                if (self.MyUnitTransform == null)
                {
                    CacheMyUnitTransform(self);
                }

                // Spine animation & flip for local player
                Unit myUnit = UnitHelper.GetMyUnitFromClientScene(self.Root());
                if (myUnit != null)
                {
                    SpineComponent spineComponent = myUnit.GetComponent<SpineComponent>();
                    if (spineComponent != null)
                    {
                        // 仅在 idle↔run 状态切换时才切换动画，方向变化不打断
                        if (self.IsDirectMoving && !wasMoving)
                        {
                            spineComponent.PlayByMotionType(MotionType.Run);
                        }
                        else if (!self.IsDirectMoving && wasMoving)
                        {
                            spineComponent.PlayByMotionType(MotionType.Idle);
                        }

                        // 移动中更新朝向翻转
                        if (self.IsDirectMoving)
                        {
                            if (x > 0f)
                            {
                                spineComponent.SetFlipX(true);
                            }
                            else if (x < 0f)
                            {
                                spineComponent.SetFlipX(false);
                            }
                        }
                    }
                }
            }

            // === Click-to-Move (existing, with mutual cancellation MOVE-05) ===
            if (Input.GetMouseButtonDown(1))
            {
                // Cancel WASD movement if active
                if (self.IsDirectMoving)
                {
                    self.LastDirection = float3.zero;
                    self.IsDirectMoving = false;
                    C2M_JoystickMove stopMsg = C2M_JoystickMove.Create();
                    stopMsg.Direction = float3.zero;
                    self.Root().GetComponent<ClientSenderComponent>().Send(stopMsg);

                    // Return to idle
                    Unit myUnit = UnitHelper.GetMyUnitFromClientScene(self.Root());
                    if (myUnit != null)
                    {
                        SpineComponent spineComponent = myUnit.GetComponent<SpineComponent>();
                        if (spineComponent != null)
                        {
                            spineComponent.PlayByMotionType(MotionType.Idle);
                        }
                    }
                }

                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, 1000, self.mapMask))
                {
                    C2M_PathfindingResult c2MPathfindingResult = C2M_PathfindingResult.Create();
                    c2MPathfindingResult.Position = hit.point;
                    self.Root().GetComponent<ClientSenderComponent>().Send(c2MPathfindingResult);
                }
            }

            // === Utility keys (keep) ===
            if (Input.GetKeyDown(KeyCode.R))
            {
                CodeLoader.Instance.Reload();
            }

            if (Input.GetKeyDown(KeyCode.T))
            {
                C2M_TransferMap c2MTransferMap = C2M_TransferMap.Create();
                self.Root().GetComponent<ClientSenderComponent>().Call(c2MTransferMap).NoContext();
            }
        }

        /// <summary>
        /// 缓存本地玩家的 Transform 引用，避免每帧 GetComponent
        /// </summary>
        private static void CacheMyUnitTransform(OperaComponent self)
        {
            Unit myUnit = UnitHelper.GetMyUnitFromClientScene(self.Root());
            if (myUnit == null)
            {
                return;
            }

            GameObjectComponent goComp = myUnit.GetComponent<GameObjectComponent>();
            if (goComp != null)
            {
                self.MyUnitTransform = goComp.Transform;
            }
        }
    }
}
