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
            NativeList<char> test = new NativeList<char>(0, Allocator.Persistent);
            Debug.Log("Capacity: " + test.Capacity + "\nLength: " + test.Length);
            test.Add('a');
            Debug.Log("Capacity: " + test.Capacity + "\nLength: " + test.Length);
            test.Add('b');
            Debug.Log("Capacity: " + test.Capacity + "\nLength: " + test.Length);
            test.Add('c');
            Debug.Log("Capacity: " + test.Capacity + "\nLength: " + test.Length);
            test.Dispose();

            NativeString t2 = new NativeString(0, Allocator.TempJob);
            t2.Add('1');
            Debug.Log("Capacity: " + t2.Capacity + "\nLength: " + t2.Length);
            t2.Add('1');
            Debug.Log("Capacity: " + t2.Capacity + "\nLength: " + t2.Length);
            t2.Add('1');
            Debug.Log("Capacity: " + t2.Capacity + "\nLength: " + t2.Length);
            t2.Add('1');
            Debug.Log("Capacity: " + t2.Capacity + "\nLength: " + t2.Length);
            t2.Add('1');
            Debug.Log("Capacity: " + t2.Capacity + "\nLength: " + t2.Length);
            t2.ResizeUninitialized(1);
            Debug.Log("Capacity: " + t2.Capacity + "\nLength: " + t2.Length);
            t2.GetHashCode();
            t2.Dispose();
        }
    }

    public static class NativeStringExtensions
    {

    }
}