using System.Collections.Generic;
using UnityEngine;
using YIUIFramework;

namespace ET.Client
{
    //主要用于在GM包上测试功能
    //当前包没有强制引用GM包
    //如果没有引用GM包  请删除这个文件
    [GM(EGMType.SuperScrollDemo, 1, "SuperScrollDemo")]
    public class GM_SuperScrollDemo_1 : IGMCommand
    {
        public List<GMParamInfo> GetParams()
        {
            return new();
        }

        public async ETTask<bool> Run(Scene clientScene, ParamVo paramVo)
        {
            clientScene.YIUIRoot().OpenPanelAsync<SuperScrollDemoPanelComponent>().NoContext();
            await ETTask.CompletedTask;
            return true;
        }
    }
}