using System;
using System.Collections.Generic;

namespace LedgerAPI
{
    using AccountID = UInt64;
    using TransferID = UInt64;
    using FreezeID = UInt64;

    public enum LedgerOperationStatus { Success = 1, InvalidAccount, InvalidFreezeID, InsufficientFunds };
    public enum LedgerTransactionApproval { Approved = 1, Rejected };
    public enum LedgerTransactionType { AddFunds = 1, RemoveFunds};
    public enum LedgerTransactionSubType { Regular = 1, TransferFunds, Freeze, Unfreeze };
    public enum LedgerTransactionRejectReason { NotRelevant = 1, InvalidAccount, InsufficientFunds, InvalidFreezeID };


    public interface ITransactionData
    {
        AccountID AccountId { get; }
        decimal Balance { get; }
        LedgerTransactionType Type { get; }
        LedgerTransactionSubType SubType { get; }
        DateTime Timestamp { get; }
        LedgerTransactionApproval Status { get; }
        LedgerTransactionRejectReason RejectReason { get; }
        TransferID TransferId { get; }

        string ToString();
    }

    public interface ILedger
    {
        LedgerOperationStatus CreateAccount(out AccountID accountID);
        LedgerOperationStatus GetAccountBalance(AccountID accountID, out decimal balance);
        LedgerOperationStatus AddFunds(AccountID accountID, decimal balance);
        LedgerOperationStatus RemoveFunds(AccountID accountID, decimal balance);
        LedgerOperationStatus TransferFunds(AccountID srcAccountID, AccountID tgtAccountID, decimal balance);
        LedgerOperationStatus FreezeFunds(AccountID accountID, decimal balance, out FreezeID freezeID);
        LedgerOperationStatus UnfreezeFunds(AccountID accountID, out decimal balance, FreezeID freezeID);
        List<ITransactionData> GetLedger();
    }

}
