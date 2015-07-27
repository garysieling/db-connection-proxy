using Hangfire;
using Monad;
using NUnit.Framework;
using RethinkDb;
using RethinkDb.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using QueryResults = System.Tuple<string, Monad.Either<System.Data.SqlClient.SqlDataReader, System.Exception>>;

namespace com.garysieling.database
{  
    public class DbDataProxyParameter: Proxy, IDbDataParameter 
    {
        List<IDbDataParameter> _params = new List<IDbDataParameter>();
        public DbDataProxyParameter(String primaryDb)
        {
            this._productionDatabase = primaryDb;
        }

        public void Add(IDbDataParameter param)
        {
            _params.Add(param);
        }

        public byte Precision
        {
            get
            {
                return _params[0].Precision; // Assume first is primary DB
            }
            set
            {
                _params.ForEach((p) => p.Precision = value);
            }
        }

        public byte Scale
        {
            get
            {
                return _params[0].Scale;
            }
            set
            {
                _params.ForEach((p) => p.Scale = value);
            }
        }

        public int Size
        {
            get
            {
                return _params[0].Size;
            }
            set
            {
                _params.ForEach((p) => p.Size = value);
            }
        }

        public DbType DbType
        {
            get
            {
                return _params[0].DbType;
            }
            set
            {
                _params.ForEach((p) => p.DbType = value);
            }
        }

        public ParameterDirection Direction
        {
            get
            {
                return _params[0].Direction;
            }
            set
            {
                _params.ForEach((p) => p.Direction = value);
            }
        }

        public bool IsNullable
        {
            get { return _params[0].IsNullable; }
        }

        public string ParameterName
        {
            get
            {
                return _params[0].ParameterName;
            }
            set
            {
                _params.ForEach((p) => p.ParameterName = value);
            }
        }

        public string SourceColumn
        {
            get
            {
                return _params[0].SourceColumn;
            }
            set
            {
                _params.ForEach((p) => p.SourceColumn = value);
            }
        }

        public DataRowVersion SourceVersion
        {
            get
            {
                return _params[0].SourceVersion;
            }
            set
            {
                _params.ForEach((p) => p.SourceVersion = value);
            }
        }

        public object Value
        {
            get
            {
                return _params[0].Value;
            }
            set
            {
                _params.ForEach((p) => p.Value = value);
            }
        }
    }
}
