using System;

public class Downloader : Singleton<Downloader>
{
    /// <summary>
    /// 根据模块的配置，下载对应的模块
    /// </summary>
    /// <param name="moduleConfig">模块的配置对象</param>
    /// <param name="action">下载完成后的回调函数</param>
    public void Download(ModuleConfig moduleConfig, Action<bool> action)
    {

    }
}
