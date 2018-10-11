using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using Unity.Collections;
using System.Linq;
using Unity;

namespace Unity.Collections
{

    public class NativeStringTester : MonoBehaviour
    {

        private void Start()
        {
            NativeString t1 = new NativeString(0, Allocator.Persistent);
            t1.Set("Potato");

            NativeString t2 = new NativeString(0, Allocator.Persistent);
            t2.Set(" World Potato");
            t1 = t1 + t2;
            t1 = t1 + t2;
            t1 = t1 + t2;
            t1 = t1 + t2;
            t1 = t1 + t2;
            t1 = t1 + t2;
            
            t1.ReplaceAll("Potato", "Hello");
            t1 += " World!";

            
            t2.Dispose();

            Debug.Log(t1.ToString());
            t1.Dispose();
        }
    }

    public static class NativeStringExtensions
    {

    }
}