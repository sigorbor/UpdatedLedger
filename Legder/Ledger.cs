using System;
using System.Collections.Generic;
using LedgerAPI;
using LedgerDatabase;

namespace LedgerLogic
{
    using AccountID = UInt64;
    using FreezeID = UInt64;

    public class Ledger : LedgerAPI.ILedger
    {
        private LedgerDatabase.AccountsTable accountsTable;
        private LedgerDatabase.TransactionsTable transactionsTable;
        private LedgerDatabase.FrozenBalancesTable frozenBalancesTable;
        private UInt64 transferID;

        public Ledger()
        {
            accountsTable = new LedgerDatabase.AccountsTable();
            transactionsTable = new LedgerDatabase.TransactionsTable();
            frozenBalancesTable = new LedgerDatabase.FrozenBalancesTable();
            transferID = 0;
         }

        LedgerOperationStatus ILedger.CreateAccount(out AccountID id)
        {
            AccountLine accLine = new AccountLine
            {
                Balance = 0
            };
            id = accountsTable.Insert(accLine);
            
            return LedgerOperationStatus.Success;
        }

        LedgerOperationStatus ILedger.GetAccountBalance(AccountID accountID, out decimal balance)
        {
            LedgerOperationStatus status = LedgerOperationStatus.Success;
            if (accountsTable.Contains(accountID))
            {
                AccountLine account = accountsTable.Select(accountID);
                lock (account)
                {
                    balance = account.Balance;
                }
            }
            else
            {
                balance = 0;
                status = LedgerOperationStatus.InvalidAccount; 
            }
            return status;
        }

        LedgerOperationStatus ILedger.AddFunds(AccountID accountID, decimal balance)
        {
            LedgerOperationStatus status = LedgerOperationStatus.Success;
            TransactionLine transaction = new TransactionLine(  accountID, 
                                                                balance, 
                                                                LedgerDatabase.TransactionType.AddFunds);
            //find account line, lock it, update
            if (accountsTable.Contains(accountID))
            {
                AccountLine account = accountsTable.Select(accountID);
                lock (account)
                {
                    account.Balance += balance;
                }
            }
            else
            {
                transaction.Status = LedgerDatabase.TransactionStatus.Rejected;
                transaction.RejectReason = LedgerDatabase.TransactionRejectReason.InvalidAccount;
                status = LedgerOperationStatus.InvalidAccount;
            }

            transactionsTable.Insert(transaction);
            return status;
        }

        LedgerOperationStatus ILedger.RemoveFunds(AccountID accountID, decimal balance)
        {
            LedgerOperationStatus status = LedgerOperationStatus.Success;
            TransactionLine transaction = new TransactionLine(  accountID,
                                                                balance,
                                                                LedgerDatabase.TransactionType.RemoveFunds);
            //find account line, lock it, update
            if (accountsTable.Contains(accountID))
            {
                AccountLine account = accountsTable.Select(accountID);
                lock (account)
                {
                    if (balance > account.Balance)
                    {
                        transaction.Status = LedgerDatabase.TransactionStatus.Rejected;
                        transaction.RejectReason = TransactionRejectReason.InsufficientFunds;
                        status = LedgerOperationStatus.InsufficientFunds;
                    }
                    else
                    {
                        account.Balance -= balance;
                    }
                }
            }
            else
            {
                transaction.Status = LedgerDatabase.TransactionStatus.Rejected;
                transaction.RejectReason = TransactionRejectReason.InvalidAccount;
                status = LedgerOperationStatus.InvalidAccount;
            }

            transactionsTable.Insert(transaction);
            return status;
        }

        LedgerOperationStatus ILedger.TransferFunds(AccountID srcAccountID, AccountID tgtAccountID, decimal balance)
        {
            LedgerOperationStatus status = LedgerOperationStatus.Success;
            TransactionLine srcTrans = new TransactionLine(srcAccountID,
                                                           balance,
                                                           LedgerDatabase.TransactionType.RemoveFunds, TransactionSubType.TransferFunds);
            TransactionLine tgtTrans = new TransactionLine(tgtAccountID,
                                                           balance,
                                                           LedgerDatabase.TransactionType.AddFunds, TransactionSubType.TransferFunds);
            lock (transactionsTable)
            {
                srcTrans.TransferId = tgtTrans.TransferId = ++transferID;
            }

            if (accountsTable.Contains(srcAccountID) && accountsTable.Contains(tgtAccountID))
            {
                AccountLine accSrc = accountsTable.Select(srcAccountID);
                AccountLine accTgt = accountsTable.Select(tgtAccountID);

                AccountLine firstAccToLock = accSrc, secondAccToLock = accTgt;
                if (srcAccountID > tgtAccountID) // avoid deadlocks by always locking at the same order
                {
                    firstAccToLock = accTgt;
                    secondAccToLock = accSrc;
                }

                lock (firstAccToLock)
                {
                    lock (secondAccToLock)
                    {
                        if (balance > accSrc.Balance)
                        {
                            srcTrans.Status = tgtTrans.Status = LedgerDatabase.TransactionStatus.Rejected;
                            srcTrans.RejectReason = tgtTrans.RejectReason = TransactionRejectReason.InsufficientFunds;
                            status = LedgerOperationStatus.InsufficientFunds;
                        }
                        else
                        {
                            accSrc.Balance -= balance;
                            accTgt.Balance += balance;
                        }
                    }
                }
            } else
            {
                srcTrans.Status = tgtTrans.Status = LedgerDatabase.TransactionStatus.Rejected;
                srcTrans.RejectReason = tgtTrans.RejectReason = TransactionRejectReason.InvalidAccount;
                status = LedgerOperationStatus.InvalidAccount;
            }

            transactionsTable.Insert(srcTrans);
            transactionsTable.Insert(tgtTrans);
            return status;
        }

