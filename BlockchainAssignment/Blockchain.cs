using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlockchainAssignment
{
    class Blockchain
    {
        // List of block objects forming the blockchain
        public List<Block> blocks;

        // Maximum number of transactions per block
        private int transactionsPerBlock = 5;

        // List of pending transactions to be mined
        public List<Transaction> transactionPool = new List<Transaction>();

        // Default Constructor - initialises the list of blocks and generates the genesis block
        public Blockchain()
        {
            blocks = new List<Block>()
            {
                new Block() // Create and append the Genesis Block
            };
        }

        // Prints the block at the specified index to the UI
        public String GetBlockAsString(int index)
        {
            // Check if referenced block exists
            if (index >= 0 && index < blocks.Count)
                return blocks[index].ToString(); // Return block as a string
            else
                return "No such block exists";
        }

        // Retrieves the most recently appended block in the blockchain
        public Block GetLastBlock()
        {
            return blocks[blocks.Count - 1];
        }

        // Retrieve pending transactions and remove from pool
        public List<Transaction> GetPendingTransactions()
        {
            // Determine the number of transactions to retrieve dependent on the number of pending transactions and the limit specified
            int n = Math.Min(transactionsPerBlock, transactionPool.Count);

            // "Pull" transactions from the transaction list (modifying the original list)
            List<Transaction> transactions = transactionPool.GetRange(0, n);
            transactionPool.RemoveRange(0, n);

            // Return the extracted transactions
            return transactions;
        }

        public List<Transaction> GetPendingTransactions(String type)
        {
            int n = Math.Min(transactionsPerBlock, transactionPool.Count());
            List<Transaction> transactions = new List<Transaction>();

            if (type == "Greedy")
            {
                List<Transaction> sortedTransactionPool = transactionPool.OrderByDescending(t => t.fee).ToList(); // Creates a variable that has the transactions sorted by fee transactions of the pool

                for (int i = 0; i < n; i++)
                {
                    transactions.Add(sortedTransactionPool.ElementAt(i));// adds to the transactions list the transaction
                    transactionPool.Remove(sortedTransactionPool.ElementAt(i)); // removes from the pool the transaction
                }

            }
            else if (type == "Altruistic")
            {
                List<Transaction> sortedTransactionPool = transactionPool.OrderBy(t => t.timestamp).ToList(); // Creates a variable that has the transactions sorted by timestamp of transactions of the pool

                for (int i = 0; i < n; i++)
                {
                    transactions.Add(sortedTransactionPool.ElementAt(i));// adds to the transactions list the transaction
                    transactionPool.Remove(sortedTransactionPool.ElementAt(i)); // removes from the pool the transaction
                }
            }
            else if (type == "Random")
            {
                int index; // Create an index for which item to add
                var rand = new Random(); // Creates the random variable method

                // Iterates until it reaches the number of transactions per block
                for (int i = 0; i < n; i++)
                {
                    index = rand.Next(transactionPool.Count); // Creates the next random index from the transactionPool
                    transactions.Add(transactionPool.ElementAt(index)); // Adds from the pool to the list of transactions
                    transactionPool.RemoveAt(index); // Removes from the pool to not be repeated
                }
            }
            else
            {
                transactions = transactionPool.GetRange(0, n);
                transactionPool.RemoveRange(0, n);
            }

            return transactions;

        }


        // This method returns a list of pending transactions based on the selected type and preference address
        public List<Transaction> GetPendingTransactions(String type, String preferenceAddress)
        {
            // Get the minimum of transactions per block and the number of transactions in the transaction pool
            int n = Math.Min(transactionsPerBlock, transactionPool.Count());
            List<Transaction> transactions = new List<Transaction>();

            // Create a variable that has the transactions of the pool sorted by preferred address
            List<Transaction> sortedTransactionPool = transactionPool.Where(t => t.senderAddress == preferenceAddress).ToList();

            // If the selected type is "AddressBased"
            if (type == "AddressBased")
            {
                // Iterate through sortedTransactionPool and add transactions to the transactions list until it reaches the minimum or the end of the pool
                for (int i = 0; i < sortedTransactionPool.Count && i < n; i++)
                {
                    transactions.Add(sortedTransactionPool.ElementAt(i));// adds to the transactions list the transaction
                    transactionPool.Remove(sortedTransactionPool.ElementAt(i)); // removes from the pool the transaction
                }

                // If the number of transactions in transactions list is less than the minimum
                if (transactions.Count < n)
                {
                    // Sort the transaction pool by fee and get the transactions with the highest fees
                    sortedTransactionPool = transactionPool.OrderByDescending(t => t.fee).ToList();
                    for (int i = 0; i < n - transactions.Count; i++)
                    {
                        transactions.Add(sortedTransactionPool.ElementAt(i));// adds to the transactions list the transaction
                        transactionPool.Remove(sortedTransactionPool.ElementAt(i)); // removes from the pool the transaction
                    }
                }
            }
            else // For other types, get the transactions from the pool according to the minimum
            {
                transactions = transactionPool.GetRange(0, n);
                transactionPool.RemoveRange(0, n);
            }

            // Return the list of transactions
            return transactions;
        }
            // Check validity of a blocks hash by recomputing the hash and comparing with the mined value
            public static bool ValidateHash(Block b)
        {
            String rehash = b.CreateHash();
            return rehash.Equals(b.hash);
        }

        // Check validity of the merkle root by recalculating the root and comparing with the mined value
        public static bool ValidateMerkleRoot(Block b)
        {
            String reMerkle = Block.MerkleRoot(b.transactionList);
            return reMerkle.Equals(b.merkleRoot);
        }

        // Check the balance associated with a wallet based on the public key
        public double GetBalance(String address)
        {
            // Accumulator value
            double balance = 0;

            // Loop through all approved transactions in order to assess account balance
            foreach(Block b in blocks)
            {
                foreach(Transaction t in b.transactionList)
                {
                    if (t.recipientAddress.Equals(address))
                    {
                        balance += t.amount; // Credit funds recieved
                    }
                    if (t.senderAddress.Equals(address))
                    {
                        balance -= (t.amount + t.fee); // Debit payments placed
                    }
                }
            }
            return balance;
        }

        // Output all blocks of the blockchain as a string
        public override string ToString()
        {
            return String.Join("\n", blocks);
        }
    }
}
