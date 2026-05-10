using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace EvenTech.DAL
{
    // Conexion centralizada a SQL Server. Hardcodea la connection string a
    // localhost\SQLEXPRESS porque EvenTech todavia no tiene un gestor de config.
    public class DAL_DB_Connection : IDisposable
    {
        public const string ConnectionString =
            @"Data Source=localhost\SQLEXPRESS;Initial Catalog=EvenTechDB;Integrated Security=True;TrustServerCertificate=True";

        private readonly SqlConnection _connection;

        public DAL_DB_Connection()
        {
            _connection = new SqlConnection(ConnectionString);
        }

        public SqlConnection Connection => _connection;

        public SqlConnection OpenConnection()
        {
            if (_connection.State == ConnectionState.Closed)
                _connection.Open();
            return _connection;
        }

        public void CloseConnection()
        {
            if (_connection.State == ConnectionState.Open)
                _connection.Close();
        }

        public void Dispose()
        {
            if (_connection != null)
            {
                if (_connection.State == ConnectionState.Open)
                    _connection.Close();
                _connection.Dispose();
            }
        }
    }
}