        LedgerOperationStatus ILedger.FreezeFunds(AccountID accountID, decimal balance, out FreezeID freezeID)
        {
            LedgerOperationStatus status = LedgerOperationStatus.Success;
            //prepare RemoveFunds/Freeze transaction
            TransactionLine transaction = new TransactionLine(  accountID,
                                                                balance,
                                                                LedgerDatabase.TransactionType.RemoveFunds, TransactionSubType.Freeze);
            if (accountsTable.Contains(accountID))
            {
                AccountLine account = accountsTable.Select(accountID);
                lock (account)
                {
                    if (balance > account.Balance)
                    {
                        freezeID = 0;
                        transaction.Status = LedgerDatabase.TransactionStatus.Rejected;
                        transaction.RejectReason = TransactionRejectReason.InsufficientFunds;
                        status = LedgerOperationStatus.InsufficientFunds;
                    }
                    else
                    {
                        //update account balance and add freeze record
                        account.Balance -= balance;
                        FrozenBalanceLine frozenBalance = new FrozenBalanceLine(accountID, balance);
                        freezeID = frozenBalancesTable.Insert(frozenBalance);
                    }
                }
            }
            else
            {
                freezeID = 0;
                transaction.Status = LedgerDatabase.TransactionStatus.Rejected;
                transaction.RejectReason = TransactionRejectReason.InvalidAccount;
                status = LedgerOperationStatus.InvalidAccount;
            }

            transactionsTable.Insert(transaction);
            return status;
        }

        LedgerOperationStatus ILedger.UnfreezeFunds(AccountID accountID, out decimal balance, UInt64 freezeID)
        {
            LedgerOperationStatus status = LedgerOperationStatus.Success;
            //prepare AddFunds/Unfreeze transaction
            TransactionLine transaction = new TransactionLine(  accountID,
                                                                0,
                                                                LedgerDatabase.TransactionType.AddFunds, TransactionSubType.Unfreeze);
            if (accountsTable.Contains(accountID))
            {
                AccountLine account = accountsTable.Select(accountID);
                lock (account)
                {
                    if (frozenBalancesTable.Contains(freezeID))
                    {
                        FrozenBalanceLine frozenBalance = frozenBalancesTable.Select(freezeID);
                        balance = frozenBalance.Balance;
                        if (frozenBalance.AccountId != accountID)
                        {
                            transaction.Status = LedgerDatabase.TransactionStatus.Rejected;
                            transaction.RejectReason = TransactionRejectReason.InvalidAccount;
                            status = LedgerOperationStatus.InvalidAccount;
                        }
                        else
                        {   
                            //update account balance and delete freeze record
                            account.Balance += frozenBalance.Balance;
                            frozenBalancesTable.Delete(freezeID);
                        }
                    } else
                    {
                        balance = 0;
                        transaction.Status = LedgerDatabase.TransactionStatus.Rejected;
                        transaction.RejectReason = TransactionRejectReason.InvalidFreezeID;
                        status = LedgerOperationStatus.InvalidFreezeID;
                    }
                }
                
            } else
            {
                balance = 0;
                transaction.Status = LedgerDatabase.TransactionStatus.Rejected;
                transaction.RejectReason = TransactionRejectReason.InvalidAccount;
                status = LedgerOperationStatus.InvalidAccount;
            }
            
            transactionsTable.Insert(transaction);
            return status;
        }

        List<ITransactionData> ILedger.GetLedger()
        {
            List<ITransactionData> result = new List<ITransactionData>();
            ICollection<TransactionLine> transactions = transactionsTable.SelectAll();
            foreach (var line in transactions)
            {
                TransactionData data = new TransactionData(line);
                result.Add(data);
            }
            return result;
        }
    }


}
