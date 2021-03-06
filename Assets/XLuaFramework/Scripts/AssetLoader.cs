using LitJson;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 模块资源加载器
/// </summary>
public class AssetLoader : Singleton<AssetLoader>
{
    /// <summary>
    /// streamingAssetsPath下的资源集合
    /// </summary>
    public Dictionary<string, Hashtable> baseToAsset;

    /// <summary>
    /// persistentDataPath下的资源集合
    /// </summary>
    public Dictionary<string, Hashtable> updateToAsset;

    /// <summary>
    /// 记录所有的BundleRef对象
    /// </summary>
    public Dictionary<string, BundleRef> nameToBundleRef;

    /// <summary>
    /// 模块资源加载器的构造函数
    /// </summary>
    public AssetLoader()
    {
        baseToAsset = new Dictionary<string, Hashtable>();
        updateToAsset = new Dictionary<string, Hashtable>();
        nameToBundleRef = new Dictionary<string, BundleRef>();
    }

    /// <summary>
    /// 根据模块的json配置文件，创建内存中的资源容器
    /// </summary>
    /// <param name="moduleABConfig"></param>
    /// <returns></returns>
    public Hashtable ConfigAssembly(ModuleABConfig moduleABConfig)
    {
        Hashtable pathToAssetRef = new Hashtable();
        for (int i = 0; i < moduleABConfig.assetArray.Length; i++)
        {
            AssetInfo assetInfo = moduleABConfig.assetArray[i];

            //装配一个AssetRef对象
            AssetRef assetRef = new AssetRef(assetInfo);
            assetRef.bundleRef = nameToBundleRef[assetInfo.bundleName];

            int count = assetInfo.dependancies.Count;
            assetRef.dependancies = new BundleRef[count];
            for (int j = 0; j < count; j++)
            {
                string bundleName = assetInfo.dependancies[j];
                assetRef.dependancies[j] = nameToBundleRef[bundleName];
            }
            //装配好了放到PathToAssetRef容器中
            pathToAssetRef.Add(assetInfo.assetPath, assetRef);
        }
        return pathToAssetRef;
    }

    internal void Unload(Dictionary<string, Hashtable> baseToAsset)
    {
        //TODO 实现卸载资源
    }

    /// <summary>
    /// 加载模块对应的全局AssetBundle资源管理文件
    /// </summary>
    /// <param name="pathType">路径类型</param>
    /// <param name="moduelName">模块名字</param>
    /// <param name="configName">AB资源配置文件的名字</param>
    /// <returns></returns>
    public async Task<ModuleABConfig> LoadAssetBundleConfig(PathType pathType, string moduelName, string configName)
    {
        string url = GetBundlePath(pathType, moduelName, configName);

        UnityWebRequest request = UnityWebRequest.Get(url);

        await request.SendWebRequest();

        if (string.IsNullOrEmpty(request.error))
        {
            return JsonMapper.ToObject<ModuleABConfig>(request.downloadHandler.text);
        }

        return null;
    }

    /// <summary>
    /// 克隆一个GameObject对象
    /// </summary>
    /// <param name="moduleName"></param>
    /// <param name="path"></param>
    /// <returns></returns>
    public GameObject Clone(string moduleName, string path)
    {
        AssetRef assetRef = LoadAssetRef<GameObject>(moduleName, path);

        if (assetRef == null || assetRef.asset == null)
            return null;

        GameObject gameObject = UnityEngine.Object.Instantiate(assetRef.asset) as GameObject;

        if (assetRef.children == null)
        {
            assetRef.children = new List<GameObject>();
        }
        assetRef.children.Add(gameObject);

        return gameObject;
    }

    /// <summary>
    /// 创建资源对象，并且将其赋予游戏对象gameObject
    /// </summary>
    /// <typeparam name="T">资源的类型</typeparam>
    /// <param name="moduleName">模块的名字</param>
    /// <param name="assetPath">资源的路径</param>
    /// <param name="gameObject">资源加载后，要挂载到的游戏对象</param>
    /// <returns></returns>
    public T CreateAsset<T>(string moduleName, string assetPath, GameObject gameObject) where T : UnityEngine.Object
    {
        if (typeof(T) == typeof(GameObject) || (!string.IsNullOrEmpty(assetPath) && assetPath.EndsWith(".prefab")))
        {
            Debug.LogError("不可以加载GameObject类型，请直接使用AssetLoader.Instance.Clone接口，path:" + assetPath);
            return null;
        }

        if (gameObject == null)
        {
            Debug.LogError("CreateAsset必须传递一个其将要被挂载的GameObject对象！");
            return null;
        }

        AssetRef assetRef = LoadAssetRef<T>(moduleName, assetPath);
        if (assetRef == null || assetRef.asset == null)
            return null;

        if (assetRef.children == null)
        {
            assetRef.children = new List<GameObject>();
        }
        assetRef.children.Add(gameObject);

        return assetRef.asset as T;
    }

