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
    public class TermDepositsController : ApiController
    {
        private BankDbEntities db = new BankDbEntities();

        // GET: api/TermDeposits
        public IQueryable<TermDeposit> GetTermDeposits()
        {
            return db.TermDeposits;
        }

        // GET: api/TermDeposits/5
        [ResponseType(typeof(TermDeposit))]
        public IHttpActionResult GetTermDeposit(int id)
        {
            TermDeposit termDeposit = db.TermDeposits.Find(id);
            if (termDeposit == null)
            {
                return NotFound();
            }

            return Ok(termDeposit);
        }

        // Get: api/MemberDeposits/1
        [HttpGet]
        [Route("api/MemberDeposits/{memberId}")]
        public IList<TermDeposit> GetMemberDeposits(int memberId)
        {
            var tds = db.TermDeposits
                .Where(td => td.MemberId == memberId)
                .ToList<TermDeposit>();
            return tds;
        }

        // PUT: api/TermDeposits/5
        [ResponseType(typeof(void))]
        public IHttpActionResult PutTermDeposit(int id, TermDeposit termDeposit)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != termDeposit.TermDepositId)
            {
                return BadRequest();
            }

            db.Entry(termDeposit).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TermDepositExists(id))
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

        // POST: api/TermDeposits
        [ResponseType(typeof(TermDeposit))]
        public IHttpActionResult PostTermDeposit(TermDepositInfo termDeposit)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            termDeposit.StartDate = DateTime.Now;
            termDeposit.EndDate = termDeposit.StartDate.AddMonths(termDeposit.Duration_Months);

            TermDeposit td = new TermDeposit
            {
                TermDepositId = termDeposit.TermDepositId,
                MemberId = termDeposit.MemberId,
                Amount = termDeposit.Amount,
                Duration_Months = termDeposit.Duration_Months,
                StartDate = termDeposit.StartDate,
                EndDate = termDeposit.EndDate
            };

            var account = db.Accounts.Find(termDeposit.AccountNumber);
            //withdraw money
            account.Withdraw(termDeposit.Amount);


            db.TermDeposits.Add(td);
            db.SaveChanges();

            return CreatedAtRoute("DefaultApi", new { id = termDeposit.TermDepositId }, termDeposit);
        }

        // DELETE: api/TermDeposits/5
        [ResponseType(typeof(TermDeposit))]
        public IHttpActionResult DeleteTermDeposit(int id)
        {
            TermDeposit termDeposit = db.TermDeposits.Find(id);
            if (termDeposit == null)
            {
                return NotFound();
            }

            db.TermDeposits.Remove(termDeposit);
            db.SaveChanges();

            return Ok(termDeposit);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool TermDepositExists(int id)
        {
            return db.TermDeposits.Count(e => e.TermDepositId == id) > 0;
        }
    }

    public class TermDepositInfo
    {
        public int TermDepositId { get; set; }
        public int MemberId { get; set; }
        public decimal Amount { get; set; }
        public int Duration_Months { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int AccountNumber { get; set; }
    }
}