using Amazon.DynamoDBv2.DataModel;
using FiapX.Application.ProcessingJobs.Repositories;
using FiapX.Domain.ProcessingJobs;
using FiapX.Infra.Data.Mappers;
using FiapX.Infra.Data.Models;
using FiapX.Infra.Data.Options;
using Microsoft.Extensions.Options;

namespace FiapX.Infra.Data.Repositories;

public sealed class ProcessingJobRepository(
    IDynamoDBContext context,
    IOptions<TableNames> tableNames) : IProcessingJobRepository
{
    private const string UserIdIndexName = "userId-index";
    private const string ResultFileIdIndexName = "resultFileId-index";
    private readonly string _tableName = tableNames.Value.ProcessingJobs;

    public async Task<ProcessingJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var model = await context.LoadAsync<ProcessingJobModel>(
            id.ToString(),
            new LoadConfig { OverrideTableName = _tableName },
            cancellationToken);

        return model?.ToDomain();
    }

    public async Task<ProcessingJob?> GetByIdempotencyKeyAsync(
        string userId,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var models = await QueryByUserAsync(userId, cancellationToken);
        var model = models.FirstOrDefault(model =>
            string.Equals(model.IdempotencyKey, idempotencyKey, StringComparison.Ordinal));

        return model?.ToDomain();
    }

    public async Task<ProcessingJob?> GetByResultFileIdAsync(Guid fileId, CancellationToken cancellationToken)
    {
        var search = context.QueryAsync<ProcessingJobModel>(
            fileId.ToString(),
            new QueryConfig
            {
                OverrideTableName = _tableName,
                IndexName = ResultFileIdIndexName
            });

        var models = await search.GetRemainingAsync(cancellationToken);
        return models.FirstOrDefault()?.ToDomain();
    }

    public async Task<IReadOnlyList<ProcessingJob>> ListByUserAsync(
        string userId,
        ProcessingStatus? status,
        int page,
        int size,
        CancellationToken cancellationToken)
    {
        var models = await QueryByUserAsync(userId, cancellationToken);
        var filtered = FilterByStatus(models, status);

        return filtered
            .OrderByDescending(model => DateTimeOffset.Parse(model.CreatedAt))
            .Skip((page - 1) * size)
            .Take(size)
            .Select(model => model.ToDomain())
            .ToList();
    }

    public async Task<int> CountByUserAsync(
        string userId,
        ProcessingStatus? status,
        CancellationToken cancellationToken)
    {
        var models = await QueryByUserAsync(userId, cancellationToken);
        return FilterByStatus(models, status).Count();
    }

    public async Task SaveAsync(ProcessingJob processingJob, CancellationToken cancellationToken)
    {
        await context.SaveAsync(
            ProcessingJobModelMapper.FromDomain(processingJob),
            new SaveConfig { OverrideTableName = _tableName },
            cancellationToken);
    }

    private async Task<IReadOnlyList<ProcessingJobModel>> QueryByUserAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        var search = context.QueryAsync<ProcessingJobModel>(
            userId,
            new QueryConfig
            {
                OverrideTableName = _tableName,
                IndexName = UserIdIndexName
            });

        return await search.GetRemainingAsync(cancellationToken);
    }

    private static IEnumerable<ProcessingJobModel> FilterByStatus(
        IEnumerable<ProcessingJobModel> models,
        ProcessingStatus? status)
    {
        if (status is null)
            return models;

        var storageStatus = ProcessingJobModelMapper.ToStorageStatus(status.Value);
        return models.Where(model => model.Status == storageStatus);
    }
}
