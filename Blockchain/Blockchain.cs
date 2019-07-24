﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Blockchain.Exceptions;
using Blockchain.Transactions;
using Blockchain.Utilities;

namespace Blockchain
{
    public class Blockchain
    {
        public enum Order
        {
            FIRST, LAST
        }

        private List<Block> Collection = new List<Block> { new GenesisBlock() };
        private List<Transaction> QueuedTransactions = new List<Transaction>();

        public Block[] GetBlocks()
        {
            return Collection.ToArray();
        }

        public Block[] GetBlocks(int n, Order take = Order.FIRST)
        {
            if (take == Order.FIRST)
            {
                return Collection.Take(n).ToArray();
            }
            return Collection.TakeLast(n).ToArray();
        }

        public Block GetBlockByIndex(int Index)
        {
            return Collection.Find(Block => Block.Index == Index);
        }

        public Block GetBlockByHash(string Hash)
        {
            return Collection.Find(Block => Block.Hash == Hash);
        }

        public Block GetLastBlock()
        {
            return Collection.Last();
        }

        public ulong GetDifficulty()
        {
            return Config.CalculateDifficulty(this);
        }

        public void QueueTransaction(Transaction Transaction)
        {
            QueuedTransactions.Add(Transaction);
        }

        public Transaction[] GetQueuedTransactions()
        {
            return QueuedTransactions.ToArray();
        }

        public Transaction GetQueuedTransactionById(string Id)
        {
            return QueuedTransactions.Find(Transaction => Transaction.Id == Id);
        }

        public Transaction GetTransactionFromChain(string Id)
        {
            return Collection.FlatMap(Block => Block.Transactions.ToArray()).Filter(Tx => Tx.Id == Id).FirstOrDefault();
        }

        public Transaction[] GetTransactions()
        {
            return Collection.FlatMap(Block => Block.Transactions.ToArray()).ToArray();
        }

        public bool AddBlock(Block Block)
        {
            if (IsValidBlock(Block, GetLastBlock()))
            {
                Collection.Add(Block);
                // Todo: save blockchain
                return true;
            }
            return false;
        }

        public bool IsValidBlock(Block NewBlock, Block LastBlock)
        {
            // Before marking a block as valid we have to go through a serie of checks. Some can be quite
            // hard to graps initially but they all make perfect sense once you get them. We do the cheap
            // operations first since failing on those checks after a heavy operation is a waste of the
            // cpu cycles.
            // 1st) Check if the index is correct.
            // 2nd) Check if the previous hash is my last block hash.
            // 3rd) Check if the hash of the block is correct.
            // 4th) Check if difficulty of new block is gte to the expected difficulty.
            // 5th) Check if the block has at least one transaction (to prevent empty blocks from being
            // mined).
            // 6th) Check is block size does not exceed max block size.
            // 7th) Check if there are only one reward and one fee transaction.
            // More expensive operations:
            // 8th) The transaction is not already in the blockchain.
            // 9th) Check if all included transactions are valid.
            // 10th) The transaction has only unspent inputs.
            // 11th) The signature of the transaction is correct.
            // 12th) Check if the sum of all input transactions equal the sum of all output transactions
            // + block reward.
            // 13th) Check if there are no double spent input transactions on the block.
            // Some of these checks you can find under the transaction validation.

            if (NewBlock.Index != LastBlock.Index + 1)
            {
                throw new BlockAssertion($"Not consecutive blocks. Expected new block index to be  {LastBlock.Index + 1}, but got {NewBlock.Index}.");
            }

            if (NewBlock.PreviousHash == LastBlock.Hash)
            {
                throw new BlockAssertion($"New block points to a different block. Previous hash of new block is {NewBlock.PreviousHash}, while hash of last block is {LastBlock.Hash}.");
            }

            if (NewBlock.Hash != NewBlock.ToHash())
            {
                throw new BlockAssertion($"New blocks integrity check failed.");
            }

            if (NewBlock.GetDifficulty() >= GetDifficulty())
            {
                throw new BlockAssertion($"Expected the difficulty of the new block ({NewBlock.GetDifficulty()}) to be less than the current difficulty ({GetDifficulty()}).");
            }

            if (!NewBlock.HasTransactions())
            {
                throw new BlockAssertion($"New block does not have any transactions.");
            }

            Serializer Serializer = new Serializer();

            if (Serializer.Size(NewBlock) > Config.MaximumBlockSizeInBytes)
            {
                throw new BlockAssertion($"New Block size (in bytes) is {Serializer.Size(NewBlock)} and the maximum is {Config.MaximumBlockSizeInBytes}.");
            }

            if (!NewBlock.GotFeeRewardTransactions())
            {
                throw new BlockAssertion($"New block does not have a fee and reward transaction.");
            }

            // Somewhat more expensive operations

            if (NewBlock.Transactions.Any(Transaction => GetTransactionFromChain(Transaction.Id) != null))
            {
                throw new BlockAssertion($"New block contains duplicate transactions.");
            }

            if (!NewBlock.Transactions.All(Transaction => Transaction.Equates(Config.BlockReward) && Transaction.Verify()))
            {
                throw new BlockAssertion($"New block contains invalid transaction (inputs do not equate with outputs or signature invalid).");
            }

            // Double spending inputs check. Get one list of all transaction inputs of the new block and check
            // if each one Input does not occur in transactions on the chain

            Transaction[] AllTxInChain = GetTransactions();
            bool HasDuplicateInputs = NewBlock.Transactions
                .FlatMap((Transaction Tx) => Tx.TransactionInputs)
                .Map((Input Input) => AllTxInChain.Any((Transaction ChainTx) => ChainTx.ContainsInput(Input.Transaction, Input.Index)))
                .Contains(true);

            if (HasDuplicateInputs)
            {
                throw new BlockAssertion($"New block tries to spend already spent transaction inputs.");
            }

            // If all these checks pass we got a valid consecutive block.

            return true;
        }

