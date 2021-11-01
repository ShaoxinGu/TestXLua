using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameLauncher : MonoBehaviour
{
    private void Awake()
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

        ModuleManager.Instance.Load(launchConfig, (success) =>
        {
            Debug.Log("Lua 代码开始");
        });
    }

    private void InitGlobal()
    {
        GlobalConfig.hotUpdateh = false;
        GlobalConfig.bundleMode = false;
    }

    public static GameLauncher Instance;
}
