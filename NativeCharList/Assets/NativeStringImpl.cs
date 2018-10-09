using System;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Collections
{

    /// <summary>
    /// What is this : struct that contains the data for a native string, that gets allocated using native memory allocation.
    /// Motivation(s): Need a single container struct to hold a native strings collection data.
    /// </summary>
	unsafe struct NativeStringData
    {
        public void* buffer;
        public int length;
        public int capacity;
    }

    /// <summary>
    /// What is this : internal implementation of a variable size string, using native memory (not GC'd).
    /// Motivation(s): just need a resizable string that does not trigger the GC, for performance reasons.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
#if ENABLE_UNITY_COLLECTIONS_CHECKS
    public unsafe struct NativeStringImpl<T, TMemManager, TSentinel>
        where TSentinel : struct, INativeBufferSentinel
#else
	public unsafe struct NativeStringImpl<T, TMemManager>
#endif
        where T : struct
        where TMemManager : struct, INativeBufferMemoryManager
    {
        public TMemManager m_MemoryAllocator;

        [NativeDisableUnsafePtrRestriction]
        NativeStringData* m_StringData;

        internal NativeStringData* GetStringData()
        {
            return m_StringData;
        }

        public void* RawBuffer => m_StringData;

        public TMemManager Allocator => m_MemoryAllocator;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal TSentinel sentinel;
#endif

        public T this[int index]
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if ((uint)index >= (uint)m_StringData->length)
                    throw new IndexOutOfRangeException($"Index {index} is out of range in NativeString of '{m_StringData->length}' Length.");
#endif

                return UnsafeUtility.ReadArrayElement<T>(m_StringData->buffer, index);
            }

            set
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if ((uint)index >= (uint)m_StringData->length)
                    throw new IndexOutOfRangeException($"Index {index} is out of range in NativeString of '{m_StringData->length}' Length.");
#endif

                UnsafeUtility.WriteArrayElement(m_StringData->buffer, index, value);
            }
        }

        public int Length
        {
            get
            {
                return m_StringData->length;
            }
        }

        public int Capacity
        {
            get
            {
                if (m_StringData == null)
                    throw new NullReferenceException();
                return m_StringData->capacity;
            }

            set
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                if (value < m_StringData->length)
                    throw new ArgumentException("Capacity must be larger than the length of the NativeString.");
#endif

                if (m_StringData->capacity == value)
                    return;

                void* newData = UnsafeUtility.Malloc(value * UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), m_MemoryAllocator.Label);
                UnsafeUtility.MemCpy(newData, m_StringData->buffer, m_StringData->length * UnsafeUtility.SizeOf<T>());
                UnsafeUtility.Free(m_StringData->buffer, m_MemoryAllocator.Label);
                m_StringData->buffer = newData;
                m_StringData->capacity = value;
            }
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        public NativeStringImpl(int capacity, Allocator allocatorLabel, TSentinel sentinel)
#else
		public NativeStringImpl(int capacity, Allocator allocatorLabel)
#endif
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            this.sentinel = sentinel;
            m_StringData = null;

            if (!UnsafeUtility.IsBlittable<T>())
            {
                this.sentinel.Dispose();
                throw new ArgumentException(string.Format("{0} used in NativeString<{0}> must be blittable", typeof(T)));
            }
#endif
            m_MemoryAllocator = default(TMemManager);
            m_StringData = (NativeStringData*)m_MemoryAllocator.Init(UnsafeUtility.SizeOf<NativeStringData>(), UnsafeUtility.AlignOf<NativeStringData>(), allocatorLabel);

            var elementSize = UnsafeUtility.SizeOf<T>();

            //@TODO: Find out why this is needed?
            capacity = Math.Max(1, capacity);
            m_StringData->buffer = UnsafeUtility.Malloc(capacity * elementSize, UnsafeUtility.AlignOf<T>(), allocatorLabel);

            m_StringData->length = 0;
            m_StringData->capacity = capacity;
        }

        public void Add(T element)
        {
            if (m_StringData->length >= m_StringData->capacity)
                Capacity = m_StringData->length + m_StringData->capacity * 2;

            this[m_StringData->length++] = element;
        }

        //@TODO: Test for AddRange
        public void AddRange(NativeArray<T> elements)
        {
            if (m_StringData->length + elements.Length > m_StringData->capacity)
                Capacity = m_StringData->length + elements.Length * 2;

            var sizeOf = UnsafeUtility.SizeOf<T>();
            UnsafeUtility.MemCpy((byte*)m_StringData->buffer + m_StringData->length * sizeOf, elements.GetUnsafePtr(), sizeOf * elements.Length);

            m_StringData->length += elements.Length;
        }

        public void RemoveAtSwapBack(int index)
        {
            var newLength = m_StringData->length - 1;
            this[index] = this[newLength];
            m_StringData->length = newLength;
        }

        public bool IsNull => m_StringData == null;

        public void Dispose()
        {
            if (m_StringData != null)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                sentinel.Dispose();
#endif

                UnsafeUtility.Free(m_StringData->buffer, m_MemoryAllocator.Label);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                m_StringData->buffer = (void*)0xDEADF00D;
#endif
                m_MemoryAllocator.Dispose(m_StringData);
                m_StringData = null;
            }
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            else
                throw new Exception("NativeString has yet to be allocated or has been dealocated!");
#endif
        }

        public void Clear()
        {
            ResizeUninitialized(0);
        }

        /// <summary>
        /// Does NOT allocate memory, but shares it.
        /// </summary>
		public NativeArray<T> ToNativeArray()
        {
            return NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(m_StringData->buffer, m_StringData->length, Collections.Allocator.Invalid);
        }

        public void ResizeUninitialized(int length)
        {
            Capacity = Math.Max(length, Capacity);
            m_StringData->length = length;
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        public NativeStringImpl<T, TMemManager, TSentinel> Clone()
        {
            var clone = new NativeStringImpl<T, TMemManager, TSentinel>(Capacity, m_MemoryAllocator.Label, sentinel);
            UnsafeUtility.MemCpy(clone.m_StringData->buffer, m_StringData->buffer, m_StringData->length * UnsafeUtility.SizeOf<T>());
            clone.m_StringData->length = m_StringData->length;

            return clone;
        }
#else
	    public NativeStringImpl<T, TMemManager> Clone()
	    {
	        var clone = new NativeStringImpl<T, TMemManager>(Capacity, m_MemoryAllocator.Label);
	        UnsafeUtility.MemCpy(clone.m_StringData->buffer, m_StringData->buffer, m_StringData->length * UnsafeUtility.SizeOf<T>());
	        clone.m_StringData->length = m_StringData->length;

	        return clone;
	    }
#endif

        public NativeArray<T> CopyToNativeArray(Allocator label)
        {
            var buffer = UnsafeUtility.Malloc(UnsafeUtility.SizeOf<T>(), UnsafeUtility.AlignOf<T>(), label);
            UnsafeUtility.MemCpy(buffer, m_StringData->buffer, Length * UnsafeUtility.SizeOf<T>());
            var copy = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(buffer, Length, label);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref copy, AtomicSafetyHandle.Create());
#endif
            return copy;
        }
    }

}

