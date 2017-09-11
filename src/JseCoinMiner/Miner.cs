using System;
using System.Collections.Generic;
using JseCoinMiner.Models.Response;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Security.Cryptography;
using System.Text;
using JseCoinMiner.Models.Request;

namespace JseCoinMiner
{
    public class Miner
    {
        public void StartNewBlock()
        {
            var client = new RestClient("https://jsecoin.com/server/request/");
            var request = new RestRequest(Method.POST);
            request.AddParameter("o", 1);
            var responseRaw = client.Execute(request);
            var block = responseRaw.Content;

            var root = JObject.Parse(block);
            var response = JsonConvert.DeserializeObject<StartNewBlockResponse>(block);
            var data = root["data"].ToString();
            response.Content = JsonConvert.DeserializeObject<List<StartNewBlockData>>(data);

            StartMining(response);
        }

        private float _hashRate = 500;
        private bool _found;
        public void StartMining(StartNewBlockResponse currentBlock)
        {
            _found = false;
            var difficulty = currentBlock.Difficulty;
            var random = new Random();
            var startNumber = (long) Math.Floor(random.Next() * 99999999f);
            for (var x = startNumber; x <= startNumber + _hashRate && !_found; x++)
            {
                currentBlock.Nonce = x;
                var mySHA256 = SHA256.Create();
                var hash = mySHA256.ComputeHash(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(currentBlock)));
                ProcessHash(Hex(hash), x, difficulty, currentBlock);
            }
            StartNewBlock();
        }

        public void ProcessHash(string hash, long nonce, int difficulty, StartNewBlockResponse block)
        {
            if (hash.Substring(0, 4) == "0000")
            {
                _found = true;
                var submission = new SubmissionRequest
                {
                    Block = block.Block,
                    Hash = hash,
                    Nonce = nonce,
                };

                var client = new RestClient("https://jsecoin.com/server/submit/");
                var request = new RestRequest(Method.POST);
                request.AddBody(JsonConvert.SerializeObject(submission));
                var response = client.Execute(request);
                Console.WriteLine("Yoooo, we got another .01, fam!");
                StartNewBlock();
            }
        }

        public string Hex(byte[] buffer)
        {
            var hexCodes = new List<string>();

            var array = new uint[buffer.Length / 4];
            Buffer.BlockCopy(buffer, 0, array, 0, buffer.Length);

            for (var i = 0; i < array.Length; i++)
            {
                var value = array[i];
                var result = DecimalToArbitrarySystem(value, 16);
                var padded = result.PadLeft(8, '0');
                hexCodes.Add(padded);
            }

            return String.Join("", hexCodes.ToArray());
        }

        public string DecimalToArbitrarySystem(uint decimalNumber, int radix)
        {
            const int BitsInLong = 64;
            const string Digits = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            if (radix < 2 || radix > Digits.Length)
                throw new ArgumentException("The radix must be >= 2 and <= " + Digits.Length.ToString());

            if (decimalNumber == 0)
                return "0";

            int index = BitsInLong - 1;
            long currentNumber = Math.Abs(decimalNumber);
            char[] charArray = new char[BitsInLong];

            while (currentNumber != 0)
            {
                int remainder = (int)(currentNumber % radix);
                charArray[index--] = Digits[remainder];
                currentNumber = currentNumber / radix;
            }

            string result = new String(charArray, index + 1, BitsInLong - index - 1);
            if (decimalNumber < 0)
            {
                result = "-" + result;
            }

            return result;
        }
    }
}