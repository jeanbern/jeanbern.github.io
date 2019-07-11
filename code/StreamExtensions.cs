using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace JBP
{
    /*  This class is a snippet from https://github.com/jeanbern/jeanbern.github.io/blob/master/code/StreamExtensions.cs
     *  Copyright © 2019 Jean-Bernard Pellerin
     *  MIT License
     *  https://github.com/jeanbern/jeanbern.github.io/blob/master/LICENSE
     */
    public static class StreamExtensions
    {
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false, true);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write<T>(this Stream stream, T value)
            where T : unmanaged
        {
            var tSpan = MemoryMarshal.CreateSpan(ref value, 1);
            var span = MemoryMarshal.AsBytes(tSpan);
            stream.Write(span);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write<T>(this Stream stream, ref T value)
            where T : unmanaged
        {
            var tSpan = MemoryMarshal.CreateSpan(ref value, 1);
            var span = MemoryMarshal.AsBytes(tSpan);
            stream.Write(span);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write<T>(this Stream stream, T[] values)
            where T : unmanaged
        {
            var tSpan = values.AsSpan();
            var span = MemoryMarshal.AsBytes(tSpan);
            stream.Write(values.Length);
            stream.Write(span);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write<T>(this Stream stream, Span<T> values)
            where T : unmanaged
        {
            var span = MemoryMarshal.AsBytes(values);
            stream.Write(values.Length);
            stream.Write(span);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this Stream stream, string value)
        {
            var encoding = Utf8NoBom;
            var valueSpan = value.AsSpan();
            var len = encoding.GetByteCount(valueSpan);

            Span<byte> byteSpan = stackalloc byte[len];
            var encodedLen = encoding.GetBytes(valueSpan, byteSpan);

            stream.Write(encodedLen);
            stream.Write(byteSpan);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(this Stream stream, char[] value)
        {
            var encoding = Utf8NoBom;
            var valueSpan = value.AsSpan();
            var encodedLength = encoding.GetByteCount(valueSpan);

            Span<byte> byteSpan = stackalloc byte[encodedLength];
            var byteLength = encoding.GetBytes(valueSpan, byteSpan);

            stream.Write(byteLength);
            stream.Write(value.Length);
            stream.Write(byteSpan);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T Read<T>(this Stream stream, ref T result)
            where T : unmanaged
        {
            var tSpan = MemoryMarshal.CreateSpan(ref result, 1);
            var span = MemoryMarshal.AsBytes(tSpan);
            stream.Read(span);
            return ref result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Read<T>(this Stream stream)
            where T : unmanaged
        {
            var result = default(T);
            var tSpan = MemoryMarshal.CreateSpan(ref result, 1);
            var span = MemoryMarshal.AsBytes(tSpan);
            stream.Read(span);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char[] ReadCharArray(this Stream stream)
        {
            var byteLength = stream.Read<int>();
            var charLength = stream.Read<int>();
            
            Span<byte> span = stackalloc byte[byteLength];
            stream.Read(span);

            var results = new char[charLength];
            var charSpan = results.AsSpan();
            Utf8NoBom.GetChars(span, charSpan);

            return results;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ReadString(this Stream stream)
        {
            var byteLength = stream.Read<int>();
            Span<byte> bytes = stackalloc byte[byteLength];
            stream.Read(bytes);
            return Utf8NoBom.GetString(bytes);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] ReadArray<T>(this Stream stream)
            where T : unmanaged
        {
            if (typeof(T) == typeof(char))
            {
                Throw(new InvalidCastException("ReadArray<char>() should be replaced with a call to ReadCharArray()."));
            }

            var length = stream.Read<int>();
#pragma warning disable U2U1023 // Do not overwrite initialized variables
            var results = new T[length];
#pragma warning restore U2U1023 // Do not overwrite initialized variables

            var tSpan = results.AsSpan();
            var span = MemoryMarshal.AsBytes(tSpan);
            stream.Read(span);

            return results;
        }

        //https://reubenbond.github.io/posts/dotnet-perf-tuning Section: Use static throw helpers
        private static void Throw(Exception e)
        {
            throw e;
        }
    }
}
