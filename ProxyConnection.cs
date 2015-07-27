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
    public class ProxyDbConnection : Proxy, IDbConnection
    {
        private List<IDbConnection> _connections;
        private IDbConnection _firstConnection;
        private String _sessionId = Guid.NewGuid().ToString();

        public ProxyDbConnection(List<IDbConnection> connections)
        {
            _connections = connections;
            _firstConnection = _connections[0];
            _productionDatabase = _connections[0].Database;
        }

        public IDbTransaction BeginTransaction(IsolationLevel il)
        {
            IDbTransaction result = null;

            foreach (var _connection in _connections)
            {
                try
                {
                    result = _connection.BeginTransaction(il);
                }
                catch (Exception e)
                {
                    saveError(e);
                }
            }

            throwErrors();

            return result;
        }

        public IDbTransaction BeginTransaction()
        {
            IDbTransaction result = null;

            foreach (var _connection in _connections)
            {
                try
                {
                    result = _connection.BeginTransaction();
                }
                catch (Exception e)
                {
                    saveError(e);
                }
            }

            throwErrors();

            return result;
        }

        public void ChangeDatabase(string databaseName)
        {
            foreach (var _connection in _connections)
            {
                try
                {
                    _connection.ChangeDatabase(databaseName);
                }
                catch (Exception e)
                {
                    saveError(e);
                }
            }

            throwErrors();
        }

        public void Close()
        {

            foreach (var _connection in _connections)
            {
                try
                {
                    _connection.Close();
                }
                catch (Exception e)
                {
                    saveError(e);
                }
            }

            throwErrors();
        }

        public string ConnectionString
        {
            get
            {
                return _firstConnection.ConnectionString;
            }
            set
            {
                _firstConnection.ConnectionString = value;
            }
        }

        public int ConnectionTimeout
        {
            get { return _firstConnection.ConnectionTimeout; }
        }

        public IDbCommand CreateCommand()
        {
            var commands = new List<IDbCommand>();
            foreach (var _connection in _connections)
            {
                try
                {
                    commands.Add(_connection.CreateCommand());
                }
                catch (Exception e)
                {
                    saveError(e);
                }
            }

            throwErrors();

            return new ProxyDbCommand(commands, _sessionId);
        }

        public string Database
        {
            get { return _firstConnection.Database; }
        }

        public void Open()
        {
            foreach (var _connection in _connections)
            {
                try
                {
                    _connection.Open();
                }
                catch (Exception e)
                {
                    saveError(e);
                }
            }

            throwErrors();
        }

        public ConnectionState State
        {
            get { return _firstConnection.State; }
        }

        public void Dispose()
        {
            foreach (var _connection in _connections)
            {
                try
                {
                    _connection.Dispose();
                }
                catch (Exception e)
                {
                    saveError(e);
                }
            }

            throwErrors();
        }
    }
}
