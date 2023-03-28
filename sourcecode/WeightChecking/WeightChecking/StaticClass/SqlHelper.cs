using Dapper;
using DevExpress.XtraReports.Design;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WeightChecking.StaticClass
{
    public class SqlHelper
    {
        private string ConnectionString;
        public SqlHelper(string connectionString)
        {
            ConnectionString = connectionString;
        }

        private IDbConnection CreateConnection()
        {
            return new SqlConnection(ConnectionString);
        }

        public async Task<int> ExecuteNonQueryWithDynamicParamAsync(string query, DynamicParameters dynamicParameters)
        {
            using (IDbConnection connection = CreateConnection())
            {
                var res = await connection.ExecuteAsync(query, param: dynamicParameters, commandType: CommandType.StoredProcedure);

                return res;
            }
        }

        public async Task<int> ExecuteTransactionAsync(string query, DynamicParameters dynamicParameters)
        {
            int res = 0;
            using (IDbConnection connection = CreateConnection())
            {
                if (connection.State == ConnectionState.Closed)
                    connection.Open();

                using (var tran = connection.BeginTransaction())
                {
                    try
                    {
                        res = await connection.ExecuteAsync(query, param: dynamicParameters, commandType: CommandType.StoredProcedure, transaction: tran);
                        if (res > 0)
                            tran.Commit();
                        else tran.Rollback();
                    }
                    catch (Exception ex)
                    {
                        tran.Rollback();
                    }

                }

                return res;
            }
        }
    }
}
