using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 一个bundle数据【用于序列化为json文件】
/// </summary>
public class BundleInfo
{
    /// <summary>
    /// 这个bundle的名字
    /// </summary>
    public string bundleName;

    /// <summary>
    /// 这个bundle资源的crc散列码
    /// </summary>
    public string crc;

    /// <summary>
    /// 这个bundle所包含的资源的路径列表
    /// </summary>
    public List<string> assets;
}

/// <summary>
/// 一个Asset数据【用于序列化为json文件】
/// </summary>
public class AssetInfo
{
    /// <summary>
    /// 这个资源的相对路径
    /// </summary>
    public string assetPath;

    /// <summary>
    /// 这个资源所属的AssetBundle的名字
    /// </summary>
    public string bundleName;

    /// <summary>
    /// 这个资源所依赖的AssetBundle列表的名字
    /// </summary>
    public List<string> dependancies;
}

/// <summary>
/// ModuleABConfig对象 对应整个单个模块的json文件
/// </summary>
public class ModuleABConfig
{
    public ModuleABConfig() { }

    public ModuleABConfig(int assetCount)
    {
        bundleDict = new Dictionary<string, BundleInfo>();
        assetArray = new AssetInfo[assetCount];
    }

    /// <summary>
    /// 键：AssetBundle的名字 值：AssetBundle数据对象
    /// </summary>
    public Dictionary<string, BundleInfo> bundleDict;

    /// <summary>
    /// asset数组
    /// </summary>
    public AssetInfo[] assetArray;

    /// <summary>
    /// 新增一个bundle记录
    /// </summary>
    /// <param name="bundleName"></param>
    /// <param name="bundleInfo"></param>
    public void AddBundle(string bundleName, BundleInfo bundleInfo)
    {
        bundleDict[bundleName] = bundleInfo;
    }

    /// <summary>
    /// 新增一个资源记录
    /// </summary>
    /// <param name="index"></param>
    /// <param name="assetInfo"></param>
    public void AddAsset(int index, AssetInfo assetInfo)
    {
        assetArray[index] = assetInfo;
    }
}