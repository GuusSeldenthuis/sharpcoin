﻿using System;

namespace Blockchain.Exceptions
{
    public class BlockAssertion: Exception
    {
        public BlockAssertion(string message) : base(message) { }
    }
}
