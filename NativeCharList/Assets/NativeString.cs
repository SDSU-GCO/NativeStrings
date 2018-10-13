using System;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using System.Diagnostics;
using Unity.Collections;

namespace Unity.Collections
{
    public struct BlittableChar
    {
        public int C;

        public BlittableChar(char c)
        {
            C = c;
        }

        public static implicit operator char(BlittableChar c)
        {
            return (char)c.C;
        }

        public static implicit operator BlittableChar(char c)
        {
            return new BlittableChar(c);
        }
    }


    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    [DebuggerDisplay("Length = {Length}")]
    [DebuggerTypeProxy(typeof(NativeStringDebugView))]
    public struct NativeString : IDisposable
    {

        #region custom extensions
        /// <summary>
        /// Assign to this to perform a deep copy of the string. Especially useful once + overload works.
        /// </summary>
        public NativeString Overwrite
        {
            set
            {
                Clear();
                Append(value);
            }
        }

        /// <summary>
        /// This function takes a copy of native list A and appends B to the end of it.
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <returns></returns>
        public unsafe void Append(NativeString A)
        {
            int start = Length;
            ResizeUninitialized(Length + A.Length);
            if (NativeStringUnsafeUtility.GetInternalStringDataPtrUnchecked(ref this) == NativeStringUnsafeUtility.GetInternalStringDataPtrUnchecked(ref A))
            {
                //It's the same stupid string. We are just repeating it.
                UnsafeUtility.MemMove(GetUnsafeOffset(start), UnsafePtr, Length * sizeof(BlittableChar));
            }
            else
            {
                UnsafeUtility.MemCpy(GetUnsafeOffset(start), A.UnsafePtrReadOnly, A.Length * sizeof(BlittableChar));
            }
        }

        /// <summary>
        /// This function takes a copy of native list A and appends/concatanates B to the end of it.
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <returns></returns>
        /*public static NativeString operator +(NativeString A, NativeString B)
        {
            //Overloading this operator isn't feasible until Unity makes Allocator.Temp auto-dispose.
            var temp = new NativeString(A.Length + B.Length, Allocator.Temp);
            temp.Append(A);
            temp.Append(B);
            return temp;
        }*/

        public unsafe static bool operator ==(NativeString A, NativeString B)
        {
            if (A.Length != B.Length)
                return false;
            return UnsafeUtility.MemCmp(A.UnsafePtrReadOnly, B.UnsafePtrReadOnly, A.Length) == 0;
        }

        public static bool operator !=(NativeString A, NativeString B)
        {
            return !(A == B);
        }

        public override string ToString()
        {
            System.Text.StringBuilder str = new System.Text.StringBuilder(Length);
            for (int i = 0; i < Length; i++)
            {
                str.Append((char)this[i]);
            }
            return str.ToString();
        }

        /// <summary>
        /// A writeable unsafe pointer for UnsafeUtility operations.
        /// </summary>
        private unsafe void* UnsafePtr
        {
            get
            {
                return NativeStringUnsafeUtility.GetUnsafePtr(this);
            }
        }

        /// <summary>
        /// A readable unsafe pointer for UnsafeUtility operations. Useful for copy source and comparisons.
        /// </summary>
        private unsafe void* UnsafePtrReadOnly
        {
            get
            {
                return NativeStringUnsafeUtility.GetUnsafePtrReadOnly(this);
            }
        }

        /// <summary>
        /// Returns the writable pointer to the character at index. Use for writing at a given location.
        /// </summary>
        /// <param name="index">The index of the character to point to</param>
        /// <returns>A writable pointer to the character located at index</returns>
        private unsafe void* GetUnsafeOffset(int index)
        {
            return (byte*)UnsafePtr + index * sizeof(BlittableChar);
        }

        /// <summary>
        /// Returns a readonly pointer to the character at index. Use as a copy source or substring comparison.
        /// </summary>
        /// <param name="offset">The index of the character to point to</param>
        /// <returns>A readonly pointer to the character located at index</returns>
        private unsafe void* GetUnsafeOffsetReadOnly(int offset)
        {
            return (byte*)UnsafePtrReadOnly + offset * sizeof(BlittableChar);
        }

        /// <summary>
        /// "Resizes" the middle of the string. Automatically moves the contents after the specified range appropriately. Useful for Replace algorithms. Added characters are unitialized. If the range is shrunk, the contents of the range may be overwritten.
        /// </summary>
        /// <param name="start">The character to start resizing at.</param>
        /// <param name="count">The number of characters in the range to be resized. Includes start.</param>
        /// <param name="newSize">The number of characters the range should be after the resize. Includes start.</param>
        private unsafe void ResizeRange(int start, int count, int newSize)
        {
            int len = Length;
            if (newSize > count)
                ResizeUninitialized(len + newSize - count);

            UnsafeUtility.MemMove(GetUnsafeOffset(start + newSize), GetUnsafeOffset(start + count), (len - (start + count)) * sizeof(BlittableChar));

            if (newSize < count)
                ResizeUninitialized(len + newSize - count);
        }

        /// <summary>
        /// Inserts an uninitialized gap in the string. Useful for Insert algorithms. Automatically moves the contents at position to the end of the string appropriately.
        /// </summary>
        /// <param name="pos">The index in which you want to insert before.</param>
        /// <param name="count">The number of characters you want to insert before.</param>
        private unsafe void InsertUnitialized(int pos, int count)
        {
            int len = Length;
            ResizeUninitialized(len + count);
            UnsafeUtility.MemMove(GetUnsafeOffset(pos + count), GetUnsafeOffset(pos), (len - pos) * sizeof(BlittableChar));
        }
        #endregion


#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal NativeStringImpl<BlittableChar, DefaultMemoryManager, NativeBufferSentinel> m_Impl;
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
            m_Impl = new NativeStringImpl<BlittableChar, DefaultMemoryManager, NativeBufferSentinel>(capacity, i_label, guardian);
#else
            m_Impl = new NativeStringImpl<T, DefaultMemoryManager>(capacity, i_label);
#endif
        }

        public BlittableChar this[int index]
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

        public void Append(BlittableChar element)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif
            m_Impl.Add(element);
        }

        public void AddRange(NativeArray<BlittableChar> elements)
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

        public static implicit operator NativeArray<BlittableChar>(NativeString nativeString)
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

        public unsafe NativeArray<BlittableChar> ToDeferredJobArray()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckExistsAndThrow(m_Safety);
#endif

            byte* buffer = (byte*)m_Impl.GetStringData();
            // We use the first bit of the pointer to infer that the array is in string mode
            // Thus the job scheduling code will need to patch it.
            buffer += 1;
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<BlittableChar>(buffer, 0, Allocator.Invalid);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, m_Safety);
#endif

            return array;
        }


        public BlittableChar[] ToArray()
        {
            NativeArray<BlittableChar> nativeArray = this;
            return nativeArray.ToArray();
        }

        public void CopyFrom(BlittableChar[] array)
        {
            //@TODO: Thats not right... This doesn't perform a resize
            Capacity = array.Length;
            NativeArray<BlittableChar> nativeArray = this;
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

        public BlittableChar[] Items => m_Array.ToArray();
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


        internal static unsafe void* GetUnsafePtrReadOnly(this NativeString nativeString)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(nativeString.m_Safety);
#endif
            var data = nativeString.m_Impl.GetStringData();
            return data->buffer;
        }
    }
}
