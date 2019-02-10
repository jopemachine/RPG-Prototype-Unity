﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class Status: MonoBehaviour
{
    public int currentHP;
    public int currentMP;
    public float stamina;

    public int FatalBlowValue;
    public int FatalBlowProb;
    public int AttackValue;
    public int DefenceValue;

    // 방어력 속성들을 이용해 최종적인 데미지를 계산
    public void CalculateDamage(Damage AtkValue)
    {
        int resultDamage;

        if (DefenceValue >= AtkValue.value)
        {
            resultDamage = 1;
        }
        else
        {
            resultDamage = AtkValue.value - DefenceValue;
        }

        if (currentHP - resultDamage >= 0)
        {
            currentHP -= resultDamage;
        }
        else
        {
            currentHP = 0;
        }

        DamageIndicator.mInstance.CallFloatingText(AtkValue);

        AtkValue.attacker.SetBool("DamagedProcessed", true);

    }

    // 공격 데미지 공식
    public Damage Attack()
    {
        bool isFatalBlow;

        float minDamage = AttackValue;
        float maxDamage = 1.5f * AttackValue;

        float damage = UnityEngine.Random.Range(minDamage, maxDamage);

        if (isFatalBlow = DecideFatalBlow())
        {
            damage *= (FatalBlowValue / 100);
        }

        return new Damage((int)(Mathf.Floor(damage)), isFatalBlow);
    }

    // 이번 공격이 치명타인지 결정
    public bool DecideFatalBlow()
    {
        int prob = UnityEngine.Random.Range(0, 100);

        if (prob > FatalBlowProb)
        {
            return false;
        }
        else
        {
            return true;
        }

    }

}

