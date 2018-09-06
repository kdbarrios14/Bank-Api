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
    public class LoansController : ApiController
    {
        private BankDbEntities db = new BankDbEntities();

        // GET: api/Loans
        public IQueryable<Loan> GetLoans()
        {
            return db.Loans;
        }

        // GET: api/Loans/5
        [ResponseType(typeof(Loan))]
        public IHttpActionResult GetLoan(int id)
        {
            Loan loan = db.Loans.Find(id);
            if (loan == null)
            {
                return NotFound();
            }

            return Ok(loan);
        }

        //GET: api/MemberLoans/1
        [HttpGet]
        [Route("api/MemberLoans/{memberId}")]
        public IList<Loan> GetMemberLoans(int memberId)
        {
            var loan = db.Loans.
                        Where(c => c.MemberId == memberId)
                        .ToList<Loan>();
            return loan.ToList();
        }

        // PUT: api/Loans/5
        [ResponseType(typeof(void))]
        public IHttpActionResult PutLoan(int id, Loan loan)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != loan.LoanId)
            {
                return BadRequest();
            }

            db.Entry(loan).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!LoanExists(id))
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

        // POST: api/Loans
        [ResponseType(typeof(Loan))]
        public IHttpActionResult PostLoan(Loan loan)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (loan.IssueDate == null)
            {
                loan.IssueDate = new DateTime();
            }

            if (loan.PaymentDate == null)
            {
                loan.PaymentDate = new DateTime();
            }
            loan.IssueDate = DateTime.Now;
            DateTime date = DateTime.Now;
            loan.PaymentDate = date.AddDays(30);

            db.Loans.Add(loan);
            db.SaveChanges();

            return CreatedAtRoute("DefaultApi", new { id = loan.LoanId }, loan);
        }

        [Route("api/Loans/payment")]
        [HttpPost]
        public IHttpActionResult payment([FromBody]Payment pay)
        {
            var loan = db.Loans.Find(pay.LoanId);
            if (loan == null)
            {
                return Content(HttpStatusCode.OK, "Can not find the loan");
            }

            var account = db.Accounts.Find(pay.AccountNumber);
            if (account == null)
            {
                return Content(HttpStatusCode.OK, "Can not find the account");

            }

            if (loan.payment(pay.Amount))
            {
                account.Withdraw(pay.Amount);
                loan.PaymentDate = loan.PaymentDate.Value.AddDays(30);

                //make transaction stub
                Transaction transaction = new Transaction
                {
                    TransTypeId = 2,
                    AccountNumber = account.AccountNumber,
                    TransactionDate = DateTime.Now,
                    Amount = pay.Amount
                };
                transaction.Amount = transaction.Amount * -1;
                db.Transactions.Add(transaction);
                
                db.SaveChanges();
                return Ok();
            }

            return Content(HttpStatusCode.OK, "Fail to make a payment!");
        }

        // DELETE: api/Loans/5
        [ResponseType(typeof(Loan))]
        public IHttpActionResult DeleteLoan(int id)
        {
            Loan loan = db.Loans.Find(id);
            if (loan == null)
            {
                return NotFound();
            }

            db.Loans.Remove(loan);
            db.SaveChanges();

            return Ok(loan);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool LoanExists(int id)
        {
            return db.Loans.Count(e => e.LoanId == id) > 0;
        }
    }
    public class Payment
    {
        public int LoanId { get; set; }
        public decimal Amount { get; set; }
        public int AccountNumber { get; set; }
    }
}