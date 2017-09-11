using System.Collections.Generic;
using Newtonsoft.Json;

namespace JseCoinMiner.Models.Response
{
    public class StartNewBlockResponse
    {
        [JsonProperty(PropertyName = "ThisIsTotallyAHack")]
        public long Nonce;
        public string Hash;
        public string PreviousHash;
        public string Version;
        public float StartTime;
        public int Frequency;
        public int Size;
        public int Difficulty;
        public string Server;
        public long Block;

        public List<StartNewBlockData> Content;
    }
}
