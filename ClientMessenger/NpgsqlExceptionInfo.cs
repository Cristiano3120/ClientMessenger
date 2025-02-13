﻿using System.Text.Json.Serialization;

namespace ClientMessenger
{
    internal sealed record NpgsqlExceptionInfos
    {
        [JsonPropertyName("npgsqlExceptions")]
        public NpgsqlExceptions Exception { get; init; }
        [JsonPropertyName("columnName")]
        public string ColumnName { get; init; }

        #region Constructors

        public NpgsqlExceptionInfos() : this(NpgsqlExceptions.None, "")
        {

        }

        public NpgsqlExceptionInfos(NpgsqlExceptions exception, string columnName)
        {
            Exception = exception;
            ColumnName = columnName;
        }

        #endregion

        public void Deconstruct(out NpgsqlExceptions exception, out string columnName)
        {
            exception = Exception;
            columnName = ColumnName;
        }
    }
}
