using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 生成AssetBundle的编辑器工具
/// </summary>
public class ABEditor
{
    /// <summary>
    /// 热更资源的根目录
    /// </summary>
    public static string rootPath = Application.dataPath + "/GameAssets";

    /// <summary>
    /// AB文件的输出路径
    /// </summary>
    public static string abOutputPath = Application.streamingAssetsPath;

    /// <summary>
    /// 所有需要打包的AB包信息：一个AssetBundle文件对应一个AssetBundleBuild对象
    /// </summary>
    public static List<AssetBundleBuild> assetBundleBuildList = new List<AssetBundleBuild>();

    /// <summary>
    /// 记录每个Asset资源属于哪个AB包文件
    /// </summary>
    public static Dictionary<string, string> assetToBundle = new Dictionary<string, string>();

    /// <summary>
    /// 记录每个Asset资源依赖的AB包文件列表
    /// </summary>
    public static Dictionary<string, List<string>> assetToDependencies = new Dictionary<string, List<string>>();

    [MenuItem("AssetBundle/BuildAssetBundle")]
    public static void BuildAssetBundle()
    {
        Debug.Log("开始--->>>生成所有模块的AB包！");
        if (Directory.Exists(abOutputPath))
        {
            Directory.Delete(abOutputPath, true);
        }

        #region 遍历所有模块，针对所有模块都打包
        DirectoryInfo rootDir = new DirectoryInfo(rootPath);
        DirectoryInfo[] dirs = rootDir.GetDirectories();
        foreach (DirectoryInfo moduleDir in dirs)
        {
            assetBundleBuildList.Clear();
            assetToBundle.Clear();
            assetToDependencies.Clear();

            string moduleName = moduleDir.Name;
            ScanChildDirectories(moduleDir);
            AssetDatabase.Refresh();

            string modueleOutputPath = abOutputPath + "/" + moduleName;
            if (Directory.Exists(modueleOutputPath))
            {
                Directory.Delete(modueleOutputPath, true);
            }

            Directory.CreateDirectory(modueleOutputPath);
            BuildPipeline.BuildAssetBundles(modueleOutputPath, assetBundleBuildList.ToArray(), BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);

            CalculateDependencies();

            AssetDatabase.Refresh();
        }
        #endregion

        Debug.Log("结束--->>>生成所有模块的AB包！");
    }

    /// <summary>
    /// 根据指定的文件夹，将文件夹下所有的一级子文件打成一个AssetBundle，并且递归遍历这个文件夹下的所有子文件夹
    /// </summary>
    /// <param name=""></param>
    private static void ScanChildDirectories(DirectoryInfo directoryInfo)
    {
        ScanCurDirectory(directoryInfo);

        DirectoryInfo[] dirs = directoryInfo.GetDirectories();
        foreach (DirectoryInfo info in dirs)
        {
            ScanChildDirectories(info);
        }
    }

    /// <summary>
    /// 遍历当前路径下的文件，把它们打成一个AB包
    /// </summary>
    /// <param name="directoryInfo"></param>
    private static void ScanCurDirectory(DirectoryInfo directoryInfo)
    {
        List<string> assetNames = new List<string>();
        FileInfo[] fileInfoList = directoryInfo.GetFiles();
        foreach (FileInfo fileInfo in fileInfoList)
        {
            if (fileInfo.Name.EndsWith(".meta"))
            {
                continue;
            }

            string assetName = fileInfo.FullName.Substring(Application.dataPath.Length - "Assets".Length).Replace('\\', '/');
            assetNames.Add(assetName);
        }

        if (assetNames.Count > 0)
        {
            string assetBundleName = directoryInfo.FullName.Substring(Application.dataPath.Length + 1).Replace('/', '_').ToLower();
            AssetBundleBuild build = new AssetBundleBuild();
            build.assetBundleName = assetBundleName;
            build.assetNames = new string[assetNames.Count];

            for (int i = 0; i < assetNames.Count; i++)
            {
                build.assetNames[i] = assetNames[i];
                assetToBundle.Add(assetNames[i], assetBundleName);
            }

            assetBundleBuildList.Add(build);
        }
    }

    /// <summary>
    /// 计算每个资源所依赖的ab包文件列表
    /// </summary>
    private static void CalculateDependencies()
    {
        foreach (string asset in assetToBundle.Keys)
        {
            string assetBundle = assetToBundle[asset];
            string[] dependencies = AssetDatabase.GetDependencies(asset);

            List<string> assetList = new List<string>();

            if (dependencies != null && dependencies.Length > 0)
            {
                foreach (string oneAsset in dependencies)
                {
                    if (oneAsset == asset || oneAsset.EndsWith(".cs"))
                    {
                        continue;
                    }
                    assetList.Add(oneAsset);
                }
            }

            if (assetList.Count > 0)
            {
                List<string> abList = new List<string>();
                foreach (string oneAsset in assetList)
                {
                    bool result = assetToBundle.TryGetValue(oneAsset, out string bundle);

                    if (result && bundle != assetBundle)
                    {
                        abList.Add(bundle);
                    }
                }
                assetToDependencies.Add(asset, abList);
            }
        }
    }

    private static void SaveModuleABConfig(string moduleName)
    {
        ModuleABConfig moduleConfig = new ModuleABConfig(assetToBundle.Count);
    }
}
