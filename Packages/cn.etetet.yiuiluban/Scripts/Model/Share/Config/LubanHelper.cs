using System.Collections.Generic;

namespace ET
{
    public static class LubanHelper
    {
        //编辑器下的配置资源路径
        public const string ConfigResPath = "Packages/cn.etetet.yiuilubangen/Assets/LubanGen";

        [StaticField]
        public static List<string> StartConfigs = new()
        {
            "StartMachineConfigCategory",
            "StartProcessConfigCategory",
            "StartSceneConfigCategory",
            "StartZoneConfigCategory",
        };
    }
}