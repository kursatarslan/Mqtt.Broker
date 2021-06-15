using System;
using System.Collections;
using System.Text;
using Mqtt.Domain.Enums;
using Mqtt.Domain.Models;

namespace Mqtt.Application.Helpers
{
  public static class Functions
  {
    /*
    *  bool is_ok = 0x01AF.GetBits(0x10); //false
       int res = 0x01AF.SetBits(0x10, true);
       is_ok = res.GetBits(0x10);  // true
    */

    public static int SetBits(this int target, int field, bool value)
    {
      if (value) //set value
        return target | field;
      return target & ~field;
    }

    public static bool GetBits(this int target, int field)
    {
      return (target & field) > 0;
    }

    public static bool GetBit(this byte b, int bitNumber)
    {
      var ba = new BitArray(new[] { b });
      return ba.Get(bitNumber);
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

    private static uint Byte4UInt(byte[] b, int startIndex)
    {
      return (uint)(((b[startIndex] & 0xff) << 24) | ((b[1 + startIndex] & 0xff) << 16) | ((b[2 + startIndex] & 0xff) << 8) | (b[3 + startIndex] & 0xff));
    }

    public static Payload GetPayload(byte[] serverPayload)
    {
      var payload = new Payload();

      payload.StationId = Byte4UInt(serverPayload, 0);
      payload.MyPlatoonId = Byte4UInt(serverPayload, 36);
      payload.Maneuver = (Maneuver)(serverPayload[40] & 0x0f);
      payload.PlatoonDissolveStatus = serverPayload[43] != 0;
      return payload;
    }

    public static byte[] DecodetoByteArray(string base64Text)
    {
      return Convert.FromBase64String(base64Text);
    }

    public static string Decode(string base64Text)
    {
      var data = DecodetoByteArray(base64Text);
      Console.WriteLine("data => " + data);
      var base64Decoded = Encoding.ASCII.GetString(data);
      return base64Decoded;
    }

    public static byte[] BitArrayToByteArray(BitArray bits)
    {
      var ret = new byte[(bits.Length - 1) / 8 + 1];
      bits.CopyTo(ret, 0);
      return ret;
    }
  }
}