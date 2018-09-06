using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.Description;
using BankApi.Models;

namespace BankApi.Controllers
{
    [EnableCors(origins: "http://localhost:4200", headers: "*", methods: "*")]
    [Authorize]
    public class TransactionsController : ApiController
    {
        private BankDbEntities db = new BankDbEntities();

        // GET: api/Transactions
        public IQueryable<Transaction> GetTransactions()
        {
            return db.Transactions;
        }

        // GET: api/Transactions/5
        [ResponseType(typeof(Transaction))]
        public IHttpActionResult GetTransaction(int id)
        {
            Transaction transaction = db.Transactions.Find(id);
            if (transaction == null)
            {
                return NotFound();
            }

            return Ok(transaction);
        }

        // GET: api/AccountTransactions/1
        [HttpGet]
        [Route("api/AccountTransactions/{accountNumber}")]
        public IList<Transaction> GetAccountTransactions(int accountNumber)
        {
            var transactions = db.Transactions
                .Where(a => a.AccountNumber == accountNumber)
                .ToList<Transaction>();
            return transactions;
        }

        // PUT: api/Transactions/5
        [ResponseType(typeof(void))]
        public IHttpActionResult PutTransaction(int id, Transaction transaction)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != transaction.TransactionId)
            {
                return BadRequest();
            }

            db.Entry(transaction).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TransactionExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST: api/Transactions
        [ResponseType(typeof(Transaction))]
        public IHttpActionResult PostTransaction(Transaction transaction)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            transaction.TransactionDate = DateTime.Now;

            var account = db.Accounts.Find(transaction.AccountNumber);
            var accountTransfer = db.Accounts.Find(transaction.AccountForTransfer);

            //transaction types
            var type = transaction.TransTypeId;
            switch (type)
            {
                case 1: //Deposit
                    //if amount is negative, return error
                    if(transaction.Amount < 0)
                    {
                        return BadRequest("Amount should be positive");
                    }
                    account.Deposit(transaction.Amount);
                    break;
                case 2: //Withdraw
                    if(account.AccountTypeId ==1 && transaction.Amount > account.CurrentBalance)
                    {
                        //cannot make transaction
                    }
                    else if(account.AccountTypeId == 2 && transaction.Amount > account.CurrentBalance)
                    {
                        //apply overdraft if account goes negative
                        account.Withdraw(transaction.Amount);
                        decimal overdraft = 35;
                        if(account.CurrentBalance < 0)
                        {
                            account.Overdraft = true;
                            account.Withdraw(overdraft);
                        }
                        transaction.Amount = transaction.Amount * -1;
                    }
                    else
                    {
                        //make normal withdrawal
                        account.Withdraw(transaction.Amount);
                        transaction.Amount = transaction.Amount * -1;
                    }
                    break;
                case 3: //Transfer
                    if(accountTransfer == null)
                    {
                        return BadRequest("Account for Transfer is missing");
                    }
                    //check if same account
                    //check if enough money

                    //make transactions to both accounts
                    account.Withdraw(transaction.Amount);
                    accountTransfer.Deposit(transaction.Amount);

                    //add transfer row to other account
                    Transaction transaction2 = new Transaction
                    {
                        TransTypeId = 3,
                        AccountNumber = accountTransfer.AccountNumber,
                        TransactionDate = DateTime.Now,
                        Amount = transaction.Amount
                    };
                    transaction.Amount = transaction.Amount * -1;
                    db.Transactions.Add(transaction2);
                    break;
            }

            db.Transactions.Add(transaction);
            db.SaveChanges();

            return CreatedAtRoute("DefaultApi", new { id = transaction.TransactionId }, transaction);
        }

        // DELETE: api/Transactions/5
        [ResponseType(typeof(Transaction))]
        public IHttpActionResult DeleteTransaction(int id)
        {
            Transaction transaction = db.Transactions.Find(id);
            if (transaction == null)
            {
                return NotFound();
            }

            db.Transactions.Remove(transaction);
            db.SaveChanges();

            return Ok(transaction);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool TransactionExists(int id)
        {
            return db.Transactions.Count(e => e.TransactionId == id) > 0;
        }
    }
}