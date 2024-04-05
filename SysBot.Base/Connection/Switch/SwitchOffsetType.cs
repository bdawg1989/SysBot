using System;
using System.Collections.Generic;

namespace SysBot.Base;

/// <summary>
/// Different offset types that can be pointed to for read/write requests.
/// </summary>
public enum SwitchOffsetType
{
    /// <summary>
    /// Heap base offset
    /// </summary>
    Heap,

    /// <summary>
    /// Main NSO base offset
    /// </summary>
    Main,

    /// <summary>
    /// Raw offset (arbitrary)
    /// </summary>
    Absolute,
}

public interface ICommandBuilder
{
    SwitchOffsetType Type { get; }

    byte[] Peek(ulong offset, int length, bool crlf = true);
    byte[] PeekMulti(IReadOnlyDictionary<ulong, int> offsets, bool crlf = true);
    byte[] Poke(ulong offset, ReadOnlySpan<byte> data, bool crlf = true);
}

public static class SwitchOffsetTypeUtil
{
    public static readonly HeapCommand Heap = new();
    public static readonly MainCommand Main = new();
    public static readonly AbsoluteCommand Absolute = new();
}

/// <summary>
/// Heap base offset
/// </summary>
public sealed class HeapCommand : ICommandBuilder
{
    public SwitchOffsetType Type => SwitchOffsetType.Heap;

    public byte[] Peek(ulong offset, int length, bool crlf = true) => SwitchCommand.Peek((uint)offset, length, crlf);

    public byte[] PeekMulti(IReadOnlyDictionary<ulong, int> offsets, bool crlf = true) => SwitchCommand.PeekMulti(offsets, crlf);

    public byte[] Poke(ulong offset, ReadOnlySpan<byte> data, bool crlf = true) => SwitchCommand.Poke((uint)offset, data, crlf);
}

/// <summary>
/// Main NSO base offset
/// </summary>
public sealed class MainCommand : ICommandBuilder
{
    public SwitchOffsetType Type => SwitchOffsetType.Main;

    public byte[] Peek(ulong offset, int length, bool crlf = true) => SwitchCommand.PeekMain(offset, length, crlf);

    public byte[] PeekMulti(IReadOnlyDictionary<ulong, int> offsets, bool crlf = true) => SwitchCommand.PeekMainMulti(offsets, crlf);

    public byte[] Poke(ulong offset, ReadOnlySpan<byte> data, bool crlf = true) => SwitchCommand.PokeMain(offset, data, crlf);
}

/// <summary>
/// Raw offset (arbitrary)
/// </summary>
public sealed class AbsoluteCommand : ICommandBuilder
{
    public SwitchOffsetType Type => SwitchOffsetType.Absolute;

    public byte[] Peek(ulong offset, int length, bool crlf = true) => SwitchCommand.PeekAbsolute(offset, length, crlf);

    public byte[] PeekMulti(IReadOnlyDictionary<ulong, int> offsets, bool crlf = true) => SwitchCommand.PeekAbsoluteMulti(offsets, crlf);

    public byte[] Poke(ulong offset, ReadOnlySpan<byte> data, bool crlf = true) => SwitchCommand.PokeAbsolute(offset, data, crlf);
}
public static class SwitchOffsetTypeExtensions
{
    /// <summary>
    /// Gets the Peek command encoder for the input <see cref="SwitchOffsetType"/>
    /// </summary>
    /// <param name="type">Offset type</param>
    /// <param name="crlf">Protocol uses CRLF to terminate messages?</param>
    public static Func<ulong, int, byte[]> GetReadMethod(this SwitchOffsetType type, bool crlf = true) => type switch
    {
        SwitchOffsetType.Heap => (o, c) => SwitchCommand.Peek((uint)o, c, crlf),
        SwitchOffsetType.Main => (o, c) => SwitchCommand.PeekMain(o, c, crlf),
        SwitchOffsetType.Absolute => (o, c) => SwitchCommand.PeekAbsolute(o, c, crlf),
        _ => throw new IndexOutOfRangeException("Invalid offset type."),
    };

    /// <summary>
    /// Gets the Peek multi command encoder for the input <see cref="SwitchOffsetType"/>
    /// </summary>
    /// <param name="type">Offset type</param>
    /// <param name="crlf">Protocol uses CRLF to terminate messages?</param>
    public static Func<IReadOnlyDictionary<ulong, int>, byte[]> GetReadMultiMethod(this SwitchOffsetType type, bool crlf = true) => type switch
    {
        SwitchOffsetType.Heap => d => SwitchCommand.PeekMulti(d, crlf),
        SwitchOffsetType.Main => d => SwitchCommand.PeekMainMulti(d, crlf),
        SwitchOffsetType.Absolute => d => SwitchCommand.PeekAbsoluteMulti(d, crlf),
        _ => throw new IndexOutOfRangeException("Invalid offset type."),
    };

    /// <summary>
    /// Gets the Poke command encoder for the input <see cref="SwitchOffsetType"/>
    /// </summary>
    /// <param name="type">Offset type</param>
    /// <param name="crlf">Protocol uses CRLF to terminate messages?</param>
    public static Func<ulong, byte[], byte[]> GetWriteMethod(this SwitchOffsetType type, bool crlf = true) => type switch
    {
        SwitchOffsetType.Heap => (o, b) => SwitchCommand.Poke((uint)o, b, crlf),
        SwitchOffsetType.Main => (o, b) => SwitchCommand.PokeMain(o, b, crlf),
        SwitchOffsetType.Absolute => (o, b) => SwitchCommand.PokeAbsolute(o, b, crlf),
        _ => throw new IndexOutOfRangeException("Invalid offset type."),
    };
}
