using Hangfire;
using NUnit.Framework;
using RethinkDb;
using RethinkDb.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Web.Mvc;

namespace com.garysieling.database
{  
    /**
     * These classes act upon a group of objects, but treating the first as canonical;
     */
    public class Proxy
    {
        private List<Exception> _errors = new List<Exception>();
        private static int _order = 0;
        private DateTime? _startTime = null;
        protected String _productionDatabase;


        public void saveError(Exception e) 
        {
            _errors.Add(e);
        }

        public void throwErrors() 
        {
            if (_errors.Count > 0)
            {
                foreach (Exception e in _errors)
                {
                    if (e.Data["database"].Equals(_productionDatabase))
                    {
                        throw e;
                    }
                }
            }
        }

        public void StartTiming() 
        {
            _startTime = DateTime.Now;
        }

        private static ConcurrentQueue<TimingRecord> _buffer = new ConcurrentQueue<TimingRecord>();
        public void Buffer(String SessionId,
              int UserId,
              String DbId,
              String Description,
              String Query,
              String QueryParms,
              String ErrorMessage = null)
        {
            double? duration = null;

            if (_startTime != null)
            {
                duration = (DateTime.Now - _startTime).Value.Ticks;
                _startTime = null;    
            }

            String domain = "";
            String IP = "";
            try 
            {
                domain = System.Web.HttpContext.Current.Request.UserHostName;
                IP = System.Web.HttpContext.Current.Request.UserHostAddress;
            } 
            catch (Exception e) 
            {

            }

            _buffer.Enqueue(
                new TimingRecord
                {
                    SessionId = SessionId,
                    UserId = UserId,
                    DbId = DbId,
                    RunDate = DateTime.Now,
                    Order = _order++,
                    Description = Description,
                    Domain = domain,
                    IP = IP,
                    Duration = duration,
                    Query = Query,
                    QueryParms = QueryParms,
                    ErrorMessage = ErrorMessage
                });

            if (_buffer.Count >= 50)
            {
                ConcurrentQueue<TimingRecord> dataToSend;
                lock (_buffer)
                {
                    dataToSend = _buffer;
                    _buffer = new ConcurrentQueue<TimingRecord>();
                }

                BackgroundJob.Enqueue(() => SaveTimingLog(dataToSend.ToArray()));            
            }
        }

        public ActionResult SaveTimingLog(TimingRecord[] timings)
        {
            var ConnectionFactory = ConfigurationAssembler.CreateConnectionFactory("logging");
            try
            {
                using (var connection = ConnectionFactory.Get())
                {
                    var table = Query.Db("performance").Table<TimingRecord>("query_timings");

                    connection.Run(table.Insert(timings));

                    connection.Dispose();
                }
            }
            catch (Exception e)
            {
                var failResult = new JsonResult();
                failResult.Data = new { Message = e.Message };
                return failResult;

            }

            var successResult = new JsonResult();
            successResult.Data = "Success";

            return successResult;
        }
    }
}
