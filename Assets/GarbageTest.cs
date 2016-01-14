using UnityEngine;
using System.Collections;
using VildNinja.Utils;
using System.Collections.Generic;

public class GarbageTest : MonoBehaviour {
    
    private List<HashList<int>> list;

    // Use this for initialization
    void Start ()
    {
        list = new List<HashList<int>>();
        for (int i = 0; i < 1000; i++)
        {
            var hash = new HashList<int>(15000);
            for (int j = 0; j < 10000; j++)
            {
                hash.Add(Random.Range(int.MinValue, int.MaxValue));
            }
        }

        var set = new HashList<int>();
        Debug.Log("Add 1 " + set.Add(1));
        Debug.Log("Add 1 " + set.Add(1));
        set.Add(30000);
        set.Add(444);
        Debug.Log("Add 5555 " + set.Add(5555));
        set.Add(500000);
        set.Add(364892679);

        set.Remove(444);
        Debug.Log("Set contains 444 = " + set.Contains(444));
        Debug.Log("Size = " + set.Count);

        foreach (var i in set)
        {
            Debug.Log("Set contains " + i + " = " + set.Contains(i));
        }


        Debug.Log("Set contains 11 = " + set.Contains(11));
        Debug.Log("Set contains 23 = " + set.Contains(23));
        Debug.Log("Set contains 16 = " + set.Contains(16));
        Debug.Log("Set contains 32 = " + set.Contains(32));
        Debug.Log("Set contains 33 = " + set.Contains(33));
    }
	
    void Update()
    {
        int sum = 0;

        for (int n = 0; n < 1000000; n++)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var hash = list[i];
                foreach (var number in hash)
                {
                    sum += number;
                }
            }
        }
    }
}
