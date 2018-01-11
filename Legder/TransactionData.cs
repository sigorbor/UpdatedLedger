using System;
using LedgerAPI;

namespace LedgerLogic
{
    using AccountID = UInt64;
    using TransferID = UInt64;

    public class TransactionData : ITransactionData
    {
        public AccountID AccountId { get; set; }
        public decimal Balance { get; set; }
        public LedgerTransactionType Type { get; set; }
        public LedgerTransactionSubType SubType { get; set; }
        public DateTime Timestamp { get; set; }
        public LedgerTransactionApproval Status { get; set; }
        public LedgerTransactionRejectReason RejectReason { get; set; }
        public TransferID TransferId { get; set; }

        public override string ToString() => "AccountId: " + AccountId + " Balance: " + Balance + " Type:" + Type + " SubType:" + SubType + " Status:" + Status + " RejectReason:" + RejectReason + " TransferId:" + TransferId + " TS:" + Timestamp;

        public TransactionData(LedgerDatabase.TransactionLine line)
        {
            AccountId = line.AccountId;
            Balance = line.Balance;
            Timestamp = line.Timestamp;
            TransferId = line.TransferId;

            switch (line.Type)
            {
                case LedgerDatabase.TransactionType.AddFunds:
                    Type = LedgerTransactionType.AddFunds;
                    break;
                case LedgerDatabase.TransactionType.RemoveFunds:
                    Type = LedgerTransactionType.RemoveFunds;
                    break;
            }
            switch (line.SubType)
            {
                case LedgerDatabase.TransactionSubType.Regular:
                    SubType = LedgerTransactionSubType.Regular;
                    break;
                case LedgerDatabase.TransactionSubType.TransferFunds:
                    SubType = LedgerTransactionSubType.TransferFunds;
                    break;
                case LedgerDatabase.TransactionSubType.Freeze:
                    SubType = LedgerTransactionSubType.Freeze;
                    break;
                case LedgerDatabase.TransactionSubType.Unfreeze:
                    SubType = LedgerTransactionSubType.Unfreeze;
                    break;
            }
            switch (line.Status)
            {
                case LedgerDatabase.TransactionStatus.Approved:
                    Status = LedgerTransactionApproval.Approved;
                    break;
                case LedgerDatabase.TransactionStatus.Rejected:
                    Status = LedgerTransactionApproval.Rejected;
                    break;
            }
            switch (line.RejectReason)
            {
                case LedgerDatabase.TransactionRejectReason.NotRelevant:
                    RejectReason = LedgerTransactionRejectReason.NotRelevant;
                    break;
                case LedgerDatabase.TransactionRejectReason.InvalidAccount:
                    RejectReason = LedgerTransactionRejectReason.InvalidAccount;
                    break;
                case LedgerDatabase.TransactionRejectReason.InsufficientFunds:
                    RejectReason = LedgerTransactionRejectReason.InsufficientFunds;
                    break;
                case LedgerDatabase.TransactionRejectReason.InvalidFreezeID:
                    RejectReason = LedgerTransactionRejectReason.InvalidFreezeID;
                    break;
            }
        }
    }
}