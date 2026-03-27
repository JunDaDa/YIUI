using UnityEngine;

namespace ET.Client
{
    [Event(SceneType.Current)]
    public class AfterUnitCreate_CreateUnitView: AEvent<Scene, AfterUnitCreate>
    {
        protected override async ETTask Run(Scene scene, AfterUnitCreate args)
        {
            Unit unit = args.Unit;
            var prefab = await scene.GetComponent<ResourcesLoaderComponent>().LoadAssetAsync<GameObject>("shushan");
            GlobalComponent globalComponent = scene.Root().GetComponent<GlobalComponent>();
            GameObject go = UnityEngine.Object.Instantiate(prefab, globalComponent.Unit, true);
            go.transform.position = unit.Position;
            unit.AddComponent<GameObjectComponent>().GameObject = go;
            var spineComponent = unit.AddComponent<SpineComponent>();
            spineComponent.PlayByMotionType(MotionType.Idle);

            PlayerComponent playerComponent = scene.Root().GetComponent<PlayerComponent>();
            bool isMyUnit = playerComponent != null && unit.Id == playerComponent.MyId;
            Log.Info($"[Timing] 3a. AfterUnitCreate: unitId={unit.Id} isMyUnit={isMyUnit}");

            if (isMyUnit)
            {
                CameraComponent cameraComponent = scene.GetComponent<CameraComponent>();
                Log.Info($"[Timing] 3b. AfterUnitCreate: CameraComponent exists={cameraComponent != null}");
                if (cameraComponent != null)
                {
                    cameraComponent.SetFollowTarget(go.transform);
                }
            }

            await ETTask.CompletedTask;
        }
    }
}
