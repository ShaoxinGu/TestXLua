using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using XLua;
using static XLua.LuaEnv;

public class GameLauncher : MonoBehaviour
{
    public async void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(this);

        InitGlobal();
        InitCustomLoaders();

        ModuleConfig launchConfig = new ModuleConfig()
        {
            moduleName = "Launch",
            moduleVersion = "202111051259",
            moduleUrl = "http://192.168.0.106:8000/"
        };

        bool result = await ModuleManager.Instance.Load(launchConfig);
        if (result)
        {
            Debug.Log("Lua 代码开始");
            gameObject.AddComponent<MonoProxy>().BindScript("Launch", "Main");
        }
    }

    public void Update()
    {
        //执行卸载策略
        AssetLoader.Instance.Unload(AssetLoader.Instance.baseToAsset);
    }

    /// <summary>
    /// 初始化全局变量
    /// </summary>
    private void InitGlobal()
    {
        GlobalConfig.hotUpdate = false;
        GlobalConfig.bundleMode = false;
    }

    /// <summary>
    /// 初始化自定义Lua加载器
    /// </summary>
    private void InitCustomLoaders()
    {
        DirectoryInfo baseDir = new DirectoryInfo(Application.dataPath + "/GameAssets");

        //遍历所有模块
        DirectoryInfo[] dirs = baseDir.GetDirectories();
        foreach(DirectoryInfo moduleDir in dirs)
        {
            string moduleName = moduleDir.Name;

            CustomLoader loader = (ref string scriptPath) =>
            {
                string assetPath = "Assets/GameAssets/" + moduleName + "/Src/" + scriptPath.Trim() + ".lua.txt";

                TextAsset asset = AssetLoader.Instance.CreateAsset<TextAsset>("Launch", assetPath, gameObject);
                if (asset != null)
                {
                    string scriptString = asset.text;

                    byte[] result = System.Text.Encoding.UTF8.GetBytes(scriptString);
                    return result;
                }
                return null;
            };
            luaEnv.AddLoader(loader);
        }
    }
    
    /// <summary>
    /// 整个工程共享一个LuaEnv对象
    /// </summary>
    public LuaEnv luaEnv { get; } = new LuaEnv();

    /// <summary>
    /// 主Mono对象
    /// </summary>
    public static GameLauncher Instance;
}
