using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// һ��bundle���ݡ��������л�Ϊjson�ļ���
/// </summary>
public class BundleInfo
{
    /// <summary>
    /// ���bundle������
    /// </summary>
    public string bundleName;

    /// <summary>
    /// ���bundle��Դ��crcɢ����
    /// </summary>
    public string crc;

    /// <summary>
    /// ���bundle����������Դ��·���б�
    /// </summary>
    public List<string> assets;
}

/// <summary>
/// һ��Asset���ݡ��������л�Ϊjson�ļ���
/// </summary>
public class AssetInfo
{
    /// <summary>
    /// �����Դ�����·��
    /// </summary>
    public string assetPath;

    /// <summary>
    /// �����Դ������AssetBundle������
    /// </summary>
    public string bundleName;

    /// <summary>
    /// �����Դ��������AssetBundle�б������
    /// </summary>
    public List<string> dependancies;
}

/// <summary>
/// ModuleABConfig���� ��Ӧ��������ģ���json�ļ�
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
    /// ����AssetBundle������ ֵ��AssetBundle���ݶ���
    /// </summary>
    public Dictionary<string, BundleInfo> bundleDict;

    /// <summary>
    /// asset����
    /// </summary>
    public AssetInfo[] assetArray;

    /// <summary>
    /// ����һ��bundle��¼
    /// </summary>
    /// <param name="bundleName"></param>
    /// <param name="bundleInfo"></param>
    public void AddBundle(string bundleName, BundleInfo bundleInfo)
    {
        bundleDict[bundleName] = bundleInfo;
    }

    /// <summary>
    /// ����һ����Դ��¼
    /// </summary>
    /// <param name="index"></param>
    /// <param name="assetInfo"></param>
    public void AddAsset(int index, AssetInfo assetInfo)
    {
        assetArray[index] = assetInfo;
    }
}