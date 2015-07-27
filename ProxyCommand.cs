using Hangfire;
using Monad;
using NUnit.Framework;
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
    public class ProxyDbCommand : Proxy, IDbCommand
    {
        private List<IDbCommand> _commands;
        private IDbCommand _firstCommand; // just for gets

        private String _sessionId;

        public ProxyDbCommand()
        {
        }

        public ProxyDbCommand(List<IDbCommand> commands, String sessionId)
        {
            _commands = commands;
            _firstCommand = commands[0];
            _productionDatabase = _firstCommand.Connection.Database;

            _sessionId = sessionId;
        }

        public void Cancel()
        {
            foreach (var _command in _commands)
            {
                try
                {
                    _command.Cancel();
                }
                catch (Exception e)
                {
                    saveError(e);
                }
            }

            throwErrors();
        }

        public string CommandText
        {
            get
            {
                return _firstCommand.CommandText;
            }
            set
            {
                foreach (var _command in _commands)
                {
                    try
                    {
                        _command.CommandText = value;
                    }
                    catch (Exception e)
                    {
                        saveError(e);
                    }
                }

                throwErrors();
            }
        }

        public int CommandTimeout
        {
            get
            {
                return _firstCommand.CommandTimeout;
            }
            set
            {
                foreach (var _command in _commands)
                {
                    try
                    {
                        _command.CommandTimeout = value;
                    }
                    catch (Exception e)
                    {
                        saveError(e);
                    }
                }

                throwErrors();
            }
        }

        public CommandType CommandType
        {
            get
            {
                return _firstCommand.CommandType;
            }
            set
            {
                foreach (var _command in _commands)
                {
                    try
                    {
                        _command.CommandType = value;
                    }
                    catch (Exception e)
                    {
                        saveError(e);
                    }
                }

                throwErrors();
            }
        }

        public IDbConnection Connection
        {
            get
            {
                return _firstCommand.Connection; // no way to get this right...
            }
            set
            {
                foreach (var _command in _commands)
                {
                    try
                    {
                        _command.Connection = value;
                    }
                    catch (Exception e)
                    {
                        saveError(e);
                    }
                }

                throwErrors();
            }
        }

        public IDbDataParameter CreateParameter()
        {
            DbDataProxyParameter result = new DbDataProxyParameter(_productionDatabase);

            foreach (var _command in _commands)
            {
                try
                {
                    result.Add(_command.CreateParameter());
                }
                catch (Exception e)
                {
                    saveError(e);
                }
            }

            throwErrors();

            return result;
        }

        public int ExecuteNonQuery()
        {
            int result = 0;
            int found = 0;

            foreach (var _command in _commands)
            {
                StartTiming();

                try
                {
                    result = _command.ExecuteNonQuery();

                }
                catch (Exception e)
                {
                    saveError(e);
                }

                Buffer(_sessionId, -1, _command.Connection.Database, "ExecuteNonQuery", _command.CommandText, _command.Parameters != null ? _command.Parameters.GetHashCode() + "" : "-1");
            }

            throwErrors();

            return result;
        }

        public delegate void AddResult(ref QueryResults result);

        public IDataReader ExecuteReader(CommandBehavior behavior)
        {
            SqlDataReader rows = null;

            var results = new List<QueryResults>();
            AddResult adder = (ref QueryResults result) =>
            {
                lock (results)
                {
                    results.Add(result);
                }
            };


            var queryResults =
                Parallel.ForEach(
                    _commands,
                    (_command) =>
                    {
                        StartTiming();

                        try
                        {
                            var q = new QueryResults(_command.Connection.Database, () =>
                            {
                                try
                                {
                                    return (SqlDataReader)_command.ExecuteReader(behavior);
                                }
                                finally
                                {
                                    // This isn't going to contain the whole read time
                                    Buffer(_sessionId, -1, _command.Connection.Database, "ExecuteReader(1)", _command.CommandText, _command.Parameters != null ? _command.Parameters.GetHashCode() + "" : "-1");
                                }
                            });

                            adder(ref q);
                        }
                        catch (Exception e)
                        {
                            e.Data.Add("database", _command.Connection.Database);

                            Buffer(_sessionId, -1, _command.Connection.Database, "ExecuteReader(1)", _command.CommandText, _command.Parameters != null ? _command.Parameters.GetHashCode() + "" : "-1", e.Message);

                            QueryResults q = new QueryResults(_command.Connection.Database, () => e);

                            adder(ref q);
                        }
                        finally
                        {
                        }
                    });

            throwErrors();

            Assert.AreEqual(_commands.Count, results.Count);

            foreach (var tuple in results)
            {
                var queryResult = tuple.Item2;

                var lambdaResult = queryResult.Invoke();
                if (tuple.Item1.Equals(_productionDatabase))
                {
                    if (lambdaResult.IsLeft)
                    {
                        var resultData = lambdaResult.Left;
                        rows = resultData;
                    }
                }
                else
                {
                    try
                    {
                        if (lambdaResult.IsLeft)
                        {
                            var resultData = lambdaResult.Left;
                            var reader = resultData;

                            reader.Close();
                        }
                    }
                    catch (Exception e)
                    {

                    }
                }
            }

            return rows;
        }

        public IDataReader ExecuteReader()
        {
            IDataReader result = null;

            foreach (var _command in _commands)
            {
                StartTiming();

                try
                {
                    result = _command.ExecuteReader();
                }
                catch (Exception e)
                {
                    saveError(e);
                }

                Buffer(_sessionId, -1, _command.Connection.Database, "ExecuteReader(2)", _command.CommandText, _command.Parameters != null ? _command.Parameters.GetHashCode() + "" : "-1");
            }

            throwErrors();

            return result;
        }

        public object ExecuteScalar()
        {
            object result = null;

            foreach (var _command in _commands)
            {
                StartTiming();

                try
                {
                    result = _command.ExecuteScalar();
                }
                catch (Exception e)
                {
                    saveError(e);
                }

                Buffer(_sessionId, -1, _command.Connection.Database, "ExecuteReader", _command.CommandText, _command.Parameters != null ? _command.Parameters.GetHashCode() + "" : "-1");
            }

            throwErrors();

            return result;
        }

        public IDataParameterCollection Parameters
        {
            get
            {
                IDataParameterCollection result = null;

                foreach (var _command in _commands)
                {
                    try
                    {
                        result = _command.Parameters;
                    }
                    catch (Exception e)
                    {
                        saveError(e);
                    }
                }

                throwErrors();

                return result;
            }
        }

        public void Prepare()
        {
            foreach (var _command in _commands)
            {
                try
                {
                    _command.Prepare();
                }
                catch (Exception e)
                {
                    saveError(e);
                }
            }

            throwErrors();
        }

        public IDbTransaction Transaction
        {
            get
            {
                return _firstCommand.Transaction; // no good way to get this right...
            }
            set
            {
                _firstCommand.Transaction = value;
            }
        }

        public UpdateRowSource UpdatedRowSource
        {
            get
            {
                UpdateRowSource result = UpdateRowSource.Both;

                foreach (var _command in _commands)
                {
                    try
                    {
                        result = _command.UpdatedRowSource;
                    }
                    catch (Exception e)
                    {
                        saveError(e);
                    }
                }

                throwErrors();

                return result;
            }
            set
            {
                foreach (var _command in _commands)
                {
                    try
                    {
                        _command.UpdatedRowSource = value;
                    }
                    catch (Exception e)
                    {
                        saveError(e);
                    }
                }

                throwErrors();
            }
        }

        public void Dispose()
        {
            foreach (var _command in _commands)
            {
                try
                {
                    _command.Dispose();
                }
                catch (Exception e)
                {
                    saveError(e);
                }
            }

            throwErrors();
        }

        public void eachReader(Action<IDbCommand, object> paramReader, object parameters)
        {
            foreach (var _command in _commands)
            {
                try
                {
                    paramReader(_command, parameters);
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
