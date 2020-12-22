﻿using System;
using System.IO;
using System.Security.Cryptography;

namespace MinecraftClient.Crypto.Streams
{
    /// <summary>
    /// An encrypted stream using AES, used for encrypting network data on the fly using AES.
    /// This is the regular AesStream class used with the regular .NET framework from Microsoft.
    /// </summary>

    public class RegularAesStream : Stream, IAesStream
    {
        private readonly CryptoStream enc;
        private readonly CryptoStream dec;
        public RegularAesStream(Stream stream, byte[] key)
        {
            BaseStream = stream;
            enc = new CryptoStream(stream, GenerateAES(key).CreateEncryptor(), CryptoStreamMode.Write);
            dec = new CryptoStream(stream, GenerateAES(key).CreateDecryptor(), CryptoStreamMode.Read);
        }
        public System.IO.Stream BaseStream { get; set; }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override void Flush()
        {
            BaseStream.Flush();
        }

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int ReadByte()
        {
            return dec.ReadByte();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return dec.Read(buffer, offset, count);
        }

        public override long Seek(long offset, System.IO.SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void WriteByte(byte b)
        {
            enc.WriteByte(b);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            enc.Write(buffer, offset, count);
        }

        private RijndaelManaged GenerateAES(byte[] key)
        {
            RijndaelManaged cipher = new RijndaelManaged
            {
                Mode = CipherMode.CFB,
                Padding = PaddingMode.None,
                KeySize = 128,
                FeedbackSize = 8,
                Key = key,
                IV = key
            };
            return cipher;
        }
    }
}
