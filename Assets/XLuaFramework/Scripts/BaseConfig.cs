/// <summary>
/// 全局配置
/// </summary>
public static class GlobalConfig
{
    /// <summary>
    /// 是否开启热更
    /// </summary>
    public static bool hotUpdate;

    /// <summary>
    /// 是否采用bundle方式加载
    /// </summary>
    public static bool bundleMode;

    /// <summary>
    /// 全局配置的构造函数
    /// </summary>
    static GlobalConfig()
    {
        hotUpdate = false;
        bundleMode = false;
    }
}

/// <summary>
/// 单个模块的配置对象
/// </summary>
public class ModuleConfig
{
    /// <summary>
    /// 模块资源在远程服务器上的基础地址
    /// </summary>
    public string DownloadURL
    {
        get
        {
            return moduleUrl + "/" + moduleName + "/" + moduleVersion;
        }
    }

    /// <summary>
    /// 模块的名字
    /// </summary>
    public string moduleName;

    /// <summary>
    /// 模块的版本号
    /// </summary>
    public string moduleVersion;

    /// <summary>
    /// 模块的热更服务器地址
    /// </summary>
    public string moduleUrl;
}

/// <summary>
/// 路径类型
/// </summary>
public enum PathType
{
    /// <summary>
    /// APP的初始只读路径，对应streamingAssetsPath
    /// </summary>
    Base,

    /// <summary>
    /// APP的更新读写路径，对应persistentDataPath
    /// </summary>
    Update
}