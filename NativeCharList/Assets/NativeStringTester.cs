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
            NativeString t1 = (NativeString)"Hello";
            t1.Add(' ');

            NativeString t2 = (NativeString)"World";
            t1 = t1 + t2;
            t2.Dispose();
            t1.Add('!');

            Debug.Log(t1);
            t1.Dispose();
        }
    }

    public static class NativeStringExtensions
    {

    }
}