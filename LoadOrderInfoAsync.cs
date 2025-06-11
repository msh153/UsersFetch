using System;
using System.Collections.Concurrent;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Services;

namespace LoadOrder
{
    [WebMethod]
    // optimal for web services to use async/await so as to not block thread but that's not about WebMethod(ASMX)
    public Order LoadOrderInfoAsync(string orderCode)
    {
        //  At the realease build shouldn't be orderCode as null or emtpy too
        //    Debug.Assert( null != orderCode && orderCode != "" ); 
        if (string.IsNullOrWhiteSpace(orderCode))
            throw new ArgumentException("Order code must not be null or empty", nameof(orderCode));

        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (_cache.TryGetValue(orderCode, out var cachedOrder))
            {
                LogElapsed(stopwatch); // in separate method for reducing duplication
                return cachedOrder;
            }

            //using var connection = new SqlConnection(_connectionString); // for c# 8

            using (var connection = new SqlConnection(_connectionString)) // using for safety from memory leaks
            {
                connection.Open();
                using (var command = new SqlCommand(
                    "SELECT OrderID, CustomerID, TotalMoney FROM dbo.Orders WHERE OrderCode = @orderCode",
                    connection)) // better to use ORM, if schema changes there is a need to modify string query
                {
                    //string.Format injects orderCode into the query string that leads to sql injections
                    command.Parameters.Add("@orderCode", SqlDbType.NVarChar, 50).Value = orderCode; // type safety, explicit type usage

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var order = new Order(
                                reader["OrderID"]?.ToString() ?? "",
                                reader["CustomerID"]?.ToString() ?? "",
                                reader.GetInt32(reader.GetOrdinal("TotalMoney"))); // named columns instead of blind cast [0]

                            // TryAdd instead of ContainsKey more effective and threadsafe
                            _cache.TryAdd(orderCode, order);
                            LogElapsed(stopwatch);
                            return order;
                        }
                    }
                }
            }

            LogElapsed(stopwatch);
            return null;
        }
        catch (SqlException ex)
        {
            _logger.Log("Database access error", ex.Message); // message should be informative
            throw new ApplicationException("Database access error", ex);
        }
        finally
        {
            stopwatch.Stop();
            _logger.Log("INFO", $"Elapsed - {stopwatch.Elapsed}");
        }
    }

    private void LogElapsed(Stopwatch stopwatch)
    {
        stopwatch.Stop();
        _logger.Log("INFO", $"Elapsed - {stopwatch.Elapsed}");
    }

    // Fields
    private readonly ConcurrentDictionary<string, Order> _cache = new(); //thread safety
    private readonly string _connectionString;
    private readonly ILogger _logger;
}
