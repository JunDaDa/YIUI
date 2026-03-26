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

            // Only send when direction changes (MOVE-07: avoid per-frame message creation)
            if (!direction.Equals(self.LastDirection))
            {
                self.LastDirection = direction;
                self.IsDirectMoving = math.lengthsq(direction) > 0.001f;

                C2M_JoystickMove msg = C2M_JoystickMove.Create();
                msg.Direction = direction;
                self.Root().GetComponent<ClientSenderComponent>().Send(msg);
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
    }
}
