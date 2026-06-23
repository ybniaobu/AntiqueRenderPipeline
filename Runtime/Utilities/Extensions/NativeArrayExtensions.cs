using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace YPipeline
{
    internal static class NativeArrayExtensions
    {
        /// <remarks>
        /// Make sure you do not write to the value! There are no checks for this!
        /// </remarks>
        public static unsafe ref T UnsafeElementAt<T>(this NativeArray<T> array, int index) where T : struct
        {
            return ref UnsafeUtility.ArrayElementAsRef<T>(array.GetUnsafeReadOnlyPtr(), index);
        }
        
        public static unsafe ref T UnsafeElementAtMutable<T>(this NativeArray<T> array, int index) where T : struct
        {
            return ref UnsafeUtility.ArrayElementAsRef<T>(array.GetUnsafePtr(), index);
        }
    }
}