using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public class ModuleManager : Singleton<ModuleManager>
{
    /// <summary>
    /// 加载一个模块
    /// </summary>
    /// <param name="moduleConfig">模块配置</param>
    public async Task<bool> Load(ModuleConfig moduleConfig)
    {
        if (GlobalConfig.hotUpdateh)
        {
            return await Downloader.Instance.Download(moduleConfig);
        }
        else
        {
            if (GlobalConfig.bundleMode)
            {
                ModuleABConfig moduleABConfig = await AssetLoader.Instance.LoadAssetBundleConfig(moduleConfig.moduleName);
                if(moduleABConfig == null)
                {
                    return false;
                }
                Hashtable pathToAssetRef = AssetLoader.Instance.ConfigAssembly(moduleABConfig);
                AssetLoader.Instance.moduleToAsset.Add(moduleConfig.moduleName, pathToAssetRef);
                return true;
            }
            else
            {
                return true;
            }
        }
    }
}
