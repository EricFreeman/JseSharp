using System;
using System.Collections.Generic;
using JseCoinMiner.Models.Response;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Security.Cryptography;
using System.Text;
using JseCoinMiner.Models.Messages;
using JseCoinMiner.Models.Request;
using UnityEventAggregator;

namespace JseCoinMiner
{
    public class Miner
    {
        private string UniqId;

        public void Start()
        {
            EventAggregator.SendMessage(new StartMiningEvent());
            while (true)
            {
                try
                {
                    UniqId = GenerateUniqueId();
                    Console.WriteLine("Starting to mine: " + UniqId);
                    StartNewBlock();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Oops, something broke! Starting over.\r\n" + e.Message);
                    UniqId = GenerateUniqueId();
                }
            }
        }

        private string GenerateUniqueId()
        {
            var uniq = new StringBuilder("0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ");
            int length = uniq.Length;
            var random = new Random();
            for (int i = length - 1; i > 0; i--)
            {
                int j = random.Next(i);
                char temp = uniq[j];
                uniq[j] = uniq[i];
                uniq[i] = temp;
            }

            return uniq.ToString().Substring(0, 20);
        }

        private void StartNewBlock()
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

            EventAggregator.SendMessage(new NewBlockEvent());
            MineBlock(response);
        }

        private float _hashRate = 1500;
        private bool _found;
        private void MineBlock(StartNewBlockResponse currentBlock)
        {
            _found = false;
            var difficulty = currentBlock.Difficulty;
            var random = new Random();
            var startNumber = random.Next(99999999);
            for (var x = startNumber; x <= startNumber + _hashRate && !_found; x++)
            {
                currentBlock.Nonce = x;
                var mySHA256 = SHA256.Create();
                var hash = mySHA256.ComputeHash(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(currentBlock)));
                ProcessHash(Hex(hash), x, difficulty, currentBlock);
            }
            StartNewBlock();
        }

        private void ProcessHash(string hash, long nonce, int difficulty, StartNewBlockResponse block)
        {
            if (hash.Substring(0, 4) == "0000")
            {
                _found = true;
                var submission = new SubmissionRequest
                {
                    block = block.Block,
                    hash = hash.ToLower(),
                    nonce = nonce.ToString(),
                    uniq = UniqId
                };

                var client = new RestClient("https://jsecoin.com/server/submit/");
                var request = new RestRequest(Method.POST);
                var body = JsonConvert.SerializeObject(submission);
                request.AddHeader("Content-type", "application/x-www-form-urlencoded");
                request.AddParameter("o", body);

                Console.WriteLine(body);

                var response = client.Execute(request);

                EventAggregator.SendMessage(new HashSubmittedEvent());

                Console.WriteLine(response.Content);

                StartNewBlock();
            }
        }

        private string Hex(byte[] buffer)
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

        private string DecimalToArbitrarySystem(uint decimalNumber, int radix)
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