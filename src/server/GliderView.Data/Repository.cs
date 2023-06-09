using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GliderView.Data
{
    public abstract class SqlRepository
    {
        protected string ConnectionString { get; private set; }

        public SqlRepository(string connectionString)
        {
            ConnectionString = connectionString;
        }

        protected SqlConnection GetOpenConnection()
        {
            var con = new SqlConnection(ConnectionString);
            if (con.State != ConnectionState.Open)
            {
                con.Open();
            }
            return con;
        }

        protected async Task<SqlConnection> GetOpenConnectionAsync()
        {
            var con = new SqlConnection(ConnectionString);
            if (con.State != ConnectionState.Open)
            {
                await con.OpenAsync();
            }
            return con;
        }
    }
}
