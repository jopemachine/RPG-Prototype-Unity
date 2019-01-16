﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 주의 : Monobehavior를 상속받지 않아야 함
 */

[System.Serializable]
public class Monster
{
    public int currentHP;

    public int ID;
    public string Name;
    public string Description;
    public int MaxHP;
    public int AttackValue;
    public int DefenceValue;
    public int ExperienceValue;
    public int Speed;
    public MonsterType Type;
    public GameObject MonsterModel;

    // spawn Area에 생성될 수 있는 최대 몬스터 갯수.
    // spawn Area에 따라 달라지므로, Spawn Manager에서 지정.
    public int MaxGenerateNumber;

    public void Damaged(int playerAtk)
    {
        int resultDamage;

        if (DefenceValue >= playerAtk)
        {
            resultDamage = 1;
        }
        else
        {
            resultDamage = playerAtk - DefenceValue;
        }

        currentHP -= resultDamage;

    }

    public Monster() { }

    public Monster(int _ID, string _Name, string _Description, int _MaxHP, int _AttackValue, int _DefenceValue, int _ExperienceValue, int _Speed, MonsterType _Type)
    {
        ID = _ID;
        Name = _Name;
        Description = _Description;
        MaxHP = _MaxHP;
        AttackValue = _AttackValue;
        DefenceValue = _DefenceValue;
        ExperienceValue = _ExperienceValue;
        Speed = _Speed;
        Type = _Type;
    }

    public Monster getCopy()
    {
        return (Monster)this.MemberwiseClone();
    }

}
