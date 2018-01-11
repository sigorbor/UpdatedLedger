using System;

namespace LedgerDatabase
{
    public class AccountLine
    {
        private decimal balance;
        private DateTime balanceModified;

        public AccountLine()
        {
            Balance = 0;
        }

        public decimal Balance {
            get { return balance; }
            set { balance = value;
                  balanceModified = DateTime.Now; }
        }
        public DateTime BalanceModified { get; }
    }

    public class AccountsTable : Table<AccountLine>
    {        
    }
}
