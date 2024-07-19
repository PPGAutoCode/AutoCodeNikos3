
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using ProjectName.Types;
using ProjectName.Interfaces;
using ProjectName.ControllersExceptions;

namespace ProjectName.Services
{
    public class APIEndpointService : IAPIEndpointService
    {
        private readonly IDbConnection _dbConnection;

        public APIEndpointService(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<List<APIEndpoint>> GetListAPIEndpoint(ListAPIEndpointRequestDto requestDto)
        {
            // Step 1: Validate the requestDto
            if (requestDto.PageLimit <= 0 || requestDto.PageOffset < 0)
            {
                throw new TechnicalException("DP-400", "Technical Error");
            }

            // Step 2: Fetch API Endpoints
            var sortField = requestDto.SortField ?? "Id";
            var sortOrder = requestDto.SortOrder ?? "asc";
            var apiEndpoints = await _dbConnection.QueryAsync<APIEndpoint>(
                $"SELECT * FROM ApiEndpoints ORDER BY {sortField} {sortOrder} OFFSET @PageOffset ROWS FETCH NEXT @PageLimit ROWS ONLY",
                new { requestDto.PageOffset, requestDto.PageLimit });

            if (!apiEndpoints.Any())
            {
                return new List<APIEndpoint>();
            }

            // Step 3: Fetch Related Tags
            var endpointIds = apiEndpoints.Select(e => e.Id).ToList();
            var tagIds = await _dbConnection.QueryAsync<Guid>(
                "SELECT ApiTagId FROM APIEndpointTags WHERE ApiEndpointId IN @EndpointIds",
                new { EndpointIds = endpointIds });

            var tags = await _dbConnection.QueryAsync<ApiTag>(
                "SELECT * FROM ApiTags WHERE Id IN @TagIds",
                new { TagIds = tagIds });

            if (tags.Count() != tagIds.Count())
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            // Step 4: Response Preparation
            var tagDictionary = tags.ToDictionary(t => t.Id);
            foreach (var endpoint in apiEndpoints)
            {
                endpoint.ApiTags = tagIds
                    .Where(id => tagDictionary.ContainsKey(id))
                    .Select(id => tagDictionary[id])
                    .ToList();
            }

            return apiEndpoints.ToList();
        }
    }
}
