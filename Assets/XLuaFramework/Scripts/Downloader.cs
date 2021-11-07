using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 下载器 工具类
/// </summary>
public class Downloader : Singleton<Downloader>
{
    /// <summary>
    /// 根据模块的配置，下载对应的模块
    /// </summary>
    /// <param name="moduleConfig">模块的配置对象</param>
    public async Task Download(ModuleConfig moduleConfig)
    {
        //用来存放热更下来的资源的本地路径
        string updatePath = GetUpdatePath(moduleConfig.moduleName);

        //远程服务器上这个模块的AB资源配置文件的URL
        string configURL = GetServerUrl(moduleConfig, moduleConfig.moduleName.ToLower() + ".json");

        UnityWebRequest request = UnityWebRequest.Get(configURL);
        request.downloadHandler = new DownloadHandlerFile(string.Format("{0}/{1}_temp.json", updatePath, moduleConfig.moduleName.ToLower()));
        Debug.Log("从远程地址：" + configURL + " 下载到本地路径: " + updatePath);

        await request.SendWebRequest();

        if (!string.IsNullOrEmpty(request.error))
        {
            Debug.LogWarning($"下载模块{moduleConfig.moduleName}的AB配置文件：{request.error}");

            bool result = await ShowMessageBox("网络异常，请检查网络后重试", "重试", "退出游戏");
            if (result == false)
            {
                Application.Quit();
                return;
            }
            await Download(moduleConfig);
            return;
        }

        Tuple<List<BundleInfo>, BundleInfo[]> tuple = await GetDownloadList(moduleConfig.moduleName);
        List<BundleInfo> downloadList = tuple.Item1;
        BundleInfo[] removeList = tuple.Item2;

        long downloadSize = CalculateSize(downloadList);
        if (downloadSize == 0)
        {
            Debug.Log("无需更新！");
            Clear(moduleConfig, removeList);
            return;
        }

        bool boxResult = await ShowMessageBox($"发现新版本，版本号为:{moduleConfig.moduleVersion}\n需要下载热更包，大小为：{SizeToString(downloadSize)}", "开始下载", "退出游戏");
        if (!boxResult)
        {
            Application.Quit();
            return;
        }

        await ExcuteDownload(moduleConfig, downloadList);

        Clear(moduleConfig, removeList);

        return;
    }

    /// <summary>
    /// 模块热更新完成后的善后工作
    /// </summary>
    /// <param name="moduleConfig"></param>
    /// <param name="removeList"></param>
    private void Clear(ModuleConfig moduleConfig, BundleInfo[] removeList)
    {
        string moduleName = moduleConfig.moduleName;
        string updatePath = GetUpdatePath(moduleName);

        //删除不需要的本地bundle文件
        for (int i = removeList.Length - 1; i >= 0; i--)
        {
            BundleInfo bundleInfo = removeList[i];
            string filePath = string.Format("{0}/{1}", updatePath, bundleInfo.bundleName);
            Debug.LogWarning("删除了文件！" + filePath);
            File.Delete(filePath);
        }

        //删除旧的配置文件
        string oldFile = string.Format("{0}/{1}.json", updatePath, moduleName.ToLower());
        if (File.Exists(oldFile))
            File.Delete(oldFile);

        //用新的配置文件替代之
        string newFile = string.Format("{0}/{1}_temp.json", updatePath, moduleName.ToLower());
        File.Move(newFile, oldFile);
    }

    /// <summary>
    /// 计算需要下载的资源大小 单位是字节
    /// </summary>
    /// <param name="downloadList"></param>
    /// <returns></returns>
    private long CalculateSize(List<BundleInfo> downloadList)
    {
        long totalSize = 0;
        foreach (BundleInfo bundleInfo in downloadList)
        {
            totalSize += bundleInfo.size;
        }
        return totalSize;
    }

    /// <summary>
    /// 弹出对话框方法
    /// </summary>
    /// <param name="messageInfo"></param>
    /// <param name="first"></param>
    /// <param name="second"></param>
    /// <returns></returns>
    private async Task<bool> ShowMessageBox(string messageInfo, string first, string second)
    {
        MessageBox messageBox = new MessageBox(messageInfo, first, second);

        MessageBox.BoxResult result = await messageBox.GetReplyAsync();
        messageBox.Close();

        if (result == MessageBox.BoxResult.First)
            return true;
        else
            return false;
    }

    /// <summary>
    /// 工具函数 把字节数转换为字符串形式
    /// </summary>
    /// <param name="size"></param>
    /// <returns></returns>
    private static string SizeToString(long size)
    {
        string sizeStr = "";

        if (size >= 1024 * 1024)
        {
            long m = size / (1024 * 1024);
            size = size % (1024 * 1024);
            sizeStr += $"{m}[M]";
        }
        if (size >= 1024)
        {
            long k = size / 1024;
            size = size % 1024;
            sizeStr += $"{k}[K]";
        }
        long b = size;
        sizeStr += $"{b}[B]";

        return sizeStr;
    }

