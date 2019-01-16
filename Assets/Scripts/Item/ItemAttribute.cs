﻿using UnityEngine;
using System.Collections;

/*
 아이템이 갖고 있는 hp, mp 회복량, 스탯 변동, 특수 효과등을
 attribute로 관리함
*/

[System.Serializable]
public class ItemAttribute
{

    public string AttributeName;
    public int AttributeValue;

    public ItemAttribute(string _AttributeName, int _AttributeValue)
    {
        AttributeName = _AttributeName;
        AttributeValue = _AttributeValue;
    }


}

