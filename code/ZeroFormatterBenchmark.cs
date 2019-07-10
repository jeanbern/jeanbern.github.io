using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using ZeroFormatter;

/*  Original Location: https://github.com/jeanbern/jeanbern.github.io/blob/master/code/StreamExtensions.cs
 *  Copyright © 2019 Jean-Bernard Pellerin
 *  MIT License
 *  https://github.com/jeanbern/jeanbern.github.io/blob/master/LICENSE
 */
public static class StreamExtensions
{
    private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false, true);

    public static unsafe void UnsafeWrite<T>(this Stream stream, T value)
        where T : unmanaged
    {
        var pointer = Unsafe.AsPointer(ref value);
        var span = new Span<byte>(pointer, sizeof(T));
        stream.Write(span);
    }
    
    public static unsafe void UnsafeWrite<T>(this Stream stream, T[] values)
        where T : unmanaged
    {
        stream.UnsafeWrite(values.Length);
        fixed (void* pointer = values)
        {
            var span = new Span<byte>(pointer, values.Length * sizeof(T));
            stream.Write(span);
        }
    }

    public static unsafe void UnsafeWrite(this Stream stream, string value)
    {
        var encoding = Utf8NoBom;
        var valueSpan = value.AsSpan();
        var len = encoding.GetByteCount(valueSpan);

        Span<byte> span = stackalloc byte[len];
        var encodedLen = encoding.GetBytes(valueSpan, span);

        stream.UnsafeWrite(encodedLen);
        stream.Write(span);
    }
    
    public static unsafe ref T UnsafeRead<T>(this Stream stream, ref T result)
        where T : unmanaged
    {
        var thing = Unsafe.AsPointer(ref result);
        var bytes = new Span<byte>(thing, sizeof(T));
        stream.Read(bytes);
        return ref result;
    }

    public static unsafe T UnsafeRead<T>(this Stream stream)
        where T : unmanaged
    {
        var result = default(T);
        var pointer = Unsafe.AsPointer(ref result);
        var span = new Span<byte>(pointer, sizeof(T));

        stream.Read(span);

        return result;
    }
    
    public static unsafe string UnsafeReadString(this Stream stream)
    {
        var byteLength = stream.UnsafeRead<int>();
        Span<byte> bytes = stackalloc byte[byteLength];
        stream.Read(bytes);
        return Utf8NoBom.GetString(bytes);
    }

    public static unsafe T[] UnsafeReadArray<T>(this Stream stream)
        where T : unmanaged
    {
        if (typeof(T) == typeof(char))
        {
            Throw(new InvalidCastException("ReadArray<char>() should be replaced with a call to ReadCharArray()."));
        }

        var length = stream.UnsafeRead<int>();
#pragma warning disable U2U1023 // Do not overwrite initialized variables
        var results = new T[length];
#pragma warning restore U2U1023 // Do not overwrite initialized variables

        fixed (void* pointer = results)
        {
            var span = new Span<byte>(pointer, length);
            stream.Read(span);
        }

        return results;
    }

    public static void Write<T>(this Stream stream, T value)
        where T : unmanaged
    {
        var tSpan = MemoryMarshal.CreateSpan(ref value, 1);
        var span = MemoryMarshal.AsBytes(tSpan);
        stream.Write(span);
    }
    
    public static void Write<T>(this Stream stream, T[] values)
        where T : unmanaged
    {
        var tSpan = MemoryMarshal.CreateSpan(ref values[0], values.Length);
        var span = MemoryMarshal.AsBytes(tSpan);
        stream.Write(values.Length);
        stream.Write(span);
    }

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
    
    public static ref T Read<T>(this Stream stream, ref T result)
        where T : unmanaged
    {
        var tSpan = MemoryMarshal.CreateSpan(ref result, 1);
        var span = MemoryMarshal.AsBytes(tSpan);
        stream.Read(span);
        return ref result;
    }

    public static T Read<T>(this Stream stream)
        where T : unmanaged
    {
        var result = default(T);
        var tSpan = MemoryMarshal.CreateSpan(ref result, 1);
        var span = MemoryMarshal.AsBytes(tSpan);
        stream.Read(span);
        return result;
    }
    
    public static string ReadString(this Stream stream)
    {
        var byteLength = stream.Read<int>();
        Span<byte> bytes = stackalloc byte[byteLength];
        stream.Read(bytes);
        return Utf8NoBom.GetString(bytes);
    }

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
        
        var tSpan = MemoryMarshal.CreateSpan(ref results[0], results.Length);
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

