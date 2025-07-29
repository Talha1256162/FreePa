namespace NextGenArchitecture.Application.Common.Interfaces;

/// <summary>
/// Unit of Work interface for managing database transactions and ensuring consistency.
/// Implements the Unit of Work pattern to coordinate changes across multiple repositories.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Saves all pending changes to the database.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of affected entities.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins a new database transaction.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The database transaction.</returns>
    Task<IDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a repository for the specified entity type.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <returns>The repository for the entity type.</returns>
    IRepository<T> Repository<T>() where T : class, NextGenArchitecture.SharedKernel.Abstractions.IEntity;

    /// <summary>
    /// Executes a function within a database transaction.
    /// The transaction is automatically committed if the function succeeds, or rolled back if it fails.
    /// </summary>
    /// <typeparam name="T">The return type of the function.</typeparam>
    /// <param name="func">The function to execute within the transaction.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the function.</returns>
    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> func, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an action within a database transaction.
    /// The transaction is automatically committed if the action succeeds, or rolled back if it fails.
    /// </summary>
    /// <param name="action">The action to execute within the transaction.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken cancellationToken = default);
}

/// <summary>
/// Database transaction interface for managing transaction lifecycle.
/// </summary>
public interface IDbTransaction : IDisposable
{
    /// <summary>
    /// Commits the transaction.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the transaction.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}