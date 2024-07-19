
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
        private readonly IApiTagService _apiTagService;

        public APIEndpointService(IDbConnection dbConnection, IApiTagService apiTagService)
        {
            _dbConnection = dbConnection;
            _apiTagService = apiTagService;
        }

        public async Task<List<APIEndpoint>> GetListAPIEndpoint(ListAPIEndpointRequestDto requestDto)
        {
            // Step 1: Validate the requestDto
            if (requestDto.PageLimit <= 0 || requestDto.PageOffset < 0)
            {
                throw new BusinessException("DP-422", "Client Error");
            }

            // Step 2: Fetch API Endpoints
            var sortField = requestDto.SortField ?? "Id";
            var sortOrder = requestDto.SortOrder ?? "asc";
            var query = $@"
                SELECT * FROM ApiEndpoints
                ORDER BY {sortField} {sortOrder}
                OFFSET {requestDto.PageOffset} ROWS
                FETCH NEXT {requestDto.PageLimit} ROWS ONLY;
            ";

            var apiEndpoints = await _dbConnection.QueryAsync<APIEndpoint>(query);

            // Step 3: Fetch and Map Related Tags
            foreach (var apiEndpoint in apiEndpoints)
            {
                var tagIds = await _dbConnection.QueryAsync<Guid>(@"
                    SELECT ApiTagId FROM APIEndpointTags
                    WHERE APIEndpointId = @APIEndpointId
                ", new { APIEndpointId = apiEndpoint.Id });

                var tags = new List<ApiTag>();
                foreach (var tagId in tagIds)
                {
                    var tagRequestDto = new ApiTagRequestDto { Id = tagId };
                    var tag = await _apiTagService.GetApiTag(tagRequestDto);
                    if (tag == null)
                    {
                        throw new TechnicalException("DP-404", "Technical Error");
                    }
                    tags.Add(tag);
                }
                apiEndpoint.ApiTags = tags;
            }

            return apiEndpoints.ToList();
        }
    }
}
