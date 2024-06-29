using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace System.Data.Common;

public abstract class DbDataSource : IDisposable, IAsyncDisposable
{
	private sealed class DbCommandWrapper : DbCommand
	{
		private readonly DbCommand _wrappedCommand;

		private readonly DbConnection _connection;

		public override string CommandText
		{
			get
			{
				return _wrappedCommand.CommandText;
			}
			[param: AllowNull]
			set
			{
				_wrappedCommand.CommandText = value;
			}
		}

		public override int CommandTimeout
		{
			get
			{
				return _wrappedCommand.CommandTimeout;
			}
			set
			{
				_wrappedCommand.CommandTimeout = value;
			}
		}

		public override CommandType CommandType
		{
			get
			{
				return _wrappedCommand.CommandType;
			}
			set
			{
				_wrappedCommand.CommandType = value;
			}
		}

		protected override DbParameterCollection DbParameterCollection => _wrappedCommand.Parameters;

		public override bool DesignTimeVisible
		{
			get
			{
				return _wrappedCommand.DesignTimeVisible;
			}
			set
			{
				_wrappedCommand.DesignTimeVisible = value;
			}
		}

		public override UpdateRowSource UpdatedRowSource
		{
			get
			{
				return _wrappedCommand.UpdatedRowSource;
			}
			set
			{
				_wrappedCommand.UpdatedRowSource = value;
			}
		}

		protected override DbConnection DbConnection
		{
			get
			{
				throw ExceptionBuilder.NotSupportedOnDataSourceCommand();
			}
			set
			{
				throw ExceptionBuilder.NotSupportedOnDataSourceCommand();
			}
		}

		protected override DbTransaction DbTransaction
		{
			get
			{
				throw ExceptionBuilder.NotSupportedOnDataSourceCommand();
			}
			set
			{
				throw ExceptionBuilder.NotSupportedOnDataSourceCommand();
			}
		}

		internal DbCommandWrapper(DbCommand wrappedCommand)
		{
			_wrappedCommand = wrappedCommand;
			_connection = wrappedCommand.Connection;
		}

		public override int ExecuteNonQuery()
		{
			_connection.Open();
			try
			{
				return _wrappedCommand.ExecuteNonQuery();
			}
			finally
			{
				try
				{
					_connection.Close();
				}
				catch (Exception e)
				{
					ExceptionBuilder.TraceExceptionWithoutRethrow(e);
				}
			}
		}

