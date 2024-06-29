namespace System.Data.Common;

internal sealed class DefaultDataSource : DbDataSource
{
	private readonly DbProviderFactory _dbProviderFactory;

	private readonly string _connectionString;

	public override string ConnectionString => _connectionString;

	internal DefaultDataSource(DbProviderFactory dbProviderFactory, string connectionString)
	{
		_dbProviderFactory = dbProviderFactory;
		_connectionString = connectionString;
	}

	protected override DbConnection CreateDbConnection()
	{
		DbConnection dbConnection = _dbProviderFactory.CreateConnection();
		if (dbConnection == null)
		{
			throw new InvalidOperationException("DbProviderFactory returned a null connection");
		}
		dbConnection.ConnectionString = _connectionString;
		return dbConnection;
	}
}
