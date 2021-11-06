using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class MessageBox
{
    public MessageBox(string messageInfo, string firstText, string secondText)
    {
        Object asset = Resources.Load("Prefabs/MessageBox");
        gameObject = Object.Instantiate(asset) as GameObject;
        gameObject.transform.Find("Bg/MessageBox/MessageInfo").GetComponent<Text>().text = messageInfo;

        Transform first = gameObject.transform.Find("Bg/MessageBox/First");
        first.Find("Text").GetComponent<Text>().text = firstText;
        first.GetComponent<Button>().onClick.AddListener(() =>
        {
            Result = BoxResult.First;
        });

        Transform second = gameObject.transform.Find("Bg/MessageBox/Second");
        second.Find("Text").GetComponent<Text>().text = secondText;
        second.GetComponent<Button>().onClick.AddListener(() =>
        {
            Result = BoxResult.Second;
        });
    }

    public async Task<BoxResult> GetReplyAsync()
    {
        return await Task.Run<BoxResult>(() =>
        {
            while (true)
            {
                if (Result != BoxResult.None)
                {
                    return Result;
                }
            }
        });
    }

    public void Close()
    {
        GameObject.Destroy(gameObject);
    }

    GameObject gameObject;
    BoxResult Result { get; set; }

    public enum BoxResult
    {
        None,
        First,
        Second
    }
}