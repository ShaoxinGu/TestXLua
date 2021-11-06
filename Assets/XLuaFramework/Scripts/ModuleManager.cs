using System;
using System.Collections;
using System.Collections.Generic;
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
        if (GlobalConfig.hotUpdate)
        {
            await Downloader.Instance.Download(moduleConfig);
            bool updateBundleReady = await LoadBundleRef_Update(moduleConfig.moduleName);
            if (!updateBundleReady)
                return false;
            bool baseBundleReady = await LoadBundleRef_Base(moduleConfig.moduleName);
            if (!baseBundleReady)
                return false;

            bool updateReady = await LoadUpdate(moduleConfig.moduleName);
            return updateReady;
        }
        else
        {
            if (GlobalConfig.bundleMode)
            {
                bool baseBundleReady = await LoadBundleRef_Base(moduleConfig.moduleName);
                if (!baseBundleReady)
                    return false;
                return await LoadBase(moduleConfig.moduleName);
            }
            else
                return true;
        }
    }

    private async Task<bool> LoadBundleRef_Update(string moduleName)
    {
        ModuleABConfig moduleABConfig = await AssetLoader.Instance.LoadAssetBundleConfig(PathType.Update, moduleName, moduleName.ToLower() + ".json");
        if(moduleABConfig == null)
        {
            Debug.LogError("AB配置文件不存在：" + moduleName);
            return false;
        }

        foreach (KeyValuePair<string, BundleInfo> keyValue in moduleABConfig.bundleDict)
        {
            string bundleName = keyValue.Key;
            BundleInfo bundleInfo = keyValue.Value;

            Debug.Log("装配热更BundleRef：" + bundleName);
            AssetLoader.Instance.nameToBundleRef[bundleName] = new BundleRef(bundleInfo, PathType.Update);
        }
        return true;
    }


    private async Task<bool> LoadBundleRef_Base(string moduleName)
    {
        ModuleABConfig moduleABConfig = await AssetLoader.Instance.LoadAssetBundleConfig(PathType.Base, moduleName, moduleName.ToLower() + ".json");
        if (moduleABConfig == null)
        {
            Debug.LogError("AB配置文件不存在：" + moduleName);
            return false;
        }

        foreach (KeyValuePair<string, BundleInfo> keyValue in moduleABConfig.bundleDict)
        {
            string bundleName = keyValue.Key;
            if (!AssetLoader.Instance.nameToBundleRef.ContainsKey(bundleName))
            {
                Debug.Log("装配基础BundleRef：" + bundleName);
                BundleInfo bundleInfo = keyValue.Value;
                AssetLoader.Instance.nameToBundleRef[bundleName] = new BundleRef(bundleInfo, PathType.Base);
            }
        }
        return true;
    }

    private async Task<bool> LoadBase(string moduleName)
    {
        ModuleABConfig moduleABConfig = await AssetLoader.Instance.LoadAssetBundleConfig(PathType.Base, moduleName, moduleName.ToLower() + ".json");
        if (moduleABConfig == null)
            return false;
        Debug.Log($"模块{moduleName}的只读路径 包含AB包的总数量：{moduleABConfig.bundleDict.Count}");

        Hashtable pathToAssetRef = AssetLoader.Instance.ConfigAssembly(moduleABConfig);
        AssetLoader.Instance.baseToAsset.Add(moduleName, pathToAssetRef);
        return true;
    }

    private async Task<bool> LoadUpdate(string moduleName)
    {
        ModuleABConfig moduleABConfig = await AssetLoader.Instance.LoadAssetBundleConfig(PathType.Update, moduleName, moduleName.ToLower() + ".json");
        if (moduleABConfig == null)
            return false;
        Debug.Log($"模块{moduleName}的读写路径 包含AB包的总数量：{moduleABConfig.bundleDict.Count}");
        Hashtable pathToAssetRef = AssetLoader.Instance.ConfigAssembly(moduleABConfig);
        AssetLoader.Instance.updateToAsset.Add(moduleName, pathToAssetRef);
        return true;
    }
}
