using System;

namespace LedgerDatabase
{
    using AccountID = UInt64;
    using TransferID = UInt64;

    public enum TransactionStatus { Approved = 1, Rejected };
    public enum TransactionRejectReason {  NotRelevant = 1, InvalidAccount, InsufficientFunds, InvalidFreezeID };
    public enum TransactionType { AddFunds = 1, RemoveFunds }
    public enum TransactionSubType { Regular = 1, TransferFunds, Freeze, Unfreeze }

    public class TransactionLine
    {
        public TransactionLine(AccountID accountId, decimal balance, TransactionType type)
        {
            AccountId = accountId;
            Balance = balance;
            Type = type;
            SubType = TransactionSubType.Regular;
            Status = TransactionStatus.Approved;
            RejectReason = TransactionRejectReason.NotRelevant;
            TransferId = 0;
            Timestamp = DateTime.Now;
        }

        public TransactionLine(AccountID accountId, decimal balance, TransactionType type, TransactionSubType subType) : this(accountId, balance, type)
        {
            SubType = subType;
        }

        public AccountID AccountId { get; set; }
        public decimal Balance { get; set; }
        public TransactionType Type { get; set; }
        public TransactionSubType SubType { get; set; }
        public DateTime Timestamp { get; set; }
        public TransactionStatus Status { get; set; }
        public TransactionRejectReason RejectReason { get; set; }
        public TransferID TransferId { get; set; }
    }

    public class TransactionsTable : Table<TransactionLine>
    {
    }
}
