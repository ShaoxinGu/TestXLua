using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// �ڴ��еĵ�����Դ����
/// </summary>
public class AssetRef
{
    /// <summary>
    /// �����Դ��������Ϣ
    /// </summary>
    public AssetInfo assetInfo;

    /// <summary>
    /// �����Դ������BundleRef����
    /// </summary>
    public BundleRef bundleRef;

    /// <summary>
    /// �����Դ��������BundleRef�����б�
    /// </summary>
    public BundleRef[] dependancies;

    /// <summary>
    /// ��bundle�ļ�����ȡ��������Դ����
    /// </summary>
    public Object asset;

    /// <summary>
    /// �����Դ�Ƿ���Prefab
    /// </summary>
    public bool isPrefab;

    /// <summary>
    /// ���AssetRef������ЩGameObject����
    /// </summary>
    public List<GameObject> children;

    /// <summary>
    /// AssetRef����Ĺ��캯��
    /// </summary>
    /// <param name="assetInfo"></param>
    public AssetRef(AssetInfo assetInfo)
    {
        this.assetInfo = assetInfo;
    }
}
