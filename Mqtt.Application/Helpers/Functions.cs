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
            var ba = new BitArray(new[] {b});
            return ba.Get(bitNumber);
        }

        public static string ToBitString(BitArray bits, int indexStart, int indexFinish)
        {
            var sb = new StringBuilder();

            for (var i = indexStart; i < indexFinish; i++)
            {
                var c = bits[i] ? '1' : '0';
                sb.Append(c);
            }

            return sb.ToString();
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

            payload.Maneuver = (Maneuver) Convert.ToInt32(Functions.ToBitString(bitArray, 0, 3), 2);
            payload.PlatoonGap = Convert.ToInt32(Functions.ToBitString(bitArray, 3, 11), 2);
            payload.PlatoonOverrideStatus = Convert.ToInt32(Functions.ToBitString(bitArray, 11, 12), 2) != 0;
            payload.VehicleRank = Convert.ToInt32(Functions.ToBitString(bitArray, 12, 16), 2);
            payload.BreakPedal = Convert.ToInt32(Functions.ToBitString(bitArray, 16, 23), 2);
            payload.PlatoonDissolveStatus = Convert.ToInt32(Functions.ToBitString(bitArray, 23, 24), 2) != 0;
            payload.StationId = Convert.ToInt32(Functions.ToBitString(bitArray, 24, 56), 2);
            payload.StreamingRequests = Convert.ToInt32(Functions.ToBitString(bitArray, 56, 58), 2);
            payload.V2HealthStatus = Convert.ToInt32(Functions.ToBitString(bitArray, 58, 59), 2) != 0;
            payload.TruckRoutingStaus = Convert.ToInt32(Functions.ToBitString(bitArray, 59, 61), 2);
            payload.RealPayload = Encoding.ASCII.GetString(serverPayload);

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