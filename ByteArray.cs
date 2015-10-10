using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace NetProto.Proto
{
    public class ByteArray
    {
        MemoryStream ms;
        byte[] b16 = new byte[2];
        byte[] b32 = new byte[4];
        byte[] b64 = new byte[8];

        // for pack
        public ByteArray()
        {
            ms = new MemoryStream();
        }

        // for unpack
        public ByteArray(byte[] buffer)
        {
            ms = new MemoryStream(buffer);
        }

        public byte[] Data()
        {
            return ms.ToArray();
        }

        public UInt16 Length()
        {
            return (UInt16)ms.Length;
        }

        public void WriteBoolean(bool b)
        {
            if (b)
            {
                ms.WriteByte(1);
            }
            else
            {
                ms.WriteByte(0);
            }
        }

        public void WriteByte(Byte b)
        {
            ms.WriteByte(b);
        }

        public void WriteRawBytes(byte[] bs)
        {
            int n = bs.Length;
            ms.Write(bs, 0, n);
        }

        public void WriteBytes(byte[] bs)
        {
            WriteUnsignedInt16((UInt16)bs.Length);
            WriteRawBytes(bs);
        }

        public void WriteUTF(string s)
        {
            WriteBytes(Encoding.UTF8.GetBytes(s));
        }

        public void WriteInt8(SByte s)
        {
            WriteByte((Byte)s);
        }

        public void WriteUnsignedInt8(Byte b)
        {
            WriteByte(b);
        }

        public void WriteInt16(Int16 i)
        {
            Byte[] byteArray = BitConverter.GetBytes(i);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(byteArray);
            }
            WriteRawBytes(byteArray);
        }

        public void WriteUnsignedInt16(UInt16 u)
        {
            Byte[] byteArray = BitConverter.GetBytes(u);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(byteArray);
            }
            WriteRawBytes(byteArray);
        }

        public void WriteInt32(Int32 i)
        {
            Byte[] byteArray = BitConverter.GetBytes(i);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(byteArray);
            }
            WriteRawBytes(byteArray);
        }

        public void WriteUnsignedInt(UInt32 u)
        {
            Byte[] byteArray = BitConverter.GetBytes(u);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(byteArray);
            }
            WriteRawBytes(byteArray);
        }

        public void WriteInt64(Int64 i)
        {
            Byte[] byteArray = BitConverter.GetBytes(i);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(byteArray);
            }
            WriteRawBytes(byteArray);
        }

        public void WriteUnsignedInt64(UInt64 u)
        {
            Byte[] byteArray = BitConverter.GetBytes(u);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(byteArray);
            }
            WriteRawBytes(byteArray);
        }

        public void WriteFloat(float f)
        {
            Byte[] byteArray = BitConverter.GetBytes(f);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(byteArray);
            }
            WriteRawBytes(byteArray);
        }

        public void WriteDouble(double d)
        {
            Byte[] byteArray = BitConverter.GetBytes(d);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(byteArray);
            }
            WriteRawBytes(byteArray);
        }

        public bool ReadBoolean()
        {
            return ReadByte() == 1;
        }

        public Byte ReadByte()
        {
            return (Byte)ms.ReadByte();
        }

        public byte[] ReadBytes()
        {
            UInt16 n = ReadUnsignedInt16();
            byte[] byteArray = new byte[n];
            int count = ms.Read(byteArray, 0, n);
            return byteArray;
        }

        public string ReadUTFBytes()
        {
            byte[] byteArray = ReadBytes();
            return Encoding.UTF8.GetString(byteArray);
        }

        public SByte ReadInt8()
        {
            return (SByte)ReadByte();
        }

        public Byte ReadUnsignedInt8()
        {
            return ReadByte();
        }

        public Int16 ReadInt16()
        {
            int count = ms.Read(b16, 0, 2);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(b16);
            }
            Int16 res = BitConverter.ToInt16(b16, 0);
            return res;
        }

        public UInt16 ReadUnsignedInt16()
        {
            int count = ms.Read(b16, 0, 2);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(b16);
            }
            UInt16 res = BitConverter.ToUInt16(b16, 0);
            return res;
        }

        public Int32 ReadInt32()
        {
            int count = ms.Read(b32, 0, 4);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(b32);
            }
            Int32 res = BitConverter.ToInt32(b32, 0);
            return res;
        }

        public UInt32 ReadUnsignedInt32()
        {
            int count = ms.Read(b32, 0, 4);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(b32);
            }
            UInt32 res = BitConverter.ToUInt32(b32, 0);
            return res;
        }

        public Int64 ReadInt64()
        {
            int count = ms.Read(b64, 0, 8);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(b64);
            }
            Int64 res = BitConverter.ToInt64(b64, 0);
            return res;
        }

        public UInt64 ReadUnsignedInt64()
        {
            int count = ms.Read(b64, 0, 8);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(b64);
            }
            UInt64 res = BitConverter.ToUInt64(b64, 0);
            return res;
        }

        public float ReadFloat()
        {
            int count = ms.Read(b32, 0, 4);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(b32);
            }
            float res = BitConverter.ToSingle(b32, 0);
            return res;
        }

        public double ReadDouble()
        {
            int count = ms.Read(b64, 0, 8);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(b64);
            }
            double res = BitConverter.ToDouble(b64, 0);
            return res;
        }

        public void Dispose()
        {
            try
            {
                ms.Dispose();
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
            }
        }
    }
}