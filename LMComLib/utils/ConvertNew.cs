using System;
using System.Collections.Generic;
using System.Text;

namespace LMNetLib {

  public static class licKeys {
    static string alphabet = "0123456789abcdefghjkmnpqrtuvwxyz";
    static string encode(byte[] input) {
      var skip = 0; // how many bits we will skip from the first byte
      var bits = 0; // 5 high bits, carry from one byte to the next
      var output = "";
      Func<byte, byte> readByte = bt => {
        if (skip < 0) { // we have a carry from the previous byte
          bits |= (bt >> (-skip));
        } else { // no carry
          bits = (bt << skip) & 248;
        }
        if (skip > 3) {// not enough data to produce a character, get us another one
          skip -= 8;
          return 1;
        }
        if (skip < 4) { // produce a character
          output += alphabet[bits >> 3];
          skip += 5;
        }
        return 0;
      };
      var i = 0;
      while (i < input.Length) i += readByte(input[i]);
      return output;
    }

    public static string encode(int licId, int counter) {
      var b1 = BitConverter.GetBytes(licId);
      var b2 = BitConverter.GetBytes(counter);
      var b3 = new byte[] { b2[0], b2[1], b2[2], b1[0], b1[1] };
      b3 = LowUtils.encrypt(b3);
      return encode(b3).ToUpper();
    }
  }

  public static class ConvertNew {
    //Pro kodovani useku 5 bitu na znaky
    const string charCodesSrc = "PMF8UKESZ2TC3J76R14YBWDGV5XALNH9";
    static Dictionary<char, byte> intCodes = new Dictionary<char, byte>();
    static Dictionary<byte, char> charCodes = new Dictionary<byte, char>();
    static ConvertNew() {
      for (byte b = 0; b < charCodesSrc.Length; b++)
        charCodes.Add(b, charCodesSrc[b]);
      for (byte b = 0; b < charCodesSrc.Length; b++)
        intCodes.Add(charCodes[b], b);
    }

    public static byte shiftBits(byte b, int srcStart, int srcEnd, int destStart) {
      b = (byte)(b << srcStart);
      b = (byte)(b >> (7 - srcEnd + srcStart));
      int shift = destStart - (7 - srcEnd + srcStart);
      return shift > 0 ? (byte)(b >> shift) : (byte)(b << -shift);
    }

    public static byte to5Bits(byte b1, int b1Start, int b1End, byte b2, int b2Start) {
      int b1Len = b1End - b1Start + 1;
      return (byte)(shiftBits(b1, b1Start, b1End, 3) | shiftBits(b2, b2Start, b2Start + 5 - b1Len - 1, 3 + b1Len));
    }

    public static byte to5Bits(byte b, int start) {
      return shiftBits(b, start, start + 4, 3);
    }

    public static void from5Bits(byte src, ref byte b1, int b1Start, int b1End, ref byte b2, int b2Start) {
      b1 = (byte)(b1 | shiftBits(src, 3, 3 + b1End - b1Start, b1Start));
      b2 = (byte)(b2 | shiftBits(src, 3 + b1End - b1Start + 1, 7, b2Start));
    }

    public static void from5Bits(byte src, ref byte b, int start) {
      b = (byte)(b | shiftBits(src, 3, 7, start));
    }

    /// <summary>
    /// Rozkrajeni byte array na useky po 5 bitech
    /// </summary>
    static byte[] ToByte32(byte[] data, int offset, int length, bool incSignature) {
      int bitsLen = length << 3; //delka vstupu v bitech
      int outputLen = bitsLen / 5;
      if ((bitsLen % 5) != 0) outputLen++;
      byte[] res = new byte[outputLen + (incSignature ? 2 : 1)]; //vysledek
      res[0] = (byte)(bitsLen % 5); //v prvnim byte je pocet bite v poslednim znaku
      int bitStart, bitEnd, byteStart, byteEnd, startIdx, endIdx;
      byte endData;
      for (int resIdx = 0; resIdx < outputLen; resIdx++) {
        bitStart = resIdx * 5; //index prvniho bitu
        bitEnd = bitStart + 4; //index posledniho bitu
        byteStart = bitStart >> 3; //index prvniho byte
        startIdx = bitStart % 8; //index prvniho bite v prvnim byte
        byteEnd = bitEnd >> 3; //index posledniho byte
        endIdx = (startIdx + 5 - 1) % 8;
        if (byteStart == byteEnd)  //prvni a posledni byte jsou stejne
          res[resIdx + 1] = to5Bits(data[byteStart + offset], startIdx);
        else { //prvni a posledni byte se lisi
          endData = byteEnd + offset >= length ? (byte)0 : data[byteEnd + offset]; //ev. doplne fake byte
          res[resIdx + 1] = to5Bits(data[byteStart + offset], startIdx, 7, endData, 0);
        }
      }
      if (incSignature) {
        res[0] = (byte)(res[0] | 0x10);
        res[outputLen + 1] = countCheck(res, 0, outputLen + 1);
      }
      return res;
    }

