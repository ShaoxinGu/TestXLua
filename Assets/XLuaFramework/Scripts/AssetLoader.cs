using System;

public class AssetLoader : Singleton<AssetLoader>
{
    /// <summary>
    /// 加载模块对应的全局AssetBundle资源管理文件
    /// </summary>
    /// <param name="moduelName">模块的名字</param>
    /// <param name="action">加载完成后的回调函数</param>
    public void LoadAssetBundleConfig(string moduelName, Action<bool> action)
    {

    }
}
