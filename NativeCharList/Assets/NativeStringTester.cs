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
            NativeString t1 = new NativeString("Hello", Allocator.Persistent);
            t1.Append(' ');

            NativeString t2 = new NativeString(Allocator.Persistent);
            t2.Append("World");
            //t1 = t1 + t2;
            t1.Append(t2);
            t2.Dispose();
            t1.Append('!');

            Debug.Log(t1);
            t1.Dispose();


            NativeString t3 = new NativeString(Allocator.Persistent);
            t3.Append("The number of girlfriends I have had in my entire life is ");
            t3.Append(0);
            t3.Append(new BlittableChar('\n'));
            t3.Append("The chance of that changing in the near is ");
            t3.Append(0.0f);
            t3.Append("%.\n");
            t3.Append("You can make the above sentence ");
            t3.Append(false);
            t3.Append("!\n");
            Debug.Log(t3);
            t3.Dispose();
        }
    }

    public static class NativeStringExtensions
    {

    }
}