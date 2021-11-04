using System;
using System.Threading.Tasks;

public class Downloader : Singleton<Downloader>
{
    /// <summary>
    /// 根据模块的配置，下载对应的模块
    /// </summary>
    /// <param name="moduleConfig">模块的配置对象</param>
    internal Task<bool> Download(ModuleConfig moduleConfig)
    {
        throw new NotImplementedException();
    }
}