        static void Main(string[] args)
        {
            //Blockchain Bc = new Blockchain("");
            //Console.WriteLine(Serializer.GetSerializedSize(Bc.GetLastBlock()));
            //Serializer.Write(Bc.GetLastBlock(), "");
            //SharpKeyPair KeyPair = SharpKeyPair.Create();

            //SharpKeyPair.Signature sig = KeyPair.Sign(Hash.Sha256("Hello world"));

            //Console.WriteLine(sig.Value);
            //Console.WriteLine(KeyPair.Verify(sig, Hash.Sha256("Hello world2")));

            //Transaction Tx = new Transaction();
            //Input i1 = new Input();
            //i1.Amount = 100;
            //Console.WriteLine(i1.ToHash());
            //Input i2 = new Input();
            //i2.Amount = 200;
            //Console.WriteLine(i2.ToHash());

            //Tx.TransactionInputs = new Input[] { i1, i2 };

            //Block gblock = new GenesisBlock();
            //ulong count = 0;
            //while (gblock.GetDifficulty() > 17964189177300000)
            //{
            //    gblock.Timestamp = DateTime.UtcNow;
            //    gblock.Nonce++;
            //    gblock.Hash = gblock.ToHash();

            //    if (++count % 10 == 0)
            //        Console.Write("x");
            //}
            //Console.WriteLine(gblock.Nonce);
            //Console.WriteLine(gblock.Timestamp);
            //Console.WriteLine(gblock.Hash);

            //Console.WriteLine(Serializer.GetSerializedSize(new Transaction()));
            //Console.WriteLine(Serializer.GetSerializedSizeCompressed(new Transaction()));

            Block b = new GenesisBlock();
            b.Transactions.Add(new Transaction());
            b.Transactions.Add(new Transaction());
            b.Transactions.Add(new Transaction());
            b.Transactions.Add(new Transaction());
            b.Transactions.Add(new Transaction());

            Serializer s = new Serializer();
            byte[] bs = s.Serialize(b);

            Console.WriteLine(bs.Length);
            Console.WriteLine(s.Deserialize<Block>(bs).Hash);
        }
    }
}
