using System;
using LedgerAPI;
using LedgerLogic;
using System.Threading;

namespace TestClient
{
    using AccountID = UInt64;
    using FreezeID = UInt64;

    class TestClient
    {
        static void ExpectSuccess(LedgerOperationStatus status)
        {
            if(status != LedgerOperationStatus.Success)
            {
                throw new Exception("Operation failed with status " + status + ", expected Success");
            }
        }

        static void ExpectTrue(bool cond)
        {
            if (!cond)
            {
                throw new Exception("Operation failed, expected TRUE");
            }
        }

        static void Main(string[] args)
        {
            ILedger ledgerAPI = new Ledger();

            //CHECK THE FUNCTIONALITY

            decimal balance = -1;
            AccountID firstAcc, secondAcc;
            ExpectSuccess(ledgerAPI.CreateAccount(out firstAcc));
            ExpectSuccess(ledgerAPI.AddFunds(firstAcc, 1000));
            ExpectSuccess(ledgerAPI.CreateAccount(out secondAcc));
            ExpectSuccess(ledgerAPI.AddFunds(secondAcc, 6000));
            ExpectSuccess(ledgerAPI.RemoveFunds(secondAcc, 4000));
            ExpectTrue(ledgerAPI.RemoveFunds(secondAcc, 3000) == LedgerOperationStatus.InsufficientFunds);
            ExpectTrue(ledgerAPI.AddFunds(13, 5000) == LedgerOperationStatus.InvalidAccount);
            ledgerAPI.GetAccountBalance(secondAcc, out balance);
            ExpectTrue(balance == 2000);
            
            FreezeID freezeId = 0;
            ExpectSuccess(ledgerAPI.FreezeFunds(secondAcc, 2000, out freezeId));
            ExpectTrue(freezeId != 0);
            FreezeID frozen = freezeId;
            ExpectTrue(ledgerAPI.FreezeFunds(secondAcc, 1000, out freezeId) == LedgerOperationStatus.InsufficientFunds);
            ExpectTrue(freezeId == 0);

            ExpectTrue(ledgerAPI.TransferFunds(secondAcc, firstAcc, 1000) == LedgerOperationStatus.InsufficientFunds);
            ExpectSuccess(ledgerAPI.TransferFunds(firstAcc, secondAcc, 1000));

            ledgerAPI.GetAccountBalance(secondAcc, out balance);
            ExpectTrue(balance == 1000);

            ExpectSuccess(ledgerAPI.UnfreezeFunds(secondAcc, out balance, frozen));
            ExpectTrue(balance == 2000);
            var ledger = ledgerAPI.GetLedger();
            ITransactionData td = ledger[ledger.Count - 1];
            ExpectTrue((td.SubType == LedgerTransactionSubType.Unfreeze && td.Status == LedgerTransactionApproval.Approved && td.RejectReason == LedgerTransactionRejectReason.NotRelevant && td.AccountId == secondAcc));

            ExpectTrue(ledgerAPI.UnfreezeFunds(secondAcc, out balance, frozen) == LedgerOperationStatus.InvalidFreezeID);
            ExpectTrue(balance == 0);

            ledger = ledgerAPI.GetLedger();
            td = ledger[ledger.Count - 1];
            ExpectTrue((td.SubType == LedgerTransactionSubType.Unfreeze && td.Status == LedgerTransactionApproval.Rejected && td.RejectReason == LedgerTransactionRejectReason.InvalidFreezeID && td.AccountId == secondAcc));

            ExpectSuccess(ledgerAPI.TransferFunds(secondAcc, firstAcc, 1000));
            ledger = ledgerAPI.GetLedger();
            var td1 = ledger[ledger.Count - 2];
            var td2 = ledger[ledger.Count - 1];
            ExpectTrue((td2.SubType == LedgerTransactionSubType.TransferFunds && td1.Status == LedgerTransactionApproval.Approved && td1.RejectReason == LedgerTransactionRejectReason.NotRelevant && td2.AccountId == firstAcc));

            foreach (var t in ledgerAPI.GetLedger())
            {
                Console.WriteLine(t.ToString());
            }

            //AND NOW GET THE SHIT OUT OF THE LEDGER AND TEST THREAD-SAFETY WITH 1000 threads!!!

            AccountID a1 = 0;
            AccountID a2 = 0;
            ledgerAPI.CreateAccount(out a1);
            ledgerAPI.CreateAccount(out a2);
            for (int i = 0; i <10; i++)
            {
                Thread thr = new Thread(new ThreadStart(() => {

                    decimal balance2 = -1;
                    ledgerAPI.AddFunds(a1, 1000);
                    ledgerAPI.AddFunds(a2, 6000);
                    ledgerAPI.RemoveFunds(a2, 4000);
                    ledgerAPI.RemoveFunds(a1, 3000);
                    ledgerAPI.AddFunds(a2, 5000);
                    ledgerAPI.GetAccountBalance(a2, out balance2);

                    FreezeID freezeId1, freezeId2 = 0;
                    ledgerAPI.FreezeFunds(a1, 2000, out freezeId1);
                    ledgerAPI.FreezeFunds(a2, 1000, out freezeId2);

                    ledgerAPI.TransferFunds(a1, a2, 1000);
                    ledgerAPI.TransferFunds(a2, a1, 1000);

                    ledgerAPI.GetAccountBalance(12, out balance2);
   
                    ledgerAPI.UnfreezeFunds(a1, out balance2, freezeId1);
                    ledgerAPI.UnfreezeFunds(a2, out balance2, freezeId2);

                    ledgerAPI.TransferFunds(a2, a1, 1000);
                }));
                thr.Start();
            }

            foreach (var t in ledgerAPI.GetLedger())
            {
                if (t.Status == LedgerTransactionApproval.Approved)
                {
                    Console.WriteLine(t.ToString());
                }
            }

            decimal b1 = 0, b2 = 0;
            ledgerAPI.GetAccountBalance(a1, out b1);
            ledgerAPI.GetAccountBalance(a2, out b2);
            ExpectTrue(b1 > 0);
            ExpectTrue(b2 > 0);
            ExpectTrue(b2 > b1);

            Console.WriteLine("a1 Balance: {0}, a2 Balance: {1}", b1, b2);

            Console.WriteLine("ALL IS GREAT");
            Console.ReadKey();
        }
    }
}