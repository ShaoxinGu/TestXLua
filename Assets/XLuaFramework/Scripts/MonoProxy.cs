using System;
using UnityEngine;
using XLua;

/// <summary>
/// MonoBehavior的代理类
/// </summary>
public class MonoProxy : MonoBehaviour
{
    /// <summary>
    /// 这个MonoProxy对象所绑定的lua脚本对象
    /// </summary>
    public LuaTable luaTable;

    private Action<LuaTable> luaStart;

    private Action<LuaTable> luaOnDestroy;

    /// <summary>
    /// 绑定对应的脚本
    /// </summary>
    /// <param name="moduleName"></param>
    /// <param name="scriptPath"></param>
    /// <returns></returns>
    public LuaTable BindScript(string moduleName, string scriptPath)
    {
        GameLauncher.Instance.luaEnv.DoString("require '" + scriptPath + "'");
        luaTable = GameLauncher.Instance.luaEnv.Global.Get<LuaTable>(scriptPath);

        //给这个luaTable对象添加一个字段指向这个c#的MonoProxy脚本对象
        luaTable.Set("MonoProxy", this);

        //补一个Awake方法调用
        Action<LuaTable> luaAwake = luaTable.Get<Action<LuaTable>>("Awake");
        luaAwake?.Invoke(luaTable);

        //获取lua脚本的成员方法
        luaTable.Get("Start", out luaStart);

        //获取lua脚本的成员方法
        luaTable.Get("OnDestroy", out luaOnDestroy);

        return luaTable;
    }

    void Start()
    {
        luaStart?.Invoke(luaTable);
    }

    void OnDestroy()
    {
        luaOnDestroy?.Invoke(luaTable);
    }
}