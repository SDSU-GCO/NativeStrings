using System;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using System.Diagnostics;
using Unity.Collections;

namespace Unity.Collections
{
    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    [DebuggerDisplay("Length = {Length}")]
    [DebuggerTypeProxy(typeof(NativeStringDebugView))]
    public struct NativeString : IDisposable
    {

        #region custom extensions
        /// <summary>
        /// This function takes a copy of native list A and appends/concatanates B to the end of it.
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <returns></returns>
        public NativeString Concatenate(NativeString A, NativeString B)
        {
            return A + B;
        }

        /// <summary>
        /// This function takes a copy of native list A and appends/concatanates B to the end of it.
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <returns></returns>
        public static NativeString operator +(NativeString A, NativeString B)
        {
            RemoveNullTermination(ref A);
            EnforceNullTermination(ref B);

            int pos = A.Length;
            A.ResizeUninitialized(A.Length + B.Length);

            for (int i = 0; i < B.Length; i++)
            {
                A[pos + i] = B[i];
            }

            return A;
        }

        public static bool operator ==(NativeString A, NativeString B)
        {
            bool equal = true;

            if (A.Length == B.Length)
            {
                for (int i = 0; i < B.Length && equal == true; i++)
                {
                    if (A[i] != B[i])
                    {
                        equal = false;
                    }
                }
            }
            else
            {
                equal = false;
            }

            return equal;
        }

        public static bool operator !=(NativeString A, NativeString B)
        {
            return !(A == B);
        }

        public override bool Equals(object obj)
        {
            return Equals(this, obj);
        }

        public override int GetHashCode()
        {
            return GetHashCode(this);
        }

        public static int GetHashCode(NativeString A)
        {
            int[] charArray = A.ToArray();
            string stringToHash = charArray.ToString();
            int hash = stringToHash.GetHashCode();
            return hash;
        }

        public static bool Equals(NativeString A, NativeString B)
        {
            return (A == B);
        }


        public static bool IsNullTerminated(NativeString A)
        {
            return A[A.Length - 1] == '\0' ? true:false;
        }

        public static bool Replace(ref NativeString A, NativeString oldString, NativeString replacement)
        {
            EnforceNullTermination(ref A);
            RemoveNullTermination(ref oldString);
            RemoveNullTermination(ref replacement);

            int pos = Contains(A, oldString);

            if (pos != -1)
            {
                ReplaceAt(ref A, pos, oldString, replacement, false);
            }

            return pos != -1;
        }

        public static bool ReplaceAll(ref NativeString A, NativeString oldString, NativeString replacement)
        {
            EnforceNullTermination(ref A);
            RemoveNullTermination(ref oldString);
            RemoveNullTermination(ref replacement);

            int pos = Contains(A, oldString);

            if (pos != -1)
            {
                ReplaceAt(ref A, pos, oldString, replacement, false);
                ReplaceAfter(ref A, pos, oldString, replacement);
            }

            return pos != -1;
        }

        public static bool ReplaceBefore(ref NativeString A, int position, NativeString oldString, NativeString replacement)
        {
            EnforceNullTermination(ref A);
            RemoveNullTermination(ref oldString);
            RemoveNullTermination(ref replacement);

            int pos = ContainsBefore(A, position, oldString);

            if (pos != -1)
            {
                ReplaceAt(ref A, pos, oldString, replacement, false);
                ReplaceBefore(ref A, pos, oldString, replacement);
            }

            return pos != -1;
        }

        public static bool ReplaceAfter(ref NativeString A, int positon, NativeString oldString, NativeString replacement)
        {
            EnforceNullTermination(ref A);
            RemoveNullTermination(ref oldString);
            RemoveNullTermination(ref replacement);

            int pos = ContainsAfter(A, positon, oldString);

            if (pos != -1)
            {
                ReplaceAt(ref A, pos, oldString, replacement, false);
                ReplaceAfter(ref A, pos, oldString, replacement);
            }

            return pos != -1;
        }

