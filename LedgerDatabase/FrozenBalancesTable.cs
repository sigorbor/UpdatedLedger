using System;

namespace LedgerDatabase
{
    using AccountID = UInt64;

    public class FrozenBalanceLine
    {
        public FrozenBalanceLine(AccountID accountId, decimal balance)
        {
            AccountId = accountId;
            Balance = balance;
        }

        public AccountID AccountId { get; }
        public decimal Balance { get; }
    }

    public class FrozenBalancesTable : Table<FrozenBalanceLine>
    {
    }
}