/*  Code below based on https://github.com/neuecc/ZeroFormatter/blob/master/sandbox/PerformanceComparison/Program.cs
 *  Copyright (c) 2016 Yoshifumi Kawai
 *  MIT License
 *  https://github.com/neuecc/ZeroFormatter/blob/master/LICENSE
 */

[ZeroFormattable]
public class Person : IEquatable<Person>
{
    private int _age;
    private Sex _sex;

    [Index(0)]
    public virtual int Age
    {
        get => _age;
        set => _age = value;
    }

    [Index(1)]
    public virtual string FirstName { get; set; }

    [Index(2)]
    public virtual string LastName { get; set; }

    [Index(3)]
    public virtual Sex Sex
    {
        get => _sex;
        set => _sex = value;
    }
    public bool Equals(Person other)
    {
        return Age == other.Age && FirstName == other.FirstName && LastName == other.LastName && Sex == other.Sex;
    }

    public void UnsafeSerialize(Stream stream)
    {
        stream.UnsafeWrite(Age);
        stream.UnsafeWrite(FirstName);
        stream.UnsafeWrite(LastName);
        stream.UnsafeWrite(Sex);
    }

    public static Person UnsafeDeserialize(Stream stream)
    {
        var person = new Person();
        stream.UnsafeRead(ref person._age);
        person.FirstName = stream.UnsafeReadString();
        person.LastName = stream.UnsafeReadString();
        stream.UnsafeRead(ref person._sex);
        return person;
    }

    public void Serialize(Stream stream)
    {
        stream.Write(Age);
        stream.Write(FirstName);
        stream.Write(LastName);
        stream.Write(Sex);
    }

    public static Person Deserialize(Stream stream)
    {
        var person = new Person();
        stream.Read(ref person._age);
        person.FirstName = stream.ReadString();
        person.LastName = stream.ReadString();
        stream.Read(ref person._sex);
        return person;
    }
}

public enum Sex : sbyte
{
    Unknown, Male, Female,
}

[ZeroFormattable]
public struct Vector3 : IEquatable<Vector3>
{
    [Index(0)]
    public float X;

    [Index(1)]
    public float Y;

    [Index(2)]
    public float Z;
    public Vector3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public override bool Equals(object obj)
    {
        return obj is Vector3 other && Equals(other);
    }

    public bool Equals(Vector3 other)
    {
        return X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
    }


    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = X.GetHashCode();
            hashCode = (hashCode * 397) ^ Y.GetHashCode();
            hashCode = (hashCode * 397) ^ Z.GetHashCode();
            return hashCode;
        }
    }
}

internal static class SerializationTesting
{
    private const int WarmUpIteration = 100;
    private const int RunIteration = 100000;

    private static int _iteration = 1;
    private static bool _dryRun = true;