        public static bool ReplaceAt(ref NativeString A, int positon, NativeString oldString, NativeString replacement, bool checkForOldString = true)
        {
            EnforceNullTermination(ref A);
            RemoveNullTermination(ref oldString);
            RemoveNullTermination(ref replacement);

            int pos;
            if (checkForOldString == true)
            {
                pos = ContainsAt(A, positon, oldString);
            }
            else
            {
                pos = positon;
            }

            if (pos != -1)
            {
                NativeString temp = new NativeString(A.Length - (pos + oldString.Length), Allocator.Temp);
                temp.ResizeUninitialized(A.Length - (pos + oldString.Length));
                int tempPos = pos + oldString.Length;
                for (int i = 0; i < temp.Length; i++)
                {
                    temp[i] = A[tempPos];

                    tempPos++;
                }
                
                OverWrite(ref A, pos, replacement);
                OverWrite(ref A, pos + replacement.Length, temp);
                EnforceNullTermination(ref A);
            }

            return pos != -1;
        }

        public static bool Insert(ref NativeString A, int position, NativeString insertion)
        {
            EnforceNullTermination(ref A);
            RemoveNullTermination(ref insertion);
            bool success = true;

            if (position>=0 && position<A.Length)
            {
                
                NativeString temp = new NativeString(A.Length - position, Allocator.Temp);
                temp.ResizeUninitialized(A.Length - position);
                for (int i = 0; i < temp.Length; i++)
                {
                    temp[i] = A[position+i];
                }

                OverWrite(ref A, position, insertion);
                OverWrite(ref A, position + insertion.Length, temp);
                EnforceNullTermination(ref A);
            }
            else
            {
                success = false;
            }

            return success;
        }

        public static void OverWrite(ref NativeString A, int position, NativeString stamp)
        {
            if((position+stamp.Length)>A.Length)
            {
                A.ResizeUninitialized(position + stamp.Length);
            }

            for(int i = 0; i < stamp.Length; i++)
            {
                A[position + i] = stamp[i];
            }
        }

        public static explicit operator NativeString(string A)
        {
            return new NativeString(A.Length, Allocator.Persistent);
        }

        public static implicit operator string(NativeString A)
        {
            System.Text.StringBuilder str = new System.Text.StringBuilder(A.Length);
            for (int i = 0; i < A.Length; i++)
            {
                str.Append((char)A[i]);
            }
            return str.ToString();
        }

        public static void EnforceNullTermination(ref NativeString A)
        {
            if (!A[A.Length - 1].Equals('\0'))
            {
                A.Add('\0');
            }

            return;
        }

        public static void RemoveNullTermination(ref NativeString A)
        {
            if (A[A.Length - 1].Equals('\0'))
            {
                A.ResizeUninitialized(A.Length - 1);
            }

            return;
        }

        public static int Contains(NativeString A, NativeString subString)
        {
            int pos = -1;
            for (int i = 0; i < A.Length && pos == -1; i++)
            {
                pos = ContainsAt(A, i, subString);
            }

            return pos;
        }

        public static int ContainsAt(NativeString A, int position, NativeString subString)
        {
            int pos = -1;
            if (!((subString.Length + position) > A.Length))
            {
                for (int j = 0; j < subString.Length && A[position + j].Equals(subString[j]); j++)
                {
                    if (j == (subString.Length - 1))
                    {
                        pos = position;
                    }
                }
            }

            return pos;
        }

        public static int ContainsAfter(NativeString A, int position, NativeString subString)
        {
            int pos = -1;
            for (int i = position + 1; i < A.Length && pos == -1; i++)
            {
                pos = ContainsAt(A, i, subString);
            }

            return pos;
        }

        public static int ContainsAtOrAfter(NativeString A, int position, NativeString subString)
        {
            int pos = -1;
            for (int i = position; i < A.Length && pos == -1; i++)
            {
                pos = ContainsAt(A, i, subString);
            }

            return pos;
        }

        public static int ContainsBefore(NativeString A, int position, NativeString subString)
        {
            int pos = -1;
            for (int i = 0; i < A.Length && i < position && pos == -1; i++)
            {
                pos = ContainsAt(A, i, subString);
            }

            return pos;
        }

        public static int ContainsAtORBefore(NativeString A, int position, NativeString subString)
        {
            int pos = -1;
            for (int i = 0; i < A.Length && i <= position && pos == -1; i++)
            {
                pos = ContainsAt(A, i, subString);
            }

            return pos;
        }
        #endregion


#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal NativeStringImpl<int, DefaultMemoryManager, NativeBufferSentinel> m_Impl;
        internal AtomicSafetyHandle m_Safety;
#else
	    internal NativeStringImpl<T, DefaultMemoryManager> m_Impl;
#endif

