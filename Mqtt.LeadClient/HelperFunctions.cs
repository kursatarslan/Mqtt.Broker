using System;
using System.Collections;
using System.Text;

namespace Mqtt.LeadClient
{
  public static class HelperFunctions
  {
    public static string RandomString(int size, bool lowerCase = false)
    {
      var _random = new Random();
      var builder = new StringBuilder(size);

      // Unicode/ASCII Letters are divided into two blocks
      // (Letters 65–90 / 97–122):
      // The first group containing the uppercase letters and
      // the second group containing the lowercase.  

      // char is a single Unicode character  
      var offset = lowerCase ? 'a' : 'A';
      const int lettersOffset = 26; // A...Z or a..z: length=26  

      for (var i = 0; i < size; i++)
      {
        var @char = (char)_random.Next(offset, offset + lettersOffset);
        builder.Append(@char);
      }

      return lowerCase ? builder.ToString().ToLower() : builder.ToString();
    }

    public static string Base64Encode(string plainText)
    {
      return Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText));
    }

    public static byte[] GetPayload(Payload payload)
    {
      var bytes = new byte[64];


      return bytes;
    }

    private static uint Byte4UInt(byte[] b, int startIndex) => (uint)(((b[startIndex] & 0xff) << 24) | ((b[1 + startIndex] & 0xff) << 16) | ((b[2 + startIndex] & 0xff) << 8) | (b[3 + startIndex] & 0xff));
    public static Payload GetPayload(byte[] serverPayload)
    {

      var payload = new Payload();

      payload.StationId = Byte4UInt(serverPayload, 0);
      payload.MyPlatoonId = Byte4UInt(serverPayload, 36);
      payload.Maneuver = (uint)(serverPayload[40] & 0x0f);
      payload.PlatoonDissolveStatus = serverPayload[43] != 0;

      return payload;
    }
    public static string ToBitString(BitArray bits, int indexStart, int indexFinish)
    {
      var sb = new StringBuilder();
      if ((indexStart < 0) || (indexFinish >= bits.Length))
      {
        return null;
      }
      for (var i = indexStart; i < indexFinish; i++)
      {
        var c = bits[i] ? '1' : '0';
        sb.Append(c);
      }

      return sb.ToString();
    }
    /*
        public static byte[] BitArrayToByteArray(BitArray bits)
        {
          var ret = new byte[(bits.Length - 1) / 8 + 1];
          bits.CopyTo(ret, 0);
          return ret;
        }
        */

    public static byte[] BitArrayToByteArray(BitArray bits)
    {
      int numBytes = bits.Count / 8;
      if (bits.Count % 8 != 0) numBytes++;

      byte[] bytes = new byte[numBytes];
      int byteIndex = 0, bitIndex = 0;

      for (int i = 0; i < bits.Count; i++)
      {
        if (bits[i])
          bytes[byteIndex] |= (byte)(1 << (7 - bitIndex));

        bitIndex++;
        if (bitIndex == 8)
        {
          bitIndex = 0;
          byteIndex++;
        }
      }
      return bytes;
    }

  }
}