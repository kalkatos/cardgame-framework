using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CardgameCore;

public class TestScript : MonoBehaviour
{
    void Start()
    {
        Debug.Log(StringUtility.PrintStringArray(StringUtility.ArgumentsBreakdown("Flip,Grid(2,3),Click", 0)));
        Debug.Log(StringUtility.PrintStringArray(StringUtility.ArgumentsBreakdown("2,3", 0)));
    }
}
