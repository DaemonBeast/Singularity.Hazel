﻿using System;
using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.ObjectPool;
using Singularity.Hazel.Api.Net.Messages;
using Singularity.Hazel.Api.Unity;

namespace Singularity.Hazel
{
    public class MessageReader : IMessageReader
    {
        private readonly ObjectPool<MessageReader> _pool;
        private bool _inUse;

        internal MessageReader(ObjectPool<MessageReader> pool)
        {
            _pool = pool;
        }

        public byte[] Buffer { get; private set; }

        public int Offset { get; internal set; }

        public int Position { get; internal set; }

        public int Length { get; internal set; }
        
        public int BytesRemaining => this.Length - this.Position;

        public byte Tag { get; private set; }

        public MessageReader Parent { get; private set; }

        private int ReadPosition => Offset + Position;
        public void Update(byte[] buffer, int offset = 0, int position = 0, int? length = null, byte tag = byte.MaxValue, MessageReader parent = null)
        {
            _inUse = true;

            Buffer = buffer;
            Offset = offset;
            Position = position;
            Length = length ?? buffer.Length;
            Tag = tag;
            Parent = parent;
        }

        internal void Reset()
        {
            _inUse = false;

            Tag = byte.MaxValue;
            Buffer = null;
            Offset = 0;
            Position = 0;
            Length = 0;
            Parent = null;
        }

        public IMessageReader ReadMessage()
        {
            var length = ReadUInt16();
            var tag = FastByte();
            var pos = ReadPosition;

            Position += length;

            var reader = _pool.Get();
            reader.Update(Buffer, pos, 0, length, tag, this);
            return reader;
        }

        public void InsertMessage(IMessageReader reader, IMessageWriter writer)
        {
            throw new NotImplementedException();
        }

        private void AdjustLength(int offset, int amount)
        {
            this.Length -= amount;

            if (this.ReadPosition > offset)
            {
                this.Position -= amount;
            }

            if (Parent != null)
            {
                var lengthOffset = this.Offset - 3;
                var curLen = this.Buffer[lengthOffset] |
                             (this.Buffer[lengthOffset + 1] << 8);

                curLen -= amount;

                this.Buffer[lengthOffset] = (byte)curLen;
                this.Buffer[lengthOffset + 1] = (byte)(this.Buffer[lengthOffset + 1] >> 8);

                Parent.AdjustLength(offset, amount);
            }
        }

        public void Dispose()
        {
            if (_inUse)
            {
                _pool.Return(this);
            }
        }

        #region Read Methods
        public bool ReadBoolean()
        {
            byte val = this.FastByte();
            return val != 0;
        }

        public sbyte ReadSByte()
        {
            return (sbyte)this.FastByte();
        }

        public byte ReadByte()
        {
            return this.FastByte();
        }

        public ushort ReadUInt16()
        {
            var output = BinaryPrimitives.ReadUInt16LittleEndian(Buffer.AsSpan(ReadPosition));
            Position += sizeof(ushort);
            return output;
        }

        public short ReadInt16()
        {
            var output = BinaryPrimitives.ReadInt16LittleEndian(Buffer.AsSpan(ReadPosition));
            Position += sizeof(short);
            return output;
        }

        public uint ReadUInt32()
        {
            var output = BinaryPrimitives.ReadUInt32LittleEndian(Buffer.AsSpan(ReadPosition));
            Position += sizeof(uint);
            return output;
        }

        public int ReadInt32()
        {
            var output = BinaryPrimitives.ReadInt32LittleEndian(Buffer.AsSpan(ReadPosition));
            Position += sizeof(int);
            return output;
        }

        public ulong ReadUInt64()
        {
            var output = BinaryPrimitives.ReadUInt64LittleEndian(Buffer.AsSpan(ReadPosition));
            Position += sizeof(ulong);
            return output;
        }

        public long ReadInt64()
        {
            var output = BinaryPrimitives.ReadInt64LittleEndian(Buffer.AsSpan(ReadPosition));
            Position += sizeof(long);
            return output;
        }

        public unsafe float ReadSingle()
        {
            var output = BinaryPrimitives.ReadSingleLittleEndian(Buffer.AsSpan(ReadPosition));
            Position += sizeof(float);
            return output;
        }

        public string ReadString(int length)
        {
            var output = Encoding.UTF8.GetString(Buffer.AsSpan(ReadPosition, length));
            Position += length;
            return output;
        }

        public string ReadString()
        {
            return ReadString(ReadPackedInt32());
        }

        public ReadOnlyMemory<byte> ReadBytesAndSize()
        {
            var len = ReadPackedInt32();
            return ReadBytes(len);
        }

        public ReadOnlyMemory<byte> ReadBytes(int length)
        {
            var output = Buffer.AsMemory(ReadPosition, length);
            Position += length;
            return output;
        }

        ///
        public int ReadPackedInt32()
        {
            return (int)this.ReadPackedUInt32();
        }

        ///
        public uint ReadPackedUInt32()
        {
            bool readMore = true;
            int shift = 0;
            uint output = 0;

            while (readMore)
            {
                byte b = FastByte();
                if (b >= 0x80)
                {
                    readMore = true;
                    b ^= 0x80;
                }
                else
                {
                    readMore = false;
                }

                output |= (uint)(b << shift);
                shift += 7;
            }

            return output;
        }

        public Vector2 ReadVector2()
        {
            const float range = 50f;

            var x = ReadUInt16() / (float)ushort.MaxValue;
            var y = ReadUInt16() / (float)ushort.MaxValue;

            return new Vector2(Mathf.Lerp(-range, range, x), Mathf.Lerp(-range, range, y));
        }

        #endregion

        public void CopyTo(IMessageWriter writer)
        {
            writer.Write((ushort)Length);
            writer.Write((byte)Tag);
            writer.Write(Buffer.AsMemory(Offset, Length));
        }

        public IMessageReader Copy(int offset = 0)
        {
            var reader = _pool.Get();
            reader.Update(Buffer, Offset + offset, Position, Length - offset, Tag, Parent);
            return reader;
        }

        public void Seek(int position)
        {
            Position = position;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private byte FastByte()
        {
            return Buffer[Offset + Position++];
        }
    }
}
