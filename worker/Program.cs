using System;
using System.Data.Common;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Newtonsoft.Json;
using Npgsql;
using StackExchange.Redis;

namespace Worker
{
    public class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                string pgConnString = Environment.GetEnvironmentVariable("POSTGRES_CONN_STRING");
                string redisHost = Environment.GetEnvironmentVariable("REDIS_HOST");

                if (string.IsNullOrWhiteSpace(pgConnString))
                {
                    Console.Error.WriteLine("❌ Environment variable 'POSTGRES_CONN_STRING' is not set or empty.");
                    return 1;
                }

                if (string.IsNullOrWhiteSpace(redisHost))
                {
                    Console.Error.WriteLine("❌ Environment variable 'REDIS_HOST' is not set or empty.");
                    return 1;
                }

                Console.WriteLine($"✅ POSTGRES_CONN_STRING received (host masked): {MaskPgConnString(pgConnString)}");
                Console.WriteLine($"✅ REDIS_HOST received: {redisHost}");

                var pgsql = OpenDbConnection(pgConnString);
                var redisConn = OpenRedisConnection(redisHost);
                var redis = redisConn.GetDatabase();

                var keepAliveCommand = pgsql.CreateCommand();
                keepAliveCommand.CommandText = "SELECT 1";

                var definition = new { vote = "", voter_id = "" };

                while (true)
                {
                    Thread.Sleep(100);

                    if (redisConn == null || !redisConn.IsConnected)
                    {
                        Console.WriteLine("🔄 Reconnecting Redis");
                        redisConn = OpenRedisConnection(redisHost);
                        redis = redisConn.GetDatabase();
                    }

                    string json = redis.ListLeftPopAsync("votes").Result;
                    if (json != null)
                    {
                        var vote = JsonConvert.DeserializeAnonymousType(json, definition);
                        Console.WriteLine($"📥 Processing vote for '{vote.vote}' by '{vote.voter_id}'");

                        if (pgsql.State != System.Data.ConnectionState.Open)
                        {
                            Console.WriteLine("🔄 Reconnecting DB");
                            pgsql = OpenDbConnection(pgConnString);
                        }
                        else
                        {
                            UpdateVote(pgsql, vote.voter_id, vote.vote);
                        }
                    }
                    else
                    {
                        keepAliveCommand.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"🔥 {ex}");
                return 1;
            }
        }

        private static string MaskPgConnString(string connString)
        {
            try
            {
                var parts = connString.Split(';');
                var host = parts.FirstOrDefault(p => p.StartsWith("Host="))?.Split('=')[1];
                var port = parts.FirstOrDefault(p => p.StartsWith("Port="))?.Split('=')[1];
                var db = parts.FirstOrDefault(p => p.StartsWith("Database="))?.Split('=')[1];
                return $"postgres://****:****@{host}:{port}/{db}";
            }
            catch
            {
                return "Invalid connection string format";
            }
        }

        private static NpgsqlConnection OpenDbConnection(string connectionString)
        {
            NpgsqlConnection connection;

            while (true)
            {
                try
                {
                    connection = new NpgsqlConnection(connectionString);
                    connection.Open();
                    break;
                }
                catch (SocketException)
                {
                    Console.Error.WriteLine("⏳ Waiting for DB (SocketException)");
                    Thread.Sleep(1000);
                }
                catch (DbException)
                {
                    Console.Error.WriteLine("⏳ Waiting for DB (DbException)");
                    Thread.Sleep(1000);
                }
            }

            Console.WriteLine("✅ Connected to DB");

            var command = connection.CreateCommand();
            command.CommandText = @"CREATE TABLE IF NOT EXISTS votes (
                                        id VARCHAR(255) NOT NULL UNIQUE,
                                        vote VARCHAR(255) NOT NULL
                                    )";
            command.ExecuteNonQuery();

            return connection;
        }

        private static ConnectionMultiplexer OpenRedisConnection(string hostname)
        {
            string ipAddress = GetIp(hostname);
            Console.WriteLine($"🌐 Found Redis at {ipAddress}");

            while (true)
            {
                try
                {
                    Console.WriteLine("🔌 Connecting to Redis");
                    return ConnectionMultiplexer.Connect(ipAddress);
                }
                catch (RedisConnectionException)
                {
                    Console.Error.WriteLine("⏳ Waiting for Redis");
                    Thread.Sleep(1000);
                }
            }
        }

        private static string GetIp(string hostname)
            => Dns.GetHostEntryAsync(hostname)
                .Result
                .AddressList
                .First(a => a.AddressFamily == AddressFamily.InterNetwork)
                .ToString();

        private static void UpdateVote(NpgsqlConnection connection, string voterId, string vote)
        {
            var command = connection.CreateCommand();
            try
            {
                command.CommandText = "INSERT INTO votes (id, vote) VALUES (@id, @vote)";
                command.Parameters.AddWithValue("@id", voterId);
                command.Parameters.AddWithValue("@vote", vote);
                command.ExecuteNonQuery();
            }
            catch (DbException)
            {
                command.CommandText = "UPDATE votes SET vote = @vote WHERE id = @id";
                command.ExecuteNonQuery();
            }
            finally
            {
                command.Dispose();
            }
        }
    }
}
