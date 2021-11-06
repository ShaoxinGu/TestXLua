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
            moduleVersion = "202111051259",
            moduleUrl = "http://192.168.0.106:8000/"
        };

        bool result = await ModuleManager.Instance.Load(launchConfig);
        if (result)
        {
            Debug.Log("Lua 代码开始");
            AssetLoader.Instance.Clone("Launch", "Assets/GameAssets/Launch/Sphere.prefab");

            GameObject philip = AssetLoader.Instance.Clone("Launch", "Assets/GameAssets/Launch/Philip.prefab");
            Sprite sprite = AssetLoader.Instance.CreateAsset<Sprite>("Launch", "Assets/GameAssets/Launch/Sprites/header.jpg", philip);
            philip.GetComponent<SpriteRenderer>().sprite = sprite;
        }
    }

    public void Update()
    {
        //执行卸载策略
    }

    private void InitGlobal()
    {
        GlobalConfig.hotUpdate = true;
        GlobalConfig.bundleMode = true;
    }

    public static GameLauncher Instance;
}