        public unsafe NativeString(Allocator i_label) : this(1, i_label, 2) { }
        public unsafe NativeString(int capacity, Allocator i_label) : this(capacity, i_label, 2) { }

        unsafe NativeString(int capacity, Allocator i_label, int stackDepth)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
#if UNITY_2018_3_OR_NEWER
	        var guardian = new NativeBufferSentinel(stackDepth, i_label);
	        m_Safety = (i_label == Allocator.Temp) ? AtomicSafetyHandle.GetTempMemoryHandle() : AtomicSafetyHandle.Create();
#else
            var guardian = new NativeBufferSentinel(stackDepth);
            m_Safety = AtomicSafetyHandle.Create();
#endif
            m_Impl = new NativeStringImpl<int, DefaultMemoryManager, NativeBufferSentinel>(capacity, i_label, guardian);
#else
            m_Impl = new NativeStringImpl<T, DefaultMemoryManager>(capacity, i_label);
#endif
        }

        public int this[int index]
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                return m_Impl[index];

            }
            set
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
                m_Impl[index] = value;

            }
        }

        public int Length
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                return m_Impl.Length;
            }
        }

        public int Capacity
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                return m_Impl.Capacity;
            }

            set
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
                m_Impl.Capacity = value;
            }
        }

        public void Add(int element)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
            m_Impl.Add(element);
        }

        public void AddRange(NativeArray<int> elements)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif

            m_Impl.AddRange(elements);
        }

        public void RemoveAtSwapBack(int index)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);

            if (index < 0 || index >= Length)
                throw new ArgumentOutOfRangeException(index.ToString());
#endif
            m_Impl.RemoveAtSwapBack(index);
        }

        public bool IsCreated => !m_Impl.IsNull;

        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckDeallocateAndThrow(m_Safety);
#if UNITY_2018_3_OR_NEWER
		    if (AtomicSafetyHandle.IsTempMemoryHandle(m_Safety))
		        m_Safety = AtomicSafetyHandle.Create();
#endif
            AtomicSafetyHandle.Release(m_Safety);
#endif
            m_Impl.Dispose();
        }

        public void Clear()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif

            m_Impl.Clear();
        }

        public static implicit operator NativeArray<int>(NativeString nativeString)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckGetSecondaryDataPointerAndThrow(nativeString.m_Safety);
            var arraySafety = nativeString.m_Safety;
            AtomicSafetyHandle.UseSecondaryVersion(ref arraySafety);
#endif

            var array = nativeString.m_Impl.ToNativeArray();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, arraySafety);
#endif
            return array;
        }

        public unsafe NativeArray<int> ToDeferredJobArray()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
#endif

            byte* buffer = (byte*)m_Impl.GetStringData();
            // We use the first bit of the pointer to infer that the array is in string mode
            // Thus the job scheduling code will need to patch it.
            buffer += 1;
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<int>(buffer, 0, Allocator.Invalid);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, m_Safety);
#endif

            return array;
        }


        public int[] ToArray()
        {
            NativeArray<int> nativeArray = this;
            return nativeArray.ToArray();
        }

        public void CopyFrom(int[] array)
        {
            //@TODO: Thats not right... This doesn't perform a resize
            Capacity = array.Length;
            NativeArray<int> nativeArray = this;
            nativeArray.CopyFrom(array);
        }

        public void ResizeUninitialized(int length)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
            m_Impl.ResizeUninitialized(length);
        }
    }


    sealed class NativeStringDebugView
    {
        NativeString m_Array;

        public NativeStringDebugView(NativeString array)
        {
            m_Array = array;
        }

        public int[] Items => m_Array.ToArray();
    }
}
namespace Unity.Collections.LowLevel.Unsafe
{
    public static class NativeStringUnsafeUtility
    {
        public static unsafe void* GetUnsafePtr(this NativeString nativeString)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(nativeString.m_Safety);
#endif
            var data = nativeString.m_Impl.GetStringData();
            return data->buffer;
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        public static AtomicSafetyHandle GetAtomicSafetyHandle(ref NativeString nativeString) 
        {
            return nativeString.m_Safety;
        }
#endif

        public static unsafe void* GetInternalStringDataPtrUnchecked(ref NativeString nativeString)
        {
            return nativeString.m_Impl.GetStringData();
        }
    }
}
