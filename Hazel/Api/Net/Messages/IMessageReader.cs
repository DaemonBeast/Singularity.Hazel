﻿// Adapted from https://github.com/Impostor/Impostor/blob/master/src/Impostor.Api/Net/Messages/IMessageReader.cs

using System;
using System.Numerics;

namespace Singularity.Hazel.Api.Net.Messages
{
    public interface IMessageReader : IDisposable
    {
        /// <summary>
        ///     Gets the tag of the message.
        /// </summary>
        byte Tag { get; }

        /// <summary>
        ///     Gets the buffer of the message.
        /// </summary>
        byte[] Buffer { get; }

        /// <summary>
        ///     Gets the offset of our current <see cref="IMessageReader" /> in the entire <see cref="Buffer" />.
        /// </summary>
        int Offset { get; }

        /// <summary>
        ///     Gets the current position of the reader.
        /// </summary>
        int Position { get; }

        /// <summary>
        ///     Gets the length of the buffer.
        /// </summary>
        int Length { get; }

        IMessageReader ReadMessage();

        bool ReadBoolean();

        sbyte ReadSByte();

        byte ReadByte();

        ushort ReadUInt16();

        short ReadInt16();

        uint ReadUInt32();

        int ReadInt32();

        ulong ReadUInt64();

        long ReadInt64();

        float ReadSingle();

        string ReadString(int length);

        string ReadString();

        ReadOnlyMemory<byte> ReadBytesAndSize();

        ReadOnlyMemory<byte> ReadBytes(int length);

        int ReadPackedInt32();

        uint ReadPackedUInt32();

        void CopyTo(IMessageWriter writer);

        void Seek(int position);

        IMessageReader Copy(int offset = 0);

        Vector2 ReadVector2();
    }
}