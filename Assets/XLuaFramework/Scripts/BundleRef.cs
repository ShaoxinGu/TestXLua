using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 在内存中的一个Bundle对象
/// </summary>
public class BundleRef
{
    /// <summary>
    /// 这个bundle的静态配置信息
    /// </summary>
    public BundleInfo bundleInfo;

    /// <summary>
    /// 记录这个BundleRef对应的AB文件需要从哪里加载
    /// </summary>
    public PathType pathType;

    /// <summary>
    /// 加载到内存的bundle对象
    /// </summary>
    public AssetBundle bundle;

    /// <summary>
    /// 这个BundleRef被哪些AssetRef对象依赖
    /// </summary>
    public List<AssetRef> children;

    /// <summary>
    /// BundleRef的构造函数
    /// </summary>
    /// <param name="bundleInfo"></param>
    public BundleRef(BundleInfo bundleInfo, PathType pathType)
    {
        this.bundleInfo = bundleInfo;
        this.pathType = pathType;
    }
}
