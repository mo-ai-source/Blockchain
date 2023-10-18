using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace BlockchainAssignment
{
    class Block

    {
        /* Block Variables */
        private DateTime timestamp; // Time of creation

        private int index; // Position of the block in the sequence of blocks
         
        public String prevHash, // A reference pointer to the previous block
            hash, // The current blocks "identity"
            merkleRoot,  // The merkle root of all transactions in the block
            minerAddress; // Public Key (Wallet Address) of the Miner

        public List<Transaction> transactionList; // List of transactions in this block

        // Proof-of-work
        public long nonce = 0; // Nonce is a one-time use token used for building trust.
        private object _nonceLock = new object(); // Used to lock the nonce number being used by a thread to prevent repetitive work.
        public int threadsNumber = 8; // Number of threads to be used for mining.
        public Stopwatch stopwatch; // The stopwatch to time the mining process.
        public long elapsedTimeInSeconds; // Used to hold the time taken for the mining process in seconds for readability.
        public bool isMiningComplete; // Boolean used to stop the other threads when the mining process is complete.
        public string finalHashResult; // The resulting hash that satisfies the difficulty level.

        //difficult

        // Dynamic Difficulty
        public int difficulty = 4;
        public static int _difficulty = 4;
        public int expectedBlockTime = 10000; // in milli-seconds
        public double averageBlockTime = 0;
        public int blockLookback = 24;
        public static double totalTime = 0;


        // Rewards
        public double reward; // Simple fixed reward established by "Coinbase"

        /* Genesis block constructor */
        public Block()
        {
            timestamp = DateTime.Now;
            index = 0;
            transactionList = new List<Transaction>();
            hash = Mine();
        }

        /* New Block constructor */
        public Block(Block lastBlock, List<Transaction> transactions, String minerAddress)
        {
            timestamp = DateTime.Now;

            index = lastBlock.index + 1;
            prevHash = lastBlock.hash;

            this.minerAddress = minerAddress; // The wallet to be credited the reward for the mining effort
            reward = 1.0; // Assign a simple fixed value reward
            transactions.Add(createRewardTransaction(transactions)); // Create and append the reward transaction
            transactionList = new List<Transaction>(transactions); // Assign provided transactions to the block


            // Adjusts the internal difficulty
            difficulty = _difficulty;

            merkleRoot = MerkleRoot(transactionList); // Calculate the merkle root of the blocks transactions
            hash = Mine(); // Conduct PoW to create a hash which meets the given difficulty requirement
        }

        /* Hashes the entire Block object */
        public String CreateHash(long nonce)
        {
            String hash = String.Empty;
            SHA256 hasher = SHA256Managed.Create();

            /* Concatenate all of the blocks properties including nonce as to generate a new hash on each call */
            String input = timestamp.ToString() + index + prevHash + nonce + merkleRoot;

            /* Apply the hash function to the block as represented by the string "input" */
            Byte[] hashByte = hasher.ComputeHash(Encoding.UTF8.GetBytes(input));

            /* Reformat to a string */
            foreach (byte x in hashByte)
                hash += String.Format("{0:x2}", x);
            
            return hash;
        }


        public String CreateHash()
        {
            String hash = String.Empty;
            SHA256 hasher = SHA256Managed.Create();

            /* Concatenate all of the blocks properties including nonce as to generate a new hash on each call */
            String input = timestamp.ToString() + index + prevHash + nonce + merkleRoot;

            /* Apply the hash function to the block as represented by the string "input" */
            Byte[] hashByte = hasher.ComputeHash(Encoding.UTF8.GetBytes(input));

            /* Reformat to a string */
            foreach (byte x in hashByte)
                hash += String.Format("{0:x2}", x);

            return hash;
        }
        // Create a Hash which satisfies the difficulty level required for PoW
        public String Mine()
        {// Initialize the nonce
            nonce = 0;
            // Hash the block using the initial nonce
            String hash = CreateHash(nonce);
            // Set up the thread array with size equal to the pre-set number of threads
            Thread[] threads = new Thread[threadsNumber];
            // Set the mining completion requirement to false initially
            isMiningComplete = false;
            // Create a string for analyzing the PoW requirement
            String re = new string('0', difficulty);
            // Start the stopwatch to time the mining process
            stopwatch = Stopwatch.StartNew();

            // Start each thread
            for (int i = 0; i < threadsNumber; i++)
            {
                // Create a delegate to the MineThread method with all arguments included
                ThreadStart threadDelegate = new ThreadStart(() => MineThread(i, re, hash));
                // Create a new thread and add it to the thread array
                threads[i] = new Thread(threadDelegate);
                // Write to the console that the thread has been initiated
                Console.WriteLine("Thread " + i + " initiated");
                // Start the thread
                threads[i].Start();
            }

            // Wait for mining to complete before stopping the stopwatch
            while (!isMiningComplete) { }
            // Stop the stopwatch
            stopwatch.Stop();
            // Calculate the elapsed time in milliseconds
            elapsedTimeInSeconds = stopwatch.ElapsedMilliseconds;

            totalTime += elapsedTimeInSeconds;

            // Calls the adjust difficulty every 10 blocks and resets the totalTime that counted the previous 10 blocks.
            if (index % 10 == 0 && index != 0) // Makes sure it's every 10 blocks and doesn't count for the genesis block
            {
                // Adjusts the difficulty based on the average of the last 10 blocks.
                averageBlockTime = totalTime / 10;
                if (averageBlockTime < 0.8 * expectedBlockTime) // increases difficulty if the time is less than 80% of the expected block time
                {
                    _difficulty += 1;
                }
                else if (averageBlockTime > 1.2 * expectedBlockTime) // decreases difficulty if the time is greater than 120% of the expected block time
                {
                    _difficulty -= 1;
                }
                totalTime = 0;
            }

            // Write the final hash and the time taken to mine it to the console
            Console.WriteLine("Hash " + finalHashResult + " found in " + elapsedTimeInSeconds + "ms");
            // Return the final hash
            return finalHashResult;
        }
            // Method for each mining thread
        public void MineThread(int threadnumber, string re, string hash)
            {
                long threadNonce; // Declare a new nonce for the thread

                // As long as the hash doesn't fulfill the difficulty requirement, keep calculating
                while (!hash.StartsWith(re))
                {
                    // If another thread has already completed mining, return from this thread
                    if (isMiningComplete) return;
                    // Lock the nonce so that no other thread can take this nonce
                    lock (_nonceLock)
                    {
                        // Set this thread's nonce to the old nonce + 1
                        threadNonce = nonce++;
                    }
                    // Create the hash using the thread's nonce
                    hash = CreateHash(threadNonce);
                // Uncomment the line below to track nonce and thread progress
                 //Console.WriteLine("Thread " + threadnumber + " with nonce " + threadNonce + " and hash " + finalHashResult);
            }
            // Set the mining completion bool to true for all threads once a hash fulfilling the difficulty requirement is found
            isMiningComplete = true;
                // Set the final hash result for all threads to the hash fulfilling the difficulty requirement
                finalHashResult = hash;
         }
        
            // Merkle Root Algorithm - Encodes transactions within a block into a single hash
            public static String MerkleRoot(List<Transaction> transactionList)
        {
            List<String> hashes = transactionList.Select(t => t.hash).ToList(); // Get a list of transaction hashes for "combining"
            
            // Handle Blocks with...
            if (hashes.Count == 0) // No transactions
            {
                return String.Empty;
            }
            if (hashes.Count == 1) // One transaction - hash with "self"
            {
                return HashCode.HashTools.combineHash(hashes[0], hashes[0]);
            }
            while (hashes.Count != 1) // Multiple transactions - Repeat until tree has been traversed
            {
                List<String> merkleLeaves = new List<String>(); // Keep track of current "level" of the tree

                for (int i=0; i<hashes.Count; i+=2) // Step over neighbouring pair combining each
                {
                    if (i == hashes.Count - 1)
                    {
                        merkleLeaves.Add(HashCode.HashTools.combineHash(hashes[i], hashes[i])); // Handle an odd number of leaves
                    }
                    else
                    {
                        merkleLeaves.Add(HashCode.HashTools.combineHash(hashes[i], hashes[i + 1])); // Hash neighbours leaves
                    }
                }
                hashes = merkleLeaves; // Update the working "layer"
            }
            return hashes[0]; // Return the root node
        }

        // Create reward for incentivising the mining of block
        public Transaction createRewardTransaction(List<Transaction> transactions)
        {
            double fees = transactions.Aggregate(0.0, (acc, t) => acc + t.fee); // Sum all transaction fees
            return new Transaction("Mine Rewards", minerAddress, (reward + fees), 0, ""); // Issue reward as a transaction in the new block
        }

        /* Concatenate all properties to output to the UI */
        public override string ToString()
        {
            return "[BLOCK START]"
                + "\nIndex: " + index
                + "\tTimestamp: " + timestamp
                + "\nPrevious Hash: " + prevHash
                + "\n-- PoW --"
                + "\nDifficulty Level: " + difficulty
                + "\nNonce: " + nonce
                + "\nHash: " + hash
                + "\n-- Rewards --"
                + "\nReward: " + reward
                + "\nMiners Address: " + minerAddress
                +"\nNumber of threads used: " + threadsNumber.ToString()
                + "\nMined in: " + elapsedTimeInSeconds + "ms\n"
                + "\n-- " + transactionList.Count + " Transactions --"
                +"\nMerkle Root: " + merkleRoot
                + "\n" + String.Join("\n", transactionList)
                + "\n[BLOCK END]";
        }
    }
}
