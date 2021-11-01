using System;
using UnityEngine;

public class ModuleManager : Singleton<ModuleManager>
{
    /// <summary>
    /// 加载一个模块
    /// </summary>
    /// <param name="moduleConfig">模块配置</param>
    /// <param name="moduleAction">模块回调</param>
    public void Load(ModuleConfig moduleConfig, Action<bool> moduleAction)
    {
        if (GlobalConfig.hotUpdateh)
        {
            Downloader.Instance.Download(moduleConfig, (downloadResult) =>
            {
                if (downloadResult)
                {
                    if (GlobalConfig.bundleMode)
                    {
                        LoadAssetBundleConfig(moduleConfig, moduleAction);
                    }
                    else
                    {
                        Debug.LogError("配置错误！HotUpdate == true && Bundle == false");
                    }
                }
                else
                {
                    Debug.LogError("下载失败!");
                }
            });
        }
        else
        {
            if (GlobalConfig.bundleMode)
            {
                LoadAssetBundleConfig(moduleConfig, moduleAction);
            }
            else
            {
                moduleAction(true);
            }
        }
    }

    private void LoadAssetBundleConfig(ModuleConfig moduleConfig, Action<bool> moduleAction)
    {
        AssetLoader.Instance.LoadAssetBundleConfig(moduleConfig.moduleName, (assetLoadResult) =>
        {
            if (assetLoadResult)
            {
                moduleAction(true);
            }
            else
            {
                Debug.LogError("LoadAssetBundleConfig 出错！");
            }
        });
    }
}
