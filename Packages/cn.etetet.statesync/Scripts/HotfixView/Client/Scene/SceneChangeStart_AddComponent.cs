using System;
using UnityEngine.SceneManagement;

namespace ET.Client
{
    [Event(SceneType.StateSync)]
    public class SceneChangeStart_AddComponent: AEvent<Scene, SceneChangeStart>
    {
        protected override async ETTask Run(Scene root, SceneChangeStart args)
        {
            try
            {
                Scene currentScene = root.CurrentScene();

                ResourcesLoaderComponent resourcesLoaderComponent = currentScene.GetComponent<ResourcesLoaderComponent>();

                // 加载场景资源
                Log.Info("[Timing] 1a. SceneChangeStart: await LoadSceneAsync begin");
                await resourcesLoaderComponent.LoadSceneAsync($"Packages/cn.etetet.demores/Scenes/{currentScene.Name}.unity", LoadSceneMode.Single);
                Log.Info("[Timing] 1b. SceneChangeStart: LoadSceneAsync done");

                currentScene.AddComponent<OperaComponent>();
                CameraComponent cameraComponent = currentScene.AddComponent<CameraComponent>();
                Log.Info("[Timing] 1c. SceneChangeStart: CameraComponent added");

                // 场景加载可能比 Wait_CreateMyUnit 慢，主角 Unit 可能已经创建
                // 此时需要补设相机跟随目标
                PlayerComponent playerComponent = root.GetComponent<PlayerComponent>();
                if (playerComponent != null)
                {
                    UnitComponent unitComponent = currentScene.GetComponent<UnitComponent>();
                    Unit myUnit = unitComponent?.Get(playerComponent.MyId);
                    if (myUnit != null)
                    {
                        GameObjectComponent goComponent = myUnit.GetComponent<GameObjectComponent>();
                        if (goComponent?.GameObject != null)
                        {
                            Log.Info("[Timing] 1d. SceneChangeStart: late-bind camera to existing unit");
                            cameraComponent.SetFollowTarget(goComponent.GameObject.transform);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }

        }
    }
}
