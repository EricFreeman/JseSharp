using System;

namespace JseCoinMiner.Runner
{
    public class Program
    {
        static void Main(string[] args)
        {
            var miner = new Miner();
            miner.StartNewBlock();
        }
    }
}