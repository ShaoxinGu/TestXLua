using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 内存中的单个资源对象
/// </summary>
public class AssetRef
{
    /// <summary>
    /// 这个资源的配置信息
    /// </summary>
    public AssetInfo assetInfo;

    /// <summary>
    /// 这个资源所属的BundleRef对象
    /// </summary>
    public BundleRef bundleRef;

    /// <summary>
    /// 这个资源所依赖的BundleRef对象列表
    /// </summary>
    public BundleRef[] dependancies;

    /// <summary>
    /// 从bundle文件中提取出来的资源对象
    /// </summary>
    public Object asset;

    /// <summary>
    /// 这个资源是否是Prefab
    /// </summary>
    public bool isPrefab;

    /// <summary>
    /// 这个AssetRef对象被哪些GameObject依赖
    /// </summary>
    public List<GameObject> children;

    /// <summary>
    /// AssetRef对象的构造函数
    /// </summary>
    /// <param name="assetInfo"></param>
    public AssetRef(AssetInfo assetInfo)
    {
        this.assetInfo = assetInfo;
    }
}
