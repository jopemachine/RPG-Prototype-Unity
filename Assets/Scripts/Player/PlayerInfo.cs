﻿using UnityEngine;
using UnityEngine.UI;

using System.Collections;
using LitJson;
using System.IO;

public class PlayerInfo : MonoBehaviour
{
    static public PlayerInfo mInstance;

    public Player player;

    private void Awake()
    {

        if (mInstance != null)
        {
            Destroy(this.gameObject);
        }
        else
        {
            DontDestroyOnLoad(this.gameObject);
            mInstance = this;
        }

        player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();

        StartCoroutine("LoadCoroutine");
    }

    #region Data Parsing And Load

    IEnumerator LoadCoroutine()
    {
        string JsonString_item = File.ReadAllText(Application.dataPath + "/Custom/Resources/PlayerInfoData.json");

        JsonData playerInfoData = JsonMapper.ToObject(JsonString_item);

        Debug.Assert(playerInfoData != null, "playerInfoData read fail");

        ParsingJsonPlayerInfo(playerInfoData);

        yield return null;
    }

    private void ParsingJsonPlayerInfo(JsonData playerInfoData)
    {
        player.Name = (playerInfoData[0]["Name"]).ToString();
        player.currentHP = (int)(playerInfoData[0]["currentHP"]);
        player.currentMP = (int)(playerInfoData[0]["currentMP"]);
        player.Level = (int)(playerInfoData[0]["Level"]);
        player.Money = (int)(playerInfoData[0]["Money"]);
        player.ExperienceValue = (int)(playerInfoData[0]["Experience"]);
    }

    #endregion


    #region Data Load and Save
    public void LoadData()
    {

    }

    public void SaveData()
    {
        JsonData playerInfo = new JsonData();
        playerInfo["Name"] = player.Name;
        playerInfo["Money"] = player.Money;
        playerInfo["currentHP"] = player.currentHP;
        playerInfo["currentMP"] = player.currentMP;
        playerInfo["Level"] = player.Level;
        playerInfo["Experience"] = player.ExperienceValue;

        File.WriteAllText(Application.dataPath + "/Custom/Resources/PlayerInfoData.json", "[" + playerInfo.ToJson() + "]");

        // Debug.Log(playerInfo.ToJson());
    }
    #endregion



}