    /// <summary>
    /// Spojeni 5 bitovych useku do bytearray
    /// </summary>
    static byte[] FromByte32(byte[] data) {
      byte lastLen = (byte)(data[0] & 0x0F);
      bool incSignature = (data[0] & 0x10) != 0;
      int outputLen = data.Length - (incSignature ? 2 : 1);
      if (incSignature && !checkCheck(data, 0, outputLen + 1, data[outputLen + 1]))
        throw new Exception();
      int bitsLen = outputLen * 5;
      if (lastLen > 0) bitsLen = bitsLen + lastLen - 5;
      int length = bitsLen / 8;
      byte[] res = new byte[length];
      Array.ForEach<byte>(res, delegate (byte b) { b = 0; });
      int bitStart, bitEnd, byteStart, byteEnd, startIdx, endIdx;
      byte actData;
      for (int resIdx = 0; resIdx < outputLen; resIdx++) {
        bitStart = resIdx * 5; //index prvniho bitu
        bitEnd = bitStart + 4; //index posledniho bitu
        byteStart = bitStart >> 3; //index prvniho byte
        startIdx = bitStart % 8; //index prvniho bite v prvnim byte
        byteEnd = bitEnd >> 3; //index posledniho byte
        endIdx = (startIdx + 5 - 1) % 8;
        if (byteStart == byteEnd) //prvni a posledni byte jsou stejne
          from5Bits(data[resIdx + 1], ref res[byteStart], startIdx);
        else { //prvni a posledni byte se lisi
          actData = 0;
          from5Bits(data[resIdx + 1], ref res[byteStart], startIdx, 7, ref actData, 0);
          if (byteEnd < length)
            res[byteEnd] = actData;
        }
      }
      return res;
    }

    const ushort c1 = 52845; const ushort c2 = 22719;
    public static void Encrypt(ref byte[] data, int start, int len, ushort key) {
      for (int i = 0; i < data.Length; i++) {
        data[i] = (byte)(data[i] ^ (key >> 8));
        key = (ushort)((data[i] + key) * c1 + c2);
      }
    }
    public static void Decrypt(ref byte[] data, int start, int len, ushort key) {
      byte old;
      for (int i = 0; i < data.Length; i++) {
        old = data[i];
        data[i] = (byte)(old ^ (key >> 8));
        key = (ushort)((old + key) * c1 + c2);
      }
    }
    public static void Encrypt(ref byte[] data, ushort key) {
      Encrypt(ref data, 0, data.Length, key);
    }
    public static void Decrypt(ref byte[] data, ushort key) {
      Decrypt(ref data, 0, data.Length, key);
    }
    public static string Encrypt(string str, ushort key) {
      byte[] data = Encoding.Unicode.GetBytes(str);
      Encrypt(ref data, key);
      return Convert.ToBase64String(data);
    }
    public static string Decrypt(string str, ushort key) {
      byte[] data = Convert.FromBase64String(str);
      Decrypt(ref data, key);
      return Encoding.Unicode.GetString(data, 0, data.Length);
    }

    public static string Byte32ToString(byte[] data, int startIdx, int len) {
      char[] res = new char[len];
      for (int i = 0; i < len; i++)
        res[i] = charCodes[data[startIdx + i]];
      return new string(res);
    }

    static string Byte32ToString(byte[] data) {
      return Byte32ToString(data, 0, data.Length);
    }

    static byte[] StringToByte32(string s) {
      byte[] res = new byte[s.Length];
      StringToByte32(s, ref res, 0);
      return res;
    }

    public static int StringToByte32(string s, ref byte[] res, int startIdx) {
      try {
        s = s.Trim().ToUpper();
        for (int i = 0; i < s.Length; i++)
          res[startIdx + i] = intCodes[s[i]];
        return s.Length;
      } catch (Exception exp) {
        throw new Exception("Wrong character", exp);
      }

    }

    public static string ToBase32Trunc(byte[] data) { //vsechny bytes jsou vyuzity, data.length je nasobek 5-ti
      string res = ToBase32(data);
      if (res[0] != charCodes[0]) throw new Exception();
      return res.Substring(1);
    }
    public static byte[] FromBase32Trunc(string s) {
      return FromBase32(charCodes[0] + s);
    }

    public static string ToBase32(byte[] data, int startIdx, int len, bool incSignature) {
      return Byte32ToString(ToByte32(data, startIdx, len, incSignature));
    }
    public static string ToBase32(byte[] data, int startIdx, int len) {
      return ToBase32(data, startIdx, len, false);
    }
    public static string ToBase32(byte[] data, bool incSignature) {
      return ToBase32(data, 0, data.Length, incSignature);
    }
    public static string ToBase32(byte[] data) {
      return ToBase32(data, false);
    }
    public static byte[] FromBase32(string s) {
      return FromByte32(StringToByte32(s));
    }

    public static string ToBase32(byte[] data, int startIdx, int len, ushort key, bool incSignature) {
      Encrypt(ref data, key);
      return Byte32ToString(ToByte32(data, startIdx, len, incSignature));
    }

    public static string ToBase32(byte[] data, ushort key, bool incSignature) {
      return ToBase32(data, 0, data.Length, key, incSignature);
    }

    public static string ToBase32(byte[] data, ushort key) {
      return ToBase32(data, key, false);
    }

    public static byte[] FromBase32(string s, ushort key) {
      byte[] res = FromByte32(StringToByte32(s));
      Decrypt(ref res, key);
      return res;
    }

    public static byte countCheck(byte[] data, int startIdx, int len) {
      int res = 0;
      for (int i = startIdx; i < startIdx + len; i++)
        res += data[i];
      return (byte)(res % 32);
    }

    public static bool checkCheck(byte[] data, int startIdx, int len, byte value) {
      int res = 0;
      for (int i = startIdx; i < startIdx + len; i++)
        res += data[i];
      return ((byte)(res % 32)) == value;
    }

  }
}
