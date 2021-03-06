﻿using Konata.Library.IO;
using Konata.Library.Protobuf;
using Konata.Msf.Crypto;
using Konata.Utils;
using System.IO;

namespace Konata.Msf
{
    public class Packet : ByteBuffer
    {
        public Packet(byte[] data = null)
            : base(data)
        {

        }

        public Packet(byte[] data, ICryptor cryptor, byte[] cryptKey)
            : base()
        {
            buffer = cryptor.Decrypt(data, cryptKey);
            bufferLength = (uint)buffer.Length;
        }

        public void PutEncryptedBytes(byte[] value, ICryptor cryptor, byte[] cryptKey)
        {
            WriteData(cryptor.Encrypt(value, cryptKey));
        }

        public void PutEncryptedBytes(byte[] value, ICryptor cryptor, byte[] cryptKey,
            Prefix prefixFlag = Prefix.None, byte limitedLength = 0)
        {
            PutBytes(cryptor.Encrypt(value, cryptKey), prefixFlag, limitedLength);
        }

        /// <summary>
        /// 放入 Packet
        /// </summary>
        /// <param name="value"></param>
        public void PutPacket(Packet value)
        {
            PutBytes(value.GetBytes());
        }

        /// <summary>
        /// 加密 Packet 放入
        /// </summary>
        /// <param name="value"></param>
        public void PutPacketEncrypted(Packet value, ICryptor cryptor, byte[] cryptKey)
        {
            PutEncryptedBytes(value.GetBytes(), cryptor, cryptKey);
        }

        /// <summary>
        /// 放入 Tlv
        /// </summary>
        /// <param name="value"></param>
        public void PutTlv(Packet value)
        {
            PutBytes(value.GetBytes());
        }

        /// <summary>
        /// 放入 ProtoNode
        /// </summary>
        /// <param name="value"></param>
        public void PutProtoNode(ProtoTreeRoot value)
        {
            PutBytes(value.Serialize().GetBytes());
        }

        public string TakeHexString(out string value, Prefix prefixFlag)
        {
            return value = Hex.Bytes2HexStr(TakeBytes(out byte[] _, prefixFlag));
        }

        public byte[] TakeDecryptedBytes(out byte[] value, ICryptor cryptor, byte[] cryptKey,
            Prefix prefixFlag = Prefix.None)
        {
            return value = cryptor.Decrypt(TakeBytes(out var _, prefixFlag), cryptKey);
        }

        public byte[] TakeTlvData(out byte[] value, out ushort cmd)
        {
            if (CheckAvailable(4))
            {
                TakeUshortBE(out cmd);
                TakeUshortBE(out ushort len);
                if (CheckAvailable(len))
                {
                    return TakeBytes(out value, len);
                }
                throw new IOException("Incomplete Tlv context.");
            }
            throw new IOException("Incomplete Tlv header.");
        }

        /// <summary>
        /// 獲取打包數據並加密
        /// </summary>
        /// <returns></returns>
        public byte[] GetEncryptedBytes(ICryptor cryptor, byte[] cryptKey)
        {
            return cryptor.Encrypt(GetBytes(), cryptKey);
        }

        private uint barExtLen;
        private uint barPos;
        private Prefix prefix;
        private Endian barLenEndian;
        private bool barEnc = false;
        private byte[] barEncBuffer;
        private uint barEncLength;
        private ICryptor barEncCryptor;
        private byte[] barEncKey;

        /// <summary>
        /// [進入屏障] 在這之後透過 PutMethods 方法組放入的數據將被計算長度
        /// </summary>
        /// <param name="prefixFlag"></param>
        /// <param name="endian"></param>
        public void EnterBarrier(Prefix prefixFlag, Endian endian, uint extend = 0)
        {
            barExtLen = extend;
            barPos = bufferLength;
            prefix = prefixFlag;
            barLenEndian = endian;
            PutEmpty((int)prefixFlag);
        }

        public void EnterBarrierEncrypted(Prefix prefixFlag, Endian endian, ICryptor cryptor, byte[] cryptKey, uint extend = 0)
        {
            EnterBarrier(prefixFlag, endian, extend);
            barEnc = true;
            barEncBuffer = buffer;
            barEncLength = bufferLength;
            barEncCryptor = cryptor;
            barEncKey = cryptKey;
            buffer = null;
            bufferLength = 0;
            writePosition = 0;
        }

        /// <summary>
        /// [離開屏障] 會立即在加入的數據前寫入長度
        /// </summary>
        public void LeaveBarrier()
        {
            if (barEnc)
            {
                byte[] enc = GetEncryptedBytes(barEncCryptor, barEncKey);
                buffer = barEncBuffer;
                bufferLength = barEncLength;
                writePosition = bufferLength;
                PutBytes(enc);
                barEnc = false;
                barEncBuffer = null;
                barEncLength = 0;
                barEncCryptor = null;
                barEncKey = null;
            }
            InsertPrefix(buffer, barPos,
                bufferLength + barExtLen - barPos - (uint)prefix, prefix, barLenEndian);
        }
    }
}
