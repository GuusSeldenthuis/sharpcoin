﻿using System;
using Blockchain.Utilities;

namespace Blockchain.Transactions
{
    [Serializable]
    public class Output
    {
        public ulong Amount = 0;
        public string Address = "";

        public override string ToString()
        {
            return $"{Amount}{Address}";
        }
    }
}
