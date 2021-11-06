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

    /// <summary>
    /// 打包AssetBundle资源，内部函数
    /// </summary>
    private static void BuildAssetBundle()
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

            string modueleOutputPath = abOutputPath + "/" + moduleName;
            if (Directory.Exists(modueleOutputPath))
            {
                Directory.Delete(modueleOutputPath, true);
            }

            Directory.CreateDirectory(modueleOutputPath);
            BuildPipeline.BuildAssetBundles(modueleOutputPath, assetBundleBuildList.ToArray(), BuildAssetBundleOptions.None, EditorUserBuildSettings.activeBuildTarget);

            CalculateDependencies();
            SaveModuleABConfig(moduleName);

            DeleteManifest(modueleOutputPath);
            File.Delete(modueleOutputPath + "/" + moduleName);

            AssetDatabase.Refresh();
        }
        #endregion

        Debug.Log("结束--->>>生成所有模块的AB包！");
    }

    /// <summary>
    /// 打本地测试包
    /// </summary>
    [MenuItem("AssetBundle/BuildAssetBundle_Dev")]
    public static void BuildAssetBundle_Dev()
    {
        abOutputPath = Application.streamingAssetsPath;
        BuildAssetBundle();
    }

    /// <summary>
    /// 打正式大版本的版本资源
    /// </summary>
    [MenuItem("AssetBundle/BuildAssetBundle_Base")]
    public static void BuildAssetBundle_Base()
    {
        abOutputPath = Application.dataPath + "/../AssetBundle_Base";
        BuildAssetBundle();
    }

    /// <summary>
    /// 正式打热更版本包
    /// </summary>
    [MenuItem("AssetBundle/BuildAssetBundle_Update")]
    public static void BuildAssetBundle_Update()
    {
        // 1.现在AssetBundle_Update文件夹中把AB包都生成出来
        abOutputPath = Application.dataPath + "/../AssetBundle_Update";
        BuildAssetBundle();

        // 2.再和AssetBundle_Base的资源版本进行比对，删除那些和AssetBundle_Base版本一样的资源

        string baseABPath = Application.dataPath + "/../AssetBundle_Base";
        if(!Directory.Exists(baseABPath))
        {
            Debug.LogWarning("找不到热更包对应的母包");
            return;
        }
        string updateABPath = abOutputPath;

        DirectoryInfo baseDir = new DirectoryInfo(baseABPath);

        //遍历baseABPath下的所有模块
        DirectoryInfo[] dirs = baseDir.GetDirectories();
        foreach (DirectoryInfo moduleDir in dirs)
        {
            string moduleName = moduleDir.Name;
            ModuleABConfig baseABConfig = LoadABConfig(baseABPath + "/" + moduleName + "/" + moduleName.ToLower() + ".json");
            ModuleABConfig updateABConfig = LoadABConfig(updateABPath + "/" + moduleName + "/" + moduleName.ToLower() + ".json");

            //计算出那些跟base版本比没有变化的bundle文件，即需要从热更包中删除的文件
            List<BundleInfo> removeList = Calculate(baseABConfig, updateABConfig);
            foreach (BundleInfo bundleInfo in removeList)
            {
                string filePath = updateABPath + "/" + moduleName + "/" + bundleInfo.bundleName;
                File.Delete(filePath);
                //同时需要处理一下热更包版本里的AB资源配置文件
                updateABConfig.bundleDict.Remove(bundleInfo.bundleName);
            }

            //重新生成热更包的AB资源配置文件
            string jsonPath = updateABPath + "/" + moduleName + "/" + moduleName.ToLower() + ".json";
            if (File.Exists(jsonPath))
                File.Delete(jsonPath);
            File.Create(jsonPath).Dispose();

            string jsonData = LitJson.JsonMapper.ToJson(updateABConfig);
            File.WriteAllText(jsonPath, jsonData);
        }
    }

    /// <summary>
    /// 计算热更包中需要删除的bundle文件列表
    /// </summary>
    /// <param name="baseABConfig"></param>
    /// <param name="updateABConfig"></param>
    /// <returns></returns>
    private static List<BundleInfo> Calculate(ModuleABConfig baseABConfig, ModuleABConfig updateABConfig)
    {
        // 收集所有的base版本的bundle文件，放到这个baseBundleDict字典中
        Dictionary<string, BundleInfo> baseBundleDict = new Dictionary<string, BundleInfo>();
        if (baseABConfig != null)
        {
            foreach (BundleInfo bundleInfo in baseABConfig.bundleDict.Values)
            {
                string uniqueId = string.Format("{0}/{1}", bundleInfo.bundleName, bundleInfo.crc);
                baseBundleDict.Add(uniqueId, bundleInfo);
            }
        }

        // 遍历Update版本中的bundle文件，把那些需要删除的bundle放入下面的removeList容器中
        List<BundleInfo> removeList = new List<BundleInfo>();
        foreach (BundleInfo bundleInfo in updateABConfig.bundleDict.Values)
        {
            string uniqueId = string.Format("{0}/{1}", bundleInfo.bundleName, bundleInfo.crc);
            if (baseBundleDict.ContainsKey(uniqueId))
            {
                removeList.Add(bundleInfo);
            }
        }
        return removeList;
    }

    /// <summary>
    /// 读取AB配置文件的工具函数
    /// </summary>
    /// <param name="abConfigPath"></param>
    /// <returns></returns>
    private static ModuleABConfig LoadABConfig(string abConfigPath)
    {
        return LitJson.JsonMapper.ToObject<ModuleABConfig>(File.ReadAllText(abConfigPath));
    }

    /// <summary>
    /// 删除Unity生成的manifest文件
    /// </summary>
    /// <param name="modueleOutputPath">模块对应的ab文件输出路径</param>
    private static void DeleteManifest(string modueleOutputPath)
    {
        FileInfo[] files = new DirectoryInfo(modueleOutputPath).GetFiles();
        foreach(FileInfo file in files)
        {
            if(file.Name.EndsWith(".manifest"))
            {
                file.Delete();
            }
        }
    }

    private static byte[] ConvertJsonString(string jsonData)
    {
        throw new NotImplementedException();
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
            string assetBundleName = directoryInfo.FullName.Substring(Application.dataPath.Length + 1).Replace('\\', '_').ToLower();
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

    /// <summary>
    /// 将一个模块的资源依赖关系数据保存成json格式的文件
    /// </summary>
    /// <param name="moduleName"></param>
    private static void SaveModuleABConfig(string moduleName)
    {
        ModuleABConfig moduleABConfig = new ModuleABConfig(assetToBundle.Count);

        //记录AB包信息
        foreach (AssetBundleBuild build in assetBundleBuildList)
        {
            BundleInfo bundleInfo = new BundleInfo();
            bundleInfo.bundleName = build.assetBundleName;
            bundleInfo.assets = new List<string>();
            foreach (string asset in build.assetNames)
            {
                bundleInfo.assets.Add(asset);
            }
            //计算一个bundle文件的CRC散列码
            string abFilePath = abOutputPath + "/" + moduleName + "/" + bundleInfo.bundleName;
            using (FileStream stream = File.OpenRead(abFilePath))
            {
                bundleInfo.crc = AssetUtility.GetCRC32Hash(stream);
                bundleInfo.size = (int)stream.Length;
            }
            moduleABConfig.AddBundle(bundleInfo.bundleName, bundleInfo);
        }

        //记录每个资源的依赖关系
        int assetIndex = 0;
        foreach (var item in assetToBundle)
        {
            AssetInfo assetInfo = new AssetInfo();
            assetInfo.assetPath = item.Key;
            assetInfo.bundleName = item.Value;
            assetInfo.dependancies = new List<string>();

            bool result = assetToDependencies.TryGetValue(item.Key, out List<string> dependancies);
            if (result)
                assetInfo.dependancies = dependancies;
            else
                assetInfo.dependancies = new List<string>();
            moduleABConfig.AddAsset(assetIndex, assetInfo);
            assetIndex++;
        }

        //开始写入Json文件
        string moduleConfigName = moduleName.ToLower() + ".json";
        string jsonPath = abOutputPath + "/" + moduleName + "/" + moduleConfigName;

        if (File.Exists(jsonPath))
            File.Delete(jsonPath);

        File.Create(jsonPath).Dispose();
        string jsonData = LitJson.JsonMapper.ToJson(moduleABConfig);
        File.WriteAllText(jsonPath, jsonData);
    }
}
