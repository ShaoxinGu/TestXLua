using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ���ڴ��е�һ��Bundle����
/// </summary>
public class BundleRef
{
    /// <summary>
    /// ���bundle�ľ�̬������Ϣ
    /// </summary>
    public BundleInfo bundleInfo;

    /// <summary>
    /// ��¼���BundleRef��Ӧ��AB�ļ���Ҫ���������
    /// </summary>
    public PathType pathType;

    /// <summary>
    /// ���ص��ڴ��bundle����
    /// </summary>
    public AssetBundle bundle;

    /// <summary>
    /// ���BundleRef����ЩAssetRef��������
    /// </summary>
    public List<AssetRef> children;

    /// <summary>
    /// BundleRef�Ĺ��캯��
    /// </summary>
    /// <param name="bundleInfo"></param>
    public BundleRef(BundleInfo bundleInfo, PathType pathType)
    {
        this.bundleInfo = bundleInfo;
        this.pathType = pathType;
    }
}
