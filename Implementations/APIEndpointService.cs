
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using ProjectName.ControllersExceptions;
using ProjectName.Interfaces;
using ProjectName.Types;

namespace ProjectName.Services
{
    public class APIEndpointService : IAPIEndpointService
    {
        private readonly IDbConnection _dbConnection;

        public APIEndpointService(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        public async Task<List<APIEndpoint>> GetListAPIEndpoint(ListAPIEndpointRequestDto request)
        {
            // Step 1: Validate Input
            if (request == null || request.PageLimit <= 0 || request.PageOffset < 0)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            // Step 2: Fetch API Endpoints
            var query = "SELECT * FROM APIEndpoints";
            if (!string.IsNullOrEmpty(request.SortField) && !string.IsNullOrEmpty(request.SortOrder))
            {
                query += $" ORDER BY {request.SortField} {request.SortOrder}";
            }
            query += $" OFFSET {request.PageOffset} ROWS FETCH NEXT {request.PageLimit} ROWS ONLY";

            var apiEndpoints = await _dbConnection.QueryAsync<APIEndpoint>(query);

            // Step 3: Pagination Check
            if (request.PageLimit == 0 && request.PageOffset == 0)
            {
                throw new TechnicalException("DP-400", "Technical Error");
            }

            // Step 4: Fetch Related Tags
            var apiEndpointIds = apiEndpoints.Select(ae => ae.Id).ToList();
            var tagQuery = "SELECT * FROM APIEndpointTags WHERE APIEndpointId IN @apiEndpointIds";
            var apiEndpointTags = await _dbConnection.QueryAsync<ApiTag>(tagQuery, new { apiEndpointIds });

            if (apiEndpointTags == null || !apiEndpointTags.Any())
            {
                throw new TechnicalException("DP-404", "Technical Error");
            }

            // Step 5: Response Preparation
            var response = apiEndpoints.Select(ae => new APIEndpoint
            {
                Id = ae.Id,
                ApiName = ae.ApiName,
                ApiScope = ae.ApiScope,
                ApiScopeProduction = ae.ApiScopeProduction,
                ApiTags = apiEndpointTags.Where(tag => tag.APIEndpointId == ae.Id).ToList(),
                Deprecated = ae.Deprecated,
                Description = ae.Description,
                Documentation = ae.Documentation,
                EndpointUrls = ae.EndpointUrls,
                AppEnvironment = ae.AppEnvironment,
                Swagger = ae.Swagger,
                Tour = ae.Tour,
                ApiVersion = ae.ApiVersion,
                Langcode = ae.Langcode,
                Sticky = ae.Sticky,
                Promote = ae.Promote,
                UrlAlias = ae.UrlAlias,
                Published = ae.Published
            }).ToList();

            // Step 6: Return the Response
            return response;
        }
    }
}
