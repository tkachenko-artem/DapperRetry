﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using AnyRetry;
using Dapper;
using Microsoft.Extensions.Logging;

namespace Aionys.Dapper
{
    public class DapperRetry
    {
        private const int DefaultRetryLimit = 5;

        private readonly TimeSpan _retryEvery = TimeSpan.FromSeconds(5);
        private readonly ILogger<DapperRetry> _logger;
        private readonly int _retryLimit;

        public DapperRetry(int retryLimit = DefaultRetryLimit)
        {
            _retryLimit = retryLimit;
        }

        public DapperRetry(ILogger<DapperRetry> logger, int retryLimit = DefaultRetryLimit)
        {
            _logger = logger;
            _retryLimit = retryLimit;
        }

        #region QueryAsync<T>
        public async Task<IEnumerable<T>> QueryAsync<T>(string connectionString, string sql, object parameters = null, int? retryLimit = null)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                return await QueryAsync<T>(db, sql, parameters, retryLimit);
            }
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(IDbConnection db, string sql, object parameters = null, int? retryLimit = null)
        {
            return await RetryActionAsync(db.QueryAsync<T>(sql, parameters), retryLimit);
        }
        #endregion

        #region QueryFirstOrDefaultAsync<T>
        public async Task<T> QueryFirstOrDefaultAsync<T>(string connectionString, string sql, object parameters = null, int? retryLimit = null)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                return await QueryFirstOrDefaultAsync<T>(db, sql, parameters, retryLimit);
            }
        }

        public async Task<T> QueryFirstOrDefaultAsync<T>(IDbConnection db, string sql, object parameters = null, int? retryLimit = null)
        {
            return await RetryActionAsync(db.QueryFirstOrDefaultAsync<T>(sql, parameters), retryLimit);
        }
        #endregion

        #region ExecuteAsync
        public async Task<int> ExecuteAsync(string connectionString, string sql, object parameters = null, int? retryLimit = null)
        {
            using (IDbConnection db = new SqlConnection(connectionString))
            {
                return await ExecuteAsync(db, sql, parameters, retryLimit);
            }
        }

        public async Task<int> ExecuteAsync(IDbConnection db, string sql, object parameters = null, int? retryLimit = null)
        {
            return await RetryActionAsync(db.ExecuteAsync(sql, parameters), retryLimit);
        }
        #endregion

        private async Task<T> RetryActionAsync<T>(Task<T> task, int? retryLimit = null)
        {
            T result = default;

            await Retry.DoAsync(async (retryIteration, maxRetryCount) =>
            {
                _logger?.LogInformation($"Dapper retry #: {retryIteration}");

                result = await task;
            }, _retryEvery, retryLimit ?? _retryLimit, onFailure: ((exception, retryIteration, maxRetryCount) =>
            {
                _logger?.LogError(exception, $"Dapper retry #: {retryIteration}");
            }));

            return result;
        }
    }
}