    public static void TestCases()
    {
        var p = new Person {Age = 99999, FirstName = "Windows", LastName = "Server", Sex = Sex.Male};

        var l = Enumerable.Range(1000, 1000).Select(x => new Person { Age = x, FirstName = "Windows", LastName = "Server", Sex = Sex.Female }).ToArray();
        const int integer = 1;
        var v3 = new Vector3 { X = 12345.12345f, Y = 3994.35226f, Z = 325125.52426f };
        var v3List = Enumerable.Range(1, 100).Select(_ => new Vector3 { X = 12345.12345f, Y = 3994.35226f, Z = 325125.52426f }).ToArray();
        var largeString = File.ReadAllText("CSharpHtml.txt");
        
        _iteration = WarmUpIteration;
        SerializeZeroFormatter(p);
        Serialize(p);
        UnsafeSerialize(p);
        SerializeZeroFormatter(l);
        Serialize(l);
        UnsafeSerialize(l);
        SerializeZeroFormatter(integer);
        SerializeCustomUnmanaged(integer);
        UnsafeSerializeCustomUnmanaged(integer);
        SerializeZeroFormatter(v3);
        SerializeCustomUnmanaged(v3);
        UnsafeSerializeCustomUnmanaged(v3);
        SerializeZeroFormatter(largeString);
        Serialize(largeString);
        UnsafeSerialize(largeString);
        SerializeZeroFormatter(v3List);
        SerializeCustomList(v3List);
        UnsafeSerializeCustomList(v3List);

        _dryRun = false;
        _iteration = RunIteration;
        Console.WriteLine("Person");
        var a1 = SerializeZeroFormatter(p);
        var b1 = Serialize(p);
        var c1 = UnsafeSerialize(p);
        Console.WriteLine("Person[]");
        var a2 = SerializeZeroFormatter(l);
        var b2 = Serialize(l);
        var c2 = UnsafeSerialize(l);
        Validate("ZeroFormatter", p, l, a1, a2);
        Validate("CoolFormatter", p, l, b1, b2);
        Validate("UnsafeFormatter", p, l, c1, c2);
        
        Console.WriteLine("int");
        var a3 = SerializeZeroFormatter(integer);
        var b3 = SerializeCustomUnmanaged(integer);
        var c3 = UnsafeSerializeCustomUnmanaged(integer);
        Console.WriteLine("Vector3");
        var a4 = SerializeZeroFormatter(v3);
        var b4 = SerializeCustomUnmanaged(v3);
        var c4 = UnsafeSerializeCustomUnmanaged(v3);
        Console.WriteLine("string");
        var a5 = SerializeZeroFormatter(largeString);
        var b5 = Serialize(largeString);
        var c5 = UnsafeSerialize(largeString);
        Console.WriteLine("Vector3[]");
        var a6 = SerializeZeroFormatter(v3List);
        var b6 = SerializeCustomList(v3List);
        var c6 = UnsafeSerializeCustomList(v3List);

        Validate2("ZeroFormatter", integer, a3);
        Validate2("CoolFormatter", integer, b3);
        Validate2("UnsafeFormatter", integer, c3);
        Validate2("ZeroFormatter", v3, a4);
        Validate2("CoolFormatter", v3, b4);
        Validate2("UnsafeFormatter", v3, c4);
        Validate2("ZeroFormatter", largeString, a5);
        Validate2("CoolFormatter", largeString, b5);
        Validate2("UnsafeFormatter", largeString, c5);
        Validate2("ZeroFormatter", v3List, a6);
        Validate2("CoolFormatter", v3List, b6);
        Validate2("UnsafeFormatter", v3List, c6);
    }

    private static void Validate(string label, Person original, IList<Person> originalList, Person copy, IList<Person> copyList)
    {
        if (!EqualityComparer<Person>.Default.Equals(original, copy))
        {
            Console.WriteLine(label + " Invalid Deserialize Small Object");
        }

        if (!originalList.SequenceEqual(copyList))
        {
            Console.WriteLine(label + " Invalid Deserialize Large Array");
        }
    }

    private static void Validate2<T>(string label, T original, T copy)
    {
        if (!EqualityComparer<T>.Default.Equals(original, copy))
        {
            Console.WriteLine(label + " Invalid Deserialize");
        }
    }

    private static void Validate2<T>(string label, T[] original, T[] copy)
    {
        if (!original.SequenceEqual(copy))
        {
            Console.WriteLine(label + " Invalid Deserialize");
        }
    }

    private static T SerializeZeroFormatter<T>(T original)
    {
        Console.WriteLine("ZeroFormatter");

        var copy = default(T);
        var bytes = Array.Empty<byte>();

        using (new Measure("Serialize"))
        {
            for (var i = 0; i < _iteration; i++)
            {
                bytes = ZeroFormatterSerializer.Serialize(original);
            }
        }

        using (new Measure("Deserialize"))
        {
            for (var i = 0; i < _iteration; i++)
            {
                copy = ZeroFormatterSerializer.Deserialize<T>(bytes);
            }
        }

        if (!_dryRun)
        {
            Console.WriteLine($"{"Binary Size",15}   {ToHumanReadableSize(bytes.Length)}");
        }

        return copy;
    }

    private static string Serialize(string original)
    {
        Console.WriteLine("CoolFormatter");

        string copy = default;
        long length;

        using (var stream = new MemoryStream())
        {
            using (new Measure("Serialize"))
            {
                for (var i = 0; i < _iteration; i++)
                {
                    stream.Position = 0;
                    stream.Write(original);
                }
            }

            length = stream.Position;
            using (new Measure("Deserialize"))
            {
                for (var i = 0; i < _iteration; i++)
                {
                    stream.Position = 0;
                    copy = stream.ReadString();
                }
            }
        }

        if (!_dryRun)
        {
            Console.WriteLine($"{"Binary Size",15}   {ToHumanReadableSize(length)}");
        }

        return copy;
    }

