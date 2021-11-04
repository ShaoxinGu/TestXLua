using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameLauncher : MonoBehaviour
{
    public async void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(this);

        InitGlobal();

        ModuleConfig launchConfig = new ModuleConfig()
        {
            moduleName = "Launch",
            moduleVersion = "202110281116",
            moduleUrl = "http://192.168.0.7/8080"
        };

        bool result = await ModuleManager.Instance.Load(launchConfig);
        if(result)
        {
            Debug.Log("Lua 代码开始");
            AssetLoader.Instance.Clone("Launch", "Assets/GameAssets/Launch/Sphere.prefab");

            GameObject philip = AssetLoader.Instance.Clone("Launch", "Assets/GameAssets/Launch/Philip.prefab");
            Sprite sprite = AssetLoader.Instance.CreateAsset<Sprite>("Launch", "Assets/GameAssets/Launch/Sprite/header.jpg", philip);
            philip.GetComponent<SpriteRenderer>().sprite = sprite;
        }
    }

    private void InitGlobal()
    {
        GlobalConfig.hotUpdateh = false;
        GlobalConfig.bundleMode = true;
    }

    public static GameLauncher Instance;
}