		public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
		{
			await _connection.OpenAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			int result;
			try
			{
				result = await _wrappedCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			finally
			{
				try
				{
					await _connection.CloseAsync().ConfigureAwait(continueOnCapturedContext: false);
				}
				catch (Exception e)
				{
					ExceptionBuilder.TraceExceptionWithoutRethrow(e);
				}
			}
			return result;
		}

		public override object ExecuteScalar()
		{
			_connection.Open();
			try
			{
				return _wrappedCommand.ExecuteScalar();
			}
			finally
			{
				try
				{
					_connection.Close();
				}
				catch (Exception e)
				{
					ExceptionBuilder.TraceExceptionWithoutRethrow(e);
				}
			}
		}

		public override async Task<object> ExecuteScalarAsync(CancellationToken cancellationToken)
		{
			await _connection.OpenAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			object result;
			try
			{
				result = await _wrappedCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			finally
			{
				try
				{
					await _connection.CloseAsync().ConfigureAwait(continueOnCapturedContext: false);
				}
				catch (Exception e)
				{
					ExceptionBuilder.TraceExceptionWithoutRethrow(e);
				}
			}
			return result;
		}

		protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
		{
			_connection.Open();
			try
			{
				return _wrappedCommand.ExecuteReader(behavior | CommandBehavior.CloseConnection);
			}
			catch
			{
				try
				{
					_connection.Close();
				}
				catch (Exception e)
				{
					ExceptionBuilder.TraceExceptionWithoutRethrow(e);
				}
				throw;
			}
		}

		protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
		{
			await _connection.OpenAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			try
			{
				return await _wrappedCommand.ExecuteReaderAsync(behavior | CommandBehavior.CloseConnection, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch
			{
				try
				{
					await _connection.CloseAsync().ConfigureAwait(continueOnCapturedContext: false);
				}
				catch (Exception e)
				{
					ExceptionBuilder.TraceExceptionWithoutRethrow(e);
				}
				throw;
			}
		}

		protected override DbParameter CreateDbParameter()
		{
			return _wrappedCommand.CreateParameter();
		}

		public override void Cancel()
		{
			_wrappedCommand.Cancel();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				DbConnection connection = _wrappedCommand.Connection;
				_wrappedCommand.Dispose();
				connection.Dispose();
			}
		}

		public override async ValueTask DisposeAsync()
		{
			DbConnection connection = _wrappedCommand.Connection;
			await _wrappedCommand.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
			await connection.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
		}

		public override void Prepare()
		{
			throw ExceptionBuilder.NotSupportedOnDataSourceCommand();
		}

		public override Task PrepareAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			return Task.FromException(ExceptionBuilder.NotSupportedOnDataSourceCommand());
		}
	}

	private sealed class DbBatchWrapper : DbBatch
	{
		private readonly DbBatch _wrappedBatch;

		private readonly DbConnection _connection;

		protected override DbBatchCommandCollection DbBatchCommands => _wrappedBatch.BatchCommands;

		public override int Timeout
		{
			get
			{
				return _wrappedBatch.Timeout;
			}
			set
			{
				_wrappedBatch.Timeout = value;
			}
		}

		protected override DbConnection DbConnection
		{
			get
			{
				throw ExceptionBuilder.NotSupportedOnDataSourceBatch();
			}
			set
			{
				throw ExceptionBuilder.NotSupportedOnDataSourceBatch();
			}
		}

		protected override DbTransaction DbTransaction
		{
			get
			{
				throw ExceptionBuilder.NotSupportedOnDataSourceBatch();
			}
			set
			{
				throw ExceptionBuilder.NotSupportedOnDataSourceBatch();
			}
		}

		internal DbBatchWrapper(DbBatch wrappedBatch)
		{
			_wrappedBatch = wrappedBatch;
			_connection = wrappedBatch.Connection;
		}

		public override int ExecuteNonQuery()
		{
			_connection.Open();
			try
			{
				return _wrappedBatch.ExecuteNonQuery();
			}
			finally
			{
				try
				{
					_connection.Close();
				}
				catch (Exception e)
				{
					ExceptionBuilder.TraceExceptionWithoutRethrow(e);
				}
			}
		}

		public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
		{
			await _connection.OpenAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			int result;
			try
			{
				result = await _wrappedBatch.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			finally
			{
				try
				{
					await _connection.CloseAsync().ConfigureAwait(continueOnCapturedContext: false);
				}
				catch (Exception e)
				{
					ExceptionBuilder.TraceExceptionWithoutRethrow(e);
				}
			}
			return result;
		}

		public override object ExecuteScalar()
		{
			_connection.Open();
			try
			{
				return _wrappedBatch.ExecuteScalar();
			}
			finally
			{
				try
				{
					_connection.Close();
				}
				catch (Exception e)
				{
					ExceptionBuilder.TraceExceptionWithoutRethrow(e);
				}
			}
		}

		public override async Task<object> ExecuteScalarAsync(CancellationToken cancellationToken)
		{
			await _connection.OpenAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			object result;
			try
			{
				result = await _wrappedBatch.ExecuteScalarAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			finally
			{
				try
				{
					await _connection.CloseAsync().ConfigureAwait(continueOnCapturedContext: false);
				}
				catch (Exception e)
				{
					ExceptionBuilder.TraceExceptionWithoutRethrow(e);
				}
			}
			return result;
		}

		protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
		{
			_connection.Open();
			try
			{
				return _wrappedBatch.ExecuteReader(behavior | CommandBehavior.CloseConnection);
			}
			catch
			{
				try
				{
					_connection.Close();
				}
				catch (Exception e)
				{
					ExceptionBuilder.TraceExceptionWithoutRethrow(e);
				}
				throw;
			}
		}

		protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
		{
			await _connection.OpenAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			try
			{
				return await _wrappedBatch.ExecuteReaderAsync(behavior | CommandBehavior.CloseConnection, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch
			{
				try
				{
					await _connection.CloseAsync().ConfigureAwait(continueOnCapturedContext: false);
				}
				catch (Exception e)
				{
					ExceptionBuilder.TraceExceptionWithoutRethrow(e);
				}
				throw;
			}
		}

		protected override DbBatchCommand CreateDbBatchCommand()
		{
			throw new NotImplementedException();
		}

		public override void Cancel()
		{
			_wrappedBatch.Cancel();
		}

		public override void Dispose()
		{
			DbConnection connection = _wrappedBatch.Connection;
			_wrappedBatch.Dispose();
			connection.Dispose();
		}

		public override async ValueTask DisposeAsync()
		{
			DbConnection connection = _wrappedBatch.Connection;
			await _wrappedBatch.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
			await connection.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
		}

		public override void Prepare()
		{
			throw ExceptionBuilder.NotSupportedOnDataSourceCommand();
		}

		public override Task PrepareAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			return Task.FromException(ExceptionBuilder.NotSupportedOnDataSourceCommand());
		}
	}

	public abstract string ConnectionString { get; }

	protected abstract DbConnection CreateDbConnection();

	protected virtual DbConnection OpenDbConnection()
	{
		DbConnection dbConnection = CreateDbConnection();
		try
		{
			dbConnection.Open();
			return dbConnection;
		}
		catch
		{
			dbConnection.Dispose();
			throw;
		}
	}

	protected virtual async ValueTask<DbConnection> OpenDbConnectionAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		DbConnection connection = CreateDbConnection();
		try
		{
			await connection.OpenAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			return connection;
		}
		catch
		{
			await connection.DisposeAsync().ConfigureAwait(continueOnCapturedContext: false);
			throw;
		}
	}

	protected virtual DbCommand CreateDbCommand(string? commandText = null)
	{
		DbCommand dbCommand = CreateDbConnection().CreateCommand();
		dbCommand.CommandText = commandText;
		return new DbCommandWrapper(dbCommand);
	}

	protected virtual DbBatch CreateDbBatch()
	{
		return new DbBatchWrapper(CreateDbConnection().CreateBatch());
	}

	public DbConnection CreateConnection()
	{
		return CreateDbConnection();
	}

	public DbConnection OpenConnection()
	{
		return OpenDbConnection();
	}

	public ValueTask<DbConnection> OpenConnectionAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		return OpenDbConnectionAsync(cancellationToken);
	}

	public DbCommand CreateCommand(string? commandText = null)
	{
		return CreateDbCommand(commandText);
	}

	public DbBatch CreateBatch()
	{
		return CreateDbBatch();
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	public async ValueTask DisposeAsync()
	{
		await DisposeAsyncCore().ConfigureAwait(continueOnCapturedContext: false);
		Dispose(disposing: false);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
	}

	protected virtual ValueTask DisposeAsyncCore()
	{
		return default(ValueTask);
	}
}
