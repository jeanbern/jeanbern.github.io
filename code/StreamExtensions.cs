using System;
#if DEBUG
using System.Diagnostics;
#endif
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace JBP
{
    /*  This class is a snippet from https://github.com/jeanbern/jeanbern.github.io/master/code/StreamExtensions.cs
     *  Copyright (c) 2019 Jean-Bernard Pellerin
     *  MIT License
     *  https://github.com/jeanbern/jeanbern.github.io/master/LICENSE
     */
    public static class StreamExtensions
    {
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false, true);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Write<T>(this Stream stream, T value)
            where T : unmanaged
        {
            var pointer = Unsafe.AsPointer(ref value);
            var span = new Span<byte>(pointer, sizeof(T));
            stream.Write(span);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // ReSharper disable once UnusedMember.Global
        public static unsafe void Write<T>(this Stream stream, in T value)
            where T : unmanaged
        {
            var results = Unsafe.AsPointer(ref Unsafe.AsRef(value));
            var span = new Span<byte>(results, sizeof(T));
            stream.Write(span);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Write<T>(this Stream stream, T[] values)
            where T : unmanaged
        {
            var byteLength = sizeof(T) * values.Length;
            stream.Write(byteLength);
            fixed (void* pointer = values)
            {
                var span = new Span<byte>(pointer, byteLength);
                stream.Write(span);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // ReSharper disable once UnusedMember.Global
        public static unsafe void Write<T>(this Stream stream, Span<T> values)
            where T : unmanaged
        {
            var byteLength = sizeof(T) * values.Length;
            stream.Write(byteLength);
            fixed (void* pointer = values)
            {
                var span = new Span<byte>(pointer, byteLength);
                stream.Write(span);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // ReSharper disable once UnusedMember.Global
        public static unsafe void Write(this Stream stream, string value)
        {
            var encoding = Utf8NoBom;
            var valueSpan = value.AsSpan();
            var len = encoding.GetByteCount(valueSpan);

            var stack = stackalloc byte[len];
            var byteSpan = new Span<byte>(stack, len);

            var encodedLen = encoding.GetBytes(valueSpan, byteSpan);
#if DEBUG
            Debug.Assert(encodedLen == len);
#endif

            stream.Write(encodedLen);
            stream.Write(byteSpan);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Write(this Stream stream, char[] value)
        {
            var encoding = Utf8NoBom;
            var valueSpan = new Span<char>(value);
            var charLength = encoding.GetByteCount(valueSpan);
#if DEBUG
            Debug.Assert(charLength >= value.Length);
            Debug.Assert(charLength <= value.Length * 2);
#endif
            var stack = stackalloc byte[charLength];
            var byteSpan = new Span<byte>(stack, charLength);
            var byteLength = encoding.GetBytes(valueSpan, byteSpan);
#if DEBUG
            Debug.Assert(byteLength == charLength);
#endif
            stream.Write(byteLength);
            stream.Write(value.Length);
            stream.Write(byteSpan);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // ReSharper disable once UnusedMember.Global
        public static unsafe ref T Read<T>(this Stream stream, ref T result)
            where T : unmanaged
        {
            var thing = Unsafe.AsPointer(ref result);
            var bytes = new Span<byte>(thing, sizeof(T));
#if DEBUG
            var bytesRead = 
#endif
            stream.Read(bytes);
#if DEBUG
            Debug.Assert(bytesRead == sizeof(T));
#endif
            return ref result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe T Read<T>(this Stream stream)
            where T : unmanaged
        {
            var result = default(T);
            var pointer = Unsafe.AsPointer(ref result);
            var span = new Span<byte>(pointer, sizeof(T));
#if DEBUG
            var bytesRead = 
#endif
            stream.Read(span);
#if DEBUG
            Debug.Assert(bytesRead == sizeof(T));
#endif
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe char[] ReadCharArray(this Stream stream)
        {
            var byteLength = stream.Read<int>();
            var charLength = stream.Read<int>();
            var bytes = stackalloc byte[byteLength];
            var byteSpan = new Span<byte>(bytes, byteLength);
#if DEBUG
            var bytesRead = 
#endif
            stream.Read(byteSpan);
#if DEBUG
            Debug.Assert(bytesRead == byteLength);
#endif
            var chars = new char[charLength];
            var charSpan = new Span<char>(chars);
#if DEBUG
            var charCount = 
#endif
            Utf8NoBom.GetChars(byteSpan, charSpan);
#if DEBUG
            Debug.Assert(charCount == charLength);
#endif

            return chars;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // ReSharper disable once UnusedMember.Global
        public static unsafe string ReadString(this Stream stream)
        {
            var byteLength = stream.Read<int>();
            var bytes = stackalloc byte[byteLength];
            var byteSpan = new Span<byte>(bytes, byteLength);
#if DEBUG 
                var bytesRead = 
#endif 
            stream.Read(byteSpan);
#if DEBUG
            Debug.Assert(bytesRead == byteLength);
#endif
            return Utf8NoBom.GetString(byteSpan);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe T[] ReadArray<T>(this Stream stream)
            where T : unmanaged
        {
            if (typeof(T) == typeof(char))
            {
                Throw(new InvalidCastException("ReadArray<char>() should be replaced with a call to ReadCharArray()."));
            }
            var byteLength = stream.Read<int>();
            if (byteLength % sizeof(T) != 0)
            {
                Throw(new InvalidDataException($"sizeof({typeof(T).Name}) = {sizeof(T).ToString()} does not evenly divide the stream length: {byteLength.ToString()}."));
            }

            var length = byteLength / sizeof(T);
#pragma warning disable U2U1023 // Do not overwrite initialized variables
            var results = new T[length];
#pragma warning restore U2U1023 // Do not overwrite initialized variables

            fixed (void* pointer = results)
            {
                var span = new Span<byte>(pointer, byteLength);
#if DEBUG 
                var bytesRead = 
#endif 
                stream.Read(span);
#if DEBUG
                Debug.Assert(bytesRead == byteLength);
#endif
            }

            return results;
        }

        //https://reubenbond.github.io/posts/dotnet-perf-tuning Section: Use static throw helpers
        private static void Throw(Exception e)
        {
            throw e;
        }
    }
}