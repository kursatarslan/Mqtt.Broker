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

    public static Payload GetPayload(byte[] serverPayload)
    {
      /*string bin_strng = "1100110001";
      int number = 0;

      number = Convert.ToInt32(bin_strng, 2);
      Console.WriteLine("Number value of binary \"{0}\" is = {1}",
          bin_strng, number);

      bin_strng = "1111100000110001";
      number = Convert.ToInt32(bin_strng, 2);
      Console.WriteLine("Number value of binary \"{0}\" is = {1}",
          bin_strng, number);
      */

      var payload = new Payload();
      var bitArray = new BitArray(serverPayload);

      payload.StationId = Convert.ToUInt32(ToBitString(bitArray, 0, 32), 2);
      payload.MyPlatoonId = Convert.ToUInt32(ToBitString(bitArray, 288, 320), 2);
      payload.Maneuver = Convert.ToUInt32(ToBitString(bitArray, 320, 323), 2);
      payload.PlatoonDissolveStatus = Convert.ToUInt16(ToBitString(bitArray, 344, 352), 2) > 0;
      // payload.PlatoonGap = Convert.ToInt32(ToBitString(bitArray, 3, 11), 2);
      // payload.PlatoonOverrideStatus = Convert.ToInt32(ToBitString(bitArray, 11, 12), 2) != 0;
      // payload.VehicleRank = Convert.ToInt32(ToBitString(bitArray, 12, 16), 2);
      // payload.BreakPedal = Convert.ToInt32(ToBitString(bitArray, 16, 23), 2);
      // payload.PlatoonDissolveStatus = Convert.ToInt32(ToBitString(bitArray, 23, 24), 2) != 0;
      //payload.StationId = Convert.ToInt32(ToBitString(bitArray, 24, 56), 2);
      // payload.StreamingRequests = Convert.ToInt32(ToBitString(bitArray, 56, 58), 2);
      //payload.V2HealthStatus = Convert.ToInt32(ToBitString(bitArray, 58, 59), 2) != 0;
      //payload.TruckRoutingStaus = Convert.ToInt32(ToBitString(bitArray, 59, 61), 2);
      //payload.RealPayload = Encoding.ASCII.GetString(serverPayload);

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

    public static byte[] BitArrayToByteArray(BitArray bits)
    {
      var ret = new byte[(bits.Length - 1) / 8 + 1];
      bits.CopyTo(ret, 0);
      return ret;
    }

  }
}