    private static Person Serialize(Person original)
    {
        Console.WriteLine("CoolFormatter");
        Person copy = default;
        long length;

        using (var stream = new MemoryStream())
        {
            using (new Measure("Serialize"))
            {
                for (var i = 0; i < _iteration; i++)
                {
                    stream.Position = 0;
                    original.Serialize(stream);
                }
            }

            length = stream.Position;
            using (new Measure("Deserialize"))
            {
                for (var i = 0; i < _iteration; i++)
                {
                    stream.Position = 0;
                    copy = Person.Deserialize(stream);
                }
            }
        }

        if (!_dryRun)
        {
            Console.WriteLine($"{"Binary Size",15}   {ToHumanReadableSize(length)}");
        }

        return copy;
    }

    private static IList<Person> Serialize(IList<Person> original)
    {
        Console.WriteLine("CoolFormatter");
        long length;

        Person[] copy = default;
        using (var stream = new MemoryStream())
        {
            using (new Measure("Serialize"))
            {
                var count = original.Count;
                for (var i = 0; i < _iteration; i++)
                {
                    stream.Position = 0;
                    stream.Write(count);
                    for (var j = 0; j < count; j++)
                    {
                        original[j].Serialize(stream);
                    }
                }
            }

            length = stream.Position;
            using (new Measure("Deserialize"))
            {
                for (var i = 0; i < _iteration; i++)
                {
                    stream.Position = 0;
                    var count = stream.Read<int>();
                    copy = new Person[count];
                    for (var j = 0; j < count; j++)
                    {
                        copy[j]  = Person.Deserialize(stream);
                    }
                }
            }
        }

        if (!_dryRun)
        {
            Console.WriteLine($"{"Binary Size",15}   {ToHumanReadableSize(length)}");
        }

        return copy;
    }
    private static T[] SerializeCustomList<T>(T[] original)
        where T : unmanaged
    {
        Console.WriteLine("CoolFormatter");
        long length;

        T[] copy = default;
        using (var stream = new MemoryStream())
        {
            using (new Measure("Serialize"))
            {
                for (var i = 0; i < _iteration; i++)
                {
                    stream.Position = 0;
                    stream.Write(original);
                }
            }

            length = stream.Position;
            using (new Measure("Deserialize"))
            {
                for (var i = 0; i < _iteration; i++)
                {
                    stream.Position = 0;
                    copy = stream.ReadArray<T>();
                }
            }
        }

        if (!_dryRun)
        {
            Console.WriteLine($"{"Binary Size",15}   {ToHumanReadableSize(length)}");
        }

        return copy;
    }

    private static T SerializeCustomUnmanaged<T>(T original)
        where T : unmanaged
    {
        Console.WriteLine("CoolFormatter");

        var copy = default(T);
        long length;

        using (var stream = new MemoryStream())
        {
            using (new Measure("Serialize"))
            {
                for (var i = 1; i < _iteration; i++)
                {
                    stream.Position = 0;
                    stream.Write(original);
                }
            }

            length = stream.Position;
            using (new Measure("Deserialize"))
            {
                for (var i = 0; i < _iteration; i++)
                {
                    stream.Position = 0;
                    stream.Read(ref copy);
                }
            }
        }

        if (!_dryRun)
        {
            Console.WriteLine($"{"Binary Size",15}   {ToHumanReadableSize(length)}");
        }

        return copy;
    }

    
    private static string UnsafeSerialize(string original)
    {
        Console.WriteLine("UnsafeFormatter");

        string copy = default;
        long length;

        using (var stream = new MemoryStream())
        {
            using (new Measure("Serialize"))
            {
                for (var i = 0; i < _iteration; i++)
                {
                    stream.Position = 0;
                    stream.UnsafeWrite(original);
                }
            }

            length = stream.Position;
            using (new Measure("Deserialize"))
            {
                for (var i = 0; i < _iteration; i++)
                {
                    stream.Position = 0;
                    copy = stream.UnsafeReadString();
                }
            }
        }

        if (!_dryRun)
        {
            Console.WriteLine($"{"Binary Size",15}   {ToHumanReadableSize(length)}");
        }

        return copy;
    }

    private static Person UnsafeSerialize(Person original)
    {
        Console.WriteLine("UnsafeFormatter");
        Person copy = default;
        long length;

        using (var stream = new MemoryStream())
        {
            using (new Measure("Serialize"))
            {
                for (var i = 0; i < _iteration; i++)
                {
                    stream.Position = 0;
                    original.UnsafeSerialize(stream);
                }
            }

            length = stream.Position;
            using (new Measure("Deserialize"))
            {
                for (var i = 0; i < _iteration; i++)
                {
                    stream.Position = 0;
                    copy = Person.UnsafeDeserialize(stream);
                }
            }
        }

        if (!_dryRun)
        {
            Console.WriteLine($"{"Binary Size",15}   {ToHumanReadableSize(length)}");
        }

        return copy;
    }

