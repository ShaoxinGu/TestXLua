/// <summary>
/// 全局配置
/// </summary>
public static class GlobalConfig
{
    /// <summary>
    /// 是否开启热更
    /// </summary>
    public static bool hotUpdateh;

    /// <summary>
    /// 是否采用bundle方式加载
    /// </summary>
    public static bool bundleMode;

    /// <summary>
    /// 全局配置的构造函数
    /// </summary>
    static GlobalConfig()
    {
        hotUpdateh = false;
        bundleMode = false;
    }
}

public class ModuleConfig
{
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