    /// <summary>
    /// 执行下载行为
    /// </summary>
    /// <param name="moduleConfig"></param>
    /// <param name="downloadList"></param>
    /// <returns></returns>
    private async Task ExcuteDownload(ModuleConfig moduleConfig, List<BundleInfo> downloadList)
    {
        while (downloadList.Count > 0)
        {
            BundleInfo bundleInfo = downloadList[downloadList.Count - 1];

            UnityWebRequest request = UnityWebRequest.Get(GetServerUrl(moduleConfig, bundleInfo.bundleName));
            string updatePath = GetUpdatePath(moduleConfig.moduleName);
            string downloadPath = string.Format("{0}/{1}", updatePath, bundleInfo.bundleName);
            request.downloadHandler = new DownloadHandlerFile(downloadPath);

            await request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("下载资源：" + downloadPath + "成功！");
                downloadList.RemoveAt(downloadList.Count - 1);
            }
            else
            {
                break;
            }
        }

        if (downloadList.Count > 0)
        {
            bool result = await ShowMessageBox("网络异常，请检查网络后点击 继续下载", "继续下载", "退出游戏");
            if (!result)
            {
                Application.Quit();
                return;
            }
            await ExcuteDownload(moduleConfig, downloadList);
            return;
        }
    }

    /// <summary>
    /// 对于给定模块，返回其所有需要下载的BundleInfo组成的List
    /// </summary>
    /// <param name="moduleName"></param>
    /// <returns></returns>
    private async Task<Tuple<List<BundleInfo>, BundleInfo[]>> GetDownloadList(string moduleName)
    {
        ModuleABConfig serverConfig = await AssetLoader.Instance.LoadAssetBundleConfig(PathType.Update, moduleName, moduleName.ToLower() + "_temp.json");
        if (serverConfig == null)
            return null;

        ModuleABConfig localConfig = await AssetLoader.Instance.LoadAssetBundleConfig(PathType.Update, moduleName, moduleName.ToLower() + ".json");
        //注意：这里不判断localConfig是否存在，localConfig确实可能不存在，比如在此模块第一次热更之前，本地update路径下没有文件

        return CalculateDiff(moduleName, localConfig, serverConfig);
    }

    /// <summary>
    /// 通过两个AB资源配置文件，对比出有差异的Bundle
    /// </summary>
    /// <param name="moduleName"></param>
    /// <param name="localConfig"></param>
    /// <param name="serverConfig"></param>
    /// <returns></returns>
    private Tuple<List<BundleInfo>, BundleInfo[]> CalculateDiff(string moduleName, ModuleABConfig localConfig, ModuleABConfig serverConfig)
    {
        List<BundleInfo> downloadList = new List<BundleInfo>();

        //记录需要删除的本地bundle文件列表
        Dictionary<string, BundleInfo> localBundleDict = new Dictionary<string, BundleInfo>();
        if (localConfig != null)
        {
            foreach (BundleInfo bundleInfo in localConfig.bundleDict.Values)
            {
                string uniqueId = string.Format("{0}|{1}", bundleInfo.bundleName, bundleInfo.crc);
                localBundleDict.Add(uniqueId, bundleInfo);
            }
        }

        //1.找到那些有差异的bundle文件，放到bundleList容器中
        //2.对于那些遗留在本地的无用的bundle文件，把它过滤在localBundleDict容器里
        foreach (BundleInfo bundleInfo in serverConfig.bundleDict.Values)
        {
            string uniqueId = string.Format("{0}|{1}", bundleInfo.bundleName, bundleInfo.crc);
            if (localBundleDict.ContainsKey(uniqueId))
            {
                localBundleDict.Remove(uniqueId);
            }
            else
            {
                downloadList.Add(bundleInfo);
            }
        }

        //对于那些遗留在本地的无用的bundle文件，要清除，不然本地文件越积累越多
        BundleInfo[] removeList = localBundleDict.Values.ToArray();

        return new Tuple<List<BundleInfo>, BundleInfo[]>(downloadList, removeList);
    }

    /// <summary>
    /// 客户端给指定模块的热更资源存放地址
    /// </summary>
    /// <param name="moduleName"></param>
    /// <returns></returns>
    private string GetUpdatePath(string moduleName)
    {
        return Application.persistentDataPath + "/Bundles/" + moduleName;
    }

    /// <summary>
    /// 文件在服务器端的完整URL
    /// </summary>
    /// <param name="moduleConfig"></param>
    /// <param name="fileName"></param>
    /// <returns></returns>
    public string GetServerUrl(ModuleConfig moduleConfig, string fileName)
    {
#if UNITY_ANDROID
        return string.Format("{0}/{1}/{2}", moduleConfig.DownloadURL, "Android", fileName);
#elif UNITY_IOS
        return string.Format("{0}/{1}/{2}", moduleConfig.DownloadURL, "iOS", fileName);
#elif UNITY_STANDALONE_WIN
        return string.Format("{0}/{1}/{2}", moduleConfig.DownloadURL, "StandaloneWindows64", fileName);
#endif
    }
}