    private static IList<Person> UnsafeSerialize(IList<Person> original)
    {
        Console.WriteLine("UnsafeFormatter");
        long length;

        Person[] copy = default;
        using (var stream = new MemoryStream())
        {
            using (new Measure("Serialize"))
            {
                var count = original.Count;
                for (var i = 0; i < _iteration; i++)
                {
                    stream.Position = 0;
                    stream.Write(count);
                    for (var j = 0; j < count; j++)
                    {
                        original[j].UnsafeSerialize(stream);
                    }
                }
            }

            length = stream.Position;
            using (new Measure("Deserialize"))
            {
                for (var i = 0; i < _iteration; i++)
                {
                    stream.Position = 0;
                    var count = stream.Read<int>();
                    copy = new Person[count];
                    for (var j = 0; j < count; j++)
                    {
                        copy[j]  = Person.UnsafeDeserialize(stream);
                    }
                }
            }
        }

        if (!_dryRun)
        {
            Console.WriteLine($"{"Binary Size",15}   {ToHumanReadableSize(length)}");
        }

        return copy;
    }
    private static T[] UnsafeSerializeCustomList<T>(T[] original)
        where T : unmanaged
    {
        Console.WriteLine("UnsafeFormatter");
        long length;

        T[] copy = default;
        using (var stream = new MemoryStream())
        {
            using (new Measure("Serialize"))
            {
                for (var i = 0; i < _iteration; i++)
                {
                    stream.Position = 0;
                    stream.UnsafeWrite(original);
                }
            }

            length = stream.Position;
            using (new Measure("Deserialize"))
            {
                for (var i = 0; i < _iteration; i++)
                {
                    stream.Position = 0;
                    copy = stream.UnsafeReadArray<T>();
                }
            }
        }

        if (!_dryRun)
        {
            Console.WriteLine($"{"Binary Size",15}   {ToHumanReadableSize(length)}");
        }

        return copy;
    }

    private static T UnsafeSerializeCustomUnmanaged<T>(T original)
        where T : unmanaged
    {
        Console.WriteLine("UnsafeFormatter");

        var copy = default(T);
        long length;

        using (var stream = new MemoryStream())
        {
            using (new Measure("Serialize"))
            {
                for (var i = 1; i < _iteration; i++)
                {
                    stream.Position = 0;
                    stream.UnsafeWrite(original);
                }
            }

            length = stream.Position;
            using (new Measure("Deserialize"))
            {
                for (var i = 0; i < _iteration; i++)
                {
                    stream.Position = 0;
                    stream.UnsafeRead(ref copy);
                }
            }
        }

        if (!_dryRun)
        {
            Console.WriteLine($"{"Binary Size",15}   {ToHumanReadableSize(length)}");
        }

        return copy;
    }

    private readonly struct Measure : IDisposable
    {
        private readonly string _label;
        private readonly Stopwatch _s;

        public Measure(string label)
        {
            _label = label;
            GC.Collect(2, GCCollectionMode.Forced, true);
            _s = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _s.Stop();
            if (!_dryRun)
            {
                Console.WriteLine($"{ _label,15}   {_s.Elapsed.TotalMilliseconds.ToString(CultureInfo.InvariantCulture)} ms");
            }

            GC.Collect(2, GCCollectionMode.Forced, true);
        }
    }

    private static string ToHumanReadableSize(long size)
    {
        return ToHumanReadableSize(new long?(size));
    }

    private static string ToHumanReadableSize(long? size)
    {
        if (size == null)
        {
            return "NULL";
        }

        double bytes = size.Value;

        if (bytes <= 1024)
        {
            return bytes.ToString("f2") + " B";
        }

        bytes /= 1024;
        if (bytes <= 1024)
        {
            return bytes.ToString("f2") + " KB";
        }

        bytes /= 1024;
        if (bytes <= 1024)
        {
            return bytes.ToString("f2") + " MB";
        }

        bytes /= 1024;
        if (bytes <= 1024)
        {
            return bytes.ToString("f2") + " GB";
        }

        bytes /= 1024;
        if (bytes <= 1024)
        {
            return bytes.ToString("f2") + " TB";
        }

        bytes /= 1024;
        if (bytes <= 1024)
        {
            return bytes.ToString("f2") + " PB";
        }

        bytes /= 1024;
        if (bytes <= 1024)
        {
            return bytes.ToString("f2") + " EB";
        }

        bytes /= 1024;
        return bytes.ToString(CultureInfo.InvariantCulture) + " ZB";
    }
}