    /// <summary>
    /// 加载AssetRef对象
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="moduleName">模块名字</param>
    /// <param name="assetPath">资源的相对路径</param>
    /// <returns></returns>
    private AssetRef LoadAssetRef<T>(string moduleName, string assetPath) where T : UnityEngine.Object
    {
#if UNITY_EDITOR
        if (GlobalConfig.bundleMode)
            return LoadAssetRef_Runtime<T>(moduleName, assetPath);
        else
            return LoadAssetRef_Editor<T>(moduleName, assetPath);
#else
        return LoadAssetRef_Runtime<T>(moduleName, assetPath);
#endif
    }

    /// <summary>
    /// 在编辑器模式下加载AssetRef对象
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="moduleName">模块名字</param>
    /// <param name="assetPath">资源的相对路径</param>
    /// <returns></returns>
    private AssetRef LoadAssetRef_Editor<T>(string moduleName, string assetPath) where T : UnityEngine.Object
    {
#if UNITY_EDITOR
        if (string.IsNullOrEmpty(moduleName))
            return null;
        AssetRef assetRef = new AssetRef(null);
        assetRef.asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
        return assetRef;
#else
        return null;
#endif
    }

    /// <summary>
    /// 在AB包模式下加载AssetRef对象
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="moduleName"></param>
    /// <param name="assetPath"></param>
    /// <returns></returns>
    private AssetRef LoadAssetRef_Runtime<T>(string moduleName, string assetPath) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(moduleName))
            return null;

        Hashtable moduleToAssetRef;
        if (GlobalConfig.hotUpdate)
            moduleToAssetRef = updateToAsset[moduleName];
        else
            moduleToAssetRef = baseToAsset[moduleName];

        AssetRef assetRef = (AssetRef)moduleToAssetRef[assetPath];
        if (assetRef == null)
        {
            Debug.LogError("未找到资源：moduleName " + moduleName + " assetPath " + assetPath);
            return null;
        }

        if (assetRef.asset != null)
            return assetRef;

        //1.处理assetRef依赖的bundleRef列表
        foreach (BundleRef oneBundleRef in assetRef.dependancies)
        {
            if (oneBundleRef.bundle == null)
            {
                string bundlePath = GetBundlePath(oneBundleRef.pathType, moduleName, oneBundleRef.bundleInfo.bundleName);
                oneBundleRef.bundle = AssetBundle.LoadFromFile(bundlePath);
            }

            if (oneBundleRef.children == null)
            {
                oneBundleRef.children = new List<AssetRef>();
            }
            oneBundleRef.children.Add(assetRef);
        }

        //2.处理assetRef属于的bundleRef对象
        BundleRef bundleRef = assetRef.bundleRef;
        if (bundleRef.bundle == null)
        {
            string bundlePath = GetBundlePath(bundleRef.pathType, moduleName, bundleRef.bundleInfo.bundleName);
            bundleRef.bundle = AssetBundle.LoadFromFile(bundlePath);
        }

        if (bundleRef.children == null)
        {
            bundleRef.children = new List<AssetRef>();
        }
        bundleRef.children.Add(assetRef);

        //3.从bundle中提取asset
        assetRef.asset = assetRef.bundleRef.bundle.LoadAsset<T>(assetRef.assetInfo.assetPath);
        if (typeof(T) == typeof(GameObject) && assetRef.assetInfo.assetPath.EndsWith(".prefab"))
            assetRef.isPrefab = true;
        else
            assetRef.isPrefab = false;

        return assetRef;
    }

    /// <summary>
    /// 工具函数 根据模块名和bundle名返回实际的资源路径
    /// </summary>
    /// <param name="moduleName"></param>
    /// <param name="bundleName"></param>
    /// <returns></returns>
    private string GetBundlePath(PathType pathType, string moduleName, string bundleName)
    {
        if (pathType == PathType.Update)
            return Application.persistentDataPath + "/Bundles/" + moduleName + "/" + bundleName;
        else
            return Application.streamingAssetsPath + "/" + moduleName + "/" + bundleName;
    }
}
